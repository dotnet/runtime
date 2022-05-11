// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

namespace Microsoft.Extensions.Configuration.Test
{
    public class ConfigurationPathComparerTest
    {
        [Fact]
        public void CompareWithNull()
        {
            ComparerTest(null, null, 0);
            ComparerTest(null, "a", -1);
            ComparerTest("b", null, 1);
            ComparerTest(null, "a:b", -1);
            ComparerTest(null, "a:b:c", -1);
        }

        [Fact]
        public void CompareWithSameLength()
        {
            ComparerTest("a", "a", 0);
            ComparerTest("a", "A", 0);

            ComparerTest("aB", "Ab", 0);
        }

        [Fact]
        public void CompareWithDifferentLengths()
        {
            ComparerTest("a", "aa", -1);
            ComparerTest("aa", "a", 1);
        }

        [Fact]
        public void CompareWithEmpty()
        {
            ComparerTest(":", "", 0);
            ComparerTest(":", "::", 0);
            ComparerTest(null, "", 0);
            ComparerTest(":", null, 0);
            ComparerTest("::", null, 0);
            ComparerTest(" : : ", null, 1);
            ComparerTest("b: :a", "b::a", -1);
            ComparerTest("b:\t:a", "b::a", -1);
            ComparerTest("b::a: ", "b::a:", 1);
        }

        [Fact]
        public void CompareWithLetters()
        {
            ComparerTest("a", "b", -1);
            ComparerTest("b", "a", 1);
        }

        [Fact]
        public void CompareWithNumbers()
        {
            ComparerTest("000", "0", 0);
            ComparerTest("001", "1", 0);

            ComparerTest("1", "1", 0);

            ComparerTest("1", "10", -1);
            ComparerTest("10", "1", 1);

            ComparerTest("2", "10", -1);
            ComparerTest("10", "2", 1);
        }

        [Fact]
        public void CompareWithNumbersAndLetters()
        {
            ComparerTest("1", "a", -1);
            ComparerTest("a", "1", 1);

            ComparerTest("100", "a", -1);
            ComparerTest("a", "100", 1);
        }

        [Fact]
        public void CompareWithNonNumbers()
        {
            ComparerTest("1a", "100", 1);
            ComparerTest("100", "1a", -1);

            ComparerTest("100a", "100", 1);
            ComparerTest("100", "100a", -1);

            ComparerTest("a100", "100", 1);
            ComparerTest("100", "a100", -1);

            ComparerTest("1a", "a", -1);
            ComparerTest("a", "1a", 1);
        }

        [Fact]
        public void CompareIdenticalPaths()
        {
            ComparerTest("abc:DEF:0:a100", "ABC:DEF:0:a100", 0);
        }

        [Fact]
        public void CompareDifferentPaths()
        {
            ComparerTest("abc:def", "ghi:2", -1);
            ComparerTest("ghi:2", "abc:def", 1);
        }

        [Fact]
        public void ComparePathsWithCommonPart()
        {
            ComparerTest("abc:def:XYQ", "abc:def:XYZ", -1);
            ComparerTest("abc:def:XYZ", "abc:def:XYQ", 1);
        }

        [Fact]
        public void ComparePathsWithCommonPartButShorter()
        {
            ComparerTest("abc:def", "abc:def:ghi", -1);
            ComparerTest("abc:def:ghi", "abc:def", 1);
        }

        [Fact]
        public void ComparePathsWithIndicesAtTheEnd()
        {
            ComparerTest("abc:def:2", "abc:def:10", -1);
            ComparerTest("abc:def:10", "abc:def:2", 1);

            ComparerTest("abc:def:10", "abc:def:22", -1);
            ComparerTest("abc:def:22", "abc:def:10", 1);
        }

        [Fact]
        public void ComparePathsWithIndicesInside()
        {
            ComparerTest("abc:def:1000:jkl", "abc:def:ghi:jkl", -1);
            ComparerTest("abc:def:ghi:jkl", "abc:def:1000:jkl", 1);

            ComparerTest("abc:def:10:jkl", "abc:def:22:jkl", -1);
            ComparerTest("abc:def:22:jkl", "abc:def:10:jkl", 1);
        }

        private static void ComparerTest(string a, string b, int expectedSign)
        {
            var result = ConfigurationKeyComparer.Instance.Compare(a, b);
            Assert.Equal(expectedSign, Math.Sign(result));
        }
    }
}
