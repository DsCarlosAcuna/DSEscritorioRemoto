using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;

namespace EscritorioRemotoDirectX.Services
{
    public static class ScreenCaptureService
    {
        public static Bitmap CaptureScreen(SharpDX.Direct3D11.Device device, OutputDuplication outputDuplication, string remoteIp = null, int remotePort = 0)
        {
            if (!string.IsNullOrEmpty(remoteIp))
            {
                return CaptureRemoteScreen(remoteIp, remotePort);
            }
            else
            {
                return CaptureLocalScreen(device, outputDuplication);
            }
        }

        public static Bitmap CaptureLocalScreen(SharpDX.Direct3D11.Device device, OutputDuplication outputDuplication)
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

                        for (int y = 0; y < height; y++)
                        {
                            IntPtr sourceRow = IntPtr.Add(dataBoxPointer, y * dataBoxRowPitch);
                            IntPtr destinationRow = IntPtr.Add(bitmapDataPointer, y * bitmapDataStride);
                            byte[] rowBytes = new byte[width * bytesPerPixel];
                            Marshal.Copy(sourceRow, rowBytes, 0, rowBytes.Length);
                            Marshal.Copy(rowBytes, 0, destinationRow, rowBytes.Length);
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

        private static Bitmap CaptureRemoteScreen(string remoteIp, int remotePort)
        {
            Bitmap bitmap = null;
            try
            {
                using (TcpClient client = new TcpClient(remoteIp, remotePort))
                using (NetworkStream stream = client.GetStream())
                {
                    stream.WriteByte(1); // Send capture request

                    byte[] lengthBuffer = new byte[4];
                    stream.Read(lengthBuffer, 0, 4);
                    int length = BitConverter.ToInt32(lengthBuffer, 0);

                    byte[] imageBuffer = new byte[length];
                    int bytesRead = 0;
                    while (bytesRead < length)
                    {
                        bytesRead += stream.Read(imageBuffer, bytesRead, length - bytesRead);
                    }

                    using (MemoryStream ms = new MemoryStream(imageBuffer))
                    {
                        return new Bitmap(ms);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error capturing remote screen: " + ex.Message);
            }
            return bitmap;
        }
    }
}