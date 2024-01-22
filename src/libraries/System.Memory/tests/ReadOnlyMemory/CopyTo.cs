// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.MemoryTests
{
    public static partial class ReadOnlyMemoryTests
    {
        [Fact]
        public static void TryCopyTo()
        {
            int[] src = { 1, 2, 3 };
            int[] dst = { 99, 100, 101 };

            ReadOnlyMemory<int> srcMemory = src;
            bool success = srcMemory.TryCopyTo(dst);
            Assert.True(success);
            Assert.Equal<int>(src, dst);
        }

        [Fact]
        public static void TryCopyToSingle()
        {
            int[] src = { 1 };
            int[] dst = { 99 };

            ReadOnlyMemory<int> srcMemory = src;
            bool success = srcMemory.TryCopyTo(dst);
            Assert.True(success);
            Assert.Equal<int>(src, dst);
        }

        [Fact]
        public static void TryCopyToArraySegmentImplicit()
        {
            int[] src = { 1, 2, 3 };
            int[] dst = { 5, 99, 100, 101, 10 };
            var segment = new ArraySegment<int>(dst, 1, 3);

            ReadOnlyMemory<int> srcMemory = src;
            bool success = srcMemory.TryCopyTo(segment);
            Assert.True(success);
            Assert.Equal<int>(src.AsSpan(), segment);
        }

        [Fact]
        public static void TryCopyToEmpty()
        {
            int[] src = { };
            int[] dst = { 99, 100, 101 };

            ReadOnlyMemory<int> srcMemory = src;
            bool success = srcMemory.TryCopyTo(dst);
            Assert.True(success);
            int[] expected = { 99, 100, 101 };
            Assert.Equal<int>(expected, dst);
        }

        [Fact]
        public static void TryCopyToLonger()
        {
            int[] src = { 1, 2, 3 };
            int[] dst = { 99, 100, 101, 102 };

            ReadOnlyMemory<int> srcMemory = src;
            bool success = srcMemory.TryCopyTo(dst);
            Assert.True(success);
            int[] expected = { 1, 2, 3, 102 };
            Assert.Equal<int>(expected, dst);
        }

        [Fact]
        public static void TryCopyToShorter()
        {
            int[] src = { 1, 2, 3 };
            int[] dst = { 99, 100 };

            ReadOnlyMemory<int> srcMemory = src;
            bool success = srcMemory.TryCopyTo(dst);
            Assert.False(success);
            int[] expected = { 99, 100 };
            Assert.Equal<int>(expected, dst);  // TryCopyTo() checks for sufficient space before doing any copying.
        }

        [Fact]
        public static void CopyToShorter()
        {
            int[] src = { 1, 2, 3 };
            int[] dst = { 99, 100 };

            ReadOnlyMemory<int> srcMemory = src;
            Assert.Throws<ArgumentException>(() => srcMemory.CopyTo(dst));
            int[] expected = { 99, 100 };
            Assert.Equal<int>(expected, dst);  // CopyTo() checks for sufficient space before doing any copying.
        }

        [Fact]
        public static void Overlapping1()
        {
            int[] a = { 90, 91, 92, 93, 94, 95, 96, 97 };

            var src = new ReadOnlyMemory<int>(a, 1, 6);
            var dst = new Memory<int>(a, 2, 6);
            src.CopyTo(dst);

            int[] expected = { 90, 91, 91, 92, 93, 94, 95, 96 };
            Assert.Equal<int>(expected, a);
        }

        [Fact]
        public static void Overlapping2()
        {
            int[] a = { 90, 91, 92, 93, 94, 95, 96, 97 };

            var src = new ReadOnlyMemory<int>(a, 2, 6);
            var dst = new Memory<int>(a, 1, 6);
            src.CopyTo(dst);

            int[] expected = { 90, 92, 93, 94, 95, 96, 97, 97 };
            Assert.Equal<int>(expected, a);
        }
    }
}
