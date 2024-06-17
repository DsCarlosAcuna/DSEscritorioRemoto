using System;
using WebSocketSharp;
using WebSocketSharp.Server;
using Newtonsoft.Json;

using EscritorioRemotoDirectX.Models;

namespace EscritorioRemotoDirectX.Services
{
    public class RemoteDesktop : WebSocketBehavior
    {
        private CaptureService _captureService;

        public RemoteDesktop()
        {
            DirectXService.InitializeDirectX();
        }

        protected override void OnMessage(MessageEventArgs e)
        {
            if (e.Data == "capture")
            {
                _captureService.StartCapture();
            }
            else if (e.Data == "stop")
            {
                _captureService.StopCapture();
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
            _captureService = new CaptureService(this.Context.WebSocket);
            _captureService.StartCapture();
        }

        protected override void OnClose(CloseEventArgs e)
        {
            Console.WriteLine("WebSocket connection closed");
            _captureService.StopCapture();
        }

        protected override void OnError(ErrorEventArgs e)
        {
            Console.WriteLine("WebSocket error: " + e.Message);
            _captureService.StopCapture();
        }
    }
}
