﻿using System;
using WebSocketSharp;
using WebSocketSharp.Server;
using System.Drawing;
using System.IO;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Diagnostics;
using System.Threading;

namespace Backend
{
    public class RemoteDesktop : WebSocketBehavior
    {
        // Este método se ejecuta cuando el servidor recibe un mensaje del cliente
        protected override void OnMessage(MessageEventArgs e)
        {
            // Si el mensaje recibido es "capture", se captura y envía una imagen de la pantalla
            if (e.Data == "capture")
            {
                Bitmap capture = CaptureScreen();
                using (MemoryStream ms = new MemoryStream())
                {
                    capture.Save(ms, ImageFormat.Jpeg);
                    Send(ms.ToArray());
                }
            }
            else if (e.Data == "status")
            {
                string status = GetPerformanceStatus();
                Send(status);
                Console.WriteLine(status);
            }
            else
            {
                var eventData = Newtonsoft.Json.JsonConvert.DeserializeObject<EventData>(e.Data);
                HandleInputEvent(eventData);
            }
        }

        // Método para capturar la pantalla
        private Bitmap CaptureScreen()
        {
            // Obtener las dimensiones de la pantalla principal
            Rectangle bounds = System.Windows.Forms.Screen.PrimaryScreen.Bounds;
            Bitmap bitmap = new Bitmap(bounds.Width, bounds.Height);

            // Crear un objeto Graphics para copiar la pantalla al bitmap
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                g.CopyFromScreen(Point.Empty, Point.Empty, bounds.Size);
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

        public class EventData
        {
            public string Type { get; set; }
            public int X { get; set; }
            public int Y { get; set; }
            public string Key { get; set; }
        }

        // Método para manejar eventos de entrada
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

        // Método para mover el mouse
        private void MouseMove(int x, int y)
        {
            Cursor.Position = new Point(x, y);
        }

        // Método para simular un clic del mouse
        private void MouseClick(int x, int y)
        {
            Cursor.Position = new Point(x, y);
            mouse_event(MOUSEEVENTF_LEFTDOWN | MOUSEEVENTF_LEFTUP, x, y, 0, 0);
        }

        // Método para simular una pulsación de tecla
        private void KeyDown(string key)
        {
            SendKeys.SendWait(key);
        }

        // Declaración de constantes para simular eventos de mouse
        private const int MOUSEEVENTF_LEFTDOWN = 0x02;
        private const int MOUSEEVENTF_LEFTUP = 0x04;

        // Método externo para simular eventos de mouse
        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern void mouse_event(int dwFlags, int dx, int dy, int cButtons, int dwExtraInfo);

    }



    public class Program
    {
        
    static void Main(string[] args)
        {
        WebSocketServer wssv = new WebSocketServer("ws://localhost:8080");

        // Añadir el servicio WebSocket que manejará las conexiones en la ruta /RemoteDesktop
        wssv.AddWebSocketService<RemoteDesktop>("/RemoteDesktop");

        // Iniciar el servidor WebSocket
        wssv.Start();
        Console.WriteLine("Servidor WebSocket iniciado en ws://localhost:8080");

            Thread monitorThread = new Thread(MonitorPerformance);
            monitorThread.Start();

            // Esperar a que se presione una tecla para detener el servidor
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
