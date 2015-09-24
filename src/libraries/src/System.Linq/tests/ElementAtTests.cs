// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Xunit;

namespace System.Linq.Tests
{
    public class ElementAtTests : EnumerableTests
    {
        [Fact]
        public void SameResultsRepeatCallsIntQuery()
        {
            var q = from x in new[] { 9999, 0, 888, -1, 66, -777, 1, 2, -12345 }
                    where x > Int32.MinValue
                    select x;

            Assert.Equal(q.ElementAt(3), q.ElementAt(3));
        }

        [Fact]
        public void SameResultsRepeatCallsStringQuery()
        {
            var q = from x in new[] { "!@#$%^", "C", "AAA", "", "Calling Twice", "SoS", String.Empty }
                    where !String.IsNullOrEmpty(x)
                    select x;

            Assert.Equal(q.ElementAt(4), q.ElementAt(4));
        }

        [Fact]
        public void SourceIListIndexNegative()
        {
            int?[] source = { 9, 8 };
            
            Assert.Throws<ArgumentOutOfRangeException>("index", () => source.ElementAt(-1));
        }

        [Fact]
        public void SourceSingleElementNotIListIndexZero()
        {
            IEnumerable<int> source = NumberRangeGuaranteedNotCollectionType(9, 1);
            
            Assert.Equal(9, source.ElementAt(0));
        }

        [Fact]
        public void SourceManyElementsNotIListIndexTargetsLast()
        {
            IEnumerable<int> source = NumberRangeGuaranteedNotCollectionType(9, 10);
            
            Assert.Equal(18, source.ElementAt(9));
        }

        [Fact]
        public void SourceManyElementsNotIListIndexTargetsMiddle()
        {
            IEnumerable<int> source = NumberRangeGuaranteedNotCollectionType(-4, 10);
            
            Assert.Equal(-1, source.ElementAt(3));
        }

        [Fact]
        public void SourceIListIndexEqualsCount()
        {
            int[] source = { 1, 2, 3, 4 };
            
            Assert.Throws<ArgumentOutOfRangeException>("index", () => source.ElementAt(source.Length));
        }

        [Fact]
        public void SourceIListEmptyIndexZero()
        {
            int[] source = { };
            
            Assert.Throws<ArgumentOutOfRangeException>("index", () => source.ElementAt(0));
        }

        [Fact]
        public void SourceIListSingleElementIndexZero()
        {
            int[] source = { -4 };
            
            Assert.Equal(-4, source.ElementAt(0));
        }

        [Fact]
        public void SourceIListManyElementsIndexTargetsLast()
        {
            int[] source = { 9, 8, 0, -5, 10 };
            
            Assert.Equal(10, source.ElementAt(source.Length - 1));
        }

        [Fact]
        public void SourceIListManyElementsIndexTargetsMiddle()
        {
            int?[] source = { 9, 8, null, -5, 10 };
            
            Assert.Null(source.ElementAt(2));
            Assert.Equal(-5, source.ElementAt(3));
        }

        [Fact]
        public void SourceNotIListIndexNegative()
        {
            IEnumerable<int> source = NumberRangeGuaranteedNotCollectionType(-4, 5);
            
            Assert.Throws<ArgumentOutOfRangeException>("index", () => source.ElementAt(-1));
        }

        [Fact]
        public void SourceNotIListIndexEqualsCount()
        {
            IEnumerable<int> source = NumberRangeGuaranteedNotCollectionType(5, 5);
            
            Assert.Throws<ArgumentOutOfRangeException>("index", () => source.ElementAt(5));
        }

        [Fact]
        public void SourceEmptyNotIListIndexZero()
        {
            IEnumerable<int> source = NumberRangeGuaranteedNotCollectionType(0, 0);
            
            Assert.Throws<ArgumentOutOfRangeException>("index", () => source.ElementAt(0));
        }

        [Fact]
        public void NullSource()
        {
            Assert.Throws<ArgumentNullException>("source", () => ((IEnumerable<int>)null).ElementAt(2));
        }
    }
}
