// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Text.RegularExpressions;
using Xunit;

public class RegexMatchValueTests
{
    [Fact]
    public static void MatchValue()
    {
        string[] arrGroupExp = { "aaabbcccccccccc", "aaa", "bb", "cccccccccc" };
        string[] arrGroupExp1 = { "abracadabra", "abra", "cad" };
        string[] arrCaptureExp = { "aaabbcccccccccc", "aaa", "bb", "cccccccccc" };
        string[] arrCaptureExp1 = { "abracad", "abra" };

        // Test the Value property of Match, Group and Capture here. Value is the more semantic equivalent of
        // the current ToString
        // trim leading and trailing white spaces
        // my answer to the csharp alias, Regex.Replace(strInput, @"\s*(.*?)\s*$", "${1}") works fine, Even Freidl gives it
        // a solution, albeit not a very fast one
        Regex regex = new Regex(@"\s*(.*?)\s*$");
        string input = " Hello World ";
        Match match = regex.Match(input);
        if (match.Success)
        {
            Assert.Equal(input, match.Value);
        }

        // for Groups and Captures
        regex = new Regex(@"(?<A1>a*)(?<A2>b*)(?<A3>c*)");
        input = "aaabbccccccccccaaaabc";
        match = regex.Match(input);
        if (match.Success)
        {
            string expected = "aaabbcccccccccc";
            Assert.Equal(expected, match.Value);

            Assert.Equal(4, match.Groups.Count);
            for (int i = 0; i < match.Groups.Count; i++)
            {
                Assert.Equal(arrGroupExp[i], match.Groups[i].Value);

                Assert.Equal(1, match.Groups[i].Captures.Count);
                Assert.Equal(arrGroupExp[i], match.Groups[i].Captures[0].Value);
            }

            Assert.Equal(1, match.Captures.Count);
            Assert.Equal(expected, match.Captures[0].Value);
        }

        // Another example - given by Brad Merril in an article on RegularExpressions
        regex = new Regex(@"(abra(cad)?)+");
        input = "abracadabra1abracadabra2abracadabra3";
        match = regex.Match(input);
        while (match.Success)
        {
            string expected = "abracadabra";
            Assert.Equal(expected, match.Value);

            Assert.Equal(3, match.Groups.Count);
            for (int i = 0; i < match.Groups.Count; i++)
            {
                Assert.Equal(arrGroupExp1[i], match.Groups[i].Value);
                // Group has a Captures property too
                if (i == 1)
                {
                    Assert.Equal(2, match.Groups[i].Captures.Count);
                    for (int j = 0; j < match.Groups[i].Captures.Count; j++)
                    {
                        Assert.Equal(arrCaptureExp1[j], match.Groups[i].Captures[j].Value);
                    }
                }
                else if (i == 2)
                {
                    expected = "cad";
                    Assert.Equal(1, match.Groups[i].Captures.Count);
                    Assert.Equal(expected, match.Groups[i].Captures[0].Value);
                }
            }
            Assert.Equal(1, match.Captures.Count);
            Assert.Equal("abracadabra", match.Captures[0].Value);
            match = match.NextMatch();
        }
    }
}
