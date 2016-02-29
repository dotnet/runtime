// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Text.RegularExpressions;
using Xunit;

public class RegexSplitTests
{
    /*
        public static String[] Split(string input); ":", "kkk:lll:mmm:nnn:ooo"
        public static String[] Split(string input, Int32 count); ":", "kkk:lll:mmm:nnn:ooo", 2
        public static String[] Split(string input, Int32 count, Int32 startat); ":", "kkk:lll:mmm:nnn:ooo", 3, 6
    */
    [Fact]
    public static void RegexSplit()
    {
        string[] saExp = new string[] { "kkk", "lll", "mmm", "nnn", "ooo" };
        string[] saExp1 = new string[] { "kkk", "lll:mmm:nnn:ooo" };
        string[] saExp2 = new string[] { "kkk:lll", "mmm", "nnn:ooo" };
        string input = "kkk:lll:mmm:nnn:ooo";
        Regex regex = new Regex(":");

        // Split(string)
        string[] result = regex.Split(input);
        Assert.Equal(saExp, result);

        // A count of 0 means split all
        result = regex.Split(input, 0);
        Assert.Equal(saExp, result);

        // public static String[] Split(string input, Int32 count); ":"
        // "kkk:lll:mmm:nnn:ooo", 2
        result = regex.Split(input, 2);
        Assert.Equal(saExp1, result);

        // public static String[] Split(string input, Int32 count, Int32 startat); ":"
        // "kkk:lll:mmm:nnn:ooo", 3, 6
        result = regex.Split(input, 3, 6);
        Assert.Equal(saExp2, result);
    }
}
