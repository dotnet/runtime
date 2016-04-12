// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Globalization.Tests
{
    public class KoreanCalendarTwoDigitYearMax
    {
        [Fact]
        public void TwoDigitYearMax()
        {
            Assert.Equal(4362, new KoreanCalendar().TwoDigitYearMax);
        }
    }
}
