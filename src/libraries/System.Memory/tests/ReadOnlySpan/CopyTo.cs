// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.SpanTests
{
    public static partial class ReadOnlySpanTests
    {
        [Fact]
        public static void TryCopyTo()
        {
            int[] src = { 1, 2, 3 };
            int[] dst = { 99, 100, 101 };

            ReadOnlySpan<int> srcSpan = new ReadOnlySpan<int>(src);
            bool success = srcSpan.TryCopyTo(dst);
            Assert.True(success);
            Assert.Equal<int>(src, dst);
        }

        [Fact]
        public static void TryCopyToLonger()
        {
            int[] src = { 1, 2, 3 };
            int[] dst = { 99, 100, 101, 102 };

            ReadOnlySpan<int> srcSpan = new ReadOnlySpan<int>(src);
            bool success = srcSpan.TryCopyTo(dst);
            Assert.True(success);
            int[] expected = { 1, 2, 3, 102 };
            Assert.Equal<int>(expected, dst);
        }

        [Fact]
        public static void TryCopyToShorter()
        {
            int[] src = { 1, 2, 3 };
            int[] dst = { 99, 100 };

            ReadOnlySpan<int> srcSpan = new ReadOnlySpan<int>(src);
            bool success = srcSpan.TryCopyTo(dst);
            Assert.False(success);
            int[] expected = { 99, 100 };
            Assert.Equal<int>(expected, dst);  // TryCopyTo() checks for sufficient space before doing any copying.
        }

        [Fact]
        public static void CopyToShorter()
        {
            int[] src = { 1, 2, 3 };
            int[] dst = { 99, 100 };

            ReadOnlySpan<int> srcSpan = new ReadOnlySpan<int>(src);
            AssertThrows<ArgumentException, int>(srcSpan, (_srcSpan) => _srcSpan.CopyTo(dst));
            int[] expected = { 99, 100 };
            Assert.Equal<int>(expected, dst);  // CopyTo() checks for sufficient space before doing any copying.
        }

        [Fact]
        public static void Overlapping1()
        {
            int[] a = { 90, 91, 92, 93, 94, 95, 96, 97 };

            ReadOnlySpan<int> src = new ReadOnlySpan<int>(a, 1, 6);
            Span<int> dst = new Span<int>(a, 2, 6);
            src.CopyTo(dst);

            int[] expected = { 90, 91, 91, 92, 93, 94, 95, 96 };
            Assert.Equal<int>(expected, a);
        }

        [Fact]
        public static void Overlapping2()
        {
            int[] a = { 90, 91, 92, 93, 94, 95, 96, 97 };

            ReadOnlySpan<int> src = new ReadOnlySpan<int>(a, 2, 6);
            Span<int> dst = new Span<int>(a, 1, 6);
            src.CopyTo(dst);

            int[] expected = { 90, 92, 93, 94, 95, 96, 97, 97 };
            Assert.Equal<int>(expected, a);
        }
    }
}
