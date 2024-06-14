using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using WebSocketSharp;
using WebSocketSharp.Server;
using System.Threading;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX.Direct3D;
using SharpDX;
using Device = SharpDX.Direct3D11.Device;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Newtonsoft.Json;

namespace EscritorioRemotoDirectX
{
    public class EventData
    {
        public string Type { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public string Key { get; set; }
    }

    public class RemoteDesktop : WebSocketBehavior
    {
        private bool _isRunning = false;
        private int _captureInterval = 100; // Intervalo de captura en milisegundos
        private Device _device;
        private OutputDuplication _outputDuplication;
        private Bitmap _previousCapture = null; // Bitmap para la captura anterior

        public RemoteDesktop()
        {
            InitializeDirectX();
        }


        protected override void OnMessage(MessageEventArgs e)
        {
            try
            {
                var eventData = JsonConvert.DeserializeObject<EventData>(e.Data);
                HandleInputEvent(eventData);
            }
            catch (JsonReaderException)
            {
                // No es un JSON válido, manejar como comando simple
                if (e.Data == "capture")
                {
                    StartCapture();
                }
                else if (e.Data == "stop")
                {
                    StopCapture();
                }                
            }
        }

        private void InitializeDirectX()
        {
            var factory = new Factory1();
            var adapter = factory.GetAdapter1(0);
            var output = adapter.GetOutput(0);
            var output1 = output.QueryInterface<Output1>();

            _device = new Device(DriverType.Hardware, DeviceCreationFlags.BgraSupport);
            _outputDuplication = output1.DuplicateOutput(_device);

            output1.Dispose();
            output.Dispose();
            adapter.Dispose();
            factory.Dispose();
        }

        private void StartCapture()
        {
            if (_isRunning) return;
            _isRunning = true;

            ThreadPool.QueueUserWorkItem(state =>
            {
                while (_isRunning)
                {
                    Bitmap capture = CaptureScreen();
                    if (capture != null)
                    {
                        using (MemoryStream ms = new MemoryStream())
                        {
                            capture.Save(ms, ImageFormat.Jpeg);
                            Send(ms.ToArray());
                        }
                    }
                    else
                    {
                        Console.WriteLine("Error: capture is null.");
                    }
                    Thread.Sleep(_captureInterval);
                }
            });
        }

        private void StopCapture()
        {
            _isRunning = false;
        }

        private Bitmap CaptureScreen()
        {
            Bitmap bitmap = null;
            try
            {
                if (_device == null || _outputDuplication == null)
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

                using (var screenTexture = new Texture2D(_device, new Texture2DDescription
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
                        _outputDuplication.AcquireNextFrame(100, out var frameInfo, out var desktopResource);
                        using (var screenTexture2D = desktopResource.QueryInterface<Texture2D>())
                        {
                            _device.ImmediateContext.CopyResource(screenTexture2D, screenTexture);
                        }

                        var dataBox = _device.ImmediateContext.MapSubresource(screenTexture, 0, MapMode.Read, SharpDX.Direct3D11.MapFlags.None);
                        bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
                        var boundsRect = new Rectangle(0, 0, width, height);
                        var bitmapData = bitmap.LockBits(boundsRect, ImageLockMode.WriteOnly, bitmap.PixelFormat);

                        int bytesPerPixel = Image.GetPixelFormatSize(bitmap.PixelFormat) / 8;
                        int dataBoxRowPitch = dataBox.RowPitch;
                        int bitmapDataStride = bitmapData.Stride;
                        IntPtr dataBoxPointer = dataBox.DataPointer;
                        IntPtr bitmapDataPointer = bitmapData.Scan0;

                        for (int y = 0; y < height; y++)
                        {
                            IntPtr sourceRow = IntPtr.Add(dataBoxPointer, y * dataBoxRowPitch);
                            IntPtr destinationRow = IntPtr.Add(bitmapDataPointer, y * bitmapDataStride);
                            byte[] rowBytes = new byte[width * bytesPerPixel];
                            Marshal.Copy(sourceRow, rowBytes, 0, rowBytes.Length);
                            Marshal.Copy(rowBytes, 0, destinationRow, rowBytes.Length);
                        }

                        bitmap.UnlockBits(bitmapData);
                        _device.ImmediateContext.UnmapSubresource(screenTexture, 0);
                        _outputDuplication.ReleaseFrame();

                        if (_previousCapture != null)
                        {
                            Bitmap diffBitmap = GetDifferenceBitmap(_previousCapture, bitmap);
                            _previousCapture.Dispose();
                            _previousCapture = bitmap;
                            bitmap = diffBitmap;
                        }                       
                    }
                    catch (SharpDXException ex)
                    {
                        Console.WriteLine("Error capturing screen: " + ex.Message);
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

        private Bitmap GetDifferenceBitmap(Bitmap bmp1, Bitmap bmp2)
        {
            int width = bmp1.Width;
            int height = bmp1.Height;
            Bitmap diffBmp = new Bitmap(width, height, PixelFormat.Format32bppArgb);

            BitmapData data1 = bmp1.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadOnly, bmp1.PixelFormat);
            BitmapData data2 = bmp2.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadOnly, bmp2.PixelFormat);
            BitmapData diffData = diffBmp.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, diffBmp.PixelFormat);

            int bytesPerPixel = Image.GetPixelFormatSize(bmp1.PixelFormat) / 8;
            int stride1 = data1.Stride;
            int stride2 = data2.Stride;
            int strideDiff = diffData.Stride;

            IntPtr ptr1 = data1.Scan0;
            IntPtr ptr2 = data2.Scan0;
            IntPtr ptrDiff = diffData.Scan0;

            for (int y = 0; y < height; y++)
            {
                byte[] row1 = new byte[width * bytesPerPixel];
                byte[] row2 = new byte[width * bytesPerPixel];
                byte[] diffRow = new byte[width * bytesPerPixel];

                Marshal.Copy(IntPtr.Add(ptr1, y * stride1), row1, 0, row1.Length);
                Marshal.Copy(IntPtr.Add(ptr2, y * stride2), row2, 0, row2.Length);

                for (int x = 0; x < row1.Length; x++)
                {
                    diffRow[x] = (byte)(row1[x] ^ row2[x]);
                }

                Marshal.Copy(diffRow, 0, IntPtr.Add(ptrDiff, y * strideDiff), diffRow.Length);
            }

            bmp1.UnlockBits(data1);
            bmp2.UnlockBits(data2);
            diffBmp.UnlockBits(diffData);

            return diffBmp;
        }

        private void HandleInputEvent(EventData eventData)
        {
            switch (eventData.Type)
            {
                case "mousemove":
                    MouseMove(eventData.X, eventData.Y);
                    break;
                case "click":
                    MouseClick(eventData.X, eventData.Y);
                    break;
                case "keydown":
                    KeyDown(eventData.Key);
                    break;
            }
        }

        private void MouseMove(int x, int y)
        {
            Cursor.Position = new Point(x, y);
        }

        private void MouseClick(int x, int y)
        {
            Cursor.Position = new Point(x, y);
            mouse_event(MOUSEEVENTF_LEFTDOWN | MOUSEEVENTF_LEFTUP, x, y, 0, 0);
        }

        private void KeyDown(string key)
        {
            SendKeys.SendWait(key);
        }

        private const int MOUSEEVENTF_LEFTDOWN = 0x02;
        private const int MOUSEEVENTF_LEFTUP = 0x04;

        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern void mouse_event(int dwFlags, int dx, int dy, int cButtons, int dwExtraInfo);

        protected override void OnClose(CloseEventArgs e)
        {
            _outputDuplication.Dispose();
            _device.Dispose();
            base.OnClose(e);
        }
    }
    public class Program
    {
        static void Main(string[] args)
        {
            WebSocketServer wssv = new WebSocketServer("ws://localhost:8080");

            wssv.AddWebSocketService<RemoteDesktop>("/RemoteDesktop");

            wssv.Start();
            Console.WriteLine("Servidor WebSocket iniciado en ws://localhost:8080");           

            Console.ReadKey();
            wssv.Stop();
        }        
    }
}