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

            TestHelpers.AssertThrows<InvalidOperationException, int>(span, (_span) => _span.Min());
            TestHelpers.AssertThrows<InvalidOperationException, int>(span, (_span) => _span.Max());
            TestHelpers.AssertThrows<InvalidOperationException, int>(span, (_span) => _span.Min(Comparer<int>.Default));
            TestHelpers.AssertThrows<InvalidOperationException, int>(span, (_span) => _span.Max(Comparer<int>.Default));
        }

        [Fact]
        public static void MinMax_NullComparer_ThrowsArgumentNullException()
        {
            ReadOnlySpan<int> span = new int[] { 4, -1, 7, 2 };

            TestHelpers.AssertThrows<ArgumentNullException, int>(span, (_span) => _span.Min(comparer: null!));
            TestHelpers.AssertThrows<ArgumentNullException, int>(span, (_span) => _span.Max(comparer: null!));
        }

        [Fact]
        public static void MinMax_Empty_ReferenceAndNullableValueType_ReturnsNull()
        {
            ReadOnlySpan<string?> strings = ReadOnlySpan<string?>.Empty;
            ReadOnlySpan<int?> nullableInts = ReadOnlySpan<int?>.Empty;

            Assert.Null(strings.Min());
            Assert.Null(strings.Max());
            Assert.Null(nullableInts.Min());
            Assert.Null(nullableInts.Max());
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

            Assert.Equal("bravo", strings.Min());
            Assert.Equal("delta", strings.Max());
            Assert.Equal(-1, nullableInts.Min());
            Assert.Equal(7, nullableInts.Max());
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
    }
}
