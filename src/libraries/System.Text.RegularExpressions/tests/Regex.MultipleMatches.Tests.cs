// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Text.RegularExpressions.Tests
{
    public partial class RegexMultipleMatchTests
    {
        [Fact]
        public static void MultipleMatches_MultipleCapturingGroups()
        {
            string[] expectedGroupValues = { "abracadabra", "abra", "cad" };
            string[] expectedGroupCaptureValues = { "abracad", "abra" };

            // Another example - given by Brad Merril in an article on RegularExpressions
            Regex regex = new Regex(@"(abra(cad)?)+");
            string input = "abracadabra1abracadabra2abracadabra3";
            Match match = regex.Match(input);
            while (match.Success)
            {
                string expected = "abracadabra";
                Assert.Equal(expected, match.Value);

                Assert.Equal(3, match.Groups.Count);
                for (int i = 0; i < match.Groups.Count; i++)
                {
                    Assert.Equal(expectedGroupValues[i], match.Groups[i].Value);
                    if (i == 1)
                    {
                        Assert.Equal(2, match.Groups[i].Captures.Count);
                        for (int j = 0; j < match.Groups[i].Captures.Count; j++)
                        {
                            Assert.Equal(expectedGroupCaptureValues[j], match.Groups[i].Captures[j].Value);
                        }
                    }
                    else if (i == 2)
                    {
                        Assert.Equal(1, match.Groups[i].Captures.Count);
                        Assert.Equal("cad", match.Groups[i].Captures[0].Value);
                    }
                }
                Assert.Equal(1, match.Captures.Count);
                Assert.Equal("abracadabra", match.Captures[0].Value);
                match = match.NextMatch();
            }
        }

        [Fact]
        public static void MultipleMatches_AlphabeticCharacters()
        {
            // Actual "(abc){2}"
            Regex regex = new Regex("(abc){2}");
            string input = " !abcabcasl  dkfjasiduf 12343214-//asdfjzpiouxoifzuoxpicvql23r\\` #$3245,2345278 :asdfas & 100% @daeeffga (ryyy27343) poiweurwabcabcasdfalksdhfaiuyoiruqwer{234}/[(132387 + x)]'aaa''?";
            for (Match match = regex.Match(input); match.Success; match = match.NextMatch())
            {
                Assert.Equal("abcabc", match.Groups[0].Value);
                Assert.True(match.Groups[0].Index == 2 || match.Groups[0].Index == 125);
            }
        }

        [Fact]
        public static void MultipleMatches_NumericalCharacters()
        {
            // Searching for numeric characters: Actual "[0-9]"
            Regex regex = new Regex("[0-9]");
            string input = "12345asdfasdfasdfljkhsda67890";
            int i = 0;
            int[] expectedIndices = new int[] { 0, 1, 2, 3, 4, 24, 25, 26, 27, 28 };
            for (Match match = regex.Match(input); match.Success; match = match.NextMatch(), i++)
            {
                Assert.True(char.IsDigit(match.Groups[0].Value[0]));
                Assert.Equal(expectedIndices[i], match.Groups[0].Index);
            }
        }

        [Fact]
        public static void MultipleMatches_NumericalCharacters_Symbols()
        {
            // Actual "[a-z0-9]+"
            Regex regex = new Regex("[a-z0-9]+");
            string input = "[token1]? GARBAGEtoken2GARBAGE ;token3!";
            int i = 0;
            string[] expectedValues = new string[] { "token1", "token2", "token3" };
            int[] expectedIndicies = new int[] { 1, 17, 32 };
            for (Match match = regex.Match(input); match.Success; match = match.NextMatch(), i++)
            {
                Assert.Equal(expectedValues[i], match.Groups[0].Value);
                Assert.Equal(expectedIndicies[i], match.Groups[0].Index);
            }
        }
    }
}
