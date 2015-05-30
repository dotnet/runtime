// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using Xunit;

namespace System.Globalization.CalendarsTests
{
    // System.Globalization.TaiwanCalendar.Eras
    public class TaiwanCalendarEras
    {
        #region PositiveTesting
        // PosTest1: Verify the Ears has one element and value is 1
        [Fact]
        public void PosTest1()
        {
            System.Globalization.Calendar tc = new TaiwanCalendar();
            Assert.Equal(1, tc.Eras.Length);
            Assert.Equal(1, tc.Eras[0]);
        }
        #endregion
    }
}