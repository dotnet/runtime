// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Linq.Tests
{
    public sealed class OrderTests : EnumerableTests
    {
        [Fact]
        public void Order()
        {
            int[] source = { 9, 1, 3, 2, 5, 4, 6, 7, 8, 0 };
            int[] expected = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };

            Assert.Equal(expected, source.Order().ToArray());
        }

        [Fact]
        public void SourceEmpty()
        {
            int[] source = { };
            Assert.Empty(source.Order());
        }

        [Fact]
        public void OrderedCount()
        {
            var source = Enumerable.Range(0, 20).Shuffle();
            Assert.Equal(20, source.Order().Count());
        }

        [Fact]
        public void ElementsAllSameKey()
        {
            int?[] source = { 9, 9, 9, 9, 9, 9 };
            int?[] expected = { 9, 9, 9, 9, 9, 9 };

            Assert.Equal(expected, source.Order());
        }
    }
}
