using System.IO;
using System.Reflection;
using System.Collections.Generic;
using Microsoft.Xunit.Performance.Api;

public class PerfHarness
{
    public static void Main(string[] args)
    {
        string assemblyName = args[0];
        using (XunitPerformanceHarness harness = new XunitPerformanceHarness(args))
        {
            harness.RunBenchmarks(assemblyName);
        }
    }
}