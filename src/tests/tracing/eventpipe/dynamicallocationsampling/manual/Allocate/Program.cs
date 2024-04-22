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

            if (args.Length > 1)
            {
                Console.WriteLine("Usage: Allocate [1|2]");
                Console.WriteLine("  1: small and big allocations");
                Console.WriteLine("  2: allocations per thread");
                return;
            }

            if (args.Length == 1)
            {
                switch(args[0])
                {
                    case "1":
                        MeasureSmallAndBigAllocations();
                        break;
                    case "2":
                        MeasureAllocationsPerThread();
                        break;
                    default:
                        Console.WriteLine($"Invalid argument: '{args[0]}'");
                        break;
                }
                return;
            }

            // default behavior
            MeasureSmallAndBigAllocations();
        }

        private static void MeasureAllocationsPerThread()
        {
            var ma = new MeasureThreadAllocations();
            Stopwatch clock = new Stopwatch();
            clock.Start();
            ma.Allocate();
            clock.Stop();
            Console.WriteLine($"Duration = {clock.ElapsedMilliseconds} ms");
        }

        static void MeasureSmallAndBigAllocations()
        {
            var ma = new MeasureAllocations();

            Stopwatch clock = new Stopwatch();
            clock.Start();
            ma.Allocate();
            clock.Stop();
            Console.WriteLine($"Duration = {clock.ElapsedMilliseconds} ms");
        }
    }
}
