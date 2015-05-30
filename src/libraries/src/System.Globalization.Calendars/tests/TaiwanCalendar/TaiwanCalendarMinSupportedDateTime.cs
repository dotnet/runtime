// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using Xunit;

namespace System.Globalization.CalendarsTests
{
    // System.Globalization.TaiwanCalendar.MinSupportedDateTime
    public class TaiwanCalendarMinSupportedDateTime
    {
        #region Positive Tests
        // PosTest1: Verify the MinSupportedDateTime is equivalent to the first moment of January 1, 1912 C.E.
        // in the Gregorian calendar
        [Fact]
        public void PosTest1()
        {
            System.Globalization.Calendar tc = new TaiwanCalendar();
            System.Globalization.Calendar gc = new GregorianCalendar();
            DateTime desiredDT = gc.ToDateTime(1912, 1, 1, 0, 0, 0, 0);
            Assert.Equal(desiredDT, tc.MinSupportedDateTime);
        }
        #endregion
    }
}

