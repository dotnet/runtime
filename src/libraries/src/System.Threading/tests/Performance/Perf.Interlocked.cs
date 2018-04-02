// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Xunit.Performance;

namespace System.Threading.Tests
{
    public class Perf_Interlocked
    {
        private const int IterationCount = 10_000_000;

        [Benchmark(InnerIterationCount = IterationCount)]
        public static void Increment_int()
        {
            int location = 0;

            foreach (var iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < IterationCount; i++)
                    {
                        Interlocked.Increment(ref location);
                    }
                }
            }
        }

        [Benchmark(InnerIterationCount = IterationCount)]
        public static void Decrement_int()
        {
            int location = 0;

            foreach (var iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < IterationCount; i++)
                    {
                        Interlocked.Decrement(ref location);
                    }
                }
            }
        }

        [Benchmark(InnerIterationCount = IterationCount)]
        public void Increment_long()
        {
            long location = 0;

            foreach (var iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < IterationCount; i++)
                    {
                        Interlocked.Increment(ref location);
                    }
                }
            }
        }

        [Benchmark(InnerIterationCount = IterationCount)]
        public void Decrement_long()
        {
            long location = 0;

            foreach (var iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < IterationCount; i++)
                    {
                        Interlocked.Decrement(ref location);
                    }
                }
            }
        }

        [Benchmark(InnerIterationCount = IterationCount)]
        public void Add_int()
        {
            int location = 0;

            foreach (var iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < IterationCount; i++)
                    {
                        Interlocked.Add(ref location, 2);
                    }
                }
            }
        }

        [Benchmark(InnerIterationCount = IterationCount)]
        public void Add_long()
        {
            long location = 0;

            foreach (var iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < IterationCount; i++)
                    {
                        Interlocked.Add(ref location, 2);
                    }
                }
            }
        }

        [Benchmark(InnerIterationCount = IterationCount)]
        public static void Exchange_int()
        {
            int location = 0;
            int newValue = 1;

            foreach (var iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < IterationCount; i++)
                    {
                        Interlocked.Exchange(ref location, newValue);
                    }
                }
            }
        }

        [Benchmark(InnerIterationCount = IterationCount)]
        public static void Exchange_long()
        {
            long location = 0;
            long newValue = 1;

            foreach (var iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < IterationCount; i++)
                    {
                        Interlocked.Exchange(ref location, newValue);
                    }
                }
            }
        }

        [Benchmark(InnerIterationCount = IterationCount)]
        public static void CompareExchange_int()
        {
            int location = 0;
            int newValue = 1;
            int comparand = 0;

            foreach (var iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < IterationCount; i++)
                    {
                        Interlocked.CompareExchange(ref location, newValue, comparand);
                    }
                }
            }
        }

        [Benchmark(InnerIterationCount = IterationCount)]
        public static void CompareExchange_long()
        {
            long location = 0;
            long newValue = 1;
            long comparand = 0;

            foreach (var iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < IterationCount; i++)
                    {
                        Interlocked.CompareExchange(ref location, newValue, comparand);
                    }
                }
            }
        }
    }
}
