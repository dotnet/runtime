// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Xunit.Performance;
using Xunit;

namespace System.Collections.Concurrent.Tests
{
    public class Perf_ConcurrentQueue
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
                        new ConcurrentQueue<int>(); // 1
                        new ConcurrentQueue<int>(); // 2
                        new ConcurrentQueue<int>(); // 3
                        new ConcurrentQueue<int>(); // 4
                        new ConcurrentQueue<int>(); // 5
                        new ConcurrentQueue<int>(); // 6
                        new ConcurrentQueue<int>(); // 7
                        new ConcurrentQueue<int>(); // 8
                        new ConcurrentQueue<int>(); // 9
                        new ConcurrentQueue<int>(); // 10
                    }
                }
            }
        }

        [Benchmark(InnerIterationCount = 10_000_000), MeasureGCAllocations]
        public void Enqueue()
        {
            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                long size = Benchmark.InnerIterationCount;
                var q = new ConcurrentQueue<int>();

                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < size; i++)
                    {
                        q.Enqueue(i);
                    }
                }
            }
        }

        [Benchmark(InnerIterationCount = 1_000_000), MeasureGCAllocations]
        [InlineData(false)]
        [InlineData(true)]
        public void TryDequeue(bool exists)
        {
            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                long size = Benchmark.InnerIterationCount;
                var b = new ConcurrentQueue<int>();

                if (exists)
                {
                    for (int i = 0; i < size; i++)
                    {
                        b.Enqueue(i);
                    }
                }

                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < size; i++)
                    {
                        b.TryDequeue(out int result);
                    }
                }
            }
        }

        [Benchmark(InnerIterationCount = 1_000_000), MeasureGCAllocations]
        public void EnqueueTryDequeue()
        {
            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                long size = Benchmark.InnerIterationCount;
                var b = new ConcurrentQueue<int>();

                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < size; i++)
                    {
                        b.Enqueue(i);
                        b.TryDequeue(out int result);
                    }
                }
            }
        }

        [Benchmark(InnerIterationCount = 1_000_000), MeasureGCAllocations]
        [InlineData(false)]
        [InlineData(true)]
        public void ProducerConsumer_EnqueueTryDequeueFromDifferentThreads(bool presize)
        {
            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                const int NumThreads = 2;
                long size = Benchmark.InnerIterationCount;
                var q = new ConcurrentQueue<int>();

                if (presize)
                {
                    for (int i = 0; i < size * 2; i++)
                    {
                        q.Enqueue(i);
                    }
                    while (q.TryDequeue(out _))
                    {
                    }
                }

                using (var barrier = new Barrier(NumThreads + 1))
                {
                    Task p = Task.Factory.StartNew(() =>
                    {
                        barrier.SignalAndWait();
                        barrier.SignalAndWait();

                        for (int i = 0; i < size; i++)
                        {
                            q.Enqueue(i);
                        }
                    }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);

                    Task c = Task.Factory.StartNew(() =>
                    {
                        barrier.SignalAndWait();
                        barrier.SignalAndWait();

                        int count = 0;
                        while (count < size)
                        {
                            if (q.TryDequeue(out int result))
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
