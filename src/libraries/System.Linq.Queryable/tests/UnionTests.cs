// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq.Expressions;
using Xunit;

namespace System.Linq.Tests
{
    public class UnionTests : EnumerableBasedTests
    {
        [Fact]
        public void CustomComparer()
        {
            string[] first = { "Bob", "Robert", "Tim", "Matt", "miT" };
            string[] second = { "ttaM", "Charlie", "Bbo" };
            string[] expected = { "Bob", "Robert", "Tim", "Matt", "Charlie" };

            var comparer = new AnagramEqualityComparer();

            Assert.Equal(expected, first.AsQueryable().Union(second.AsQueryable(), comparer), comparer);
        }

        [Fact]
        public void FirstNullCustomComparer()
        {
            IQueryable<string> first = null;
            string[] second = { "ttaM", "Charlie", "Bbo" };

            var ane = AssertExtensions.Throws<ArgumentNullException>("source1", () => first.Union(second.AsQueryable(), new AnagramEqualityComparer()));
        }

        [Fact]
        public void SecondNullCustomComparer()
        {
            string[] first = { "Bob", "Robert", "Tim", "Matt", "miT" };
            IQueryable<string> second = null;

            var ane = AssertExtensions.Throws<ArgumentNullException>("source2", () => first.AsQueryable().Union(second, new AnagramEqualityComparer()));
        }

        [Fact]
        public void FirstNullNoComparer()
        {
            IQueryable<string> first = null;
            string[] second = { "ttaM", "Charlie", "Bbo" };

            var ane = AssertExtensions.Throws<ArgumentNullException>("source1", () => first.Union(second.AsQueryable()));
        }

        [Fact]
        public void SecondNullNoComparer()
        {
            string[] first = { "Bob", "Robert", "Tim", "Matt", "miT" };
            IQueryable<string> second = null;

            var ane = AssertExtensions.Throws<ArgumentNullException>("source2", () => first.AsQueryable().Union(second));
        }

        [Fact]
        public void CommonElementsShared()
        {
            int[] first = { 1, 2, 3, 4, 5, 6 };
            int[] second = { 6, 7, 7, 7, 8, 1 };
            int[] expected = { 1, 2, 3, 4, 5, 6, 7, 8 };

            Assert.Equal(expected, first.AsQueryable().Union(second.AsQueryable()));
        }

        [Fact]
        public void Union1()
        {
            var count = (new int[] { 0, 1, 2 }).AsQueryable().Union((new int[] { 1, 2, 3 }).AsQueryable()).Count();
            Assert.Equal(4, count);
        }

        [Fact]
        public void Union2()
        {
            var count = (new int[] { 0, 1, 2 }).AsQueryable().Union((new int[] { 1, 2, 3 }).AsQueryable(), EqualityComparer<int>.Default).Count();
            Assert.Equal(4, count);
        }

        [Fact]
        public void UnionBy_NullSource1_ThrowsArgumentNullException()
        {
            IQueryable<int> source1 = null;

            AssertExtensions.Throws<ArgumentNullException>("source1", () => source1.UnionBy(Enumerable.Empty<int>(), x => x));
            AssertExtensions.Throws<ArgumentNullException>("source1", () => source1.UnionBy(Enumerable.Empty<int>(), x => x, EqualityComparer<int>.Default));
        }

        [Fact]
        public void UnionBy_NullSource2_ThrowsArgumentNullException()
        {
            IQueryable<int> source1 = Enumerable.Empty<int>().AsQueryable();
            IQueryable<int> source2 = null;

            AssertExtensions.Throws<ArgumentNullException>("source2", () => source1.UnionBy(source2, x => x));
            AssertExtensions.Throws<ArgumentNullException>("source2", () => source1.UnionBy(source2, x => x, EqualityComparer<int>.Default));
        }

        [Fact]
        public void UnionBy_NullKeySelector_ThrowsArgumentNullException()
        {
            IQueryable<int> source = Enumerable.Empty<int>().AsQueryable();
            Expression<Func<int, int>> keySelector = null;

            AssertExtensions.Throws<ArgumentNullException>("keySelector", () => source.UnionBy(source, keySelector));
            AssertExtensions.Throws<ArgumentNullException>("keySelector", () => source.UnionBy(source, keySelector, EqualityComparer<int>.Default));
        }

        [Fact]
        public void UnionBy()
        {
            var expected = Enumerable.Range(0, 10);
            var actual = Enumerable.Range(0, 5).AsQueryable().UnionBy(Enumerable.Range(5, 5), x => x).ToArray();
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void UnionBy_CustomComparison()
        {
            var expected = Enumerable.Range(0, 10);
            var actual = Enumerable.Range(0, 5).AsQueryable().UnionBy(Enumerable.Range(5, 5), x => x, EqualityComparer<int>.Default).ToArray();
            Assert.Equal(expected, actual);
        }
    }
}
