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
        private bool _isRunning = false;
        private SharpDX.Direct3D11.Device _device;
        private OutputDuplication _outputDuplication;
        private int _captureInterval = 500;
        private Bitmap _previousCapture;
        private Bitmap _previousRegion;

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

        private void InitializeDirectX()
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

        private void StartCapture()
        {
            if (_isRunning) return;
            _isRunning = true;

            Bitmap previousCapture = null;

            ThreadPool.QueueUserWorkItem(state =>
            {
                while (_isRunning)
                {
                    Bitmap capture = ScreenCaptureService.CaptureScreen(_device, _outputDuplication);
                    if (capture != null)
                    {
                        if (previousCapture != null)
                        {
                            Bitmap xorBitmap = new Bitmap(capture.Width, capture.Height);

                            for (int y = 0; y < capture.Height; y++)
                            {
                                for (int x = 0; x < capture.Width; x++)
                                {
                                    Color pixelCurrent = capture.GetPixel(x, y);
                                    Color pixelPrevious = previousCapture.GetPixel(x, y);

                                    int r = pixelCurrent.R ^ pixelPrevious.R;
                                    int g = pixelCurrent.G ^ pixelPrevious.G;
                                    int b = pixelCurrent.B ^ pixelPrevious.B;

                                    Color xorColor = Color.FromArgb(r, g, b);
                                    xorBitmap.SetPixel(x, y, xorColor);
                                }
                            }

                            using (MemoryStream ms = new MemoryStream())
                            {
                                xorBitmap.Save(ms, ImageFormat.Png);
                                Send(ms.ToArray());
                            }

                            xorBitmap.Dispose();
                        }
                        else
                        {
                            using (MemoryStream ms = new MemoryStream())
                            {
                                capture.Save(ms, ImageFormat.Png);
                                Send(ms.ToArray());
                            }
                        }

                        previousCapture = (Bitmap)capture.Clone();
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

        protected override void OnClose(CloseEventArgs e)
        {
            _outputDuplication.Dispose();
            _device.Dispose();
            base.OnClose(e);
        }
    }
}