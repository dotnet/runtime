// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.SpanTests
{
    //
    // Tests for Span<T>.ctor(T[], int). If the test is not specific to this overload, consider putting it in CtorArray.cs instread.
    //
    public static partial class ReadOnlySpanTests
    {
        [Fact]
        public static void CtorArrayInt1()
        {
            int[] a = { 90, 91, 92, 93, 94, 95, 96, 97, 98 };
            ReadOnlySpan<int> span = new ReadOnlySpan<int>(a, 3);
            span.Validate<int>(93, 94, 95, 96, 97, 98);
        }

        [Fact]
        public static void CtorArrayInt2()
        {
            long[] a = { 90, 91, 92, 93, 94, 95, 96, 97, 98 };
            ReadOnlySpan<long> span = new ReadOnlySpan<long>(a, 3);
            span.Validate<long>(93, 94, 95, 96, 97, 98);
        }

        [Fact]
        public static void CtorArrayIntNegativeStart()
        {
            int[] a = new int[3];
            Assert.Throws<ArgumentOutOfRangeException>(() => new ReadOnlySpan<int>(a, -1).DontBox());
        }

        [Fact]
        public static void CtorArrayIntStartTooLarge()
        {
            int[] a = new int[3];
            Assert.Throws<ArgumentOutOfRangeException>(() => new ReadOnlySpan<int>(a, 4).DontBox());
        }

        [Fact]
        public static void CtorArrayIntStartEqualsLength()
        {
            // Valid for start to equal the array length. This returns an empty span that starts "just past the array."
            int[] a = { 91, 92, 93 };
            ReadOnlySpan<int> span = new ReadOnlySpan<int>(a, 3);
            span.Validate<int>();
        }
    }
}

