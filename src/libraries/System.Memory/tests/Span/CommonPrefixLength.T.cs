// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography.X509Certificates;
using Xunit;

namespace System.SpanTests
{
    public static class CommonPrefixLengthTests
    {
        [Theory]
        [InlineData(0, 0)]
        [InlineData(1, 0)]
        [InlineData(0, 1)]
        [InlineData(2, 0)]
        [InlineData(0, 2)]
        public static void OneOrBothZeroLength_Returns0(int length1, int length2)
        {
            ValidateWithDefaultValues(length1, length2, NonDefaultEqualityComparer<char>.Instance, 0);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(15)]
        public static void SameLengthAllEqual_ReturnsLength(int length)
        {
            ValidateWithDefaultValues(length, length, NonDefaultEqualityComparer<char>.Instance, length);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(15)]
        public static void FirstShorterAllEqual_ReturnsFirstLength(int length)
        {
            ValidateWithDefaultValues(length, length + 1, NonDefaultEqualityComparer<char>.Instance, length);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(15)]
        public static void SecondShorterAllEqual_ReturnsSecondLength(int length)
        {
            ValidateWithDefaultValues(length + 1, length, NonDefaultEqualityComparer<char>.Instance, length);
        }

        private static void ValidateWithDefaultValues<T>(int length1, int length2, IEqualityComparer<T> customComparer, int expected)
        {
            Assert.Equal(expected, MemoryExtensions.CommonPrefixLength((ReadOnlySpan<T>)new T[length1], new T[length2]));
            Assert.Equal(expected, MemoryExtensions.CommonPrefixLength((ReadOnlySpan<T>)new T[length1], new T[length2], null));
            Assert.Equal(expected, MemoryExtensions.CommonPrefixLength((ReadOnlySpan<T>)new T[length1], new T[length2], EqualityComparer<T>.Default));
            Assert.Equal(expected, MemoryExtensions.CommonPrefixLength((ReadOnlySpan<T>)new T[length1], new T[length2], customComparer));

            Assert.Equal(expected, MemoryExtensions.CommonPrefixLength((Span<T>)new T[length1], new T[length2]));
            Assert.Equal(expected, MemoryExtensions.CommonPrefixLength((Span<T>)new T[length1], new T[length2], null));
            Assert.Equal(expected, MemoryExtensions.CommonPrefixLength((Span<T>)new T[length1], new T[length2], EqualityComparer<T>.Default));
            Assert.Equal(expected, MemoryExtensions.CommonPrefixLength((Span<T>)new T[length1], new T[length2], customComparer));
        }

        [Fact]
        public static void PartialEquals_ReturnsPrefixLength_ValueType()
        {
            byte[] arr1 = new byte[] { 1, 2, 3, 4, 5 };
            byte[] arr2 = new byte[] { 1, 2, 3, 6, 7 };

            Assert.Equal(3, MemoryExtensions.CommonPrefixLength((ReadOnlySpan<byte>)arr1, arr2));
            Assert.Equal(3, MemoryExtensions.CommonPrefixLength((ReadOnlySpan<byte>)arr1, arr2, null));
            Assert.Equal(3, MemoryExtensions.CommonPrefixLength((ReadOnlySpan<byte>)arr1, arr2, EqualityComparer<byte>.Default));
            Assert.Equal(3, MemoryExtensions.CommonPrefixLength((ReadOnlySpan<byte>)arr1, arr2, NonDefaultEqualityComparer<byte>.Instance));

            Assert.Equal(3, MemoryExtensions.CommonPrefixLength((Span<byte>)arr1, arr2));
            Assert.Equal(3, MemoryExtensions.CommonPrefixLength((Span<byte>)arr1, arr2, null));
            Assert.Equal(3, MemoryExtensions.CommonPrefixLength((Span<byte>)arr1, arr2, EqualityComparer<byte>.Default));
            Assert.Equal(3, MemoryExtensions.CommonPrefixLength((Span<byte>)arr1, arr2, NonDefaultEqualityComparer<byte>.Instance));
        }

        [Fact]
        public static void PartialEquals_ReturnsPrefixLength_ReferenceType()
        {
            string[] arr1 = new string[] { null, "a", null, "b", "c", "d", "e" };
            string[] arr2 = new string[] { null, "a", null, "b", "f", "g", "h" };

            Assert.Equal(4, MemoryExtensions.CommonPrefixLength((ReadOnlySpan<string>)arr1, arr2));
            Assert.Equal(4, MemoryExtensions.CommonPrefixLength((ReadOnlySpan<string>)arr1, arr2, null));
            Assert.Equal(4, MemoryExtensions.CommonPrefixLength((ReadOnlySpan<string>)arr1, arr2, EqualityComparer<string>.Default));
            Assert.Equal(4, MemoryExtensions.CommonPrefixLength((ReadOnlySpan<string>)arr1, arr2, NonDefaultEqualityComparer<string>.Instance));

            Assert.Equal(4, MemoryExtensions.CommonPrefixLength((Span<string>)arr1, arr2));
            Assert.Equal(4, MemoryExtensions.CommonPrefixLength((Span<string>)arr1, arr2, null));
            Assert.Equal(4, MemoryExtensions.CommonPrefixLength((Span<string>)arr1, arr2, EqualityComparer<string>.Default));
            Assert.Equal(4, MemoryExtensions.CommonPrefixLength((Span<string>)arr1, arr2, NonDefaultEqualityComparer<string>.Instance));
        }

        [Fact]
        public static void Comparer_UsedInComparisons_ReferenceType()
        {
            string[] arr1 = new string[] { null, "a", null, "b", "c", "d", "e" };
            string[] arr2 = new string[] { null, "A", null, "B", "F", "G", "H" };

            Assert.Equal(4, MemoryExtensions.CommonPrefixLength((ReadOnlySpan<string>)arr1, arr2, StringComparer.OrdinalIgnoreCase));
            Assert.Equal(4, MemoryExtensions.CommonPrefixLength((Span<string>)arr1, arr2, StringComparer.OrdinalIgnoreCase));

            Assert.Equal(1, MemoryExtensions.CommonPrefixLength((ReadOnlySpan<string>)arr1, arr2, NonDefaultEqualityComparer<string>.Instance));
            Assert.Equal(1, MemoryExtensions.CommonPrefixLength((Span<string>)arr1, arr2, NonDefaultEqualityComparer<string>.Instance));

            Assert.Equal(1, MemoryExtensions.CommonPrefixLength((ReadOnlySpan<string>)arr1, arr2, EqualityComparer<string>.Default));
            Assert.Equal(1, MemoryExtensions.CommonPrefixLength((Span<string>)arr1, arr2, EqualityComparer<string>.Default));
        }

        [Fact]
        public static void Comparer_UsedInComparisons_ValueType()
        {
            int[] arr1 = new[] { 1, 2, 3, 4, 5, 6 };
            int[] arr2 = new[] { -1, 2, -3, 4, -7, -8 };

            Assert.Equal(4, MemoryExtensions.CommonPrefixLength((ReadOnlySpan<int>)arr1, arr2, AbsoluteValueComparer.Instance));
            Assert.Equal(4, MemoryExtensions.CommonPrefixLength((Span<int>)arr1, arr2, AbsoluteValueComparer.Instance));

            Assert.Equal(0, MemoryExtensions.CommonPrefixLength((ReadOnlySpan<int>)arr1, arr2, NonDefaultEqualityComparer<int>.Instance));
            Assert.Equal(0, MemoryExtensions.CommonPrefixLength((Span<int>)arr1, arr2, NonDefaultEqualityComparer<int>.Instance));

            Assert.Equal(0, MemoryExtensions.CommonPrefixLength((ReadOnlySpan<int>)arr1, arr2, EqualityComparer<int>.Default));
            Assert.Equal(0, MemoryExtensions.CommonPrefixLength((Span<int>)arr1, arr2, EqualityComparer<int>.Default));
        }

        private sealed class NonDefaultEqualityComparer<T> : IEqualityComparer<T>
        {
            public static NonDefaultEqualityComparer<T> Instance { get; } = new NonDefaultEqualityComparer<T>();
            public bool Equals(T? x, T? y) => EqualityComparer<T>.Default.Equals(x, y);
            public int GetHashCode([DisallowNull] T obj) => EqualityComparer<T>.Default.GetHashCode(obj);
        }

        private sealed class AbsoluteValueComparer : IEqualityComparer<int>
        {
            public static AbsoluteValueComparer Instance { get; } = new AbsoluteValueComparer();
            public bool Equals(int x, int y) => Math.Abs(x) == Math.Abs(y);
            public int GetHashCode(int x) => Math.Abs(x).GetHashCode();
        }
    }
}
