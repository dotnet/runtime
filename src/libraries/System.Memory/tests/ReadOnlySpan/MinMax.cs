// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.SpanTests
{
    public static partial class ReadOnlySpanTests
    {
        [Fact]
        public static void MinMax_Empty_NonNullableValueType_Throws()
        {
            ReadOnlySpan<int> span = ReadOnlySpan<int>.Empty;
            IComparer<int> nonDefaultComparer = Comparer<int>.Create((x, y) => x.CompareTo(y));

            TestHelpers.AssertThrows<InvalidOperationException, int>(span, _ => _.Min());
            TestHelpers.AssertThrows<InvalidOperationException, int>(span, _ => _.Max());
            TestHelpers.AssertThrows<InvalidOperationException, int>(span, _ => _.Min(Comparer<int>.Default));
            TestHelpers.AssertThrows<InvalidOperationException, int>(span, _ => _.Max(Comparer<int>.Default));
            TestHelpers.AssertThrows<InvalidOperationException, int>(span, _ => _.Min(nonDefaultComparer));
            TestHelpers.AssertThrows<InvalidOperationException, int>(span, _ => _.Max(nonDefaultComparer));
        }

        [Fact]
        public static void MinMax_Empty_ReferenceAndNullableValueType_ReturnsNull()
        {
            ReadOnlySpan<string?> strings = ReadOnlySpan<string?>.Empty;
            ReadOnlySpan<int?> nullableInts = ReadOnlySpan<int?>.Empty;
            IComparer<string?> stringComparer = Comparer<string?>.Create((left, right) => string.CompareOrdinal(left, right));
            IComparer<int?> nullableIntComparer = Comparer<int?>.Create((left, right) => left.GetValueOrDefault().CompareTo(right.GetValueOrDefault()));

            Assert.Null(strings.Min());
            Assert.Null(strings.Max());
            Assert.Null(nullableInts.Min());
            Assert.Null(nullableInts.Max());

            Assert.Null(strings.Min(comparer: null));
            Assert.Null(strings.Max(comparer: null));
            Assert.Null(nullableInts.Min(comparer: null));
            Assert.Null(nullableInts.Max(comparer: null));

            Assert.Null(strings.Min(stringComparer));
            Assert.Null(strings.Max(stringComparer));
            Assert.Null(nullableInts.Min(nullableIntComparer));
            Assert.Null(nullableInts.Max(nullableIntComparer));
        }

        [Fact]
        public static void MinMax_AllNull_ReturnsNull()
        {
            ReadOnlySpan<string?> strings = new string?[] { null, null, null };
            ReadOnlySpan<int?> nullableInts = new int?[] { null, null, null };

            Assert.Null(strings.Min());
            Assert.Null(strings.Max());
            Assert.Null(nullableInts.Min());
            Assert.Null(nullableInts.Max());
        }

        [Fact]
        public static void MinMax_NullNotFirst_NullIgnoredForComparison()
        {
            ReadOnlySpan<string?> strings = new string?[] { "charlie", null, "bravo", null, "delta" };
            ReadOnlySpan<int?> nullableInts = new int?[] { 4, null, -1, null, 7 };
            IComparer<string?> stringComparer = Comparer<string?>.Create((left, right) => string.CompareOrdinal(left, right));
            IComparer<int?> nullableIntComparer = Comparer<int?>.Create((left, right) => left.GetValueOrDefault().CompareTo(right.GetValueOrDefault()));

            Assert.Equal("bravo", strings.Min());
            Assert.Equal("delta", strings.Max());
            Assert.Equal(-1, nullableInts.Min());
            Assert.Equal(7, nullableInts.Max());

            Assert.Equal("bravo", strings.Min(stringComparer));
            Assert.Equal("delta", strings.Max(stringComparer));
            Assert.Equal(-1, nullableInts.Min(nullableIntComparer));
            Assert.Equal(7, nullableInts.Max(nullableIntComparer));
        }

        [Fact]
        public static void MinMax_DefaultComparer_ProducesExpectedValues()
        {
            ReadOnlySpan<int> ints = new int[] { 4, -1, 7, 2 };
            ReadOnlySpan<string?> strings = new string?[] { null, "charlie", "bravo", null, "delta" };

            Assert.Equal(-1, ints.Min());
            Assert.Equal(7, ints.Max());

            Assert.Equal("bravo", strings.Min());
            Assert.Equal("delta", strings.Max());
        }

        [Fact]
        public static void MinMax_CustomComparer_IsUsed()
        {
            ReadOnlySpan<int> ints = new int[] { 4, -1, 7, 2 };
            IComparer<int> reverse = Comparer<int>.Create((left, right) => right.CompareTo(left));

            Assert.Equal(7, ints.Min(reverse));
            Assert.Equal(-1, ints.Max(reverse));
        }

        [Fact]
        public static void MinMax_IntegerTypes_DefaultAndNullComparer_ProduceExpectedValues()
        {
            AssertMinMaxValues(new byte[] { 12, 3, 255, 7 }, (byte)3, (byte)255);
            AssertMinMaxValues(new sbyte[] { 12, -9, 100, 7 }, (sbyte)-9, (sbyte)100);
            AssertMinMaxValues(new ushort[] { 12, 3, 65535, 7 }, (ushort)3, ushort.MaxValue);
            AssertMinMaxValues(new short[] { 12, -9, 100, 7 }, (short)-9, (short)100);
            AssertMinMaxValues(new char[] { 'x', 'b', 'm', 'z' }, 'b', 'z');
            AssertMinMaxValues(new uint[] { 12u, 3u, 400u, 7u }, 3u, 400u);
            AssertMinMaxValues(new int[] { 12, -9, 400, 7 }, -9, 400);
            AssertMinMaxValues(new ulong[] { 12ul, 3ul, 400ul, 7ul }, 3ul, 400ul);
            AssertMinMaxValues(new long[] { 12L, -9L, 400L, 7L }, -9L, 400L);
            AssertMinMaxValues(new nuint[] { (nuint)12, (nuint)3, (nuint)400, (nuint)7 }, (nuint)3, (nuint)400);
            AssertMinMaxValues(new nint[] { (nint)12, (nint)(-9), (nint)400, (nint)7 }, (nint)(-9), (nint)400);
            AssertMinMaxValues(new Int128[] { (Int128)12, (Int128)(-9), (Int128)400, (Int128)7 }, (Int128)(-9), (Int128)400);
            AssertMinMaxValues(new UInt128[] { (UInt128)12, (UInt128)3, (UInt128)400, (UInt128)7 }, (UInt128)3, (UInt128)400);
        }

        [Theory]
        [InlineData(float.NaN, 1f, 2f)]
        [InlineData(1f, float.NaN, 2f)]
        [InlineData(1f, 2f, float.NaN)]
        public static void MinMax_Float_NaN_UsesExpectedOrdering(float first, float second, float third)
        {
            float[] values = [first, second, third];
            ReadOnlySpan<float> span = values;

            Assert.True(float.IsNaN(span.Min()));
            Assert.True(float.IsNaN(span.Min(comparer: null)));
            Assert.Equal(2f, span.Max());
            Assert.Equal(2f, span.Max(comparer: null));
        }

        [Theory]
        [InlineData(double.NaN, 1d, 2d)]
        [InlineData(1d, double.NaN, 2d)]
        [InlineData(1d, 2d, double.NaN)]
        public static void MinMax_Double_NaN_UsesExpectedOrdering(double first, double second, double third)
        {
            double[] values = [first, second, third];
            ReadOnlySpan<double> span = values;

            Assert.True(double.IsNaN(span.Min()));
            Assert.True(double.IsNaN(span.Min(comparer: null)));
            Assert.Equal(2d, span.Max());
            Assert.Equal(2d, span.Max(comparer: null));
        }

        private static void AssertMinMaxValues<T>(T[] values, T expectedMin, T expectedMax)
            where T : IComparable<T>
        {
            ReadOnlySpan<T> span = values;
            IComparer<T> comparer = Comparer<T>.Create(static (left, right) => left.CompareTo(right));
            IComparer<T> reverseComparer = Comparer<T>.Create(static (left, right) => right.CompareTo(left));

            Assert.Equal(expectedMin, span.Min());
            Assert.Equal(expectedMax, span.Max());
            Assert.Equal(expectedMin, span.Min(comparer: null));
            Assert.Equal(expectedMax, span.Max(comparer: null));
            Assert.Equal(expectedMin, span.Min(comparer));
            Assert.Equal(expectedMax, span.Max(comparer));
            Assert.Equal(expectedMax, span.Min(reverseComparer));
            Assert.Equal(expectedMin, span.Max(reverseComparer));
        }
    }
}
