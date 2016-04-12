// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using Xunit;

namespace System.Linq.Tests
{
    public class ConcatTests : EnumerableTests
    {
        [Fact]
        public void SameResultsRepeatCallsIntQuery()
        {
            var q1 = from x1 in new int?[] { 2, 3, null, 2, null, 4, 5 }
                     select x1;
            var q2 = from x2 in new int?[] { 1, 9, null, 4 }
                     select x2;

            Assert.Equal(q1.Concat(q2), q1.Concat(q2));
        }

        [Fact]
        public void SameResultsRepeatCallsStringQuery()
        {
            var q1 = from x1 in new[] { "AAA", String.Empty, "q", "C", "#", "!@#$%^", "0987654321", "Calling Twice" }
                     select x1;
            var q2 = from x2 in new[] { "!@#$%^", "C", "AAA", "", "Calling Twice", "SoS" }
                     select x2;

            Assert.Equal(q1.Concat(q2), q1.Concat(q2));
        }

        [Fact]
        public void BothEmpty()
        {
            int[] first = { };
            int[] second = { };
            Assert.Empty(first.Concat(second));
        }

        [Fact]
        public void EmptyAndNonEmpty()
        {
            int[] first = { };
            int[] second = { 2, 6, 4, 6, 2 };

            Assert.Equal(second, first.Concat(second));
        }

        [Fact]
        public void NonEmptyAndEmpty()
        {
            int[] first = { 2, 6, 4, 6, 2 };
            int[] second = { };

            Assert.Equal(first, first.Concat(second));
        }

        [Fact]
        public void NonEmptyAndNonEmpty()
        {
            int?[] first = { 2, null, 3, 5, 9 };
            int?[] second = { null, 8, 10 };
            int?[] expected = { 2, null, 3, 5, 9, null, 8, 10 };

            Assert.Equal(expected, first.Concat(second));
        }

        [Fact]
        public void ForcedToEnumeratorDoesntEnumerate()
        {
            var iterator = NumberRangeGuaranteedNotCollectionType(0, 3).Concat(Enumerable.Range(0, 3));
            // Don't insist on this behaviour, but check it's correct if it happens
            var en = iterator as IEnumerator<int>;
            Assert.False(en != null && en.MoveNext());
        }

        [Fact]
        public void FirstNull()
        {
            Assert.Throws<ArgumentNullException>("first", () => ((IEnumerable<int>)null).Concat(Enumerable.Range(0, 0)));
        }

        [Fact]
        public void SecondNull()
        {
            Assert.Throws<ArgumentNullException>("second", () => Enumerable.Range(0, 0).Concat(null));
        }

        [Fact]
        public void TwoEnumerableSources()
        {
            VerifyEquals(
                Enumerable.Range(0, 7),
                Enumerable.Range(0, 3).Concat(Enumerable.Range(3, 4)));
        }

        [Fact]
        public void TwoArraySources()
        {
            VerifyEquals(
                Enumerable.Range(0, 7),
                Enumerable.Range(0, 3).ToArray().Concat(Enumerable.Range(3, 4).ToArray()));
        }

        [Fact]
        public void TwoArraySelectSources()
        {
            VerifyEquals(
                Enumerable.Range(0, 7),
                Enumerable.Range(0, 3).ToArray().Select(i => i).Concat(Enumerable.Range(3, 4).ToArray().Select(i => i)));
        }

        [Fact]
        public void TwoNonCollectionSources()
        {
            VerifyEquals(
                Enumerable.Range(0, 7),
                NumberRangeGuaranteedNotCollectionType(0, 3).Concat(NumberRangeGuaranteedNotCollectionType(3, 4).ToArray().Select(i => i)));
        }

        [Fact]
        public void ThreeEnumerableSources()
        {
            VerifyEquals(
                Enumerable.Range(0, 12),
                Enumerable.Range(0, 3).Concat(Enumerable.Range(3, 4)).Concat(Enumerable.Range(7, 5)));
        }

        [Fact]
        public void FourEnumerableSources()
        {
            VerifyEquals(
                Enumerable.Range(0, 18),
                Enumerable.Range(0, 3).Concat(Enumerable.Range(3, 4)).Concat(Enumerable.Range(7, 5)).Concat(Enumerable.Range(12, 6)));
        }

        [Fact]
        public void FiveEnumerableSources()
        {
            VerifyEquals(
                Enumerable.Range(0, 25),
                Enumerable.Range(0, 3).Concat(Enumerable.Range(3, 4)).Concat(Enumerable.Range(7, 5)).Concat(Enumerable.Range(12, 6)).Concat(Enumerable.Range(18, 7)));
        }

        [Fact]
        public void FiveNonCollectionSources()
        {
            VerifyEquals(
                Enumerable.Range(0, 25),
                NumberRangeGuaranteedNotCollectionType(0, 3)
                .Concat(NumberRangeGuaranteedNotCollectionType(3, 4))
                .Concat(NumberRangeGuaranteedNotCollectionType(7, 5))
                .Concat(NumberRangeGuaranteedNotCollectionType(12, 6))
                .Concat(NumberRangeGuaranteedNotCollectionType(18, 7)));
        }

        [Fact]
        public void FiveListSources()
        {
            VerifyEquals(
                Enumerable.Range(0, 25),
                Enumerable.Range(0, 3).ToList()
                          .Concat(Enumerable.Range(3, 4).ToList())
                          .Concat(Enumerable.Range(7, 5).ToList())
                          .Concat(Enumerable.Range(12, 6).ToList())
                          .Concat(Enumerable.Range(18, 7).ToList()));
        }

        [Fact]
        public void ConcatOfConcats()
        {
            VerifyEquals(
                Enumerable.Range(0, 20),
                Enumerable.Concat(
                    Enumerable.Concat(
                        Enumerable.Range(0, 4),
                        Enumerable.Range(4, 6)),
                    Enumerable.Concat(
                        Enumerable.Range(10, 3),
                        Enumerable.Range(13, 7))));
        }

        [Fact]
        public void ConcatWithSelf()
        {
            IEnumerable<int> source = Enumerable.Repeat(1, 4).Concat(Enumerable.Repeat(1, 5));
            source = source.Concat(source);
            VerifyEquals(Enumerable.Repeat(1, 18), source);
        }

        private void VerifyEquals(IEnumerable<int> expected, IEnumerable<int> actual)
        {
            Assert.Equal(expected, actual);
            Assert.Equal(expected, actual.ToArray());
            Assert.Equal(expected, actual.ToList());
            Assert.Equal(expected, actual.Select(i => i).ToArray());
            Assert.Equal(expected, actual.Where(i => true).ToArray());
            Assert.Equal(expected, actual.OrderBy(i => i));
            Assert.Equal(expected, Enumerable.Empty<int>().Concat(actual));
            Assert.Equal(expected.Count(), actual.Count());
        }

        [Fact]
        public void ManyEmptyConcats()
        {
            IEnumerable<int> source = Enumerable.Empty<int>();
            for (int i = 0; i < 256; i++)
            {
                source = source.Concat(Enumerable.Empty<int>());
            }
            Assert.Equal(0, source.Count());
            Assert.Equal(Enumerable.Empty<int>(), source);
        }

        [Fact]
        public void ManyNonEmptyConcats()
        {
            IEnumerable<int> source = Enumerable.Empty<int>();
            for (int i = 0; i < 256; i++)
            {
                source = source.Concat(Enumerable.Repeat(i, 1));
            }
            Assert.Equal(256, source.Count());
            Assert.Equal(Enumerable.Range(0, 256), source);
        }
    }
}
