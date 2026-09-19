// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text.RegularExpressions;
using Xunit;

namespace System.Text.RegularExpressions.Examples
{
    public class MatchExamples
    {
        [Fact]
        public static void MatchZipCode()
        {
            #region Match
            string input = "Zip code: 98052";
            var regex = new Regex(@"(?<=Zip code: )\d{5}");
            Match match = regex.Match(input, 5);
            if (match.Success)
                Console.WriteLine($"Match found: {match.Value}");

            // This code prints the following output:
            //
            // Match found: 98052
            #endregion

            Assert.True(match.Success);
            Assert.Equal("98052", match.Value);
        }
    }

    public class RegexConstructorExamples
    {
        [Fact]
        public static void ConstructorWithPattern()
        {
            #region RegexCtorString
            string pattern = @"\b[at]\w+\b";
            string input = "The archive was trimmed and tagged.";
            MatchCollection matches = new Regex(pattern).Matches(input);
            foreach (Match match in matches)
                Console.WriteLine(match.Value);

            // This code prints the following output:
            //
            // archive
            // trimmed
            // and
            // tagged
            #endregion

            Assert.Equal(4, matches.Count);
            Assert.Equal("archive", matches[0].Value);
            Assert.Equal("trimmed", matches[1].Value);
            Assert.Equal("and", matches[2].Value);
            Assert.Equal("tagged", matches[3].Value);
        }

        [Fact]
        public static void ConstructorWithPatternAndOptions()
        {
            #region RegexCtorStringOptions
            string pattern = @"\b[at]\w+\b";
            string input = "The archive was trimmed and tagged.";
            MatchCollection matches = new Regex(pattern, RegexOptions.IgnoreCase).Matches(input);
            foreach (Match match in matches)
                Console.WriteLine(match.Value);

            // This code prints the following output:
            //
            // The
            // archive
            // trimmed
            // and
            // tagged
            #endregion

            Assert.Equal(5, matches.Count);
            Assert.Equal("The", matches[0].Value);
            Assert.Equal("archive", matches[1].Value);
            Assert.Equal("trimmed", matches[2].Value);
            Assert.Equal("and", matches[3].Value);
            Assert.Equal("tagged", matches[4].Value);
        }

        [Fact]
        public static void ConstructorWithPatternOptionsAndMatchTimeout()
        {
            #region RegexCtorStringOptionsMatchTimeout
            string pattern = @"(a+)+$";
            string input = new string('a', 15) + "!";
            TimeSpan timeout = TimeSpan.FromTicks(1);
            bool isMatch;

            try
            {
                isMatch = new Regex(pattern, RegexOptions.None, timeout).IsMatch(input);
            }
            catch (RegexMatchTimeoutException)
            {
                timeout = TimeSpan.FromSeconds(3);
                isMatch = new Regex(pattern, RegexOptions.None, timeout).IsMatch(input);
            }

            Console.WriteLine($"Match found: {isMatch}");

            // This code prints the following output:
            //
            // Match found: False
            #endregion

            Assert.False(isMatch);
        }
    }
}
