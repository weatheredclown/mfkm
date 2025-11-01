using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mfkm
{
    internal class Program
    {
        static int Main()
        {
            Console.ResetColor();
            PlayContainer pc = new();
            pc.PlayGames();
            pc.PrintSummary();
            return 1;
        }
    }
}
