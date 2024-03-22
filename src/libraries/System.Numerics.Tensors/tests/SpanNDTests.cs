// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace System.Numerics.Tensors.Tests
{
    public class SpanNDTests
    {
        [Fact]
        public static void IntArrayAsSpanND()
        {
            int[] a = { 91, 92, -93, 94 };
            SpanND<int> spanInt = a.AsSpanND(4);
            Assert.Equal(1, spanInt.Rank);

            Assert.Equal(1, spanInt.Lengths.Length);
            Assert.Equal(4, spanInt.Lengths[0]);
            Assert.Equal(1, spanInt.Strides.Length);
            Assert.Equal(1, spanInt.Strides[0]);
            Assert.Equal(91, spanInt[0]);
            Assert.Equal(92, spanInt[1]);
            Assert.Equal(-93, spanInt[2]);
            Assert.Equal(94, spanInt[3]);
            Assert.Equal(a, spanInt.ToArray());
            spanInt[0] = 100;
            spanInt[1] = 101;
            spanInt[2] = -102;
            spanInt[3] = 103;

            Assert.Equal(100, spanInt[0]);
            Assert.Equal(101, spanInt[1]);
            Assert.Equal(-102, spanInt[2]);
            Assert.Equal(103, spanInt[3]);

            a[0] = 91;
            a[1] = 92;
            a[2] = -93;
            a[3] = 94;
            spanInt = a.AsSpanND(2, 2);
            Assert.Equal(a, spanInt.ToArray());
            Assert.Equal(2, spanInt.Rank);
            //Assert.Equal(4, spanInt.Length);
            Assert.Equal(2, spanInt.Lengths.Length);
            Assert.Equal(2, spanInt.Lengths[0]);
            Assert.Equal(2, spanInt.Lengths[1]);
            Assert.Equal(2, spanInt.Strides.Length);
            Assert.Equal(2, spanInt.Strides[0]);
            Assert.Equal(1, spanInt.Strides[1]);
            Assert.Equal(91, spanInt[0, 0]);
            Assert.Equal(92, spanInt[0, 1]);
            Assert.Equal(-93, spanInt[1, 0]);
            Assert.Equal(94, spanInt[1, 1]);

            spanInt[0, 0] = 100;
            spanInt[0, 1] = 101;
            spanInt[1, 0] = -102;
            spanInt[1, 1] = 103;

            Assert.Equal(100, spanInt[0, 0]);
            Assert.Equal(101, spanInt[0, 1]);
            Assert.Equal(-102, spanInt[1, 0]);
            Assert.Equal(103, spanInt[1, 1]);
        }

        [Fact]
        public static void SpanNDFillTest()
        {
            int[] a = [1, 2, 3, 4, 5, 6, 7, 8, 9];
            SpanND<int> spanInt = a.AsSpanND(3, 3);
            spanInt.Fill(-1);
            var enumerator = spanInt.GetEnumerator();
            while (enumerator.MoveNext())
            {
                Assert.Equal(-1, enumerator.Current);
            }

            spanInt.Fill(int.MinValue);
            enumerator = spanInt.GetEnumerator();
            while (enumerator.MoveNext())
            {
                Assert.Equal(int.MinValue, enumerator.Current);
            }

            spanInt.Fill(int.MaxValue);
            enumerator = spanInt.GetEnumerator();
            while (enumerator.MoveNext())
            {
                Assert.Equal(int.MaxValue, enumerator.Current);
            }

            a = [1, 2, 3, 4, 5, 6, 7, 8, 9];
            spanInt = a.AsSpanND(9);
            spanInt.Fill(-1);
            enumerator = spanInt.GetEnumerator();
            while (enumerator.MoveNext())
            {
                Assert.Equal(-1, enumerator.Current);
            }

            a = [.. Enumerable.Range(0, 27)];
            spanInt = a.AsSpanND(3,3,3);
            spanInt.Fill(-1);
            enumerator = spanInt.GetEnumerator();
            while (enumerator.MoveNext())
            {
                Assert.Equal(-1, enumerator.Current);
            }

            a = [.. Enumerable.Range(0, 12)];
            spanInt = a.AsSpanND(3, 2, 2);
            spanInt.Fill(-1);
            enumerator = spanInt.GetEnumerator();
            while (enumerator.MoveNext())
            {
                Assert.Equal(-1, enumerator.Current);
            }

            a = [.. Enumerable.Range(0, 16)];
            spanInt = a.AsSpanND(2,2,2,2);
            spanInt.Fill(-1);
            enumerator = spanInt.GetEnumerator();
            while (enumerator.MoveNext())
            {
                Assert.Equal(-1, enumerator.Current);
            }

            a = [.. Enumerable.Range(0, 24)];
            spanInt = a.AsSpanND(3, 2, 2, 2);
            spanInt.Fill(-1);
            enumerator = spanInt.GetEnumerator();
            while (enumerator.MoveNext())
            {
                Assert.Equal(-1, enumerator.Current);
            }
        }

        [Fact]
        public static void SpanNDClearTest()
        {
            int[] a = [1, 2, 3, 4, 5, 6, 7, 8, 9];
            SpanND<int> spanInt = a.AsSpanND(3, 3);
            spanInt.Clear();
            var enumerator = spanInt.GetEnumerator();
            while (enumerator.MoveNext())
            {
                Assert.Equal(0, enumerator.Current);
            }

            spanInt.Clear();
            enumerator = spanInt.GetEnumerator();
            while (enumerator.MoveNext())
            {
                Assert.Equal(0, enumerator.Current);
            }

            spanInt.Clear();
            enumerator = spanInt.GetEnumerator();
            while (enumerator.MoveNext())
            {
                Assert.Equal(0, enumerator.Current);
            }

            a = [1, 2, 3, 4, 5, 6, 7, 8, 9];
            spanInt = a.AsSpanND(9);
            spanInt.Clear();
            enumerator = spanInt.GetEnumerator();
            while (enumerator.MoveNext())
            {
                Assert.Equal(0, enumerator.Current);
            }

            a = [.. Enumerable.Range(0, 27)];
            spanInt = a.AsSpanND(3, 3, 3);
            spanInt.Clear();
            enumerator = spanInt.GetEnumerator();
            while (enumerator.MoveNext())
            {
                Assert.Equal(0, enumerator.Current);
            }

            a = [.. Enumerable.Range(0, 12)];
            spanInt = a.AsSpanND(3, 2, 2);
            spanInt.Clear();
            enumerator = spanInt.GetEnumerator();
            while (enumerator.MoveNext())
            {
                Assert.Equal(0, enumerator.Current);
            }

            a = [.. Enumerable.Range(0, 16)];
            spanInt = a.AsSpanND(2, 2, 2, 2);
            spanInt.Clear();
            enumerator = spanInt.GetEnumerator();
            while (enumerator.MoveNext())
            {
                Assert.Equal(0, enumerator.Current);
            }

            a = [.. Enumerable.Range(0, 24)];
            spanInt = a.AsSpanND(3, 2, 2, 2);
            spanInt.Clear();
            enumerator = spanInt.GetEnumerator();
            while (enumerator.MoveNext())
            {
                Assert.Equal(0, enumerator.Current);
            }
        }

        [Fact]
        public static void SpanNDCopyTest()
        {
            int[] leftData = [1, 2, 3, 4, 5, 6, 7, 8, 9];
            int[] rightData = new int[9];
            SpanND<int> leftSpan = leftData.AsSpanND(3, 3);
            SpanND<int> rightSpan = rightData.AsSpanND(3, 3);
            leftSpan.CopyTo(rightSpan);
            var leftEnum = leftSpan.GetEnumerator();
            var rightEnum = rightSpan.GetEnumerator();
            while(leftEnum.MoveNext() && rightEnum.MoveNext())
            {
                Assert.Equal(leftEnum.Current, rightEnum.Current);
            }

            //Make sure its a copy
            leftSpan[0, 0] = 100;
            Assert.NotEqual(leftSpan[0, 0], rightSpan[0, 0]);

            leftData = [1, 2, 3, 4, 5, 6, 7, 8, 9];
            rightData = new int[15];
            leftSpan = leftData.AsSpanND(9);
            rightSpan = rightData.AsSpanND(15);
            leftSpan.CopyTo(rightSpan);
            leftEnum = leftSpan.GetEnumerator();
            rightEnum = rightSpan.GetEnumerator();
            // Make sure the first 9 spots are equal after copy
            while (leftEnum.MoveNext() && rightEnum.MoveNext())
            {
                Assert.Equal(leftEnum.Current, rightEnum.Current);
            }
            // The rest of the slots shouldn't have been touched.
            while(rightEnum.MoveNext())
            {
                Assert.Equal(0, rightEnum.Current);
            }

            //Make sure its a copy
            leftSpan[0] = 100;
            Assert.NotEqual(leftSpan[0], rightSpan[0]);

            leftData = [.. Enumerable.Range(0, 27)];
            rightData = [.. Enumerable.Range(0, 27)];
            leftSpan = leftData.AsSpanND(3, 3, 3);
            rightSpan = rightData.AsSpanND(3, 3, 3);
            leftSpan.CopyTo(rightSpan);

            while (leftEnum.MoveNext() && rightEnum.MoveNext())
            {
                Assert.Equal(leftEnum.Current, rightEnum.Current);
            }

            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                var l = leftData.AsSpanND(3, 3, 3);
                var r = new SpanND<int>();
                l.CopyTo(r);
            });
        }

        [Fact]
        public static void SpanNDTryCopyTest()
        {
            int[] leftData = [1, 2, 3, 4, 5, 6, 7, 8, 9];
            int[] rightData = new int[9];
            SpanND<int> leftSpan = leftData.AsSpanND(3, 3);
            SpanND<int> rightSpan = rightData.AsSpanND(3, 3);
            var success = leftSpan.TryCopyTo(rightSpan);
            Assert.True(success);
            var leftEnum = leftSpan.GetEnumerator();
            var rightEnum = rightSpan.GetEnumerator();
            while (leftEnum.MoveNext() && rightEnum.MoveNext())
            {
                Assert.Equal(leftEnum.Current, rightEnum.Current);
            }

            //Make sure its a copy
            leftSpan[0, 0] = 100;
            Assert.NotEqual(leftSpan[0, 0], rightSpan[0, 0]);

            leftData = [1, 2, 3, 4, 5, 6, 7, 8, 9];
            rightData = new int[15];
            leftSpan = leftData.AsSpanND(9);
            rightSpan = rightData.AsSpanND(15);
            success = leftSpan.TryCopyTo(rightSpan);
            leftEnum = leftSpan.GetEnumerator();
            rightEnum = rightSpan.GetEnumerator();
            Assert.True(success);
            // Make sure the first 9 spots are equal after copy
            while (leftEnum.MoveNext() && rightEnum.MoveNext())
            {
                Assert.Equal(leftEnum.Current, rightEnum.Current);
            }
            // The rest of the slots shouldn't have been touched.
            while (rightEnum.MoveNext())
            {
                Assert.Equal(0, rightEnum.Current);
            }

            //Make sure its a copy
            leftSpan[0] = 100;
            Assert.NotEqual(leftSpan[0], rightSpan[0]);

            leftData = [.. Enumerable.Range(0, 27)];
            rightData = [.. Enumerable.Range(0, 27)];
            leftSpan = leftData.AsSpanND(3, 3, 3);
            rightSpan = rightData.AsSpanND(3, 3, 3);
            success = leftSpan.TryCopyTo(rightSpan);
            Assert.True(success);

            while (leftEnum.MoveNext() && rightEnum.MoveNext())
            {
                Assert.Equal(leftEnum.Current, rightEnum.Current);
            }

            var l = leftData.AsSpanND(3, 3, 3);
            var r = new SpanND<int>();
            success = l.TryCopyTo(r);
            Assert.False(success);
        }

        [Fact]
        public static void SpanNDSliceTest()
        {
            int[] a = [1, 2, 3, 4, 5, 6, 7, 8, 9];
            SpanND<int> spanInt = a.AsSpanND(3, 3);

            Assert.Throws<ArgumentOutOfRangeException>(() => a.AsSpanND(2, 3).Slice(0..1));
            Assert.Throws<ArgumentOutOfRangeException>(() => a.AsSpanND(2, 3).Slice(1..2));
            Assert.Throws<ArgumentOutOfRangeException>(() => a.AsSpanND(2, 3).Slice(0..1, 5..6));

            var sp = spanInt.Slice(1..3, 1..3);
            Assert.Equal(5, sp[0, 0]);
            Assert.Equal(6, sp[0, 1]);
            Assert.Equal(8, sp[1, 0]);
            Assert.Equal(9, sp[1, 1]);
            int[] slice = [5, 6, 8, 9];
            Assert.Equal(slice, sp.ToArray());
            var enumerator = sp.GetEnumerator();
            var index = 0;
            while (enumerator.MoveNext())
            {
                Assert.Equal(slice[index++], enumerator.Current);
            }

            sp = spanInt.Slice(0..3, 0..3);
            Assert.Equal(1, sp[0, 0]);
            Assert.Equal(2, sp[0, 1]);
            Assert.Equal(3, sp[0, 2]);
            Assert.Equal(4, sp[1, 0]);
            Assert.Equal(5, sp[1, 1]);
            Assert.Equal(6, sp[1, 2]);
            Assert.Equal(7, sp[2, 0]);
            Assert.Equal(8, sp[2, 1]);
            Assert.Equal(9, sp[2, 2]);
            Assert.Equal(a, sp.ToArray());
            enumerator = sp.GetEnumerator();
            index = 0;
            while (enumerator.MoveNext())
            {
                Assert.Equal(a[index++], enumerator.Current);
            }

            sp = spanInt.Slice(0..1, 0..1);
            Assert.Equal(1, sp[0, 0]);
            Assert.Throws<IndexOutOfRangeException>(() => a.AsSpanND(3, 3).Slice(0..1, 0..1)[0, 1]);
            slice = [1];
            Assert.Equal(slice, sp.ToArray());
            enumerator = sp.GetEnumerator();
            index = 0;
            while (enumerator.MoveNext())
            {
                Assert.Equal(slice[index++], enumerator.Current);
            }

            sp = spanInt.Slice(0..2, 0..2);
            Assert.Equal(1, sp[0, 0]);
            Assert.Equal(2, sp[0, 1]);
            Assert.Equal(4, sp[1, 0]);
            Assert.Equal(5, sp[1, 1]);
            slice = [1, 2, 4, 5];
            Assert.Equal(slice, sp.ToArray());
            enumerator = sp.GetEnumerator();
            index = 0;
            while (enumerator.MoveNext())
            {
                Assert.Equal(slice[index++], enumerator.Current);
            }

            int[] numbers = [.. Enumerable.Range(0, 27)];
            spanInt = numbers.AsSpanND(3, 3, 3);
            sp = spanInt.Slice(1..2, 1..2, 1..2);
            Assert.Equal(13, sp[0, 0, 0]);
            slice = [13];
            Assert.Equal(slice, sp.ToArray());
            enumerator = sp.GetEnumerator();
            index = 0;
            while (enumerator.MoveNext())
            {
                Assert.Equal(slice[index++], enumerator.Current);
            }

            sp = spanInt.Slice(1..3, 1..3, 1..3);
            Assert.Equal(13, sp[0, 0, 0]);
            Assert.Equal(14, sp[0, 0, 1]);
            Assert.Equal(16, sp[0, 1, 0]);
            Assert.Equal(17, sp[0, 1, 1]);
            Assert.Equal(22, sp[1, 0, 0]);
            Assert.Equal(23, sp[1, 0, 1]);
            Assert.Equal(25, sp[1, 1, 0]);
            Assert.Equal(26, sp[1, 1, 1]);
            slice = [13, 14, 16, 17, 22, 23, 25, 26];
            Assert.Equal(slice, sp.ToArray());
            enumerator = sp.GetEnumerator();
            index = 0;
            while (enumerator.MoveNext())
            {
                Assert.Equal(slice[index++], enumerator.Current);
            }

            numbers = [.. Enumerable.Range(0, 16)];
            spanInt = numbers.AsSpanND(2, 2, 2, 2);
            sp = spanInt.Slice(1..2, 0..2, 1..2, 0..2);
            Assert.Equal(10, sp[0,0,0,0]);
            Assert.Equal(11, sp[0,0,0,1]);
            Assert.Equal(14, sp[0,1,0,0]);
            Assert.Equal(15, sp[0,1,0,1]);
            slice = [10, 11, 14, 15];
            Assert.Equal(slice, sp.ToArray());
            enumerator = sp.GetEnumerator();
            index = 0;
            while (enumerator.MoveNext())
            {
                Assert.Equal(slice[index++], enumerator.Current);
            }
        }

        

        [Fact]
        public static void LongArrayAsSpanND()
        {
            long[] b = { 91, -92, 93, 94, -95 };
            SpanND<long> spanLong = b.AsSpanND(5);
            Assert.Equal(91, spanLong[0]);
            Assert.Equal(-92, spanLong[1]);
            Assert.Equal(93, spanLong[2]);
            Assert.Equal(94, spanLong[3]);
            Assert.Equal(-95, spanLong[4]);
        }

        //[Fact]
        //public static void ObjectArrayAsSpanND()
        //{
        //    object o1 = new object();
        //    object o2 = new object();
        //    object[] c = { o1, o2 };
        //    SpanND<object> spanObject = c.AsSpanND();
        //    spanObject.ValidateReferenceType(o1, o2);
        //}

        [Fact]
        public static void NullArrayAsSpanND()
        {
            int[] a = null;
            SpanND<int> span = a.AsSpanND();
            Assert.True(span == default);
        }

        //[Fact]
        //public static void EmptyArrayAsSpanND()
        //{
        //    int[] empty = Array.Empty<int>();
        //    SpanND<int> span = empty.AsSpanND();
        //    span.ValidateNonNullEmpty();
        //}

        //[Fact]
        //public static void IntArraySegmentAsSpanND()
        //{
        //    int[] a = { 91, 92, -93, 94 };
        //    ArraySegment<int> segmentInt = new ArraySegment<int>(a, 1, 2);
        //    SpanND<int> spanInt = segmentInt.AsSpanND();
        //    spanInt.Validate(92, -93);
        //}

        //[Fact]
        //public static void LongArraySegmentAsSpanND()
        //{
        //    long[] b = { 91, -92, 93, 94, -95 };
        //    ArraySegment<long> segmentLong = new ArraySegment<long>(b, 1, 3);
        //    SpanND<long> spanLong = segmentLong.AsSpanND();
        //    spanLong.Validate(-92, 93, 94);
        //}

        //[Fact]
        //public static void ObjectArraySegmentAsSpanND()
        //{
        //    object o1 = new object();
        //    object o2 = new object();
        //    object o3 = new object();
        //    object o4 = new object();
        //    object[] c = { o1, o2, o3, o4 };
        //    ArraySegment<object> segmentObject = new ArraySegment<object>(c, 1, 2);
        //    SpanND<object> spanObject = segmentObject.AsSpanND();
        //    spanObject.ValidateReferenceType(o2, o3);
        //}

        //[Fact]
        //public static void ZeroLengthArraySegmentAsSpanND()
        //{
        //    int[] empty = Array.Empty<int>();
        //    ArraySegment<int> segmentEmpty = new ArraySegment<int>(empty);
        //    SpanND<int> spanEmpty = segmentEmpty.AsSpanND();
        //    spanEmpty.ValidateNonNullEmpty();

        //    int[] a = { 91, 92, -93, 94 };
        //    ArraySegment<int> segmentInt = new ArraySegment<int>(a, 0, 0);
        //    SpanND<int> spanInt = segmentInt.AsSpanND();
        //    spanInt.ValidateNonNullEmpty();
        //}

        //[Fact]
        //public static void CovariantAsSpanNDNotSupported()
        //{
        //    object[] a = new string[10];
        //    Assert.Throws<ArrayTypeMismatchException>(() => a.AsSpanND());
        //    Assert.Throws<ArrayTypeMismatchException>(() => a.AsSpanND(0, a.Length));
        //}

        //[Fact]
        //public static void GuidArrayAsSpanNDWithStartAndLength()
        //{
        //    var arr = new Guid[20];

        //    SpanND<Guid> slice = arr.AsSpanND(2, 2);
        //    Guid guid = Guid.NewGuid();
        //    slice[1] = guid;

        //    Assert.Equal(guid, arr[3]);
        //}

        //[Theory]
        //[InlineData(0, 0)]
        //[InlineData(3, 0)]
        //[InlineData(3, 1)]
        //[InlineData(3, 2)]
        //[InlineData(3, 3)]
        //[InlineData(10, 0)]
        //[InlineData(10, 3)]
        //[InlineData(10, 10)]
        //public static void ArrayAsSpanNDWithStart(int length, int start)
        //{
        //    int[] a = new int[length];
        //    SpanND<int> s = a.AsSpanND(start);
        //    Assert.Equal(length - start, s.Length);
        //    if (start != length)
        //    {
        //        s[0] = 42;
        //        Assert.Equal(42, a[start]);
        //    }
        //}

        //[Theory]
        //[InlineData(0, 0)]
        //[InlineData(3, 0)]
        //[InlineData(3, 1)]
        //[InlineData(3, 2)]
        //[InlineData(3, 3)]
        //[InlineData(10, 0)]
        //[InlineData(10, 3)]
        //[InlineData(10, 10)]
        //public static void ArraySegmentAsSpanNDWithStart(int length, int start)
        //{
        //    const int segmentOffset = 5;

        //    int[] a = new int[length + segmentOffset];
        //    ArraySegment<int> segment = new ArraySegment<int>(a, 5, length);
        //    SpanND<int> s = segment.AsSpanND(start);
        //    Assert.Equal(length - start, s.Length);
        //    if (s.Length != 0)
        //    {
        //        s[0] = 42;
        //        Assert.Equal(42, a[segmentOffset + start]);
        //    }
        //}

        //[Theory]
        //[InlineData(0, 0, 0)]
        //[InlineData(3, 0, 3)]
        //[InlineData(3, 1, 2)]
        //[InlineData(3, 2, 1)]
        //[InlineData(3, 3, 0)]
        //[InlineData(10, 0, 5)]
        //[InlineData(10, 3, 2)]
        //public static void ArrayAsSpanNDWithStartAndLength(int length, int start, int subLength)
        //{
        //    int[] a = new int[length];
        //    SpanND<int> s = a.AsSpanND(start, subLength);
        //    Assert.Equal(subLength, s.Length);
        //    if (subLength != 0)
        //    {
        //        s[0] = 42;
        //        Assert.Equal(42, a[start]);
        //    }
        //}

        //[Theory]
        //[InlineData(0, 0, 0)]
        //[InlineData(3, 0, 3)]
        //[InlineData(3, 1, 2)]
        //[InlineData(3, 2, 1)]
        //[InlineData(3, 3, 0)]
        //[InlineData(10, 0, 5)]
        //[InlineData(10, 3, 2)]
        //public static void ArraySegmentAsSpanNDWithStartAndLength(int length, int start, int subLength)
        //{
        //    const int segmentOffset = 5;

        //    int[] a = new int[length + segmentOffset];
        //    ArraySegment<int> segment = new ArraySegment<int>(a, segmentOffset, length);
        //    SpanND<int> s = segment.AsSpanND(start, subLength);
        //    Assert.Equal(subLength, s.Length);
        //    if (subLength != 0)
        //    {
        //        s[0] = 42;
        //        Assert.Equal(42, a[segmentOffset + start]);
        //    }
        //}

        //[Theory]
        //[InlineData(0, -1)]
        //[InlineData(0, 1)]
        //[InlineData(5, 6)]
        //public static void ArrayAsSpanNDWithStartNegative(int length, int start)
        //{
        //    int[] a = new int[length];
        //    Assert.Throws<ArgumentOutOfRangeException>(() => a.AsSpanND(start));
        //}

        //[Theory]
        //[InlineData(0, -1, 0)]
        //[InlineData(0, 1, 0)]
        //[InlineData(0, 0, -1)]
        //[InlineData(0, 0, 1)]
        //[InlineData(5, 6, 0)]
        //[InlineData(5, 3, 3)]
        //public static void ArrayAsSpanNDWithStartAndLengthNegative(int length, int start, int subLength)
        //{
        //    int[] a = new int[length];
        //    Assert.Throws<ArgumentOutOfRangeException>(() => a.AsSpanND(start, subLength));
        //}
    }
}
