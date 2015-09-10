// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
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

        // PosTest2: Call GetEra when DateTimeFormatInfo from en-us and fr-FR cultures
        [Theory]
        [InlineData("en-us")]
        [InlineData("fr-FR")]
        public void PosTest2(string localeName)
        {
            CultureInfo cultureInfo = new CultureInfo(localeName);
            DateTimeFormatInfo info = cultureInfo.DateTimeFormat;
            string eraName = DateTimeFormatInfoData.GetEraName(cultureInfo);

            VerificationHelper(info, 1, eraName);
            VerificationHelper(info, 0, eraName);
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
