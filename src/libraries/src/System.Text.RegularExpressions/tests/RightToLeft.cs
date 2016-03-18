// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Text.RegularExpressions;
using Xunit;

public class RightToLeft
{
    // This test case was added to improve code coverage on methods that have a different code path with the RightToLeft option
    [Fact]
    public static void RightToLeftTests()
    {
        // IsMatch(string) with RightToLeft
        Regex regex = new Regex(@"\s+\d+", RegexOptions.RightToLeft);
        string input = "sdf 12sad";
        Assert.True(regex.IsMatch(input));

        input = " asdf12 ";
        Assert.False(regex.IsMatch(input));

        // Match(string, int, int) with RightToLeft
        regex = new Regex(@"foo\d+", RegexOptions.RightToLeft);
        input = "0123456789foo4567890foo         ";
        Match match = regex.Match(input, 0, input.Length);
        Assert.True(match.Success);
        Assert.Equal("foo4567890", match.Value);

        match = regex.Match(input, 10, input.Length - 10);
        Assert.True(match.Success);
        Assert.Equal("foo4567890", match.Value);

        match = regex.Match(input, 10, 4);
        Assert.True(match.Success);
        Assert.Equal("foo4", match.Value);

        match = regex.Match(input, 10, 3);
        Assert.False(match.Success);

        match = regex.Match(input, 11, input.Length - 11);
        Assert.False(match.Success);

        // Matches(string) with RightToLeft
        regex = new Regex(@"foo\d+", RegexOptions.RightToLeft);
        input = "0123456789foo4567890foo1foo  0987";

        MatchCollection collection = regex.Matches(input);
        Assert.Equal(2, collection.Count);
        Assert.Equal("foo1", collection[0].Value);
        Assert.Equal("foo4567890", collection[1].Value);

        // Replace(string, string) with RightToLeft
        regex = new Regex(@"foo\s+", RegexOptions.RightToLeft);
        input = "0123456789foo4567890foo         ";
        string replace = "bar";
        string replaceResult = regex.Replace(input, replace);
        Assert.Equal("0123456789foo4567890bar", replaceResult);

        // Replace(string, string, int count) with RightToLeft
        regex = new Regex(@"\d", RegexOptions.RightToLeft);
        input = "0123456789foo4567890foo         ";
        replace = "#";
        replaceResult = regex.Replace(input, replace, 17);
        Assert.Equal("##########foo#######foo         ", replaceResult);

        replaceResult = regex.Replace(input, replace, 7);
        Assert.Equal("0123456789foo#######foo         ", replaceResult);

        replaceResult = regex.Replace(input, replace, 0);
        Assert.Equal("0123456789foo4567890foo         ", replaceResult);

        replaceResult = regex.Replace(input, replace, -1);
        Assert.Equal("##########foo#######foo         ", replaceResult);

        // Replace(string, MatchEvaluator) with RightToLeft
        regex = new Regex(@"foo\s+", RegexOptions.RightToLeft);
        input = "0123456789foo4567890foo         ";
        replace = "bar";
        replaceResult = regex.Replace(input, new MatchEvaluator(MyBarMatchEvaluator));
        Assert.Equal("0123456789foo4567890bar", replaceResult);

        // Replace(string, string, int count) with RightToLeft
        regex = new Regex(@"\d", RegexOptions.RightToLeft);
        input = "0123456789foo4567890foo         ";
        replaceResult = regex.Replace(input, new MatchEvaluator(MyPoundMatchEvaluator), 17);
        Assert.Equal("##########foo#######foo         ", replaceResult);

        replaceResult = regex.Replace(input, new MatchEvaluator(MyPoundMatchEvaluator), 7);
        Assert.Equal("0123456789foo#######foo         ", replaceResult);

        replaceResult = regex.Replace(input, new MatchEvaluator(MyPoundMatchEvaluator), 0);
        Assert.Equal("0123456789foo4567890foo         ", replaceResult);

        replaceResult = regex.Replace(input, new MatchEvaluator(MyPoundMatchEvaluator), -1);
        Assert.Equal("##########foo#######foo         ", replaceResult);

        // Split(string) with RightToLeft
        regex = new Regex("foo", RegexOptions.RightToLeft);
        input = "0123456789foo4567890foo         ";
        string[] splitResult = regex.Split(input);
        Assert.Equal(new string[] { "0123456789", "4567890", "         " }, splitResult);

        // Split(string, int) with RightToLeft
        regex = new Regex(@"\d", RegexOptions.RightToLeft);
        input = "1a2b3c4d5e6f7g8h9i0k";
        splitResult = regex.Split(input, 11);
        Assert.Equal(new string[] { "", "a", "b", "c", "d", "e", "f", "g", "h", "i", "k" }, splitResult);

        splitResult = regex.Split(input, 10);
        Assert.Equal(new string[] { "1a", "b", "c", "d", "e", "f", "g", "h", "i", "k" }, splitResult);

        splitResult = regex.Split(input, 2);
        Assert.Equal(new string[] { "1a2b3c4d5e6f7g8h9i", "k" }, splitResult);

        splitResult = regex.Split(input, 1);
        Assert.Equal(new string[] { "1a2b3c4d5e6f7g8h9i0k" }, splitResult);
    }

    private static string MyBarMatchEvaluator(Match match) => "bar";
    private static string MyPoundMatchEvaluator(Match match) => "#";
}
