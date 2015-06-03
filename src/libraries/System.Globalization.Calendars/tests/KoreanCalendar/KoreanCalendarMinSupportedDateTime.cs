// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using Xunit;

namespace System.Globalization.CalendarsTests
{
    //System.Globalization.KoreanCalendar.MinSupportedDateTime
    public class KoreanCalendarMinSupportedDateTime
    {
        #region Test Logic
        // PosTest1:get the property
        [Fact]
        public void PosTest1()
        {
            System.Globalization.Calendar kC = new KoreanCalendar();
            DateTime expectedValue = DateTime.MinValue;
            DateTime actualValue;
            actualValue = kC.MinSupportedDateTime;
            Assert.Equal(expectedValue, actualValue);
        }
        #endregion
    }
}