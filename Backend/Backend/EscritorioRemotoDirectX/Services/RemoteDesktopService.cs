using System;
using System.IO;
using System.Threading;
using System.Drawing;
using System.Drawing.Imaging;
using SharpDX.Direct3D11;
using SharpDX.Direct3D;
using WebSocketSharp;
using WebSocketSharp.Server;
using Newtonsoft.Json;
using SharpDX.DXGI;
using EscritorioRemotoDirectX.Models;
using SharpDX;

namespace EscritorioRemotoDirectX.Services
{
    public class RemoteDesktop : WebSocketBehavior
    {
        private static readonly object captureLock = new object();
        private static bool _isRunning = false;
        private static SharpDX.Direct3D11.Device _device;
        private static OutputDuplication _outputDuplication;
        private static int _captureInterval = 100;
        private static Bitmap _previousCapture;

        public RemoteDesktop()
        {
            InitializeDirectX();
        }

        protected override void OnMessage(MessageEventArgs e)
        {
            if (e.Data == "capture")
            {
                StartCapture();
            }
            else if (e.Data == "stop")
            {
                StopCapture();
            }
            else
            {
                var eventData = JsonConvert.DeserializeObject<EventData>(e.Data);
                InputHandlingService.HandleInputEvent(eventData);
            }
        }

        protected override void OnOpen()
        {
            Console.WriteLine("WebSocket connection opened");
        }

        protected override void OnClose(CloseEventArgs e)
        {
            Console.WriteLine("WebSocket connection closed");
            StopCapture();
        }

        protected override void OnError(WebSocketSharp.ErrorEventArgs e)
        {
            Console.WriteLine("WebSocket error: " + e.Message);
            StopCapture();
        }

        private void InitializeDirectX()
        {
            if (_device == null)
            {
                var factory = new Factory1();
                var adapter = factory.GetAdapter1(0);
                var output = adapter.GetOutput(0);
                var output1 = output.QueryInterface<Output1>();

                _device = new SharpDX.Direct3D11.Device(DriverType.Hardware, DeviceCreationFlags.BgraSupport);
                _outputDuplication = output1.DuplicateOutput(_device);

                output1.Dispose();
                output.Dispose();
                adapter.Dispose();
                factory.Dispose();
            }
        }

        private void StartCapture()
        {
            if (_isRunning) return;
            _isRunning = true;

            ThreadPool.QueueUserWorkItem(state =>
            {
                while (_isRunning)
                {
                    lock (captureLock)
                    {
                        Bitmap capture = ScreenCaptureService.CaptureScreen(_device, _outputDuplication);
                        if (capture != null)
                        {
                            if (_previousCapture != null)
                            {
                                Bitmap xorBitmap = ApplyXor(_previousCapture, capture);
                                using (MemoryStream ms = new MemoryStream())
                                {
                                    xorBitmap.Save(ms, ImageFormat.Png);
                                    if (this.State == WebSocketState.Open)
                                    {
                                        Send(ms.ToArray());
                                    }
                                }
                                xorBitmap.Dispose();
                            }
                            else
                            {
                                using (MemoryStream ms = new MemoryStream())
                                {
                                    capture.Save(ms, ImageFormat.Png);
                                    if (this.State == WebSocketState.Open)
                                    {
                                        Send(ms.ToArray());
                                    }
                                }
                            }

                            if (_previousCapture != null)
                            {
                                _previousCapture.Dispose();
                            }
                            _previousCapture = (Bitmap)capture.Clone();
                        }
                        else
                        {
                            Console.WriteLine("Error: capture is null.");
                        }
                    }
                    Thread.Sleep(_captureInterval);
                }
            });
        }

        private void StopCapture()
        {
            lock (captureLock)
            {
                _isRunning = false;
            }
        }

        private Bitmap ApplyXor(Bitmap previousCapture, Bitmap currentCapture)
        {
            int width = previousCapture.Width;
            int height = previousCapture.Height;
            Bitmap xorBitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);

            BitmapData previousData = previousCapture.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            BitmapData currentData = currentCapture.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            BitmapData xorData = xorBitmap.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);

            int bytesPerPixel = Image.GetPixelFormatSize(PixelFormat.Format32bppArgb) / 8;
            int heightInPixels = previousData.Height;
            int widthInBytes = previousData.Width * bytesPerPixel;

            unsafe
            {
                byte* prevPtr = (byte*)previousData.Scan0;
                byte* currPtr = (byte*)currentData.Scan0;
                byte* xorPtr = (byte*)xorData.Scan0;

                for (int y = 0; y < heightInPixels; y++)
                {
                    byte* prevRow = prevPtr + (y * previousData.Stride);
                    byte* currRow = currPtr + (y * currentData.Stride);
                    byte* xorRow = xorPtr + (y * xorData.Stride);

                    for (int x = 0; x < widthInBytes; x += bytesPerPixel)
                    {
                        xorRow[x] = (byte)(currRow[x] ^ prevRow[x]);         // Blue
                        xorRow[x + 1] = (byte)(currRow[x + 1] ^ prevRow[x + 1]); // Green
                        xorRow[x + 2] = (byte)(currRow[x + 2] ^ prevRow[x + 2]); // Red
                        xorRow[x + 3] = 255; // Alpha
                    }
                }
            }

            previousCapture.UnlockBits(previousData);
            currentCapture.UnlockBits(currentData);
            xorBitmap.UnlockBits(xorData);

            return xorBitmap;
        }
    }
}

public static class ScreenCaptureService
{
    public static Bitmap CaptureScreen(SharpDX.Direct3D11.Device device, OutputDuplication outputDuplication)
    {
        Bitmap bitmap = null;
        try
        {
            if (device == null || outputDuplication == null)
            {
                throw new ArgumentNullException("Device or OutputDuplication is null");
            }

            var factory = new Factory1();
            var adapter = factory.GetAdapter1(0);
            var output = adapter.GetOutput(0);
            var output1 = output.QueryInterface<Output1>();

            var bounds = output.Description.DesktopBounds;
            var width = bounds.Right - bounds.Left;
            var height = bounds.Bottom - bounds.Top;

            using (var screenTexture = new Texture2D(device, new Texture2DDescription
            {
                Width = width,
                Height = height,
                MipLevels = 1,
                ArraySize = 1,
                Format = Format.B8G8R8A8_UNorm,
                SampleDescription = new SampleDescription(1, 0),
                Usage = ResourceUsage.Staging,
                BindFlags = BindFlags.None,
                CpuAccessFlags = CpuAccessFlags.Read,
                OptionFlags = ResourceOptionFlags.None
            }))
            {
                try
                {
                    OutputDuplicateFrameInformation frameInfo;
                    SharpDX.DXGI.Resource desktopResource;

                    outputDuplication.AcquireNextFrame(100, out frameInfo, out desktopResource);

                    using (var screenTexture2D = desktopResource.QueryInterface<Texture2D>())
                    {
                        device.ImmediateContext.CopyResource(screenTexture2D, screenTexture);
                    }

                    var dataBox = device.ImmediateContext.MapSubresource(screenTexture, 0, MapMode.Read, SharpDX.Direct3D11.MapFlags.None);
                    bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
                    var boundsRect = new Rectangle(0, 0, width, height);
                    var bitmapData = bitmap.LockBits(boundsRect, ImageLockMode.WriteOnly, bitmap.PixelFormat);

                    int bytesPerPixel = Image.GetPixelFormatSize(bitmap.PixelFormat) / 8;
                    int dataBoxRowPitch = dataBox.RowPitch;
                    int bitmapDataStride = bitmapData.Stride;
                    IntPtr dataBoxPointer = dataBox.DataPointer;
                    IntPtr bitmapDataPointer = bitmapData.Scan0;

                    unsafe
                    {
                        byte* src = (byte*)dataBoxPointer;
                        byte* dest = (byte*)bitmapDataPointer;

                        for (int y = 0; y < height; y++)
                        {
                            System.Buffer.MemoryCopy(src + y * dataBoxRowPitch, dest + y * bitmapDataStride, width * bytesPerPixel, width * bytesPerPixel);
                        }
                    }

                    bitmap.UnlockBits(bitmapData);
                    device.ImmediateContext.UnmapSubresource(screenTexture, 0);
                    outputDuplication.ReleaseFrame();
                }
                catch (SharpDXException ex)
                {
                    if (ex.ResultCode == Result.WaitTimeout)
                    {
                        Console.WriteLine("No new frame available within timeout.");
                    }
                    else
                    {
                        Console.WriteLine("Error capturing screen: " + ex.Message);
                    }
                }
            }

            output1.Dispose();
            output.Dispose();
            adapter.Dispose();
            factory.Dispose();
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error during screen capture: " + ex.Message);
        }

        return bitmap;
    }
}
