// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Xunit;

namespace System.Linq.Tests
{
    public class ContainsTests : EnumerableTests
    {
        [Fact]
        public void SameResultsRepeatCallsIntQuery()
        {
            var q = from x in new[] { 9999, 0, 888, -1, 66, -777, 1, 2, -12345 }
                    where x > Int32.MinValue
                    select x;

            Assert.Equal(q.Contains(-1), q.Contains(-1));
        }

        [Fact]
        public void SameResultsRepeatCallsStringQuery()
        {
            var q = from x in new[] { "!@#$%^", "C", "AAA", "", "Calling Twice", "SoS", String.Empty }
                    where !String.IsNullOrEmpty(x)
                    select x;

            Assert.Equal(q.Contains("X"), q.Contains("X"));
        }

        [Fact]
        public void SingleNullInICollectionT()
        {
            string[] source = { null };

            Assert.True(source.Contains(null, StringComparer.Ordinal));
        }

        [Fact]
        public void EmptyICollectionT()
        {
            int[] source = { };

            Assert.False(source.Contains(6));
        }

        [Fact]
        public void NotPresentICollectionT()
        {
            int[] source = { 8, 10, 3, 0, -8 };
            
            Assert.False(source.Contains(6));
        }

        [Fact]
        public void FirstElementMatchesICollectionT()
        {
            int[] source = { 8, 10, 3, 0, -8 };
            
            Assert.True(source.Contains(source[0]));
        }

        [Fact]
        public void LastElementMatchesICollectionT()
        {
            int[] source = { 8, 10, 3, 0, -8 };
            
            Assert.True(source.Contains(source[source.Length - 1]));
        }

        [Fact]
        public void MultipleMatchesICollectionT()
        {
            int[] source = { 8, 0, 10, 3, 0, -8, 0 };
            
            Assert.True(source.Contains(0));
        }

        [Fact]
        public void NullSoughtNoNullInICollectionT()
        {
            int?[] source = { 8, 0, 10, 3, 0, -8, 0 };
            
            Assert.False(source.Contains(null));
        }

        [Fact]
        public void NullMatchesInICollectionT()
        {
            int?[] source = { 8, 0, 10, null, 3, 0, -8, 0 };
            
            Assert.True(source.Contains(null));
        }

        [Fact]
        public void DefaultComparerFromNullICollectionT()
        {
            string[] source = { "Bob", "Robert", "Tim" };

            Assert.False(source.Contains("trboeR", null));
            Assert.True(source.Contains("Tim", null));
        }

        [Fact]
        public void CustomComparerFromNullICollectionT()
        {
            string[] source = { "Bob", "Robert", "Tim" };
            
            Assert.True(source.Contains("trboeR", new AnagramEqualityComparer()));
            Assert.False(source.Contains("nevar", new AnagramEqualityComparer()));
        }

        [Fact]
        public void EmptyNotICollectionT()
        {
            IEnumerable<int> source = NumberRangeGuaranteedNotCollectionType(0, 0);
            
            Assert.False(source.Contains(0));
        }

        [Fact]
        public void NotPresentNotICollectionT()
        {
            IEnumerable<int> source = NumberRangeGuaranteedNotCollectionType(4, 5);
            
            Assert.False(source.Contains(3));
        }

        [Fact]
        public void FirstElementMatchesNotICollectionT()
        {
            IEnumerable<int> source = NumberRangeGuaranteedNotCollectionType(3, 5);
            
            Assert.True(source.Contains(3));
        }

        [Fact]
        public void LastElementMatchesNotICollectionT()
        {
            IEnumerable<int> source = NumberRangeGuaranteedNotCollectionType(3, 5);
            
            Assert.True(source.Contains(7));
        }

        [Fact]
        public void PresentMultipleTimesNotICollectionT()
        {
            IEnumerable<int> source = RepeatedNumberGuaranteedNotCollectionType(10, 3);
            
            Assert.True(source.Contains(10));
        }

        [Fact]
        public void NullSoughtNoNullInNotICollectionT()
        {
            IEnumerable<int?> source = NullableNumberRangeGuaranteedNotCollectionType(3, 4);
            
            Assert.False(source.Contains(null));
        }

        [Fact]
        public void NullMatchesInNotICollectionT()
        {
            IEnumerable<int?> source = RepeatedNullableNumberGuaranteedNotCollectionType(null, 5);
            
            Assert.True(source.Contains(null));
        }
        
        [Fact]
        public void NullSource()
        {
            IEnumerable<int> source = null;
            
            Assert.Throws<ArgumentNullException>("source", () => source.Contains(42));
            Assert.Throws<ArgumentNullException>("source", () => source.Contains(42, EqualityComparer<int>.Default));
        }
    }
}
