// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Linq.Tests
{
    public class LongCountTests : EnumerableTests
    {
        [Fact]
        public void SameResultsRepeatCallsIntQuery()
        {
            var q = from x in new[] { 9999, 0, 888, -1, 66, -777, 1, 2, -12345 }
                        where x > int.MinValue
                        select x;

            Assert.Equal(q.LongCount(), q.LongCount());
        }

        [Fact]
        public void SameResultsRepeatCallsStringQuery()
        {
            var q = from x in new[] { "!@#$%^", "C", "AAA", "", "Calling Twice", "SoS", string.Empty }
                    where !string.IsNullOrEmpty(x)
                    select x;

            Assert.Equal(q.LongCount(), q.LongCount());
        }

        public static IEnumerable<object[]> LongCount_TestData()
        {
            yield return new object[] { new int[0], null, 0L };
            yield return new object[] { new int[] { 3 }, null, 1L };

            Func<int, bool> isEvenFunc = IsEven;
            yield return new object[] { new int[0], isEvenFunc, 0L };
            yield return new object[] { new int[] { 4 }, isEvenFunc, 1L };
            yield return new object[] { new int[] { 5 }, isEvenFunc, 0L };
            yield return new object[] { new int[] { 2, 5, 7, 9, 29, 10 }, isEvenFunc, 2L };
            yield return new object[] { new int[] { 2, 20, 22, 100, 50, 10 }, isEvenFunc, 6L };
        }

        [Theory]
        [MemberData(nameof(LongCount_TestData))]
        public static void LongCount(IEnumerable<int> source, Func<int, bool> predicate, long expected)
        {
            if (predicate == null)
            {
                Assert.Equal(expected, source.LongCount());
            }
            else
            {
                Assert.Equal(expected, source.LongCount(predicate));
            }
        }

        [Theory]
        [MemberData(nameof(LongCount_TestData))]
        public static void LongCountRunOnce(IEnumerable<int> source, Func<int, bool> predicate, long expected)
        {
            if (predicate == null)
            {
                Assert.Equal(expected, source.RunOnce().LongCount());
            }
            else
            {
                Assert.Equal(expected, source.RunOnce().LongCount(predicate));
            }
        }

        [Fact]
        public void NullableArray_IncludesNullValues()
        {
            int?[] data = { -10, 4, 9, null, 11 };
            Assert.Equal(5, data.LongCount());
        }

        [Fact]
        public void NullSource_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((IEnumerable<int>)null).LongCount());
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((IEnumerable<int>)null).LongCount(i => i != 0));
        }

        [Fact]
        public void NullPredicate_ThrowsArgumentNullException()
        {
            Func<int, bool> predicate = null;
            AssertExtensions.Throws<ArgumentNullException>("predicate", () => Enumerable.Range(0, 3).LongCount(predicate));
        }
    }
}
