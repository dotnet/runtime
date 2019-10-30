// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Linq.Tests
{
    public class DefaultIfEmptyTests : EnumerableTests
    {
        [Fact]
        public void SameResultsRepeatCallsNonEmptyQuery()
        {
            var q = from x in new[] { 9999, 0, 888, -1, 66, -777, 1, 2, -12345 }
                    where x > int.MinValue
                    select x;

            Assert.Equal(q.DefaultIfEmpty(5), q.DefaultIfEmpty(5));
        }

        [Fact]
        public void SameResultsRepeatCallsEmptyQuery()
        {
            var q = from x in NumberRangeGuaranteedNotCollectionType(0, 0)
                    select x;

            Assert.Equal(q.DefaultIfEmpty(88), q.DefaultIfEmpty(88));

        }

        public static IEnumerable<object[]> TestData()
        {
            yield return new object[] { new int[0], 0, new int[] { 0 } };
            yield return new object[] { new int[] { 3 }, 0, new int[] { 3 } };
            yield return new object[] { new int[] { 3, -1, 0, 10, 15 }, 0, new int[] { 3, -1, 0, 10, 15 } };

            yield return new object[] { new int[0], -10, new int[] { -10 } };
            yield return new object[] { new int[] { 3 }, 9, new int[] { 3 } };
            yield return new object[] { new int[] { 3, -1, 0, 10, 15 }, 9, new int[] { 3, -1, 0, 10, 15 } };
            yield return new object[] { Enumerable.Empty<int>(), 0, new int[] { 0 } };
        }

        [Theory]
        [MemberData(nameof(TestData))]
        public static void DefaultIfEmpty(IEnumerable<int> source, int defaultValue, int[] expected)
        {
            IEnumerable<int> result;
            if (defaultValue == 0)
            {
                result = source.DefaultIfEmpty();
                Assert.Equal(result, result);
                Assert.Equal(expected, result);
                Assert.Equal(expected.Length, result.Count());
                Assert.Equal(expected, result.ToList());
                Assert.Equal(expected, result.ToArray());
            }
            result = source.DefaultIfEmpty(defaultValue);
            Assert.Equal(result, result);
            Assert.Equal(expected, result);
            Assert.Equal(expected.Length, result.Count());
            Assert.Equal(expected, result.ToList());
            Assert.Equal(expected, result.ToArray());
        }

        [Theory, MemberData(nameof(TestData))]
        public static void DefaultIfEmptyRunOnce(IEnumerable<int> source, int defaultValue, int[] expected)
        {
            if (defaultValue == 0)
            {
                Assert.Equal(expected, source.RunOnce().DefaultIfEmpty());
            }

            Assert.Equal(expected, source.RunOnce().DefaultIfEmpty(defaultValue));
        }

        [Fact]
        public void NullableArray_Empty_WithoutDefaultValue()
        {
            int?[] source = new int?[0];
            Assert.Equal(new int?[] { null }, source.DefaultIfEmpty());
        }

        [Fact]
        public void NullableArray_Empty_WithDefaultValue()
        {
            int?[] source = new int?[0];
            int? defaultValue = 9;
            Assert.Equal(new int?[] { defaultValue }, source.DefaultIfEmpty(defaultValue));
        }

        [Fact]
        public void NullSource_ThrowsArgumentNullException()
        {
            IEnumerable<int> source = null;

            AssertExtensions.Throws<ArgumentNullException>("source", () => source.DefaultIfEmpty());
            AssertExtensions.Throws<ArgumentNullException>("source", () => source.DefaultIfEmpty(42));
        }

        [Fact]
        public void ForcedToEnumeratorDoesntEnumerate()
        {
            var iterator = NumberRangeGuaranteedNotCollectionType(0, 3).DefaultIfEmpty();
            // Don't insist on this behaviour, but check it's correct if it happens
            var en = iterator as IEnumerator<int>;
            Assert.False(en != null && en.MoveNext());
        }
    }
}
