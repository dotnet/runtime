// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace System.IO.Pipelines.Tests
{
    public partial class PipePoolTests
    {
        [Fact]
        public async Task WritesToArrayPoolByDefault()
        {
            var pipe = new Pipe();
            pipe.Writer.WriteEmpty(10);
            await pipe.Writer.FlushAsync();
            pipe.Writer.Complete();

            ReadResult result = await pipe.Reader.ReadAsync();
            Assert.Equal(10, result.Buffer.Length);

            SequenceMarshal.TryGetReadOnlySequenceSegment(
               result.Buffer,
               out ReadOnlySequenceSegment<byte> start,
               out int startIndex,
               out ReadOnlySequenceSegment<byte> end,
               out int endIndex);

            var startSegment = (BufferSegment)start;
            var endSegment = (BufferSegment)end;

            Assert.Same(startSegment, endSegment);
            Assert.NotNull(startSegment.Memory);
            Assert.IsType<byte[]>(startSegment.MemoryOwner);

            pipe.Reader.AdvanceTo(result.Buffer.End);
            pipe.Reader.Complete();
        }

        [Fact]
        public async Task GetMemoryOverMaxPoolSizeAllocatesArray()
        {
            using (var pool = new DisposeTrackingBufferPool())
            {
                var pipe = new Pipe(new PipeOptions(pool: pool));

                // Allocate 5 KB
                pipe.Writer.WriteEmpty(5 * 1024);
                await pipe.Writer.FlushAsync();
                pipe.Writer.Complete();

                Assert.Equal(0, pool.CurrentlyRentedBlocks);

                ReadResult result = await pipe.Reader.ReadAsync();
                Assert.Equal(5 * 1024, result.Buffer.Length);

                SequenceMarshal.TryGetReadOnlySequenceSegment(
                   result.Buffer,
                   out ReadOnlySequenceSegment<byte> start,
                   out int startIndex,
                   out ReadOnlySequenceSegment<byte> end,
                   out int endIndex);

                var startSegment = (BufferSegment)start;
                var endSegment = (BufferSegment)end;

                Assert.Same(startSegment, endSegment);

                // Null owner implies that the buffer is allocated and wasn't rented from the pool
                Assert.Null(startSegment.MemoryOwner);

                pipe.Reader.AdvanceTo(result.Buffer.End);
                pipe.Reader.Complete();

                Assert.Equal(0, pool.CurrentlyRentedBlocks);
                Assert.Equal(0, pool.DisposedBlocks);
            }
        }

        [Fact]
        public async Task GetMemoryAtMaxPoolSizeAllocatesFromPool()
        {
            using (var pool = new DisposeTrackingBufferPool())
            {
                var pipe = new Pipe(new PipeOptions(pool: pool));

                pipe.Writer.WriteEmpty(pool.MaxBufferSize);
                await pipe.Writer.FlushAsync();
                pipe.Writer.Complete();

                Assert.Equal(1, pool.CurrentlyRentedBlocks);

                ReadResult result = await pipe.Reader.ReadAsync();
                Assert.Equal(pool.MaxBufferSize, result.Buffer.Length);

                SequenceMarshal.TryGetReadOnlySequenceSegment(
                   result.Buffer,
                   out ReadOnlySequenceSegment<byte> start,
                   out int startIndex,
                   out ReadOnlySequenceSegment<byte> end,
                   out int endIndex);

                var startSegment = (BufferSegment)start;
                var endSegment = (BufferSegment)end;

                Assert.Same(startSegment, endSegment);

                // Null owner implies that the buffer is allocated and wasn't rented from the pool
                Assert.NotNull(startSegment.MemoryOwner);

                pipe.Reader.AdvanceTo(result.Buffer.End);
                pipe.Reader.Complete();

                Assert.Equal(0, pool.CurrentlyRentedBlocks);
                Assert.Equal(1, pool.DisposedBlocks);
            }
        }

        [Fact]
        public async Task WriteAsyncWritesIntoPool()
        {
            using (var pool = new DisposeTrackingBufferPool())
            {
                var pipe = new Pipe(new PipeOptions(pool: pool, minimumSegmentSize: pool.MaxBufferSize));

                var buffer = new byte[pool.MaxBufferSize * 2 + 1];
                await pipe.Writer.WriteAsync(buffer);
                pipe.Writer.Complete();

                Assert.Equal(3, pool.CurrentlyRentedBlocks);

                ReadResult result = await pipe.Reader.ReadAsync();
                Assert.Equal(pool.MaxBufferSize * 2 + 1, result.Buffer.Length);

                pipe.Reader.AdvanceTo(result.Buffer.End);
                pipe.Reader.Complete();

                Assert.Equal(0, pool.CurrentlyRentedBlocks);
                Assert.Equal(3, pool.DisposedBlocks);
            }
        }
    }
}
