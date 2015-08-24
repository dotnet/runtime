// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Xunit;

namespace System.Linq.Tests
{
    public class CountTests
    {
        private static bool IsEven(int num)
        {
            return num % 2 == 0;
        }

        public static IEnumerable<int> RepeatedNumberGuaranteedNotCollectionType(int num, long count)
        {
            for (long i = 0; i < count; i++) yield return num;
        }

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
    }
}
