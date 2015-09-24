// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Xunit;

namespace System.Linq.Tests
{
    public class TakeTests : EnumerableTests
    {
        [Fact]
        public void SameResultsRepeatCallsIntQuery()
        {
            var q = from x in new[] { 9999, 0, 888, -1, 66, -777, 1, 2, -12345 }
                    where x > Int32.MinValue
                    select x;

            Assert.Equal(q.Take(9), q.Take(9));
        }

        [Fact]
        public void SameResultsRepeatCallsStringQuery()
        {
            var q = from x in new[] { "!@#$%^", "C", "AAA", "", "Calling Twice", "SoS", String.Empty }
                    where !String.IsNullOrEmpty(x)
                    select x;

            Assert.Equal(q.Take(7), q.Take(7));
        }

        [Fact]
        public void SourceEmptyCountPositive()
        {
            int[] source = { };
            Assert.Empty(source.Take(5));
        }

        [Fact]
        public void SourceNonEmptyCountNegative()
        {
            int[] source = { 2, 5, 9, 1 };
            Assert.Empty(source.Take(-5));
        }

        [Fact]
        public void SourceNonEmptyCountZero()
        {
            int[] source = { 2, 5, 9, 1 };
            Assert.Empty(source.Take(0));
        }

        [Fact]
        public void SourceNonEmptyCountOne()
        {
            int[] source = { 2, 5, 9, 1 };
            int[] expected = { 2 };

            Assert.Equal(expected, source.Take(1));
        }

        [Fact]
        public void SourceNonEmptyTakeAllExactly()
        {
            int[] source = { 2, 5, 9, 1 };
            
            Assert.Equal(source, source.Take(source.Length));
        }

        [Fact]
        public void SourceNonEmptyTakeAllButOne()
        {
            int[] source = { 2, 5, 9, 1 };
            int[] expected = { 2, 5, 9 };
            
            Assert.Equal(expected, source.Take(3));
        }

        [Fact]
        public void SourceNonEmptyTakeExcessive()
        {
            int?[] source = { 2, 5, null, 9, 1 };

            Assert.Equal(source, source.Take(source.Length + 1));
        }
        
        [Fact]
        public void ThrowsOnNullSource()
        {
            int[] source = null;
            Assert.Throws<ArgumentNullException>("source", () => source.Take(5));
        }

        [Fact]
        public void ForcedToEnumeratorDoesntEnumerate()
        {
            var iterator = NumberRangeGuaranteedNotCollectionType(0, 3).Take(2);
            // Don't insist on this behaviour, but check its correct if it happens
            var en = iterator as IEnumerator<int>;
            Assert.False(en != null && en.MoveNext());
        }
    }
}
