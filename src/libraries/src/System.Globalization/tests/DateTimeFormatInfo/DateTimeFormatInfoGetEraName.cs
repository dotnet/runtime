// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using System.Runtime.InteropServices;
using Xunit;

namespace System.Globalization.Tests
{
    public class DateTimeFormatInfoGetEraName
    {
        // PosTest1: Call GetEra when DateTimeFormatInfo instance's calendar is Gregorian calendar
        [Fact]
        public void PosTest1()
        {
            DateTimeFormatInfo info = CultureInfo.InvariantCulture.DateTimeFormat;

            VerificationHelper(info, 1, "A.D.");
            VerificationHelper(info, 0, "A.D.");
        }

        // PosTest2: Call GetEra when DateTimeFormatInfo from en-us culture
        [Fact]
        public void PosTest2()
        {
            DateTimeFormatInfo info = new CultureInfo("en-us").DateTimeFormat;

            bool isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            string eraName = isWindows ? "A.D." : "AD";

            VerificationHelper(info, 1, eraName);
            VerificationHelper(info, 0, eraName);
        }

        // PosTest3: Call GetEra when DateTimeFormatInfo created from fr-FR
        [Fact]
        [ActiveIssue(846, PlatformID.AnyUnix)] 
        public void PosTest3()
        {
            DateTimeFormatInfo info = new CultureInfo("fr-FR").DateTimeFormat;

            // For Win7, "fr-FR" is "ap J.-C".
            // for windows<Win7 & MAC, every culture is "A.D."
            String expectedRetValue = "ap. J.-C.";

            VerificationHelper(info, 1, expectedRetValue);
            VerificationHelper(info, 0, expectedRetValue);
        }


        // NegTest1: ArgumentOutOfRangeException should be thrown when era does not represent a valid era in the calendar specified in the Calendar property
        [Fact]
        public void TestInvalidEra()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                new DateTimeFormatInfo().GetEraName(-1);
            });
        }

        private void VerificationHelper(DateTimeFormatInfo info, int era, string expected)
        {
            string actual = info.GetEraName(era);
            Assert.Equal(expected, actual);
        }
    }
}
