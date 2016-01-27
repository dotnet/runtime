// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Globalization;
using Xunit;

namespace System.Globalization.CalendarsTests
{
    // System.Globalization.ThaiBuddhistCalendar.Eras
    public class ThaiBuddhistCalendarEras
    {
        #region Positive Tests
        // PosTest1: Verify the Ears has one element and value is 1
        [Fact]
        public void PosTest1()
        {
            System.Globalization.Calendar tbc = new ThaiBuddhistCalendar();
            Assert.Equal(1, tbc.Eras.Length);
            Assert.Equal(1, tbc.Eras[0]);
        }
        #endregion
    }
}
