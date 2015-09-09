// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using Xunit;

namespace System.Globalization.Tests
{
    public class DateTimeFormatInfoGetAbbreviatedEraName
    {
        // PosTest1: Call GetAbbreviatedEraName to get Era's abbreviated name
        [Fact]
        public void PosTest1()
        {
            DateTimeFormatInfo info = new CultureInfo("en-us").DateTimeFormat;

            bool isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            string abbreviatedEraName = isWindows ? "AD" : "A";

            VerificationHelper(info, 0, abbreviatedEraName);
            VerificationHelper(info, 1, abbreviatedEraName);
        }

        // PosTest2: Call GetAbbreviatedEraName to get Era's abbreviated name on instance created from ctor
        [Fact]
        public void PosTest2()
        {
            DateTimeFormatInfo info = new DateTimeFormatInfo();

            VerificationHelper(info, 0, "AD");
            VerificationHelper(info, 1, "AD");
        }

        // PosTest3: Call GetAbbreviatedEraName to get Era's abbreviated name on ja-JP culture
        [Fact]
        [ActiveIssue(846, PlatformID.AnyUnix)] 
        public void PosTest3()
        {
            DateTimeFormatInfo info = new CultureInfo("ja-JP").DateTimeFormat;
            //For Windows<Win7 and others, the default calendar is Gregorian Calendar, AD is expected to be the Era Name
            String expectedEraName = "\u897F\u66A6";
            VerificationHelper(info, 1, expectedEraName);
        }

        // NegTest1: ArgumentOutOfRangeException should be thrown when era does not represent a valid era in the calendar specified in the Calendar property
        [Fact]
        public void TestInvalidEra()
        {
            DateTimeFormatInfo info = new CultureInfo("en-us").DateTimeFormat;

            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                info.GetAbbreviatedEraName(-1);
            });
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                info.GetAbbreviatedEraName(2);
            });
        }

        private void VerificationHelper(DateTimeFormatInfo info, int era, string expected)
        {
            string actual = info.GetAbbreviatedEraName(era);
            Assert.Equal(expected, actual);
        }
    }
}
