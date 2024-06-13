using System.IO;
using System.Threading;
using WebSocketSharp;
using WebSocketSharp.Server;
using Newtonsoft.Json;
using SharpDX.Direct3D11;
using EscritorioRemotoDirectX.Utils;
using EscritorioRemotoDirectX.Models;
using System.Drawing.Imaging;
using System.Drawing;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using System;

namespace EscritorioRemotoDirectX.Services
{
    public class RemoteDesktopService : WebSocketBehavior
    {
        private bool _isRunning = false;
        private int _captureInterval = 100;
        private Device _device;
        private SharpDX.DXGI.OutputDuplication _outputDuplication;
        private Mat _previousCapture = null;
        private bool _isFirstCapture = true; // Flag to indicate if it's the first capture

        public RemoteDesktopService()
        {
            DirectXHelper.InitializeDirectX(out _device, out _outputDuplication);
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
                try
                {
                    var eventData = JsonConvert.DeserializeObject<EventData>(e.Data);
                    InputHandlingService.HandleInputEvent(eventData);
                }
                catch (JsonReaderException)
                {
                    Console.WriteLine("");
                }
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
                    var capture = ScreenCaptureService.CaptureScreen(_device, _outputDuplication);
                    if (capture != null)
                    {
                        Mat currentCapture = BitmapConverter.ToMat(capture);
                        Rect boundingBox = new Rect();

                        if (_isFirstCapture || _previousCapture == null || HasDifference(_previousCapture, currentCapture, out boundingBox))
                        {
                            Bitmap regionBitmap;
                            if (_isFirstCapture)
                            {
                                // Send the whole image on the first capture
                                regionBitmap = capture;
                                _isFirstCapture = false;
                            }
                            else
                            {
                                // Send only the changed region
                                var changedRegion = new Mat(currentCapture, boundingBox);
                                regionBitmap = BitmapConverter.ToBitmap(changedRegion);
                            }

                            byte[] regionData = ImageToByteArray(regionBitmap);
                            if (regionData != null)
                            {
                                Send(regionData);
                            }
                            regionBitmap.Dispose();
                        }

                        _previousCapture?.Dispose();
                        _previousCapture = currentCapture;
                    }
                    Thread.Sleep(_captureInterval);
                }
            });
        }

        private bool HasDifference(Mat previousImage, Mat currentImage, out Rect boundingBox)
        {
            boundingBox = new Rect();

            if (previousImage.Size() != currentImage.Size())
            {
                boundingBox = new Rect(0, 0, currentImage.Width, currentImage.Height);
                return true;
            }

            Mat diff = new Mat();
            Cv2.BitwiseXor(previousImage, currentImage, diff);
            Cv2.CvtColor(diff, diff, ColorConversionCodes.BGR2GRAY);

            Mat nonZero = new Mat();
            Cv2.FindNonZero(diff, nonZero);

            if (nonZero.Rows > 0)
            {
                boundingBox = Cv2.BoundingRect(nonZero);
                return true;
            }

            return false;
        }

        private byte[] ImageToByteArray(Bitmap image)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                image.Save(ms, ImageFormat.Png);
                return ms.ToArray();
            }
        }

        private void StopCapture()
        {
            _isRunning = false;
        }

        protected override void OnClose(CloseEventArgs e)
        {
            _outputDuplication.Dispose();
            _device.Dispose();
            _previousCapture?.Dispose();
            base.OnClose(e);
        }
    }
}