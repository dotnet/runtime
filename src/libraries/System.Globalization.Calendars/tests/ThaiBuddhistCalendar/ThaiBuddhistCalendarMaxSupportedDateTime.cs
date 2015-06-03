// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using Xunit;

namespace System.Globalization.CalendarsTests
{
    // System.Globalization.ThaiBuddhistCalendar.MaxSupportedDateTime
    public class ThaiBuddhistCalendarMaxSupportedDateTime
    {
        #region Positive Tests
        // PosTest1: Verify the MaxSupportedDateTime is equivalent to
        // the last moment of December 31, 9999 C.E. in the Gregorian calendar
        [Fact]
        public void PosTest1()
        {
            System.Globalization.Calendar tbc = new ThaiBuddhistCalendar();
            System.Globalization.Calendar gc = new GregorianCalendar();
            DateTime desiretdDT = gc.ToDateTime(9999, 12, 31, 23, 59, 59, 999);
            desiretdDT = desiretdDT.AddTicks(9999);
            Assert.Equal(tbc.MaxSupportedDateTime, desiretdDT);
        }
        #endregion
    }
}