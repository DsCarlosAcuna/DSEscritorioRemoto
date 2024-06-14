using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;

namespace EscritorioRemotoDirectX.Services
{
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
                        outputDuplication.AcquireNextFrame(1000, out var frameInfo, out var desktopResource);
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
    }
}
