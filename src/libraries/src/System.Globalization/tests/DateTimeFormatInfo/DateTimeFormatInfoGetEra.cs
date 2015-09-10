// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using Xunit;

namespace System.Globalization.Tests
{
    public class DateTimeFormatInfoGetEra
    {
        // PosTest1: Call GetEra when DateTimeFormatInfo instance's calendar is Gregorian calendar
        [Fact]
        public void TestGregorian()
        {
            DateTimeFormatInfo info = CultureInfo.InvariantCulture.DateTimeFormat;
            VerificationHelper(info, "A.D.", 1);
            VerificationHelper(info, "AD", 1);
            VerificationHelper(info, "a.d.", 1);
            VerificationHelper(info, "ad", 1);
            VerificationHelper(info, "C.E.", -1);
            VerificationHelper(info, "CE", -1);
            VerificationHelper(info, "B.C.", -1);
            VerificationHelper(info, "BC", -1);
            VerificationHelper(info, "", -1);
        }

        // PosTest2: Call GetEra when DateTimeFormatInfo from en-us culture
        [Fact]
        public void TestEnUS()
        {
            CultureInfo cultureInfo = new CultureInfo("en-us");
            DateTimeFormatInfo info = cultureInfo.DateTimeFormat;

            string eraName = DateTimeFormatInfoData.GetEraName(cultureInfo);
            string lowerEraName = eraName.ToLower();
            string abbrevEraName = DateTimeFormatInfoData.GetAbbreviatedEraName(cultureInfo);
            string lowerAbbrevEraName = abbrevEraName.ToLower();

            VerificationHelper(info, eraName, 1);
            VerificationHelper(info, lowerEraName, 1);
            VerificationHelper(info, abbrevEraName, 1);
            VerificationHelper(info, lowerAbbrevEraName, 1);

            VerificationHelper(info, "C.E.", -1);
            VerificationHelper(info, "CE", -1);
            VerificationHelper(info, "B.C.", -1);
            VerificationHelper(info, "BC", -1);
            VerificationHelper(info, "", -1);
        }

        // PosTest3: Call GetEra when DateTimeFormatInfo created from fr-FR
        [Fact]
        public void TestFrFR()
        {
            DateTimeFormatInfo info = new CultureInfo("fr-FR").DateTimeFormat;

            // For Win7, "fr-FR" is "ap J.-C".
            // for windows<Win7 & MAC, every culture is "A.D."
            int expectedRetValue = -1;
            VerificationHelper(info, "A.D.", expectedRetValue);
            VerificationHelper(info, "AD", expectedRetValue);
            VerificationHelper(info, "a.d.", expectedRetValue);
            VerificationHelper(info, "ad", expectedRetValue);

            VerificationHelper(info, "C.E.", -1);
            VerificationHelper(info, "CE", -1);
            VerificationHelper(info, "B.C.", -1);
            VerificationHelper(info, "BC", -1);
            VerificationHelper(info, "ap. J.-C.", -expectedRetValue);
            VerificationHelper(info, "AP. J.-C.", -expectedRetValue);
            VerificationHelper(info, "ap. j.-c.", -expectedRetValue);
            VerificationHelper(info, "ap J-C", -1);
        }

        // NegTest1: ArgumentNullException should be thrown when eraName is a null reference
        [Fact]
        public void TestNull()
        {
            Assert.Throws<ArgumentNullException>(() =>
            {
                new DateTimeFormatInfo().GetEra(null);
            });
        }

        private void VerificationHelper(DateTimeFormatInfo info, string eraName, int expected)
        {
            int actual = info.GetEra(eraName);
            Assert.Equal(expected, actual);
        }
    }
}
