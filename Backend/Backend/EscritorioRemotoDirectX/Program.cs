using System;
using WebSocketSharp.Server;

using EscritorioRemotoDirectX.Services;

namespace EscritorioRemotoDirectX
{
    public class Program
    {
        static void Main(string[] args)
        {
            WebSocketServer wssv = new WebSocketServer("ws://localhost:8080");

            wssv.AddWebSocketService<RemoteDesktop>("/RemoteDesktop");

            wssv.Start();
            Console.WriteLine("Servidor WebSocket iniciado en ws://localhost:8080");

            Console.ReadKey();
            wssv.Stop();
        }
    }
}