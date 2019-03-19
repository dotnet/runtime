// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Globalization.Tests
{
    public class DateTimeFormatInfoMonthGenitiveNames
    {
        public static IEnumerable<object[]> MonthGenitiveNames_Get_TestData()
        {
            yield return new object[] { DateTimeFormatInfo.InvariantInfo, new string[] { "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December", "" } };
            yield return new object[]
            {
                new CultureInfo("ru-RU").DateTimeFormat,
                new string[]
                {
                    "\u044F\u043D\u0432\u0430\u0440\u044F",
                    "\u0444\u0435\u0432\u0440\u0430\u043B\u044F",
                    "\u043C\u0430\u0440\u0442\u0430",
                    "\u0430\u043F\u0440\u0435\u043B\u044F",
                    "\u043C\u0430\u044F",
                    "\u0438\u044E\u043D\u044F",
                    "\u0438\u044E\u043B\u044F",
                    "\u0430\u0432\u0433\u0443\u0441\u0442\u0430",
                    "\u0441\u0435\u043D\u0442\u044F\u0431\u0440\u044F",
                    "\u043E\u043A\u0442\u044F\u0431\u0440\u044F",
                    "\u043D\u043E\u044F\u0431\u0440\u044F",
                    "\u0434\u0435\u043A\u0430\u0431\u0440\u044F",
                    ""
                }
            };
        }

        [Theory]
        [MemberData(nameof(MonthGenitiveNames_Get_TestData))]
        public void MonthGenitiveNames_Get_ReturnsExpected(DateTimeFormatInfo format, string[] expected)
        {
            Assert.Equal(expected, format.MonthGenitiveNames);
        }

        [Fact]
        public void MonthGenitiveNames_Get_ReturnsClone()
        {
            var format = new DateTimeFormatInfo();
            Assert.Equal(format.MonthGenitiveNames, format.MonthGenitiveNames);
            Assert.NotSame(format.MonthGenitiveNames, format.MonthGenitiveNames);
        }

        public static IEnumerable<object[]> MonthGenitiveNames_Set_TestData()
        {
            yield return new object[] { new string[] { "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12", "" } };
            yield return new object[] { new string[] { "", "", "", "", "", "", "", "", "", "", "", "", "" } };
        }

        [Theory]
        [MemberData(nameof(MonthGenitiveNames_Set_TestData))]
        public void MonthGenitiveNames_Set_GetReturnsExpected(string[] value)
        {
            var format = new DateTimeFormatInfo();
            format.MonthGenitiveNames = value;
            Assert.Equal(value, format.MonthGenitiveNames);

            // Does not clone in setter, only in getter.
            value[0] = null;
            Assert.NotSame(value, format.MonthGenitiveNames);
            Assert.Equal(value, format.MonthGenitiveNames);
        }

        [Fact]
        public void MonthGenitiveNames_SetNullValue_ThrowsArgumentNullException()
        {
            var format = new DateTimeFormatInfo();
            AssertExtensions.Throws<ArgumentNullException>("value", () => format.MonthGenitiveNames = null);
        }

        [Fact]
        public void MonthGenitiveNames_SetNullValueInValues_ThrowsArgumentNullException()
        {
            var format = new DateTimeFormatInfo();
            AssertExtensions.Throws<ArgumentNullException>("value", () => format.MonthGenitiveNames = new string[] { "1", "2", "3", null, "5", "6", "7", "8", "9", "10", "11", "12", "" });
        }

        public static IEnumerable<object[]> MonthGenitiveNames_SetInvalidLength_TestData()
        {
            yield return new object[] { new string[] { "Jan" } };
            yield return new object[] { new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec", "", "Additional" } };
        }

        [Theory]
        [MemberData(nameof(MonthGenitiveNames_SetInvalidLength_TestData))]
        public void MonthGenitiveNames_SetNullValueInValues_ThrowsArgumentNullException(string[] value)
        {
            var format = new DateTimeFormatInfo();
            AssertExtensions.Throws<ArgumentException>("value", () => format.MonthGenitiveNames = value);
        }

        [Fact]
        public void MonthGenitiveNames_SetReadOnly_ThrowsInvalidOperationException()
        {
            Assert.Throws<InvalidOperationException>(() => DateTimeFormatInfo.InvariantInfo.MonthGenitiveNames = new string[] { "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12", "" });
        }
        
        [Fact]
        public void MonthGenitiveNames_Format_ReturnsExpected()
        {
            var format = new DateTimeFormatInfo();
            format.MonthGenitiveNames = new string[] { "Jan.", "Feb.", "Mar.", "Apr.", "May.", "Jun.", "Jul.", "Aug.", "Sep.", "Oct.", "Nov.", "Dec.", "." };
            Assert.Equal("19 Jun. 76", new DateTime(1976, 6, 19).ToString("dd MMMM yy", format));
        }

        [Fact]
        public void MonthGenitiveNames_FormatWithNull_ThrowsNullReferenceException()
        {
            var value = new string[] { "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12", "13" };
            var format = new DateTimeFormatInfo
            {
                MonthGenitiveNames = value
            };
            value[0] = null;

            var dateTime = new DateTime(2014, 1, 28);
            Assert.Throws<NullReferenceException>(() => dateTime.ToString("dd MMMM", format));
        }
    }
}
