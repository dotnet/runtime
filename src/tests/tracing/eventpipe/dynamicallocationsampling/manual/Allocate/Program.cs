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
                Console.WriteLine("Usage: Allocate [1|2|3|4]");
                Console.WriteLine("  1: small and big allocations");
                Console.WriteLine("  2: allocations per thread");
                Console.WriteLine("  3: arrays of double (for x86)");
                Console.WriteLine("  4: different types of objects");
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
                    case "3":
                        MeasureDoubleAllocations();
                        break;
                    case "4":
                        MeasureDifferentTypes();
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

        // used to compute percentiles
        const int Iterations = 10;

        // number of objects to allocate
        const int Count = 1_000_000;

        private static void MeasureDifferentTypes()
        {
            var ma = new MeasureDifferentTypes();
            Stopwatch clock = new Stopwatch();
            clock.Start();

            AllocationsRunEventSource.Log.StartRun(Iterations, Count);
            for (int i = 0; i < Iterations; i++)
            {
                AllocationsRunEventSource.Log.StartIteration(i);
                ma.Allocate(Count);
                AllocationsRunEventSource.Log.StopIteration(i);
            }
            AllocationsRunEventSource.Log.StopRun();

            clock.Stop();
            Console.WriteLine($"Duration = {clock.ElapsedMilliseconds} ms");
        }

        private static void MeasureDoubleAllocations()
        {
            var ma = new MeasureDoubles();
            Stopwatch clock = new Stopwatch();
            clock.Start();
            ma.Allocate(Count);
            clock.Stop();
            Console.WriteLine($"Duration = {clock.ElapsedMilliseconds} ms");
        }

        private static void MeasureAllocationsPerThread()
        {
            var ma = new MeasureThreadAllocations();
            Stopwatch clock = new Stopwatch();
            clock.Start();
            ma.Allocate(Count);
            clock.Stop();
            Console.WriteLine($"Duration = {clock.ElapsedMilliseconds} ms");
        }

        static void MeasureSmallAndBigAllocations()
        {
            var ma = new MeasureAllocations();

            Stopwatch clock = new Stopwatch();
            clock.Start();
            ma.Allocate(Count);
            clock.Stop();
            Console.WriteLine($"Duration = {clock.ElapsedMilliseconds} ms");
        }
    }
}
