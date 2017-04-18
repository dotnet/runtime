// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using Xunit;

namespace System.Linq.Tests
{
    public class SingleOrDefaultTests : EnumerableTests
    {
        [Fact]
        public void SameResultsRepeatCallsIntQuery()
        {
            var q = from x in new[] { 0.12335f }
                    select x;

            Assert.Equal(q.SingleOrDefault(), q.SingleOrDefault());
        }

        [Fact]
        public void SameResultsRepeatCallsStringQuery()
        {
            var q = from x in new[] { "" }
                    select x;

            Assert.Equal(q.SingleOrDefault(string.IsNullOrEmpty), q.SingleOrDefault(string.IsNullOrEmpty));
        }

        [Fact]
        public void EmptyIList()
        {
            int?[] source = { };
            int? expected = null;

            Assert.Equal(expected, source.SingleOrDefault());
        }

        [Fact]
        public void SingleElementIList()
        {
            int[] source = { 4 };
            int expected = 4;

            Assert.Equal(expected, source.SingleOrDefault());
        }

        [Fact]
        public void ManyElementIList()
        {
            int[] source = { 4, 4, 4, 4, 4 };

            Assert.Throws<InvalidOperationException>(() => source.SingleOrDefault());
        }

        [Fact]
        public void EmptyNotIList()
        {
            IEnumerable<int> source = RepeatedNumberGuaranteedNotCollectionType(0, 0);
            int expected = default(int);

            Assert.Equal(expected, source.SingleOrDefault());
        }

        [Fact]
        public void SingleElementNotIList()
        {
            IEnumerable<int> source = RepeatedNumberGuaranteedNotCollectionType(-5, 1);
            int expected = -5;

            Assert.Equal(expected, source.SingleOrDefault());
        }

        [Fact]
        public void ManyElementNotIList()
        {
            IEnumerable<int> source = RepeatedNumberGuaranteedNotCollectionType(3, 5);

            Assert.Throws<InvalidOperationException>(() => source.SingleOrDefault());
        }

        [Fact]
        public void EmptySourceWithPredicate()
        {
            int[] source = { };
            int expected = default(int);

            Assert.Equal(expected, source.SingleOrDefault(i => i % 2 == 0));
        }

        [Fact]
        public void SingleElementPredicateTrue()
        {
            int[] source = { 4 };
            int expected = 4;
            
            Assert.Equal(expected, source.SingleOrDefault(i => i % 2 == 0));
        }

        [Fact]
        public void SingleElementPredicateFalse()
        {
            int[] source = { 3 };
            int expected = default(int);

            Assert.Equal(expected, source.SingleOrDefault(i => i % 2 == 0));
        }

        [Fact]
        public void ManyElementsPredicateFalseForAll()
        {
            int[] source = { 3, 1, 7, 9, 13, 19 };
            int expected = default(int);

            Assert.Equal(expected, source.SingleOrDefault(i => i % 2 == 0));
        }

        [Fact]
        public void ManyElementsPredicateTrueForLast()
        {
            int[] source = { 3, 1, 7, 9, 13, 19, 20 };
            int expected = 20;

            Assert.Equal(expected, source.SingleOrDefault(i => i % 2 == 0));
        }

        [Fact]
        public void ManyElementsPredicateTrueForFirstAndFifth()
        {
            int[] source = { 2, 3, 1, 7, 10, 13, 19, 9 };

            Assert.Throws<InvalidOperationException>(() => source.SingleOrDefault(i => i % 2 == 0));
        }

        [Theory]
        [InlineData(1, 100)]
        [InlineData(42, 100)]
        public void FindSingleMatch(int target, int range)
        {
            Assert.Equal(target, Enumerable.Range(0, range).SingleOrDefault(i => i == target));
        }

        [Theory]
        [InlineData(1, 100)]
        [InlineData(42, 100)]
        public void RunOnce(int target, int range)
        {
            Assert.Equal(target, Enumerable.Range(0, range).RunOnce().SingleOrDefault(i => i == target));
        }

        [Fact]
        public void ThrowsOnNullSource()
        {
            int[] source = null;
            AssertExtensions.Throws<ArgumentNullException>("source", () => source.SingleOrDefault());
            AssertExtensions.Throws<ArgumentNullException>("source", () => source.SingleOrDefault(i => i % 2 == 0));
        }

        [Fact]
        public void ThrowsOnNullPredicate()
        {
            int[] source = { };
            Func<int, bool> nullPredicate = null;
            AssertExtensions.Throws<ArgumentNullException>("predicate", () => source.SingleOrDefault(nullPredicate));
        }
    }
}
