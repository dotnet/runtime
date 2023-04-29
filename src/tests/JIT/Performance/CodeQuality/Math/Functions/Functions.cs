// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Xunit;

namespace Functions
{
    public static class Program
    {
#if DEBUG
        private const int defaultIterations = 1;
#else
        private const int defaultIterations = 1000;
#endif

        private static readonly IDictionary<string, Action> TestList = new Dictionary<string, Action>() {
            ["absdouble"] = MathTests.AbsDoubleTest,
            ["abssingle"] = MathTests.AbsSingleTest,
            ["acosdouble"] = MathTests.AcosDoubleTest,
            ["acossingle"] = MathTests.AcosSingleTest,
            ["asindouble"] = MathTests.AsinDoubleTest,
            ["asinsingle"] = MathTests.AsinSingleTest,
            ["atandouble"] = MathTests.AtanDoubleTest,
            ["atansingle"] = MathTests.AtanSingleTest,
            ["atan2double"] = MathTests.Atan2DoubleTest,
            ["atan2single"] = MathTests.Atan2SingleTest,
            ["ceilingdouble"] = MathTests.CeilingDoubleTest,
            ["ceilingsingle"] = MathTests.CeilingSingleTest,
            ["cosdouble"] = MathTests.CosDoubleTest,
            ["cossingle"] = MathTests.CosSingleTest,
            ["coshdouble"] = MathTests.CoshDoubleTest,
            ["coshsingle"] = MathTests.CoshSingleTest,
            ["expdouble"] = MathTests.ExpDoubleTest,
            ["expsingle"] = MathTests.ExpSingleTest,
            ["floordouble"] = MathTests.FloorDoubleTest,
            ["floorsingle"] = MathTests.FloorSingleTest,
            ["logdouble"] = MathTests.LogDoubleTest,
            ["logsingle"] = MathTests.LogSingleTest,
            ["log10double"] = MathTests.Log10DoubleTest,
            ["log10single"] = MathTests.Log10SingleTest,
            ["powdouble"] = MathTests.PowDoubleTest,
            ["powsingle"] = MathTests.PowSingleTest,
            ["rounddouble"] = MathTests.RoundDoubleTest,
            ["roundsingle"] = MathTests.RoundSingleTest,
            ["sindouble"] = MathTests.SinDoubleTest,
            ["sinsingle"] = MathTests.SinSingleTest,
            ["sinhdouble"] = MathTests.SinhDoubleTest,
            ["sinhsingle"] = MathTests.SinhSingleTest,
            ["sqrtdouble"] = MathTests.SqrtDoubleTest,
            ["sqrtsingle"] = MathTests.SqrtSingleTest,
            ["tandouble"] = MathTests.TanDoubleTest,
            ["tansingle"] = MathTests.TanSingleTest,
            ["tanhdouble"] = MathTests.TanhDoubleTest,
            ["tanhsingle"] = MathTests.TanhSingleTest
        };

        [Fact]
        public static int TestEntryPoint()
        {
            return Test(Array.Empty<string>());
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static int Test(string[] args)
        {
            var isPassing = true; var iterations = defaultIterations;
            ICollection<string> testsToRun = new HashSet<string>();

            try
            {
                for (int index = 0; index < args.Length; index++)
                {
                    if (args[index].ToLowerInvariant() == "-bench")
                    {
                        index++;

                        if ((index >= args.Length) || !int.TryParse(args[index], out iterations))
                        {
                            iterations = defaultIterations;
                        }
                    }
                    else if (args[index].ToLowerInvariant() == "all")
                    {
                        testsToRun = TestList.Keys;
                        break;
                    }
                    else
                    {
                        var testName = args[index].ToLowerInvariant();

                        if (!TestList.ContainsKey(testName))
                        {
                            PrintUsage();
                            break;
                        }

                        testsToRun.Add(testName);
                    }
                }

                if (testsToRun.Count == 0)
                {
                    testsToRun = TestList.Keys;
                }

                foreach (var testToRun in testsToRun)
                {
                    Console.WriteLine($"Running {testToRun} test...");
                    Test(iterations, TestList[testToRun]);
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine($"    Error: {exception.Message}");
                isPassing = false;
            }

            return isPassing ? 100 : -1;
        }

        private static void PrintUsage()
        {
            Console.WriteLine(@"Usage:
Functions [name] [-bench #]

  [name]: The name of the function to test. Defaults to 'all'.
    all");

            foreach (var testName in TestList.Keys)
            {
                Console.WriteLine($"  {testName}");
            }

            Console.WriteLine($@"
  [-bench #]: The number of iterations. Defaults to {defaultIterations}");
        }

        private static void Test(int iterations, Action action)
        {
            // ****************************************************************

            Console.WriteLine("  Warming up...");

            var startTimestamp = Stopwatch.GetTimestamp();

            action();

            var totalElapsedTime = (Stopwatch.GetTimestamp() - startTimestamp);
            var totalElapsedTimeInSeconds = (totalElapsedTime / (double)(Stopwatch.Frequency));

            Console.WriteLine($"    Total Time: {totalElapsedTimeInSeconds}");

            // ****************************************************************

            Console.WriteLine($"  Executing {iterations} iterations...");

            totalElapsedTime = 0L;

            for (var iteration = 0; iteration < iterations; iteration++)
            {
                startTimestamp = Stopwatch.GetTimestamp();

                action();

                totalElapsedTime += (Stopwatch.GetTimestamp() - startTimestamp);
            }

            totalElapsedTimeInSeconds = (totalElapsedTime / (double)(Stopwatch.Frequency));

            Console.WriteLine($"    Total Time: {totalElapsedTimeInSeconds} seconds");
            Console.WriteLine($"    Average Time: {totalElapsedTimeInSeconds / iterations} seconds");

            // ****************************************************************
        }
    }
}
