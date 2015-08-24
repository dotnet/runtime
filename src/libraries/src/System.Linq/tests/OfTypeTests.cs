// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Xunit;

namespace System.Linq.Tests
{
    public class OfTypeTests
    {
        [Fact]
        public void SameResultsRepeatCallsIntQuery()
        {
            var q = from x in new[] { 9999, 0, 888, -1, 66, -777, 1, 2, -12345 }
                    where x > Int32.MinValue
                    select x;

            Assert.Equal(q.OfType<int>(), q.OfType<int>());
        }

        [Fact]
        public void SameResultsRepeatCallsStringQuery()
        {
            var q = from x in new[] { "!@#$%^", "C", "AAA", "", "Calling Twice", "SoS", String.Empty }
                    where String.IsNullOrEmpty(x)
                    select x;

            Assert.Equal(q.OfType<int>(), q.OfType<int>());
        }

        [Fact]
        public void EmptySource()
        {
            object[] source = { };
            int[] expected = { };

            Assert.Equal(expected, source.OfType<int>());
        }

        [Fact]
        public void LongSequenceFromIntSource()
        {
            int[] source = { 99, 45, 81 };
            long[] expected = { };

            Assert.Equal(expected, source.OfType<long>());

        }

        [Fact]
        public void HeterogenousSourceNoAppropriateElements()
        {
            object[] source = { "Hello", 3.5, "Test" };
            int[] expected = { };

            Assert.Equal(expected, source.OfType<int>());
        }

        [Fact]
        public void HeterogenousSourceOnlyFirstOfType()
        {
            object[] source = { 10, "Hello", 3.5, "Test" };
            int[] expected = { 10 };

            Assert.Equal(expected, source.OfType<int>());
        }

        [Fact]
        public void AllElementsOfNullableTypeNullsSkipped()
        {
            object[] source = { 10, -4, null, null, 4, 9 };
            int?[] expected = { 10, -4, 4, 9 };

            Assert.Equal(expected, source.OfType<int?>());
        }

        [Fact]
        public void HeterogenousSourceSomeOfType()
        {
            object[] source = { 3.5m, -4, "Test", "Check", 4, 8.0, 10.5, 9 };
            int[] expected = { -4, 4, 9 };

            Assert.Equal(expected, source.OfType<int>());
        }

        [Fact]
        public void IntFromNullableInt()
        {
            int[] source = { -4, 4, 9 };
            int?[] expected = { -4, 4, 9 };

            Assert.Equal(expected, source.OfType<int?>());
        }

        [Fact]
        public void IntFromNullableIntWithNulls()
        {
            int?[] source = { null, -4, 4, null, 9 };
            int[] expected = { -4, 4, 9 };

            Assert.Equal(expected, source.OfType<int>());
        }

        [Fact]
        public void NullableDecimalFromString()
        {
            string[] source = { "Test1", "Test2", "Test9" };
            decimal?[] expected = { };

            Assert.Equal(expected, source.OfType<decimal?>());
        }

        [Fact]
        public void LongFromDouble()
        {
            long[] source = { 99L, 45L, 81L };
            double[] expected = { };

            Assert.Equal(expected, source.OfType<double>());
        }
    }
}
