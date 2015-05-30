// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using Xunit;

namespace System.Globalization.CalendarsTests
{
    public class JapaneseCalendarEras
    {
        #region Public Fields
        public static readonly int[] c_EXPECTED_ERAS = new int[] { 4, 3, 2, 1 };
        #endregion

        #region Positive Test Cases
        [Fact]
        public void PosTest1()
        {
            int[] actual = new JapaneseCalendar().Eras;
            Assert.NotNull(actual);
            Assert.Equal(c_EXPECTED_ERAS.Length, actual.Length);
            Assert.Equal(c_EXPECTED_ERAS, actual);
        }
        #endregion
    }
}
