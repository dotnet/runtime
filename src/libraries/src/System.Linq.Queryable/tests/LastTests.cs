// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq.Expressions;
using Xunit;

namespace System.Linq.Tests
{
    public class LastTests : EnumerableBasedTests
    {
        [Fact]
        public void Empty()
        {            
            Assert.Throws<InvalidOperationException>(() => Enumerable.Empty<int>().AsQueryable().Last());
        }

        [Fact]
        public void OneElement()
        {
            int[] source = { 5 };
            Assert.Equal(5, source.AsQueryable().Last());
        }

        [Fact]
        public void ManyElementsLastIsDefault()
        {
            int?[] source = { -10, 2, 4, 3, 0, 2, null };
            Assert.Null(source.AsQueryable().Last());
        }

        [Fact]
        public void ManyElementsLastIsNotDefault()
        {
            int?[] source = { -10, 2, 4, 3, 0, 2, null, 19 };
            Assert.Equal(19, source.AsQueryable().Last());
        }

        [Fact]
        public void EmptySourcePredicate()
        {
            IQueryable<int> source = Enumerable.Empty<int>().AsQueryable();

            Assert.Throws<InvalidOperationException>(() => source.Last(x => true));
            Assert.Throws<InvalidOperationException>(() => source.Last(x => false));
        }

        [Fact]
        public void PredicateTrueForSome()
        {
            int[] source = { 3, 7, 10, 7, 9, 2, 11, 18, 13, 9 };
            Assert.Equal(18, source.AsQueryable().Last(i => i % 2 == 0));
        }

        [Fact]
        public void NullSource()
        {
            Assert.Throws<ArgumentNullException>("source", () => ((IQueryable<int>)null).Last());
        }

        [Fact]
        public void NullSourcePredicateUsed()
        {
            Assert.Throws<ArgumentNullException>("source", () => ((IQueryable<int>)null).Last(i => i != 2));
        }

        [Fact]
        public void NullPredicate()
        {
            Expression<Func<int, bool>> predicate = null;
            Assert.Throws<ArgumentNullException>("predicate", () => Enumerable.Range(0, 3).AsQueryable().Last(predicate));
        }

        [Fact]
        public void Last1()
        {
            var val = (new int[] { 0, 1, 2 }).AsQueryable().Last();
            Assert.Equal(2, val);
        }

        [Fact]
        public void Last2()
        {
            var val = (new int[] { 0, 1, 2 }).AsQueryable().Last(n => n > 1);
            Assert.Equal(2, val);
        }
    }
}
