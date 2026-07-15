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
}
