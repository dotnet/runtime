// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;
using System.Runtime.CompilerServices;

namespace System.SpanTests
{
    public static partial class ReadOnlySpanTests
    {
        [Fact]
        public static void ArrayAsReadOnlySpan()
        {
            int[] a = { 19, -17 };
            ReadOnlySpan<int> spanInt = a.AsReadOnlySpan();
            spanInt.Validate<int>(19, -17);

            long[] b = { 1, -3, 7, -15, 31 };
            ReadOnlySpan<long> spanLong = b.AsReadOnlySpan();
            spanLong.Validate<long>(1, -3, 7, -15, 31);

            object o1 = new object();
            object o2 = new object();
            object[] c = { o1, o2 };
            ReadOnlySpan<object> spanObject = c.AsReadOnlySpan();
            spanObject.Validate<object>(o1, o2);
        }

        [Fact]
        public static void NullArrayAsReadOnlySpan()
        {
            int[] a = null;
            ReadOnlySpan<int> span;
            AssertThrows<ArgumentNullException, int>(span, _span => _span = a.AsReadOnlySpan());
        }

        [Fact]
        public static void EmptyArrayAsReadOnlySpan()
        {
            int[] empty = Array.Empty<int>();
            ReadOnlySpan<int> span = empty.AsReadOnlySpan();
            span.Validate<int>();
        }

        [Fact]
        public static void ArraySegmentAsSpan()
        {
            int[] a = { 19, -17 };
            ArraySegment<int> segmentInt = new ArraySegment<int>(a, 1, 1);
            ReadOnlySpan<int> spanInt = segmentInt.AsReadOnlySpan();
            spanInt.Validate<int>(-17);

            long[] b = { 1, -3, 7, -15, 31 };
            ArraySegment<long> segmentLong = new ArraySegment<long>(b, 1, 3);
            ReadOnlySpan<long> spanLong = segmentLong.AsReadOnlySpan();
            spanLong.Validate<long>(-3, 7, -15);

            object o1 = new object();
            object o2 = new object();
            object o3 = new object();
            object o4 = new object();
            object[] c = { o1, o2, o3, o4 };
            ArraySegment<object> segmentObject = new ArraySegment<object>(c, 0, 2);
            ReadOnlySpan<object> spanObject = segmentObject.AsReadOnlySpan();
            spanObject.Validate<object>(o1, o2);
        }

        [Fact]
        public static void ZeroLengthArraySegmentAsReadOnlySpan()
        {
            int[] empty = Array.Empty<int>();
            ArraySegment<int> emptySegment = new ArraySegment<int>(empty);
            ReadOnlySpan<int> span = emptySegment.AsReadOnlySpan();
            span.Validate<int>();

            int[] a = { 19, -17 };
            ArraySegment<int> segmentInt = new ArraySegment<int>(a, 1, 0);
            ReadOnlySpan<int> spanInt = segmentInt.AsReadOnlySpan();
            spanInt.Validate<int>();
        }
    }
}