// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Text.RegularExpressions;
using Xunit;

public partial class RegexMatchTests
{
    /*
    public static Match Match(string input);
        - Alternation constructs : Actual - "(111|aaa)", "aaa"
        - Actual - "abc(?(1)111|222)", "abc222"
        - Actual - "(?< 1>\\d+)abc(?(1)222|111)", "111abc222"
    */
    [Fact]
    public static void RegexMatchTestCase7()
    {
        string strMatch = "111abc222";
        int[] iMatch = { 0, 9 };
        string[] strGroup = { "111abc222", "111" };
        int[,] iGroup = { { 0, 9 }, { 0, 3 } };
        string[][] strGrpCap = new string[2][];
        strGrpCap[0] = new string[] { "111abc222" };
        strGrpCap[1] = new string[] { "111" };
        int[][] iGrpCap = new int[2][];
        iGrpCap[0] = new int[] { 0, 9 };
        iGrpCap[1] = new int[] { 0, 3 };
        int[] iGrpCapCnt = { 1, 1 };

        // Alternation constructs : Actual - "(111|aaa)"
        // "aaa"
        Regex regex = new Regex("(111|aaa)");
        Match match = regex.Match("aaa");
        Assert.True(match.Success);
        Assert.Equal("aaa", match.Groups[1].Value);

        // Actual - "abc(?(1)111|222)"
        // "abc222"
        regex = new Regex("(abbc)(?(1)111|222)");
        match = regex.Match("abbc222");
        Assert.False(match.Success);

        // Actual - "(?<1>\\d+)abc(?(1)222|111)"
        // "111abc222"
        regex = new Regex(@"(?<MyDigits>\d+)abc(?(MyDigits)222|111)");
        match = regex.Match("111abc222"); Assert.True(match.Success);
        Assert.True(match.Success);

        Assert.Equal(strMatch, match.Value);
        Assert.Equal(iMatch[0], match.Index);
        Assert.Equal(iMatch[1], match.Length);

        Assert.Equal(1, match.Captures.Count);
        Assert.Equal(strMatch, match.Captures[0].Value);
        Assert.Equal(iMatch[0], match.Captures[0].Index);
        Assert.Equal(iMatch[1], match.Captures[0].Length);

        Assert.Equal(2, match.Groups.Count);

        // Group 0 always is the Match
        Assert.Equal(strMatch, match.Groups[0].Value);
        Assert.Equal(iMatch[0], match.Groups[0].Index);
        Assert.Equal(iMatch[1], match.Groups[0].Length);

        //Group 0's Capture is always the Match
        Assert.Equal(1, match.Groups[0].Captures.Count);
        Assert.Equal(strMatch, match.Groups[0].Captures[0].Value);
        Assert.Equal(iMatch[0], match.Groups[0].Captures[0].Index);
        Assert.Equal(iMatch[1], match.Groups[0].Captures[0].Length);

        for (int i = 1; i < match.Groups.Count; i++)
        {
            Assert.Equal(strGroup[i], match.Groups[i].Value);
            Assert.Equal(iGroup[i, 0], match.Groups[i].Index);
            Assert.Equal(iGroup[i, 1], match.Groups[i].Length);

            Assert.Equal(1, match.Groups[i].Captures.Count);
            for (int j = 0; j < match.Groups[i].Captures.Count; j++)
            {
                Assert.Equal(strGrpCap[i][j], match.Groups[i].Captures[j].Value);
                Assert.Equal(iGrpCap[i][j], match.Groups[i].Captures[j].Index);
                Assert.Equal(iGrpCap[i][match.Groups[i].Captures.Count + j], match.Groups[i].Captures[j].Length);
            }
        }
    }
}
