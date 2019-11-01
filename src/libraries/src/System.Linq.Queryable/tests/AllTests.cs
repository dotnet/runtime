// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq.Expressions;
using Xunit;

namespace System.Linq.Tests
{
    public class AllTests : EnumerableBasedTests
    {
        [Fact]
        public void PredicateTrueAllExceptLast()
        {
            int[] source = { 4, 2, 10, 12, 8, 6, 3 };

            Assert.False(source.AsQueryable().All(i => i % 2 == 0));
        }

        [Fact]
        public void NullSource()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((IQueryable<int>)null).All(i => i != 0));
        }

        [Fact]
        public void NullPredicateUsed()
        {
            Expression<Func<int, bool>> predicate = null;
            AssertExtensions.Throws<ArgumentNullException>("predicate", () => Enumerable.Range(0, 3).AsQueryable().All(predicate));
        }

        [Fact]
        public void All()
        {
            var val = (new int[] { 0, 2, 1 }).AsQueryable().All(n => n > 1);
            Assert.False(val);
        }
    }
}
