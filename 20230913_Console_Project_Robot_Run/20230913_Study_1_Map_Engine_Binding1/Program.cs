using MyGame;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _20230906_Study_1_Map_Engine_Binding1
{
    class Program
    {
        static void Main(string[] args)
        {
            FullScreen.set_window();
            Console.CursorVisible = false;
            Console.OutputEncoding = System.Text.Encoding.Unicode;
            Game game = new Game();
            game.play_map();
        }
    }
}
