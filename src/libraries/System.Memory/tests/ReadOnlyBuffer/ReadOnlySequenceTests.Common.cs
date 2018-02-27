// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.MemoryTests;
using System.Text;
using Xunit;

namespace System.Memory.Tests
{
    public class CommonReadOnlySequenceTests
    {
        [Fact]
        public void SegmentStartIsConsideredInBoundsCheck()
        {
            // 0               50           100    0             50             100
            // [                ##############] -> [##############                ]
            //                         ^c1            ^c2
            var bufferSegment1 = new BufferSegment<byte>(new byte[49]);
            BufferSegment<byte> bufferSegment2 = bufferSegment1.Append(new byte[50]);

            var buffer = new ReadOnlySequence<byte>(bufferSegment1, 0, bufferSegment2, 50);

            SequencePosition c1 = buffer.GetPosition(buffer.Start, 25); // segment 1 index 75
            SequencePosition c2 = buffer.GetPosition(buffer.Start, 55); // segment 2 index 5

            ReadOnlySequence<byte> sliced = buffer.Slice(c1, c2);
            Assert.Equal(30, sliced.Length);
        }

        [Fact]
        public void GetPositionPrefersNextSegment()
        {
            BufferSegment<byte> bufferSegment1 = new BufferSegment<byte>(new byte[50]);
            BufferSegment<byte> bufferSegment2 = bufferSegment1.Append(new byte[0]);

            ReadOnlySequence<byte> buffer = new ReadOnlySequence<byte>(bufferSegment1, 0, bufferSegment2, 0);

            SequencePosition c1 = buffer.GetPosition(buffer.Start, 50);

            Assert.Equal(0, c1.GetInteger());
            Assert.Equal(bufferSegment2, c1.GetObject());
        }

        [Fact]
        public void GetPositionDoesNotCrossOutsideBuffer()
        {
            var bufferSegment1 = new BufferSegment<byte>(new byte[100]);
            BufferSegment<byte> bufferSegment2 = bufferSegment1.Append(new byte[100]);
            BufferSegment<byte> bufferSegment3 = bufferSegment2.Append(new byte[0]);

            var buffer = new ReadOnlySequence<byte>(bufferSegment1, 0, bufferSegment2, 100);

            SequencePosition c1 = buffer.GetPosition(buffer.Start, 200);

            Assert.Equal(100, c1.GetInteger());
            Assert.Equal(bufferSegment2, c1.GetObject());
        }

        [Fact]
        public void Create_WorksWithArray()
        {
            var buffer = new ReadOnlySequence<byte>(new byte[] { 1, 2, 3, 4, 5 });
            Assert.Equal(buffer.ToArray(), new byte[] {  1, 2, 3, 4, 5 });
        }

        [Fact]
        public void Empty_ReturnsLengthZeroBuffer()
        {
            var buffer = ReadOnlySequence<byte>.Empty;
            Assert.Equal(0, buffer.Length);
            Assert.Equal(true, buffer.IsSingleSegment);
            Assert.Equal(0, buffer.First.Length);
        }

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
        public void Ctor_Array_ValidatesArguments()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new ReadOnlySequence<byte>(new byte[] { 1, 2, 3, 4, 5 }, 6, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new ReadOnlySequence<byte>(new byte[] { 1, 2, 3, 4, 5 }, 4, 2));
            Assert.Throws<ArgumentOutOfRangeException>(() => new ReadOnlySequence<byte>(new byte[] { 1, 2, 3, 4, 5 }, -4, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new ReadOnlySequence<byte>(new byte[] { 1, 2, 3, 4, 5 }, 4, -2));
            Assert.Throws<ArgumentNullException>(() => new ReadOnlySequence<byte>((byte[])null, 4, 2));
        }

        [Fact]
        public void Ctor_OwnedMemory_Offset()
        {
            var ownedMemory = new CustomMemoryForTest<byte>(new byte[] { 1, 2, 3, 4, 5 }, 0, 5);
            var buffer = new ReadOnlySequence<byte>(ownedMemory, 2, 3);
            Assert.Equal(buffer.ToArray(), new byte[] { 3, 4, 5 });
        }

        [Fact]
        public void Ctor_OwnedMemory_NoOffset()
        {
            var ownedMemory = new CustomMemoryForTest<byte>(new byte[] { 1, 2, 3, 4, 5 }, 0, 5);
            var buffer = new ReadOnlySequence<byte>(ownedMemory);
            Assert.Equal(buffer.ToArray(), new byte[] { 1, 2, 3, 4, 5 });
        }

        [Fact]
        public void Ctor_OwnedMemory_ValidatesArguments()
        {
            var ownedMemory = new CustomMemoryForTest<byte>(new byte[] { 1, 2, 3, 4, 5 }, 0, 5);
            Assert.Throws<ArgumentOutOfRangeException>(() => new ReadOnlySequence<byte>(ownedMemory, 6, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new ReadOnlySequence<byte>(ownedMemory, 4, 2));
            Assert.Throws<ArgumentOutOfRangeException>(() => new ReadOnlySequence<byte>(ownedMemory, -4, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new ReadOnlySequence<byte>(ownedMemory, 4, -2));
            Assert.Throws<ArgumentNullException>(() => new ReadOnlySequence<byte>((CustomMemoryForTest<byte>)null, 4, 2));
        }

        [Fact]
        public void Ctor_Memory()
        {
            var memory = new ReadOnlyMemory<byte>(new byte[] { 1, 2, 3, 4, 5 });
            var buffer = new ReadOnlySequence<byte>(memory.Slice(2, 3));
            Assert.Equal(new byte[] { 3, 4, 5 }, buffer.ToArray());
        }

        [Fact]
        public void Ctor_MemoryList_ValidatesArguments()
        {
            var segment = new BufferSegment<byte>(new byte[] { 1, 2, 3, 4, 5 });
            Assert.Throws<ArgumentOutOfRangeException>(() => new ReadOnlySequence<byte>(segment, 6, segment, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new ReadOnlySequence<byte>(segment, 0, segment, 6));
            Assert.Throws<ArgumentOutOfRangeException>(() => new ReadOnlySequence<byte>(segment, 3, segment, 0));

            Assert.Throws<ArgumentOutOfRangeException>(() => new ReadOnlySequence<byte>(segment, -5, segment, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new ReadOnlySequence<byte>(segment, 0, segment, -5));

            Assert.Throws<ArgumentNullException>(() => new ReadOnlySequence<byte>(null, 5, segment, 0));
            Assert.Throws<ArgumentNullException>(() => new ReadOnlySequence<byte>(segment, 5, null, 0));
        } 

        [Fact]
        public void HelloWorldAcrossTwoBlocks()
        {
            //     block 1       ->    block2
            // [padding..hello]  ->  [  world   ]
            const int blockSize = 4096;

            byte[] bytes = Encoding.ASCII.GetBytes("Hello World");
            byte[] firstBytes = Enumerable.Repeat((byte)'a', blockSize - 5).Concat(bytes.Take(5)).ToArray();
            byte[] secondBytes = bytes.Skip(5).Concat( Enumerable.Repeat((byte)'a', blockSize - (bytes.Length - 5))).ToArray();

            BufferSegment<byte> firstSegement = new BufferSegment<byte>(firstBytes);
            BufferSegment<byte> secondSegement = firstSegement.Append(secondBytes);

            ReadOnlySequence<byte> buffer = new ReadOnlySequence<byte>(firstSegement, 0, secondSegement, bytes.Length - 5);
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
    }
}
