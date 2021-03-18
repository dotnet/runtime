// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

using Xunit;

namespace System.Linq.Tests
{
    public class MatchTests : EnumerableTests
    {
        [Fact]
        public void TestEmptySource()
        {
            var (matched, unmatched) = Array.Empty<int>().Match(n => n != 0);

            Assert.NotNull(matched);
            Assert.Empty(matched);

            Assert.NotNull(unmatched);
            Assert.Empty(unmatched);
        }

        [Fact]
        public void TestIntCollection()
        {
            int[] source = { -10, 2, 9, 4, 3, 0, 2, 17, 42 };

            var (matched, unmatched) = source.Match(n => n % 2 == 0);

            Assert.True(matched.SequenceEqual(new int[] { -10, 2, 4, 0, 2, 42 }));
            Assert.True(unmatched.SequenceEqual(new int[] { 9, 3, 17 }));
        }

        [Fact]
        public void TestStringCollection()
        {
            string[] source = { "!@#$%^", "C", "AAA", "", "\t", "Calling Twice", "SoS", string.Empty };

            var (matched, unmatched) = source.Match(s => string.IsNullOrWhiteSpace(s));

            Assert.True(matched.SequenceEqual(new string[] { "", "\t", string.Empty }));
            Assert.True(unmatched.SequenceEqual(new string[] { "!@#$%^", "C", "AAA", "Calling Twice", "SoS" }));
        }

        [Fact]
        public void SameResultsRepeatCallsIntQuery()
        {
            var q = from x in new[] { 9999, 0, 888, -1, 66, -777, 1, 2, -12345 }
                    where x > int.MinValue
                    select x;

            var (matched, unmatched) = q.Match(n => n % 2 == 0);
            var (secondMatched, secondUnmatched) = q.Match(n => n % 2 == 0);

            Assert.True(matched.SequenceEqual(secondMatched));
            Assert.True(unmatched.SequenceEqual(secondUnmatched));
        }

        [Fact]
        public void SameResultsRepeatCallsStringQuery()
        {
            var q = from x in new string[] { "!@#$%^", "C", "AAA", "", "\t", "Calling Twice", "SoS", string.Empty }
                    where x.Length < 100
                    select x;

            var (matched, unmatched) = q.Match(s => string.IsNullOrWhiteSpace(s));
            var (secondMatched, secondUnmatched) = q.Match(s => string.IsNullOrWhiteSpace(s));

            Assert.True(matched.SequenceEqual(secondMatched));
            Assert.True(unmatched.SequenceEqual(secondUnmatched));
        }

        [Fact]
        public void EmptyMatchedResult()
        {
            int[] source = { -10, 2, 9, 4, 3, 0, 2, 17, 42 };

            var (matched, unmatched) = source.Match(n => n > 1000);

            Assert.NotNull(matched);
            Assert.Empty(matched);

            Assert.NotNull(unmatched);
            Assert.NotEmpty(unmatched);
        }

        [Fact]
        public void EmptyUnmatchedResult()
        {
            int[] source = { -10, 2, 9, 4, 3, 0, 2, 17, 42 };

            var (matched, unmatched) = source.Match(n => n < 1000);

            Assert.NotNull(matched);
            Assert.NotEmpty(matched);

            Assert.NotNull(unmatched);
            Assert.Empty(unmatched);
        }

        [Fact]
        public void NullSource_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((IEnumerable<int>)null).Match(i => i == 0));
        }

        [Fact]
        public void NullPredicate_ThrowsArgumentNullException()
        {
            Func<int, bool> predicate = null;
            AssertExtensions.Throws<ArgumentNullException>("predicate", () => Enumerable.Range(0, 3).Match(predicate));
        }
    }
}
