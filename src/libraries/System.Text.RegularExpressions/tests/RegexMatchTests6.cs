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
        - Backreferences : Actual - "(\\w)\\1", "aa"
        - Actual - "(?<char>\\w)\\<char>", "aa"
        - Actual - "(?< 4 3>\\w)\\43", "aa"
    */
    [Fact]
    public static void RegexMatchTestCase6()
    {
        string strMatch1 = "aa";
        int[] iMatch1 = { 0, 2 };
        string[] strGroup1 = { "aa", "a" };
        int[,] iGroup1 = { { 0, 2 }, { 0, 1 } };
        string[][] strGrpCap1 = new string[2][];
        strGrpCap1[0] = new string[] { "aa" };
        strGrpCap1[1] = new string[] { "a" };
        int[][] iGrpCap1 = new int[2][];
        iGrpCap1[0] = new int[] { 0, 2 };
        iGrpCap1[1] = new int[] { 0, 1 };

        // Backreferences : Actual - "(\\w)\\1"
        // "aa"
        Regex regex = new Regex(@"(\w)\1");
        Match match = regex.Match("aa"); Assert.True(match.Success);

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

        Assert.Equal(1, match.Groups[0].Captures.Count);

        // Group 0's Capture is always the Match
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

        // Actual - "(?<char>\\w)\\<char>"
        // "aa"
        regex = new Regex(@"(?<char>\w)\<char>");
        match = regex.Match("aa");
        Assert.True(match.Success);

        // Actual - "(?<43>\\w)\\43"
        // "aa"
        regex = new Regex(@"(?<43>\w)\43");
        match = regex.Match("aa");
        Assert.True(match.Success);
    }
}
