// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.SpanTests
{
    //
    // Tests for Span<T>.ctor(T[], int, int). If the test is not specific to this overload, consider putting it in CtorArray.cs instread.
    //
    public static partial class SpanTests
    {
        [Fact]
        public static void CtorArrayIntInt1()
        {
            int[] a = { 90, 91, 92, 93, 94, 95, 96, 97, 98 };
            Span<int> span = new Span<int>(a, 3, 2);
            span.Validate<int>(93, 94);
        }

        [Fact]
        public static void CtorArrayIntInt2()
        {
            long[] a = { 90, 91, 92, 93, 94, 95, 96, 97, 98 };
            Span<long> span = new Span<long>(a, 4, 3);
            span.Validate<long>(94, 95, 96);
        }

        [Fact]
        public static void CtorArrayIntIntRangeExtendsToEndOfArray()
        {
            long[] a = { 90, 91, 92, 93, 94, 95, 96, 97, 98 };
            Span<long> span = new Span<long>(a, 4, 5);
            span.Validate<long>(94, 95, 96, 97, 98);
        }

        [Fact]
        public static void CtorArrayIntIntNegativeStart()
        {
            int[] a = new int[3];
            Assert.Throws<ArgumentOutOfRangeException>(() => new Span<int>(a, -1, 0).DontBox());
        }

        [Fact]
        public static void CtorArrayIntIntStartTooLarge()
        {
            int[] a = new int[3];
            Assert.Throws<ArgumentOutOfRangeException>(() => new Span<int>(a, 4, 0).DontBox());
        }

        [Fact]
        public static void CtorArrayIntIntNegativeLength()
        {
            int[] a = new int[3];
            Assert.Throws<ArgumentOutOfRangeException>(() => new Span<int>(a, 0, -1).DontBox());
        }

        [Fact]
        public static void CtorArrayIntIntStartAndLengthTooLarge()
        {
            int[] a = new int[3];
            Assert.Throws<ArgumentOutOfRangeException>(() => new Span<int>(a, 3, 1).DontBox());
            Assert.Throws<ArgumentOutOfRangeException>(() => new Span<int>(a, 2, 2).DontBox());
            Assert.Throws<ArgumentOutOfRangeException>(() => new Span<int>(a, 1, 3).DontBox());
            Assert.Throws<ArgumentOutOfRangeException>(() => new Span<int>(a, 0, 4).DontBox());
            Assert.Throws<ArgumentOutOfRangeException>(() => new Span<int>(a, int.MaxValue, int.MaxValue).DontBox());
        }

        [Fact]
        public static void CtorArrayIntIntStartEqualsLength()
        {
            // Valid for start to equal the array length. This returns an empty span that starts "just past the array."
            int[] a = { 91, 92, 93 };
            Span<int> span = new Span<int>(a, 3, 0);
            span.Validate<int>();
        }
    }
}

