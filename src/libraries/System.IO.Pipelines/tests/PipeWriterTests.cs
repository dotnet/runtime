// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace System.IO.Pipelines.Tests
{
    public class PipeWriterTests : PipeTest
    {
        public PipeWriterTests() : base(0, 0)
        {
        }

        private byte[] Read()
        {
            Pipe.Writer.FlushAsync().GetAwaiter().GetResult();
            ReadResult readResult = Pipe.Reader.ReadAsync().GetAwaiter().GetResult();
            byte[] data = readResult.Buffer.ToArray();
            Pipe.Reader.AdvanceTo(readResult.Buffer.End);
            return data;
        }

        [Theory]
        [InlineData(3, -1, 0)]
        [InlineData(3, 0, -1)]
        [InlineData(3, 0, 4)]
        [InlineData(3, 4, 0)]
        [InlineData(3, -1, -1)]
        [InlineData(3, 4, 4)]
        public void ThrowsForInvalidParameters(int arrayLength, int offset, int length)
        {
            PipeWriter writer = Pipe.Writer;
            var array = new byte[arrayLength];
            for (var i = 0; i < array.Length; i++)
            {
                array[i] = (byte)(i + 1);
            }

            writer.Write(new Span<byte>(array, 0, 0));
            writer.Write(new Span<byte>(array, array.Length, 0));

            try
            {
                writer.Write(new Span<byte>(array, offset, length));
                Assert.Fail();
            }
            catch (Exception ex)
            {
                Assert.True(ex is ArgumentOutOfRangeException);
            }

            writer.Write(new Span<byte>(array, 0, array.Length));
            Assert.Equal(array, Read());
        }

        [Theory]
        [InlineData(0, 3)]
        [InlineData(1, 1)]
        [InlineData(1, 2)]
        [InlineData(2, 1)]
        public void CanWriteWithOffsetAndLength(int offset, int length)
        {
            PipeWriter writer = Pipe.Writer;
            var array = new byte[] { 1, 2, 3 };

            writer.Write(new Span<byte>(array, offset, length));

            Assert.Equal(array.Skip(offset).Take(length).ToArray(), Read());
        }

        [Fact]
        public void CanWriteIntoHeadlessBuffer()
        {
            PipeWriter writer = Pipe.Writer;

            writer.Write(new byte[] { 1, 2, 3 });
            Assert.Equal(new byte[] { 1, 2, 3 }, Read());
        }

        [Fact]
        public void CanWriteMultipleTimes()
        {
            PipeWriter writer = Pipe.Writer;

            writer.Write(new byte[] { 1 });
            writer.Write(new byte[] { 2 });
            writer.Write(new byte[] { 3 });

            Assert.Equal(new byte[] { 1, 2, 3 }, Read());
        }

        [Fact]
        public async Task CanWriteOverTheBlockLength()
        {
            Memory<byte> memory = Pipe.Writer.GetMemory();
            PipeWriter writer = Pipe.Writer;

            IEnumerable<byte> source = Enumerable.Range(0, memory.Length).Select(i => (byte)i);
            byte[] expectedBytes = source.Concat(source).Concat(source).ToArray();

            await writer.WriteAsync(expectedBytes);

            Assert.Equal(expectedBytes, Read());
        }

        [Fact]
        public void EnsureAllocatesSpan()
        {
            PipeWriter writer = Pipe.Writer;
            var span = writer.GetSpan(10);

            Assert.True(span.Length >= 10);
            // 0 byte Flush would not complete the reader so we complete.
            Pipe.Writer.Complete();
            Assert.Equal(new byte[] { }, Read());
        }

        [Fact]
        public void SlicesSpanAndAdvancesAfterWrite()
        {
            int initialLength = Pipe.Writer.GetSpan(3).Length;

            PipeWriter writer = Pipe.Writer;

            writer.Write(new byte[] { 1, 2, 3 });
            Span<byte> span = Pipe.Writer.GetSpan();

            Assert.Equal(initialLength - 3, span.Length);
            Assert.Equal(new byte[] { 1, 2, 3 }, Read());
        }

        [Theory]
        [InlineData(5)]
        [InlineData(50)]
        [InlineData(500)]
        [InlineData(5000)]
        [InlineData(50000)]
        public async Task WriteLargeDataBinary(int length)
        {
            var data = new byte[length];
            new Random(length).NextBytes(data);
            PipeWriter output = Pipe.Writer;
            await output.WriteAsync(data);

            ReadResult result = await Pipe.Reader.ReadAsync();
            ReadOnlySequence<byte> input = result.Buffer;
            Assert.Equal(data, input.ToArray());
            Pipe.Reader.AdvanceTo(input.End);
        }

        [Fact]
        public async Task CanWriteNothingToBuffer()
        {
            PipeWriter buffer = Pipe.Writer;
            buffer.GetMemory(0);
            buffer.Advance(0); // doing nothing, the hard way
            await buffer.FlushAsync();
        }

        [Fact]
        public async Task WriteNothingThenWriteToNewSegment()
        {
            // Regression test: write nothing to force a segment to be created, then do a large write that's larger than the currently empty segment to force another new segment
            // Verify that no 0 length segments are returned from the Reader.
            PipeWriter buffer = Pipe.Writer;
            Memory<byte> memory = buffer.GetMemory();
            buffer.Advance(0); // doing nothing, the hard way
            await buffer.FlushAsync();

            memory = buffer.GetMemory(memory.Length + 1);
            buffer.Advance(memory.Length);
            await buffer.FlushAsync();

            var res = await Pipe.Reader.ReadAsync();
            Assert.True(res.Buffer.IsSingleSegment);
            Assert.Equal(memory.Length, res.Buffer.Length);
        }

        [Fact]
        public async Task WriteNothingBetweenTwoFullWrites()
        {
            int totalWrittenLength = 0;
            PipeWriter buffer = Pipe.Writer;
            Memory<byte> memory = buffer.GetMemory();
            buffer.Advance(memory.Length); // doing nothing, the hard way
            totalWrittenLength += memory.Length;
            await buffer.FlushAsync();

            memory = buffer.GetMemory();
            buffer.Advance(0); // doing nothing, the hard way
            await buffer.FlushAsync();

            memory = buffer.GetMemory(memory.Length + 1);
            buffer.Advance(memory.Length);
            totalWrittenLength += memory.Length;
            await buffer.FlushAsync();

            var res = await Pipe.Reader.ReadAsync();
            var segmentCount = 0;
            foreach (ReadOnlyMemory<byte> _ in res.Buffer)
            {
                segmentCount++;
            }
            Assert.Equal(2, segmentCount);
            Assert.Equal(totalWrittenLength, res.Buffer.Length);
        }

        [Fact]
        public async Task WriteNothingThenWriteSomeBytes()
        {
            PipeWriter buffer = Pipe.Writer;
            _ = buffer.GetMemory();
            buffer.Advance(0); // doing nothing, the hard way
            await buffer.FlushAsync();

            var memory = buffer.GetMemory();
            buffer.Advance(memory.Length);
            await buffer.FlushAsync();

            var res = await Pipe.Reader.ReadAsync();
            Assert.True(res.Buffer.IsSingleSegment);
            Assert.Equal(memory.Length, res.Buffer.Length);
        }

        [Fact]
        public void EmptyWriteDoesNotThrow()
        {
            Pipe.Writer.Write(new byte[0]);
        }

        [Fact]
        public void ThrowsOnAdvanceOverMemorySize()
        {
            Memory<byte> buffer = Pipe.Writer.GetMemory(1);
            Assert.Throws<ArgumentOutOfRangeException>(() => Pipe.Writer.Advance(buffer.Length + 1));
        }

        [Fact]
        public void ThrowsOnAdvanceWithNoMemory()
        {
            PipeWriter buffer = Pipe.Writer;
            Assert.Throws<ArgumentOutOfRangeException>(() => buffer.Advance(1));
        }

        [Fact]
        public async Task WritesUsingGetSpanWorks()
        {
            byte[] bytes = "abcdefghijklmnopqrstuvwzyz"u8.ToArray();
            var pipe = new Pipe(new PipeOptions(pool: new HeapBufferPool(), minimumSegmentSize: 1));
            PipeWriter writer = pipe.Writer;

            for (int i = 0; i < bytes.Length; i++)
            {
                writer.GetSpan()[0] = bytes[i];
                writer.Advance(1);
            }

            await writer.FlushAsync();
            writer.Complete();
            Assert.Equal(0, writer.UnflushedBytes);
            ReadResult readResult = await pipe.Reader.ReadAsync();
            Assert.Equal(bytes, readResult.Buffer.ToArray());
            pipe.Reader.AdvanceTo(readResult.Buffer.End);

            pipe.Reader.Complete();
        }

        [Fact]
        public async Task WritesUsingGetMemoryWorks()
        {
            byte[] bytes = "abcdefghijklmnopqrstuvwzyz"u8.ToArray();
            var pipe = new Pipe(new PipeOptions(pool: new HeapBufferPool(), minimumSegmentSize: 1));
            PipeWriter writer = pipe.Writer;

            for (int i = 0; i < bytes.Length; i++)
            {
                writer.GetMemory().Span[0] = bytes[i];
                writer.Advance(1);
            }

            await writer.FlushAsync();
            writer.Complete();
            Assert.Equal(0, writer.UnflushedBytes);
            ReadResult readResult = await pipe.Reader.ReadAsync();
            Assert.Equal(bytes, readResult.Buffer.ToArray());
            pipe.Reader.AdvanceTo(readResult.Buffer.End);

            pipe.Reader.Complete();
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/91547", typeof(PlatformDetection), nameof(PlatformDetection.IsWasmThreadingSupported))]
        public async Task CompleteWithLargeWriteThrows()
        {
            var completeDelay = TimeSpan.FromMilliseconds(10);
            var testTimeout = TimeSpan.FromMilliseconds(10000);
            var pipe = new Pipe();
            pipe.Reader.Complete();

            var task = Task.Run(async () =>
            {
                await Task.Delay(completeDelay);
                pipe.Writer.Complete();
            });

            // Complete while writing
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                var testStartTime = DateTime.UtcNow;
                var buffer = new byte[10000000];
                ulong i = 0;
                while (true)
                {
                    await pipe.Writer.WriteAsync(buffer);

                    // abort test if we're executing for more than the testTimeout (check every 10000th iteration)
                    if (i++ % 10000 == 0 && DateTime.UtcNow - testStartTime > testTimeout)
                        break;
                }
            });
        }

        [Fact]
        public async Task WriteAsyncWithACompletedReaderNoops()
        {
            var pool = new DisposeTrackingBufferPool();
            var pipe = new Pipe(new PipeOptions(pool));
            pipe.Reader.Complete();

            byte[] writeBuffer = new byte[100];
            for (var i = 0; i < 10000; i++)
            {
                await pipe.Writer.WriteAsync(writeBuffer);
            }

            Assert.Equal(0, pool.CurrentlyRentedBlocks);
        }

        [Fact]
        public async Task GetMemoryFlushWithACompletedReaderNoops()
        {
            var pool = new DisposeTrackingBufferPool();
            var pipe = new Pipe(new PipeOptions(pool));
            pipe.Reader.Complete();

            for (var i = 0; i < 10000; i++)
            {
                var mem = pipe.Writer.GetMemory();
                pipe.Writer.Advance(mem.Length);
                await pipe.Writer.FlushAsync(default);
            }

            Assert.Equal(1, pool.CurrentlyRentedBlocks);
            pipe.Writer.Complete();
            Assert.Equal(0, pool.CurrentlyRentedBlocks);
            Assert.Equal(0, Pipe.Writer.UnflushedBytes);
        }
    }
}
