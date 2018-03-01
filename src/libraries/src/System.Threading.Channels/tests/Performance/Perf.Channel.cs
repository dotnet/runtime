// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.Xunit.Performance;

namespace System.Threading.Channels.Tests
{
    public sealed class UnboundedChannelPerfTests : PerfTests
    {
        public override Channel<int> CreateChannel() => Channel.CreateUnbounded<int>();
    }

    public sealed class SpscUnboundedChannelPerfTests : PerfTests
    {
        public override Channel<int> CreateChannel() => Channel.CreateUnbounded<int>(new UnboundedChannelOptions { SingleReader = true, SingleWriter = true });
    }

    public sealed class BoundedChannelPerfTests : PerfTests
    {
        public override Channel<int> CreateChannel() => Channel.CreateBounded<int>(10);
    }

    public abstract class PerfTests
    {
        public abstract Channel<int> CreateChannel();

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

        [Benchmark(InnerIterationCount = 1_000_000), MeasureGCAllocations]
        public async Task PingPong()
        {
            Channel<int> channel1 = CreateChannel();
            Channel<int> channel2 = CreateChannel();

            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                long iters = Benchmark.InnerIterationCount;
                using (iteration.StartMeasurement())
                {
                    await Task.WhenAll(
                        Task.Run(async () =>
                        {
                            ChannelReader<int> reader = channel1.Reader;
                            ChannelWriter<int> writer = channel2.Writer;
                            for (int i = 0; i < iters; i++)
                            {
                                await writer.WriteAsync(i);
                                await reader.ReadAsync();
                            }
                        }),
                        Task.Run(async () =>
                        {
                            ChannelWriter<int> writer = channel1.Writer;
                            ChannelReader<int> reader = channel2.Reader;
                            for (int i = 0; i < iters; i++)
                            {
                                await reader.ReadAsync();
                                await writer.WriteAsync(i);
                            }
                        }));
                }
            }
        }
    }
}
