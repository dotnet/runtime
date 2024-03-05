// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.Linq.Tests
{
    public class CountByTests : EnumerableTests
    {
        [Fact]
        public void CountBy_SourceNull_ThrowsArgumentNullException()
        {
            string[] first = null;

            AssertExtensions.Throws<ArgumentNullException>("source", () => first.CountBy(x => x));
            AssertExtensions.Throws<ArgumentNullException>("source", () => first.CountBy(x => x, new AnagramEqualityComparer()));
        }

        [Fact]
        public void CountBy_KeySelectorNull_ThrowsArgumentNullException()
        {
            string[] source = { "Bob", "Tim", "Robert", "Chris" };
            Func<string, string> keySelector = null;

            AssertExtensions.Throws<ArgumentNullException>("keySelector", () => source.CountBy(keySelector));
            AssertExtensions.Throws<ArgumentNullException>("keySelector", () => source.CountBy(keySelector, new AnagramEqualityComparer()));
        }

        [Fact]
        public void CountBy_SourceThrowsOnGetEnumerator()
        {
            IEnumerable<int> source = new ThrowsOnGetEnumerator();

            var enumerator = source.CountBy(x => x).GetEnumerator();

            Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());
        }

        [Fact]
        public void CountBy_SourceThrowsOnMoveNext()
        {
            IEnumerable<int> source = new ThrowsOnMoveNext();

            var enumerator = source.CountBy(x => x).GetEnumerator();

            Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());
        }

        [Fact]
        public void CountBy_SourceThrowsOnCurrent()
        {
            IEnumerable<int> source = new ThrowsOnCurrentEnumerator();

            var enumerator = source.CountBy(x => x).GetEnumerator();

            Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());
        }

        [Theory]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/92387", typeof(PlatformDetection), nameof(PlatformDetection.IsNativeAot))]
        [MemberData(nameof(CountBy_TestData))]
        public static void CountBy_HasExpectedOutput<TSource, TKey>(IEnumerable<TSource> source, Func<TSource, TKey> keySelector, IEqualityComparer<TKey>? comparer, IEnumerable<KeyValuePair<TKey, int>> expected)
        {
            Assert.Equal(expected, source.CountBy(keySelector, comparer));
        }

        [Theory]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/92387", typeof(PlatformDetection), nameof(PlatformDetection.IsNativeAot))]
        [MemberData(nameof(CountBy_TestData))]
        public static void CountBy_RunOnce_HasExpectedOutput<TSource, TKey>(IEnumerable<TSource> source, Func<TSource, TKey> keySelector, IEqualityComparer<TKey>? comparer, IEnumerable<KeyValuePair<TKey, int>> expected)
        {
            Assert.Equal(expected, source.RunOnce().CountBy(keySelector, comparer));
        }

        public static IEnumerable<object[]> CountBy_TestData()
        {
            yield return WrapArgs(
                source: Enumerable.Empty<int>(),
                keySelector: x => x,
                comparer: null,
                expected: Enumerable.Empty<KeyValuePair<int,int>>());

            yield return WrapArgs(
                source: Enumerable.Range(0, 10),
                keySelector: x => x,
                comparer: null,
                expected: Enumerable.Range(0, 10).Select(x => new KeyValuePair<int, int>(x, 1)));

            yield return WrapArgs(
                source: Enumerable.Range(5, 10),
                keySelector: x => true,
                comparer: null,
                expected: Enumerable.Repeat(true, 1).Select(x => new KeyValuePair<bool, int>(x, 10)));

            yield return WrapArgs(
                source: Enumerable.Range(0, 20),
                keySelector: x => x % 5,
                comparer: null,
                expected: Enumerable.Range(0, 5).Select(x => new KeyValuePair<int, int>(x, 4)));

            yield return WrapArgs(
                source: Enumerable.Repeat(5, 20),
                keySelector: x => x,
                comparer: null,
                expected: Enumerable.Repeat(5, 1).Select(x => new KeyValuePair<int, int>(x, 20)));

            yield return WrapArgs(
                source: new string[] { "Bob", "bob", "tim", "Bob", "Tim" },
                keySelector: x => x,
                null,
                expected:
                [
                    new("Bob", 2),
                    new("bob", 1),
                    new("tim", 1),
                    new("Tim", 1)
                ]);

            yield return WrapArgs(
                source: new string[] { "Bob", "bob", "tim", "Bob", "Tim" },
                keySelector: x => x,
                StringComparer.OrdinalIgnoreCase,
                expected:
                [
                    new("Bob", 3),
                    new("tim", 2)
                ]);

            yield return WrapArgs(
                source: new (string Name, int Age)[] { ("Tom", 20), ("Dick", 30), ("Harry", 40) },
                keySelector: x => x.Age,
                comparer: null,
                expected: new int[] { 20, 30, 40 }.Select(x => new KeyValuePair<int, int>(x, 1)));

            yield return WrapArgs(
                source: new (string Name, int Age)[] { ("Tom", 20), ("Dick", 20), ("Harry", 40) },
                keySelector: x => x.Age,
                comparer: null,
                expected:
                [
                    new(20, 2),
                    new(40, 1)
                ]);

            yield return WrapArgs(
                source: new (string Name, int Age)[] { ("Bob", 20), ("bob", 30), ("Harry", 40) },
                keySelector: x => x.Name,
                comparer: null,
                expected: new string[] { "Bob", "bob", "Harry" }.Select(x => new KeyValuePair<string, int>(x, 1)));

            yield return WrapArgs(
                source: new (string Name, int Age)[] { ("Bob", 20), ("bob", 30), ("Harry", 40) },
                keySelector: x => x.Name,
                comparer: StringComparer.OrdinalIgnoreCase,
                expected:
                [
                    new("Bob", 2),
                    new("Harry", 1)
                ]);

            object[] WrapArgs<TSource, TKey>(IEnumerable<TSource> source, Func<TSource, TKey> keySelector, IEqualityComparer<TKey>? comparer, IEnumerable<KeyValuePair<TKey, int>> expected)
                => new object[] { source, keySelector, comparer, expected };
        }
    }
}
