// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.SpanTests
{
    public static class CommonSuffixLengthTests
    {
        [Theory]
        [InlineData("", "", 0)]
        [InlineData("abc", "", 0)]
        [InlineData("", "abc", 0)]
        [InlineData("abc", "xyz", 0)]
        [InlineData("abc", "xbc", 2)]
        [InlineData("abc", "abc", 3)]
        [InlineData("xabc", "abc", 3)]
        [InlineData("abc", "xabc", 3)]
        [InlineData("abcdef", "xyzdef", 3)]
        public static void Characters(string first, string second, int expected)
        {
            ReadOnlySpan<char> span = first.AsSpan();
            ReadOnlySpan<char> other = second.AsSpan();

            Assert.Equal(expected, MemoryExtensions.CommonSuffixLength(span, other));
            Assert.Equal(expected, MemoryExtensions.CommonSuffixLength(span, other, null));
            Assert.Equal(expected, MemoryExtensions.CommonSuffixLength(span, other, EqualityComparer<char>.Default));
        }

        [Fact]
        public static void ValueTypesAndCustomComparer()
        {
            int[] first = { 1, 2, 3, 4, 5 };
            int[] second = { 9, 3, 4, 5 };

            Assert.Equal(3, MemoryExtensions.CommonSuffixLength((ReadOnlySpan<int>)first, second));
            Assert.Equal(3, MemoryExtensions.CommonSuffixLength((ReadOnlySpan<int>)first, second, null));
            Assert.Equal(3, MemoryExtensions.CommonSuffixLength((ReadOnlySpan<int>)first, second, EqualityComparer<int>.Default));

            second = new[] { 9, -3, -4, -5 };
            EqualityComparer<int> absoluteValueComparer = EqualityComparer<int>.Create((x, y) => Math.Abs(x) == Math.Abs(y));
            Assert.Equal(3, MemoryExtensions.CommonSuffixLength((ReadOnlySpan<int>)first, second, absoluteValueComparer));
            Assert.Equal(0, MemoryExtensions.CommonSuffixLength((ReadOnlySpan<int>)first, second));
        }

        [Fact]
        public static void ReferenceTypesAndCustomComparer()
        {
            string[] first = { "start", "A", null, "end" };
            string[] second = { "A", null, "end" };

            Assert.Equal(3, MemoryExtensions.CommonSuffixLength((ReadOnlySpan<string>)first, second));
            Assert.Equal(3, MemoryExtensions.CommonSuffixLength((ReadOnlySpan<string>)first, second, null));

            second = new[] { "a", null, "END" };
            Assert.Equal(3, MemoryExtensions.CommonSuffixLength((ReadOnlySpan<string>)first, second, StringComparer.OrdinalIgnoreCase));
            Assert.Equal(0, MemoryExtensions.CommonSuffixLength((ReadOnlySpan<string>)first, second));
        }

        [Fact]
        public static void FloatingPointUsesEqualityComparerSemantics()
        {
            double[] first = { 1.0, double.NaN };
            double[] second = { 2.0, double.NaN };

            Assert.Equal(1, MemoryExtensions.CommonSuffixLength((ReadOnlySpan<double>)first, second));
            Assert.Equal(1, MemoryExtensions.CommonSuffixLength((ReadOnlySpan<double>)first, second, EqualityComparer<double>.Default));
        }
    }
}
