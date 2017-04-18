// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq.Expressions;
using Xunit;

namespace System.Linq.Tests
{
    public class FirstTests : EnumerableBasedTests
    {
        [Fact]
        public void Empty()
        {
            int[] source = { };
            Assert.Throws<InvalidOperationException>(() => source.AsQueryable().First());
        }

        [Fact]
        public void ManyElementsFirstIsDefault()
        {
            int?[] source = { null, -10, 2, 4, 3, 0, 2 };
            Assert.Null(source.AsQueryable().First());
        }

        [Fact]
        public void ManyELementsFirstIsNotDefault()
        {
            int?[] source = { 19, null, -10, 2, 4, 3, 0, 2 };
            Assert.Equal(19, source.AsQueryable().First());
        }

        [Fact]
        public void OneElementTruePredicate()
        {
            int[] source = { 4 };
            Assert.Equal(4, source.AsQueryable().First(i => i % 2 == 0));
        }

        [Fact]
        public void ManyElementsPredicateFalseForAll()
        {
            int[] source = { 9, 5, 1, 3, 17, 21 };
            Assert.Throws<InvalidOperationException>(() => source.AsQueryable().First(i => i % 2 == 0));
        }

        [Fact]
        public void PredicateTrueForSome()
        {
            int[] source = { 3, 7, 10, 7, 9, 2, 11, 17, 13, 8 };
            Assert.Equal(10, source.AsQueryable().First(i => i % 2 == 0));
        }

        [Fact]
        public void NullSource()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((IQueryable<int>)null).First());
        }

        [Fact]
        public void NullSourcePredicateUsed()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((IQueryable<int>)null).First(i => i != 2));
        }

        [Fact]
        public void NullPredicate()
        {
            Expression<Func<int, bool>> predicate = null;
            AssertExtensions.Throws<ArgumentNullException>("predicate", () => Enumerable.Range(0, 3).AsQueryable().First(predicate));
        }

        [Fact]
        public void First1()
        {
            var val = (new int[] { 1, 2 }).AsQueryable().First();
            Assert.Equal(1, val);
        }

        [Fact]
        public void First2()
        {
            var val = (new int[] { 0, 1, 2 }).AsQueryable().First(n => n > 1);
            Assert.Equal(2, val);
        }
    }
}
