// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using Xunit;

namespace System.Linq.Tests
{
    public class DistinctTests : EnumerableTests
    {
        [Fact]
        public void SameResultsRepeatCallsIntQuery()
        {
            var q = from x in new[] { 0, 9999, 0, 888, -1, 66, -1, -777, 1, 2, -12345, 66, 66, -1, -1 }
                    where x > Int32.MinValue
                    select x;

            Assert.Equal(q.Distinct(), q.Distinct());
        }

        [Fact]
        public void SameResultsRepeatCallsStringQuery()
        {
            var q = from x in new[] { "!@#$%^", "C", "AAA", "Calling Twice", "SoS" }
                    where String.IsNullOrEmpty(x)
                    select x;


            Assert.Equal(q.Distinct(), q.Distinct());
        }

        [Fact]
        public void EmptySource()
        {
            int[] source = { };
            Assert.Empty(source.Distinct());
        }

        [Fact]
        public void EmptySourceRunOnce()
        {
            int[] source = { };
            Assert.Empty(source.RunOnce().Distinct());
        }

        [Fact]
        public void SingleNullElementExplicitlyUseDefaultComparer()
        {
            string[] source = { null };
            string[] expected = { null };

            Assert.Equal(expected, source.Distinct(EqualityComparer<string>.Default));
        }

        [Fact]
        public void EmptyStringDistinctFromNull()
        {
            string[] source = { null, null, string.Empty };
            string[] expected = { null, string.Empty };

            Assert.Equal(expected, source.Distinct(EqualityComparer<string>.Default));
        }

        [Fact]
        public void CollapsDuplicateNulls()
        {
            string[] source = { null, null };
            string[] expected = { null };

            Assert.Equal(expected, source.Distinct(EqualityComparer<string>.Default));
        }

        [Fact]
        public void SourceAllDuplicates()
        {
            int[] source = { 5, 5, 5, 5, 5, 5 };
            int[] expected = { 5 };

            Assert.Equal(expected, source.Distinct());
        }

        [Fact]
        public void AllUnique()
        {
            int[] source = { 2, -5, 0, 6, 10, 9 };

            Assert.Equal(source, source.Distinct());
        }

        [Fact]
        public void SomeDuplicatesIncludingNulls()
        {
            int?[] source = { 1, 1, 1, 2, 2, 2, null, null };
            int?[] expected = { 1, 2, null };

            Assert.Equal(expected, source.Distinct());
        }

        [Fact]
        public void SomeDuplicatesIncludingNullsRunOnce()
        {
            int?[] source = { 1, 1, 1, 2, 2, 2, null, null };
            int?[] expected = { 1, 2, null };

            Assert.Equal(expected, source.RunOnce().Distinct());
        }

        [Fact]
        public void LastSameAsFirst()
        {
            int[] source = { 1, 2, 3, 4, 5, 1 };
            int[] expected = { 1, 2, 3, 4, 5 };

            Assert.Equal(expected, source.Distinct());
        }

        // Multiple elements repeat non-consecutively
        [Fact]
        public void RepeatsNonConsecutive()
        {
            int[] source = { 1, 1, 2, 2, 4, 3, 1, 3, 2 };
            int[] expected = { 1, 2, 4, 3 };

            Assert.Equal(expected, source.Distinct());
        }

        [Fact]
        public void RepeatsNonConsecutiveRunOnce()
        {
            int[] source = { 1, 1, 2, 2, 4, 3, 1, 3, 2 };
            int[] expected = { 1, 2, 4, 3 };

            Assert.Equal(expected, source.RunOnce().Distinct());
        }

        [Fact]
        public void NullComparer()
        {
            string[] source = { "Bob", "Tim", "bBo", "miT", "Robert", "iTm" };
            string[] expected = { "Bob", "Tim", "bBo", "miT", "Robert", "iTm" };

            Assert.Equal(expected, source.Distinct());
        }

        [Fact]
        public void NullSource()
        {
            string[] source = null;

            AssertExtensions.Throws<ArgumentNullException>("source", () => source.Distinct());
        }

        [Fact]
        public void NullSourceCustomComparer()
        {
            string[] source = null;

            AssertExtensions.Throws<ArgumentNullException>("source", () => source.Distinct(StringComparer.Ordinal));
        }

        [Fact]
        public void CustomEqualityComparer()
        {
            string[] source = { "Bob", "Tim", "bBo", "miT", "Robert", "iTm" };
            string[] expected = { "Bob", "Tim", "Robert" };

            Assert.Equal(expected, source.Distinct(new AnagramEqualityComparer()), new AnagramEqualityComparer());
        }

        [Fact]
        public void CustomEqualityComparerRunOnce()
        {
            string[] source = { "Bob", "Tim", "bBo", "miT", "Robert", "iTm" };
            string[] expected = { "Bob", "Tim", "Robert" };

            Assert.Equal(expected, source.RunOnce().Distinct(new AnagramEqualityComparer()), new AnagramEqualityComparer());
        }

        [Theory, MemberData(nameof(SequencesWithDuplicates))]
        public void FindDistinctAndValidate<T>(T unusedArgumentToForceTypeInference, IEnumerable<T> original)
        {
            // Convert to list to avoid repeated enumerations of the enumerables.
            var originalList = original.ToList();
            var distinctList = originalList.Distinct().ToList();

            // Ensure the result doesn't contain duplicates.
            var hashSet = new HashSet<T>();
            foreach (var i in distinctList)
                Assert.True(hashSet.Add(i));

            var originalSet = new HashSet<T>(original);
            Assert.Superset(originalSet, hashSet);
            Assert.Subset(originalSet, hashSet);
        }

        public static IEnumerable<object[]> SequencesWithDuplicates()
        {
            // Validate an array of different numeric data types.
            yield return new object[] { 0, new int[] { 1, 1, 1, 2, 3, 5, 5, 6, 6, 10 } };
            yield return new object[] { 0L, new long[] { 1, 1, 1, 2, 3, 5, 5, 6, 6, 10 } };
            yield return new object[] { 0F, new float[] { 1, 1, 1, 2, 3, 5, 5, 6, 6, 10 } };
            yield return new object[] { 0.0, new double[] { 1, 1, 1, 2, 3, 5, 5, 6, 6, 10 } };
            yield return new object[] { 0M, new decimal[] { 1, 1, 1, 2, 3, 5, 5, 6, 6, 10 } };
            // Try strings
            yield return new object[] { "", new []
                {
                    "add",
                    "add",
                    "subtract",
                    "multiply",
                    "divide",
                    "divide2",
                    "subtract",
                    "add",
                    "power",
                    "exponent",
                    "hello",
                    "class",
                    "namespace",
                    "namespace",
                    "namespace",
                }
            };
        }

        [Fact]
        public void ForcedToEnumeratorDoesntEnumerate()
        {
            var iterator = NumberRangeGuaranteedNotCollectionType(0, 3).Distinct();
            // Don't insist on this behaviour, but check it's correct if it happens
            var en = iterator as IEnumerator<int>;
            Assert.False(en != null && en.MoveNext());
        }

        [Fact]
        public void ToArray()
        {
            int?[] source = { 1, 1, 1, 2, 2, 2, null, null };
            int?[] expected = { 1, 2, null };

            Assert.Equal(expected, source.Distinct().ToArray());
        }

        [Fact]
        public void ToList()
        {
            int?[] source = { 1, 1, 1, 2, 2, 2, null, null };
            int?[] expected = { 1, 2, null };

            Assert.Equal(expected, source.Distinct().ToList());
        }

        [Fact]
        public void Count()
        {
            int?[] source = { 1, 1, 1, 2, 2, 2, null, null };
            Assert.Equal(3, source.Distinct().Count());
        }

        [Fact]
        public void RepeatEnumerating()
        {
            int?[] source = { 1, 1, 1, 2, 2, 2, null, null };

            var result = source.Distinct();

            Assert.Equal(result, result);
        }
    }
}
