// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using Xunit;

namespace System.Globalization.Tests
{
    public class DateTimeFormatInfoClone
    {
        // PosTest1: Call Clone method on a instance created from Ctor
        [Fact]
        public void PosTest1()
        {
            DateTimeFormatInfo expected = new DateTimeFormatInfo();

            VerificationHelper(expected, expected.Clone());
        }

        // PosTest2: Call Clone method on a instance created from several cultures
        [Fact]
        public void PosTest2()
        {
            DateTimeFormatInfo expected = new CultureInfo("en-us").DateTimeFormat;
            VerificationHelper(expected, expected.Clone());

            expected = new CultureInfo("fr-FR").DateTimeFormat;
            VerificationHelper(expected, expected.Clone());
        }

        // PosTest3: Call Clone method on a readonly instance created from several cultures
        [Fact]
        public void PosTest3()
        {
            DateTimeFormatInfo expected = CultureInfo.InvariantCulture.DateTimeFormat;
            VerificationHelper(expected, expected.Clone());
        }

        private void VerificationHelper(DateTimeFormatInfo expected, Object obj)
        {
            Assert.True(obj is DateTimeFormatInfo);
            DateTimeFormatInfo actual = obj as DateTimeFormatInfo;
            Assert.False(actual.IsReadOnly);
            IsEquals(actual.AbbreviatedDayNames, expected.AbbreviatedDayNames);
            IsEquals(actual.AbbreviatedMonthGenitiveNames, expected.AbbreviatedMonthGenitiveNames);
            IsEquals(actual.AbbreviatedMonthNames, expected.AbbreviatedMonthNames);
            IsEquals(actual.DayNames, expected.DayNames);
            IsEquals(actual.MonthGenitiveNames, expected.MonthGenitiveNames);
            IsEquals(actual.MonthNames, expected.MonthNames);
            IsEquals(actual.ShortestDayNames, expected.ShortestDayNames);
            IsEquals(actual.AMDesignator, expected.AMDesignator);
            //DateTimeFormatInfo.DateSeparator property has been removed
            IsEquals(actual.FullDateTimePattern, expected.FullDateTimePattern);
            IsEquals(actual.LongDatePattern, expected.LongDatePattern);
            IsEquals(actual.LongTimePattern, expected.LongTimePattern);
            IsEquals(actual.MonthDayPattern, expected.MonthDayPattern);
            IsEquals(actual.PMDesignator, expected.PMDesignator);
            IsEquals(actual.RFC1123Pattern, expected.RFC1123Pattern);
            IsEquals(actual.ShortDatePattern, expected.ShortDatePattern);
            IsEquals(actual.ShortTimePattern, expected.ShortTimePattern);
            IsEquals(actual.SortableDateTimePattern, expected.SortableDateTimePattern);
            //DateTimeFormatInfo.TimeSeparator property has been removed
            IsEquals(actual.UniversalSortableDateTimePattern, expected.UniversalSortableDateTimePattern);
            IsEquals(actual.YearMonthPattern, expected.YearMonthPattern);
            IsEquals(actual.CalendarWeekRule, expected.CalendarWeekRule);
            IsEquals(actual.FirstDayOfWeek, expected.FirstDayOfWeek);
        }

        private void IsEquals(string str1, string str2)
        {
            Assert.Equal(str2, str1);
        }

        private void IsEquals(DayOfWeek value1, DayOfWeek value2)
        {
            Assert.Equal(value2, value1);
        }

        private void IsEquals(CalendarWeekRule value1, CalendarWeekRule value2)
        {
            Assert.Equal(value2, value1);
        }

        private void IsEquals(string[] array1, string[] array2)
        {
            Assert.Equal(array2, array1);
        }
    }
}
