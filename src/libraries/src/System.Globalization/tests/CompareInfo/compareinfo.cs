// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using Xunit;

namespace System.Globalization.Tests
{
    public class CompareInfoTest
    {
        [Fact]
        [ActiveIssue(846, PlatformID.AnyUnix)]
        public void TestCompareInfo()
        {
            CompareInfo ciENG = CompareInfo.GetCompareInfo("en-US");
            CompareInfo ciFR = CompareInfo.GetCompareInfo("fr-FR");

            Assert.True(ciENG.Name.Equals("en-US", StringComparison.CurrentCultureIgnoreCase));
            Assert.NotEqual(ciENG.GetHashCode(), ciFR.GetHashCode());
            Assert.NotEqual(ciENG, ciFR);
        }

        [Fact]
        [ActiveIssue(846, PlatformID.AnyUnix)]
        public void CompareInfoIndexTest1()
        {
            // Creates CompareInfo for the InvariantCulture.
            CompareInfo myComp = CultureInfo.InvariantCulture.CompareInfo;

            // iS is the starting index of the substring. 
            int iS = 8;
            // iL is the length of the substring. 
            int iL = 18;

            // Searches for the ligature Æ.
            String myStr = "Is AE or ae the same as \u00C6 or \u00E6?";

            Assert.Equal(myComp.IndexOf(myStr, "AE", iS, iL), 24);
            Assert.Equal(myComp.LastIndexOf(myStr, "AE", iS + iL - 1, iL), 24);

            Assert.Equal(myComp.IndexOf(myStr, "ae", iS, iL), 9);
            Assert.Equal(myComp.LastIndexOf(myStr, "ae", iS + iL - 1, iL), 9);

            Assert.Equal(myComp.IndexOf(myStr, '\u00C6', iS, iL), 24);
            Assert.Equal(myComp.LastIndexOf(myStr, '\u00C6', iS + iL - 1, iL), 24);

            Assert.Equal(myComp.IndexOf(myStr, '\u00E6', iS, iL), 9);
            Assert.Equal(myComp.LastIndexOf(myStr, '\u00E6', iS + iL - 1, iL), 9);

            Assert.Equal(myComp.IndexOf(myStr, "AE", iS, iL, CompareOptions.Ordinal), -1);
            Assert.Equal(myComp.LastIndexOf(myStr, "AE", iS + iL - 1, iL, CompareOptions.Ordinal), -1);

            Assert.Equal(myComp.IndexOf(myStr, "ae", iS, iL, CompareOptions.Ordinal), 9);
            Assert.Equal(myComp.LastIndexOf(myStr, "ae", iS + iL - 1, iL, CompareOptions.Ordinal), 9);

            Assert.Equal(myComp.IndexOf(myStr, '\u00C6', iS, iL, CompareOptions.Ordinal), 24);
            Assert.Equal(myComp.LastIndexOf(myStr, '\u00C6', iS + iL - 1, iL, CompareOptions.Ordinal), 24);

            Assert.Equal(myComp.IndexOf(myStr, '\u00E6', iS, iL, CompareOptions.Ordinal), -1);
            Assert.Equal(myComp.LastIndexOf(myStr, '\u00E6', iS + iL - 1, iL, CompareOptions.Ordinal), -1);

            Assert.Equal(myComp.IndexOf(myStr, "AE", iS, iL, CompareOptions.IgnoreCase), 9);
            Assert.Equal(myComp.LastIndexOf(myStr, "AE", iS + iL - 1, iL, CompareOptions.IgnoreCase), 24);

            Assert.Equal(myComp.IndexOf(myStr, "ae", iS, iL, CompareOptions.IgnoreCase), 9);
            Assert.Equal(myComp.LastIndexOf(myStr, "ae", iS + iL - 1, iL, CompareOptions.IgnoreCase), 24);

            Assert.Equal(myComp.IndexOf(myStr, '\u00C6', iS, iL, CompareOptions.IgnoreCase), 9);
            Assert.Equal(myComp.LastIndexOf(myStr, '\u00C6', iS + iL - 1, iL, CompareOptions.IgnoreCase), 24);

            Assert.Equal(myComp.IndexOf(myStr, '\u00E6', iS, iL, CompareOptions.IgnoreCase), 9);
            Assert.Equal(myComp.LastIndexOf(myStr, '\u00E6', iS + iL - 1, iL, CompareOptions.IgnoreCase), 24);
        }

        [Fact]
        [ActiveIssue(846, PlatformID.AnyUnix)]
        public void CompareInfoIndexTest2()
        {
            // Creates CompareInfo for the InvariantCulture.
            CompareInfo myComp = CultureInfo.InvariantCulture.CompareInfo;

            // iS is the starting index of the substring. 
            int iS = 8;
            // iL is the length of the substring. 
            int iL = 18;

            // Searches for the combining character sequence Latin capital letter U with diaeresis or Latin small letter u with diaeresis.
            string myStr = "Is \u0055\u0308 or \u0075\u0308 the same as \u00DC or \u00FC?";

            Assert.Equal(myComp.IndexOf(myStr, "U\u0308", iS, iL), 24);
            Assert.Equal(myComp.LastIndexOf(myStr, "U\u0308", iS + iL - 1, iL), 24);

            Assert.Equal(myComp.IndexOf(myStr, "u\u0308", iS, iL), 9);
            Assert.Equal(myComp.LastIndexOf(myStr, "u\u0308", iS + iL - 1, iL), 9);

            Assert.Equal(myComp.IndexOf(myStr, '\u00DC', iS, iL), 24);
            Assert.Equal(myComp.LastIndexOf(myStr, '\u00DC', iS + iL - 1, iL), 24);

            Assert.Equal(myComp.IndexOf(myStr, '\u00FC', iS, iL), 9);
            Assert.Equal(myComp.LastIndexOf(myStr, '\u00FC', iS + iL - 1, iL), 9);

            Assert.Equal(myComp.IndexOf(myStr, "U\u0308", iS, iL, CompareOptions.Ordinal), -1);
            Assert.Equal(myComp.LastIndexOf(myStr, "U\u0308", iS + iL - 1, iL, CompareOptions.Ordinal), -1);

            Assert.Equal(myComp.IndexOf(myStr, "u\u0308", iS, iL, CompareOptions.Ordinal), 9);
            Assert.Equal(myComp.LastIndexOf(myStr, "u\u0308", iS + iL - 1, iL, CompareOptions.Ordinal), 9);

            Assert.Equal(myComp.IndexOf(myStr, '\u00DC', iS, iL, CompareOptions.Ordinal), 24);
            Assert.Equal(myComp.LastIndexOf(myStr, '\u00DC', iS + iL - 1, iL, CompareOptions.Ordinal), 24);

            Assert.Equal(myComp.IndexOf(myStr, '\u00FC', iS, iL, CompareOptions.Ordinal), -1);
            Assert.Equal(myComp.LastIndexOf(myStr, '\u00FC', iS + iL - 1, iL, CompareOptions.Ordinal), -1);

            Assert.Equal(myComp.IndexOf(myStr, "U\u0308", iS, iL, CompareOptions.IgnoreCase), 9);
            Assert.Equal(myComp.LastIndexOf(myStr, "U\u0308", iS + iL - 1, iL, CompareOptions.IgnoreCase), 24);

            Assert.Equal(myComp.IndexOf(myStr, "u\u0308", iS, iL, CompareOptions.IgnoreCase), 9);
            Assert.Equal(myComp.LastIndexOf(myStr, "u\u0308", iS + iL - 1, iL, CompareOptions.IgnoreCase), 24);

            Assert.Equal(myComp.IndexOf(myStr, '\u00DC', iS, iL, CompareOptions.IgnoreCase), 9);
            Assert.Equal(myComp.LastIndexOf(myStr, '\u00DC', iS + iL - 1, iL, CompareOptions.IgnoreCase), 24);

            Assert.Equal(myComp.IndexOf(myStr, '\u00FC', iS, iL, CompareOptions.IgnoreCase), 9);
            Assert.Equal(myComp.LastIndexOf(myStr, '\u00FC', iS + iL - 1, iL, CompareOptions.IgnoreCase), 24);
        }

        [Theory]
        [InlineData("de-DE", "Ü", "UE", -1)]
        [InlineData("de-DE_phoneb", "Ü", "UE", 0)]
        [ActiveIssue(846, PlatformID.AnyUnix)]
        public void TestLocaleAlternateSortOrder(string locale, string string1, string string2, int expected)
        {
            CultureInfo myTestCulture = new CultureInfo(locale);
            CompareInfo ci = myTestCulture.CompareInfo;
            int actual = ci.Compare(string1, string2);
            Assert.Equal(expected, actual);
        }
    }
}