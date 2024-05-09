using System;
using System.Diagnostics;

namespace Allocate
{
    public enum Scenario
    {
        SmallAndBig                  = 1,
        PerThread                    = 2,
        ArrayOfDouble                = 3,
        FinalizerAndArraysAndStrings = 4,
        RatioSizedArrays             = 5,
    }


    internal class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("Usage: Allocate --scenario (1|2|3|4|5) [--iterations (number of iterations)] [--allocations (allocations count)]");
                Console.WriteLine("                     1: small and big allocations");
                Console.WriteLine("                     2: allocations per thread");
                Console.WriteLine("                     3: arrays of double (for x86)");
                Console.WriteLine("                     4: different types of objects");
                Console.WriteLine("                     5: ratio sized arrays");
                return;
            }
            ParseCommandLine(args, out Scenario scenario, out int allocationsCount, out int iterations);

            IAllocations allocationsRun = null;
            switch(scenario)
            {
                case Scenario.SmallAndBig:
                    allocationsRun = new AllocateSmallAndBig();
                    break;
                case Scenario.PerThread:
                    allocationsRun = new ThreadedAllocations();
                    break;
                case Scenario.ArrayOfDouble:
                    allocationsRun = new AllocateArraysOfDoubles();
                    break;
                case Scenario.FinalizerAndArraysAndStrings:
                    allocationsRun = new AllocateDifferentTypes();
                    break;
                case Scenario.RatioSizedArrays:
                    allocationsRun = new AllocateRatioSizedArrays();
                    break;
                default:
                    Console.WriteLine($"Invalid scenario: '{scenario}'");
                    return;
            }

            Console.WriteLine($"pid = {Process.GetCurrentProcess().Id}");
            Console.ReadLine();

            if (allocationsRun != null)
            {
                Stopwatch clock = new Stopwatch();
                clock.Start();

                AllocationsRunEventSource.Log.StartRun(iterations, allocationsCount);
                for (int i = 0; i < iterations; i++)
                {
                    AllocationsRunEventSource.Log.StartIteration(i);
                    allocationsRun.Allocate(allocationsCount);
                    AllocationsRunEventSource.Log.StopIteration(i);
                }
                AllocationsRunEventSource.Log.StopRun();

                clock.Stop();
                Console.WriteLine($"Duration = {clock.ElapsedMilliseconds} ms");
            }
        }

        private static void ParseCommandLine(string[] args, out Scenario scenario, out int allocationsCount, out int iterations)
        {
            iterations = 100;
            allocationsCount = 1_000_000;
            scenario = Scenario.SmallAndBig;

            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i];

                if ("--scenario".Equals(arg, StringComparison.OrdinalIgnoreCase))
                {
                    int valueOffset = i + 1;
                    if (valueOffset < args.Length && int.TryParse(args[valueOffset], out var number))
                    {
                        scenario = (Scenario)number;
                    }
                }
                else
                if ("--iterations".Equals(arg, StringComparison.OrdinalIgnoreCase))
                {
                    int valueOffset = i + 1;
                    if (valueOffset < args.Length && int.TryParse(args[valueOffset], out var number))
                    {
                        if (number <= 0)
                        {
                            throw new ArgumentOutOfRangeException($"Invalid iterations count '{number}': must be > 0");
                        }

                        iterations = number;
                    }
                }
                else
                if ("--allocations".Equals(arg, StringComparison.OrdinalIgnoreCase))
                {
                    int valueOffset = i + 1;
                    if (valueOffset < args.Length && int.TryParse(args[valueOffset], out var number))
                    {
                        if (number <= 0)
                        {
                            throw new ArgumentOutOfRangeException($"Invalid numbers of allocations '{number}: must be > 0");
                        }

                        allocationsCount = number;
                    }
                }
            }
        }
    }
}
