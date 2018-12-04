// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Xunit.Performance;

namespace System.IO.Pipelines.Tests
{
    public sealed class Perf_Pipe
    {
        private const int InnerIterationCount = 10_000;

        [Benchmark(InnerIterationCount = InnerIterationCount)]
        public async void SyncReadAsync()
        {
            // Setup
            var pipe = new Pipe(new PipeOptions(pool: null, readerScheduler: PipeScheduler.Inline, writerScheduler: PipeScheduler.Inline, useSynchronizationContext: false));

            PipeWriter writer = pipe.Writer;
            PipeReader reader = pipe.Reader;

            await writer.WriteAsync(new ReadOnlyMemory<byte>(new byte[] { 0, 0, 0, 0 }));
            await writer.FlushAsync();

            // Actual perf testing
            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        ReadResult result = await reader.ReadAsync();
                        reader.AdvanceTo(result.Buffer.Start);
                    }
                }
            }
        }

        [Benchmark(InnerIterationCount = InnerIterationCount)]
        public async void ReadAsync()
        {
            // Setup
            var pipe = new Pipe(new PipeOptions(pool: null, readerScheduler: PipeScheduler.Inline, writerScheduler: PipeScheduler.Inline, useSynchronizationContext: false));

            PipeWriter writer = pipe.Writer;
            PipeReader reader = pipe.Reader;

            var data = new byte[] { 0 };

            // Actual perf testing
            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        ValueTask<ReadResult> task = reader.ReadAsync();

                        await writer.WriteAsync(data);
                        await writer.FlushAsync();

                        ReadResult result = await task;
                        reader.AdvanceTo(result.Buffer.End);
                    }
                }
            }
        }

        [Benchmark(InnerIterationCount = InnerIterationCount)]
        public async void SyncReadAsyncWithCancellationToken()
        {
            // Setup
            var pipe = new Pipe(new PipeOptions(pool: null, readerScheduler: PipeScheduler.Inline, writerScheduler: PipeScheduler.Inline, useSynchronizationContext: false));

            PipeWriter writer = pipe.Writer;
            PipeReader reader = pipe.Reader;
            CancellationTokenSource cts = new CancellationTokenSource();

            await writer.WriteAsync(new ReadOnlyMemory<byte>(new byte[] { 0, 0, 0, 0 }));
            await writer.FlushAsync();

            // Actual perf testing
            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        ReadResult result = await reader.ReadAsync(cts.Token);
                        reader.AdvanceTo(result.Buffer.Start);
                    }
                }
            }
        }

        [Benchmark(InnerIterationCount = InnerIterationCount)]
        public async void ReadAsyncWithCancellationToken()
        {
            // Setup
            var pipe = new Pipe(new PipeOptions(pool: null, readerScheduler: PipeScheduler.Inline, writerScheduler: PipeScheduler.Inline, useSynchronizationContext: false));

            PipeWriter writer = pipe.Writer;
            PipeReader reader = pipe.Reader;

            var data = new byte[] { 0 };

            CancellationTokenSource cts = new CancellationTokenSource();

            // Actual perf testing
            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        ValueTask<ReadResult> task = reader.ReadAsync(cts.Token);

                        await writer.WriteAsync(data);
                        await writer.FlushAsync();

                        ReadResult result = await task;
                        reader.AdvanceTo(result.Buffer.End);
                    }
                }
            }
        }

    }
}
