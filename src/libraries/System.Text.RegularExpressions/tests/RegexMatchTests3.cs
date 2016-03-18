// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Text.RegularExpressions;
using Xunit;

public partial class RegexMatchTests
{
    /*
    public Match Match(string input);
        - Using greedy quantifiers : Actual - "(a+)(b*)(c?)", "aaabbbccc"
        - Using lazy quantifiers: Actual - "(d+?)(e*?)(f??)", "dddeeefff"
    */
    [Fact]
    public static void RegexMatchTestCase3()
    {
        string strMatch1 = "aaabbbc";
        int[] iMatch1 = { 0, 7 };
        string[] strGroup1 = { "aaabbbc", "aaa", "bbb", "c" };
        int[,] iGroup1 = { { 0, 7 }, { 0, 3 }, { 3, 3 }, { 6, 1 } };

        string[][] strGrpCap1 = new string[4][];
        strGrpCap1[0] = new string[] { "aaabbbc" };
        strGrpCap1[1] = new string[] { "aaa" };
        strGrpCap1[2] = new string[] { "bbb" };
        strGrpCap1[3] = new string[] { "c" };

        int[][] iGrpCap1 = new int[4][];
        iGrpCap1[0] = new int[] { 5, 9, 3, 3 };
        iGrpCap1[1] = new int[] { 0, 3 };
        iGrpCap1[2] = new int[] { 3, 3 };
        iGrpCap1[3] = new int[] { 6, 1 };

        string strMatch2 = "d";
        int[] iMatch2 = { 0, 1 };
        string[] strGroup2 = { "d", "d", string.Empty, string.Empty };
        int[,] iGroup2 = { { 0, 1 }, { 0, 1 }, { 1, 0 }, { 1, 0 } };

        string[][] strGrpCap2 = new string[4][];
        strGrpCap2[0] = new string[] { "d" };
        strGrpCap2[1] = new string[] { "d" };
        strGrpCap2[2] = new string[] { string.Empty };
        strGrpCap2[3] = new string[] { string.Empty };

        int[][] iGrpCap2 = new int[4][];
        iGrpCap2[0] = new int[] { 0, 1 };
        iGrpCap2[1] = new int[] { 0, 1 };
        iGrpCap2[2] = new int[] { 1, 0 };
        iGrpCap2[3] = new int[] { 1, 0 };

        // - Using greedy quantifiers : Actual - "(a+)(b*)(c?)"
        // "aaabbbccc"
        Regex regex = new Regex("(a+)(b*)(c?)");
        Match match = regex.Match("aaabbbccc");
        Assert.True(match.Success);

        Assert.Equal(strMatch1, match.Value);
        Assert.Equal(iMatch1[0], match.Index);
        Assert.Equal(iMatch1[1], match.Length);

        Assert.Equal(1, match.Captures.Count);
        Assert.Equal(strMatch1, match.Captures[0].Value);
        Assert.Equal(iMatch1[0], match.Captures[0].Index);
        Assert.Equal(iMatch1[1], match.Captures[0].Length);

        Assert.Equal(4, match.Groups.Count);

        // Group 0 always is the Match
        Assert.Equal(strMatch1, match.Groups[0].Value);
        Assert.Equal(iMatch1[0], match.Groups[0].Index);
        Assert.Equal(iMatch1[1], match.Groups[0].Length);

        //Group 0's Capture is always the Match
        Assert.Equal(1, match.Groups[0].Captures.Count);
        Assert.Equal(strMatch1, match.Groups[0].Captures[0].Value);
        Assert.Equal(iMatch1[0], match.Groups[0].Captures[0].Index);
        Assert.Equal(iMatch1[1], match.Groups[0].Captures[0].Length);

        for (int i = 1; i < match.Groups.Count; i++)
        {
            Assert.Equal(strGroup1[i], match.Groups[i].Value);
            Assert.Equal(iGroup1[i, 0], match.Groups[i].Index);
            Assert.Equal(iGroup1[i, 1], match.Groups[i].Length);

            Assert.Equal(1, match.Groups[i].Captures.Count);

            for (int j = 0; j < match.Groups[i].Captures.Count; j++)
            {
                Assert.Equal(strGrpCap1[i][j], match.Groups[i].Captures[j].Value);
                Assert.Equal(iGrpCap1[i][j], match.Groups[i].Captures[j].Index);
                Assert.Equal(iGrpCap1[i][match.Groups[i].Captures.Count + j], match.Groups[i].Captures[j].Length);
            }
        }

        // Using lazy quantifiers: Actual - "(d+?)(e*?)(f??)"
        // "dddeeefff"
        // Interesting match from this pattern and input. If needed to go to the end of the string change the ? to + in the last lazy quantifier
        regex = new Regex("(d+?)(e*?)(f??)");
        match = regex.Match("dddeeefff");
        Assert.True(match.Success);

        Assert.Equal(strMatch2, match.Value);
        Assert.Equal(iMatch2[0], match.Index);
        Assert.Equal(iMatch2[1], match.Length);

        Assert.Equal(1, match.Captures.Count);
        Assert.Equal(strMatch2, match.Captures[0].Value);
        Assert.Equal(iMatch2[0], match.Captures[0].Index);
        Assert.Equal(iMatch2[1], match.Captures[0].Length);

        Assert.Equal(4, match.Groups.Count);

        // Group 0 always is the Match
        Assert.Equal(strMatch2, match.Groups[0].Value);
        Assert.Equal(iMatch2[0], match.Groups[0].Index);
        Assert.Equal(iMatch2[1], match.Groups[0].Length);

        //Group 0's Capture is always the Match
        Assert.Equal(1, match.Groups[0].Captures.Count);
        Assert.Equal(strMatch2, match.Groups[0].Captures[0].Value);
        Assert.Equal(iMatch2[0], match.Groups[0].Captures[0].Index);
        Assert.Equal(iMatch2[1], match.Groups[0].Captures[0].Length);

        for (int i = 1; i < match.Groups.Count; i++)
        {
            Assert.Equal(strGroup2[i], match.Groups[i].Value);
            Assert.Equal(iGroup2[i, 0], match.Groups[i].Index);
            Assert.Equal(iGroup2[i, 1], match.Groups[i].Length);

            Assert.Equal(1, match.Groups[i].Captures.Count);
            for (int j = 0; j < match.Groups[i].Captures.Count; j++)
            {
                Assert.Equal(strGrpCap2[i][j], match.Groups[i].Captures[j].Value);
                Assert.Equal(iGrpCap2[i][j], match.Groups[i].Captures[j].Index);
                Assert.Equal(iGrpCap2[i][match.Groups[i].Captures.Count + j], match.Groups[i].Captures[j].Length);
            }
        }
    }
}
