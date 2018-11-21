// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Xunit.Performance;

namespace System.IO.Pipelines.Tests
{
    public sealed class Perf_Pipe
    {
        [Benchmark(InnerIterationCount = 1000)]
        public async void ReadAsync()
        {
            // Setup
            var pipe = new Pipe(new PipeOptions(pool: null, readerScheduler: PipeScheduler.Inline, writerScheduler: PipeScheduler.Inline, useSynchronizationContext: false));

            var writer = pipe.Writer;
            var reader = pipe.Reader;

            await writer.WriteAsync(new ReadOnlyMemory<byte>(new byte[] { 0, 0, 0, 0 }));
            await writer.FlushAsync();

            // Actual perf testing
            foreach (var iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        var result = await reader.ReadAsync();
                        reader.AdvanceTo(result.Buffer.Start);
                    }
                }
            }
        }
    }
}
