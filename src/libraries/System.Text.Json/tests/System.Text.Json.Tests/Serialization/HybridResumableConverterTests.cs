// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;
using System.Threading.Tasks;
using System.IO.Pipelines;
using System.Buffers;
using System.Collections.Generic;

namespace System.Text.Json.Serialization.Tests
{
    public class HybridResumableConverterTests
    {
        internal class InstrumentedMemoryPool : MemoryPool<byte>
        {
            public List<int> RequestedBufferSizes = new();
            public int CumulativeAllocatedBytes = 0;
            public int CurrentAllocatedBytes = 0;
            public int PeakAllocatedBytes = 0;

            public override int MaxBufferSize => int.MaxValue;

            public override IMemoryOwner<byte> Rent(int minBufferSize = -1)
            {
                RequestedBufferSizes.Add(minBufferSize);
                IMemoryOwner<byte> memory = MemoryPool<byte>.Shared.Rent(minBufferSize);
                CurrentAllocatedBytes += memory.Memory.Length;
                PeakAllocatedBytes = Math.Max(PeakAllocatedBytes, CurrentAllocatedBytes);
                CumulativeAllocatedBytes += memory.Memory.Length;
                return new IntrumentedOwner(this, memory);
            }

            private void Return(IntrumentedOwner owner)
            {
                CurrentAllocatedBytes -= owner._memory.Memory.Length;
                owner._memory.Dispose();
            }

            protected override void Dispose(bool disposing) { }

            private class IntrumentedOwner : IMemoryOwner<byte>
            {
                public readonly InstrumentedMemoryPool _parent;
                public readonly IMemoryOwner<byte> _memory;

                public IntrumentedOwner(InstrumentedMemoryPool parent, IMemoryOwner<byte> memory)
                {
                    _parent = parent;
                    _memory = memory;
                }

                public Memory<byte> Memory => _memory.Memory;
                public void Dispose() => _parent.Return(this);
            }
        }

        [Fact]
        public static async Task WriteByteArraySegmentedAsync()
        {
            // We need to create a large enough value that will trigger the segmented writing logic.
            // The threshold in the code is 90% of 4 * MinimumSegmentSize, so we need a write to exceed that.
            // We also provide the buffer pool to validate that the requested buffers are less than the total write size.
            int threshold = (int)(0.9 * 4 * PipeOptions.Default.MinimumSegmentSize);
            var pool = new InstrumentedMemoryPool();
            var pipe = new Pipe(new PipeOptions(pool));

            var consumerFunc = async () =>
            {
                PipeReader reader = pipe.Reader;
                ReadResult result;
                while (!(result = await reader.ReadAsync()).IsCompleted) reader.AdvanceTo(result.Buffer.End);
                await reader.CompleteAsync();
            };

            Task consumer = Task.Run(consumerFunc);

            // Exceed the threshold by a large amount.
            int writeSize = 64 * threshold;
            await JsonSerializer.SerializeAsync(pipe.Writer, new byte[writeSize]);
            await pipe.Writer.CompleteAsync();
            await consumer;

            // Ensure all requested buffer sizes are capped. Note the threshold is just a heuristic, so it is possible that the threshold
            // will be far exceeded in practice. We just want to ensure that the requested buffer sizes have a constant upper bound.
            Assert.All(pool.RequestedBufferSizes, size => Assert.InRange(size, 0, 16 * threshold));
            Assert.InRange(pool.PeakAllocatedBytes, 0, 16 * threshold);
        }

        // TODO move to string test class
        [Fact]
        public static async Task WriteStringSegmentedAsync()
        {
            // We need to create a large enough value that will trigger the segmented writing logic.
            // The threshold in the code is 90% of 4 * MinimumSegmentSize, so we need a write to exceed that.
            // We also provide the buffer pool to validate that the requested buffers are less than the total write size.
            int threshold = (int)(0.9 * 4 * PipeOptions.Default.MinimumSegmentSize);
            var pool = new InstrumentedMemoryPool();
            var pipe = new Pipe(new PipeOptions(pool));

            var consumerFunc = async () =>
            {
                PipeReader reader = pipe.Reader;
                ReadResult result;
                while (!(result = await reader.ReadAsync()).IsCompleted) reader.AdvanceTo(result.Buffer.End);
                await reader.CompleteAsync();
            };

            Task consumer = Task.Run(consumerFunc);

            // Exceed the threshold by a large amount.
            int writeSize = 64 * threshold;
            await JsonSerializer.SerializeAsync(pipe.Writer, new string('a', writeSize));
            await pipe.Writer.CompleteAsync();
            await consumer;

            // Ensure all requested buffer sizes are capped. Note the threshold is just a heuristic, so it is possible that the threshold
            // will be far exceeded in practice. We just want to ensure that the requested buffer sizes have a constant upper bound.
            Assert.All(pool.RequestedBufferSizes, size => Assert.InRange(size, 0, 16 * threshold));
            Assert.InRange(pool.PeakAllocatedBytes, 0, 16 * threshold);
        }
    }
}
