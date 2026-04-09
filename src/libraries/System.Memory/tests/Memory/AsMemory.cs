// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.MemoryTests
{
    public static class AsMemory
    {
        [Theory]
        [InlineData(0, 0)]
        [InlineData(3, 0)]
        [InlineData(3, 1)]
        [InlineData(3, 2)]
        [InlineData(3, 3)]
        [InlineData(10, 0)]
        [InlineData(10, 3)]
        [InlineData(10, 10)]
        public static void ArrayAsMemoryWithStart(int length, int start)
        {
            int[] a = new int[length];
            Memory<int> m = a.AsMemory(start);
            Assert.Equal(length - start, m.Length);
            if (start != length)
            {
                m.Span[0] = 42;
                Assert.Equal(42, a[start]);
            }
        }

        [Theory]
        [InlineData(0, 0)]
        [InlineData(3, 0)]
        [InlineData(3, 1)]
        [InlineData(3, 2)]
        [InlineData(3, 3)]
        [InlineData(10, 0)]
        [InlineData(10, 3)]
        [InlineData(10, 10)]
        public static void ArraySegmentAsMemoryWithStart(int length, int start)
        {
            const int segmentOffset = 5;

            int[] a = new int[length + segmentOffset];
            ArraySegment<int> segment = new ArraySegment<int>(a, 5, length);
            Memory<int> m = segment.AsMemory(start);
            Assert.Equal(length - start, m.Length);
            if (m.Length != 0)
            {
                m.Span[0] = 42;
                Assert.Equal(42, a[segmentOffset + start]);
            }
        }

        [Theory]
        [InlineData(0, 0, 0)]
        [InlineData(3, 0, 3)]
        [InlineData(3, 1, 2)]
        [InlineData(3, 2, 1)]
        [InlineData(3, 3, 0)]
        [InlineData(10, 0, 5)]
        [InlineData(10, 3, 2)]
        public static void ArrayAsMemoryWithStartAndLength(int length, int start, int subLength)
        {
            int[] a = new int[length];
            Memory<int> m = a.AsMemory(start, subLength);
            Assert.Equal(subLength, m.Length);
            if (subLength != 0)
            {
                m.Span[0] = 42;
                Assert.Equal(42, a[start]);
            }
        }

        [Theory]
        [InlineData(0, 0, 0)]
        [InlineData(3, 0, 3)]
        [InlineData(3, 1, 2)]
        [InlineData(3, 2, 1)]
        [InlineData(3, 3, 0)]
        [InlineData(10, 0, 5)]
        [InlineData(10, 3, 2)]
        public static void ArraySegmentAsMemoryWithStartAndLength(int length, int start, int subLength)
        {
            const int segmentOffset = 5;

            int[] a = new int[length + segmentOffset];
            ArraySegment<int> segment = new ArraySegment<int>(a, segmentOffset, length);
            Memory<int> m = segment.AsMemory(start, subLength);
            Assert.Equal(subLength, m.Length);
            if (subLength != 0)
            {
                m.Span[0] = 42;
                Assert.Equal(42, a[segmentOffset + start]);
            }
        }

        [Theory]
        [InlineData(0, -1)]
        [InlineData(0, 1)]
        [InlineData(5, 6)]
        public static void ArrayAsMemoryWithStartNegative(int length, int start)
        {
            int[] a = new int[length];
            Assert.Throws<ArgumentOutOfRangeException>(() => a.AsMemory(start));
        }

        [Theory]
        [InlineData(0, -1, 0)]
        [InlineData(0, 1, 0)]
        [InlineData(0, 0, -1)]
        [InlineData(0, 0, 1)]
        [InlineData(5, 6, 0)]
        [InlineData(5, 3, 3)]
        public static void ArrayAsMemoryWithStartAndLengthNegative(int length, int start, int subLength)
        {
            int[] a = new int[length];
            Assert.Throws<ArgumentOutOfRangeException>(() => a.AsMemory(start, subLength));
        }
    }
}
