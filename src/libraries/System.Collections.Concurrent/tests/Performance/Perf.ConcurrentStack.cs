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
    public class Perf_ConcurrentStack
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
                        new ConcurrentStack<int>(); // 1
                        new ConcurrentStack<int>(); // 2
                        new ConcurrentStack<int>(); // 3
                        new ConcurrentStack<int>(); // 4
                        new ConcurrentStack<int>(); // 5
                        new ConcurrentStack<int>(); // 6
                        new ConcurrentStack<int>(); // 7
                        new ConcurrentStack<int>(); // 8
                        new ConcurrentStack<int>(); // 9
                        new ConcurrentStack<int>(); // 10
                    }
                }
            }
        }

        [Benchmark(InnerIterationCount = 10_000_000), MeasureGCAllocations]
        public void Push()
        {
            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                long size = Benchmark.InnerIterationCount;
                var q = new ConcurrentStack<int>();

                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < size; i++)
                    {
                        q.Push(i);
                    }
                }
            }
        }

        [Benchmark(InnerIterationCount = 1_000_000), MeasureGCAllocations]
        [InlineData(false)]
        [InlineData(true)]
        public void TryPop(bool exists)
        {
            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                long size = Benchmark.InnerIterationCount;
                var b = new ConcurrentStack<int>();

                if (exists)
                {
                    for (int i = 0; i < size; i++)
                    {
                        b.Push(i);
                    }
                }

                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < size; i++)
                    {
                        b.TryPop(out int result);
                    }
                }
            }
        }

        [Benchmark(InnerIterationCount = 1_000_000), MeasureGCAllocations]
        public void PushTryPop()
        {
            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                long size = Benchmark.InnerIterationCount;
                var b = new ConcurrentStack<int>();

                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < size; i++)
                    {
                        b.Push(i);
                        b.TryPop(out int result);
                    }
                }
            }
        }

        [Benchmark(InnerIterationCount = 1_000_000), MeasureGCAllocations]
        [InlineData(0)]
        [InlineData(3)]
        public void Pooling_PushTakeFromSameThreads(int initialSize)
        {
            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                const int NumThreads = 2;
                long size = Benchmark.InnerIterationCount;
                var b = new ConcurrentStack<int>();

                using (var barrier = new Barrier(NumThreads + 1))
                {
                    Task[] tasks = Enumerable.Range(0, NumThreads).Select(_ => Task.Factory.StartNew(() =>
                    {
                        for (int i = 0; i < initialSize; i++)
                        {
                            b.Push(i);
                        }

                        barrier.SignalAndWait();
                        barrier.SignalAndWait();

                        for (int i = 0; i < size; i++)
                        {
                            b.Push(i);
                            b.TryPop(out int result);
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
        public void ProducerConsumer_PushTakeFromDifferentThreads()
        {
            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                const int NumThreads = 2;
                long size = Benchmark.InnerIterationCount;
                var b = new ConcurrentStack<int>();

                using (var barrier = new Barrier(NumThreads + 1))
                {
                    Task p = Task.Factory.StartNew(() =>
                    {
                        barrier.SignalAndWait();
                        barrier.SignalAndWait();

                        for (int i = 0; i < size; i++)
                        {
                            b.Push(i);
                        }
                    }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);

                    Task c = Task.Factory.StartNew(() =>
                    {
                        barrier.SignalAndWait();
                        barrier.SignalAndWait();

                        int count = 0;
                        while (count < size)
                        {
                            if (b.TryPop(out int result))
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
