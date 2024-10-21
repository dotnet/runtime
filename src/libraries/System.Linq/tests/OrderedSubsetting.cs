// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Xunit;

namespace System.Linq.Tests
{
    public class OrderedSubsetting : EnumerableTests
    {
        [Fact]
        public void FirstMultipleTruePredicateResult()
        {
            Assert.Equal(10, Enumerable.Range(1, 99).OrderBy(i => i).First(x => x % 10 == 0));
            Assert.Equal(100, Enumerable.Range(1, 999).Concat(Enumerable.Range(1001, 3)).OrderByDescending(i => i.ToString().Length).ThenBy(i => i).First(x => x % 10 == 0));
        }

        [Fact]
        public void FirstOrDefaultMultipleTruePredicateResult()
        {
            Assert.Equal(10, Enumerable.Range(1, 99).OrderBy(i => i).FirstOrDefault(x => x % 10 == 0));
            Assert.Equal(100, Enumerable.Range(1, 999).Concat(Enumerable.Range(1001, 3)).OrderByDescending(i => i.ToString().Length).ThenBy(i => i).FirstOrDefault(x => x % 10 == 0));
        }

        [Fact]
        public void FirstNoTruePredicateResult()
        {
            Assert.Throws<InvalidOperationException>(() => Enumerable.Range(1, 99).OrderBy(i => i).First(x => x > 1000));
        }

        [Fact]
        public void FirstEmptyOrderedEnumerable()
        {
            Assert.Throws<InvalidOperationException>(() => Enumerable.Empty<int>().OrderBy(i => i).First());
            Assert.Throws<InvalidOperationException>(() => Enumerable.Empty<int>().OrderBy(i => i).First(x => true));
        }

        [Fact]
        public void FirstOrDefaultNoTruePredicateResult()
        {
            Assert.Equal(0, Enumerable.Range(1, 99).OrderBy(i => i).FirstOrDefault(x => x > 1000));
        }

        [Fact]
        public void FirstOrDefaultEmptyOrderedEnumerable()
        {
            Assert.Equal(0, Enumerable.Empty<int>().OrderBy(i => i).FirstOrDefault());
            Assert.Equal(0, Enumerable.Empty<int>().OrderBy(i => i).FirstOrDefault(x => true));
        }

        [Fact]
        public void Last()
        {
            Assert.Equal(10, Enumerable.Range(1, 99).Reverse().OrderByDescending(i => i).Last(x => x % 10 == 0));
            Assert.Equal(100, Enumerable.Range(1, 999).Concat(Enumerable.Range(1001, 3)).Reverse().OrderBy(i => i.ToString().Length).ThenByDescending(i => i).Last(x => x % 10 == 0));
            Assert.Equal(10, Enumerable.Range(1, 10).OrderBy(i => 1).Last());
        }

        [Fact]
        public void LastMultipleTruePredicateResult()
        {
            Assert.Equal(90, Enumerable.Range(1, 99).OrderBy(i => i).Last(x => x % 10 == 0));
            Assert.Equal(90, Enumerable.Range(1, 999).Concat(Enumerable.Range(1001, 3)).OrderByDescending(i => i.ToString().Length).ThenBy(i => i).Last(x => x % 10 == 0));
        }

        [Fact]
        public void LastOrDefaultMultipleTruePredicateResult()
        {
            Assert.Equal(90, Enumerable.Range(1, 99).OrderBy(i => i).LastOrDefault(x => x % 10 == 0));
            Assert.Equal(90, Enumerable.Range(1, 999).Concat(Enumerable.Range(1001, 3)).OrderByDescending(i => i.ToString().Length).ThenBy(i => i).LastOrDefault(x => x % 10 == 0));
        }

        [Fact]
        public void LastNoTruePredicateResult()
        {
            Assert.Throws<InvalidOperationException>(() => Enumerable.Range(1, 99).OrderBy(i => i).Last(x => x > 1000));
        }

        [Fact]
        public void LastEmptyOrderedEnumerable()
        {
            Assert.Throws<InvalidOperationException>(() => Enumerable.Empty<int>().OrderBy(i => i).Last());
            Assert.Throws<InvalidOperationException>(() => Enumerable.Empty<int>().OrderBy(i => i).Last(x => true));
        }

        [Fact]
        public void LastOrDefaultNoTruePredicateResult()
        {
            Assert.Equal(0, Enumerable.Range(1, 99).OrderBy(i => i).LastOrDefault(x => x > 1000));
        }

        [Fact]
        public void LastOrDefaultEmptyOrderedEnumerable()
        {
            Assert.Equal(0, Enumerable.Empty<int>().OrderBy(i => i).LastOrDefault());
            Assert.Equal(0, Enumerable.Empty<int>().OrderBy(i => i).LastOrDefault(x => true));
        }

        [Fact]
        public void Take()
        {
            var source = Enumerable.Range(0, 100).Shuffle().ToArray();
            var ordered = source.OrderBy(i => i);
            Assert.Equal(Enumerable.Range(0, 20), ordered.Take(20));
            Assert.Equal(Enumerable.Range(0, 30), ordered.Take(50).Take(30));
            Assert.Empty(ordered.Take(0));
            Assert.Empty(ordered.Take(-1));
            Assert.Empty(ordered.Take(int.MinValue));
            Assert.Equal(new int[] { 0 }, ordered.Take(1));
            Assert.Equal(Enumerable.Range(0, 100), ordered.Take(101));
            Assert.Equal(Enumerable.Range(0, 100), ordered.Take(int.MaxValue));
            Assert.Equal(Enumerable.Range(0, 100), ordered.Take(100));
            Assert.Equal(Enumerable.Range(0, 99), ordered.Take(99));
            Assert.Equal(Enumerable.Range(0, 100), ordered);
        }

        [Fact]
        public void TakeThenFirst()
        {
            var source = Enumerable.Range(0, 100).Shuffle().ToArray();
            var ordered = source.OrderBy(i => i);
            Assert.Equal(0, ordered.Take(20).First());
            Assert.Equal(0, ordered.Take(20).FirstOrDefault());
        }

        [Fact]
        public void TakeThenLast()
        {
            var source = Enumerable.Range(0, 100).Shuffle().ToArray();
            var ordered = source.OrderBy(i => i);
            Assert.Equal(19, ordered.Take(20).Last());
            Assert.Equal(19, ordered.Take(20).LastOrDefault());
        }

        [Fact]
        public void Skip()
        {
            var source = Enumerable.Range(0, 100).Shuffle().ToArray();
            var ordered = source.OrderBy(i => i);
            Assert.Equal(Enumerable.Range(20, 80), ordered.Skip(20));
            Assert.Equal(Enumerable.Range(80, 20), ordered.Skip(50).Skip(30));
            Assert.Equal(20, ordered.Skip(20).First());
            Assert.Equal(20, ordered.Skip(20).FirstOrDefault());
            Assert.Equal(Enumerable.Range(0, 100), ordered.Skip(0));
            Assert.Equal(Enumerable.Range(0, 100), ordered.Skip(-1));
            Assert.Equal(Enumerable.Range(0, 100), ordered.Skip(int.MinValue));
            Assert.Equal(new int[] { 99 }, ordered.Skip(99));
            Assert.Empty(ordered.Skip(101));
            Assert.Empty(ordered.Skip(int.MaxValue));
            Assert.Empty(ordered.Skip(100));
            Assert.Equal(Enumerable.Range(1, 99), ordered.Skip(1));
            Assert.Equal(Enumerable.Range(0, 100), ordered);
        }

        [Fact]
        public void SkipThenFirst()
        {
            var source = Enumerable.Range(0, 100).Shuffle().ToArray();
            var ordered = source.OrderBy(i => i);
            Assert.Equal(20, ordered.Skip(20).First());
            Assert.Equal(20, ordered.Skip(20).FirstOrDefault());
        }

        [Fact]
        public void SkipExcessiveThenFirstThrows()
        {
            Assert.Throws<InvalidOperationException>(() => Enumerable.Range(2, 10).Shuffle().OrderBy(i => i).Skip(20).First());
        }

        [Fact]
        public void SkipExcessiveThenFirstOrDefault()
        {
            Assert.Equal(0, Enumerable.Range(2, 10).Shuffle().OrderBy(i => i).Skip(20).FirstOrDefault());
        }

        [Fact]
        public void SkipExcessiveEmpty()
        {
            Assert.Empty(Enumerable.Range(0, 10).Shuffle().OrderBy(i => i).Skip(42));
        }

        [Fact]
        public void SkipThenLast()
        {
            var source = Enumerable.Range(0, 100).Shuffle().ToArray();
            var ordered = source.OrderBy(i => i);
            Assert.Equal(99, ordered.Skip(20).Last());
            Assert.Equal(99, ordered.Skip(20).LastOrDefault());
        }

        [Fact]
        public void SkipExcessiveThenLastThrows()
        {
            Assert.Throws<InvalidOperationException>(() => Enumerable.Range(2, 10).Shuffle().OrderBy(i => i).Skip(20).Last());
        }

        [Fact]
        public void SkipExcessiveThenLastOrDefault()
        {
            Assert.Equal(0, Enumerable.Range(2, 10).Shuffle().OrderBy(i => i).Skip(20).LastOrDefault());
        }

        [Fact]
        public void SkipAndTake()
        {
            var source = Enumerable.Range(0, 100).Shuffle().ToArray();
            var ordered = source.OrderBy(i => i);
            Assert.Equal(Enumerable.Range(20, 60), ordered.Skip(20).Take(60));
            Assert.Equal(Enumerable.Range(30, 20), ordered.Skip(20).Skip(10).Take(50).Take(20));
            Assert.Equal(Enumerable.Range(30, 20), ordered.Skip(20).Skip(10).Take(20).Take(int.MaxValue));
            Assert.Empty(ordered.Skip(10).Take(9).Take(0));
            Assert.Empty(ordered.Skip(200).Take(10));
            Assert.Empty(ordered.Skip(3).Take(0));
        }

        [Fact]
        public void TakeAndSkip()
        {
            var source = Enumerable.Range(0, 100).Shuffle().ToArray();
            var ordered = source.OrderBy(i => i);
            Assert.Empty(ordered.Skip(100).Take(20));
            Assert.Equal(Enumerable.Range(10, 20), ordered.Take(30).Skip(10));
            Assert.Equal(Enumerable.Range(10, 1), ordered.Take(11).Skip(10));
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsSpeedOptimized))]
        public void TakeAndSkip_DoesntIterateRangeUnlessNecessary()
        {
            Assert.Empty(Enumerable.Range(0, int.MaxValue).Take(int.MaxValue).OrderBy(i => i).Skip(int.MaxValue - 4).Skip(15));
        }

        [Fact]
        public void TakeThenTakeExcessive()
        {
            var source = Enumerable.Range(0, 100).Shuffle().ToArray();
            var ordered = source.OrderBy(i => i);
            Assert.Equal(ordered.Take(20), ordered.Take(20).Take(100));
        }

        [Fact]
        public void TakeThenSkipAll()
        {
            var source = Enumerable.Range(0, 100).Shuffle().ToArray();
            var ordered = source.OrderBy(i => i);
            Assert.Empty(ordered.Take(20).Skip(30));
        }

        [Fact]
        public void SkipAndTakeThenFirst()
        {
            var source = Enumerable.Range(0, 100).Shuffle().ToArray();
            var ordered = source.OrderBy(i => i);
            Assert.Equal(20, ordered.Skip(20).Take(60).First());
            Assert.Equal(20, ordered.Skip(20).Take(60).FirstOrDefault());
        }

        [Fact]
        public void SkipAndTakeThenLast()
        {
            var source = Enumerable.Range(0, 100).Shuffle().ToArray();
            var ordered = source.OrderBy(i => i);
            Assert.Equal(79, ordered.Skip(20).Take(60).Last());
            Assert.Equal(79, ordered.Skip(20).Take(60).LastOrDefault());
        }

        [Fact]
        public void ElementAt()
        {
            var source = Enumerable.Range(0, 100).Shuffle().ToArray();
            var ordered = source.OrderBy(i => i);
            Assert.Equal(42, ordered.ElementAt(42));
            Assert.Equal(93, ordered.ElementAt(93));
            Assert.Equal(99, ordered.ElementAt(99));
            Assert.Equal(42, ordered.ElementAtOrDefault(42));
            Assert.Equal(93, ordered.ElementAtOrDefault(93));
            Assert.Equal(99, ordered.ElementAtOrDefault(99));
            Assert.Throws<ArgumentOutOfRangeException>(() => ordered.ElementAt(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => ordered.ElementAt(100));
            Assert.Throws<ArgumentOutOfRangeException>(() => ordered.ElementAt(1000));
            Assert.Throws<ArgumentOutOfRangeException>(() => ordered.ElementAt(int.MinValue));
            Assert.Throws<ArgumentOutOfRangeException>(() => ordered.ElementAt(int.MaxValue));
            Assert.Equal(0, ordered.ElementAtOrDefault(-1));
            Assert.Equal(0, ordered.ElementAtOrDefault(100));
            Assert.Equal(0, ordered.ElementAtOrDefault(1000));
            Assert.Equal(0, ordered.ElementAtOrDefault(int.MinValue));
            Assert.Equal(0, ordered.ElementAtOrDefault(int.MaxValue));
            var skipped = ordered.Skip(10).Take(80);
            Assert.Equal(52, skipped.ElementAt(42));
            Assert.Equal(83, skipped.ElementAt(73));
            Assert.Equal(89, skipped.ElementAt(79));
            Assert.Equal(52, skipped.ElementAtOrDefault(42));
            Assert.Equal(83, skipped.ElementAtOrDefault(73));
            Assert.Equal(89, skipped.ElementAtOrDefault(79));
            Assert.Throws<ArgumentOutOfRangeException>(() => skipped.ElementAt(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => skipped.ElementAt(80));
            Assert.Throws<ArgumentOutOfRangeException>(() => skipped.ElementAt(1000));
            Assert.Throws<ArgumentOutOfRangeException>(() => skipped.ElementAt(int.MinValue));
            Assert.Throws<ArgumentOutOfRangeException>(() => skipped.ElementAt(int.MaxValue));
            Assert.Equal(0, skipped.ElementAtOrDefault(-1));
            Assert.Equal(0, skipped.ElementAtOrDefault(80));
            Assert.Equal(0, skipped.ElementAtOrDefault(1000));
            Assert.Equal(0, skipped.ElementAtOrDefault(int.MinValue));
            Assert.Equal(0, skipped.ElementAtOrDefault(int.MaxValue));
            skipped = ordered.Skip(1000).Take(20);
            Assert.Throws<ArgumentOutOfRangeException>(() => skipped.ElementAt(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => skipped.ElementAt(0));
            Assert.Throws<InvalidOperationException>(() => skipped.First());
            Assert.Throws<InvalidOperationException>(() => skipped.Last());
            Assert.Equal(0, skipped.ElementAtOrDefault(-1));
            Assert.Equal(0, skipped.ElementAtOrDefault(0));
            Assert.Equal(0, skipped.FirstOrDefault());
            Assert.Equal(0, skipped.LastOrDefault());
        }

        [Fact]
        public void ToArray()
        {
            Assert.Equal(Enumerable.Range(10, 20), Enumerable.Range(0, 100).Shuffle().OrderBy(i => i).Skip(10).Take(20).ToArray());
        }

        [Fact]
        public void ToList()
        {
            Assert.Equal(Enumerable.Range(10, 20), Enumerable.Range(0, 100).Shuffle().OrderBy(i => i).Skip(10).Take(20).ToList());
        }

        [Fact]
        public void Count()
        {
            Assert.Equal(20, Enumerable.Range(0, 100).Shuffle().OrderBy(i => i).Skip(10).Take(20).Count());
            Assert.Equal(1, Enumerable.Range(0, 100).Shuffle().OrderBy(i => i).Take(2).Skip(1).Count());
        }

        [Fact]
        public void SkipTakesOnlyOne()
        {
            Assert.Equal(new[] { 1 }, Enumerable.Range(1, 10).Shuffle().OrderBy(i => i).Take(1));
            Assert.Equal(new[] { 2 }, Enumerable.Range(1, 10).Shuffle().OrderBy(i => i).Skip(1).Take(1));
            Assert.Equal(new[] { 3 }, Enumerable.Range(1, 10).Shuffle().OrderBy(i => i).Take(3).Skip(2));
            Assert.Equal(new[] { 1 }, Enumerable.Range(1, 10).Shuffle().OrderBy(i => i).Take(3).Take(1));
        }

        [Fact]
        public void EmptyToArray()
        {
            Assert.Empty(Enumerable.Range(0, 100).Shuffle().OrderBy(i => i).Skip(100).ToArray());
        }

        [Fact]
        public void EmptyToList()
        {
            Assert.Empty(Enumerable.Range(0, 100).Shuffle().OrderBy(i => i).Skip(100).ToList());
        }

        [Fact]
        public void EmptyCount()
        {
            Assert.Equal(0, Enumerable.Range(0, 100).Shuffle().OrderBy(i => i).Skip(100).Count());
            Assert.Equal(0, Enumerable.Range(0, 100).Shuffle().OrderBy(i => i).Take(0).Count());
        }

        [Fact]
        public void AttemptedMoreArray()
        {
            Assert.Equal(Enumerable.Range(0, 20), Enumerable.Range(0, 20).Shuffle().OrderBy(i => i).Take(30).ToArray());
        }

        [Fact]
        public void AttemptedMoreList()
        {
            Assert.Equal(Enumerable.Range(0, 20), Enumerable.Range(0, 20).Shuffle().OrderBy(i => i).Take(30).ToList());
        }

        [Fact]
        public void AttemptedMoreCount()
        {
            Assert.Equal(20, Enumerable.Range(0, 20).Shuffle().OrderBy(i => i).Take(30).Count());
        }

        [Fact]
        public void SingleElementToArray()
        {
            Assert.Equal(Enumerable.Repeat(10, 1), Enumerable.Range(0, 20).Shuffle().OrderBy(i => i).Skip(10).Take(1).ToArray());
        }

        [Fact]
        public void SingleElementToList()
        {
            Assert.Equal(Enumerable.Repeat(10, 1), Enumerable.Range(0, 20).Shuffle().OrderBy(i => i).Skip(10).Take(1).ToList());
        }

        [Fact]
        public void SingleElementCount()
        {
            Assert.Equal(1, Enumerable.Range(0, 20).Shuffle().OrderBy(i => i).Skip(10).Take(1).Count());
        }

        [Fact]
        public void EnumeratorDoesntContinue()
        {
            var enumerator = NumberRangeGuaranteedNotCollectionType(0, 3).Shuffle().OrderBy(i => i).Skip(1).GetEnumerator();
            while (enumerator.MoveNext()) { }
            Assert.False(enumerator.MoveNext());
        }

        [Fact]
        public void Select()
        {
            Assert.Equal(new[] { 0, 2, 4, 6, 8 }, Enumerable.Range(-1, 8).Shuffle().OrderBy(i => i).Skip(1).Take(5).Select(i => i * 2));
        }

        [Fact]
        public void SelectForcedToEnumeratorDoesntEnumerate()
        {
            var iterator = Enumerable.Range(-1, 8).Shuffle().OrderBy(i => i).Skip(1).Take(5).Select(i => i * 2);
            // Don't insist on this behaviour, but check it's correct if it happens
            var en = iterator as IEnumerator<int>;
            Assert.False(en is not null && en.MoveNext());
        }

        [Fact]
        public void SelectElementAt()
        {
            var source = Enumerable.Range(0, 9).Shuffle().OrderBy(i => i).Skip(1).Take(5).Select(i => i * 2);
            Assert.Equal(6, source.ElementAt(2));
            Assert.Equal(8, source.ElementAtOrDefault(3));
            Assert.Equal(0, source.ElementAtOrDefault(8));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => source.ElementAt(-2));
        }

        [Fact]
        public void SelectFirst()
        {
            var source = Enumerable.Range(0, 9).Shuffle().OrderBy(i => i).Skip(1).Take(5).Select(i => i * 2);
            Assert.Equal(2, source.First());
            Assert.Equal(2, source.FirstOrDefault());
            source = source.Skip(20);
            Assert.Equal(0, source.FirstOrDefault());
            Assert.Throws<InvalidOperationException>(() => source.First());
        }

        [Fact]
        public void SelectLast()
        {
            var source = Enumerable.Range(0, 9).Shuffle().OrderBy(i => i).Skip(1).Take(5).Select(i => i * 2);
            Assert.Equal(10, source.Last());
            Assert.Equal(10, source.LastOrDefault());
            source = source.Skip(20);
            Assert.Equal(0, source.LastOrDefault());
            Assert.Throws<InvalidOperationException>(() => source.Last());
        }

        [Fact]
        public void SelectArray()
        {
            var source = Enumerable.Range(0, 9).Shuffle().OrderBy(i => i).Skip(1).Take(5).Select(i => i * 2);
            Assert.Equal(new[] { 2, 4, 6, 8, 10 }, source.ToArray());
        }

        [Fact]
        public void SelectList()
        {
            var source = Enumerable.Range(0, 9).Shuffle().OrderBy(i => i).Skip(1).Take(5).Select(i => i * 2);
            Assert.Equal(new[] { 2, 4, 6, 8, 10 }, source.ToList());
        }

        [Fact]
        public void SelectCount()
        {
            var source = Enumerable.Range(0, 9).Shuffle().OrderBy(i => i).Skip(1).Take(5).Select(i => i * 2);
            Assert.Equal(5, source.Count());
            source = Enumerable.Range(0, 9).Shuffle().OrderBy(i => i).Skip(1).Take(1000).Select(i => i * 2);
            Assert.Equal(8, source.Count());
        }

        [Fact]
        public void RunOnce()
        {
            var source = Enumerable.Range(0, 100).Shuffle().ToArray();
            Assert.Equal(Enumerable.Range(30, 20), source.RunOnce().OrderBy(i => i).Skip(20).Skip(10).Take(50).Take(20));
            Assert.Empty(source.RunOnce().OrderBy(i => i).Skip(10).Take(9).Take(0));
            Assert.Equal(20, source.RunOnce().OrderBy(i => i).Skip(20).Take(60).First());
            Assert.Equal(79, source.RunOnce().OrderBy(i => i).Skip(20).Take(60).Last());
            Assert.Equal(93, source.RunOnce().OrderBy(i => i).ElementAt(93));
            Assert.Equal(42, source.RunOnce().OrderBy(i => i).ElementAtOrDefault(42));
            Assert.Equal(20, source.RunOnce().OrderBy(i => i).Skip(10).Take(20).Count());
            Assert.Equal(1, source.RunOnce().OrderBy(i => i).Take(2).Skip(1).Count());
        }
    }
}
