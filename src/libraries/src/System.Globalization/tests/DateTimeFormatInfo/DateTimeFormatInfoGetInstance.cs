// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Globalization.Tests
{
    public class TestIFormatProviderClass : IFormatProvider
    {
        public object GetFormat(Type formatType)
        {
            return this;
        }
    }

    public class TestIFormatProviderClass2 : IFormatProvider
    {
        public object GetFormat(Type formatType)
        {
            return new DateTimeFormatInfo();
        }
    }

    public class DateTimeFormatInfoGetInstance
    {
        public static IEnumerable<object[]> GetInstance_NotNull_TestData()
        {
            yield return new object[] { new DateTimeFormatInfo() };
            yield return new object[] { new CultureInfo("en-US") };
            yield return new object[] { new TestIFormatProviderClass2() };
        }

        [Theory]
        [MemberData(nameof(GetInstance_NotNull_TestData))]
        public void GetInstance_NotNull(IFormatProvider provider)
        {
            Assert.NotNull(DateTimeFormatInfo.GetInstance(provider));
        }

        private void AssertSameValues(DateTimeFormatInfo expected, DateTimeFormatInfo value)
        {
            if (value.Equals(expected))
            {
                // same instance, we don't have to test the values 
                return;
            }

            Assert.Equal(expected.AbbreviatedDayNames, value.AbbreviatedDayNames);
            Assert.Equal(expected.AbbreviatedMonthGenitiveNames, value.AbbreviatedMonthGenitiveNames);
            Assert.Equal(expected.AbbreviatedMonthNames, value.AbbreviatedMonthNames);
            Assert.Equal(expected.DayNames, value.DayNames);
            Assert.Equal(expected.MonthGenitiveNames, value.MonthGenitiveNames);
            Assert.Equal(expected.MonthNames, value.MonthNames);
            Assert.Equal(expected.ShortestDayNames, value.ShortestDayNames);

            Assert.Equal(expected.AMDesignator, value.AMDesignator);
            Assert.Equal(expected.FullDateTimePattern, value.FullDateTimePattern);
            Assert.Equal(expected.LongDatePattern, value.LongDatePattern);
            Assert.Equal(expected.LongTimePattern, value.LongTimePattern);
            Assert.Equal(expected.MonthDayPattern, value.MonthDayPattern);
            Assert.Equal(expected.PMDesignator, value.PMDesignator);
            Assert.Equal(expected.RFC1123Pattern, value.RFC1123Pattern);
            Assert.Equal(expected.ShortDatePattern, value.ShortDatePattern);
            Assert.Equal(expected.ShortTimePattern, value.ShortTimePattern);
            Assert.Equal(expected.SortableDateTimePattern, value.SortableDateTimePattern);
            Assert.Equal(expected.UniversalSortableDateTimePattern, value.UniversalSortableDateTimePattern);
            Assert.Equal(expected.YearMonthPattern, value.YearMonthPattern);
            Assert.Equal(expected.CalendarWeekRule, value.CalendarWeekRule);
            Assert.Equal(expected.FirstDayOfWeek, value.FirstDayOfWeek);
        }

        [Fact]
        public void GetInstance_ExpectedCurrent()
        {
            AssertSameValues(DateTimeFormatInfo.CurrentInfo, DateTimeFormatInfo.GetInstance(null));
            AssertSameValues(DateTimeFormatInfo.CurrentInfo, DateTimeFormatInfo.GetInstance(new TestIFormatProviderClass()));
        }
    }
}
