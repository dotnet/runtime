// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq.Expressions;
using Xunit;

namespace System.Linq.Tests
{
    public class FirstOrDefaultTests : EnumerableBasedTests
    {
        [Fact]
        public void Empty()
        {
            int[] source = { };

            Assert.Equal(0, source.AsQueryable().FirstOrDefault());
        }

        [Fact]
        public void EmptyDefault()
        {
            int[] source = { };
            int defaultValue = 5;

            Assert.Equal(defaultValue, source.AsQueryable().FirstOrDefault(defaultValue));
        }

        [Fact]
        public void ManyElementsFirstIsDefault()
        {
            int?[] source = { null, -10, 2, 4, 3, 0, 2 };
            Assert.Null(source.AsQueryable().FirstOrDefault());
        }

        [Fact]
        public void ManyELementsFirstIsNotDefault()
        {
            int?[] source = { 19, null, -10, 2, 4, 3, 0, 2 };
            Assert.Equal(19, source.AsQueryable().FirstOrDefault());
        }

        [Fact]
        public void OneElementTruePredicate()
        {
            int[] source = { 4 };
            Assert.Equal(4, source.AsQueryable().FirstOrDefault(i => i % 2 == 0));
        }

        [Fact]
        public void OneElementTruePredicateDefault()
        {
            int[] source = { 4 };
            Assert.Equal(4, source.AsQueryable().FirstOrDefault(i => i % 2 == 0, 5));
        }

        [Fact]
        public void OneElementFalsePredicate()
        {
            int[] source = { 3 };
            Assert.Equal(0, source.AsQueryable().FirstOrDefault(i => i % 2 == 0));
        }

        [Fact]
        public void OneElementFalsePredicateDefault()
        {
            int[] source = { 3 };
            Assert.Equal(5, source.AsQueryable().FirstOrDefault(i => i % 2 == 0, 5));
        }

        [Fact]
        public void ManyElementsPredicateFalseForAll()
        {
            int[] source = { 9, 5, 1, 3, 17, 21 };
            Assert.Equal(0, source.AsQueryable().FirstOrDefault(i => i % 2 == 0));
        }

        [Fact]
        public void ManyElementsPredicateFalseForAllDefault()
        {
            int[] source = { 9, 5, 1, 3, 17, 21 };
            Assert.Equal(2, source.AsQueryable().FirstOrDefault(i => i % 2 == 0, 2));
        }

        [Fact]
        public void PredicateTrueForSome()
        {
            int[] source = { 3, 7, 10, 7, 9, 2, 11, 17, 13, 8 };
            Assert.Equal(10, source.AsQueryable().FirstOrDefault(i => i % 2 == 0));
        }
        [Fact]
        public void PredicateTrueForSomeDefault()
        {
            int[] source = { 3, 7, 10, 7, 9, 2, 11, 17, 13, 8 };
            Assert.Equal(10, source.AsQueryable().FirstOrDefault(i => i % 2 == 0, 5));
        }

        [Fact]
        public void NullSource()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((IQueryable<int>)null).FirstOrDefault());
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((IQueryable<int>)null).FirstOrDefault(5));
        }

        [Fact]
        public void NullSourcePredicateUsed()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((IQueryable<int>)null).FirstOrDefault(i => i != 2));
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((IQueryable<int>)null).FirstOrDefault(i => i != 2, 5));
        }

        [Fact]
        public void NullPredicate()
        {
            Expression<Func<int, bool>> predicate = null;
            AssertExtensions.Throws<ArgumentNullException>("predicate", () => Enumerable.Range(0, 3).AsQueryable().FirstOrDefault(predicate));
            AssertExtensions.Throws<ArgumentNullException>("predicate", () => Enumerable.Range(0, 3).AsQueryable().FirstOrDefault(predicate, 5));
        }

        [Fact]
        public void FirstOrDefault1()
        {
            var val = (new int[] { 1, 2 }).AsQueryable().FirstOrDefault();
            Assert.Equal(1, val);
        }

        [Fact]
        public void FirstOrDefault2()
        {
            var val = (new int[] { 0, 1, 2 }).AsQueryable().FirstOrDefault(n => n > 1);
            Assert.Equal(2, val);
        }
    }
}
