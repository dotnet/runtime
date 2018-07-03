// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Linq.Tests
{
    public class SingleTests : EnumerableTests
    {
        [Fact]
        public void SameResultsRepeatCallsIntQuery()
        {
            var q = from x in new[] { 999.9m }
                    select x;

            Assert.Equal(q.Single(), q.Single());
        }

        [Fact]
        public void SameResultsRepeatCallsStringQuery()
        {
            var q = from x in new[] { "!@#$%^" }
                    where !string.IsNullOrEmpty(x)
                    select x;

            Assert.Equal(q.Single(), q.Single());
        }

        [Fact]
        public void SameResultsRepeatCallsIntQueryWithZero()
        {
            var q = from x in new[] { 0 }
                    select x;

            Assert.Equal(q.Single(), q.Single());
        }

        [Fact]
        public void EmptyIList()
        {
            int[] source = { };
            
            Assert.Throws<InvalidOperationException>(() => source.Single());
        }

        [Fact]
        public void SingleElementIList()
        {
            int[] source = { 4 };
            int expected = 4;

            Assert.Equal(expected, source.Single());
        }

        [Fact]
        public void ManyElementIList()
        {
            int[] source = { 4, 4, 4, 4, 4 };

            Assert.Throws<InvalidOperationException>(() => source.Single());
        }

        [Fact]
        public void EmptyNotIList()
        {
            IEnumerable<int> source = RepeatedNumberGuaranteedNotCollectionType(0, 0);

            Assert.Throws<InvalidOperationException>(() => source.Single());
        }

        [Fact]
        public void SingleElementNotIList()
        {
            IEnumerable<int> source = RepeatedNumberGuaranteedNotCollectionType(-5, 1);
            int expected = -5;

            Assert.Equal(expected, source.Single());
        }

        [Fact]
        public void ManyElementNotIList()
        {
            IEnumerable<int> source = RepeatedNumberGuaranteedNotCollectionType(3, 5);

            Assert.Throws<InvalidOperationException>(() => source.Single());
        }

        [Fact]
        public void EmptySourceWithPredicate()
        {
            int[] source = { };

            Assert.Throws<InvalidOperationException>(() => source.Single(i => i % 2 == 0));
        }

        [Fact]
        public void SingleElementPredicateTrue()
        {
            int[] source = { 4 };
            int expected = 4;
            
            Assert.Equal(expected, source.Single(i => i % 2 == 0));
        }

        [Fact]
        public void SingleElementPredicateFalse()
        {
            int[] source = { 3 };
            
            Assert.Throws<InvalidOperationException>(() => source.Single(i => i % 2 == 0));
        }

        [Fact]
        public void ManyElementsPredicateFalseForAll()
        {
            int[] source = { 3, 1, 7, 9, 13, 19 };

            Assert.Throws<InvalidOperationException>(() => source.Single(i => i % 2 == 0));
        }

        [Fact]
        public void ManyElementsPredicateTrueForLast()
        {
            int[] source = { 3, 1, 7, 9, 13, 19, 20 };
            int expected = 20;

            Assert.Equal(expected, source.Single(i => i % 2 == 0));
        }

        [Fact]
        public void ManyElementsPredicateTrueForFirstAndLast()
        {
            int[] source = { 2, 3, 1, 7, 9, 13, 19, 10 };

            Assert.Throws<InvalidOperationException>(() => source.Single(i => i % 2 == 0));
        }

        [Theory]
        [InlineData(1, 100)]
        [InlineData(42, 100)]
        public void FindSingleMatch(int target, int range)
        {
            Assert.Equal(target, Enumerable.Range(0, range).Single(i => i == target));
        }

        [Theory]
        [InlineData(1, 100)]
        [InlineData(42, 100)]
        public void RunOnce(int target, int range)
        {
            Assert.Equal(target, Enumerable.Range(0, range).RunOnce().Single(i => i == target));
        }

        [Fact]
        public void ThrowsOnNullSource()
        {
            int[] source = null;
            AssertExtensions.Throws<ArgumentNullException>("source", () => source.Single());
            AssertExtensions.Throws<ArgumentNullException>("source", () => source.Single(i => i % 2 == 0));
        }

        [Fact]
        public void ThrowsOnNullPredicate()
        {
            int[] source = { };
            Func<int, bool> nullPredicate = null;
            AssertExtensions.Throws<ArgumentNullException>("predicate", () => source.Single(nullPredicate));
        }
    }
}
