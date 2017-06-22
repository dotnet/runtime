// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using Xunit;

namespace System.ComponentModel.Tests
{
    public class DateTimeConverterTests : ConverterTestBase
    {
        private static TypeConverter s_converter = new DateTimeConverter();
        private static DateTime s_testDate = new DateTime(1998, 12, 5);

        [Fact]
        public static void CanConvertFrom_WithContext()
        {
            CanConvertFrom_WithContext(new object[2, 2]
                {
                    { typeof(string), true },
                    { typeof(int), false }
                },
                DateTimeConverterTests.s_converter);
        }

        [Fact]
        public static void ConvertFrom_WithContext()
        {
            ConvertFrom_WithContext(new object[3, 3]
                {
                    { "  ", DateTime.MinValue, null },
                    { DateTimeConverterTests.s_testDate.ToString(), DateTimeConverterTests.s_testDate, null },
                    { DateTimeConverterTests.s_testDate.ToString(CultureInfo.InvariantCulture.DateTimeFormat), DateTimeConverterTests.s_testDate, CultureInfo.InvariantCulture }
                },
                DateTimeConverterTests.s_converter);
        }

        [Fact]
        public static void ConvertFrom_WithContext_Negative()
        {
            Assert.Throws<NotSupportedException>(
                () => DateTimeConverterTests.s_converter.ConvertFrom(TypeConverterTests.s_context, null, 1));

            Assert.Throws<FormatException>(
                () => DateTimeConverterTests.s_converter.ConvertFrom(TypeConverterTests.s_context, null, "aaa"));
        }

        [Fact]
        public static void ConvertTo_WithContext()
        {
            RemoteInvoke(() => {
                CultureInfo.CurrentCulture = new CultureInfo("pl-PL");
                DateTimeFormatInfo formatInfo = (DateTimeFormatInfo)CultureInfo.CurrentCulture.GetFormat(typeof(DateTimeFormatInfo));
                string formatWithTime = formatInfo.ShortDatePattern + " " + formatInfo.ShortTimePattern;
                string format = formatInfo.ShortDatePattern;
                DateTime testDateAndTime = new DateTime(1998, 12, 5, 22, 30, 30);

                ConvertTo_WithContext(new object[5, 3]
                    {
                    { DateTimeConverterTests.s_testDate, DateTimeConverterTests.s_testDate.ToString(format, CultureInfo.CurrentCulture), null },
                    { testDateAndTime, testDateAndTime.ToString(formatWithTime, CultureInfo.CurrentCulture), null },
                    { DateTime.MinValue, string.Empty, null },
                    { DateTimeConverterTests.s_testDate, "1998-12-05", CultureInfo.InvariantCulture },
                    { testDateAndTime, "12/05/1998 22:30:30", CultureInfo.InvariantCulture }
                    },
                    DateTimeConverterTests.s_converter);

                return SuccessExitCode;
            });
        }
    }
}
