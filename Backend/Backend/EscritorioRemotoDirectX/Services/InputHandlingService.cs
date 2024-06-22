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
                    MouseClick(eventData.X, eventData.Y, MouseEventFlag.LeftDown | MouseEventFlag.LeftUp);
                    break;
                case "rightclick":
                    MouseClick(eventData.X, eventData.Y, MouseEventFlag.RightDown | MouseEventFlag.RightUp);
                    break;
                case "middleclick":
                    MouseClick(eventData.X, eventData.Y, MouseEventFlag.MiddleDown | MouseEventFlag.MiddleUp);
                    break;
                case "doubleclick":
                    MouseDoubleClick(eventData.X, eventData.Y);
                    break;
                case "scroll":
                    MouseScroll(eventData.Delta);
                    break;
                case "drag":
                    MouseDrag(eventData.X, eventData.Y, eventData.DX, eventData.DY);
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

        private static void MouseClick(int x, int y, MouseEventFlag mouseEventFlag)
        {
            Cursor.Position = new System.Drawing.Point(x, y);
            mouse_event(mouseEventFlag, x, y, 0, 0);
        }

        private static void MouseDoubleClick(int x, int y)
        {
            Cursor.Position = new System.Drawing.Point(x, y);
            mouse_event(MouseEventFlag.LeftDown | MouseEventFlag.LeftUp, x, y, 0, 0);
            mouse_event(MouseEventFlag.LeftDown | MouseEventFlag.LeftUp, x, y, 0, 0);
        }

        private static void MouseScroll(int delta)
        {
            mouse_event(MouseEventFlag.Wheel, 0, 0, (uint)delta, 0);
        }

        private static void MouseDrag(int x, int y, int dx, int dy)
        {
            Cursor.Position = new System.Drawing.Point(x, y);
            mouse_event(MouseEventFlag.LeftDown, x, y, 0, 0);
            Cursor.Position = new System.Drawing.Point(x + dx, y + dy);
            mouse_event(MouseEventFlag.LeftUp, x + dx, y + dy, 0, 0);
        }

        private static void KeyDown(string key)
        {
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
                case "insert":
                    SendKeys.SendWait("{INSERT}");
                    break;
                case "space":
                    SendKeys.SendWait(" ");
                    break;
                case "shift":
                    SendKeys.SendWait("+");
                    break;
                case "ctrl":
                    SendKeys.SendWait("^");
                    break;
                case "alt":
                    SendKeys.SendWait("%");
                    break;
                case "f1":
                case "f2":
                case "f3":
                case "f4":
                case "f5":
                case "f6":
                case "f7":
                case "f8":
                case "f9":
                case "f10":
                case "f11":
                case "f12":
                    SendKeys.SendWait("{" + key.ToUpper() + "}");
                    break;
                default:
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
            RightUp = 0x0010,
            MiddleDown = 0x0020,
            MiddleUp = 0x0040,
            Wheel = 0x0800
        }
    }
}
