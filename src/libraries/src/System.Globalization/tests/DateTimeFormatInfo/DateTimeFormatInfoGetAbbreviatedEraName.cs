// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
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
            CultureInfo cultureInfo = new CultureInfo("en-us");
            DateTimeFormatInfo info = cultureInfo.DateTimeFormat;
            string abbreviatedEraName = DateTimeFormatInfoData.GetAbbreviatedEraName(cultureInfo);

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
        public void PosTest3()
        {
            CultureInfo cultureInfo = new CultureInfo("ja-JP");
            DateTimeFormatInfo info = cultureInfo.DateTimeFormat;
            string expectedEraName = DateTimeFormatInfoData.GetAbbreviatedEraName(cultureInfo);

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
