// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;
using System.Runtime.CompilerServices;

namespace System.SpanTests
{
    public static partial class SpanTests
    {
        [Fact]
        public static void ArrayAsSpan()
        {
            int[] a = { 91, 92, -93, 94 };
            Span<int> spanInt = a.AsSpan();
            spanInt.Validate<int>(91, 92, -93, 94);

            long[] b = { 91, -92, 93, 94, -95 };
            Span<long> spanLong = b.AsSpan();
            spanLong.Validate<long>(91, -92, 93, 94, -95);

            object o1 = new object();
            object o2 = new object();
            object[] c = { o1, o2 };
            Span<object> spanObject = c.AsSpan();
            spanObject.Validate<object>(o1, o2);
        }

        [Fact]
        public static void ZeroLengthArrayAsSpan()
        {
            int[] empty = Array.Empty<int>();
            Span<int> span = empty.AsSpan();
            span.Validate<int>();
        }

        [Fact]
        public static void ArraySegmentAsSpan()
        {
            int[] a = { 91, 92, -93, 94 };
            ArraySegment<int> segmentInt = new ArraySegment<int>(a, 1, 2);
            Span<int> spanInt = segmentInt.AsSpan();
            spanInt.Validate<int>(92, -93);

            long[] b = { 91, -92, 93, 94, -95 };
            ArraySegment<long> segmentLong = new ArraySegment<long>(b, 1, 3);
            Span<long> spanLong = segmentLong.AsSpan();
            spanLong.Validate<long>(-92, 93, 94);

            object o1 = new object();
            object o2 = new object();
            object o3 = new object();
            object o4 = new object();
            object[] c = { o1, o2, o3, o4 };
            ArraySegment<object> segmentObject = new ArraySegment<object>(c, 1, 2);
            Span<object> spanObject = segmentObject.AsSpan();
            spanObject.Validate<object>(o2, o3);
        }

        [Fact]
        public static void ZeroLengthArraySegmentAsSpan()
        {
            int[] empty = Array.Empty<int>();
            ArraySegment<int> segmentEmpty = new ArraySegment<int>(empty);
            Span<int> spanEmpty = segmentEmpty.AsSpan();
            spanEmpty.Validate<int>();

            int[] a = { 91, 92, -93, 94 };
            ArraySegment<int> segmentInt = new ArraySegment<int>(a, 0, 0);
            Span<int> spanInt = segmentInt.AsSpan();
            spanInt.Validate<int>();
        }
    }
}
