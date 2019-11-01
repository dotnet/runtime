// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq.Expressions;
using Xunit;

namespace System.Linq.Tests
{
    public class WhereTests : EnumerableBasedTests
    {
        [Fact]
        public void Where_SourceIsNull_ArgumentNullExceptionThrown()
        {
            IQueryable<int> source = null;
            AssertExtensions.Throws<ArgumentNullException>("source", () => source.Where(i => true));
            AssertExtensions.Throws<ArgumentNullException>("source", () => source.Where((v, i) => true));
        }

        [Fact]
        public void Where_PredicateIsNull_ArgumentNullExceptionThrown()
        {
            IQueryable<int> source = Enumerable.Range(1, 10).AsQueryable();
            Expression<Func<int, bool>> simplePredicate = null;
            Expression<Func<int, int, bool>> complexPredicate = null;

            AssertExtensions.Throws<ArgumentNullException>("predicate", () => source.Where(simplePredicate));
            AssertExtensions.Throws<ArgumentNullException>("predicate", () => source.Where(complexPredicate));
        }

        [Fact]
        public void ReturnsExpectedValues_True()
        {
            int[] source = new[] { 1, 2, 3, 4, 5 };
            Assert.Equal(source, source.AsQueryable().Where(i => true));
        }

        [Fact]
        public void ReturnsExpectedValues_False()
        {
            int[] source = new[] { 1, 2, 3, 4, 5 };
            Assert.Empty(source.AsQueryable().Where(i => false));
        }

        [Fact]
        public void ReturnsExpectedValuesIndexed_True()
        {
            int[] source = new[] { 1, 2, 3, 4, 5 };
            Assert.Equal(source, source.AsQueryable().Where((e, i) => true));
        }

        [Fact]
        public void ReturnsExpectedValuesIndexed_False()
        {
            int[] source = new[] { 1, 2, 3, 4, 5 };
            Assert.Empty(source.AsQueryable().Where((e, i) => false));
        }

        [Fact]
        public void Where1()
        {
            var count = (new int[] { 0, 1, 2 }).AsQueryable().Where(n => n > 1).Count();
            Assert.Equal(1, count);
        }

        [Fact]
        public void Where2()
        {
            var count = (new int[] { 0, 1, 2 }).AsQueryable().Where((n, i) => n > 1 || i == 0).Count();
            Assert.Equal(2, count);
        }
    }
}
