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

                        if (_previousCapture == null || HasDifference(_previousCapture, currentCapture))
                        {
                            byte[] imageData = ImageToByteArray(capture);
                            if (imageData != null)
                            {
                                Send(imageData);
                            }
                        }

                        _previousCapture?.Dispose();
                        _previousCapture = currentCapture;
                    }
                    Thread.Sleep(_captureInterval);
                }
            });
        }

        private bool HasDifference(Mat previousImage, Mat currentImage)
        {
            if (previousImage.Size() != currentImage.Size())
            {
                return true;
            }

            Mat diff = new Mat();
            Cv2.BitwiseXor(previousImage, currentImage, diff);
            Cv2.CvtColor(diff, diff, ColorConversionCodes.BGR2GRAY);
            var nonZeroCount = Cv2.CountNonZero(diff);

            return nonZeroCount > 0;
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