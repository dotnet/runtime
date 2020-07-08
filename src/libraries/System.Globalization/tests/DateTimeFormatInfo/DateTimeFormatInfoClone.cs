// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.Globalization.Tests
{
    public class DateTimeFormatInfoClone
    {
        public static IEnumerable<object[]> Clone_TestData()
        {
            yield return new object[] { new DateTimeFormatInfo() };
            yield return new object[] { new CultureInfo("en-us").DateTimeFormat };
            yield return new object[] { new CultureInfo("fr-FR").DateTimeFormat };
            yield return new object[] { DateTimeFormatInfo.InvariantInfo };
        }

        [Theory]
        [MemberData(nameof(Clone_TestData))]
        public void Clone_Invoke_ReturnsExpected(DateTimeFormatInfo format)
        {
            DateTimeFormatInfo clone = (DateTimeFormatInfo)format.Clone();
            Assert.NotSame(format, clone);

            Assert.False(clone.IsReadOnly);
            Assert.Equal(format.AbbreviatedDayNames, clone.AbbreviatedDayNames);
            Assert.Equal(format.AbbreviatedMonthGenitiveNames, clone.AbbreviatedMonthGenitiveNames);
            Assert.Equal(format.AbbreviatedMonthNames, clone.AbbreviatedMonthNames);
            Assert.Equal(format.DayNames, clone.DayNames);
            Assert.Equal(format.MonthGenitiveNames, clone.MonthGenitiveNames);
            Assert.Equal(format.MonthNames, clone.MonthNames);
            Assert.Equal(format.ShortestDayNames, clone.ShortestDayNames);

            Assert.Equal(format.AMDesignator, clone.AMDesignator);
            Assert.Equal(format.FullDateTimePattern, clone.FullDateTimePattern);
            Assert.Equal(format.LongDatePattern, clone.LongDatePattern);
            Assert.Equal(format.LongTimePattern, clone.LongTimePattern);
            Assert.Equal(format.MonthDayPattern, clone.MonthDayPattern);
            Assert.Equal(format.PMDesignator, clone.PMDesignator);
            Assert.Equal(format.RFC1123Pattern, clone.RFC1123Pattern);
            Assert.Equal(format.ShortDatePattern, clone.ShortDatePattern);
            Assert.Equal(format.ShortTimePattern, clone.ShortTimePattern);
            Assert.Equal(format.SortableDateTimePattern, clone.SortableDateTimePattern);
            Assert.Equal(format.UniversalSortableDateTimePattern, clone.UniversalSortableDateTimePattern);
            Assert.Equal(format.YearMonthPattern, clone.YearMonthPattern);
            Assert.Equal(format.CalendarWeekRule, clone.CalendarWeekRule);
            Assert.Equal(format.FirstDayOfWeek, clone.FirstDayOfWeek);
        }
    }
}
