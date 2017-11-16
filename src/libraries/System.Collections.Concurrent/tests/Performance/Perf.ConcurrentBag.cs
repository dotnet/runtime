// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Xunit.Performance;
using Xunit;

namespace System.Collections.Concurrent.Tests
{
    public class Perf_ConcurrentBag
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
                        new ConcurrentBag<int>(); // 1
                        new ConcurrentBag<int>(); // 2
                        new ConcurrentBag<int>(); // 3
                        new ConcurrentBag<int>(); // 4
                        new ConcurrentBag<int>(); // 5
                        new ConcurrentBag<int>(); // 6
                        new ConcurrentBag<int>(); // 7
                        new ConcurrentBag<int>(); // 8
                        new ConcurrentBag<int>(); // 9
                        new ConcurrentBag<int>(); // 10
                    }
                }
            }
        }

        [Benchmark(InnerIterationCount = 10_000_000), MeasureGCAllocations]
        public void Add()
        {
            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                long size = Benchmark.InnerIterationCount;
                var q = new ConcurrentBag<int>();

                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < size; i++)
                    {
                        q.Add(i);
                    }
                }
            }
        }

        [Benchmark(InnerIterationCount = 1_000_000), MeasureGCAllocations]
        [InlineData(false)]
        [InlineData(true)]
        public void TryTake(bool exists)
        {
            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                long size = Benchmark.InnerIterationCount;
                var b = new ConcurrentBag<int>();

                if (exists)
                {
                    for (int i = 0; i < size; i++)
                    {
                        b.Add(i);
                    }
                }

                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < size; i++)
                    {
                        b.TryTake(out int result);
                    }
                }
            }
        }

        [Benchmark(InnerIterationCount = 1_000_000), MeasureGCAllocations]
        public void AddTake()
        {
            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                long size = Benchmark.InnerIterationCount;
                var b = new ConcurrentBag<int>();

                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < size; i++)
                    {
                        b.Add(i);
                        b.TryTake(out int result);
                    }
                }
            }
        }

        [Benchmark(InnerIterationCount = 1_000_000), MeasureGCAllocations]
        [InlineData(0)]
        [InlineData(3)]
        public void Pooling_AddTakeFromSameThreads(int initialSize)
        {
            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                const int NumThreads = 2;
                long size = Benchmark.InnerIterationCount;
                var b = new ConcurrentBag<int>();

                using (var barrier = new Barrier(NumThreads + 1))
                {
                    Task[] tasks = Enumerable.Range(0, NumThreads).Select(_ => Task.Factory.StartNew(() =>
                    {
                        for (int i = 0; i < initialSize; i++)
                        {
                            b.Add(i);
                        }

                        barrier.SignalAndWait();
                        barrier.SignalAndWait();

                        for (int i = 0; i < size; i++)
                        {
                            b.Add(i);
                            b.TryTake(out int result);
                        }
                    }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default)).ToArray();

                    barrier.SignalAndWait();
                    using (iteration.StartMeasurement())
                    {
                        barrier.SignalAndWait();
                        Task.WaitAll(tasks);
                    }
                }
            }
        }

        [Benchmark(InnerIterationCount = 1_000_000), MeasureGCAllocations]
        public void ProducerConsumer_AddTakeFromDifferentThreads()
        {
            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                const int NumThreads = 2;
                long size = Benchmark.InnerIterationCount;
                var b = new ConcurrentBag<int>();

                using (var barrier = new Barrier(NumThreads + 1))
                {
                    Task p = Task.Factory.StartNew(() =>
                    {
                        barrier.SignalAndWait();
                        barrier.SignalAndWait();

                        for (int i = 0; i < size; i++)
                        {
                            b.Add(i);
                        }
                    }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);

                    Task c = Task.Factory.StartNew(() =>
                    {
                        barrier.SignalAndWait();
                        barrier.SignalAndWait();

                        int count = 0;
                        while (count < size)
                        {
                            if (b.TryTake(out int result))
                            {
                                count++;
                            }
                        }
                    }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);

                    barrier.SignalAndWait();
                    using (iteration.StartMeasurement())
                    {
                        barrier.SignalAndWait();
                        Task.WaitAll(p, c);
                    }
                }
            }
        }
    }
}
