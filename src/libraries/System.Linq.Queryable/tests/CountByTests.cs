// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq.Expressions;
using Xunit;

namespace System.Linq.Tests
{
    public class CountByTests : EnumerableBasedTests
    {
        [Fact]
        public void NullSource_ThrowsArgumentNullException()
        {
            IQueryable<int> source = null;

            AssertExtensions.Throws<ArgumentNullException>("source", () => source.CountBy(x => x));
            AssertExtensions.Throws<ArgumentNullException>("source", () => source.CountBy(x => x, EqualityComparer<int>.Default));
        }

        [Fact]
        public void NullKeySelector_ThrowsArgumentNullException()
        {
            IQueryable<int> source = Enumerable.Empty<int>().AsQueryable();
            Expression<Func<int, int>> keySelector = null;

            AssertExtensions.Throws<ArgumentNullException>("keySelector", () => source.CountBy(keySelector));
            AssertExtensions.Throws<ArgumentNullException>("keySelector", () => source.CountBy(keySelector, EqualityComparer<int>.Default));
        }

        [Fact]
        public void EmptySource()
        {
            int[] source = { };
            Assert.Empty(source.AsQueryable().CountBy(x => x));
        }

        [Fact]
        public void CountBy()
        {
            string[] source = { "now", "own", "won" };
            var counts = source.AsQueryable().CountBy(x => x).ToArray();
            Assert.Equal(source.Length, counts.Length);
            Assert.Equal(source, counts.Select(x => x.Key).ToArray());
            Assert.All(counts, x => Assert.Equal(1, x.Value));
        }

        [Fact]
        public void CountBy_CustomKeySelector()
        {
            string[] source = { "now", "own", "won" };
            var counts = source.AsQueryable().CountBy(x => string.Concat(x.Order())).ToArray();
            var count = Assert.Single(counts);
            Assert.Equal(source[0], count.Key);
            Assert.Equal(source.Length, count.Value);
        }

        [Fact]
        public void CountBy_CustomComparison()
        {
            string[] source = { "now", "own", "won" };
            var counts = source.AsQueryable().CountBy(x => x, new AnagramEqualityComparer()).ToArray();
            var count = Assert.Single(counts);
            Assert.Equal(source[0], count.Key);
            Assert.Equal(source.Length, count.Value);
        }
    }
}
