// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.MemoryTests
{
    public static partial class MemoryTests
    {
        [Fact]
        public static void TryCopyTo()
        {
            int[] src = { 1, 2, 3 };
            int[] dst = { 99, 100, 101 };

            Memory<int> srcMemory = src;
            bool success = srcMemory.TryCopyTo(dst);
            Assert.True(success);
            Assert.Equal<int>(src, dst);
        }

        [Fact]
        public static void TryCopyToSingle()
        {
            int[] src = { 1 };
            int[] dst = { 99 };

            Memory<int> srcMemory = src;
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

            Memory<int> srcMemory = src;
            bool success = srcMemory.TryCopyTo(segment);
            Assert.True(success);
            Assert.Equal(src, segment);
        }

        [Fact]
        public static void TryCopyToEmpty()
        {
            int[] src = { };
            int[] dst = { 99, 100, 101 };

            Memory<int> srcMemory = src;
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

            Memory<int> srcMemory = src;
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

            Memory<int> srcMemory = src;
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

            Memory<int> srcMemory = src;
            Assert.Throws<ArgumentException>(() => srcMemory.CopyTo(dst));
            int[] expected = { 99, 100 };
            Assert.Equal<int>(expected, dst);  // CopyTo() checks for sufficient space before doing any copying.
        }

        [Fact]
        public static void Overlapping1()
        {
            int[] a = { 90, 91, 92, 93, 94, 95, 96, 97 };

            var src = new Memory<int>(a, 1, 6);
            var dst = new Memory<int>(a, 2, 6);
            src.CopyTo(dst);

            int[] expected = { 90, 91, 91, 92, 93, 94, 95, 96 };
            Assert.Equal<int>(expected, a);
        }

        [Fact]
        public static void Overlapping2()
        {
            int[] a = { 90, 91, 92, 93, 94, 95, 96, 97 };

            var src = new Memory<int>(a, 2, 6);
            var dst = new Memory<int>(a, 1, 6);
            src.CopyTo(dst);

            int[] expected = { 90, 92, 93, 94, 95, 96, 97, 97 };
            Assert.Equal<int>(expected, a);
        }

        [Fact]
        public static void CopyToArray()
        {
            int[] src = { 1, 2, 3 };
            Memory<int> dst = new int[3] { 99, 100, 101 };

            src.CopyTo(dst);
            Assert.Equal<int>(src, dst.ToArray());
        }

        [Fact]
        public static void CopyToSingleArray()
        {
            int[] src = { 1 };
            Memory<int> dst = new int[1] { 99 };

            src.CopyTo(dst);
            Assert.Equal<int>(src, dst.ToArray());
        }

        [Fact]
        public static void CopyToEmptyArray()
        {
            int[] src = { };
            Memory<int> dst = new int[3] { 99, 100, 101 };

            src.CopyTo(dst);
            int[] expected = { 99, 100, 101 };
            Assert.Equal<int>(expected, dst.ToArray());

            Memory<int> dstEmpty = new int[0] { };

            src.CopyTo(dstEmpty);
            int[] expectedEmpty = { };
            Assert.Equal<int>(expectedEmpty, dstEmpty.ToArray());
        }

        [Fact]
        public static void CopyToLongerArray()
        {
            int[] src = { 1, 2, 3 };
            Memory<int> dst = new int[4] { 99, 100, 101, 102 };

            src.CopyTo(dst);
            int[] expected = { 1, 2, 3, 102 };
            Assert.Equal<int>(expected, dst.ToArray());
        }

        [Fact]
        public static void CopyToShorterArray()
        {
            int[] src = { 1, 2, 3 };
            Memory<int> dst = new int[2] { 99, 100 };

            Assert.Throws<ArgumentException>(() => src.CopyTo(dst));
            int[] expected = { 99, 100 };
            Assert.Equal<int>(expected, dst.ToArray());  // CopyTo() checks for sufficient space before doing any copying.
        }

        [Fact]
        public static void CopyToCovariantArray()
        {
            string[] src = new string[] { "Hello" };
            Memory<object> dst = new object[] { "world" };

            src.CopyTo<object>(dst);
            Assert.Equal("Hello", dst.Span[0]);
        }
    }
}
