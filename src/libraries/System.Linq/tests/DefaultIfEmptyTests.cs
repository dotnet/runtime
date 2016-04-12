// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
            // Don't insist on this behaviour, but check it's correct if it happens
            var en = iterator as IEnumerator<int>;
            Assert.False(en != null && en.MoveNext());
        }

        [Fact]
        public void RepeatEnumeration()
        {
            var q = Enumerable.Range(0, 3).DefaultIfEmpty(9);

            Assert.Equal(q, q);
        }

        [Fact]
        public void ToArray()
        {
            int[] source = { 3, -1, 0, 10, 15 };

            Assert.Equal(source, source.DefaultIfEmpty(9).ToArray());
        }

        [Fact]
        public void EmptyToArray()
        {
            Assert.Equal(new[] { 9 }, Enumerable.Empty<int>().DefaultIfEmpty(9).ToArray());
        }

        [Fact]
        public void ToList()
        {
            int[] source = { 3, -1, 0, 10, 15 };

            Assert.Equal(source, source.DefaultIfEmpty(9).ToList());
        }

        [Fact]
        public void EmptyToList()
        {
            Assert.Equal(new[] { 9 }, Enumerable.Empty<int>().DefaultIfEmpty(9).ToList());
        }

        [Fact]
        public void Count()
        {
            int[] source = { 3, -1, 0, 10, 15 };

            Assert.Equal(5, source.DefaultIfEmpty().Count());
        }

        [Fact]
        public void EmptyCount()
        {
            Assert.Equal(1, Enumerable.Empty<int>().DefaultIfEmpty().Count());
        }
    }
}
