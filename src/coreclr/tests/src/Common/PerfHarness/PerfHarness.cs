using System;
using Microsoft.Xunit.Performance.Api;

public class PerfHarness
{
    public static int Main(string[] args)
    {
        try
        {
            using (XunitPerformanceHarness harness = new XunitPerformanceHarness(args))
                harness.RunBenchmarks(assemblyFileName: args[0]);
            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine("[ERROR] Benchmark execution failed.");
            Console.WriteLine($"  {ex.ToString()}");
            return 1;
        }
    }
}
