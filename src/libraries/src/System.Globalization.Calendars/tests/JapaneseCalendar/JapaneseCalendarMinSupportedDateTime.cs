// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using Xunit;

namespace System.Globalization.CalendarsTests
{
    public class JapaneseCalendarMinSupportedDateTime
    {
        #region Positive Test Cases
        // PosTest1: Call MinSupportedDateTime to get max supported date time
        [Fact]
        public void PosTest1()
        {
            DateTime actual = new JapaneseCalendar().MinSupportedDateTime;
            DateTime expected = new DateTime(1868, 9, 8);
            Assert.Equal(expected, actual);
        }
        #endregion
    }
}
