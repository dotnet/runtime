// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.Linq.Tests
{
    public class CountTests : EnumerableTests
    {
        [Fact]
        public void SameResultsRepeatCallsIntQuery()
        {
            var q = from x in new[] { 9999, 0, 888, -1, 66, -777, 1, 2, -12345 }
                    where x > int.MinValue
                    select x;

            Assert.Equal(q.Count(), q.Count());
        }

        [Fact]
        public void SameResultsRepeatCallsStringQuery()
        {
            var q = from x in new[] { "!@#$%^", "C", "AAA", "", "Calling Twice", "SoS", string.Empty }
                    where !string.IsNullOrEmpty(x)
                    select x;

            Assert.Equal(q.Count(), q.Count());
        }

        public static IEnumerable<object[]> Int_TestData()
        {
            yield return new object[] { new int[0], null, 0 };

            Func<int, bool> isEvenFunc = IsEven;
            yield return new object[] { new int[0], isEvenFunc, 0 };
            yield return new object[] { new int[] { 4 }, isEvenFunc, 1 };
            yield return new object[] { new int[] { 5 }, isEvenFunc, 0 };
            yield return new object[] { new int[] { 2, 5, 7, 9, 29, 10 }, isEvenFunc, 2 };
            yield return new object[] { new int[] { 2, 20, 22, 100, 50, 10 }, isEvenFunc, 6 };

            yield return new object[] { RepeatedNumberGuaranteedNotCollectionType(0, 0), null, 0 };
            yield return new object[] { RepeatedNumberGuaranteedNotCollectionType(5, 1), null, 1 };
            yield return new object[] { RepeatedNumberGuaranteedNotCollectionType(5, 10), null, 10 };
        }

        [Theory]
        [MemberData(nameof(Int_TestData))]
        public void Int(IEnumerable<int> source, Func<int, bool> predicate, int expected)
        {
            if (predicate == null)
            {
                Assert.Equal(expected, source.Count());
            }
            else
            {
                Assert.Equal(expected, source.Count(predicate));
            }
        }

        [Theory, MemberData(nameof(Int_TestData))]
        public void IntRunOnce(IEnumerable<int> source, Func<int, bool> predicate, int expected)
        {
            if (predicate == null)
            {
                Assert.Equal(expected, source.RunOnce().Count());
            }
            else
            {
                Assert.Equal(expected, source.RunOnce().Count(predicate));
            }
        }

        [Fact]
        public void NullableIntArray_IncludesNullObjects()
        {
            int?[] data = { -10, 4, 9, null, 11 };
            Assert.Equal(5, data.Count());
        }

        [Theory]
        [MemberData(nameof(CountsAndTallies))]
        public void CountMatchesTally<T>(int count, IEnumerable<T> enumerable)
        {
            Assert.Equal(count, enumerable.Count());
        }

        [Theory, MemberData(nameof(CountsAndTallies))]
        public void RunOnce<T>(int count, IEnumerable<T> enumerable)
        {
            Assert.Equal(count, enumerable.RunOnce().Count());
        }

        private static IEnumerable<object[]> EnumerateCollectionTypesAndCounts<T>(int count, IEnumerable<T> enumerable)
        {
            yield return new object[] { count, enumerable };
            yield return new object[] { count, enumerable.ToArray() };
            yield return new object[] { count, enumerable.ToList() };
            yield return new object[] { count, new Stack<T>(enumerable) };
        }

        public static IEnumerable<object[]> CountsAndTallies()
        {
            int count = 5;
            var range = Enumerable.Range(1, count);
            foreach (object[] variant in EnumerateCollectionTypesAndCounts(count, range))
                yield return variant;
            foreach (object[] variant in EnumerateCollectionTypesAndCounts(count, range.Select(i => (float)i)))
                yield return variant;
            foreach (object[] variant in EnumerateCollectionTypesAndCounts(count, range.Select(i => (double)i)))
                yield return variant;
            foreach (object[] variant in EnumerateCollectionTypesAndCounts(count, range.Select(i => (decimal)i)))
                yield return variant;
        }

        [Fact]
        public void NullSource_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((IEnumerable<int>)null).Count());
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((IEnumerable<int>)null).Count(i => i != 0));
        }

        [Fact]
        public void NullPredicate_ThrowsArgumentNullException()
        {
            Func<int, bool> predicate = null;
            AssertExtensions.Throws<ArgumentNullException>("predicate", () => Enumerable.Range(0, 3).Count(predicate));
        }

        [Fact]
        public void NonEnumeratedCount_NullSource_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((IEnumerable<int>)null).TryGetNonEnumeratedCount(out _));
        }

        [Theory]
        [MemberData(nameof(NonEnumeratedCount_SupportedEnumerables))]
        public void NonEnumeratedCount_SupportedEnumerables_ShouldReturnExpectedCount<T>(int expectedCount, IEnumerable<T> source)
        {
            Assert.True(source.TryGetNonEnumeratedCount(out int actualCount));
            Assert.Equal(expectedCount, actualCount);
        }

        [Theory]
        [MemberData(nameof(NonEnumeratedCount_UnsupportedEnumerables))]
        public void NonEnumeratedCount_UnsupportedEnumerables_ShouldReturnFalse<T>(IEnumerable<T> source)
        {
            Assert.False(source.TryGetNonEnumeratedCount(out int actualCount));
            Assert.Equal(0, actualCount);
        }

        [Fact]
        public void NonEnumeratedCount_ShouldNotEnumerateSource()
        {
            bool isEnumerated = false;
            Assert.False(Source().TryGetNonEnumeratedCount(out int count));
            Assert.Equal(0, count);
            Assert.False(isEnumerated);

            IEnumerable<int> Source()
            {
                isEnumerated = true;
                yield return 42;
            }
        }

        private class MockReadOnlyCollection<T> : IReadOnlyCollection<T>
        {
            public IEnumerator<T> GetEnumerator()
            {
                throw new InvalidOperationException();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            public int Count => 1;
        }

        [Fact]
        public void NonEnumeratedCount_ShouldNotEnumerateReadOnlyCollection()
        {
            var readOnlyCollection = new MockReadOnlyCollection<int>();
            Assert.True(readOnlyCollection.TryGetNonEnumeratedCount(out var count));
            Assert.Equal(1, count);
        }

        public static IEnumerable<object[]> NonEnumeratedCount_SupportedEnumerables()
        {
            yield return WrapArgs(4, new int[]{ 1, 2, 3, 4 });
            yield return WrapArgs(4, new List<int>(new int[] { 1, 2, 3, 4 }));
            yield return WrapArgs(4, new Stack<int>(new int[] { 1, 2, 3, 4 }));

            yield return WrapArgs(0, Enumerable.Empty<string>());

            if (PlatformDetection.IsSpeedOptimized)
            {
                yield return WrapArgs(100, Enumerable.Range(1, 100));
                yield return WrapArgs(80, Enumerable.Repeat(1, 80));
                yield return WrapArgs(50, Enumerable.Range(1, 50).Select(x => x + 1));
                yield return WrapArgs(4, new int[] { 1, 2, 3, 4 }.Select(x => x + 1));
                yield return WrapArgs(50, Enumerable.Range(1, 50).Select(x => x + 1).Select(x => x - 1));
                yield return WrapArgs(20, Enumerable.Range(1, 20).Reverse());
                yield return WrapArgs(20, Enumerable.Range(1, 20).OrderBy(x => -x));
                yield return WrapArgs(20, Enumerable.Range(1, 10).Concat(Enumerable.Range(11, 10)));
            }

            static object[] WrapArgs<T>(int expectedCount, IEnumerable<T> source) => new object[] { expectedCount, source };
        }

        public static IEnumerable<object[]> NonEnumeratedCount_UnsupportedEnumerables()
        {
            yield return WrapArgs(Enumerable.Range(1, 100).Where(x => x % 2 == 0));
            yield return WrapArgs(Enumerable.Range(1, 100).GroupBy(x => x % 2 == 0));
            yield return WrapArgs(new Stack<int>(new int[] { 1, 2, 3, 4 }).Select(x => x + 1));
            yield return WrapArgs(Enumerable.Range(1, 100).Distinct());

            if (!PlatformDetection.IsSpeedOptimized)
            {
                yield return WrapArgs(Enumerable.Range(1, 100));
                yield return WrapArgs(Enumerable.Repeat(1, 80));
                yield return WrapArgs(Enumerable.Range(1, 50).Select(x => x + 1));
                yield return WrapArgs(new int[] { 1, 2, 3, 4 }.Select(x => x + 1));
                yield return WrapArgs(Enumerable.Range(1, 50).Select(x => x + 1).Select(x => x - 1));
                yield return WrapArgs(Enumerable.Range(1, 20).Reverse());
                yield return WrapArgs(Enumerable.Range(1, 20).OrderBy(x => -x));
                yield return WrapArgs(Enumerable.Range(1, 10).Concat(Enumerable.Range(11, 10)));
            }

            static object[] WrapArgs<T>(IEnumerable<T> source) => new object[] { source };
        }
    }
}
