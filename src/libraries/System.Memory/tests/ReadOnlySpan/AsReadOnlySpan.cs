// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using Xunit;

namespace System.SpanTests
{
    public static partial class ReadOnlySpanTests
    {
        [Fact]
        public static void IntArrayAsReadOnlySpan()
        {
            int[] a = { 19, -17 };
            ReadOnlySpan<int> spanInt = a.AsReadOnlySpan();
            spanInt.Validate(19, -17);
        }

        [Fact]
        public static void LongArrayAsReadOnlySpan()
        {
            long[] b = { 1, -3, 7, -15, 31 };
            ReadOnlySpan<long> spanLong = b.AsReadOnlySpan();
            spanLong.Validate(1, -3, 7, -15, 31);
        }

        [Fact]
        public static void ObjectArrayAsReadOnlySpan()
        {
            object o1 = new object();
            object o2 = new object();
            object[] c = { o1, o2 };
            ReadOnlySpan<object> spanObject = c.AsReadOnlySpan();
            spanObject.ValidateReferenceType(o1, o2);
        }

        [Fact]
        public static void NullArrayAsReadOnlySpan()
        {
            int[] a = null;
            Assert.Throws<ArgumentNullException>(() => a.AsReadOnlySpan().DontBox());
        }

        [Fact]
        public static void EmptyArrayAsReadOnlySpan()
        {
            int[] empty = Array.Empty<int>();
            ReadOnlySpan<int> span = empty.AsReadOnlySpan();
            span.ValidateNonNullEmpty();
        }

        [Fact]
        public static void IntArraySegmentAsSpan()
        {
            int[] a = { 19, -17 };
            ArraySegment<int> segmentInt = new ArraySegment<int>(a, 1, 1);
            ReadOnlySpan<int> spanInt = segmentInt.AsReadOnlySpan();
            spanInt.Validate(-17);
        }

        [Fact]
        public static void LongArraySegmentAsSpan()
        {
            long[] b = { 1, -3, 7, -15, 31 };
            ArraySegment<long> segmentLong = new ArraySegment<long>(b, 1, 3);
            ReadOnlySpan<long> spanLong = segmentLong.AsReadOnlySpan();
            spanLong.Validate(-3, 7, -15);
        }

        [Fact]
        public static void ObjectArraySegmentAsSpan()
        {
            object o1 = new object();
            object o2 = new object();
            object o3 = new object();
            object o4 = new object();
            object[] c = { o1, o2, o3, o4 };
            ArraySegment<object> segmentObject = new ArraySegment<object>(c, 0, 2);
            ReadOnlySpan<object> spanObject = segmentObject.AsReadOnlySpan();
            spanObject.ValidateReferenceType(o1, o2);
        }

        [Fact]
        public static void ZeroLengthArraySegmentAsReadOnlySpan()
        {
            int[] empty = Array.Empty<int>();
            ArraySegment<int> emptySegment = new ArraySegment<int>(empty);
            ReadOnlySpan<int> span = emptySegment.AsReadOnlySpan();
            span.ValidateNonNullEmpty();

            int[] a = { 19, -17 };
            ArraySegment<int> segmentInt = new ArraySegment<int>(a, 1, 0);
            ReadOnlySpan<int> spanInt = segmentInt.AsReadOnlySpan();
            spanInt.ValidateNonNullEmpty();
        }

        [Fact]
        public static void StringAsReadOnlySpanNullary()
        {
            string s = "Hello";
            ReadOnlySpan<char> span = s.AsReadOnlySpan();
            char[] expected = s.ToCharArray();
            span.Validate(expected);
        }

        [Fact]
        public static void StringAsReadOnlySpanEmptyString()
        {
            string s = "";
            ReadOnlySpan<char> span = s.AsReadOnlySpan();
            span.ValidateNonNullEmpty();
        }

        [Fact]
        public static void StringAsReadOnlySpanNullChecked()
        {
            string s = null;
            Assert.Throws<ArgumentNullException>(() => s.AsReadOnlySpan().DontBox());
            Assert.Throws<ArgumentNullException>(() => s.AsReadOnlySpan(0).DontBox());
            Assert.Throws<ArgumentNullException>(() => s.AsReadOnlySpan(0, 0).DontBox());
        }

        [Fact]
        public static void EmptySpanAsReadOnlySpan()
        {
            Span<int> span = default;
            Assert.True(span.AsReadOnlySpan().IsEmpty);
        }

        [Fact]
        public static void SpanAsReadOnlySpan()
        {
            int[] a = { 19, -17 };
            Span<int> span = new Span<int>(a);
            ReadOnlySpan<int> readOnlySpan = span.AsReadOnlySpan();

            readOnlySpan.Validate(a);
        }

        [Theory]
        [MemberData(nameof(TestHelpers.StringSliceTestData), MemberType = typeof(TestHelpers))]
        public static unsafe void AsReadOnlySpan_PointerAndLength(string text, int start, int length)
        {
            ReadOnlySpan<char> span;
            if (start == -1)
            {
                start = 0;
                length = text.Length;
                span = text.AsReadOnlySpan();
            }
            else if (length == -1)
            {
                length = text.Length - start;
                span = text.AsReadOnlySpan(start);
            }
            else
            {
                span = text.AsReadOnlySpan(start, length);
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
        public static unsafe void AsReadOnlySpan_2Arg_OutOfRange(string text, int start)
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("start", () => text.AsReadOnlySpan(start).DontBox());
        }

        [Theory]
        [MemberData(nameof(TestHelpers.StringSlice3ArgTestOutOfRangeData), MemberType = typeof(TestHelpers))]
        public static unsafe void AsReadOnlySpan_3Arg_OutOfRange(string text, int start, int length)
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("start", () => text.AsReadOnlySpan(start, length).DontBox());
        }
    }
}
