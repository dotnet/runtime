// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq.Expressions;
using Xunit;

namespace System.Linq.Tests
{
    public class ExceptTests : EnumerableBasedTests
    {
        [Fact]
        public void BothEmpty()
        {
            int[] first = { };
            int[] second = { };
            Assert.Empty(first.AsQueryable().Except(second.AsQueryable()));
        }

        [Fact]
        public void EachHasRepeatsBetweenAndAmongstThemselves()
        {
            int?[] first = { 1, 2, 2, 3, 4, 5 };
            int?[] second = { 5, 3, 2, 6, 6, 3, 1, null, null };
            int?[] expected = { 4 };

            Assert.Equal(expected, first.AsQueryable().Except(second.AsQueryable()));
        }

        [Fact]
        public void NullEqualityComparer()
        {
            string[] first = { "Bob", "Tim", "Robert", "Chris" };
            string[] second = { "bBo", "shriC" };
            string[] expected = { "Bob", "Tim", "Robert", "Chris" };

            Assert.Equal(expected, first.AsQueryable().Except(second.AsQueryable(), null));
        }

        [Fact]
        public void FirstNullCustomComparer()
        {
            IQueryable<string> first = null;
            string[] second = { "bBo", "shriC" };

            AssertExtensions.Throws<ArgumentNullException>("source1", () => first.Except(second.AsQueryable(), new AnagramEqualityComparer()));
        }

        [Fact]
        public void SecondNullCustomComparer()
        {
            string[] first = { "Bob", "Tim", "Robert", "Chris" };
            IQueryable<string> second = null;

            AssertExtensions.Throws<ArgumentNullException>("source2", () => first.AsQueryable().Except(second, new AnagramEqualityComparer()));
        }

        [Fact]
        public void FirstNullNoComparer()
        {
            IQueryable<string> first = null;
            string[] second = { "bBo", "shriC" };

            AssertExtensions.Throws<ArgumentNullException>("source1", () => first.Except(second.AsQueryable()));
        }

        [Fact]
        public void SecondNullNoComparer()
        {
            string[] first = { "Bob", "Tim", "Robert", "Chris" };
            IQueryable<string> second = null;

            AssertExtensions.Throws<ArgumentNullException>("source2", () => first.AsQueryable().Except(second));
        }

        [Fact]
        public void Except1()
        {
            var count = (new int[] { 0, 1, 2 }).AsQueryable().Except((new int[] { 1, 2, 3 }).AsQueryable()).Count();
            Assert.Equal(1, count);
        }

        [Fact]
        public void Except2()
        {
            var count = (new int[] { 0, 1, 2 }).AsQueryable().Except((new int[] { 1, 2, 3 }).AsQueryable(), EqualityComparer<int>.Default).Count();
            Assert.Equal(1, count);
        }

        [Fact]
        public void ExceptBy_NullSource1_ThrowsArgumentNullException()
        {
            IQueryable<int> source1 = null;

            AssertExtensions.Throws<ArgumentNullException>("source1", () => source1.ExceptBy(Enumerable.Empty<int>(), x => x));
            AssertExtensions.Throws<ArgumentNullException>("source1", () => source1.ExceptBy(Enumerable.Empty<int>(), x => x, EqualityComparer<int>.Default));
        }

        [Fact]
        public void ExceptBy_NullSource2_ThrowsArgumentNullException()
        {
            IQueryable<int> source1 = Enumerable.Empty<int>().AsQueryable();
            IQueryable<int> source2 = null;

            AssertExtensions.Throws<ArgumentNullException>("source2", () => source1.ExceptBy(source2, x => x));
            AssertExtensions.Throws<ArgumentNullException>("source2", () => source1.ExceptBy(source2, x => x, EqualityComparer<int>.Default));
        }

        [Fact]
        public void ExceptBy_NullKeySelector_ThrowsArgumentNullException()
        {
            IQueryable<int> source = Enumerable.Empty<int>().AsQueryable();
            Expression<Func<int, int>> keySelector = null;

            AssertExtensions.Throws<ArgumentNullException>("keySelector", () => source.ExceptBy(source, keySelector));
            AssertExtensions.Throws<ArgumentNullException>("keySelector", () => source.ExceptBy(source, keySelector, EqualityComparer<int>.Default));
        }

        [Fact]
        public void ExceptBy()
        {
            var expected = Enumerable.Range(5, 5);
            var actual = Enumerable.Range(0, 10).AsQueryable().ExceptBy(Enumerable.Range(0, 5), x => x).ToArray();
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void ExceptBy_CustomComparison()
        {
            var expected = Enumerable.Range(5, 5);
            var actual = Enumerable.Range(0, 10).AsQueryable().ExceptBy(Enumerable.Range(0, 5), x => x, EqualityComparer<int>.Default).ToArray();
            Assert.Equal(expected, actual);
        }
    }
}
