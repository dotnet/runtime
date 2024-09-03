// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq.Expressions;
using Xunit;

namespace System.Linq.Tests
{
    public class AggregateByTests : EnumerableBasedTests
    {
        [Fact]
        public void NullSource_ThrowsArgumentNullException()
        {
            IQueryable<int> source = null;

            AssertExtensions.Throws<ArgumentNullException>("source", () => source.AggregateBy(x => x, x => 0, (x, y) => x+ y));
            AssertExtensions.Throws<ArgumentNullException>("source", () => source.AggregateBy(x => x, x => 0, (x, y) => x + y, EqualityComparer<int>.Default));
        }

        [Fact]
        public void NullKeySelector_ThrowsArgumentNullException()
        {
            IQueryable<int> source = Enumerable.Empty<int>().AsQueryable();
            Expression<Func<int, int>> keySelector = null;

            AssertExtensions.Throws<ArgumentNullException>("keySelector", () => source.AggregateBy(keySelector, x => 0, (x, y) => x + y));
            AssertExtensions.Throws<ArgumentNullException>("keySelector", () => source.AggregateBy(keySelector, x => 0, (x, y) => x + y, EqualityComparer<int>.Default));
        }

        [Fact]
        public void NullSeedSelector_ThrowsArgumentNullException()
        {
            IQueryable<int> source = Enumerable.Empty<int>().AsQueryable();
            Expression<Func<int, int>> seedSelector = null;

            AssertExtensions.Throws<ArgumentNullException>("seedSelector", () => source.AggregateBy(x => x, seedSelector, (x, y) => x + y));
            AssertExtensions.Throws<ArgumentNullException>("seedSelector", () => source.AggregateBy(x => x, seedSelector, (x, y) => x + y, EqualityComparer<int>.Default));
        }

        [Fact]
        public void NullFunc_ThrowsArgumentNullException()
        {
            IQueryable<int> source = Enumerable.Empty<int>().AsQueryable();
            Expression<Func<int, int, int>> func = null;

            AssertExtensions.Throws<ArgumentNullException>("func", () => source.AggregateBy(x => x, x => 0, func));
            AssertExtensions.Throws<ArgumentNullException>("func", () => source.AggregateBy(x => x, x => 0, func, EqualityComparer<int>.Default));
        }

        [Fact]
        public void EmptySource()
        {
            int[] source = { };
            Assert.Empty(source.AsQueryable().AggregateBy(x => x, x => 0, (x, y) => x + y));
        }

        [Fact]
        public void AggregateBy()
        {
            string[] source = { "now", "own", "won" };
            var counts = source.AsQueryable().AggregateBy(x => x, 0, (x, _) => x + 1).ToArray();
            Assert.Equal(source.Length, counts.Length);
            Assert.Equal(source, counts.Select(x => x.Key).ToArray());
            Assert.All(counts, x => Assert.Equal(1, x.Value));
        }

        [Fact]
        public void AggregateBy_CustomKeySelector()
        {
            string[] source = { "now", "own", "won" };
            var counts = source.AsQueryable().AggregateBy(x => string.Concat(x.Order()), 0, (x, _) => x + 1).ToArray();
            var count = Assert.Single(counts);
            Assert.Equal(source[0], count.Key);
            Assert.Equal(source.Length, count.Value);
        }

        [Fact]
        public void AggregateBy_CustomComparison()
        {
            string[] source = { "now", "own", "won" };
            var counts = source.AsQueryable().AggregateBy(x => x, 0, (x, _) => x + 1, new AnagramEqualityComparer()).ToArray();
            var count = Assert.Single(counts);
            Assert.Equal(source[0], count.Key);
            Assert.Equal(source.Length, count.Value);
        }
    }
}
