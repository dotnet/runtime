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
            Assert.Equal(0, MemoryExtensions.CommonPrefixLength((ReadOnlySpan<char>)new char[length1], new char[length2]));
            Assert.Equal(0, MemoryExtensions.CommonPrefixLength((ReadOnlySpan<char>)new char[length1], new char[length2], null));
            Assert.Equal(0, MemoryExtensions.CommonPrefixLength((ReadOnlySpan<char>)new char[length1], new char[length2], EqualityComparer<char>.Default));
            Assert.Equal(0, MemoryExtensions.CommonPrefixLength((ReadOnlySpan<char>)new char[length1], new char[length2], NonDefaultEqualityComparer<char>.Instance));

            Assert.Equal(0, MemoryExtensions.CommonPrefixLength((Span<char>)new char[length1], new char[length2]));
            Assert.Equal(0, MemoryExtensions.CommonPrefixLength((Span<char>)new char[length1], new char[length2], null));
            Assert.Equal(0, MemoryExtensions.CommonPrefixLength((Span<char>)new char[length1], new char[length2], EqualityComparer<char>.Default));
            Assert.Equal(0, MemoryExtensions.CommonPrefixLength((Span<char>)new char[length1], new char[length2], NonDefaultEqualityComparer<char>.Instance));
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(15)]
        public static void SameLengthAllEqual_ReturnsLength(int length)
        {
            Assert.Equal(length, MemoryExtensions.CommonPrefixLength((ReadOnlySpan<char>)new char[length], new char[length]));
            Assert.Equal(length, MemoryExtensions.CommonPrefixLength((ReadOnlySpan<char>)new char[length], new char[length], null));
            Assert.Equal(length, MemoryExtensions.CommonPrefixLength((ReadOnlySpan<char>)new char[length], new char[length], EqualityComparer<char>.Default));
            Assert.Equal(length, MemoryExtensions.CommonPrefixLength((ReadOnlySpan<char>)new char[length], new char[length], NonDefaultEqualityComparer<char>.Instance));

            Assert.Equal(length, MemoryExtensions.CommonPrefixLength((Span<char>)new char[length], new char[length]));
            Assert.Equal(length, MemoryExtensions.CommonPrefixLength((Span<char>)new char[length], new char[length], null));
            Assert.Equal(length, MemoryExtensions.CommonPrefixLength((Span<char>)new char[length], new char[length], EqualityComparer<char>.Default));
            Assert.Equal(length, MemoryExtensions.CommonPrefixLength((Span<char>)new char[length], new char[length], NonDefaultEqualityComparer<char>.Instance));
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(15)]
        public static void FirstShorterAllEqual_ReturnsFirstLength(int length)
        {
            Assert.Equal(length, MemoryExtensions.CommonPrefixLength((ReadOnlySpan<char>)new char[length], new char[length + 1]));
            Assert.Equal(length, MemoryExtensions.CommonPrefixLength((ReadOnlySpan<char>)new char[length], new char[length + 1], null));
            Assert.Equal(length, MemoryExtensions.CommonPrefixLength((ReadOnlySpan<char>)new char[length], new char[length + 1], EqualityComparer<char>.Default));
            Assert.Equal(length, MemoryExtensions.CommonPrefixLength((ReadOnlySpan<char>)new char[length], new char[length + 1], NonDefaultEqualityComparer<char>.Instance));

            Assert.Equal(length, MemoryExtensions.CommonPrefixLength((Span<char>)new char[length], new char[length + 1]));
            Assert.Equal(length, MemoryExtensions.CommonPrefixLength((Span<char>)new char[length], new char[length + 1], null));
            Assert.Equal(length, MemoryExtensions.CommonPrefixLength((Span<char>)new char[length], new char[length + 1], EqualityComparer<char>.Default));
            Assert.Equal(length, MemoryExtensions.CommonPrefixLength((Span<char>)new char[length], new char[length + 1], NonDefaultEqualityComparer<char>.Instance));
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(15)]
        public static void SecondShorterAllEqual_ReturnsSecondLength(int length)
        {
            Assert.Equal(length, MemoryExtensions.CommonPrefixLength((ReadOnlySpan<char>)new char[length + 1], new char[length]));
            Assert.Equal(length, MemoryExtensions.CommonPrefixLength((ReadOnlySpan<char>)new char[length + 1], new char[length], null));
            Assert.Equal(length, MemoryExtensions.CommonPrefixLength((ReadOnlySpan<char>)new char[length + 1], new char[length], EqualityComparer<char>.Default));
            Assert.Equal(length, MemoryExtensions.CommonPrefixLength((ReadOnlySpan<char>)new char[length + 1], new char[length], NonDefaultEqualityComparer<char>.Instance));

            Assert.Equal(length, MemoryExtensions.CommonPrefixLength((Span<char>)new char[length + 1], new char[length]));
            Assert.Equal(length, MemoryExtensions.CommonPrefixLength((Span<char>)new char[length + 1], new char[length], null));
            Assert.Equal(length, MemoryExtensions.CommonPrefixLength((Span<char>)new char[length + 1], new char[length], EqualityComparer<char>.Default));
            Assert.Equal(length, MemoryExtensions.CommonPrefixLength((Span<char>)new char[length + 1], new char[length], NonDefaultEqualityComparer<char>.Instance));
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

        private sealed class NonDefaultEqualityComparer<T> : IEqualityComparer<T>
        {
            public static NonDefaultEqualityComparer<T> Instance { get; } = new NonDefaultEqualityComparer<T>();
            public bool Equals(T? x, T? y) => EqualityComparer<T>.Default.Equals(x, y);
            public int GetHashCode([DisallowNull] T obj) => EqualityComparer<T>.Default.GetHashCode(obj);
        }
    }
}
