// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using Xunit;
using Xunit.Abstractions;

namespace System.Linq.Tests
{
    public class SkipWhileTests : EnumerableTests
    {
        [Fact]
        public void SkipWhileAllTrue()
        {
            Assert.Equal(Enumerable.Empty<int>(), Enumerable.Range(0, 20).SkipWhile(i => i < 40));
            Assert.Equal(Enumerable.Empty<int>(), Enumerable.Range(0, 20).SkipWhile((i, idx) => i == idx));
        }

        [Fact]
        public void SkipWhileAllFalse()
        {
            Assert.Equal(Enumerable.Range(0, 20), Enumerable.Range(0, 20).SkipWhile(i => i != 0));
            Assert.Equal(Enumerable.Range(0, 20), Enumerable.Range(0, 20).SkipWhile((i, idx) => i != idx));
        }

        [Fact]
        public void SkipWhileThrowsOnNull()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((IEnumerable<int>)null).SkipWhile(i => i < 40));
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((IEnumerable<int>)null).SkipWhile((i, idx) => i == idx));
            AssertExtensions.Throws<ArgumentNullException>("predicate", () => Enumerable.Range(0, 20).SkipWhile((Func<int, int, bool>)null));
            AssertExtensions.Throws<ArgumentNullException>("predicate", () => Enumerable.Range(0, 20).SkipWhile((Func<int, bool>)null));
        }

        [Fact]
        public void SkipWhilePassesPredicateExceptionWhenEnumerated()
        {
            var source = Enumerable.Range(-2, 5).SkipWhile(i => 1 / i <= 0);
            using(var en = source.GetEnumerator())
            {
                Assert.Throws<DivideByZeroException>(() => en.MoveNext());
            }
        }

        [Fact]
        public void SkipWhileHalf()
        {
            Assert.Equal(Enumerable.Range(10, 10), Enumerable.Range(0, 20).SkipWhile(i => i < 10));
            Assert.Equal(Enumerable.Range(10, 10), Enumerable.Range(0, 20).SkipWhile((i, idx) => idx < 10));
        }

        [Fact]
        public void RunOnce()
        {
            Assert.Equal(Enumerable.Range(10, 10), Enumerable.Range(0, 20).RunOnce().SkipWhile(i => i < 10));
            Assert.Equal(Enumerable.Range(10, 10), Enumerable.Range(0, 20).RunOnce().SkipWhile((i, idx) => idx < 10));
        }

        [Fact]
        public void SkipErrorWhenSourceErrors()
        {
            var source = NumberRangeGuaranteedNotCollectionType(-2, 5).Select(i => (decimal)i).Select(m => 1 / m).Skip(4);
            using(var en = source.GetEnumerator())
            {
                Assert.Throws<DivideByZeroException>(() => en.MoveNext());
            }
        }

        [Fact]
        public void SameResultsRepeatCallsIntQuery()
        {
            var q = from x in new[] { 9999, 0, 888, -1, 66, -777, 1, 2, -12345 }
                    where x > int.MinValue
                    select x;

            Assert.Equal(q.SkipWhile(x => true), q.SkipWhile(x => true));
        }

        [Fact]
        public void SameResultsRepeatCallsStringQuery()
        {
            var q = from x in new[] { "!@#$%^", "C", "AAA", "", "Calling Twice", "SoS", string.Empty }
                    where !string.IsNullOrEmpty(x)
                    select x;

            Assert.Equal(q.SkipWhile(x => true), q.SkipWhile(x => true));
        }

        [Fact]
        public void PredicateManyFalseOnSecond()
        {
            int[] source = { 8, 3, 12, 4, 6, 10 };
            int[] expected = { 3, 12, 4, 6, 10 };

            Assert.Equal(expected, source.SkipWhile(e => e % 2 == 0));
        }

        [Fact]
        public void PredicateManyFalseOnSecondIndex()
        {
            int[] source = { 8, 3, 12, 4, 6, 10 };
            int[] expected = { 3, 12, 4, 6, 10 };
            
            Assert.Equal(expected, source.SkipWhile((e, i) => e % 2 == 0));
        }

        [Fact]
        public void PredicateTrueOnSecondFalseOnFirstAndOthers()
        {
            int[] source = { 3, 2, 4, 12, 6 };
            int[] expected = { 3, 2, 4, 12, 6 };

            Assert.Equal(expected, source.SkipWhile(e => e % 2 == 0));
        }

        [Fact]
        public void PredicateTrueOnSecondFalseOnFirstAndOthersIndex()
        {
            int[] source = { 3, 2, 4, 12, 6 };
            int[] expected = { 3, 2, 4, 12, 6 };

            Assert.Equal(expected, source.SkipWhile((e, i) => e % 2 == 0));
        }

        [Fact]
        public void FirstExcludedByIndex()
        {
            int[] source = { 6, 2, 5, 3, 8 };
            int[] expected = { 2, 5, 3, 8 };

            Assert.Equal(expected, source.SkipWhile((element, index) => index == 0));
        }

        [Fact]
        public void AllButLastExcludedByIndex()
        {
            int[] source = { 6, 2, 5, 3, 8 };
            int[] expected = { 8 };

            Assert.Equal(expected, source.SkipWhile((element, index) => index < source.Length - 1));
        }

        [Fact(Skip = "Valid test but too intensive to enable even in OuterLoop")]
        public void IndexSkipWhileOverflowBeyondIntMaxValueElements()
        {
            var skipped = new FastInfiniteEnumerator<int>().SkipWhile((e, i) => true);
            
            using(var en = skipped.GetEnumerator())
                Assert.Throws<OverflowException>(() =>
                {
                    while(en.MoveNext())
                    {
                    }
                });
        }

        [Fact]
        public void ForcedToEnumeratorDoesntEnumerate()
        {
            var iterator = NumberRangeGuaranteedNotCollectionType(0, 3).SkipWhile(e => true);
            // Don't insist on this behaviour, but check it's correct if it happens
            var en = iterator as IEnumerator<int>;
            Assert.False(en != null && en.MoveNext());
        }

        [Fact]
        public void ForcedToEnumeratorDoesntEnumerateIndexed()
        {
            var iterator = NumberRangeGuaranteedNotCollectionType(0, 3).SkipWhile((e, i) => true);
            // Don't insist on this behaviour, but check it's correct if it happens
            var en = iterator as IEnumerator<int>;
            Assert.False(en != null && en.MoveNext());
        }
    }
}
