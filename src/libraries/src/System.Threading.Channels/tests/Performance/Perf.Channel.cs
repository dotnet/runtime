// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.Xunit.Performance;

namespace System.Threading.Channels.Tests
{
    public sealed class Perf_UnboundedChannelTests : Perf_BufferingTests
    {
        public override Channel<int> CreateChannel() => Channel.CreateUnbounded<int>();
    }

    public sealed class Perf_UnboundedSpscChannelTests : Perf_BufferingTests
    {
        public override Channel<int> CreateChannel() => Channel.CreateUnbounded<int>(new UnboundedChannelOptions { SingleReader = true, SingleWriter = true });
    }

    public sealed class Perf_BoundedChannelTests : Perf_BufferingTests
    {
        public override Channel<int> CreateChannel() => Channel.CreateBounded<int>(10);
    }

    public sealed class Perf_UnbufferedChannelTests : Perf_Tests
    {
        public override Channel<int> CreateChannel() => Channel.CreateUnbuffered<int>();
    }

    public abstract class Perf_BufferingTests : Perf_Tests
    {
        [Benchmark(InnerIterationCount = 1_000_000), MeasureGCAllocations]
        public void TryWriteThenTryRead()
        {
            Channel<int> channel = CreateChannel();
            ChannelReader<int> reader = channel.Reader;
            ChannelWriter<int> writer = channel.Writer;

            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                long iters = Benchmark.InnerIterationCount;
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < iters; i++)
                    {
                        writer.TryWrite(i);
                        reader.TryRead(out _);
                    }
                }
            }
        }

        [Benchmark(InnerIterationCount = 1_000_000), MeasureGCAllocations]
        public async Task WriteAsyncThenReadAsync()
        {
            Channel<int> channel = CreateChannel();
            ChannelReader<int> reader = channel.Reader;
            ChannelWriter<int> writer = channel.Writer;

            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                long iters = Benchmark.InnerIterationCount;
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < iters; i++)
                    {
                        await writer.WriteAsync(i);
                        await reader.ReadAsync();
                    }
                }
            }
        }

        [Benchmark(InnerIterationCount = 1_000_000), MeasureGCAllocations]
        public async Task ReadAsyncThenWriteAsync()
        {
            Channel<int> channel = CreateChannel();
            ChannelReader<int> reader = channel.Reader;
            ChannelWriter<int> writer = channel.Writer;

            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                long iters = Benchmark.InnerIterationCount;
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < iters; i++)
                    {
                        ValueTask<int> r = reader.ReadAsync();
                        await writer.WriteAsync(42);
                        await r;
                    }
                }
            }
        }
    }

    public abstract class Perf_Tests
    {
        public abstract Channel<int> CreateChannel();

        [Benchmark(InnerIterationCount = 1_000_000), MeasureGCAllocations]
        public async Task ConcurrentReadAsyncWriteAsync()
        {
            Channel<int> channel = CreateChannel();

            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                long iters = Benchmark.InnerIterationCount;
                using (iteration.StartMeasurement())
                {
                    await Task.WhenAll(
                        Task.Run(async () =>
                        {
                            ChannelReader<int> reader = channel.Reader;
                            for (int i = 0; i < iters; i++)
                            {
                                await reader.ReadAsync();
                            }
                        }),
                        Task.Run(async () =>
                        {
                            ChannelWriter<int> writer = channel.Writer;
                            for (int i = 0; i < iters; i++)
                            {
                                await writer.WriteAsync(i);
                            }
                        }));
                }
            }
        }
    }
}
