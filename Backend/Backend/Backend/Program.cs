using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebSocketSharp;
using WebSocketSharp.Server;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Forms;

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
        WebSocketServer wssv = new WebSocketServer("ws://192.168.1.127:8080");

        // Añadir el servicio WebSocket que manejará las conexiones en la ruta /RemoteDesktop
        wssv.AddWebSocketService<RemoteDesktop>("/RemoteDesktop");

        // Iniciar el servidor WebSocket
        wssv.Start();
        Console.WriteLine("Servidor WebSocket iniciado en ws://192.168.1.127:8080");

        // Esperar a que se presione una tecla para detener el servidor
        Console.ReadKey();
        wssv.Stop();
    }
    }
}
