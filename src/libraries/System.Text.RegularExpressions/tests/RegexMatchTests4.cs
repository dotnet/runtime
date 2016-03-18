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
        - Noncapturing group: Actual - "(a+)(?:b*)(ccc)", "aaabbbccc"
        - Zero-width positive lookahead assertion: Actual - "abc(?=XXX)\\w+", "abcXXXdef"
        - Zero-width negative lookahead assertion: Actual - "abc(?!XXX)\\w+", "abcXXXdef" - Negative
        - Zero-width positive lookbehind assertion: Actual - "(\\w){6}(?<  =  XXX ) def ", " abcXXXdef "
        - Zero-width  negative  lookbehind  assertion: Actual - " ( \ \ w ) { 6 } ( ? < ! XXX ) def ", " XXXabcdef "
        - Nonbacktracking subexpression: Actual - " [ ^ 0 - 9 ] + ( ?>[0-9]+)3", "abc123"
    */
    [Fact]
    public static void RegexMatchTestCase4()
    {
        string strMatch1 = "aaabbbccc";
        int[] iMatch1 = { 0, 9 };
        string[] strGroup1 = { "aaabbbccc", "aaa", "ccc" };
        int[,] iGroup1 = { { 0, 9 }, { 0, 3 }, { 6, 3 } };
        string[][] strGrpCap1 = new string[3][];
        strGrpCap1[0] = new string[] { "aaabbbccc" };
        strGrpCap1[1] = new string[] { "aaa" };
        strGrpCap1[2] = new string[] { "ccc" };
        int[][] iGrpCap1 = new int[3][];
        iGrpCap1[0] = new int[] { 5, 9 };
        iGrpCap1[1] = new int[] { 0, 3 };
        iGrpCap1[2] = new int[] { 6, 3 };

        string strMatch2 = "abcXXXdef";
        int[] iMatch2 = { 0, 9 };

        // Noncapturing group : Actual - "(a+)(?:b*)(ccc)"
        // "aaabbbccc"
        Regex regex = new Regex("(a+)(?:b*)(ccc)");
        Match match = regex.Match("aaabbbccc");
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

        // Zero-width positive lookahead assertion: Actual - "abc(?=XXX)\\w+"
        // "abcXXXdef"
        regex = new Regex(@"abc(?=XXX)\w+");
        match = regex.Match("abcXXXdef");
        Assert.True(match.Success);
        Assert.Equal(strMatch2, match.Value);
        Assert.Equal(iMatch2[0], match.Index);
        Assert.Equal(iMatch2[1], match.Length);

        Assert.Equal(1, match.Captures.Count);
        Assert.Equal(strMatch2, match.Captures[0].Value);
        Assert.Equal(iMatch2[0], match.Captures[0].Index);
        Assert.Equal(iMatch2[1], match.Captures[0].Length);

        Assert.Equal(1, match.Groups.Count);

        // Group 0 always is the Match
        Assert.Equal(strMatch2, match.Groups[0].Value);
        Assert.Equal(iMatch2[0], match.Groups[0].Index);
        Assert.Equal(iMatch2[1], match.Groups[0].Length);

        Assert.Equal(1, match.Groups[0].Captures.Count);

        // Group 0's Capture is always the Match
        Assert.Equal(strMatch2, match.Groups[0].Captures[0].Value);
        Assert.Equal(iMatch2[0], match.Groups[0].Captures[0].Index);
        Assert.Equal(iMatch2[1], match.Groups[0].Captures[0].Length);

        // Zero-width negative lookahead assertion: Actual - "abc(?!XXX)\\w+"
        // "abcXXXdef" - Negative
        regex = new Regex(@"abc(?!XXX)\w+");
        match = regex.Match("abcXXXdef");
        Assert.False(match.Success);

        // Zero-width positive lookbehind assertion: Actual - "(\\w){6}(?<=XXX)def"
        // "abcXXXdef"
        regex = new Regex(@"(\w){6}(?<=XXX)def");
        match = regex.Match("abcXXXdef");
        Assert.True(match.Success);

        // Zero-width negative lookbehind assertion: Actual - "(\\w){6}(?<!XXX)def"
        // "XXXabcdef"
        regex = new Regex(@"(\w){6}(?<!XXX)def");
        match = regex.Match("XXXabcdef");
        Assert.True(match.Success);

        // Nonbacktracking subexpression: Actual - "[^0-9]+(?>[0-9]+)3"
        // "abc123"
        // The last 3 causes the match to fail, since the non backtracking subexpression does not give up the last digit it matched
        // for it to be a success. For a correct match, remove the last character, '3' from the pattern
        regex = new Regex("[^0-9]+(?>[0-9]+)3");
        match = regex.Match("abc123");
        Assert.False(match.Success);
    }
}
