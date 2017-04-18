// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using Xunit;

namespace System.Linq.Tests
{
    public class OfTypeTests : EnumerableTests
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
            Assert.Empty(source.OfType<int>());
        }

        [Fact]
        public void LongSequenceFromIntSource()
        {
            int[] source = { 99, 45, 81 };
            Assert.Empty(source.OfType<long>());

        }

        [Fact]
        public void HeterogenousSourceNoAppropriateElements()
        {
            object[] source = { "Hello", 3.5, "Test" };
            Assert.Empty(source.OfType<int>());
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
        public void RunOnce()
        {
            object[] source = { 3.5m, -4, "Test", "Check", 4, 8.0, 10.5, 9 };
            int[] expected = { -4, 4, 9 };

            Assert.Equal(expected, source.RunOnce().OfType<int>());
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
            Assert.Empty(source.OfType<decimal?>());
        }

        [Fact]
        public void LongFromDouble()
        {
            long[] source = { 99L, 45L, 81L };
            Assert.Empty(source.OfType<double>());
        }

        [Fact]
        public void NullSource()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((IEnumerable<object>)null).OfType<string>());
        }

        [Fact]
        public void ForcedToEnumeratorDoesntEnumerate()
        {
            var iterator = NumberRangeGuaranteedNotCollectionType(0, 3).OfType<int>();
            // Don't insist on this behaviour, but check it's correct if it happens
            var en = iterator as IEnumerator<int>;
            Assert.False(en != null && en.MoveNext());
        }
    }
}
