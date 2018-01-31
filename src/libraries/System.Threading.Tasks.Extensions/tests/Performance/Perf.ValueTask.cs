// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;
using Microsoft.Xunit.Performance;
using Xunit;

namespace System.Threading.Tasks
{
    public class ValueTaskPerfTest
    {
        [Benchmark(InnerIterationCount = 10_000_000), MeasureGCAllocations]
        public async Task Await_FromResult()
        {
            ValueTask<int> vt = new ValueTask<int>(42);
            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                long iters = Benchmark.InnerIterationCount;
                using (iteration.StartMeasurement())
                {
                    for (long i = 0; i < iters; i++)
                    {
                        await vt;
                    }
                }
            }
        }

        [Benchmark(InnerIterationCount = 10_000_000), MeasureGCAllocations]
        public async Task Await_FromCompletedTask()
        {
            ValueTask<int> vt = new ValueTask<int>(Task.FromResult(42));
            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                long iters = Benchmark.InnerIterationCount;
                using (iteration.StartMeasurement())
                {
                    for (long i = 0; i < iters; i++)
                    {
                        await vt;
                    }
                }
            }
        }

        [Benchmark(InnerIterationCount = 10_000_000), MeasureGCAllocations]
        public async Task CreateAndAwait_FromResult()
        {
            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                long iters = Benchmark.InnerIterationCount;
                using (iteration.StartMeasurement())
                {
                    for (long i = 0; i < iters; i++)
                    {
                        await new ValueTask<int>((int)i);
                    }
                }
            }
        }

        [Benchmark(InnerIterationCount = 10_000_000), MeasureGCAllocations]
        public async Task CreateAndAwait_FromResult_ConfigureAwait()
        {
            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                long iters = Benchmark.InnerIterationCount;
                using (iteration.StartMeasurement())
                {
                    for (long i = 0; i < iters; i++)
                    {
                        await new ValueTask<int>((int)i).ConfigureAwait(false);
                    }
                }
            }
        }

        [Benchmark(InnerIterationCount = 10_000_000), MeasureGCAllocations]
        public async Task CreateAndAwait_FromCompletedTask()
        {
            Task<int> t = Task.FromResult(42);
            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                long iters = Benchmark.InnerIterationCount;
                using (iteration.StartMeasurement())
                {
                    for (long i = 0; i < iters; i++)
                    {
                        await new ValueTask<int>(t);
                    }
                }
            }
        }

        [Benchmark(InnerIterationCount = 10_000_000), MeasureGCAllocations]
        public async Task CreateAndAwait_FromCompletedTask_ConfigureAwait()
        {
            Task<int> t = Task.FromResult(42);
            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                long iters = Benchmark.InnerIterationCount;
                using (iteration.StartMeasurement())
                {
                    for (long i = 0; i < iters; i++)
                    {
                        await new ValueTask<int>(t).ConfigureAwait(false);
                    }
                }
            }
        }

        [Benchmark(InnerIterationCount = 1_000_000), MeasureGCAllocations]
        public async Task CreateAndAwait_FromYieldingAsyncMethod()
        {
            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                long iters = Benchmark.InnerIterationCount;
                using (iteration.StartMeasurement())
                {
                    for (long i = 0; i < iters; i++)
                    {
                        await new ValueTask<int>(YieldOnce());
                    }
                }
            }
        }

        [Benchmark(InnerIterationCount = 1_000_000), MeasureGCAllocations]
        public async Task CreateAndAwait_FromDelayedTCS()
        {
            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                long iters = Benchmark.InnerIterationCount;
                using (iteration.StartMeasurement())
                {
                    for (long i = 0; i < iters; i++)
                    {
                        var tcs = new TaskCompletionSource<int>();
                        ValueTask<int> vt = AwaitTcsAsValueTask(tcs);
                        tcs.SetResult(42);
                        await vt;
                    }
                }
            }
        }

        [Benchmark(InnerIterationCount = 10_000_000), MeasureGCAllocations]
        public void Copy_PassAsArgumentAndReturn_FromResult()
        {
            ValueTask<int> vt = new ValueTask<int>(42);
            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                long iters = Benchmark.InnerIterationCount;
                using (iteration.StartMeasurement())
                {
                    for (long i = 0; i < iters; i++)
                    {
                        vt = ReturnValueTask(vt);
                    }
                }
            }
        }

        [Benchmark(InnerIterationCount = 10_000_000), MeasureGCAllocations]
        public void Copy_PassAsArgumentAndReturn_FromTask()
        {
            ValueTask<int> vt = new ValueTask<int>(Task.FromResult(42));
            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                long iters = Benchmark.InnerIterationCount;
                using (iteration.StartMeasurement())
                {
                    for (long i = 0; i < iters; i++)
                    {
                        vt = ReturnValueTask(vt);
                    }
                }
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static ValueTask<int> ReturnValueTask(ValueTask<int> vt) => vt;

        private async ValueTask<int> AwaitTcsAsValueTask(TaskCompletionSource<int> tcs) => await new ValueTask<int>(tcs.Task).ConfigureAwait(false);

        private async Task<int> YieldOnce() { await Task.Yield(); return 42; }
    }
}
