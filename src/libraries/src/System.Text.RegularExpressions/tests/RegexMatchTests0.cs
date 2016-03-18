// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Text.RegularExpressions;
using Xunit;

public partial class RegexMatchTests
{
    /*
    public static Match Match(string input, string pattern);
        - Testing \B special character escape: "adfadsfSUCCESSadsfadsf", ".*\\B(SUCCESS)\\B.*"
        - Testing octal sequence matches: "011", "\\060(\\061)?\\061"
        - Testing hexadecimal sequence matches: "012", "(\\x30\\x31\\x32)"
        - Testing control character escapes???: "2", "(\u0032)"
    */
    [Fact]
    public static void RegexMatchTestCase0()
    {
        string strMatch1 = "adfadsfSUCCESSadsfadsf";
        int[] iMatch1 = { 0, 22 };
        string[] strGroup1 = { "adfadsfSUCCESSadsfadsf", "SUCCESS" };
        int[] iGroup1 = { 7, 7 };
        string[] strGrpCap1 = { "SUCCESS" };
        int[,] iGrpCap1 = { { 7, 7 } };
        
        // Testing \B special character escape
        // "adfadsfSUCCESSadsfadsf", ".*\\B(SUCCESS)\\B.*"
        Match match = Regex.Match("adfadsfSUCCESSadsfadsf", @".*\B(SUCCESS)\B.*");
        Assert.True(match.Success);
        Assert.Equal(strMatch1, match.Value);
        Assert.Equal(iMatch1[0], match.Index);
        Assert.Equal(iMatch1[1], match.Length);

        Assert.Equal(1, match.Captures.Count);
        Assert.Equal(strMatch1, match.Captures[0].Value);
        Assert.Equal(iMatch1[0], match.Captures[0].Index);
        Assert.Equal(iMatch1[1], match.Captures[0].Length);

        Assert.Equal(2, match.Groups.Count);
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
            Assert.Equal(iGroup1[0], match.Groups[i].Index);
            Assert.Equal(iGroup1[1], match.Groups[i].Length);
            Assert.Equal(1, match.Groups[i].Captures.Count);

            for (int j = 0; j < match.Groups[i].Captures.Count; j++)
            {
                Assert.Equal(strGrpCap1[j], match.Groups[i].Captures[j].Value);
                Assert.Equal(iGrpCap1[j, 0], match.Groups[i].Captures[j].Index);
                Assert.Equal(iGrpCap1[j, 1], match.Groups[i].Captures[j].Length);
            }
        }

        // Testing octal sequence matches
        // "011", "\\060(\\061)?\\061"
        // Octal \061 is ASCII 49
        match = Regex.Match("011", @"\060(\061)?\061");
        Assert.True(match.Success);

        // Testing hexadecimal sequence matches
        // "012", "(\\x30\\x31\\x32)"
        // Hex \x31 is ASCII 49
        match = Regex.Match("012", @"(\x30\x31\x32)");
        Assert.True(match.Success);

        // Testing control character escapes???
        // "2", "(\u0032)"
        match = Regex.Match("4", "(\u0034)");
        Assert.True(match.Success);
    }
}
