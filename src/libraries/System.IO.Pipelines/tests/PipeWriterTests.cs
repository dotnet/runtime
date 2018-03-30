// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Buffers;
using System.Collections.Generic;
using System.Linq;
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
                Assert.True(false);
            }
            catch (Exception ex)
            {
                Assert.True(ex is ArgumentOutOfRangeException);
            }

            writer.Write(new Span<byte>(array, 0, array.Length));
            Assert.Equal(array, Read());
        }

        [Theory]
        [InlineData(0, 0, 3)]
        [InlineData(0, 1, 2)]
        [InlineData(0, 2, 1)]
        [InlineData(0, 1, 1)]
        [InlineData(1, 0, 3)]
        [InlineData(1, 1, 2)]
        [InlineData(1, 2, 1)]
        [InlineData(1, 1, 1)]
        public void CanWriteWithOffsetAndLenght(int alloc, int offset, int length)
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
        public void CanWriteOverTheBlockLength()
        {
            Memory<byte> memory = Pipe.Writer.GetMemory();
            PipeWriter writer = Pipe.Writer;

            IEnumerable<byte> source = Enumerable.Range(0, memory.Length).Select(i => (byte)i);
            byte[] expectedBytes = source.Concat(source).Concat(source).ToArray();

            writer.Write(expectedBytes);

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
            output.Write(data);
            await output.FlushAsync();

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
        public void EmptyWriteDoesNotThrow()
        {
            Pipe.Writer.Write(new byte[0]);
        }

        [Fact]
        public void ThrowsOnAdvanceOverMemorySize()
        {
            Memory<byte> buffer = Pipe.Writer.GetMemory(1);
            var exception = Assert.Throws<InvalidOperationException>(() => Pipe.Writer.Advance(buffer.Length + 1));
            Assert.Equal("Can't advance past buffer size.", exception.Message);
        }

        [Fact]
        public void ThrowsOnAdvanceWithNoMemory()
        {
            PipeWriter buffer = Pipe.Writer;
            var exception = Assert.Throws<InvalidOperationException>(() => buffer.Advance(1));
            Assert.Equal("No writing operation. Make sure GetMemory() was called.", exception.Message);
        }

        [Fact]
        public void GetMemory_AdjustsToPoolMaxBufferSize()
        {
            PipeWriter buffer = Pipe.Writer;
            var memory = buffer.GetMemory(int.MaxValue);
            Assert.True(memory.Length >= 4096);
        }
    }
}
