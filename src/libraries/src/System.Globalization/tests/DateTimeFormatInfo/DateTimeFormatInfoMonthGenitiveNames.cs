// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Globalization.Tests
{
    public class DateTimeFormatInfoMonthGenitiveNames
    {
        public static IEnumerable<object[]> MonthGenitiveNames_TestData()
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
        [MemberData(nameof(MonthGenitiveNames_TestData))]
        public void MonthGenitiveNames(DateTimeFormatInfo format, string[] expected)
        {
            Assert.Equal(expected, format.MonthGenitiveNames);
        }

        [Fact]
        public void MonthGenitiveNames_Set()
        {
            string[] newMonthGenitiveNames = new string[] { "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12", "" };
            var format = new DateTimeFormatInfo();
            format.MonthGenitiveNames = newMonthGenitiveNames;
            Assert.Equal(newMonthGenitiveNames, format.MonthGenitiveNames);
        }

        [Fact]
        public void MonthGenitiveNames_Set_Invalid()
        {
            AssertExtensions.Throws<ArgumentNullException>("value", () => new DateTimeFormatInfo().MonthGenitiveNames = null); // Value is null
            AssertExtensions.Throws<ArgumentNullException>("value", () => new DateTimeFormatInfo().MonthGenitiveNames = new string[] { "1", "2", "3", null, "5", "6", "7", "8", "9", "10", "11", "12", "" }); // Value has null
            AssertExtensions.Throws<ArgumentException>("value", () => new DateTimeFormatInfo().MonthGenitiveNames = new string[] { "Jan" }); // Value.Length is not 13

            // DateTimeFormatInfo.InvariantInfo is read only
            Assert.Throws<InvalidOperationException>(() => DateTimeFormatInfo.InvariantInfo.MonthGenitiveNames = new string[] { "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12", "" });
        }
    }
}
