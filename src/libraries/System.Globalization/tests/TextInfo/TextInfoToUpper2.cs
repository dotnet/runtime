// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using Xunit;

namespace System.Globalization.Tests
{
    public class TextInfoToUpper2
    {
        // PosTest1: normal string ToUpper
        [Fact]
        public void TestEnUSNormalString()
        {
            string strA = "HelloWorld!";
            string ActualResult;

            TextInfo textInfo = new CultureInfo("en-US").TextInfo;

            ActualResult = textInfo.ToUpper(strA);
            Assert.Equal("HELLOWORLD!", ActualResult);
        }

        // PosTest2: empty string ToUpper
        [Fact]
        public void TestEmptyString()
        {
            string strA = string.Empty;
            string ActualResult;

            TextInfo textInfo = new CultureInfo("en-US").TextInfo;
            ActualResult = textInfo.ToUpper(strA);
            Assert.Equal(string.Empty, ActualResult);
        }

        // PosTest3: normal string with special symbols '\u0009' and '\0'
        [Fact]
        public void TestEnUSStringWithSymbols()
        {
            string strA = "Hello\n\0World\u0009!";
            string ActualResult;

            TextInfo textInfo = new CultureInfo("en-US").TextInfo;
            ActualResult = textInfo.ToUpper(strA);
            Assert.Equal("HELLO\n\0WORLD\t!", ActualResult);
        }

        // PosTest4: normal string ToUpper and TextInfo is French (France) CultureInfo's
        [Fact]
        public void TestFrFRNormalString()
        {
            string strA = "HelloWorld!";
            string ActualResult;

            TextInfo textInfo = new CultureInfo("fr-FR").TextInfo;

            ActualResult = textInfo.ToUpper(strA);
            Assert.Equal("HELLOWORLD!", ActualResult);
        }

        // PosTest5: normal string with special symbols and TextInfo is French (France) CultureInfo's
        [Fact]
        public void TestFrFRStringWithSymbols()
        {
            string strA = "Hello\n\0World\u0009!";
            string ActualResult;

            TextInfo textInfo = new CultureInfo("fr-FR").TextInfo;
            ActualResult = textInfo.ToUpper(strA);
            Assert.Equal("HELLO\n\0WORLD\t!", ActualResult);
        }

        // PosTest6: normal string ToUpper and TextInfo is Turkish CultureInfo's
        [Fact]
        public void TestTrTRNormalString()
        {
            string strA = "H\u0131!";
            string ActualResult;

            TextInfo textInfo = new CultureInfo("tr-TR").TextInfo;

            ActualResult = textInfo.ToUpper(strA);
            Assert.Equal("HI!", ActualResult);
        }

        // PosTest7: normal string with special symbols and TextInfo is Turkish CultureInfo's
        [Fact]
        public void TestTrTRStringWithSymbols()
        {
            string strA = "H\u0131\n\0Hi\u0009!";
            string ActualResult;

            TextInfo textInfo = new CultureInfo("tr-TR").TextInfo;
            ActualResult = textInfo.ToUpper(strA);
            Assert.Equal("HI\n\0H\u0130\t!", ActualResult);
        }

        // NegTest1: The string is a null reference
        [Fact]
        public void TestEnUSNullString()
        {
            TextInfo textInfoUS = new CultureInfo("en-US").TextInfo;
            string str = null;
            Assert.Throws<ArgumentNullException>(() =>
            {
                textInfoUS.ToUpper(str);
            });
        }

        // NegTest2: The string is a null reference and TextInfo is French (France) CultureInfo's
        [Fact]
        public void TestFrFRNullString()
        {
            TextInfo textInfoUS = new CultureInfo("fr-FR").TextInfo;
            string str = null;

            Assert.Throws<ArgumentNullException>(() =>
            {
                textInfoUS.ToUpper(str);
            });
        }

        // NegTest3: The string is a null reference and TextInfo is Turkish CultureInfo's
        [Fact]
        public void TestTrTRNullString()
        {
            TextInfo textInfoUS = new CultureInfo("tr-TR").TextInfo;
            string str = null;

            Assert.Throws<ArgumentNullException>(() =>
            {
                textInfoUS.ToUpper(str);
            });
        }
    }
}

