// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
