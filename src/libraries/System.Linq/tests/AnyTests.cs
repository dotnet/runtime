// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.Linq.Tests
{
    public class AnyTests : EnumerableTests
    {
        [Fact]
        public void SameResultsRepeatCallsIntQuery()
        {
            var q = from x in new[] { 9999, 0, 888, -1, 66, -777, 1, 2, -12345 }
                    where x > int.MinValue
                    select x;

            Func<int, bool> predicate = IsEven;
            Assert.Equal(q.Any(predicate), q.Any(predicate));
        }

        [Fact]
        public void SameResultsRepeatCallsStringQuery()
        {
            var q = from x in new[] { "!@#$%^", "C", "AAA", "", "Calling Twice", "SoS", string.Empty }
                    select x;

            Func<string, bool> predicate = string.IsNullOrEmpty;
            Assert.Equal(q.Any(predicate), q.Any(predicate));
        }

        public static IEnumerable<object[]> TestData()
        {
            foreach (int count in new[] { 0, 1, 2 })
            {
                bool expected = count > 0;

                var arr = new int[count];
                var collectionTypes = new IEnumerable<int>[]
                {
                    arr,
                    new List<int>(arr),
                    new TestCollection<int>(arr),
                    new TestNonGenericCollection<int>(arr),
                    NumberRangeGuaranteedNotCollectionType(0, count),
                };

                foreach (IEnumerable<int> source in collectionTypes)
                {
                    yield return new object[] { source, expected };
                    yield return new object[] { source.Select(i => i), expected };
                    yield return new object[] { source.Where(i => true), expected };

                    yield return new object[] { source.Where(i => false), false };
                }
            }
        }

        [Theory]
        [MemberData(nameof(TestData))]
        public void Any(IEnumerable<int> source, bool expected)
        {
            Assert.Equal(expected, source.Any());
        }

        public static IEnumerable<object[]> TestDataForGroupBy()
        {
            yield return new object[] { Array.Empty<int>().GroupBy(num => num), false };
            yield return new object[] { new int[2] { 1, 2 }.GroupBy(num => num), true };
            yield return new object[] { new int[5] { 1, 2, 1, 3, 2 }.GroupBy(n => n, (k, v) => v), true };
        }

        [Theory, MemberData(nameof(TestDataForGroupBy))]
        public void Any_GroupBy(IEnumerable<object> values, bool expectedValue)
        {
            // Provide test coverage for `Any` calls over nonempty `IListSource` implementations.
            Assert.Equal(expectedValue, values.Any());
        }

        public static IEnumerable<object[]> TestDataWithPredicate()
        {
            yield return new object[] { new int[0], null, false };
            yield return new object[] { new int[] { 3 }, null, true };

            Func<int, bool> isEvenFunc = IsEven;
            yield return new object[] { new int[0], isEvenFunc, false };
            yield return new object[] { new int[] { 4 }, isEvenFunc, true };
            yield return new object[] { new int[] { 5 }, isEvenFunc, false };
            yield return new object[] { new int[] { 5, 9, 3, 7, 4 }, isEvenFunc, true };
            yield return new object[] { new int[] { 5, 8, 9, 3, 7, 11 }, isEvenFunc, true };

            int[] range = Enumerable.Range(1, 10).ToArray();
            yield return new object[] { range, (Func<int, bool>)(i => i > 10), false };
            for (int j = 0; j <= 9; j++)
            {
                int k = j; // Local copy for iterator
                yield return new object[] { range, (Func<int, bool>)(i => i > k), true };
            }
        }

        [Theory]
        [MemberData(nameof(TestDataWithPredicate))]
        public void Any_Predicate(IEnumerable<int> source, Func<int, bool> predicate, bool expected)
        {
            if (predicate == null)
            {
                Assert.Equal(expected, source.Any());
            }
            else
            {
                Assert.Equal(expected, source.Any(predicate));
            }
        }

        [Theory, MemberData(nameof(TestDataWithPredicate))]
        public void AnyRunOnce(IEnumerable<int> source, Func<int, bool> predicate, bool expected)
        {
            if (predicate == null)
            {
                Assert.Equal(expected, source.RunOnce().Any());
            }
            else
            {
                Assert.Equal(expected, source.RunOnce().Any(predicate));
            }
        }

        [Fact]
        public void NullObjectsInArray_Included()
        {
            int?[] source = { null, null, null, null };
            Assert.True(source.Any());
        }

        [Fact]
        public void NullSource_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((IEnumerable<int>)null).Any());
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((IEnumerable<int>)null).Any(i => i != 0));
        }

        [Fact]
        public void NullPredicate_ThrowsArgumentNullException()
        {
            Func<int, bool> predicate = null;
            AssertExtensions.Throws<ArgumentNullException>("predicate", () => Enumerable.Range(0, 3).Any(predicate));
        }
    }
}
