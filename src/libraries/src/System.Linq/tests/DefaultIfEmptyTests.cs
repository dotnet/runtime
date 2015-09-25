// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Xunit;

namespace System.Linq.Tests
{
    public class DefaultIfEmptyTests : EnumerableTests
    {
        [Fact]
        public void SameResultsRepeatCallsNonEmptyQuery()
        {
            var q = from x in new[] { 9999, 0, 888, -1, 66, -777, 1, 2, -12345 }
                    where x > Int32.MinValue
                    select x;
                    
            Assert.Equal(q.DefaultIfEmpty(5), q.DefaultIfEmpty(5));
        }

        [Fact]
        public void SameResultsRepeatCallsEmptyQuery()
        {
            var q = from x in NumberRangeGuaranteedNotCollectionType(0, 0)
                    select x;
            
            Assert.Equal(q.DefaultIfEmpty(88), q.DefaultIfEmpty(88));

        }

        [Fact]
        public void EmptyNullableSourceNoDefaultPassed()
        {
            int?[] source = { };
            int?[] expected = { default(int?) };

            Assert.Equal(expected, source.DefaultIfEmpty());
        }

        [Fact]
        public void EmptyNonNullableSourceNoDefaultPassed()
        {
            int[] source = { };
            int[] expected = { default(int) };

            Assert.Equal(expected, source.DefaultIfEmpty());
        }

        [Fact]
        public void NonEmptyNonNullableSourceNoDefaultPassed()
        {
            int[] source = { 3 };

            Assert.Equal(source, source.DefaultIfEmpty());
        }

        [Fact]
        public void SeveralElementsNoDefaultPassed()
        {
            int[] source = { 3, -1, 0, 10, 15 };

            Assert.Equal(source, source.DefaultIfEmpty());
        }

        [Fact]
        public void EmptyNullableDefaultValuePassed()
        {
            int?[] source = { };
            int? defaultValue = 9;
            int?[] expected = { defaultValue };

            Assert.Equal(expected, source.DefaultIfEmpty(defaultValue));
        }

        [Fact]
        public void EmptyNonNullableDefaultValuePassed()
        {
            int[] source = { };
            int defaultValue = -10;
            int[] expected = { defaultValue };

            Assert.Equal(expected, source.DefaultIfEmpty(defaultValue));
        }

        [Fact]
        public void NonEmptyDefaultValuePassed()
        {
            int[] source = { 3 };

            Assert.Equal(source, source.DefaultIfEmpty(9));
        }

        [Fact]
        public void SeveralItemsDefaultValuePassed()
        {
            int[] source = { 3, -1, 0, 10, 15 };

            Assert.Equal(source, source.DefaultIfEmpty(9));
        }
        
        [Fact]
        public void NullSource()
        {
            IEnumerable<int> source = null;
            
            Assert.Throws<ArgumentNullException>("source", () => source.DefaultIfEmpty());
            Assert.Throws<ArgumentNullException>("source", () => source.DefaultIfEmpty(42));
        }

        [Fact]
        public void ForcedToEnumeratorDoesntEnumerate()
        {
            var iterator = NumberRangeGuaranteedNotCollectionType(0, 3).DefaultIfEmpty();
            // Don't insist on this behaviour, but check its correct if it happens
            var en = iterator as IEnumerator<int>;
            Assert.False(en != null && en.MoveNext());
        }
    }
}
