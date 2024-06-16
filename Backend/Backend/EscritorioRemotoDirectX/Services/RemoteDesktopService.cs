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

namespace EscritorioRemotoDirectX.Services
{
    public class RemoteDesktop : WebSocketBehavior
    {
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
                    Thread.Sleep(_captureInterval);
                }
            });
        }

        private void StopCapture()
        {
            _isRunning = false;
        }

        private Bitmap ApplyXor(Bitmap previousCapture, Bitmap currentCapture)
        {
            int width = previousCapture.Width;
            int height = previousCapture.Height;
            Bitmap xorBitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Color pixelCurrent = currentCapture.GetPixel(x, y);
                    Color pixelPrevious = previousCapture.GetPixel(x, y);

                    int r = pixelCurrent.R ^ pixelPrevious.R;
                    int g = pixelCurrent.G ^ pixelPrevious.G;
                    int b = pixelCurrent.B ^ pixelPrevious.B;

                    Color xorColor = Color.FromArgb(r, g, b);
                    xorBitmap.SetPixel(x, y, xorColor);
                }
            }
            return xorBitmap;
        }
    }
}
