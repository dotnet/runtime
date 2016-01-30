// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CompositionThroughput
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            //SerialRunForPeformanceAnalysis(new LightweightWebBenchmark());
            //return;

            Console.WriteLine("Composition Throughput Benchmarks");
            Console.WriteLine("=================================");
            Console.Write("    ");
            WriteResultRow("Benchmark", "Version", "Serial", "Parallel", "Delta", "Start");

            var suites = new[] {
                new Suite("Selftest", 30, new Benchmark[] {
                    new FiftyPerSecondTestBenchmark()
                }),
                new Suite("Shootout with New", 20000000, new Benchmark[] {
                    new LightweightNewBenchmark(),
                    new LightweightNLNewBenchmark(),
                    new OperatorNewBenchmark()
                }),
                new Suite("Huge Graph", 200000, new Benchmark[] {
                    new LightweightHugeGraphABenchmark(),
                    new LightweightLongGraphBBenchmark(),
                    new LightweightHugeGraphCBenchmark(),
                    new LightweightHugeGraph4Benchmark()
                }),
                new Suite("Web", 200000, new Benchmark[] {
                    new NativeCodeWebBenchmark(),
                    new LightweightWebBenchmark()
                })
            };

            foreach (var suite in suites)
            {
                RunSuite(suite);
            }
        }

        private static void Debug(Benchmark benchmark)
        {
            var op = benchmark.GetOperation();
            op();
        }

        private static void SerialRunForPeformanceAnalysis(Benchmark benchmark)
        {
            var op = benchmark.GetOperation();
            bool keepGoing = true;
            var worker = new Thread(() =>
            {
                while (keepGoing)
                {
                    op();
                }
            });

            worker.Start();
            Thread.Sleep(60000); // Run for 1 minute
            keepGoing = false;
            worker.Join();
        }

        private static void RunSuite(Suite suite)
        {
            Console.WriteLine("--- {0} - {1} operations ---", suite.Name, suite.StandardRunOperations);

            var benchmarks = suite.IncludedBenchmarks.Concat(new[] { new ControlBenchmark() }).OrderBy(f => Guid.NewGuid()).ToArray();

            var results = from f in benchmarks
                          let op = f.GetOperation()
                          select new
                          {
                              Framework = f,
                              TestPass = f.SelfTest(),
                              Startup = MeasureStartupMilliseconds(f),
                              Serial = ThroughputHarness.MeasureOperationsPerSecond(op, suite.StandardRunOperations, false),
                              Parallel = ThroughputHarness.MeasureOperationsPerSecond(op, suite.StandardRunOperations * Environment.ProcessorCount, true)
                          };

            foreach (var result in results)
            {
                WritePassFail(result.TestPass, result.Parallel != 0 && result.Serial != 0);
                WriteResultRow(result.Framework.Description,
                    result.Framework.Version.ToString(),
                    result.Serial,
                    result.Parallel,
                    ((decimal)result.Parallel / result.Serial).ToString("#.00"),
                    result.Startup);
            }
        }

        private static long MeasureStartupMilliseconds(Benchmark benchmark)
        {
            var sw = new Stopwatch();
            sw.Start();
            var op = benchmark.GetOperation();
            op();
            sw.Stop();
            return sw.ElapsedMilliseconds;
        }

        private static void WritePassFail(bool passed, bool producedResult)
        {
            var c = ConsoleColor.Red;
            var ch = 'F';

            if (passed)
            {
                if (producedResult)
                {
                    c = ConsoleColor.Green;
                    ch = 'P';
                }
                else
                {
                    c = ConsoleColor.Yellow;
                    ch = 'W';
                }
            }

            Console.Write("[");
            Console.ForegroundColor = c;
            Console.Write(ch);
            Console.ResetColor();
            Console.Write("] ");
        }

        private static void WriteResultRow(string description, string version, object s, object p, object delta, object startup)
        {
            Console.WriteLine("{0} | {1} | {2} | {3} | {4} | {5}",
                description.PadLeft(4).PadRight(21),
                version.PadRight(9),
                s.ToString().PadLeft(9),
                p.ToString().PadLeft(9),
                delta.ToString().PadLeft(5),
                startup.ToString().PadLeft(4));
        }
    }
}
