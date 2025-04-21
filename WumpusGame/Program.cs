using System;
using System.Threading.Tasks;

namespace WumpusAdventure
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var game = new Game.WumpusGame();
            await game.Start();
        }
    }
}