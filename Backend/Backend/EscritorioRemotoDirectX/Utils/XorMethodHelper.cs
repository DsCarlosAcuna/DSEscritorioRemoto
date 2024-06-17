using System.Drawing.Imaging;
using System.Drawing;

namespace EscritorioRemotoDirectX.Utils
{
    public static class XorMethodHelper
    {
        public static Bitmap ApplyXor(Bitmap previousCapture, Bitmap currentCapture)
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
