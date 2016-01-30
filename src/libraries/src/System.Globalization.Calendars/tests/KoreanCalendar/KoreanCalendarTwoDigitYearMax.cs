// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
