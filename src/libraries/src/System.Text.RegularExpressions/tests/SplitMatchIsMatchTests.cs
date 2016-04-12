// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Text.RegularExpressions;
using Xunit;

public class SplitMatchIsMatchTests
{
    /*
        public static string[] Split(string input, string pattern); very simple
        public static bool IsMatch(string input, string pattern, string options); "m" option with 5 patterns
        public static bool IsMatch(string input, string pattern); "abc", "^b"
        public static Match Match(string input, string pattern); ???, "XSP_TEST_FAILURE SUCCESS", ".*\\b(\\w+)\\b"
    */
    [Fact]
    public static void SplitMatchIsMatch()
    {
        string[] strMatch1 = { "line2\nline3", "line2\nline3", "line3\n\nline4", "line3\n\nline4", "line2\nline3" };
        int[,] iMatch1 = { { 6, 11 }, { 6, 11 }, { 12, 12 }, { 12, 12 }, { 6, 11 } };
        string[] strGroup1 = { };
        string[] strGrpCap1 = { };

        string strMatch2 = "XSP_TEST_FAILURE SUCCESS";
        int[] iMatch2 = { 0, 24 };
        string[] strGroup2 = { "XSP_TEST_FAILURE SUCCESS", "SUCCESS" };
        int[] iGroup2 = { 17, 7 };
        string[] strGrpCap2 = { "SUCCESS" };
        int[] iGrpCap2 = { 17, 7 };
        
        // public static string[] Split(string input, string pattern); very simple
        string[] sa = Regex.Split("word0    word1    word2    word3", "    ");
        Assert.Equal(new string[] { "word0", "word1", "word2", "word3" }, sa);

        // public static bool IsMatch(string input, string pattern, string options); "m" option with 5 patterns
        Match match;
        sa = new string[] { "(line2$\n)line3", "(line2\n^)line3", "(line3\n$\n)line4", "(line3\n^\n)line4", "(line2$\n^)line3" };
        for (int i = 0; i < sa.Length; i++)
        {
            Assert.True(Regex.IsMatch("line1\nline2\nline3\n\nline4", sa[i], RegexOptions.Multiline));
            match = Regex.Match("line1\nline2\nline3\n\nline4", sa[i], RegexOptions.Multiline);

            Assert.Equal(strMatch1[i], match.Value);
            Assert.Equal(iMatch1[i, 0], match.Index);
            Assert.Equal(iMatch1[i, 1], match.Length);

            Assert.Equal(1, match.Captures.Count);
            for (int j = 0; j < match.Captures.Count; j++)
            {
                Assert.Equal(strMatch1[i], match.Captures[j].Value);
                Assert.Equal(iMatch1[i, 0], match.Captures[j].Index);
                Assert.Equal(iMatch1[i, 1], match.Captures[j].Length);
            }
            Assert.Equal(2, match.Groups.Count);
        }

        // public static bool IsMatch(string input, string pattern); "abc", "^b"
        Assert.False(Regex.IsMatch("abc", "^b"));
        
        // public static Match Match(string input, string pattern); ???, "XSP_TEST_FAILURE SUCCESS", ".*\\b(\\w+)\\b"
        match = Regex.Match("XSP_TEST_FAILURE SUCCESS", @".*\b(\w+)\b");
        Assert.True(match.Success);

        Assert.Equal(strMatch2, match.Value);
        Assert.Equal(iMatch2[0], match.Index);
        Assert.Equal(iMatch2[1], match.Length);

        Assert.Equal(1, match.Captures.Count);
        Assert.Equal(strMatch2, match.Captures[0].Value);
        Assert.Equal(iMatch2[0], match.Captures[0].Index);
        Assert.Equal(iMatch2[1], match.Captures[0].Length);

        Assert.Equal(2, match.Groups.Count);
        Assert.Equal(strMatch2, match.Groups[0].Value);
        Assert.Equal(iMatch2[0], match.Groups[0].Index);
        Assert.Equal(iMatch2[1], match.Groups[0].Length);

        Assert.Equal(1, match.Groups[0].Captures.Count);
        Assert.Equal(strMatch2, match.Groups[0].Captures[0].Value);
        Assert.Equal(iMatch2[0], match.Groups[0].Captures[0].Index);
        Assert.Equal(iMatch2[1], match.Groups[0].Captures[0].Length);

        for (int i = 1; i < match.Groups.Count; i++)
        {
            Assert.Equal(strGroup2[i], match.Groups[i].Value);
            Assert.Equal(iGroup2[0], match.Groups[i].Index);
            Assert.Equal(iGroup2[1], match.Groups[i].Length);

            Assert.Equal(1, match.Groups[i].Captures.Count);
            for (int j = 0; j < match.Groups[i].Captures.Count; j++)
            {
                Assert.Equal(strGroup2[i], match.Groups[i].Captures[j].Value);
                Assert.Equal(iGroup2[0], match.Groups[i].Captures[j].Index);
                Assert.Equal(iGroup2[1], match.Groups[i].Captures[j].Length);
            }
        }
    }
}
