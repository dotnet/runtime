// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq.Expressions;
using Xunit;

namespace System.Linq.Tests
{
    public class LongCountTests : EnumerableBasedTests
    {
        [Fact]
        public void EmptySource()
        {
            int[] data = { };
            Assert.Equal(0, data.AsQueryable().LongCount());
        }

        [Fact]
        public void EmptySourceWithPredicate()
        {
            int[] data = { };
            Assert.Equal(0, data.AsQueryable().LongCount());
        }

        [Fact]
        public void MultipleElements()
        {
            int?[] data = { -10, 4, 9, null, 11 };
            Assert.Equal(data.Length, data.AsQueryable().LongCount());
        }

        [Fact]
        public void PredicateTrueFirstAndLast()
        {
            int[] data = { 2, 5, 7, 9, 29, 10 };
            Assert.Equal(2, data.AsQueryable().LongCount(i => i % 2 == 0));
        }

        [Fact]
        public void NullSource()
        {
            Assert.Throws<ArgumentNullException>("source", () => ((IQueryable<int>)null).LongCount());
        }

        [Fact]
        public void NullSourcePredicateUsed()
        {
            Assert.Throws<ArgumentNullException>("source", () => ((IQueryable<int>)null).LongCount(i => i != 0));
        }

        [Fact]
        public void NullPredicateUsed()
        {
            Expression<Func<int, bool>> predicate = null;
            Assert.Throws<ArgumentNullException>("predicate", () => Enumerable.Range(0, 3).AsQueryable().LongCount(predicate));
        }

        [Fact]
        public void LongCount1()
        {
            var count = (new int[] { 0 }).AsQueryable().LongCount();
            Assert.Equal(1L, count);
        }

        [Fact]
        public void LongCount2()
        {
            var count = (new int[] { 0, 1, 2 }).AsQueryable().LongCount(n => n > 0);
            Assert.Equal(2L, count);
        }
    }
}
