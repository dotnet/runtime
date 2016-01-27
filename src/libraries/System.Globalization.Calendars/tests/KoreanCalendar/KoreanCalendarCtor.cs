// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Globalization;
using Xunit;

namespace System.Globalization.CalendarsTests
{
    //System.Globalization.KoreanCalendar.Ctor
    public class KoreanCalendarCtor
    {
        #region Test Logic
        // PosTest1:Create a instance of KoreanCalendar
        [Fact]
        public void PosTest1()
        {
            System.Globalization.Calendar kC = new KoreanCalendar();
            Assert.NotNull(kC);
            Assert.Equal(1, kC.MinSupportedDateTime.Year);
            Assert.Equal(9999, kC.MaxSupportedDateTime.Year);
        }
        #endregion
    }
}
