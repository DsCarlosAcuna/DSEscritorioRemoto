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

namespace EscritorioRemotoDirectX.Services
{
    public class RemoteDesktopService : WebSocketBehavior
    {
        private bool _isRunning = false;
        private int _captureInterval = 100;
        private Device _device;
        private SharpDX.DXGI.OutputDuplication _outputDuplication;

        public RemoteDesktopService()
        {
            DirectXHelper.InitializeDirectX(out _device, out _outputDuplication);
        }

        protected override void OnMessage(MessageEventArgs e)
        {
            try
            {
                var eventData = JsonConvert.DeserializeObject<EventData>(e.Data);
                InputHandlingService.HandleInputEvent(eventData);
            }
            catch (JsonReaderException)
            {
                if (e.Data == "capture")
                {
                    StartCapture();
                }
                else if (e.Data == "stop")
                {
                    StopCapture();
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
                        byte[] compressedData = CompressImage(capture, 10L); // Ajusta la calidad según sea necesario (0L a 100L)
                        if (compressedData != null)
                        {
                            Send(compressedData);
                        }
                    }
                    Thread.Sleep(_captureInterval);
                }
            });
        }

        private byte[] CompressImage(Bitmap image, long quality)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                ImageCodecInfo jpgEncoder = GetEncoder(ImageFormat.Jpeg);
                Encoder qualityEncoder = Encoder.Quality;
                EncoderParameters encoderParams = new EncoderParameters(1);
                EncoderParameter encoderParam = new EncoderParameter(qualityEncoder, quality);
                encoderParams.Param[0] = encoderParam;

                image.Save(ms, jpgEncoder, encoderParams);
                return ms.ToArray();
            }
        }

        private ImageCodecInfo GetEncoder(ImageFormat format)
        {
            ImageCodecInfo[] codecs = ImageCodecInfo.GetImageDecoders();
            foreach (ImageCodecInfo codec in codecs)
            {
                if (codec.FormatID == format.Guid)
                {
                    return codec;
                }
            }
            return null;
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
