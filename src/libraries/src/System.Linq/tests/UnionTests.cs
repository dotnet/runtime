// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using Xunit;

namespace System.Linq.Tests
{
    public class UnionTests : EnumerableTests
    {        
        [Fact]
        public void SameResultsRepeatCallsIntQuery()
        {
            var q1 = from x1 in new int?[] { 2, 3, null, 2, null, 4, 5 }
                     select x1;
            var q2 = from x2 in new int?[] { 1, 9, null, 4 }
                     select x2;
                     
            Assert.Equal(q1.Union(q2), q1.Union(q2));
        }
        
        [Fact]
        public void SameResultsRepeatCallsStringQuery()
        {
            var q1 = from x1 in new[] { "AAA", String.Empty, "q", "C", "#", "!@#$%^", "0987654321", "Calling Twice" }
                     select x1;
            var q2 = from x2 in new[] { "!@#$%^", "C", "AAA", "", "Calling Twice", "SoS" }
                     select x2;
                     
            Assert.Equal(q1.Union(q2), q1.Union(q2));
        }

        [Fact]
        public void BothEmpty()
        {
            int[] first = { };
            int[] second = { };
            Assert.Empty(first.Union(second));
        }

        [Fact]
        public void CustomComparer()
        {
            string[] first = { "Bob", "Robert", "Tim", "Matt", "miT" };
            string[] second = { "ttaM", "Charlie", "Bbo" };
            string[] expected = { "Bob", "Robert", "Tim", "Matt", "Charlie" };
            
            var comparer = new AnagramEqualityComparer();

            Assert.Equal(expected, first.Union(second, comparer), comparer);
        }

        [Fact]
        public void FirstNullCustomComparer()
        {
            string[] first = null;
            string[] second = { "ttaM", "Charlie", "Bbo" };
            
            var ane = Assert.Throws<ArgumentNullException>("first", () => first.Union(second, new AnagramEqualityComparer()));
        }

        [Fact]
        public void SecondNullCustomComparer()
        {
            string[] first = { "Bob", "Robert", "Tim", "Matt", "miT" };
            string[] second = null;

            var ane = Assert.Throws<ArgumentNullException>("second", () => first.Union(second, new AnagramEqualityComparer()));
        }

        [Fact]
        public void FirstNullNoComparer()
        {
            string[] first = null;
            string[] second = { "ttaM", "Charlie", "Bbo" };

            var ane = Assert.Throws<ArgumentNullException>("first", () => first.Union(second));
        }

        [Fact]
        public void SecondNullNoComparer()
        {
            string[] first = { "Bob", "Robert", "Tim", "Matt", "miT" };
            string[] second = null;

            var ane = Assert.Throws<ArgumentNullException>("second", () => first.Union(second));
        }

        [Fact]
        public void SingleNullWithEmpty()
        {
            string[] first = { null };
            string[] second = new string[0];
            string[] expected = { null };
            
            Assert.Equal(expected, first.Union(second, EqualityComparer<string>.Default));
        }

        [Fact]
        public void NullEmptyStringMix()
        {
            string[] first = { null, null, string.Empty };
            string[] second = { null, null };
            string[] expected = { null, string.Empty };

            Assert.Equal(expected, first.Union(second, EqualityComparer<string>.Default));
        }

        [Fact]
        public void DoubleNullWithEmpty()
        {
            string[] first = { null, null };
            string[] second = new string[0];
            string[] expected = { null };

            Assert.Equal(expected, first.Union(second, EqualityComparer<string>.Default));
        }

        [Fact]
        public void EmptyWithNonEmpty()
        {
            int[] first = { };
            int[] second = { 2, 4, 5, 3, 2, 3, 9 };
            int[] expected = { 2, 4, 5, 3, 9 };

            Assert.Equal(expected, first.Union(second));
        }

        [Fact]
        public void NonEmptyWithEmpty()
        {
            int[] first = { 2, 4, 5, 3, 2, 3, 9 };
            int[] second = { };
            int[] expected = { 2, 4, 5, 3, 9 };

            Assert.Equal(expected, first.Union(second));
        }

        [Fact]
        public void CommonElementsShared()
        {
            int[] first = { 1, 2, 3, 4, 5, 6 };
            int[] second = { 6, 7, 7, 7, 8, 1 };
            int[] expected = { 1, 2, 3, 4, 5, 6, 7, 8 };

            Assert.Equal(expected, first.Union(second));
        }

        [Fact]
        public void SameElementRepeated()
        {
            int[] first = { 1, 1, 1, 1, 1, 1 };
            int[] second = { 1, 1, 1, 1, 1, 1 };
            int[] expected = { 1 };

            Assert.Equal(expected, first.Union(second));
        }

        [Fact]
        public void RepeatedElementsWithSingleElement()
        {
            int[] first = { 1, 2, 3, 5, 3, 6 };
            int[] second = { 7 };
            int[] expected = { 1, 2, 3, 5, 6, 7 };

            Assert.Equal(expected, first.Union(second));
        }

        [Fact]
        public void SingleWithAllUnique()
        {
            int?[] first = { 2 };
            int?[] second = { 3, null, 4, 5 };
            int?[] expected = { 2, 3, null, 4, 5 };

            Assert.Equal(expected, first.Union(second));
        }

        [Fact]
        public void EachHasRepeatsBetweenAndAmongstThemselves()
        {
            int?[] first = { 1, 2, 3, 4, null, 5, 1 };
            int?[] second = { 6, 2, 3, 4, 5, 6 };
            int?[] expected = { 1, 2, 3, 4, null, 5, 6 };

            Assert.Equal(expected, first.Union(second));
        }

        [Fact]
        public void NullEqualityComparer()
        {
            string[] first = { "Bob", "Robert", "Tim", "Matt", "miT" };
            string[] second = { "ttaM", "Charlie", "Bbo" };
            string[] expected = { "Bob", "Robert", "Tim", "Matt", "miT", "ttaM", "Charlie", "Bbo" };

            Assert.Equal(expected, first.Union(second, null));
        }

        [Fact]
        public void ForcedToEnumeratorDoesntEnumerate()
        {
            var iterator = NumberRangeGuaranteedNotCollectionType(0, 3).Union(Enumerable.Range(0, 3));
            // Don't insist on this behaviour, but check its correct if it happens
            var en = iterator as IEnumerator<int>;
            Assert.False(en != null && en.MoveNext());
        }

        [Fact]
        public void ToArray()
        {
            string[] first = { "Bob", "Robert", "Tim", "Matt", "miT" };
            string[] second = { "ttaM", "Charlie", "Bbo" };
            string[] expected = { "Bob", "Robert", "Tim", "Matt", "miT", "ttaM", "Charlie", "Bbo" };

            Assert.Equal(expected, first.Union(second).ToArray());
        }

        [Fact]
        public void ToList()
        {
            string[] first = { "Bob", "Robert", "Tim", "Matt", "miT" };
            string[] second = { "ttaM", "Charlie", "Bbo" };
            string[] expected = { "Bob", "Robert", "Tim", "Matt", "miT", "ttaM", "Charlie", "Bbo" };

            Assert.Equal(expected, first.Union(second).ToList());
        }

        [Fact]
        public void Count()
        {
            string[] first = { "Bob", "Robert", "Tim", "Matt", "miT" };
            string[] second = { "ttaM", "Charlie", "Bbo" };

            Assert.Equal(8, first.Union(second).Count());
        }

        [Fact]
        public void RepeatEnumerating()
        {
            string[] first = { "Bob", "Robert", "Tim", "Matt", "miT" };
            string[] second = { "ttaM", "Charlie", "Bbo" };

            var result = first.Union(second);

            Assert.Equal(result, result);
        }
    }
}
