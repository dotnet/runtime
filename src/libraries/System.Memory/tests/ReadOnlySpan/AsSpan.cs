// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using Xunit;

namespace System.SpanTests
{
    public static partial class ReadOnlySpanTests
    {
        [Fact]
        public static void StringAsSpanNullary()
        {
            string s = "Hello";
            ReadOnlySpan<char> span = s.AsSpan();
            char[] expected = s.ToCharArray();
            span.Validate(expected);
        }

        [Fact]
        public static void StringAsSpanEmptyString()
        {
            string s = "";
            ReadOnlySpan<char> span = s.AsSpan();
            span.ValidateNonNullEmpty();
        }

        [Fact]
        public static void StringAsSpanNullChecked()
        {
            string s = null;
            ReadOnlySpan<char> span = s.AsSpan();
            span.Validate();
            Assert.True(span == default);

            span = s.AsSpan(0);
            span.Validate();
            Assert.True(span == default);

            span = s.AsSpan(0, 0);
            span.Validate();
            Assert.True(span == default);
        }

        [Fact]
        public static void StringAsSpanNullNonZeroStartAndLength()
        {
            string str = null;

            Assert.Throws<ArgumentOutOfRangeException>(() => str.AsSpan(1).DontBox());
            Assert.Throws<ArgumentOutOfRangeException>(() => str.AsSpan(-1).DontBox());

            Assert.Throws<ArgumentOutOfRangeException>(() => str.AsSpan(0, 1).DontBox());
            Assert.Throws<ArgumentOutOfRangeException>(() => str.AsSpan(1, 0).DontBox());
            Assert.Throws<ArgumentOutOfRangeException>(() => str.AsSpan(1, 1).DontBox());
            Assert.Throws<ArgumentOutOfRangeException>(() => str.AsSpan(-1, -1).DontBox());
        }

        [Theory]
        [MemberData(nameof(TestHelpers.StringSliceTestData), MemberType = typeof(TestHelpers))]
        public static unsafe void AsSpan_StartAndLength(string text, int start, int length)
        {
            ReadOnlySpan<char> span;
            if (start == -1)
            {
                start = 0;
                length = text.Length;
                span = text.AsSpan();
            }
            else if (length == -1)
            {
                length = text.Length - start;
                span = text.AsSpan(start);
            }
            else
            {
                span = text.AsSpan(start, length);
            }

            Assert.Equal(length, span.Length);

            fixed (char* pText = text)
            {
                char* expected = pText + start;
                void* actual = Unsafe.AsPointer(ref MemoryMarshal.GetReference(span));
                Assert.Equal((IntPtr)expected, (IntPtr)actual);
            }
        }

        [Theory]
        [MemberData(nameof(TestHelpers.StringSlice2ArgTestOutOfRangeData), MemberType = typeof(TestHelpers))]
        public static unsafe void AsSpan_2Arg_OutOfRange(string text, int start)
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("start", () => text.AsSpan(start).DontBox());
        }

        [Theory]
        [MemberData(nameof(TestHelpers.StringSlice3ArgTestOutOfRangeData), MemberType = typeof(TestHelpers))]
        public static unsafe void AsSpan_3Arg_OutOfRange(string text, int start, int length)
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("start", () => text.AsSpan(start, length).DontBox());
        }
    }
}
