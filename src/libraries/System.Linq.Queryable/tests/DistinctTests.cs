// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq.Expressions;
using Xunit;

namespace System.Linq.Tests
{
    public class DistinctTests : EnumerableBasedTests
    {
        [Fact]
        public void EmptySource()
        {
            int[] source = { };
            Assert.Empty(source.AsQueryable().Distinct());
        }

        [Fact]
        public void SingleNullElementExplicitlyUseDefaultComparer()
        {
            string[] source = { null };
            string[] expected = { null };

            Assert.Equal(expected, source.AsQueryable().Distinct(EqualityComparer<string>.Default));
        }

        [Fact]
        public void EmptyStringDistinctFromNull()
        {
            string[] source = { null, null, string.Empty };
            string[] expected = { null, string.Empty };

            Assert.Equal(expected, source.AsQueryable().Distinct(EqualityComparer<string>.Default));
        }

        [Fact]
        public void SourceAllDuplicates()
        {
            int[] source = { 5, 5, 5, 5, 5, 5 };
            int[] expected = { 5 };

            Assert.Equal(expected, source.AsQueryable().Distinct());
        }

        [Fact]
        public void AllUnique()
        {
            int[] source = { 2, -5, 0, 6, 10, 9 };

            Assert.Equal(source, source.AsQueryable().Distinct());
        }

        [Fact]
        public void NullSource()
        {
            IQueryable<string> source = null;
            AssertExtensions.Throws<ArgumentNullException>("source", () => source.Distinct());
        }

        [Fact]
        public void NullSourceCustomComparer()
        {
            IQueryable<string> source = null;
            AssertExtensions.Throws<ArgumentNullException>("source", () => source.Distinct(StringComparer.Ordinal));
        }

        [Fact]
        public void Distinct1()
        {
            var count = (new int[] { 0, 1, 2, 2, 0 }).AsQueryable().Distinct().Count();
            Assert.Equal(3, count);
        }

        [Fact]
        public void Distinct2()
        {
            var count = (new int[] { 0, 1, 2, 2, 0 }).AsQueryable().Distinct(EqualityComparer<int>.Default).Count();
            Assert.Equal(3, count);
        }

        [Fact]
        public void DistinctBy_NullSource_ThrowsArgumentNullException()
        {
            IQueryable<int> source = null;

            AssertExtensions.Throws<ArgumentNullException>("source", () => source.DistinctBy(x => x));
            AssertExtensions.Throws<ArgumentNullException>("source", () => source.DistinctBy(x => x, EqualityComparer<int>.Default));
        }

        [Fact]
        public void DistinctBy_NullKeySelector_ThrowsArgumentNullException()
        {
            IQueryable<int> source = Enumerable.Empty<int>().AsQueryable();
            Expression<Func<int, int>> keySelector = null;

            AssertExtensions.Throws<ArgumentNullException>("keySelector", () => source.DistinctBy(keySelector));
            AssertExtensions.Throws<ArgumentNullException>("keySelector", () => source.DistinctBy(keySelector, EqualityComparer<int>.Default));
        }

        [Fact]
        public void DistinctBy()
        {
            var expected = Enumerable.Range(0, 3);
            var actual = Enumerable.Range(0, 20).AsQueryable().DistinctBy(x => x % 3).ToArray();
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void DistinctBy_CustomComparison()
        {
            var expected = Enumerable.Range(0, 3);
            var actual = Enumerable.Range(0, 20).AsQueryable().DistinctBy(x => x % 3, EqualityComparer<int>.Default).ToArray();
            Assert.Equal(expected, actual);
        }
    }
}
