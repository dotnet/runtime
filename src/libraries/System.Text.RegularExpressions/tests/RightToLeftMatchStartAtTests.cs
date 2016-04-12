// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Text.RegularExpressions;
using Xunit;

public class RightToLeftMatchStartAtTests
{
    /*
    public Boolean RightToLeft;
    public Match Match(string input, int start);
        - "aaa", "aaabbb", 3
        - "aaa", "r", "aaabbb", 3
        - "AAA", "i", "aaabbb", 3
    */
    [Fact]
    public static void RightToLeftMatchStartAt()
    {
        string strMatch1 = "aaa";
        int[] iMatch1 = { 0, 3 };
        string[] strGroup1 = { "aaa" };
        int[,] iGroup1 = { { 0, 3 } };
        string[][] strGrpCap1 = new string[1][];
        strGrpCap1[0] = new string[] { "aaa" };
        int[][] iGrpCap1 = new int[1][];
        iGrpCap1[0] = new int[] { 5, 9, 3, 3 };
        int[] iGrpCapCnt1 = { 1, 1 };
        
        // public Boolean RightToLeft;
        Regex regex = new Regex("aaa");
        Assert.False(regex.RightToLeft); 
        
        regex = new Regex("aaa", RegexOptions.RightToLeft);
        Assert.True(regex.RightToLeft);

        // public Match Match(string input, int start); "aaa", "aaabbb", 3
        regex = new Regex("aaa");
        Match match = regex.Match("aaabbb", 3);
        Assert.False(match.Success);

        // [] public static Match Match(string input, Int32 startat); "aaa", "r", "aaabbb", 3
        regex = new Regex("aaa", RegexOptions.RightToLeft);
        match = regex.Match("aaabbb", 3);
        Assert.True(match.Success);

        Assert.Equal(strMatch1, match.Value);
        Assert.Equal(iMatch1[0], match.Index);
        Assert.Equal(iMatch1[1], match.Length);

        Assert.Equal(1, match.Captures.Count);

        Assert.Equal(strMatch1, match.Captures[0].Value);
        Assert.Equal(iMatch1[0], match.Captures[0].Index);
        Assert.Equal(iMatch1[1], match.Captures[0].Length);

        Assert.Equal(1, match.Groups.Count);

        // Group 0 always is the Match
        Assert.Equal(strMatch1, match.Groups[0].Value);
        Assert.Equal(iMatch1[0], match.Groups[0].Index);
        Assert.Equal(iMatch1[1], match.Groups[0].Length);

        Assert.Equal(1, match.Groups[0].Captures.Count);
        Assert.Equal(strMatch1, match.Groups[0].Captures[0].Value);
        Assert.Equal(iMatch1[0], match.Groups[0].Captures[0].Index);
        Assert.Equal(iMatch1[1], match.Groups[0].Captures[0].Length);

        // public Match Match(string input); "AAA", "i", "aaabbb", 3
        regex = new Regex("AAA", RegexOptions.IgnoreCase);
        match = regex.Match("aaabbb");
        Assert.True(match.Success);
    }
}
