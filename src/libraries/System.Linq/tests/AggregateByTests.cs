// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.Linq.Tests
{
    public class AggregateByTests : EnumerableTests
    {
        [Fact]
        public void AggregateBy_SourceNull_ThrowsArgumentNullException()
        {
            string[] first = null;

            AssertExtensions.Throws<ArgumentNullException>("source", () => first.AggregateBy(x => x, string.Empty, (x, y) => x + y));
            AssertExtensions.Throws<ArgumentNullException>("source", () => first.AggregateBy(x => x, string.Empty, (x, y) => x + y, new AnagramEqualityComparer()));
        }

        [Fact]
        public void AggregateBy_KeySelectorNull_ThrowsArgumentNullException()
        {
            string[] source = { };
            Func<string, string> keySelector = null;

            AssertExtensions.Throws<ArgumentNullException>("keySelector", () => source.AggregateBy(keySelector, string.Empty, (x, y) => x + y));
            AssertExtensions.Throws<ArgumentNullException>("keySelector", () => source.AggregateBy(keySelector, string.Empty, (x, y) => x + y, new AnagramEqualityComparer()));
        }

        [Fact]
        public void AggregateBy_SeedSelectorNull_ThrowsArgumentNullException()
        {
            string[] source = { };
            Func<string, string> seedSelector = null;

            AssertExtensions.Throws<ArgumentNullException>("seedSelector", () => source.AggregateBy(x => x, seedSelector, (x, y) => x + y));
            AssertExtensions.Throws<ArgumentNullException>("seedSelector", () => source.AggregateBy(x => x, seedSelector, (x, y) => x + y, new AnagramEqualityComparer()));
        }

        [Fact]
        public void AggregateBy_FuncNull_ThrowsArgumentNullException()
        {
            string[] source = { };
            Func<string, string, string> func = null;

            AssertExtensions.Throws<ArgumentNullException>("func", () => source.AggregateBy(x => x, string.Empty, func));
            AssertExtensions.Throws<ArgumentNullException>("func", () => source.AggregateBy(x => x, string.Empty, func, new AnagramEqualityComparer()));
        }

        [Fact]
        public void AggregateBy_SourceThrowsOnGetEnumerator()
        {
            IEnumerable<int> source = new ThrowsOnGetEnumerator();

            var enumerator = source.AggregateBy(x => x, 0, (x, y) => x + y).GetEnumerator();

            Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());
        }

        [Fact]
        public void AggregateBy_SourceThrowsOnMoveNext()
        {
            IEnumerable<int> source = new ThrowsOnMoveNext();

            var enumerator = source.AggregateBy(x => x, 0, (x, y) => x + y).GetEnumerator();

            Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());
        }

        [Fact]
        public void AggregateBy_SourceThrowsOnCurrent()
        {
            IEnumerable<int> source = new ThrowsOnCurrentEnumerator();

            var enumerator = source.AggregateBy(x => x, 0, (x, y) => x + y).GetEnumerator();

            Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());
        }

        [Theory]
        [MemberData(nameof(AggregateBy_TestData))]
        public static void AggregateBy_HasExpectedOutput<TSource, TKey, TAccumulate>(IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TKey, TAccumulate> seedSelector, Func<TAccumulate, TSource, TAccumulate> func, IEqualityComparer<TKey>? comparer, IEnumerable<KeyValuePair<TKey, TAccumulate>> expected)
        {
            Assert.Equal(expected, source.AggregateBy(keySelector, seedSelector, func, comparer));
        }

        [Theory]
        [MemberData(nameof(AggregateBy_TestData))]
        public static void AggregateBy_RunOnce_HasExpectedOutput<TSource, TKey, TAccumulate>(IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TKey, TAccumulate> seedSelector, Func<TAccumulate, TSource, TAccumulate> func, IEqualityComparer<TKey>? comparer, IEnumerable<KeyValuePair<TKey, TAccumulate>> expected)
        {
            Assert.Equal(expected, source.RunOnce().AggregateBy(keySelector, seedSelector, func, comparer));
        }

        public static IEnumerable<object[]> AggregateBy_TestData()
        {
            yield return WrapArgs(
                source: Enumerable.Empty<int>(),
                keySelector: x => x,
                seedSelector: x => 0,
                func: (x, y) => x + y,
                comparer: null,
                expected: Enumerable.Empty<KeyValuePair<int,int>>());

            yield return WrapArgs(
                source: Enumerable.Range(0, 10),
                keySelector: x => x,
                seedSelector: x => 0,
                func: (x, y) => x + y,
                comparer: null,
                expected: Enumerable.Range(0, 10).ToDictionary(x => x, x => x));

            yield return WrapArgs(
                source: Enumerable.Range(5, 10),
                keySelector: x => true,
                seedSelector: x => 0,
                func: (x, y) => x + y,
                comparer: null,
                expected: Enumerable.Repeat(true, 1).ToDictionary(x => x, x => 95));

            yield return WrapArgs(
                source: Enumerable.Range(0, 20),
                keySelector: x => x % 5,
                seedSelector: x => 0,
                func: (x, y) => x + y,
                comparer: null,
                expected: Enumerable.Range(0, 5).ToDictionary(x => x, x => 30 + 4 * x));

            yield return WrapArgs(
                source: Enumerable.Repeat(5, 20),
                keySelector: x => x,
                seedSelector: x => 0,
                func: (x, y) => x + y,
                comparer: null,
                expected: Enumerable.Repeat(5, 1).ToDictionary(x => x, x => 100));

            yield return WrapArgs(
                source: new string[] { "Bob", "bob", "tim", "Bob", "Tim" },
                keySelector: x => x,
                seedSelector: x => string.Empty,
                func: (x, y) => x + y,
                null,
                expected: new string[] { "Bob", "bob", "tim", "Tim" }.ToDictionary(x => x, x => x == "Bob" ? "BobBob" : x));

            yield return WrapArgs(
                source: new string[] { "Bob", "bob", "tim", "Bob", "Tim" },
                keySelector: x => x,
                seedSelector: x => string.Empty,
                func: (x, y) => x + y,
                StringComparer.OrdinalIgnoreCase,
                expected: new string[] { "Bob", "tim" }.ToDictionary(x => x, x => x == "Bob" ? "BobbobBob" : "timTim"));

            yield return WrapArgs(
                source: new (string Name, int Age)[] { ("Tom", 20), ("Dick", 30), ("Harry", 40) },
                keySelector: x => x.Age,
                seedSelector: x => "I am ",
                func: (x, y) => x + y.Name,
                comparer: null,
                expected: new int[] { 20, 30, 40 }.ToDictionary(x => x, x => x == 20 ? "I am Tom" : x == 30 ? "I am Dick" : "I am Harry"));

            yield return WrapArgs(
                source: new (string Name, int Age)[] { ("Tom", 20), ("Dick", 20), ("Harry", 40) },
                keySelector: x => x.Age,
                seedSelector: x => string.Empty,
                func: (x, y) => x + y.Name,
                comparer: null,
                expected: new int[] { 20, 40 }.ToDictionary(x => x, x => x == 20 ? "TomDick" : "Harry"));

            yield return WrapArgs(
                source: new (string Name, int Age)[] { ("Bob", 20), ("bob", 20), ("Harry", 20) },
                keySelector: x => x.Name,
                seedSelector: x => 0,
                func: (x, y) => x + y.Age,
                comparer: null,
                expected: new string[] { "Bob", "bob", "Harry" }.ToDictionary(x => x, x => 20));

            yield return WrapArgs(
                source: new (string Name, int Age)[] { ("Bob", 20), ("bob", 30), ("Harry", 40) },
                keySelector: x => x.Name,
                seedSelector: x => 0,
                func: (x, y) => x + y.Age,
                comparer: StringComparer.OrdinalIgnoreCase,
                expected: new string[] { "Bob", "Harry" }.ToDictionary(x => x, x => x == "Bob" ? 50 : 40));

            object[] WrapArgs<TSource, TKey, TAccumulate>(IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TKey, TAccumulate> seedSelector, Func<TAccumulate, TSource, TAccumulate> func, IEqualityComparer<TKey>? comparer, IEnumerable<KeyValuePair<TKey, TAccumulate>> expected)
                => new object[] { source, keySelector, seedSelector, func, comparer, expected };
        }
    }
}
