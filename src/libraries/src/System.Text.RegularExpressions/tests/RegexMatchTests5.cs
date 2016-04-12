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
        - Using beginning/end of string chars \A, \Z, ^ : Actual - "\\Aaaa\\w+zzz\\Z", "aaaasdfajsdlfjzzz"
        - Using beginning/end of string chars \A, \Z, ^ : Actual - "\\Aaaa\\w+zzz\\Z", "aaaasdfajsdlfjzzza"

    public Match Match(string input, int start);
        - Using beginning/end of string chars \A, \Z, ^ : Actual - "\\Gbbb", "aaabbb", 3
        - Using beginning/end of string chars \A, \Z, ^ : Actual - "^b", "ab", 1
    */
    [Fact]
    public static void RegexMatchTestCase5()
    {
        // Using beginning/end of string chars \A, \Z : Actual - "\\Aaaa\\w+zzz\\Z"
        // "aaaasdfajsdlfjzzz"
        Regex regex = new Regex(@"\Aaaa\w+zzz\Z");
        Match match = regex.Match("aaaasdfajsdlfjzzz");
        Assert.True(match.Success);

        // Using beginning/end of string chars \A, \Z : Actual - "\\Aaaa\\w+zzz\\Z"
        // "aaaasdfajsdlfjzzza"
        regex = new Regex(@"\Aaaa\w+zzz\Z");
        match = regex.Match("aaaasdfajsdlfjzzza");
        Assert.False(match.Success);

        // Using beginning/end of string chars \A, \Z : Actual - "\\Aaaa\\w+zzz\\Z"
        // "line2\nline3\n"
        regex = new Regex(@"\A(line2\n)line3\Z", RegexOptions.Multiline);
        match = regex.Match("line2\nline3\n");
        Assert.True(match.Success);

        // Using beginning/end of string chars ^ : Actual - "^b"
        // "ab", 1
        regex = new Regex("^b");
        match = regex.Match("ab");
        Assert.False(match.Success);
    }
}
