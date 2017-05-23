// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Xunit.Performance;

namespace System.Threading.Tests
{
    public class Perf_Interlocked
    {
        [Benchmark(InnerIterationCount = 1000)]
        public static void Increment_int()
        {
            int location = 0;

            foreach (var iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        Interlocked.Increment(ref location);
                    }
                }
            }
        }

        [Benchmark(InnerIterationCount = 1000)]
        public static void Decrement_int()
        {
            int location = 0;

            foreach (var iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        Interlocked.Decrement(ref location);
                    }
                }
            }
        }

        [Benchmark(InnerIterationCount = 1000)]
        public void Increment_long()
        {
            long location = 0;

            foreach (var iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        Interlocked.Increment(ref location);
                    }
                }
            }
        }

        [Benchmark(InnerIterationCount = 1000)]
        public void Decrement_long()
        {
            long location = 0;

            foreach (var iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        Interlocked.Decrement(ref location);
                    }
                }
            }
        }

        [Benchmark(InnerIterationCount = 1000)]
        public void Add_int()
        {
            int location = 0;

            foreach (var iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        Interlocked.Add(ref location, 2);
                    }
                }
            }
        }

        [Benchmark(InnerIterationCount = 1000)]
        public void Add_long()
        {
            long location = 0;

            foreach (var iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        Interlocked.Add(ref location, 2);
                    }
                }
            }
        }

        [Benchmark(InnerIterationCount = 1000)]
        public static void Exchange_int()
        {
            int location = 0;
            int newValue = 1;

            foreach (var iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        Interlocked.Exchange(ref location, newValue);
                    }
                }
            }
        }

        [Benchmark(InnerIterationCount = 1000)]
        public static void Exchange_long()
        {
            long location = 0;
            long newValue = 1;

            foreach (var iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        Interlocked.Exchange(ref location, newValue);
                    }
                }
            }
        }

        [Benchmark(InnerIterationCount = 500)]
        public static void CompareExchange_int()
        {
            int location = 0;
            int newValue = 1;
            int comparand = 0;

            foreach (var iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        Interlocked.CompareExchange(ref location, newValue, comparand);
                    }
                }
            }
        }

        [Benchmark(InnerIterationCount = 500)]
        public static void CompareExchange_long()
        {
            long location = 0;
            long newValue = 1;
            long comparand = 0;

            foreach (var iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        Interlocked.CompareExchange(ref location, newValue, comparand);
                    }
                }
            }
        }
    }
}
