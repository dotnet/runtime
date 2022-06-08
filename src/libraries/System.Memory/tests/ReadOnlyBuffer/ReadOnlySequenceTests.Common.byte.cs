// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;

namespace System.Memory.Tests
{
    public class ReadOnlySequenceTestsCommonByte: ReadOnlySequenceTestsCommon<byte>
    {
        #region Constructor

        [Fact]
        public void Ctor_Array_Offset()
        {
            var buffer = new ReadOnlySequence<byte>(new byte[] { 1, 2, 3, 4, 5 }, 2, 3);
            Assert.Equal(buffer.ToArray(), new byte[] { 3, 4, 5 });
        }

        [Fact]
        public void Ctor_Array_NoOffset()
        {
            var buffer = new ReadOnlySequence<byte>(new byte[] { 1, 2, 3, 4, 5 });
            Assert.Equal(buffer.ToArray(), new byte[] { 1, 2, 3, 4, 5 });
        }

        [Fact]
        public void Ctor_Memory()
        {
            var memory = new ReadOnlyMemory<byte>(new byte[] { 1, 2, 3, 4, 5 });
            var buffer = new ReadOnlySequence<byte>(memory.Slice(2, 3));
            Assert.Equal(new byte[] { 3, 4, 5 }, buffer.ToArray());
        }

        #endregion

        [Fact]
        public void HelloWorldAcrossTwoBlocks()
        {
            //     block 1       ->    block2
            // [padding..hello]  ->  [  world   ]
            const int blockSize = 4096;

            byte[] items = "Hello World"u8.ToArray();
            byte[] firstItems = Enumerable.Repeat((byte)'a', blockSize - 5).Concat(items.Take(5)).ToArray();
            byte[] secondItems = items.Skip(5).Concat(Enumerable.Repeat((byte)'a', blockSize - (items.Length - 5))).ToArray();

            var firstSegment = new BufferSegment<byte>(firstItems);
            BufferSegment<byte> secondSegment = firstSegment.Append(secondItems);

            var buffer = new ReadOnlySequence<byte>(firstSegment, 0, secondSegment, items.Length - 5);
            Assert.False(buffer.IsSingleSegment);
            ReadOnlySequence<byte> helloBuffer = buffer.Slice(blockSize - 5);
            Assert.False(helloBuffer.IsSingleSegment);
            var memory = new List<ReadOnlyMemory<byte>>();
            foreach (ReadOnlyMemory<byte> m in helloBuffer)
            {
                memory.Add(m);
            }

            List<ReadOnlyMemory<byte>> spans = memory;

            Assert.Equal(2, memory.Count);
            var helloBytes = new byte[spans[0].Length];
            spans[0].Span.CopyTo(helloBytes);
            var worldBytes = new byte[spans[1].Length];
            spans[1].Span.CopyTo(worldBytes);
            Assert.Equal("Hello", Encoding.ASCII.GetString(helloBytes));
            Assert.Equal(" World", Encoding.ASCII.GetString(worldBytes));
        }

        [Fact]
        public static void SliceStartPositionAndLength()
        {
            var segment1 = new BufferSegment<byte>(new byte[10]);
            BufferSegment<byte> segment2 = segment1.Append(new byte[10]);

            var buffer = new ReadOnlySequence<byte>(segment1, 0, segment2, 10);

            ReadOnlySequence<byte> sliced = buffer.Slice(buffer.GetPosition(10), 10);
            Assert.Equal(10, sliced.Length);

            Assert.Equal(segment2, sliced.Start.GetObject());
            Assert.Equal(segment2, sliced.End.GetObject());

            Assert.Equal(0, sliced.Start.GetInteger());
            Assert.Equal(10, sliced.End.GetInteger());
        }

        [Fact]
        public static void SliceStartAndEndPosition()
        {
            var segment1 = new BufferSegment<byte>(new byte[10]);
            BufferSegment<byte> segment2 = segment1.Append(new byte[10]);

            var buffer = new ReadOnlySequence<byte>(segment1, 0, segment2, 10);

            ReadOnlySequence<byte> sliced = buffer.Slice(10, buffer.GetPosition(20));
            Assert.Equal(10, sliced.Length);

            Assert.Equal(segment2, sliced.Start.GetObject());
            Assert.Equal(segment2, sliced.End.GetObject());

            Assert.Equal(0, sliced.Start.GetInteger());
            Assert.Equal(10, sliced.End.GetInteger());
        }
    }
}
