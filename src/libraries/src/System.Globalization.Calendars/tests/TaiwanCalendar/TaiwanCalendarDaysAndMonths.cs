// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using System.Runtime.InteropServices;
using Xunit;

namespace System.Globalization.CalendarsTests
{
    public class TaiwanCalendarDaysAndMonths
    {
        [Fact]
        public void Test1()
        {
            string[] edays = {
                "\u661F\u671F\u65E5",
                "\u661F\u671F\u4E00",
                "\u661F\u671F\u4E8C",
                "\u661F\u671F\u4E09",
                "\u661F\u671F\u56DB",
                "\u661F\u671F\u4E94",
                "\u661F\u671F\u516D"
            };

            string[] emonths = GetMonthNames();

            DateTimeFormatInfo dtfi = new CultureInfo("zh-TW").DateTimeFormat;
            dtfi.Calendar = new TaiwanCalendar();

            // Actual Day Names and Month Names for TaiwanCalendar
            string[] adays = dtfi.DayNames;
            for (int i = 0; i < edays.Length; i++)
            {
                Assert.Equal(edays[i], adays[i]);
            }

            string[] amonths = dtfi.MonthNames;
            for (int i = 0; i < edays.Length; i++)
            {
                Assert.Equal(emonths[i], amonths[i]);
            }
        }

        private static string[] GetMonthNames()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return new string[] {
                    "\u4E00\u6708",
                    "\u4E8C\u6708",
                    "\u4E09\u6708",
                    "\u56DB\u6708",
                    "\u4E94\u6708",
                    "\u516D\u6708",
                    "\u4E03\u6708",
                    "\u516B\u6708",
                    "\u4E5D\u6708",
                    "\u5341\u6708",
                    "\u5341\u4E00\u6708",
                    "\u5341\u4E8C\u6708",
                    string.Empty,
                };
            }
            else
            {
                // CLDR has a digit followed by the month symbol for the month names
                return new string[] {
                    "1\u6708",
                    "2\u6708",
                    "3\u6708",
                    "4\u6708",
                    "5\u6708",
                    "6\u6708",
                    "7\u6708",
                    "8\u6708",
                    "9\u6708",
                    "10\u6708",
                    "11\u6708",
                    "12\u6708",
                    string.Empty,
                };
            }
        }
    }
}
