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
using System.Diagnostics;

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
                else if (e.Data == "status")
                {
                    string status = GetPerformanceStatus();
                    Send(status);
                    Console.WriteLine(status);
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
            {// Obtener la descripción de la pantalla
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

        private string GetPerformanceStatus()
        {
            var cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            var ramCounter = new PerformanceCounter("Memory", "Available MBytes");

            // Se deben inicializar los contadores antes de obtener el primer valor
            cpuCounter.NextValue();
            Thread.Sleep(1000); // Esperar un segundo para obtener un valor correcto
            float cpuUsage = cpuCounter.NextValue();
            float availableMemory = ramCounter.NextValue();

            return $"CPU Usage: {cpuUsage}% | Available Memory: {availableMemory}MB";
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
                // Se deben inicializar los contadores antes de obtener el primer valor
                cpuCounter.NextValue();
                Thread.Sleep(1000); // Esperar un segundo para obtener un valor correcto
                float cpuUsage = cpuCounter.NextValue();
                float availableMemory = ramCounter.NextValue();

                Console.WriteLine($"CPU Usage: {cpuUsage}% | Available Memory: {availableMemory}MB");

                Thread.Sleep(5000); // Esperar 5 segundos antes de la siguiente medición
            }
        }
    }
}
