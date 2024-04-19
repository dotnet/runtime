using System;
using System.Diagnostics;

namespace Allocate
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine($"pid = {Process.GetCurrentProcess().Id}");
            Console.ReadLine();

            var ma = new MeasureAllocations();

            Stopwatch clock = new Stopwatch();
            clock.Start();
            ma.Allocate();
            clock.Stop();
            Console.WriteLine($"Duration = {clock.ElapsedMilliseconds} ms");
        }
    }
}
