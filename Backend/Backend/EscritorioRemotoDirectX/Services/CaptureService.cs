using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading;
using WebSocketSharp;
using Newtonsoft.Json;

using EscritorioRemotoDirectX.Utils;

namespace EscritorioRemotoDirectX.Services
{
    public class CaptureService
    {
        private static readonly object captureLock = new object();
        private bool _isRunning = false;
        private int _captureInterval = 100;
        private Bitmap _previousCapture;
        private WebSocket _webSocket;

        public CaptureService(WebSocket webSocket, int captureInterval = 100)
        {
            _webSocket = webSocket;
            _captureInterval = captureInterval;
        }

        public void StartCapture()
        {
            if (_isRunning) return;
            _isRunning = true;

            ThreadPool.QueueUserWorkItem(state =>
            {
                while (_isRunning)
                {
                    lock (captureLock)
                    {
                        Bitmap capture = ScreenCaptureService.CaptureScreen(DirectXService.Device, DirectXService.OutputDuplication);
                        if (capture != null)
                        {
                            byte[] imageData;
                            using (MemoryStream ms = new MemoryStream())
                            {
                                if (_previousCapture != null)
                                {
                                    Bitmap xorBitmap = XorMethodHelper.ApplyXor(_previousCapture, capture);
                                    xorBitmap.Save(ms, ImageFormat.Png);
                                    xorBitmap.Dispose();
                                }
                                else
                                {
                                    capture.Save(ms, ImageFormat.Png);
                                }
                                imageData = ms.ToArray();
                            }

                            if (_previousCapture != null)
                            {
                                _previousCapture.Dispose();
                            }
                            _previousCapture = (Bitmap)capture.Clone();

                            if (_webSocket.ReadyState == WebSocketState.Open)
                            {
                                var payload = new
                                {
                                    Timestamp = DateTime.UtcNow.Ticks,
                                    ImageData = Convert.ToBase64String(imageData)
                                };
                                string jsonPayload = JsonConvert.SerializeObject(payload);
                                _webSocket.Send(jsonPayload);
                            }
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

        public void StopCapture()
        {
            lock (captureLock)
            {
                _isRunning = false;
            }
        }
    }
}
