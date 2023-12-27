using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace _20230906_Study_1_Map_Engine_Binding1
{
    class FullScreen
    {
        const int SWP_NOZORDER = 0x4;
        const int SWP_NOACTIVATE = 0x10;

        [DllImport("kernel32")]
        static extern IntPtr GetConsoleWindow();

        [DllImport("user32")]
        static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter,
            int x, int y, int cx, int cy, int flags);

        public static void set_window()
        {
            Console.WindowWidth = 50;
            Console.WindowHeight = 50;
            Console.BufferWidth = 50;
            Console.BufferHeight = 56;
            Console.BackgroundColor = ConsoleColor.Black;

            //Console.ForegroundColor = ConsoleColor.DarkMagenta;
            int width_ = 1050;
            int height_ = 460;
            SetWindowPosition((1920- width_) /2, (1080- height_) /2, width_, height_);
            //Console.SetWindowSize(200, 50);
            Console.Title = "Robot Run";
        }
        /// <summary>
        /// Sets the console window location and size in pixels
        /// </summary>
        public static void SetWindowPosition(int x, int y, int width, int height)
        {
            SetWindowPos(Handle, IntPtr.Zero, x, y, width, height, SWP_NOZORDER | SWP_NOACTIVATE);
        }

        public static IntPtr Handle
        {
            get
            {
                return GetConsoleWindow();
            }
        }
    }
}
