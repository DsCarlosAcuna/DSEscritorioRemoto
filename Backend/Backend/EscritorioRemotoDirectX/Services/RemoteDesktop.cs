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
        private DatabaseService _databaseService;

        public RemoteDesktop()
        {
            DirectXService.InitializeDirectX();
            _databaseService = new DatabaseService("D:\\Documents\\MyMainJob\\Digital Solutions 324 SL\\Databases\\dsescritorioremoto.db");
        }

        protected override void OnMessage(MessageEventArgs e)
        {
            var requestData = JsonConvert.DeserializeObject<RequestData>(e.Data);

            if (requestData.Command == "authenticate")
            {
                try
                {
                    var (ip, port) = _databaseService.GetConnectionDetails(requestData.PcName, requestData.Username, requestData.Password);
                    StartCapture(ip, port);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Authentication failed: " + ex.Message);
                    Send("Authentication failed: " + ex.Message);
                }
            }
            else if (_captureService != null && requestData.Command == "capture")
            {
                _captureService.StartCapture();
            }
            else if (_captureService != null && requestData.Command == "stop")
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
            Console.WriteLine("Conexión WebSocket abierta");
        }

        protected override void OnClose(CloseEventArgs e)
        {
            Console.WriteLine("Conexión WebSocket cerrada");
            _captureService?.StopCapture();
        }

        protected override void OnError(ErrorEventArgs e)
        {
            Console.WriteLine("Error de WebSocket: " + e.Message);
            _captureService?.StopCapture();
        }

        private void StartCapture(string ip, int port)
        {
            Console.WriteLine($"Autenticado correctamente, iniciando la captura para escritorio remoto en {ip}:{port}");
            _captureService = new CaptureService(this.Context.WebSocket);
            _captureService.StartCapture();
        }
    }
}
