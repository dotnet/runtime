// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
                    where x > Int32.MinValue
                    select x;

            Assert.Equal(q.Count(), q.Count());
        }

        [Fact]
        public void SameResultsRepeatCallsStringQuery()
        {
            var q = from x in new[] { "!@#$%^", "C", "AAA", "", "Calling Twice", "SoS", String.Empty }
                    where !String.IsNullOrEmpty(x)
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
        public void CountMatchesTally<T, TEn>(T unusedArgumentToForceTypeInference, int count, TEn enumerable)
            where TEn : IEnumerable<T>
        {
            Assert.Equal(count, enumerable.Count());
        }

        [Theory, MemberData(nameof(CountsAndTallies))]
        public void RunOnce<T, TEn>(T unusedArgumentToForceTypeInference, int count, TEn enumerable)
            where TEn : IEnumerable<T>
        {
            Assert.Equal(count, enumerable.RunOnce().Count());
        }

        private static IEnumerable<object[]> EnumerateCollectionTypesAndCounts<T>(int count, IEnumerable<T> enumerable)
        {
            yield return new object[] { default(T), count, enumerable };
            yield return new object[] { default(T), count, enumerable.ToArray() };
            yield return new object[] { default(T), count, enumerable.ToList() };
            yield return new object[] { default(T), count, new Stack<T>(enumerable) };
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
    }
}
