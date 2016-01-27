// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
