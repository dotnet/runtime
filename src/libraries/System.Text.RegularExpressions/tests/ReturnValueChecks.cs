// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Text.RegularExpressions;
using Xunit;

public class ReturnValueChecks
{
    // All the work on Return value checks are spread throughout the test bed.
    [Fact]
    public static void ReturnValueChecksTests()
    {
        string strMatch1 = "aaabb.cc";
        int[] iMatch1 = { 0, 8 };
        string[] strGroup1 = { "aaabb.cc", "bb.c", "c" };
        int[,] iGroup1 = { { 0, 8 }, { 3, 4 }, { 7, 1 } };
        string[,] strGrpCap1 = { { "aaabb.cc" }, { "bb.c" }, { "c" } };
        int[,,] iGrpCap1 = { { { 0, 8 } }, { { 3, 4 } }, { { 7, 1 } } };

        // Using *, +, ?, {} : Actual - "a+\\.?b*\\.?c{2}"
        // "ab.cc"
        Regex result = new Regex(@"a+\.?b*\.+c{2}");
        Match match = result.Match("ab.cc");
        Assert.True(match.Success);

        // Using |, (), ^, $, .: Actual - "^aaa(bb.+)(d|c)$"
        // "aaabb.cc"
        result = new Regex("^aaa(bb.+)(d|c)$");
        match = result.Match("aaabb.cc");
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
                Assert.Equal(strGrpCap1[i, j], match.Groups[i].Captures[j].Value);
                Assert.Equal(iGrpCap1[i, j, 0], match.Groups[i].Captures[j].Index);
                Assert.Equal(iGrpCap1[i, j, 1], match.Groups[i].Captures[j].Length);
            }
        }
    }
}
