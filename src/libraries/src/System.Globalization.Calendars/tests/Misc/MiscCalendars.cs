// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using Xunit;

namespace System.Globalization.CalendarsTests
{
    public static class MiscCalendarsTests
    {
        [Fact]
        public static void HebrewTest()
        {
            Calendar hCal = new HebrewCalendar();
            DateTime dTest = hCal.ToDateTime(5360, 04, 14, 0, 0, 0, 0);
            Assert.True(dTest.Equals(new DateTime(1600, 1, 1)));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                dTest = hCal.ToDateTime(0, 03, 25, 0, 0, 0, 0);
            });
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                dTest = hCal.ToDateTime(10000, 03, 25, 0, 0, 0, 0);
            });
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                dTest = hCal.ToDateTime(5000, 0, 25, 0, 0, 0, 0);
            });
        }

        [Fact]
        public static void HijriTest()
        {
            HijriCalendar hCal = new HijriCalendar();
            DateTime dTest = hCal.ToDateTime(1008, 06, 15, 0, 0, 0, 0);
            Assert.Equal(dTest, new DateTime(1600, 1, 1).AddDays(hCal.HijriAdjustment));
        }

        [Fact]
        public static void GregorianTest()
        {
            GregorianCalendar gCal = new GregorianCalendar();
            DateTime dTest = gCal.ToDateTime(1600, 1, 1, 0, 0, 0, 0);
            Assert.Equal(dTest, new DateTime(1600, 1, 1));
        }

        [Fact]
        public static void JapaneseTest()
        {
            JapaneseCalendar jCal = new JapaneseCalendar();
            DateTime dTest = jCal.ToDateTime(1, 1, 8, 0, 0, 0, 0);
            Assert.Equal(dTest, new DateTime(1989, 1, 8));
        }

        [Fact]
        public static void KoreanTest()
        {
            KoreanCalendar jCal = new KoreanCalendar();
            DateTime dTest = jCal.ToDateTime(3933, 1, 1, 0, 0, 0, 0);
            Assert.Equal(dTest, new DateTime(1600, 1, 1));
        }

        [Fact]
        public static void ThaiTest()
        {
            ThaiBuddhistCalendar tCal = new ThaiBuddhistCalendar();
            DateTime dTest = tCal.ToDateTime(2143, 1, 1, 0, 0, 0, 0);
            Assert.Equal(dTest, new DateTime(1600, 1, 1));
        }
    }
}
