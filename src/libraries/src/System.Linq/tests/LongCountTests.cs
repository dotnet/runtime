// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Xunit;

namespace System.Linq.Tests
{
    public class LongCountTests
    {
        private static bool IsEven(int num)
        {
            return num % 2 == 0;
        }

        [Fact]
        public void SameResultsRepeatCallsIntQuery()
        {
            var q = from x in new[] { 9999, 0, 888, -1, 66, -777, 1, 2, -12345 }
                        where x > Int32.MinValue
                        select x;

            Assert.Equal(q.LongCount(), q.LongCount());
        }

        [Fact]
        public void SameResultsRepeatCallsStringQuery()
        {
            var q = from x in new[] { "!@#$%^", "C", "AAA", "", "Calling Twice", "SoS", String.Empty }
                    where !String.IsNullOrEmpty(x)
                    select x;

            Assert.Equal(q.LongCount(), q.LongCount());
        }

        [Fact]
        public void EmptySource()
        {
            int[] data = { };
            int expected = 0;

            Assert.Equal(expected, data.LongCount());
        }

        [Fact]
        public void EmptySourceWithPredicate()
        {
            int[] data = { };
            int expected = 0;

            Assert.Equal(expected, data.LongCount(IsEven));
        }

        [Fact]
        public void SingleElement()
        {
            int[] data = { 3 };
            int expected = 1;

            Assert.Equal(expected, data.LongCount());
        }

        [Fact]
        public void SingleElementMatchesPredicate()
        {
            int[] data = { 4 };
            int expected = 1;

            Assert.Equal(expected, data.LongCount(IsEven));
        }

        [Fact]
        public void MultipleElements()
        {
            int?[] data = { -10, 4, 9, null, 11 };
            int expected = 5;

            Assert.Equal(expected, data.LongCount());
        }

        [Fact]
        public void SingleElementDoesntMatchPredicate()
        {
            int[] data = { 5 };
            int expected = 0;

            Assert.Equal(expected, data.LongCount(IsEven));
        }

        [Fact]
        public void PredicateTrueFirstAndLast()
        {
            int[] data = { 2, 5, 7, 9, 29, 10 };
            int expected = 2;

            Assert.Equal(expected, data.LongCount(IsEven));
        }

        [Fact]
        public void MultipleElementsAllMatchPredicate()
        {
            int[] data = { 2, 20, 22, 100, 50, 10 };
            int expected = 6;

            Assert.Equal(expected, data.LongCount(IsEven));
        }
    }
}
