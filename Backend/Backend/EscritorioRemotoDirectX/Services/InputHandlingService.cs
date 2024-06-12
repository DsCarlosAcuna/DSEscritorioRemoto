using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using EscritorioRemotoDirectX.Models;

namespace EscritorioRemotoDirectX.Services
{
    public static class InputHandlingService
    {
        public static void HandleInputEvent(EventData eventData)
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

        private static void MouseMove(int x, int y)
        {
            Cursor.Position = new Point(x, y);
        }

        private static void MouseClick(int x, int y)
        {
            Cursor.Position = new Point(x, y);
            mouse_event(MOUSEEVENTF_LEFTDOWN | MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
        }

        private static void KeyDown(string key)
        {
            SendKeys.SendWait(key);
        }

        private const int MOUSEEVENTF_LEFTDOWN = 0x02;
        private const int MOUSEEVENTF_LEFTUP = 0x04;

        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern void mouse_event(int dwFlags, int dx, int dy, int cButtons, int dwExtraInfo);
    }
}
