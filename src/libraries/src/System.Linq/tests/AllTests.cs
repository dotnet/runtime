// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Xunit;

namespace System.Linq.Tests
{
    public class AllTests
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

            Func<int, bool> predicate = IsEven; 

            Assert.Equal(q.All(predicate), q.All(predicate));
        }

        [Fact]
        public void SameResultsRepeatCallsStringQuery()
        {
            var q = from x in new[] { "!@#$%^", "C", "AAA", "", "Calling Twice", "SoS", String.Empty }
                    select x;

            Func<string, bool> predicate = String.IsNullOrEmpty;

            Assert.Equal(q.All(predicate), q.All(predicate));
        }

        [Fact]
        public void SourceIsEmpty()
        {
            int[] source = { };
            
            Assert.True(source.All(IsEven));
        }

        [Fact]
        public void OneElementPredicateFalse()
        {
            int[] source = { 3 };

            Assert.False(source.All(IsEven));
        }

        [Fact]
        public void OneElementPredicateTrue()
        {
            int[] source = { 4 };

            Assert.True(source.All(IsEven));
        }

        [Fact]
        public void PredicateTrueOnSomeButNotAll()
        {
            int[] source = { 4, 8, 3, 5, 10, 20, 12 };

            Assert.False(source.All(IsEven));
        }

        [Fact]
        public void PredicateTrueAllExceptLast()
        {
            int[] source = { 4, 2, 10, 12, 8, 6, 3 };

            Assert.False(source.All(IsEven));
        }

        [Fact]
        public void TrueForAll()
        {
            int[] source = { 4, 2, 10, 12, 8, 6, 14 };
            
            Assert.True(source.All(IsEven));
        }
    }
}
