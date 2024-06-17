using System;
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
                default:
                    Console.WriteLine("Unrecognized event type: " + eventData.Type);
                    break;
            }
        }

        private static void MouseMove(int x, int y)
        {
            Cursor.Position = new System.Drawing.Point(x, y);
        }

        private static void MouseClick(int x, int y)
        {
            Cursor.Position = new System.Drawing.Point(x, y);
            mouse_event(MouseEventFlag.LeftDown | MouseEventFlag.LeftUp, x, y, 0, 0);
        }

        private static void KeyDown(string key)
        {
            // Handle special keys
            switch (key.ToLower())
            {
                case "backspace":
                    SendKeys.SendWait("{BACKSPACE}");
                    break;
                case "enter":
                    SendKeys.SendWait("{ENTER}");
                    break;
                case "tab":
                    SendKeys.SendWait("{TAB}");
                    break;
                case "escape":
                    SendKeys.SendWait("{ESC}");
                    break;
                case "left":
                    SendKeys.SendWait("{LEFT}");
                    break;
                case "up":
                    SendKeys.SendWait("{UP}");
                    break;
                case "right":
                    SendKeys.SendWait("{RIGHT}");
                    break;
                case "down":
                    SendKeys.SendWait("{DOWN}");
                    break;
                case "delete":
                    SendKeys.SendWait("{DELETE}");
                    break;
                case "home":
                    SendKeys.SendWait("{HOME}");
                    break;
                case "end":
                    SendKeys.SendWait("{END}");
                    break;
                case "pageup":
                    SendKeys.SendWait("{PGUP}");
                    break;
                case "pagedown":
                    SendKeys.SendWait("{PGDN}");
                    break;
                default:
                    // Send the key as it is for alphanumeric and other keys
                    SendKeys.SendWait(key);
                    break;
            }
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern void mouse_event(MouseEventFlag dwFlags, int dx, int dy, uint dwData, int dwExtraInfo);

        [Flags]
        private enum MouseEventFlag : uint
        {
            LeftDown = 0x0002,
            LeftUp = 0x0004,
            RightDown = 0x0008,
            RightUp = 0x0010
        }
    }
}
