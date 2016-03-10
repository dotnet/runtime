// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Globalization;
using Xunit;

namespace System.Globalization.CalendarsTests
{
    //System.Globalization.KoreanCalendar.ToFourDigitYear(System.Int32)
    public class KoreanCalendarToFourDigitYear
    {
        private readonly RandomDataGenerator _generator = new RandomDataGenerator();

        #region Positive Test Logic
        // PosTest1:Invoke the method with min two digit year
        [Fact]
        public void PosTest1()
        {
            System.Globalization.Calendar kC = new KoreanCalendar();
            int twoDigitMax = kC.TwoDigitYearMax;
            int lBound = twoDigitMax - 99;
            int rBound = twoDigitMax;
            int twoDigitYear = 0;
            int expectedValue;
            if (twoDigitYear < (lBound % 100))
            {
                expectedValue = (lBound / 100 + 1) * 100 + twoDigitYear;
            }
            else
            {
                expectedValue = (lBound / 100) * 100 + twoDigitYear;
            }

            int actualValue = kC.ToFourDigitYear(twoDigitYear);
            Assert.Equal(expectedValue, actualValue);
        }

        // PosTest2:Invoke the method with max two digit year
        [Fact]
        public void PosTest2()
        {
            System.Globalization.Calendar kC = new KoreanCalendar();
            int twoDigitMax = kC.TwoDigitYearMax;
            int lBound = twoDigitMax - 99;
            int rBound = twoDigitMax;
            int twoDigitYear = 99;
            int expectedValue;
            if (twoDigitYear < (lBound % 100))
            {
                expectedValue = (lBound / 100 + 1) * 100 + twoDigitYear;
            }
            else
            {
                expectedValue = (lBound / 100) * 100 + twoDigitYear;
            }

            int actualValue = kC.ToFourDigitYear(twoDigitYear);
            Assert.Equal(expectedValue, actualValue);
        }

        // PosTest3:Invoke the method with random two digit year
        [Fact]
        public void PosTest3()
        {
            System.Globalization.Calendar kC = new KoreanCalendar();
            int twoDigitMax = kC.TwoDigitYearMax;
            int lBound = twoDigitMax - 99;
            int rBound = twoDigitMax;
            int twoDigitYear = _generator.GetInt16(-55) % 100;
            int expectedValue;
            if (twoDigitYear < (lBound % 100))
            {
                expectedValue = (lBound / 100 + 1) * 100 + twoDigitYear;
            }
            else
            {
                expectedValue = (lBound / 100) * 100 + twoDigitYear;
            }

            int actualValue = kC.ToFourDigitYear(twoDigitYear);
            Assert.Equal(expectedValue, actualValue);
        }
        #endregion
    }
}
