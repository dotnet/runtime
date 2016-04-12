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
        - Numbering pattern slots: "(?< 1>\\d{3})(?< 2>\\d{3})(?< 3>\\d{4})", "8885551111"
        - Not naming pattern slots at all: "^(cat|chat)", "cats are bad"
        - "([0-9]+(\\.[0-9]+){3})", "209.25.0.111"
    */
    [Fact]
    public static void RegexMatchTestCase11()
    {
        string strMatch1 = "209.25.0.111";
        int[] iMatch1 = { 0, 12 };
        string[] strGroup1 = { "209.25.0.111", "209.25.0.111", ".111" };
        int[,] iGroup1 = { { 0, 12 }, { 0, 12 }, { 8, 4 } };
        string[][] strGrpCap1 = new string[3][];
        strGrpCap1[0] = new string[] { "209.25.0.111" };
        strGrpCap1[1] = new string[] { "209.25.0.111" };
        strGrpCap1[2] = new string[] { ".25", ".0", ".111" };
        int[][] iGrpCap1 = new int[3][];
        iGrpCap1[0] = new int[] { 0, 12 };
        iGrpCap1[1] = new int[] { 0, 12 };
        iGrpCap1[2] = new int[] { 3, 6, 8, 3, 2, 4 };
        int[] iGrpCapCnt1 = { 1, 1, 3 };

        // Numbering pattern slots: "(?<1>\\d{3})(?<2>\\d{3})(?<3>\\d{4})"
        // "8885551111"
        Regex regex = new Regex(@"(?<1>\d{3})(?<2>\d{3})(?<3>\d{4})");
        Match match = regex.Match("8885551111");
        Assert.True(match.Success);
        
        match = regex.Match("Invalid string");
        Assert.False(match.Success);

        // Not naming pattern slots at all: "^(cat|chat)"
        // "cats are bad"
        regex = new Regex("^(cat|chat)");
        match = regex.Match("cats are bad");
        Assert.True(match.Success);

        // "([0-9]+(\\.[0-9]+){3})"
        // "209.25.0.111"
        regex = new Regex(@"([0-9]+(\.[0-9]+){3})");
        match = regex.Match("209.25.0.111");
        Assert.True(match.Success);

        Assert.Equal(strMatch1, match.Value);
        Assert.Equal(iMatch1[0], match.Index);
        Assert.Equal(iMatch1[1], match.Length);

        Assert.Equal(1, match.Captures.Count);

        Assert.Equal(strMatch1, match.Captures[0].Value);
        Assert.Equal(iMatch1[0], match.Captures[0].Index);
        Assert.Equal(iMatch1[1], match.Captures[0].Length);

        Assert.Equal(3, match.Groups.Count);

        // Group 0 always is the Match
        Assert.Equal(strMatch1, match.Groups[0].Value);
        Assert.Equal(iMatch1[0], match.Groups[0].Index);
        Assert.Equal(iMatch1[1], match.Groups[0].Length);

        Assert.Equal(1, match.Groups[0].Captures.Count);
        Assert.Equal(strMatch1, match.Groups[0].Captures[0].Value);
        Assert.Equal(iMatch1[0], match.Groups[0].Captures[0].Index);
        Assert.Equal(iMatch1[1], match.Groups[0].Captures[0].Length);
        
        for (int i = 1; i < match.Groups.Count; i++)
        {
            Assert.Equal(strGroup1[i], match.Groups[i].Value);
            Assert.Equal(iGroup1[i, 0], match.Groups[i].Index);
            Assert.Equal(iGroup1[i, 1], match.Groups[i].Length);

            Assert.Equal(iGrpCapCnt1[i], match.Groups[i].Captures.Count);
            for (int j = 0; j < match.Groups[i].Captures.Count; j++)
            {
                Assert.Equal(strGrpCap1[i][j], match.Groups[i].Captures[j].Value);
                Assert.Equal(iGrpCap1[i][j], match.Groups[i].Captures[j].Index);
                Assert.Equal(iGrpCap1[i][match.Groups[i].Captures.Count + j], match.Groups[i].Captures[j].Length);
            }
        }
    }
}
