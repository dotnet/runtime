// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using Xunit;

namespace System.Globalization.CalendarsTests
{
    //System.Globalization.KoreanCalendar.Eras
    public class KoreanCalendarEras
    {
        #region Test Logic
        [Fact]
        public void PosTest1()
        {
            System.Globalization.Calendar kC = new KoreanCalendar();
            int[] eras;
            int expectedValue = 1;
            int actualValue;
            eras = kC.Eras;
            actualValue = eras[0];
            Assert.Equal(expectedValue, actualValue);
            Assert.Equal(1, eras.Length);
        }
        #endregion
    }
}