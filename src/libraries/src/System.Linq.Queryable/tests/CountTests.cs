// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq.Expressions;
using Xunit;

namespace System.Linq.Tests
{
    public class CountTests : EnumerableBasedTests
    {
        [Fact]
        public void Empty()
        {
            Assert.Equal(0, Enumerable.Empty<int>().AsQueryable().Count());
        }

        [Fact]
        public void EmptySourceWithPredicate()
        {
            Assert.Equal(0, Enumerable.Empty<int>().AsQueryable().Count(i => i % 2 == 0));
        }

        [Fact]
        public void NonEmpty()
        {
            int?[] data = { -10, 4, 9, null, 11 };
            Assert.Equal(5, data.AsQueryable().Count());
        }

        [Fact]
        public void PredicateTrueFirstAndLast()
        {
            int[] data = { 2, 5, 7, 9, 29, 10 };
            Assert.Equal(2, data.AsQueryable().Count(i => i % 2 == 0));
        }

        [Fact]
        public void NullSource()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((IQueryable<int>)null).Count());
        }

        [Fact]
        public void NullSourcePredicateUsed()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((IQueryable<int>)null).Count(i => i != 0));
        }

        [Fact]
        public void NullPredicateUsed()
        {
            Expression<Func<int, bool>> predicate = null;
            AssertExtensions.Throws<ArgumentNullException>("predicate", () => Enumerable.Range(0, 3).AsQueryable().Count(predicate));
        }

        [Fact]
        public void Count1()
        {
            var count = (new int[] { 0 }).AsQueryable().Count();
            Assert.Equal(1, count);
        }

        [Fact]
        public void Count2()
        {
            var count = (new int[] { 0, 1, 2 }).AsQueryable().Count(n => n > 0);
            Assert.Equal(2, count);
        }
    }
}
