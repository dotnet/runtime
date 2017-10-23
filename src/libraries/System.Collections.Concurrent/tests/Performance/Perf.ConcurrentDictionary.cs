// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Xunit.Performance;
using Xunit;

namespace System.Collections.Concurrent.Tests
{
    public class Perf_ConcurrentDictionary
    {
        [Benchmark(InnerIterationCount = 1_000_000), MeasureGCAllocations]
        public void Ctor()
        {
            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                long innerIters = Benchmark.InnerIterationCount / 10;
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < innerIters; i++)
                    {
                        new ConcurrentDictionary<int, string>(); // 1
                        new ConcurrentDictionary<int, string>(); // 2
                        new ConcurrentDictionary<int, string>(); // 3
                        new ConcurrentDictionary<int, string>(); // 4
                        new ConcurrentDictionary<int, string>(); // 5
                        new ConcurrentDictionary<int, string>(); // 6
                        new ConcurrentDictionary<int, string>(); // 7
                        new ConcurrentDictionary<int, string>(); // 8
                        new ConcurrentDictionary<int, string>(); // 9
                        new ConcurrentDictionary<int, string>(); // 10
                    }
                }
            }
        }

        [Benchmark(InnerIterationCount = 1_000_000), MeasureGCAllocations]
        [InlineData(false)]
        [InlineData(true)]
        public void TryAdd(bool exists)
        {
            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                long size = Benchmark.InnerIterationCount;

                var d = new ConcurrentDictionary<int, int>();
                if (exists)
                {
                    for (int i = 0; i < size; i++)
                    {
                        d.TryAdd(i, i);
                    }
                }

                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < size; i++)
                    {
                        d.TryAdd(i, i);
                    }
                }
            }
        }

        [Benchmark(InnerIterationCount = 1_000_000), MeasureGCAllocations]
        [InlineData(false)]
        [InlineData(true)]
        public void TryGetValue(bool exists)
        {
            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                long size = Benchmark.InnerIterationCount;

                var d = new ConcurrentDictionary<long, long>();
                for (int i = 0; i < size; i++)
                {
                    d.TryAdd(exists ? i : i + size, i);
                }

                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < size; i++)
                    {
                        d.TryGetValue(i, out long result);
                    }
                }
            }
        }

        [Benchmark(InnerIterationCount = 1_000_000), MeasureGCAllocations]
        [InlineData(false)]
        [InlineData(true)]
        public void GetOrAdd(bool exists)
        {
            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                long size = Benchmark.InnerIterationCount;

                var d = new ConcurrentDictionary<long, long>();
                for (int i = 0; i < size; i++)
                {
                    d.TryAdd(exists ? i : i + size, i);
                }

                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < size; i++)
                    {
                        d.GetOrAdd(i, i);
                    }
                }
            }
        }
    }
}
