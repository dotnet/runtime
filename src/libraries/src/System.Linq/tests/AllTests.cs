// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Linq.Tests
{
    public class AllTests : EnumerableTests
    {
        [Fact]
        public void SameResultsRepeatCallsIntQuery()
        {
            var q = from x in new[] { 9999, 0, 888, -1, 66, -777, 1, 2, -12345 }
                    where x > int.MinValue
                    select x;

            Func<int, bool> predicate = IsEven;
            Assert.Equal(q.All(predicate), q.All(predicate));
        }

        [Fact]
        public void SameResultsRepeatCallsStringQuery()
        {
            var q = from x in new[] { "!@#$%^", "C", "AAA", "", "Calling Twice", "SoS", string.Empty }
                    select x;

            Func<string, bool> predicate = string.IsNullOrEmpty;
            Assert.Equal(q.All(predicate), q.All(predicate));
        }

        public static IEnumerable<object[]> All_TestData()
        {
            Func<int, bool> isEvenFunc = IsEven;
            yield return new object[] { new int[0], isEvenFunc, true };
            yield return new object[] { new int[] { 3 }, isEvenFunc, false };
            yield return new object[] { new int[] { 4 }, isEvenFunc, true };
            yield return new object[] { new int[] { 3 }, isEvenFunc, false };
            yield return new object[] { new int[] { 4, 8, 3, 5, 10, 20, 12 }, isEvenFunc, false };
            yield return new object[] { new int[] { 4, 2, 10, 12, 8, 6, 3 }, isEvenFunc, false };
            yield return new object[] { new int[] { 4, 2, 10, 12, 8, 6, 14 }, isEvenFunc, true };

            int[] range = Enumerable.Range(1, 10).ToArray();
            yield return new object[] { range, (Func<int, bool>)(i => i > 0), true };
            for (int j = 1; j <= 10; j++)
            {
                int k = j; // Local copy for iterator
                yield return new object[] { range, (Func<int, bool>)(i => i > k), false };
            }
        }

        [Theory]
        [MemberData(nameof(All_TestData))]
        public void All(IEnumerable<int> source, Func<int, bool> predicate, bool expected)
        {
            Assert.Equal(expected, source.All(predicate));
        }

        [Theory, MemberData(nameof(All_TestData))]
        public void AllRunOnce(IEnumerable<int> source, Func<int, bool> predicate, bool expected)
        {
            Assert.Equal(expected, source.RunOnce().All(predicate));
        }

        [Fact]
        public void NullSource_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((IEnumerable<int>)null).All(i => i != 0));
        }

        [Fact]
        public void NullPredicate_ThrowsArgumentNullException()
        {
            Func<int, bool> predicate = null;
            AssertExtensions.Throws<ArgumentNullException>("predicate", () => Enumerable.Range(0, 3).All(predicate));
        }
    }
}
