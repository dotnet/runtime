// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using Xunit;

namespace System.Globalization.Tests
{
    public class TextInfoToLower
    {
        // PosTest1: uppercase character
        [Fact]
        public void TestEnUSUppercaseCharacter()
        {
            char ch = 'A';
            char expectedChar = 'a';
            TextInfo textInfo = new CultureInfo("en-US").TextInfo;

            char actualChar = textInfo.ToLower(ch);
            Assert.Equal(expectedChar, actualChar);
        }

        // PosTest2: lowercase character
        [Fact]
        public void TestEnUSLowercaseCharacter()
        {
            char ch = 'a';
            char expectedChar = ch;
            TextInfo textInfo = new CultureInfo("en-US").TextInfo;

            char actualChar = textInfo.ToLower(ch);
            Assert.Equal(expectedChar, actualChar);
        }

        // PosTest3: non-alphabetic character
        [Fact]
        public void TestNonAlphabeticCharacter()
        {
            for (int i = 0; i <= 9; i++)
            {
                char ch = Convert.ToChar(i);
                char expectedChar = ch;
                TextInfo textInfo = new CultureInfo("en-US").TextInfo;

                char actualChar = textInfo.ToLower(ch);
                Assert.Equal(expectedChar, actualChar);
            }
        }

        // PosTest4: uppercase character and TextInfo is french CultureInfo's
        [Fact]
        public void TestFrFRUpperCaseCharacter()
        {
            char ch = 'G';
            char expectedChar = 'g';
            TextInfo textInfo = new CultureInfo("fr-FR").TextInfo;
            char actualChar = textInfo.ToLower(ch);
            Assert.Equal(expectedChar, actualChar);
        }

        // PosTest5: lowercase character and TextInfo is french(France) CultureInfo's
        [Fact]
        public void TestFrFRLowerCaseCharacter()
        {
            char ch = 'g';
            char expectedChar = ch;
            TextInfo textInfo = new CultureInfo("fr-FR").TextInfo;

            char actualChar = textInfo.ToLower(ch);
            Assert.Equal(expectedChar, actualChar);
        }

        // PosTest6: uppercase character for Turkish Culture
        [Fact]
        public void TestTrTRUppercaseCharacter()
        {
            char ch = '\u0130';
            char expectedChar = 'i';
            TextInfo textInfo = new CultureInfo("tr-TR").TextInfo;

            char actualChar = textInfo.ToLower(ch);
            Assert.Equal(expectedChar, actualChar);

            ch = 'I';
            expectedChar = '\u0131';
            actualChar = textInfo.ToLower(ch);
            Assert.Equal(expectedChar, actualChar);
        }

        // PosTest7: lowercase character for Turkish Culture
        [Fact]
        public void TestTrTRLowercaseCharacter()
        {
            char ch = 'i';
            char expectedChar = ch;
            TextInfo textInfo = new CultureInfo("tr-TR").TextInfo;

            char actualChar = textInfo.ToLower(ch);
            Assert.Equal(expectedChar, actualChar);
        }
    }
}

