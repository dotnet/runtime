// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.Linq.Tests
{
    public class TakeWhileTests : EnumerableTests
    {
        [Fact]
        public void Empty()
        {
            Assert.Equal(Enumerable.Empty<int>(), Enumerable.Empty<int>().TakeWhile(i => i < 40));
            Assert.Equal(Enumerable.Empty<int>(), Enumerable.Empty<int>().TakeWhile((i, index) => i < 40));
        }

        [Fact]
        public void SameResultsRepeatCallsIntQuery()
        {
            var q = from x in new[] { 9999, 0, 888, -1, 66, -777, 1, 2, -12345 }
                    where x > int.MinValue
                    select x;

            Assert.Equal(q.TakeWhile(x => true), q.TakeWhile(x => true));
        }

        [Fact]
        public void SameResultsRepeatCallsStringQuery()
        {
            var q = from x in new[] { "!@#$%^", "C", "AAA", "", "Calling Twice", "SoS", string.Empty }
                    where !string.IsNullOrEmpty(x)
                    select x;

            Assert.Equal(q.TakeWhile(x => true), q.TakeWhile(x => true));
        }

        [Fact]
        public void SourceEmpty()
        {
            Assert.Empty(Enumerable.Empty<int>().TakeWhile(e => true));
        }

        [Fact]
        public void SourceEmptyIndexed()
        {
            Assert.Empty(Enumerable.Empty<int>().TakeWhile((e, i) => true));
        }

        [Fact]
        public void SourceNonEmptyPredicateFalseForAll()
        {
            int[] source = { 9, 7, 15, 3, 11 };
            Assert.Empty(source.TakeWhile(x => x % 2 == 0));
        }

        [Fact]
        public void SourceNonEmptyPredicateFalseForAllWithIndex()
        {
            int[] source = { 9, 7, 15, 3, 11 };
            Assert.Empty(source.TakeWhile((x, i) => x % 2 == 0));
        }

        [Fact]
        public void SourceNonEmptyPredicateTrueSomeFalseSecond()
        {
            int[] source = { 8, 3, 12, 4, 6, 10 };
            int[] expected = { 8 };

            Assert.Equal(expected, source.TakeWhile(x => x % 2 == 0));
        }

        [Fact]
        public void SourceNonEmptyPredicateTrueSomeFalseSecondWithIndex()
        {
            int[] source = { 8, 3, 12, 4, 6, 10 };
            int[] expected = { 8 };

            Assert.Equal(expected, source.TakeWhile((x, i) => x % 2 == 0));
        }

        [Fact]
        public void SourceNonEmptyPredicateTrueSomeFalseFirst()
        {
            int[] source = { 3, 2, 4, 12, 6 };
            Assert.Empty(source.TakeWhile(x => x % 2 == 0));
        }

        [Fact]
        public void SourceNonEmptyPredicateTrueSomeFalseFirstWithIndex()
        {
            int[] source = { 3, 2, 4, 12, 6 };
            Assert.Empty(source.TakeWhile((x, i) => x % 2 == 0));
        }

        [Fact]
        public void FirstTakenByIndex()
        {
            int[] source = { 6, 2, 5, 3, 8 };
            int[] expected = { 6 };

            Assert.Equal(expected, source.TakeWhile((element, index) => index == 0));
        }

        [Fact]
        public void AllButLastTakenByIndex()
        {
            int[] source = { 6, 2, 5, 3, 8 };
            int[] expected = { 6, 2, 5, 3 };

            Assert.Equal(expected, source.TakeWhile((element, index) => index < source.Length - 1));
        }

        [Fact]
        public void RunOnce()
        {
            int[] source = {8, 3, 12, 4, 6, 10};
            int[] expected = {8};
            Assert.Equal(expected, source.RunOnce().TakeWhile(x => x % 2 == 0));
            source = new[] {6, 2, 5, 3, 8};
            expected = new[] {6, 2, 5, 3};
            Assert.Equal(expected, source.RunOnce().TakeWhile((element, index) => index < source.Length - 1));
        }

        [ConditionalFact(typeof(TestEnvironment), nameof(TestEnvironment.IsStressModeEnabled))]
        public void IndexTakeWhileOverflowBeyondIntMaxValueElements()
        {
            var taken = new FastInfiniteEnumerator<int>().TakeWhile((e, i) => true);

            using(var en = taken.GetEnumerator())
                Assert.Throws<OverflowException>(() =>
                {
                    while(en.MoveNext())
                    {
                    }
                });
        }

        [Fact]
        public void ThrowsOnNullSource()
        {
            int[] source = null;
            AssertExtensions.Throws<ArgumentNullException>("source", () => source.TakeWhile(x => true));
        }

        [Fact]
        public void ThrowsOnNullPredicate()
        {
            int[] source = { 1, 2, 3 };
            Func<int, bool> nullPredicate = null;

            AssertExtensions.Throws<ArgumentNullException>("predicate", () => source.TakeWhile(nullPredicate));
        }

        [Fact]
        public void ThrowsOnNullSourceIndexed()
        {
            int[] source = null;
            AssertExtensions.Throws<ArgumentNullException>("source", () => source.TakeWhile((x, i) => true));
        }

        [Fact]
        public void ThrowsOnNullPredicateIndexed()
        {
            int[] source = { 1, 2, 3 };
            Func<int, int, bool> nullPredicate = null;

            AssertExtensions.Throws<ArgumentNullException>("predicate", () => source.TakeWhile(nullPredicate));
        }

        [Fact]
        public void ForcedToEnumeratorDoesntEnumerate()
        {
            var iterator = NumberRangeGuaranteedNotCollectionType(0, 3).TakeWhile(e => true);
            // Don't insist on this behaviour, but check it's correct if it happens
            var en = iterator as IEnumerator<int>;
            Assert.False(en != null && en.MoveNext());
        }

        [Fact]
        public void ForcedToEnumeratorDoesntEnumerateIndexed()
        {
            var iterator = NumberRangeGuaranteedNotCollectionType(0, 3).TakeWhile((e, i) => true);
            // Don't insist on this behaviour, but check it's correct if it happens
            var en = iterator as IEnumerator<int>;
            Assert.False(en != null && en.MoveNext());
        }
    }
}
