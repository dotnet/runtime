// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Text.RegularExpressions;
using Xunit;

public class R2LGetGroupNamesMatchTests
{
    /*
        public static String[] GetGroupNames(); "(?<first_name>\\S+)\\s(?<last_name>\\S+)"
        public static Match Match(string input); "David Bau"
        public static Boolean IsMatch(string input); //D+, "12321"
    */
    [Fact]
    public static void R2LGetGroupNamesMatch()
    {
        Regex regex = new Regex("(?<first_name>\\S+)\\s(?<last_name>\\S+)");
        Assert.Equal(new string[] { "0", "first_name", "last_name" }, regex.GetGroupNames());

        Match match = regex.Match("David Bau");
        Assert.True(match.Success);
        
        regex = new Regex(@"\D+");
        Assert.False(regex.IsMatch("12321"));
    }
}
