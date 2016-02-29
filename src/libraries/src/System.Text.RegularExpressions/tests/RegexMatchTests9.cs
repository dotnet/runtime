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
        - Turning on case insensitive option in mid-pattern : Actual - "aaa(?i:match this)bbb", "aaaMaTcH ThIsbbb"
        - Turning off case insensitive option in mid-pattern : Actual - "aaa(?-i:match this)bbb", "i", "AaAmatch thisBBb"
        - Turning on/off all the options at once : Actual - "aaa(?imnsx-imnsx:match this)bbb", "i", "AaAmatch thisBBb"
        - Actual - "aaa(?#ignore this completely)bbb", "aaabbb"
    */
    [Fact]
    public static void RegexMatchTestCase9()
    {
        // Turning on case insensitive option in mid-pattern : Actual - "aaa(?i:match this)bbb"
        // "aaaMaTcH ThIsbbb"
        Regex regex;
        Match match;
        if ("i".ToUpper() == "I")
        {
            regex = new Regex("aaa(?i:match this)bbb");
            match = regex.Match("aaaMaTcH ThIsbbb");
            Assert.True(match.Success);
        }

        // Turning off case insensitive option in mid-pattern : Actual - "aaa(?-i:match this)bbb", "i"
        // "AaAmatch thisBBb"
        regex = new Regex("aaa(?-i:match this)bbb", RegexOptions.IgnoreCase);
        match = regex.Match("AaAmatch thisBBb");
        Assert.True(match.Success);

        // Turning on/off all the options at once : Actual - "aaa(?imnsx-imnsx:match this)bbb", "i"
        // "AaAmatch thisBBb"
        regex = new Regex("aaa(?-i:match this)bbb", RegexOptions.IgnoreCase);
        match = regex.Match("AaAmatcH thisBBb");
        Assert.False(match.Success);

        // Actual - "aaa(?#ignore this completely)bbb"
        // "aaabbb"
        regex = new Regex("aaa(?#ignore this completely)bbb");
        match = regex.Match("aaabbb");
        Assert.True(match.Success);
    }
}
