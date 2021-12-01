// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq.Expressions;
using Xunit;

namespace System.Linq.Tests
{
    public class IntersectTests : EnumerableBasedTests
    {
        [Fact]
        public void BothEmpty()
        {
            int[] first = { };
            int[] second = { };
            Assert.Empty(first.AsQueryable().Intersect(second.AsQueryable()));
        }

        [Fact]
        public void FirstNullCustomComparer()
        {
            IQueryable<string> first = null;
            string[] second = { "ekiM", "bBo" };

            var ane = AssertExtensions.Throws<ArgumentNullException>("source1", () => first.Intersect(second.AsQueryable(), new AnagramEqualityComparer()));
        }

        [Fact]
        public void SecondNullCustomComparer()
        {
            string[] first = { "Tim", "Bob", "Mike", "Robert" };
            IQueryable<string> second = null;

            var ane = AssertExtensions.Throws<ArgumentNullException>("source2", () => first.AsQueryable().Intersect(second, new AnagramEqualityComparer()));
        }

        [Fact]
        public void FirstNullNoComparer()
        {
            IQueryable<string> first = null;
            string[] second = { "ekiM", "bBo" };

            var ane = AssertExtensions.Throws<ArgumentNullException>("source1", () => first.Intersect(second.AsQueryable()));
        }

        [Fact]
        public void SecondNullNoComparer()
        {
            string[] first = { "Tim", "Bob", "Mike", "Robert" };
            IQueryable<string> second = null;

            var ane = AssertExtensions.Throws<ArgumentNullException>("source2", () => first.AsQueryable().Intersect(second));
        }

        [Fact]
        public void SingleNullWithEmpty()
        {
            string[] first = { null };
            string[] second = new string[0];
            Assert.Empty(first.AsQueryable().Intersect(second.AsQueryable(), EqualityComparer<string>.Default));
        }

        [Fact]
        public void CustomComparer()
        {
            string[] first = { "Tim", "Bob", "Mike", "Robert" };
            string[] second = { "ekiM", "bBo" };
            string[] expected = { "Bob", "Mike" };

            Assert.Equal(expected, first.AsQueryable().Intersect(second.AsQueryable(), new AnagramEqualityComparer()));
        }

        [Fact]
        public void Intersect1()
        {
            var count = (new int[] { 0, 1, 2 }).AsQueryable().Intersect((new int[] { 1, 2, 3 }).AsQueryable()).Count();
            Assert.Equal(2, count);
        }

        [Fact]
        public void Intersect2()
        {
            var count = (new int[] { 0, 1, 2 }).AsQueryable().Intersect((new int[] { 1, 2, 3 }).AsQueryable(), EqualityComparer<int>.Default).Count();
            Assert.Equal(2, count);
        }

        [Fact]
        public void IntersectBy_NullSource1_ThrowsArgumentNullException()
        {
            IQueryable<int> source1 = null;

            AssertExtensions.Throws<ArgumentNullException>("source1", () => source1.IntersectBy(Enumerable.Empty<int>(), x => x));
            AssertExtensions.Throws<ArgumentNullException>("source1", () => source1.IntersectBy(Enumerable.Empty<int>(), x => x, EqualityComparer<int>.Default));
        }

        [Fact]
        public void IntersectBy_NullSource2_ThrowsArgumentNullException()
        {
            IQueryable<int> source1 = Enumerable.Empty<int>().AsQueryable();
            IQueryable<int> source2 = null;

            AssertExtensions.Throws<ArgumentNullException>("source2", () => source1.IntersectBy(source2, x => x));
            AssertExtensions.Throws<ArgumentNullException>("source2", () => source1.IntersectBy(source2, x => x, EqualityComparer<int>.Default));
        }

        [Fact]
        public void IntersectBy_NullKeySelector_ThrowsArgumentNullException()
        {
            IQueryable<int> source = Enumerable.Empty<int>().AsQueryable();
            Expression<Func<int, int>> keySelector = null;

            AssertExtensions.Throws<ArgumentNullException>("keySelector", () => source.IntersectBy(source, keySelector));
            AssertExtensions.Throws<ArgumentNullException>("keySelector", () => source.IntersectBy(source, keySelector, EqualityComparer<int>.Default));
        }

        [Fact]
        public void IntersectBy()
        {
            var expected = Enumerable.Range(5, 5);
            var actual = Enumerable.Range(0, 10).AsQueryable().IntersectBy(Enumerable.Range(5, 20), x => x).ToArray();
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void IntersectBy_CustomComparison()
        {
            var expected = Enumerable.Range(5, 5);
            var actual = Enumerable.Range(0, 10).AsQueryable().IntersectBy(Enumerable.Range(5, 20), x => x, EqualityComparer<int>.Default).ToArray();
            Assert.Equal(expected, actual);
        }
    }
}
