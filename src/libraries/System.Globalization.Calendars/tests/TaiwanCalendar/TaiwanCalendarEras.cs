// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
