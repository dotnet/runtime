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
        - Using [a-z], \s, \w : Actual - "([a-zA-Z]+)\\s(\\w+)", "David Bau"
        - \\S, \\d, \\D, \\W: Actual - "(\\S+):\\W(\\d+)\\s(\\D+)", "Price: 5 dollars"
        - \\S, \\d, \\D, \\W: Actual - "[^0-9]+(\\d+)", "Price: 30 dollars"
    */
    [Fact]
    public static void RegexMatchTestCase2()
    {
        // Using [a-z], \s, \w : Actual - "([a-zA-Z]+)\\s(\\w+)"
        // "David Bau"
        Regex regex = new Regex(@"([a-zA-Z]+)\s(\w+)");
        Match match = regex.Match("David Bau");
        Assert.True(match.Success);

        // \\S, \\d, \\D, \\W: Actual - "(\\S+):\\W(\\d+)\\s(\\D+)"
        // "Price: 5 dollars"
        regex = new Regex(@"(\S+):\W(\d+)\s(\D+)");
        match = regex.Match("Price: 5 dollars");
        Assert.True(match.Success);

        // \\S, \\d, \\D, \\W: Actual - "[^0-9]+(\\d+)"
        // "Price: 30 dollars"
        regex = new Regex(@"[^0-9]+(\d+)");
        match = regex.Match("Price: 30 dollars");
        Assert.True(match.Success);
    }
}
