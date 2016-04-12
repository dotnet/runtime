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
        - First testing pattern "abcabc" : Actual "(abc){2}"
            " !abcabcasl  dkfjasiduf 12343214-//asdfjzpiouxoifzuoxpicvql23r\\` #$3245,2345278 :asdfas &  100% @daeeffga (ryyy27343) poiweurwabcabcasdfalksdhfaiuyoiruqwer{234}/[(132387 + x)]'aaa''?"

        - Searching for numeric characters : Actual "[0-9]", "12345asdfasdfasdfljkhsda67890"
        - Different pattern specification. This time range of symbols is allowed. : Actual "[a-z0-9]+", "[token1]? GARBAGEtoken2GARBAGE ;token3!"
        - Trying empty string: Actual "[a-z0-9]+", ""
    */
    [Fact]
    public static void MatchTestCase10()
    {
        Match match;
        int[] iaExp1 = { 0, 1, 2, 3, 4, 24, 25, 26, 27, 28 };
        string[] saExp2 = { "token1", "token2", "token3" };
        int[] iaExp2 = { 1, 17, 32 };
        
        // First testing pattern "abcabc": Actual "(abc){2}"
        // " !abcabcasl  dkfjasiduf 12343214-//asdfjzpiouxoifzuoxpicvql23r\\` #$3245,2345278 :asdfas & 100% @daeeffga (ryyy27343) poiweurwabcabcasdfalksdhfaiuyoiruqwer{234}/[(132387 + x)]'aaa''?"
        Regex regex = new Regex("(abc){2}");
        string s = " !abcabcasl  dkfjasiduf 12343214-//asdfjzpiouxoifzuoxpicvql23r\\` #$3245,2345278 :asdfas & 100% @daeeffga (ryyy27343) poiweurwabcabcasdfalksdhfaiuyoiruqwer{234}/[(132387 + x)]'aaa''?";
        for (match = regex.Match(s); match.Success; match = match.NextMatch())
        {
            Assert.Equal("abcabc", match.Groups[0].Value);
            Assert.True(match.Groups[0].Index == 2 || match.Groups[0].Index == 125);
        }

        // Searching for numeric characters: Actual "[0-9]"
        // "12345asdfasdfasdfljkhsda67890"
        regex = new Regex("[0-9]");
        s = "12345asdfasdfasdfljkhsda67890";
        int i = 0;
        for (match = regex.Match(s); match.Success; match = match.NextMatch(), i++)
        {
            Assert.True(char.IsDigit(match.Groups[0].Value[0]));
            Assert.Equal(iaExp1[i], match.Groups[0].Index);
        }

        // Different pattern specification. This time range of symbols is allowed: Actual "[a-z0-9]+"
        // "[token1]? GARBAGEtoken2GARBAGE ;token3!"
        regex = new Regex("[a-z0-9]+");
        s = "[token1]? GARBAGEtoken2GARBAGE ;token3!";
        i = 0;
        for (match = regex.Match(s); match.Success; match = match.NextMatch(), i++)
        {
            Assert.Equal(saExp2[i], match.Groups[0].Value);
            Assert.Equal(iaExp2[i], match.Groups[0].Index);
        }

        // Trying empty string: Actual "[a-z0-9]+", ""
        regex = new Regex("[a-z0-9]+");
        match = regex.Match("");
        Assert.False(match.Success);
    }
}
