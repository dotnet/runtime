// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Text.RegularExpressions;
using Xunit;

public partial class RegexMatchTests
{
    /*
    public Match Match(string input, RegexOptions options);
        - Using "n" Regex option. Only explicitly named groups should be captured: Actual - "([0-9]*)\\s(?<s>[a-z_A-Z]+)", "n", "200 dollars"
        - Single line mode "s". Includes new line character: Actual - "([^/]+)","s", "abc\n"
        - "x" option. Removes unescaped white space from the pattern: Actual - " ([^/]+) ","x", "abc"
        - "x" option. Removes unescaped white space from the pattern: Actual - "\x20([^/]+)\x20","x", "abc"
    */
    [Fact]
    public static void RegexMatchTestCase8()
    {
        string strMatch1 = "200 dollars";
        int[] iMatch1 = { 0, 11 };
        string[] strGroup1 = { "200 dollars", "dollars" };
        int[,] iGroup1 = { { 0, 11 }, { 4, 7 } };
        string[][] strGrpCap1 = new string[2][];
        strGrpCap1[0] = new string[] { "200 dollars" };
        strGrpCap1[1] = new string[] { "dollars" };
        int[][] iGrpCap1 = new int[2][];
        iGrpCap1[0] = new int[] { 0, 11 };
        iGrpCap1[1] = new int[] { 4, 7 };
        int[] iGrpCapCnt1 = { 1, 1 };

        string strMatch2 = "abc\nsfc";
        int[] iMatch2 = { 0, 7 };
        string[] strGroup2 = { "abc\nsfc", "abc\nsfc" };
        int[,] iGroup2 = { { 0, 7 }, { 0, 7 } };
        string[][] strGrpCap2 = new string[2][];
        strGrpCap2[0] = new string[] { "abc\nsfc" };
        strGrpCap2[1] = new string[] { "abc\nsfc" };
        int[][] iGrpCap2 = new int[2][];
        iGrpCap2[0] = new int[] { 0, 11 };
        iGrpCap2[1] = new int[] { 0, 7 };
        int[] iGrpCapCnt2 = { 1, 1 };

        // Using "n" Regex option. Only explicitly named groups should be captured : Actual - "([0-9]*)\\s(?<s>[a-z_A-Z]+)", "n"
        // "200 dollars"
        Regex regex = new Regex(@"([0-9]*)\s(?<s>[a-z_A-Z]+)", RegexOptions.ExplicitCapture);
        Match match = regex.Match("200 dollars");
        Assert.True(match.Success);

        Assert.Equal(strMatch1, match.Value);
        Assert.Equal(iMatch1[0], match.Index);
        Assert.Equal(iMatch1[1], match.Length);

        Assert.Equal(1, match.Captures.Count);
        Assert.Equal(strMatch1, match.Captures[0].Value);
        Assert.Equal(iMatch1[0], match.Captures[0].Index);
        Assert.Equal(iMatch1[1], match.Captures[0].Length);

        Assert.Equal(2, match.Groups.Count);

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

        // Single line mode "s". Includes new line character. : Actual - "([^/]+)","s"
        // "abc\n"
        regex = new Regex("(.*)", RegexOptions.Singleline);
        match = regex.Match("abc\nsfc");
        Assert.True(match.Success);

        Assert.Equal(strMatch2, match.Value);
        Assert.Equal(iMatch2[0], match.Index);
        Assert.Equal(iMatch2[1], match.Length);

        Assert.Equal(1, match.Captures.Count);
        Assert.Equal(strMatch2, match.Captures[0].Value);
        Assert.Equal(iMatch2[0], match.Captures[0].Index);
        Assert.Equal(iMatch2[1], match.Captures[0].Length);

        Assert.Equal(2, match.Groups.Count);

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

        // "x" option. Removes unescaped white space from the pattern. : Actual - " ([^/]+) ","x"
        // "abc"
        regex = new Regex("            ((.)+)      ", RegexOptions.IgnorePatternWhitespace);
        match = regex.Match("abc");
        Assert.True(match.Success);

        // "x" option. Removes unescaped white space from the pattern. : Actual - "\x20([^/]+)\x20","x"
        // "abc"
        regex = new Regex("\x20([^/]+)\x20\x20\x20\x20\x20\x20\x20", RegexOptions.IgnorePatternWhitespace);
        match = regex.Match(" abc       ");
        Assert.True(match.Success);
    }
}
