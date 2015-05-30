// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using Xunit;

namespace System.Globalization.CalendarsTests
{
    //System.Globalization.KoreanCalendar.MaxSupportedDateTime
    public class KoreanCalendarMaxSupportedDateTime
    {
        #region Test Logic
        // PosTest1:get the property
        [Fact]
        public void PosTest1()
        {
            System.Globalization.Calendar kC = new KoreanCalendar();
            DateTime expectedValue = DateTime.MaxValue;
            DateTime actualValue;
            actualValue = kC.MaxSupportedDateTime;
            Assert.Equal(expectedValue, actualValue);
        }
        #endregion
    }
}