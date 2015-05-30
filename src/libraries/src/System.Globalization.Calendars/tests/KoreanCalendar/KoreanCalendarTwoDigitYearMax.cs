// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using Xunit;

namespace System.Globalization.CalendarsTests
{
    //System.Globalization.KoreanCalendar.TwoDigitYearMax
    public class KoreanCalendarTwoDigitYearMax
    {
        #region Test Logic
        // PosTest1:get the property
        [Fact]
        public void PosTest1()
        {
            System.Globalization.Calendar kC = new KoreanCalendar();
            int expectedValue = 4362;
            int actualValue;
            actualValue = kC.TwoDigitYearMax;
            Assert.Equal(expectedValue, actualValue);
        }
        #endregion
    }
}