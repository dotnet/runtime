// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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

        #region Negative Test Logic
        // NegTest1:Invoke the method with year out of lower range
        [Fact]
        public void NegTest1()
        {
            System.Globalization.Calendar kC = new KoreanCalendar();
            int actualValue;
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                actualValue = kC.ToFourDigitYear(100);
            });
        }

        // NegTest2:Invoke the method with negative year
        [Fact]
        public void NegTest2()
        {
            System.Globalization.Calendar kC = new KoreanCalendar();
            int actualValue;
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                actualValue = kC.ToFourDigitYear(-1);
            });
        }

        // NegTest3:Invoke the method with year out of the upper range
        [Fact]
        public void NegTest3()
        {
            System.Globalization.Calendar kC = new KoreanCalendar();
            int actualValue;
            // it stands to reason that if we're looking to throw an exception then it might be a good idea
            // to ensure the value passed into the method is one that will actual cause the exception to be
            // thrown
            // 100-2333  throw exception
            // 2334-12332 no exception
            // 12333 or more throw exception
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                actualValue = kC.ToFourDigitYear(12333);
            });
        }
        #endregion
    }
}