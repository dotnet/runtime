// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.Linq.Tests
{
    public class IntersectTests : EnumerableTests
    {
        [Fact]
        public void SameResultsRepeatCallsIntQuery()
        {
            var first = from x1 in new int?[] { 2, 3, null, 2, null, 4, 5 }
                        select x1;
            var second = from x2 in new int?[] { 1, 9, null, 4 }
                         select x2;

            Assert.Equal(first.Intersect(second), first.Intersect(second));
        }

        [Fact]
        public void SameResultsRepeatCallsStringQuery()
        {
            var first = from x1 in new[] { "AAA", string.Empty, "q", "C", "#", "!@#$%^", "0987654321", "Calling Twice" }
                        select x1;
            var second = from x2 in new[] { "!@#$%^", "C", "AAA", "", "Calling Twice", "SoS" }
                         select x2;

            Assert.Equal(first.Intersect(second), first.Intersect(second));
        }

        public static IEnumerable<object[]> Int_TestData()
        {
            yield return new object[] { new int[0], new int[0], new int[0] };
            yield return new object[] { new int[] { -5, 3, -2, 6, 9 }, new int[] { 0, 5, 2, 10, 20 }, new int[0] };
            yield return new object[] { new int[] { 1, 2, 2, 3, 4, 3, 5 }, new int[] { 1, 4, 4, 2, 2, 2 }, new int[] { 1, 2, 4 } };
            yield return new object[] { new int[] { 1, 1, 1, 1, 1, 1 }, new int[] { 1, 1, 1, 1, 1 }, new int[] { 1 } };
        }

        [Theory]
        [MemberData(nameof(Int_TestData))]
        public void Int(IEnumerable<int> first, IEnumerable<int> second, int[] expected)
        {
            Assert.Equal(expected, first.Intersect(second));
            Assert.Equal(expected, first.Intersect(second, null));
        }

        public static IEnumerable<object[]> String_TestData()
        {
            IEqualityComparer<string> defaultComparer = EqualityComparer<string>.Default;
            yield return new object[] { new string[1], new string[0], defaultComparer, new string[0] };
            yield return new object[] { new string[] { null, null, string.Empty }, new string[2], defaultComparer,  new string[] { null } };
            yield return new object[] { new string[2], new string[0], defaultComparer, new string[0] };

            yield return new object[] { new string[] { "Tim", "Bob", "Mike", "Robert" }, new string[] { "ekiM", "bBo" }, null, new string[0] };
            yield return new object[] { new string[] { "Tim", "Bob", "Mike", "Robert" }, new string[] { "ekiM", "bBo" }, new AnagramEqualityComparer(), new string[] { "Bob", "Mike" } };
        }

        [Theory]
        [MemberData(nameof(String_TestData))]
        public void String(IEnumerable<string> first, IEnumerable<string> second, IEqualityComparer<string> comparer, string[] expected)
        {
            if (comparer == null)
            {
                Assert.Equal(expected, first.Intersect(second));
            }
            Assert.Equal(expected, first.Intersect(second, comparer));
        }

        public static IEnumerable<object[]> NullableInt_TestData()
        {
            yield return new object[] { new int?[0], new int?[] { -5, 0, null, 1, 2, 9, 2 }, new int?[0] };
            yield return new object[] { new int?[] { -5, 0, 1, 2, null, 9, 2 }, new int?[0], new int?[0] };
            yield return new object[] { new int?[] { 1, 2, null, 3, 4, 5, 6 }, new int?[] { 6, 7, 7, 7, null, 8, 1 }, new int?[] { 1, null, 6 } };
        }

        [Theory]
        [MemberData(nameof(NullableInt_TestData))]
        public void NullableInt(IEnumerable<int?> first, IEnumerable<int?> second, int?[] expected)
        {
            Assert.Equal(expected, first.Intersect(second));
            Assert.Equal(expected, first.Intersect(second, null));
        }

        [Theory, MemberData(nameof(NullableInt_TestData))]
        public void NullableIntRunOnce(IEnumerable<int?> first, IEnumerable<int?> second, int?[] expected)
        {
            Assert.Equal(expected, first.RunOnce().Intersect(second.RunOnce()));
            Assert.Equal(expected, first.RunOnce().Intersect(second.RunOnce(), null));
        }

        [Fact]
        public void FirstNull_ThrowsArgumentNullException()
        {
            string[] first = null;
            string[] second = { "ekiM", "bBo" };

            AssertExtensions.Throws<ArgumentNullException>("first", () => first.Intersect(second));
            AssertExtensions.Throws<ArgumentNullException>("first", () => first.Intersect(second, new AnagramEqualityComparer()));
        }

        [Fact]
        public void SecondNull_ThrowsArgumentNullException()
        {
            string[] first = { "Tim", "Bob", "Mike", "Robert" };
            string[] second = null;

            AssertExtensions.Throws<ArgumentNullException>("second", () => first.Intersect(second));
            AssertExtensions.Throws<ArgumentNullException>("second", () => first.Intersect(second, new AnagramEqualityComparer()));
        }

        [Fact]
        public void ForcedToEnumeratorDoesntEnumerate()
        {
            var iterator = NumberRangeGuaranteedNotCollectionType(0, 3).Intersect(Enumerable.Range(0, 3));
            // Don't insist on this behaviour, but check it's correct if it happens
            var en = iterator as IEnumerator<int>;
            Assert.False(en != null && en.MoveNext());
        }

        [Fact]
        public void HashSetWithBuiltInComparer_HashSetContainsNotUsed()
        {
            IEnumerable<string> input1 = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "a" };
            IEnumerable<string> input2 = new[] { "A" };

            Assert.Equal(Enumerable.Empty<string>(), input1.Intersect(input2));
            Assert.Equal(Enumerable.Empty<string>(), input1.Intersect(input2, null));
            Assert.Equal(Enumerable.Empty<string>(), input1.Intersect(input2, EqualityComparer<string>.Default));
            Assert.Equal(new[] { "a" }, input1.Intersect(input2, StringComparer.OrdinalIgnoreCase));

            Assert.Equal(Enumerable.Empty<string>(), input2.Intersect(input1));
            Assert.Equal(Enumerable.Empty<string>(), input2.Intersect(input1, null));
            Assert.Equal(Enumerable.Empty<string>(), input2.Intersect(input1, EqualityComparer<string>.Default));
            Assert.Equal(new[] { "A" }, input2.Intersect(input1, StringComparer.OrdinalIgnoreCase));
        }

        [Fact]
        public void IntersectBy_FirstNull_ThrowsArgumentNullException()
        {
            string[] first = null;
            string[] second = { "bBo", "shriC" };

            AssertExtensions.Throws<ArgumentNullException>("first", () => first.IntersectBy(second, x => x));
            AssertExtensions.Throws<ArgumentNullException>("first", () => first.IntersectBy(second, x => x, new AnagramEqualityComparer()));
        }

        [Fact]
        public void IntersectBy_SecondNull_ThrowsArgumentNullException()
        {
            string[] first = { "Bob", "Tim", "Robert", "Chris" };
            string[] second = null;

            AssertExtensions.Throws<ArgumentNullException>("second", () => first.IntersectBy(second, x => x));
            AssertExtensions.Throws<ArgumentNullException>("second", () => first.IntersectBy(second, x => x, new AnagramEqualityComparer()));
        }

        [Fact]
        public void IntersectBy_KeySelectorNull_ThrowsArgumentNullException()
        {
            string[] first = { "Bob", "Tim", "Robert", "Chris" };
            string[] second = { "bBo", "shriC" };
            Func<string, string> keySelector = null;

            AssertExtensions.Throws<ArgumentNullException>("keySelector", () => first.IntersectBy(second, keySelector));
            AssertExtensions.Throws<ArgumentNullException>("keySelector", () => first.IntersectBy(second, keySelector, new AnagramEqualityComparer()));
        }

        [Theory]
        [MemberData(nameof(IntersectBy_TestData))]
        public static void IntersectBy_HasExpectedOutput<TSource, TKey>(IEnumerable<TSource> first, IEnumerable<TKey> second, Func<TSource, TKey> keySelector, IEqualityComparer<TKey>? comparer, IEnumerable<TSource> expected)
        {
            Assert.Equal(expected, first.IntersectBy(second, keySelector, comparer));
        }

        [Theory]
        [MemberData(nameof(IntersectBy_TestData))]
        public static void IntersectBy_RunOnce_HasExpectedOutput<TSource, TKey>(IEnumerable<TSource> first, IEnumerable<TKey> second, Func<TSource, TKey> keySelector, IEqualityComparer<TKey>? comparer, IEnumerable<TSource> expected)
        {
            Assert.Equal(expected, first.RunOnce().IntersectBy(second.RunOnce(), keySelector, comparer));
        }

        public static IEnumerable<object[]> IntersectBy_TestData()
        {
            yield return WrapArgs(
                first: Enumerable.Range(0, 10),
                second: Enumerable.Range(0, 5),
                keySelector: x => x,
                comparer: null,
                expected: Enumerable.Range(0, 5));

            yield return WrapArgs(
                first: Enumerable.Range(0, 10),
                second: Enumerable.Range(10, 10),
                keySelector: x => x,
                comparer: null,
                expected: Enumerable.Empty<int>());

            yield return WrapArgs(
                first: Enumerable.Repeat(5, 20),
                second: Enumerable.Empty<int>(),
                keySelector: x => x,
                comparer: null,
                expected: Enumerable.Empty<int>());

            yield return WrapArgs(
                first: Enumerable.Repeat(5, 20),
                second: Enumerable.Repeat(5, 3),
                keySelector: x => x,
                comparer: null,
                expected: Enumerable.Repeat(5, 1));

            yield return WrapArgs(
                first: new string[] { "Bob", "Tim", "Robert", "Chris" },
                second: new string[] { "bBo", "shriC" },
                keySelector: x => x,
                null,
                expected: Array.Empty<string>());

            yield return WrapArgs(
                first: new string[] { "Bob", "Tim", "Robert", "Chris" },
                second: new string[] { "bBo", "shriC" },
                keySelector: x => x,
                new AnagramEqualityComparer(),
                expected: new string[] { "Bob", "Chris" });

            yield return WrapArgs(
                first: new (string Name, int Age)[] { ("Tom", 20), ("Dick", 30), ("Harry", 40) },
                second: new int[] { 15, 20, 40 },
                keySelector: x => x.Age,
                comparer: null,
                expected: new (string Name, int Age)[] { ("Tom", 20), ("Harry", 40) });

            yield return WrapArgs(
                first: new (string Name, int Age)[] { ("Tom", 20), ("Dick", 30), ("Harry", 40) },
                second: new string[] { "moT" },
                keySelector: x => x.Name,
                comparer: null,
                expected: Array.Empty<(string Name, int Age)>());

            yield return WrapArgs(
                first: new (string Name, int Age)[] { ("Tom", 20), ("Dick", 30), ("Harry", 40) },
                second: new string[] { "moT" },
                keySelector: x => x.Name,
                comparer: new AnagramEqualityComparer(),
                expected: new (string Name, int Age)[] { ("Tom", 20) });

            object[] WrapArgs<TSource, TKey>(IEnumerable<TSource> first, IEnumerable<TKey> second, Func<TSource, TKey> keySelector, IEqualityComparer<TKey>? comparer, IEnumerable<TSource> expected)
                => new object[] { first, second, keySelector, comparer, expected };
        }
    }
}
