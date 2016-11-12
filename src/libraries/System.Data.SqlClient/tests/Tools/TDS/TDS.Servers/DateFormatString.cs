// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.SqlServer.TDS.Servers
{
    /// <summary>
    /// Class that converts date format from string to enumeration
    /// </summary>
    public static class DateFormatString
    {
        private const string DayMonthYear = "dmy";
        private const string DayYearMonth = "dym";
        private const string MonthDayYear = "mdy";
        private const string MonthYearDay = "myd";
        private const string YearDayMonth = "ydm";
        private const string YearMonthDay = "ymd";

        /// <summary>
        /// Convert a language to enumeration
        /// </summary>
        public static DateFormatType ToEnum(string value)
        {
            // Check every langauge
            if (string.Compare(value, DayMonthYear, true) == 0)
            {
                return DateFormatType.DayMonthYear;
            }
            else if (string.Compare(value, DayYearMonth, true) == 0)
            {
                return DateFormatType.DayYearMonth;
            }
            else if (string.Compare(value, MonthDayYear, true) == 0)
            {
                return DateFormatType.MonthDayYear;
            }
            else if (string.Compare(value, MonthYearDay, true) == 0)
            {
                return DateFormatType.MonthYearDay;
            }
            else if (string.Compare(value, YearDayMonth, true) == 0)
            {
                return DateFormatType.YearDayMonth;
            }
            else if (string.Compare(value, YearMonthDay, true) == 0)
            {
                return DateFormatType.YearMonthDay;
            }

            // Unknown value
            throw new Exception("Unrecognized date format string \"" + value + "\"");
        }

        /// <summary>
        /// Convert enumeration to string
        /// </summary>
        public static string ToString(DateFormatType value)
        {
            // Switch through the langauges
            switch (value)
            {
                case DateFormatType.DayMonthYear:
                    {
                        return DayMonthYear;
                    }
                case DateFormatType.DayYearMonth:
                    {
                        return DayYearMonth;
                    }
                case DateFormatType.MonthDayYear:
                    {
                        return MonthDayYear;
                    }
                case DateFormatType.MonthYearDay:
                    {
                        return MonthYearDay;
                    }
                case DateFormatType.YearDayMonth:
                    {
                        return YearDayMonth;
                    }
                case DateFormatType.YearMonthDay:
                    {
                        return YearMonthDay;
                    }
            }

            // Unknown value
            throw new Exception("Unrecognized date format type \"" + value.ToString() + "\"");
        }
    }
}
