// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.FileSystemGlobbing.Internal.PathSegments;
using Xunit;

namespace Microsoft.Extensions.FileSystemGlobbing.Tests.PatternSegments
{
    public class WildcardPathSegmentTests
    {
        [Theory]
        [InlineData(StringComparison.Ordinal)]
        [InlineData(StringComparison.OrdinalIgnoreCase)]
        public void DefaultConstructor(StringComparison comparisonType)
        {
            var paramBegin = "begin";
            var paramContains = new List<string> { "1", "2", "three" };
            var paramEnd = "end";

            var segment = new WildcardPathSegment(paramBegin, paramContains, paramEnd, comparisonType);

            Assert.Equal(paramBegin, segment.BeginsWith);
            Assert.Equal<string>(paramContains, segment.Contains);
            Assert.Equal(paramEnd, segment.EndsWith);
        }

        [Theory]
        [InlineData(StringComparison.CurrentCulture)]
        [InlineData(StringComparison.CurrentCultureIgnoreCase)]
        [InlineData(StringComparison.InvariantCulture)]
        [InlineData(StringComparison.InvariantCultureIgnoreCase)]
        public void DefaultConstructor_ThrowException_WhenNotOrdinalComparison(StringComparison comparisonType)
        {
            var paramBegin = "begin";
            var paramContains = new List<string> { "1", "2", "three" };
            var paramEnd = "end";

            AssertExtensions.ThrowsContains<InvalidOperationException>(() => new WildcardPathSegment(paramBegin, paramContains, paramEnd, comparisonType), comparisonType.ToString());
        }

        [Theory]
        [MemberData(nameof(GetPositiveOrdinalIgnoreCaseDataSample))]
        public void PositiveOrdinalIgnoreCaseMatch(string testSample, object segment)
        {
            var wildcardPathSegment = (WildcardPathSegment)segment;
            Assert.True(
                wildcardPathSegment.Match(testSample),
                string.Format("[TestSample: {0}] [Wildcard: {1}]", testSample, Serialize(wildcardPathSegment)));
        }

        [Theory]
        [MemberData(nameof(GetNegativeOrdinalIgnoreCaseDataSample))]
        public void NegativeOrdinalIgnoreCaseMatch(string testSample, object segment)
        {
            var wildcardPathSegment = (WildcardPathSegment)segment;
            Assert.False(
                wildcardPathSegment.Match(testSample),
                string.Format("[TestSample: {0}] [Wildcard: {1}]", testSample, Serialize(wildcardPathSegment)));
        }

        [Theory]
        [MemberData(nameof(GetPositiveOrdinalDataSample))]
        public void PositiveOrdinalMatch(string testSample, object segment)
        {
            var wildcardPathSegment = (WildcardPathSegment)segment;
            Assert.True(
                wildcardPathSegment.Match(testSample),
                string.Format("[TestSample: {0}] [Wildcard: {1}]", testSample, Serialize(wildcardPathSegment)));
        }

        [Theory]
        [MemberData(nameof(GetNegativeOrdinalDataSample))]
        public void NegativeOrdinalMatch(string testSample, object segment)
        {
            var wildcardPathSegment = (WildcardPathSegment)segment;
            Assert.False(
                wildcardPathSegment.Match(testSample),
                string.Format("[TestSample: {0}] [Wildcard: {1}]", testSample, Serialize(wildcardPathSegment)));
        }

        public static IEnumerable<object[]> GetPositiveOrdinalIgnoreCaseDataSample()
        {
            yield return WrapResult(StringComparison.OrdinalIgnoreCase, "abc", "a", "c");
            yield return WrapResult(StringComparison.OrdinalIgnoreCase, "abBb123c", "a", "c");
            yield return WrapResult(StringComparison.OrdinalIgnoreCase, "aaac", "a", "c");
            yield return WrapResult(StringComparison.OrdinalIgnoreCase, "acccc", "a", "c");
            yield return WrapResult(StringComparison.OrdinalIgnoreCase, "aacc", "a", "c");
            yield return WrapResult(StringComparison.OrdinalIgnoreCase, "aacc", "aa", "c");
            yield return WrapResult(StringComparison.OrdinalIgnoreCase, "acc", "ac", "c");
            yield return WrapResult(StringComparison.OrdinalIgnoreCase, "abcdefgh", "ab", "cd", "ef", "gh");
            yield return WrapResult(StringComparison.OrdinalIgnoreCase, "abCDEfgh", "ab", "cd", "ef", "gh");
            yield return WrapResult(StringComparison.OrdinalIgnoreCase, "ab123cd321ef123gh", "ab", "cd", "ef", "gh");
            yield return WrapResult(StringComparison.OrdinalIgnoreCase, "abcd321ef123gh", "ab", "cd", "ef", "gh");
            yield return WrapResult(StringComparison.OrdinalIgnoreCase, "ababcd321ef123gh", "ab", "cd", "ef", "gh");
        }

        public static IEnumerable<object[]> GetNegativeOrdinalIgnoreCaseDataSample()
        {
            yield return WrapResult(StringComparison.OrdinalIgnoreCase, "aa", "a", "c");
            yield return WrapResult(StringComparison.OrdinalIgnoreCase, "cc", "a", "c");
            yield return WrapResult(StringComparison.OrdinalIgnoreCase, "ab", "a", "c");
            yield return WrapResult(StringComparison.OrdinalIgnoreCase, "ab", "a", "b", "c");
            yield return WrapResult(StringComparison.OrdinalIgnoreCase, "bc", "a", "b", "c");
            yield return WrapResult(StringComparison.OrdinalIgnoreCase, "ac", "a", "b", "c");
            yield return WrapResult(StringComparison.OrdinalIgnoreCase, "abc", "a", "b", "b", "c");
            yield return WrapResult(StringComparison.OrdinalIgnoreCase, "ab", "ab", "c");
            yield return WrapResult(StringComparison.OrdinalIgnoreCase, "ab", "abb", "c");
            yield return WrapResult(StringComparison.OrdinalIgnoreCase, "ac", "ac", "c");
        }

        public static IEnumerable<object[]> GetPositiveOrdinalDataSample()
        {
            yield return WrapResult(StringComparison.Ordinal, "abc", "a", "c");
            yield return WrapResult(StringComparison.Ordinal, "abBb123c", "a", "c");
            yield return WrapResult(StringComparison.Ordinal, "aaac", "a", "c");
            yield return WrapResult(StringComparison.Ordinal, "Aaac", "A", "c");
            yield return WrapResult(StringComparison.Ordinal, "acccC", "a", "C");
            yield return WrapResult(StringComparison.Ordinal, "aacc", "a", "c");
            yield return WrapResult(StringComparison.Ordinal, "aAcc", "aA", "c");
            yield return WrapResult(StringComparison.Ordinal, "acc", "ac", "c");
            yield return WrapResult(StringComparison.Ordinal, "abcDefgh", "ab", "cD", "ef", "gh");
            yield return WrapResult(StringComparison.Ordinal, "aB123cd321ef123gh", "aB", "cd", "ef", "gh");
            yield return WrapResult(StringComparison.Ordinal, "abcd321ef123gh", "ab", "cd", "ef", "gh");
            yield return WrapResult(StringComparison.Ordinal, "ababcdCD321ef123gh", "ab", "cd", "ef", "gh");
            yield return WrapResult(StringComparison.Ordinal, "ababcdCD321ef123gh", "ab", "CD", "ef", "gh");
            yield return WrapResult(StringComparison.Ordinal, "ababcd321eF123gh", "ab", "cd", "eF", "gh");
        }

        public static IEnumerable<object[]> GetNegativeOrdinalDataSample()
        {
            yield return WrapResult(StringComparison.Ordinal, "aa", "a", "c");
            yield return WrapResult(StringComparison.Ordinal, "abc", "A", "c");
            yield return WrapResult(StringComparison.Ordinal, "cc", "a", "c");
            yield return WrapResult(StringComparison.Ordinal, "ab", "a", "c");
            yield return WrapResult(StringComparison.Ordinal, "ab", "a", "b", "c");
            yield return WrapResult(StringComparison.Ordinal, "bc", "a", "b", "c");
            yield return WrapResult(StringComparison.Ordinal, "ac", "a", "b", "c");
            yield return WrapResult(StringComparison.Ordinal, "abc", "a", "b", "b", "c");
            yield return WrapResult(StringComparison.Ordinal, "ab", "ab", "c");
            yield return WrapResult(StringComparison.Ordinal, "ab", "abb", "c");
            yield return WrapResult(StringComparison.Ordinal, "ac", "ac", "c");
            yield return WrapResult(StringComparison.Ordinal, "abBb123C", "a", "c");
            yield return WrapResult(StringComparison.Ordinal, "Aaac", "a", "c");
            yield return WrapResult(StringComparison.Ordinal, "aAac", "A", "c");
            yield return WrapResult(StringComparison.Ordinal, "aCc", "a", "C");
            yield return WrapResult(StringComparison.Ordinal, "aacc", "aA", "c");
            yield return WrapResult(StringComparison.Ordinal, "acc", "aC", "c");
            yield return WrapResult(StringComparison.Ordinal, "abcDefgh", "ab", "cd", "ef", "gh");
            yield return WrapResult(StringComparison.Ordinal, "aB123cd321ef123gh", "aB", "cd", "EF", "gh");
            yield return WrapResult(StringComparison.Ordinal, "abcd321ef123gh", "ab", "cd", "efF", "gh");
            yield return WrapResult(StringComparison.Ordinal, "ababcdCD321ef123gh", "AB", "cd", "ef", "gh");
            yield return WrapResult(StringComparison.Ordinal, "ababcdCD321ef123gh", "ab", "CD", "EF", "gh");
        }

        private static object[] WrapResult(StringComparison comparisonType, params string[] values)
        {
            if (values == null || values.Length < 3)
            {
                throw new InvalidOperationException("At least three values are required to create a data sample");
            }

            var beginWith = values[1];
            var endWith = values[values.Length - 1];
            var contains = values.Skip(2).Take(values.Length - 3);

            return new object[] { values[0], new WildcardPathSegment(beginWith, contains.ToList(), endWith, comparisonType) };
        }

        private static string Serialize(WildcardPathSegment segment)
        {
            return string.Format("{0}:{1}:{2}",
                segment.BeginsWith,
                string.Join(",", segment.Contains.ToArray()),
                segment.EndsWith);
        }
    }
}
