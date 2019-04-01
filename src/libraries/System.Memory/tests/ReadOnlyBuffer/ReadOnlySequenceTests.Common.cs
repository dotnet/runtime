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
    public abstract class ReadOnlySequenceTestsCommon<T>
    {
        #region Position

        [Fact]
        public void SegmentStartIsConsideredInBoundsCheck()
        {
            // 0               50           100    0             50             100
            // [                ##############] -> [##############                ]
            //                         ^c1            ^c2
            var bufferSegment1 = new BufferSegment<T>(new T[49]);
            BufferSegment<T> bufferSegment2 = bufferSegment1.Append(new T[50]);

            var buffer = new ReadOnlySequence<T>(bufferSegment1, 0, bufferSegment2, 50);

            SequencePosition c1 = buffer.GetPosition(25); // segment 1 index 75
            SequencePosition c2 = buffer.GetPosition(55); // segment 2 index 5

            ReadOnlySequence<T> sliced = buffer.Slice(c1, c2);
            Assert.Equal(30, sliced.Length);

            c1 = buffer.GetPosition(25, buffer.Start); // segment 1 index 75
            c2 = buffer.GetPosition(55, buffer.Start); // segment 2 index 5

            sliced = buffer.Slice(c1, c2);
            Assert.Equal(30, sliced.Length);
        }

        [Fact]
        public void GetPositionPrefersNextSegment()
        {
            BufferSegment<T> bufferSegment1 = new BufferSegment<T>(new T[50]);
            BufferSegment<T> bufferSegment2 = bufferSegment1.Append(new T[0]);

            ReadOnlySequence<T> buffer = new ReadOnlySequence<T>(bufferSegment1, 0, bufferSegment2, 0);

            SequencePosition c1 = buffer.GetPosition(50);

            Assert.Equal(0, c1.GetInteger());
            Assert.Equal(bufferSegment2, c1.GetObject());

            c1 = buffer.GetPosition(50, buffer.Start);

            Assert.Equal(0, c1.GetInteger());
            Assert.Equal(bufferSegment2, c1.GetObject());
        }

        [Fact]
        public void GetPositionDoesNotCrossOutsideBuffer()
        {
            var bufferSegment1 = new BufferSegment<T>(new T[100]);
            BufferSegment<T> bufferSegment2 = bufferSegment1.Append(new T[100]);
            BufferSegment<T> bufferSegment3 = bufferSegment2.Append(new T[0]);

            var buffer = new ReadOnlySequence<T>(bufferSegment1, 0, bufferSegment2, 100);

            SequencePosition c1 = buffer.GetPosition(200);

            Assert.Equal(100, c1.GetInteger());
            Assert.Equal(bufferSegment2, c1.GetObject());

            c1 = buffer.GetPosition(200, buffer.Start);

            Assert.Equal(100, c1.GetInteger());
            Assert.Equal(bufferSegment2, c1.GetObject());
        }

        [Fact]
        public void CheckEndReachableDoesNotCrossPastEnd()
        {
            var bufferSegment1 = new BufferSegment<T>(new T[100]);
            BufferSegment<T> bufferSegment2 = bufferSegment1.Append(new T[100]);
            BufferSegment<T> bufferSegment3 = bufferSegment2.Append(new T[100]);
            BufferSegment<T> bufferSegment4 = bufferSegment3.Append(new T[100]);

            var buffer = new ReadOnlySequence<T>(bufferSegment1, 0, bufferSegment4, 100);

            SequencePosition c1 = buffer.GetPosition(200);

            Assert.Equal(0, c1.GetInteger());
            Assert.Equal(bufferSegment3, c1.GetObject());

            ReadOnlySequence<T> seq = buffer.Slice(0, c1);
            Assert.Equal(200, seq.Length);

            c1 = buffer.GetPosition(200, buffer.Start);

            Assert.Equal(0, c1.GetInteger());
            Assert.Equal(bufferSegment3, c1.GetObject());

            seq = buffer.Slice(0, c1);
            Assert.Equal(200, seq.Length);
        }

        #endregion

        #region First

        [Fact]
        public void AsArray_CanGetFirst()
        {
            var memory = new ReadOnlyMemory<T>(new T[5]);
            VerifyCanGetFirst(new ReadOnlySequence<T>(memory), expectedSize: 5);
        }

        [Fact]
        public void AsMemoryManager_CanGetFirst()
        {
            MemoryManager<T> manager = new CustomMemoryForTest<T>(new T[5]);
            ReadOnlyMemory<T> memoryFromManager = ((ReadOnlyMemory<T>)manager.Memory);

            VerifyCanGetFirst(new ReadOnlySequence<T>(memoryFromManager), expectedSize: 5);
        }

        [Fact]
        public void AsMultiSegment_CanGetFirst()
        {
            var bufferSegment1 = new BufferSegment<T>(new T[100]);
            BufferSegment<T> bufferSegment2 = bufferSegment1.Append(new T[100]);
            BufferSegment<T> bufferSegment3 = bufferSegment2.Append(new T[100]);
            BufferSegment<T> bufferSegment4 = bufferSegment3.Append(new T[200]);

            var buffer = new ReadOnlySequence<T>(bufferSegment1, 0, bufferSegment4, 200);
            // Verify first 3 segments
            Assert.Equal(500, buffer.Length);
            int length = 500;
            for (int s = 0; s < 3; s++)
            {
                for (int i = 100; i > 0; i--)
                {
                    Assert.Equal(i, buffer.First.Length);
                    Assert.Equal(i, buffer.FirstSpan.Length);
                    buffer = buffer.Slice(1);
                    length--;
                    Assert.Equal(length, buffer.Length);
                }
            }
            // Verify last segment
            VerifyCanGetFirst(buffer, expectedSize: 200);
        }
        
        protected void VerifyCanGetFirst(ReadOnlySequence<T> buffer, int expectedSize)
        {
            Assert.Equal(expectedSize, buffer.Length);
            int length = expectedSize;

            for (int i = length; i > 0; i--)
            {
                Assert.Equal(i, buffer.First.Length);
                Assert.Equal(i, buffer.FirstSpan.Length);
                buffer = buffer.Slice(1);
                length--;
                Assert.Equal(length, buffer.Length);
            }

            Assert.Equal(0, buffer.Length);
            Assert.Equal(0, buffer.First.Length);
            Assert.Equal(0, buffer.FirstSpan.Length);
        }

        #endregion

        #region EmptySegments

        [Fact]
        public void SeekSkipsEmptySegments()
        {
            var bufferSegment1 = new BufferSegment<T>(new T[100]);
            BufferSegment<T> bufferSegment2 = bufferSegment1.Append(new T[0]);
            BufferSegment<T> bufferSegment3 = bufferSegment2.Append(new T[0]);
            BufferSegment<T> bufferSegment4 = bufferSegment3.Append(new T[100]);

            var buffer = new ReadOnlySequence<T>(bufferSegment1, 0, bufferSegment4, 100);

            SequencePosition c1 = buffer.GetPosition(100);

            Assert.Equal(0, c1.GetInteger());
            Assert.Equal(bufferSegment4, c1.GetObject());

            c1 = buffer.GetPosition(100, buffer.Start);

            Assert.Equal(0, c1.GetInteger());
            Assert.Equal(bufferSegment4, c1.GetObject());
        }

        [Fact]
        public void TryGetReturnsEmptySegments()
        {
            var bufferSegment1 = new BufferSegment<T>(new T[0]);
            BufferSegment<T> bufferSegment2 = bufferSegment1.Append(new T[0]);
            BufferSegment<T> bufferSegment3 = bufferSegment2.Append(new T[0]);
            BufferSegment<T> bufferSegment4 = bufferSegment3.Append(new T[0]);

            var buffer = new ReadOnlySequence<T>(bufferSegment1, 0, bufferSegment3, 0);

            var start = buffer.Start;
            Assert.True(buffer.TryGet(ref start, out var memory));
            Assert.Equal(0, memory.Length);
            Assert.True(buffer.TryGet(ref start, out memory));
            Assert.Equal(0, memory.Length);
            Assert.True(buffer.TryGet(ref start, out memory));
            Assert.Equal(0, memory.Length);
            Assert.False(buffer.TryGet(ref start, out memory));
        }

        [Fact]
        public void SeekEmptySkipDoesNotCrossPastEnd()
        {
            var bufferSegment1 = new BufferSegment<T>(new T[100]);
            BufferSegment<T> bufferSegment2 = bufferSegment1.Append(new T[0]);
            BufferSegment<T> bufferSegment3 = bufferSegment2.Append(new T[0]);
            BufferSegment<T> bufferSegment4 = bufferSegment3.Append(new T[100]);

            var buffer = new ReadOnlySequence<T>(bufferSegment1, 0, bufferSegment2, 0);

            SequencePosition c1 = buffer.GetPosition(100);

            Assert.Equal(0, c1.GetInteger());
            Assert.Equal(bufferSegment2, c1.GetObject());

            c1 = buffer.GetPosition(100, buffer.Start);

            Assert.Equal(0, c1.GetInteger());
            Assert.Equal(bufferSegment2, c1.GetObject());

            // Go out of bounds for segment
            Assert.Throws<ArgumentOutOfRangeException>(() => c1 = buffer.GetPosition(150, buffer.Start));
            Assert.Throws<ArgumentOutOfRangeException>(() => c1 = buffer.GetPosition(250, buffer.Start));
        }

        [Fact]
        public void SeekEmptySkipDoesNotCrossPastEndWithExtraChainedBlocks()
        {
            var bufferSegment1 = new BufferSegment<T>(new T[100]);
            BufferSegment<T> bufferSegment2 = bufferSegment1.Append(new T[0]);
            BufferSegment<T> bufferSegment3 = bufferSegment2.Append(new T[0]);
            BufferSegment<T> bufferSegment4 = bufferSegment3.Append(new T[100]);
            BufferSegment<T> bufferSegment5 = bufferSegment4.Append(new T[0]);
            BufferSegment<T> bufferSegment6 = bufferSegment5.Append(new T[100]);

            var buffer = new ReadOnlySequence<T>(bufferSegment1, 0, bufferSegment2, 0);

            SequencePosition c1 = buffer.GetPosition(100);

            Assert.Equal(0, c1.GetInteger());
            Assert.Equal(bufferSegment2, c1.GetObject());

            c1 = buffer.GetPosition(100, buffer.Start);

            Assert.Equal(0, c1.GetInteger());
            Assert.Equal(bufferSegment2, c1.GetObject());

            // Go out of bounds for segment
            Assert.Throws<ArgumentOutOfRangeException>(() => c1 = buffer.GetPosition(150, buffer.Start));
            Assert.Throws<ArgumentOutOfRangeException>(() => c1 = buffer.GetPosition(250, buffer.Start));
        }

        #endregion

        #region TryGet

        [Fact]
        public void TryGetStopsAtEnd()
        {
            var bufferSegment1 = new BufferSegment<T>(new T[100]);
            BufferSegment<T> bufferSegment2 = bufferSegment1.Append(new T[100]);
            BufferSegment<T> bufferSegment3 = bufferSegment2.Append(new T[100]);
            BufferSegment<T> bufferSegment4 = bufferSegment3.Append(new T[100]);
            BufferSegment<T> bufferSegment5 = bufferSegment4.Append(new T[100]);

            var buffer = new ReadOnlySequence<T>(bufferSegment1, 0, bufferSegment3, 100);

            var start = buffer.Start;
            Assert.True(buffer.TryGet(ref start, out var memory));
            Assert.Equal(100, memory.Length);
            Assert.True(buffer.TryGet(ref start, out memory));
            Assert.Equal(100, memory.Length);
            Assert.True(buffer.TryGet(ref start, out memory));
            Assert.Equal(100, memory.Length);
            Assert.False(buffer.TryGet(ref start, out memory));
        }

        [Fact]
        public void TryGetStopsAtEndWhenEndIsLastItemOfFull()
        {
            var bufferSegment1 = new BufferSegment<T>(new T[100]);
            BufferSegment<T> bufferSegment2 = bufferSegment1.Append(new T[100]);

            var buffer = new ReadOnlySequence<T>(bufferSegment1, 0, bufferSegment1, 100);

            SequencePosition start = buffer.Start;
            Assert.True(buffer.TryGet(ref start, out ReadOnlyMemory<T> memory));
            Assert.Equal(100, memory.Length);
            Assert.False(buffer.TryGet(ref start, out memory));
        }

        [Fact]
        public void TryGetStopsAtEndWhenEndIsFirstItemOfFull()
        {
            var bufferSegment1 = new BufferSegment<T>(new T[100]);
            BufferSegment<T> bufferSegment2 = bufferSegment1.Append(new T[100]);

            var buffer = new ReadOnlySequence<T>(bufferSegment1, 0, bufferSegment2, 0);

            SequencePosition start = buffer.Start;
            Assert.True(buffer.TryGet(ref start, out ReadOnlyMemory<T> memory));
            Assert.Equal(100, memory.Length);
            Assert.True(buffer.TryGet(ref start, out memory));
            Assert.Equal(0, memory.Length);
            Assert.False(buffer.TryGet(ref start, out memory));
        }

        [Fact]
        public void TryGetStopsAtEndWhenEndIsFirstItemOfEmpty()
        {
            var bufferSegment1 = new BufferSegment<T>(new T[100]);
            BufferSegment<T> bufferSegment2 = bufferSegment1.Append(new T[0]);

            var buffer = new ReadOnlySequence<T>(bufferSegment1, 0, bufferSegment2, 0);

            SequencePosition start = buffer.Start;
            Assert.True(buffer.TryGet(ref start, out ReadOnlyMemory<T> memory));
            Assert.Equal(100, memory.Length);
            Assert.True(buffer.TryGet(ref start, out memory));
            Assert.Equal(0, memory.Length);
            Assert.False(buffer.TryGet(ref start, out memory));
        }

        #endregion

        #region Enumerable

        [Fact]
        public void EnumerableStopsAtEndWhenEndIsLastItemOfFull()
        {
            var bufferSegment1 = new BufferSegment<T>(new T[100]);
            BufferSegment<T> bufferSegment2 = bufferSegment1.Append(new T[100]);

            var buffer = new ReadOnlySequence<T>(bufferSegment1, 0, bufferSegment1, 100);

            List<int> sizes = new List<int>();
            foreach (ReadOnlyMemory<T> memory in buffer)
            {
                sizes.Add(memory.Length);
            }

            Assert.Equal(1, sizes.Count);
            Assert.Equal(new[] { 100 }, sizes);
        }

        [Fact]
        public void EnumerableStopsAtEndWhenEndIsFirstItemOfFull()
        {
            var bufferSegment1 = new BufferSegment<T>(new T[100]);
            BufferSegment<T> bufferSegment2 = bufferSegment1.Append(new T[100]);

            var buffer = new ReadOnlySequence<T>(bufferSegment1, 0, bufferSegment2, 0);

            List<int> sizes = new List<int>();
            foreach (ReadOnlyMemory<T> memory in buffer)
            {
                sizes.Add(memory.Length);
            }

            Assert.Equal(2, sizes.Count);
            Assert.Equal(new[] { 100, 0 }, sizes);
        }

        [Fact]
        public void EnumerableStopsAtEndWhenEndIsFirstItemOfEmpty()
        {
            var bufferSegment1 = new BufferSegment<T>(new T[100]);
            BufferSegment<T> bufferSegment2 = bufferSegment1.Append(new T[0]);

            var buffer = new ReadOnlySequence<T>(bufferSegment1, 0, bufferSegment2, 0);

            List<int> sizes = new List<int>();
            foreach (ReadOnlyMemory<T> memory in buffer)
            {
                sizes.Add(memory.Length);
            }

            Assert.Equal(2, sizes.Count);
            Assert.Equal(new[] { 100, 0 }, sizes);
        }

        #endregion

        #region Constructor 

        [Fact]
        public void Ctor_Array_ValidatesArguments()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new ReadOnlySequence<T>(new T[5], 6, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new ReadOnlySequence<T>(new T[5], 4, 2));
            Assert.Throws<ArgumentOutOfRangeException>(() => new ReadOnlySequence<T>(new T[5], -4, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new ReadOnlySequence<T>(new T[5], 4, -2));
            Assert.Throws<ArgumentNullException>(() => new ReadOnlySequence<T>(null, 4, 2));
        }

        [Fact]
        public void Ctor_SingleSegment_ValidatesArguments()
        {
            var segment = new BufferSegment<T>(new T[5]);
            
            Assert.Throws<ArgumentNullException>(() => new ReadOnlySequence<T>(null, 2, segment, 3));
            Assert.Throws<ArgumentNullException>(() => new ReadOnlySequence<T>(segment, 2, null, 3));
            Assert.Throws<ArgumentOutOfRangeException>(() => new ReadOnlySequence<T>(segment, 6, segment, 3));
            Assert.Throws<ArgumentOutOfRangeException>(() => new ReadOnlySequence<T>(segment, 2, segment, 6));
            Assert.Throws<ArgumentOutOfRangeException>(() => new ReadOnlySequence<T>(segment, -1, segment, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new ReadOnlySequence<T>(segment, 0, segment, -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new ReadOnlySequence<T>(segment, 3, segment, 2));
        }

        [Fact]
        public void Ctor_MultiSegments_ValidatesArguments()
        {
            var segment1 = new BufferSegment<T>(new T[5]);
            BufferSegment<T> segment2 = segment1.Append(new T[5]);

            Assert.Throws<ArgumentNullException>(() => new ReadOnlySequence<T>(null, 5, segment2, 3));
            Assert.Throws<ArgumentNullException>(() => new ReadOnlySequence<T>(segment1, 2, null, 3));
            Assert.Throws<ArgumentOutOfRangeException>(() => new ReadOnlySequence<T>(segment1, 6, segment2, 3));
            Assert.Throws<ArgumentOutOfRangeException>(() => new ReadOnlySequence<T>(segment1, 2, segment2, 6));
            Assert.Throws<ArgumentOutOfRangeException>(() => new ReadOnlySequence<T>(segment1, -1, segment2, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new ReadOnlySequence<T>(segment1, 0, segment2, -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new ReadOnlySequence<T>(segment2, 2, segment1, 3));
        }

        #endregion

    }
}
