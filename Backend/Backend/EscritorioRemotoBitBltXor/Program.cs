using System;
using WebSocketSharp;
using WebSocketSharp.Server;
using System.Drawing;
using System.IO;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Diagnostics;
using System.Threading;

namespace EscritorioRemotoBitBltXor
{
    public class RemoteDesktop : WebSocketBehavior
    {
        private Bitmap previousFrame;
        private bool isFirstFrame = true;

        // Este método se ejecuta cuando el servidor recibe un mensaje del cliente
        protected override void OnMessage(MessageEventArgs e)
        {
            if (e.Data == "capture")
            {
                Bitmap capture = CaptureScreen();
                if (isFirstFrame)
                {
                    // Enviar el primer fotograma completo
                    using (MemoryStream ms = new MemoryStream())
                    {
                        capture.Save(ms, ImageFormat.Jpeg);
                        Send(ms.ToArray());
                    }
                    isFirstFrame = false;
                }
                else
                {
                    Bitmap diff = GetDifferenceBitmap(previousFrame, capture);
                    using (MemoryStream ms = new MemoryStream())
                    {
                        diff.Save(ms, ImageFormat.Jpeg);
                        Send(ms.ToArray());
                    }
                }
                previousFrame?.Dispose();
                previousFrame = capture;
            }
            else if (e.Data == "status")
            {
                string status = GetPerformanceStatus();
                Send(status);
                Console.WriteLine(status);
            }
            else
            {
                var eventData = Newtonsoft.Json.JsonConvert.DeserializeObject<EventData>(e.Data);
                HandleInputEvent(eventData);
            }
        }

        private Bitmap CaptureScreen()
        {
            Rectangle bounds = System.Windows.Forms.Screen.PrimaryScreen.Bounds;
            Bitmap bitmap = new Bitmap(bounds.Width, bounds.Height);
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                IntPtr hdcDest = g.GetHdc();
                IntPtr hdcSrc = GetDC(IntPtr.Zero);
                BitBlt(hdcDest, 0, 0, bounds.Width, bounds.Height, hdcSrc, 0, 0, SRCCOPY);
                ReleaseDC(IntPtr.Zero, hdcSrc);
                g.ReleaseHdc(hdcDest);
            }
            return bitmap;
        }

        private Bitmap GetDifferenceBitmap(Bitmap bmp1, Bitmap bmp2)
        {
            int width = bmp1.Width;
            int height = bmp1.Height;
            Bitmap diffBitmap = new Bitmap(width, height);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Color c1 = bmp1.GetPixel(x, y);
                    Color c2 = bmp2.GetPixel(x, y);
                    int r = c1.R ^ c2.R;
                    int g = c1.G ^ c2.G;
                    int b = c1.B ^ c2.B;
                    diffBitmap.SetPixel(x, y, Color.FromArgb(r, g, b));
                }
            }
            return diffBitmap;
        }

        [DllImport("gdi32.dll")]
        private static extern bool BitBlt(IntPtr hdcDest, int nXDest, int nYDest, int nWidth, int nHeight, IntPtr hdcSrc, int nXSrc, int nYSrc, int dwRop);

        [DllImport("user32.dll")]
        private static extern IntPtr GetDC(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

        private const int SRCCOPY = 0x00CC0020;

        private string GetPerformanceStatus()
        {
            var cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            var ramCounter = new PerformanceCounter("Memory", "Available MBytes");

            cpuCounter.NextValue();
            Thread.Sleep(1000);
            float cpuUsage = cpuCounter.NextValue();
            float availableMemory = ramCounter.NextValue();

            return $"CPU Usage: {cpuUsage}% | Available Memory: {availableMemory}MB";
        }

        public class EventData
        {
            public string Type { get; set; }
            public int X { get; set; }
            public int Y { get; set; }
            public string Key { get; set; }
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
    }
    public class Program
    {
        static void Main(string[] args)
        {
            WebSocketServer wssv = new WebSocketServer("ws://localhost:8080");

            wssv.AddWebSocketService<RemoteDesktop>("/RemoteDesktop");

            wssv.Start();
            Console.WriteLine("Servidor WebSocket iniciado en ws://localhost:8080");

            Thread monitorThread = new Thread(MonitorPerformance);
            monitorThread.Start();

            Console.ReadKey();
            wssv.Stop();
        }

        private static void MonitorPerformance()
        {
            var cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            var ramCounter = new PerformanceCounter("Memory", "Available MBytes");

            while (true)
            {
                cpuCounter.NextValue();
                Thread.Sleep(1000);
                float cpuUsage = cpuCounter.NextValue();
                float availableMemory = ramCounter.NextValue();

                Console.WriteLine($"CPU Usage: {cpuUsage}% | Available Memory: {availableMemory}MB");

                Thread.Sleep(5000);
            }
        }
    }
}
