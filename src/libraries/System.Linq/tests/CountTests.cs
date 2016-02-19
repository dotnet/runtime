// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using Xunit;

namespace System.Linq.Tests
{
    public class CountTests : EnumerableTests
    {
        [Fact]
        public void SameResultsRepeatCallsIntQuery()
        {
            var q = from x in new[] { 9999, 0, 888, -1, 66, -777, 1, 2, -12345 }
                    where x > Int32.MinValue
                    select x;

            Assert.Equal(q.Count(), q.Count());
        }

        [Fact]
        public void SameResultsRepeatCallsStringQuery()
        {
            var q = from x in new[] { "!@#$%^", "C", "AAA", "", "Calling Twice", "SoS", String.Empty }
                    where !String.IsNullOrEmpty(x)
                    select x;

            Assert.Equal(q.Count(), q.Count());
        }

        [Fact]
        public void EmptyICollectionT()
        {
            int[] data = { };
            int expected = 0;

            Assert.Equal(expected, data.Count());
        }

        [Fact]
        public void EmptySourceWithPredicate()
        {
            int[] data = { };
            int expected = 0;

            Assert.Equal(expected, data.Count(IsEven));
        }

        [Fact]
        public void NonEmptyICollectionT()
        {
            int?[] data = { -10, 4, 9, null, 11 };
            int expected = 5;

            Assert.Equal(expected, data.Count());
        }

        [Fact]
        public void SingleElementMatchesPredicate()
        {
            int[] data = { 4 };
            int expected = 1;

            Assert.Equal(expected, data.Count(IsEven));
        }

        [Fact]
        public void EmptyNonICollectionT()
        {
            IEnumerable<int> data = RepeatedNumberGuaranteedNotCollectionType(0, 0);
            int expected = 0;

            Assert.Equal(expected, data.Count());
        }

        [Fact]
        public void SingleElementDoesntMatchPredicate()
        {
            int[] data = { 5 };
            int expected = 0;

            Assert.Equal(expected, data.Count(IsEven));
        }

        [Fact]
        public void SingleElementNonICollectionT()
        {
            IEnumerable<int> data = RepeatedNumberGuaranteedNotCollectionType(5, 1);
            int expected = 1;

            Assert.Equal(expected, data.Count());
        }

        [Fact]
        public void PredicateTrueFirstAndLast()
        {
            int[] data = { 2, 5, 7, 9, 29, 10 };
            int expected = 2;

            Assert.Equal(expected, data.Count(IsEven));
        }

        [Fact]
        public void MultipleElementsNonICollectionT()
        {
            IEnumerable<int> data = RepeatedNumberGuaranteedNotCollectionType(5, 10);
            int expected = 10;

            Assert.Equal(expected, data.Count());
        }

        [Fact]
        public void MultipleElementsAllMatchPredicate()
        {
            int[] data = { 2, 20, 22, 100, 50, 10 };
            int expected = 6;

            Assert.Equal(expected, data.Count(IsEven));
        }

        [Theory, MemberData(nameof(CountsAndTallies))]
        public void CountMatchesTally<T, TEn>(T unusedArgumentToForceTypeInference, int count, TEn enumerable)
            where TEn : IEnumerable<T>
        {
            Assert.Equal(count, enumerable.Count());
        }

        private static IEnumerable<object[]> EnumerateCollectionTypesAndCounts<T>(int count, IEnumerable<T> enumerable)
        {
            yield return new object[] { default(T), count, enumerable };
            yield return new object[] { default(T), count, enumerable.ToArray() };
            yield return new object[] { default(T), count, enumerable.ToList() };
            yield return new object[] { default(T), count, new Stack<T>(enumerable) };
        }

        public static IEnumerable<object[]> CountsAndTallies()
        {
            int count = 5;
            var range = Enumerable.Range(1, count);
            foreach (object[] variant in EnumerateCollectionTypesAndCounts(count, range))
                yield return variant;
            foreach (object[] variant in EnumerateCollectionTypesAndCounts(count, range.Select(i => (float)i)))
                yield return variant;
            foreach (object[] variant in EnumerateCollectionTypesAndCounts(count, range.Select(i => (double)i)))
                yield return variant;
            foreach (object[] variant in EnumerateCollectionTypesAndCounts(count, range.Select(i => (decimal)i)))
                yield return variant;
        }

        [Fact]
        public void NullSource()
        {
            Assert.Throws<ArgumentNullException>("source", () => ((IEnumerable<int>)null).Count());
        }

        [Fact]
        public void NullSourcePredicateUsed()
        {
            Assert.Throws<ArgumentNullException>("source", () => ((IEnumerable<int>)null).Count(i => i != 0));
        }

        [Fact]
        public void NullPredicateUsed()
        {
            Func<int, bool> predicate = null;
            Assert.Throws<ArgumentNullException>("predicate", () => Enumerable.Range(0, 3).Count(predicate));
        }
    }
}
