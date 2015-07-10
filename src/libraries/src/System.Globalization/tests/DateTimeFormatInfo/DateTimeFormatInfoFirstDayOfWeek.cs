// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using Xunit;

namespace System.Globalization.Tests
{
    public class DateTimeFormatInfoFirstDayOfWeek
    {
        // PosTest1: Call FirstDayOfWeek getter method should return correct value for InvariantInfo
        [Fact]
        public void TestGetter()
        {
            VerificationHelper(DateTimeFormatInfo.InvariantInfo,
                    DayOfWeek.Sunday,
                    false);
        }

        // PosTest2: Call FirstDayOfWeek setter method should return correct value
        [Fact]
        public void TestSetter()
        {
            DayOfWeek[] days = new DayOfWeek[] {
                DayOfWeek.Friday,
                DayOfWeek.Monday,
                DayOfWeek.Saturday,
                DayOfWeek.Sunday,
                DayOfWeek.Thursday,
                DayOfWeek.Tuesday,
                DayOfWeek.Wednesday,
            };

            for (int i = 0; i < days.Length; ++i)
            {
                VerificationHelper(new DateTimeFormatInfo(),
                    days[i],
                    true);
            }
        }

        // NegTest1: ArgumentOutOfRangeException should be thrown when The property is being set to a 
        // value that is not a valid FirstDayOfWeek value
        [Fact]
        public void NegTest1()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                new DateTimeFormatInfo().FirstDayOfWeek = (DayOfWeek)(-1);
            });
        }

        // NegTest2: InvalidOperationException should be thrown when The property is being set and 
        // the DateTimeFormatInfo is read-only
        [Fact]
        public void NegTest2()
        {
            Assert.Throws<InvalidOperationException>(() =>
            {
                DateTimeFormatInfo.InvariantInfo.FirstDayOfWeek = DayOfWeek.Wednesday;
            });
        }

        private void VerificationHelper(DateTimeFormatInfo info, DayOfWeek expected, bool setter)
        {
            if (setter)
            {
                info.FirstDayOfWeek = expected;
            }

            DayOfWeek actual = info.FirstDayOfWeek;
            Assert.Equal(expected, actual);
        }
    }
}
