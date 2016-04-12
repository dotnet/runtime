// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using Xunit;

namespace System.Linq.Tests
{
    public class ExceptTests : EnumerableTests
    {
        // Class which is passed as an argument for EqualityComparer
        [Fact]
        public void SameResultsRepeatCallsIntQuery()
        {
            var q1 = from x1 in new int?[] { 2, 3, null, 2, null, 4, 5 }
                     select x1;
            var q2 = from x2 in new int?[] { 1, 9, null, 4 }
                     select x2;

            Assert.Equal(q1.Except(q2), q1.Except(q2));
        }

        [Fact]
        public void SameResultsRepeatCallsStringQuery()
        {
            var q1 = from x1 in new[] { "AAA", String.Empty, "q", "C", "#", "!@#$%^", "0987654321", "Calling Twice" }
                     select x1;
            var q2 = from x2 in new[] { "!@#$%^", "C", "AAA", "", "Calling Twice", "SoS" }
                     select x2;

            var rst1 = q1.Except(q2);
            var rst2 = q1.Except(q2);

            Assert.Equal(q1.Except(q2), q1.Except(q2));
        }

        [Fact]
        public void BothEmpty()
        {
            int[] first = { };
            int[] second = { };
            Assert.Empty(first.Except(second));
        }

        [Fact]
        public void SingleNullWithEmpty()
        {
            string[] first = { null };
            string[] second = new string[0];
            string[] expected = { null };

            Assert.Equal(expected, first.Except(second, EqualityComparer<string>.Default));
        }

        [Fact]
        public void NullEmptyStringMix()
        {
            string[] first = { null, null, string.Empty };
            string[] second = { null };
            string[] expected = { string.Empty };

            Assert.Equal(expected, first.Except(second, EqualityComparer<string>.Default));
        }

        [Fact]
        public void DoubleNullWithEmpty()
        {
            string[] first = { null, null };
            string[] second = new string[0];
            string[] expected = { null };

            Assert.Equal(expected, first.Except(second, EqualityComparer<string>.Default));
        }

        [Fact]
        public void EmptyWithNonEmpty()
        {
            int[] first = { };
            int[] second = { -6, -8, -6, 2, 0, 0, 5, 6 };
            Assert.Empty(first.Except(second));
        }

        [Fact]
        public void NonEmptyWithEmpty()
        {
            int?[] first = { -6, -8, -6, 2, 0, 0, 5, 6, null, null };
            int?[] second = { };
            int?[] expected = { -6, -8, 2, 0, 5, 6, null };

            Assert.Equal(expected, first.Except(second));
        }

        [Fact]
        public void EachHasRepeatsBetweenAndAmongstThemselves()
        {
            int?[] first = { 1, 2, 2, 3, 4, 5 };
            int?[] second = { 5, 3, 2, 6, 6, 3, 1, null, null };
            int?[] expected = { 4 };

            Assert.Equal(expected, first.Except(second));
        }

        [Fact]
        public void ReatedInFirstAndAllInSecondNotInFirst()
        {
            int[] first = { 1, 1, 1, 1, 1 };
            int[] second = { 2, 3, 4 };
            int[] expected = { 1 };

            Assert.Equal(expected, first.Except(second));
        }

        [Fact]
        public void OrderPreserved()
        {
            int?[] first = { 2, 3, null, 2, null, 4, 5 };
            int?[] second = { 1, 9, null, 4 };
            int?[] expected = { 2, 3, 5 };

            Assert.Equal(expected, first.Except(second));
        }

        [Fact]
        public void NullEqualityComparer()
        {
            string[] first = { "Bob", "Tim", "Robert", "Chris" };
            string[] second = { "bBo", "shriC" };
            string[] expected = { "Bob", "Tim", "Robert", "Chris" };

            Assert.Equal(expected, first.Except(second, null));
        }

        [Fact]
        public void CustomComparer()
        {
            string[] first = { "Bob", "Tim", "Robert", "Chris" };
            string[] second = { "bBo", "shriC" };
            string[] expected = { "Tim", "Robert" };

            Assert.Equal(expected, first.Except(second, new AnagramEqualityComparer()));
        }

        [Fact]
        public void FirstNullCustomComparer()
        {
            string[] first = null;
            string[] second = { "bBo", "shriC" };

            var ane = Assert.Throws<ArgumentNullException>("first", () => first.Except(second, new AnagramEqualityComparer()));
        }

        [Fact]
        public void SecondNullCustomComparer()
        {
            string[] first = { "Bob", "Tim", "Robert", "Chris" };
            string[] second = null;

            var ane = Assert.Throws<ArgumentNullException>("second", () => first.Except(second, new AnagramEqualityComparer()));
        }

        [Fact]
        public void FirstNullNoComparer()
        {
            string[] first = null;
            string[] second = { "bBo", "shriC" };

            var ane = Assert.Throws<ArgumentNullException>("first", () => first.Except(second));
        }

        [Fact]
        public void SecondNullNoComparer()
        {
            string[] first = { "Bob", "Tim", "Robert", "Chris" };
            string[] second = null;

            var ane = Assert.Throws<ArgumentNullException>("second", () => first.Except(second));
        }

        [Fact]
        public void ForcedToEnumeratorDoesntEnumerate()
        {
            var iterator = NumberRangeGuaranteedNotCollectionType(0, 3).Except(Enumerable.Range(0, 3));
            // Don't insist on this behaviour, but check it's correct if it happens
            var en = iterator as IEnumerator<int>;
            Assert.False(en != null && en.MoveNext());
        }
    }
}
