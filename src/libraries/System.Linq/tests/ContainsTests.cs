// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using Xunit;

namespace System.Linq.Tests
{
    public class ContainsTests : EnumerableTests
    {
        [Fact]
        public void SameResultsRepeatCallsIntQuery()
        {
            var q = from x in new[] { 9999, 0, 888, -1, 66, -777, 1, 2, -12345 }
                    where x > int.MinValue
                    select x;

            Assert.Equal(q.Contains(-1), q.Contains(-1));
        }

        [Fact]
        public void SameResultsRepeatCallsStringQuery()
        {
            var q = from x in new[] { "!@#$%^", "C", "AAA", "", "Calling Twice", "SoS", string.Empty }
                    where !string.IsNullOrEmpty(x)
                    select x;

            Assert.Equal(q.Contains("X"), q.Contains("X"));
        }

        public static IEnumerable<object[]> Int_TestData()
        {
            foreach (Func<IEnumerable<int>, IEnumerable<int>> transform in IdentityTransforms<int>())
            {
                yield return [transform(new int[0]), 6, false];
                yield return [transform([8, 10, 3, 0, -8]), 6, false];
                yield return [transform([8, 10, 3, 0, -8]), 8, true];
                yield return [transform([8, 10, 3, 0, -8]), -8, true];
                yield return [transform([8, 0, 10, 3, 0, -8, 0]), 0, true];

                yield return [transform(Enumerable.Range(0, 0)), 0, false];
                yield return [transform(Enumerable.Range(4, 5)), 3, false];
                yield return [transform(Enumerable.Range(3, 5)), 3, true];
                yield return [transform(Enumerable.Range(3, 5)), 7, true];
                yield return [transform(Enumerable.Range(10, 3)), 10, true];
            }
        }

        [Theory]
        [MemberData(nameof(Int_TestData))]
        public void Int(IEnumerable<int> source, int value, bool expected)
        {
            Assert.Equal(expected, source.Contains(value));
            Assert.Equal(expected, source.Contains(value, null));
        }

        [Theory, MemberData(nameof(Int_TestData))]
        public void IntRunOnce(IEnumerable<int> source, int value, bool expected)
        {
            Assert.Equal(expected, source.RunOnce().Contains(value));
            Assert.Equal(expected, source.RunOnce().Contains(value, null));
        }

        public static IEnumerable<object[]> String_TestData()
        {
            yield return [new string[] { null }, StringComparer.Ordinal, null, true];
            yield return [new string[] { "Bob", "Robert", "Tim" }, null, "trboeR", false];
            yield return [new string[] { "Bob", "Robert", "Tim" }, null, "Tim", true];
            yield return [new string[] { "Bob", "Robert", "Tim" }, new AnagramEqualityComparer(), "trboeR", true];
            yield return [new string[] { "Bob", "Robert", "Tim" }, new AnagramEqualityComparer(), "nevar", false];
        }

        [Theory]
        [MemberData(nameof(String_TestData))]
        public void String(IEnumerable<string> source, IEqualityComparer<string> comparer, string value, bool expected)
        {
            if (comparer is null)
            {
                Assert.Equal(expected, source.Contains(value));
            }
            Assert.Equal(expected, source.Contains(value, comparer));
        }

        [Theory, MemberData(nameof(String_TestData))]
        public void StringRunOnce(IEnumerable<string> source, IEqualityComparer<string> comparer, string value, bool expected)
        {
            if (comparer is null)
            {
                Assert.Equal(expected, source.RunOnce().Contains(value));
            }
            Assert.Equal(expected, source.RunOnce().Contains(value, comparer));
        }

        public static IEnumerable<object[]> NullableInt_TestData()
        {
            yield return [new int?[] { 8, 0, 10, 3, 0, -8, 0 }, null, false];
            yield return [new int?[] { 8, 0, 10, null, 3, 0, -8, 0 }, null, true];

            yield return [NullableNumberRangeGuaranteedNotCollectionType(3, 4), null, false];
            yield return [RepeatedNullableNumberGuaranteedNotCollectionType(null, 5), null, true];
        }

        [Theory]
        [MemberData(nameof(NullableInt_TestData))]
        public void NullableInt(IEnumerable<int?> source, int? value, bool expected)
        {
            Assert.Equal(expected, source.Contains(value));
            Assert.Equal(expected, source.Contains(value, null));
        }

        [Fact]
        public void NullSource_ThrowsArgumentNullException()
        {
            IEnumerable<int> source = null;

            AssertExtensions.Throws<ArgumentNullException>("source", () => source.Contains(42));
            AssertExtensions.Throws<ArgumentNullException>("source", () => source.Contains(42, EqualityComparer<int>.Default));
        }

        [Fact]
        public void ExplicitNullComparerDoesNotDeferToCollection()
        {
            IEnumerable<string> source = new HashSet<string>(new AnagramEqualityComparer()) {"ABC"};
            Assert.False(source.Contains("BAC", null));
        }

        [Fact]
        public void ExplicitComparerDoesNotDeferToCollection()
        {
            IEnumerable<string> source = new HashSet<string> {"ABC"};
            Assert.True(source.Contains("abc", StringComparer.OrdinalIgnoreCase));
        }

        [Fact]
        public void ExplicitComparerDoestNotDeferToCollectionWithComparer()
        {
            IEnumerable<string> source = new HashSet<string>(StringComparer.OrdinalIgnoreCase) {"ABC"};
            Assert.True(source.Contains("BAC", new AnagramEqualityComparer()));
        }

        [Fact]
        public void NoComparerDoesDeferToCollection()
        {
            IEnumerable<string> source = new HashSet<string>(StringComparer.OrdinalIgnoreCase) {"ABC"};
            Assert.True(source.Contains("abc"));
        }

        [Fact]
        public void FollowingVariousOperators()
        {
            IEnumerable<int> source = Enumerable.Range(1, 3);
            foreach (var transform in IdentityTransforms<int>())
            {
                IEnumerable<int> transformedSource = transform(source);
                IEnumerable<int> transformedEmpty = transform([]);

                Assert.True(transformedSource.Contains(1));
                Assert.True(transformedSource.Contains(2));
                Assert.True(transformedSource.Contains(3));
                Assert.False(transformedSource.Contains(0));
                Assert.False(transformedSource.Contains(4));

                // Append/Prepend
                var ap = transformedSource.Append(4).Prepend(5).Append(6).Prepend(7);
                Assert.True(ap.Contains(3));
                Assert.True(ap.Contains(4));
                Assert.True(ap.Contains(5));
                Assert.True(ap.Contains(6));
                Assert.True(ap.Contains(7));
                Assert.False(ap.Contains(8));

                // Concat
                Assert.True(transform([4, 5, 6]).Concat(transformedSource).Contains(2));
                Assert.False(transform([4, 5, 6]).Concat(transformedSource).Contains(7));
                Assert.True(transform([4, 5, 6]).Concat(transform([7, 8, 9])).Concat(transformedSource).Contains(2));
                Assert.False(transform([4, 5, 6]).Concat(transform([7, 8, 9])).Concat(transformedSource).Contains(10));

                // DefaultIfEmpty
                Assert.True(transformedSource.DefaultIfEmpty(4).Contains(1));
                Assert.False(transformedEmpty.DefaultIfEmpty(4).Contains(0));
                Assert.True(transformedEmpty.DefaultIfEmpty(4).Contains(4));
                Assert.False(transformedSource.DefaultIfEmpty(4).Contains(4));
                Assert.False(transformedSource.DefaultIfEmpty(0).Contains(4));
                Assert.False(transformedSource.DefaultIfEmpty().Contains(0));
                Assert.True(transformedEmpty.DefaultIfEmpty().Contains(0));
                Assert.False(transformedSource.DefaultIfEmpty().Contains(4));

                // Distinct
                Assert.True(transform(source.Concat(source)).Distinct().Contains(2));
                Assert.False(transform(source.Concat(source)).Distinct().Contains(4));
                Assert.True(transform(source.Concat(source)).Distinct().Contains(1));
                Assert.True(transform(source.Concat(source)).Distinct(EqualityComparer<int>.Create((x, y) => true, x => 0)).Contains(1));
                Assert.False(transform(source.Concat(source)).Distinct(EqualityComparer<int>.Create((x, y) => true, x => 0)).Contains(2));
                Assert.False(transform(source.Concat(source)).Distinct(EqualityComparer<int>.Create((x, y) => true, x => 0)).Contains(0));

                // OrderBy
                Assert.True(transformedSource.OrderBy(x => x).Contains(2));
                Assert.True(transformedSource.OrderBy(x => x).ThenBy(x => x).Contains(2));
                Assert.False(transformedSource.OrderBy(x => x).Contains(4));
                Assert.False(transformedSource.OrderBy(x => x).ThenBy(x => x).Contains(4));

                // OrderByDescending
                Assert.True(transformedSource.OrderByDescending(x => x).Contains(2));
                Assert.True(transformedSource.OrderByDescending(x => x).ThenByDescending(x => x).Contains(2));
                Assert.False(transformedSource.OrderByDescending(x => x).Contains(4));
                Assert.False(transformedSource.OrderByDescending(x => x).ThenByDescending(x => x).Contains(4));

                // Where/Select
                Assert.True(transformedSource.Where(x => x > 1).Contains(2));
                Assert.False(transformedSource.Where(x => x > 3).Contains(2));
                Assert.True(transformedSource.Select(x => x * 2).Contains(6));
                Assert.False(transformedSource.Select(x => x * 2).Contains(3));
                Assert.True(transformedSource.Where(x => x % 2 == 0).Select(x => x * 2).Contains(4));
                Assert.False(transformedSource.Where(x => x % 2 == 0).Select(x => x * 2).Contains(6));

                // SelectMany
                Assert.True(transformedSource.SelectMany(x => new[] { x }).Contains(2));
                Assert.True(transformedSource.SelectMany(x => new List<int> { x, x * 2 }).Contains(2));
                Assert.False(transformedSource.SelectMany(x => new[] { x }).Contains(4));
                Assert.True(transformedSource.SelectMany(x => new List<int> { x, x * 2 }).Contains(4));
                Assert.False(transformedSource.SelectMany(x => new List<int> { x, x * 2 }).Contains(5));

                // Shuffle
                Assert.True(transformedSource.Shuffle().Contains(2));
                Assert.False(transformedSource.Shuffle().Contains(4));
                Assert.False(transformedSource.Shuffle().Take(1).Contains(4));
                Assert.True(transformedSource.Shuffle().Take(3).Contains(2));
                Assert.False(transformedSource.Shuffle().Take(1).Contains(4));
                for (int trial = 0; trial < 100 && !transformedSource.Shuffle().Take(1).Contains(3); trial++)
                {
                    if (trial == 99)
                    {
                        Assert.Fail("Shuffle().Take() didn't contain value after 100 tries. The chances of that are infinitesimal with a correct implementation.");
                    }
                }

                // Skip/Take
                Assert.True(transformedSource.Skip(2).Contains(3));
                Assert.True(transformedSource.Skip(2).Take(1).Contains(3));
                Assert.True(transformedSource.Take(1).Contains(1));
                Assert.False(transformedSource.Take(1).Contains(2));
                Assert.False(transformedSource.Take(1).Contains(2));
                Assert.False(transformedSource.Take(2).Contains(3));
                Assert.False(transformedSource.Skip(1).Take(1).Contains(1));
                Assert.True(transformedSource.Skip(1).Take(1).Contains(2));
                Assert.False(transformedSource.Skip(1).Take(1).Contains(3));

                // Union
                Assert.True(transformedSource.Union(transform([4])).Contains(4));
                Assert.True(transformedSource.Union(transform([4]), EqualityComparer<int>.Create((x, y) => true, x => 0)).Contains(1));
                Assert.False(transformedSource.Union(transform([4]), EqualityComparer<int>.Create((x, y) => true, x => 0)).Contains(4));
                Assert.False(transformedSource.Union(transform([3])).Contains(4));
            }

            // DefaultIfEmpty
            Assert.True(Enumerable.Empty<int>().DefaultIfEmpty(1).Contains(1));
            Assert.False(Enumerable.Empty<int>().DefaultIfEmpty(1).Contains(0));

            // Distinct
            Assert.True(new string[] { "a", "A" }.Distinct().Contains("a"));
            Assert.True(new string[] { "a", "A" }.Distinct().Contains("A"));
            Assert.True(new string[] { "a", "A" }.Distinct(StringComparer.OrdinalIgnoreCase).Contains("a"));
            Assert.False(new string[] { "a", "A" }.Distinct(StringComparer.OrdinalIgnoreCase).Contains("A"));

            // Repeat
            Assert.True(Enumerable.Repeat(1, 5).Contains(1));
            Assert.False(Enumerable.Repeat(1, 5).Contains(2));

            // Cast
            Assert.True(new int[] { 1, 2, 3 }.Cast<object>().Contains(2));
            Assert.True(new object[] { 1, 2, 3 }.Cast<int>().Contains(2));
            Assert.False(new object[] { 1, 2, 3 }.Cast<int>().Contains(4));

            // OfType
            Assert.True(new object[] { 1, "2", 3 }.OfType<int>().Contains(3));
            Assert.False(new object[] { 1, "2", 3 }.OfType<int>().Contains(4));
            Assert.False(new object[] { 1, "2", 3 }.OfType<int>().Contains(2));
            Assert.True(new object[] { 1, "2", 3 }.OfType<string>().Contains("2"));
            Assert.False(new object[] { 1, "2", 3 }.OfType<string>().Contains("4"));

            // Union
            Assert.True(new string[] { "a" }.Union(new string[] { "A" }).Contains("a"));
            Assert.True(new string[] { "a" }.Union(new string[] { "A" }).Contains("A"));
            Assert.True(new string[] { "a" }.Union(new string[] { "A" }, StringComparer.OrdinalIgnoreCase).Contains("a"));
            Assert.False(new string[] { "a" }.Union(new string[] { "A" }, StringComparer.OrdinalIgnoreCase).Contains("A"));
        }
    }
}
