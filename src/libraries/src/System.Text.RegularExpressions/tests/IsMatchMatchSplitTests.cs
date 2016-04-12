// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Text.RegularExpressions;
using Xunit;

public class IsMatchMatchSplitTests
{
    [Theory]
    [InlineData("abc", "abc", RegexOptions.None, true)]
    [InlineData("abc", "aBc", RegexOptions.None, false)]
    [InlineData("abc", "abc", RegexOptions.IgnoreCase, true)]
    public static void IsMatch(string input, string pattern, RegexOptions options, bool expected)
    {
        if (options == RegexOptions.None)
        {
            Assert.Equal(expected, Regex.IsMatch(input, pattern, options));
        }
        Assert.Equal(expected, Regex.IsMatch(input, pattern, options));
    }

    /*
        public static Match Match(string input, String pattern, String options); "abc","[aBc]", 3"i"
        public static String[] Split(string input, String pattern, String options); "[abc]", "i", "1A2B3C4"
    */
    [Fact]
    public static void Split()
    {
        string[] saExp1 = { "a", "b", "c" };
        string[] saExp2 = { "1", "2", "3", "4" };
                
        string s = "1A2B3C4";
        string[] sa = Regex.Split(s, "[abc]", RegexOptions.IgnoreCase);
        Assert.Equal(sa, saExp2);
    }
}
