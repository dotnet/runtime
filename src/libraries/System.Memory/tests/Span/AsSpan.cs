// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.SpanTests
{
    public static partial class SpanTests
    {
        [Fact]
        public static void IntArrayAsSpan()
        {
            int[] a = { 91, 92, -93, 94 };
            Span<int> spanInt = a.AsSpan();
            spanInt.Validate(91, 92, -93, 94);
        }

        [Fact]
        public static void LongArrayAsSpan()
        {
            long[] b = { 91, -92, 93, 94, -95 };
            Span<long> spanLong = b.AsSpan();
            spanLong.Validate(91, -92, 93, 94, -95);
        }

        [Fact]
        public static void ObjectArrayAsSpan()
        {
            object o1 = new object();
            object o2 = new object();
            object[] c = { o1, o2 };
            Span<object> spanObject = c.AsSpan();
            spanObject.ValidateReferenceType(o1, o2);
        }

        [Fact]
        public static void NullArrayAsSpan()
        {
            int[] a = null;
            Span<int> span = a.AsSpan();
            span.Validate();
            Assert.True(span == default);
        }

        [Fact]
        public static void EmptyArrayAsSpan()
        {
            int[] empty = Array.Empty<int>();
            Span<int> span = empty.AsSpan();
            span.ValidateNonNullEmpty();
        }

        [Fact]
        public static void IntArraySegmentAsSpan()
        {
            int[] a = { 91, 92, -93, 94 };
            ArraySegment<int> segmentInt = new ArraySegment<int>(a, 1, 2);
            Span<int> spanInt = segmentInt.AsSpan();
            spanInt.Validate(92, -93);
        }

        [Fact]
        public static void LongArraySegmentAsSpan()
        {
            long[] b = { 91, -92, 93, 94, -95 };
            ArraySegment<long> segmentLong = new ArraySegment<long>(b, 1, 3);
            Span<long> spanLong = segmentLong.AsSpan();
            spanLong.Validate(-92, 93, 94);
        }

        [Fact]
        public static void ObjectArraySegmentAsSpan()
        {
            object o1 = new object();
            object o2 = new object();
            object o3 = new object();
            object o4 = new object();
            object[] c = { o1, o2, o3, o4 };
            ArraySegment<object> segmentObject = new ArraySegment<object>(c, 1, 2);
            Span<object> spanObject = segmentObject.AsSpan();
            spanObject.ValidateReferenceType(o2, o3);
        }

        [Fact]
        public static void ZeroLengthArraySegmentAsSpan()
        {
            int[] empty = Array.Empty<int>();
            ArraySegment<int> segmentEmpty = new ArraySegment<int>(empty);
            Span<int> spanEmpty = segmentEmpty.AsSpan();
            spanEmpty.ValidateNonNullEmpty();

            int[] a = { 91, 92, -93, 94 };
            ArraySegment<int> segmentInt = new ArraySegment<int>(a, 0, 0);
            Span<int> spanInt = segmentInt.AsSpan();
            spanInt.ValidateNonNullEmpty();
        }

        [Fact]
        public static void CovariantAsSpanNotSupported()
        {
            object[] a = new string[10];
            Assert.Throws<ArrayTypeMismatchException>(() => a.AsSpan());
            Assert.Throws<ArrayTypeMismatchException>(() => a.AsSpan(0, a.Length));
        }

        [Fact]
        public static void GuidArrayAsSpanWithStartAndLength()
        {
            var arr = new Guid[20];

            Span<Guid> slice = arr.AsSpan().Slice(2, 2);
            Guid guid = Guid.NewGuid();
            slice[1] = guid;

            Assert.Equal(guid, arr[3]);
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
        public static void ArrayAsSpanWithStart(int length, int start)
        {
            int[] a = new int[length];
            Span<int> s = a.AsSpan(start);
            Assert.Equal(length - start, s.Length);
            if (start != length)
            {
                s[0] = 42;
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
        public static void ArraySegmentAsSpanWithStart(int length, int start)
        {
            const int segmentOffset = 5;

            int[] a = new int[length + segmentOffset];
            ArraySegment<int> segment = new ArraySegment<int>(a, 5, length);
            Span<int> s = segment.AsSpan(start);
            Assert.Equal(length - start, s.Length);
            if (s.Length != 0)
            {
                s[0] = 42;
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
        public static void ArrayAsSpanWithStartAndLength(int length, int start, int subLength)
        {
            int[] a = new int[length];
            Span<int> s = a.AsSpan(start, subLength);
            Assert.Equal(subLength, s.Length);
            if (subLength != 0)
            {
                s[0] = 42;
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
        public static void ArraySegmentAsSpanWithStartAndLength(int length, int start, int subLength)
        {
            const int segmentOffset = 5;

            int[] a = new int[length + segmentOffset];
            ArraySegment<int> segment = new ArraySegment<int>(a, segmentOffset, length);
            Span<int> s = segment.AsSpan(start, subLength);
            Assert.Equal(subLength, s.Length);
            if (subLength != 0)
            {
                s[0] = 42;
                Assert.Equal(42, a[segmentOffset + start]);
            }
        }

        [Theory]
        [InlineData(0, -1)]
        [InlineData(0, 1)]
        [InlineData(5, 6)]
        public static void ArrayAsSpanWithStartNegative(int length, int start)
        {
            int[] a = new int[length];
            Assert.Throws<ArgumentOutOfRangeException>(() => a.AsSpan(start));
        }

        [Theory]
        [InlineData(0, -1, 0)]
        [InlineData(0, 1, 0)]
        [InlineData(0, 0, -1)]
        [InlineData(0, 0, 1)]
        [InlineData(5, 6, 0)]
        [InlineData(5, 3, 3)]
        public static void ArrayAsSpanWithStartAndLengthNegative(int length, int start, int subLength)
        {
            int[] a = new int[length];
            Assert.Throws<ArgumentOutOfRangeException>(() => a.AsSpan(start, subLength));
        }
    }
}
