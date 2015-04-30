// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Xunit;

namespace System.ComponentModel.Tests
{
    public class DateTimeOffsetConverterTests : ConverterTestBase
    {
        private static TypeConverter s_converter = new DateTimeOffsetConverter();
        private static DateTimeOffset s_testOffset = new DateTimeOffset(new DateTime(1998, 12, 5));

        [Fact]
        public static void CanConvertFrom_WithContext()
        {
            CanConvertFrom_WithContext(new object[2, 2]
                {
                    { typeof(string), true },
                    { typeof(int), false }
                },
                DateTimeOffsetConverterTests.s_converter);
        }

        [Fact]
        public static void ConvertFrom_WithContext()
        {
            ConvertFrom_WithContext(new object[3, 3]
                {
                    { "  ", DateTimeOffset.MinValue, null },
                    { DateTimeOffsetConverterTests.s_testOffset.ToString(), DateTimeOffsetConverterTests.s_testOffset, null },
                    { DateTimeOffsetConverterTests.s_testOffset.ToString(CultureInfo.InvariantCulture.DateTimeFormat), DateTimeOffsetConverterTests.s_testOffset, CultureInfo.InvariantCulture }
                },
                DateTimeOffsetConverterTests.s_converter);
        }

        [Fact]
        public static void ConvertFrom_WithContext_Negative()
        {
            Assert.Throws<NotSupportedException>(
                () => DateTimeOffsetConverterTests.s_converter.ConvertFrom(TypeConverterTests.s_context, null, 1));

            Assert.Throws<FormatException>(
                () => DateTimeOffsetConverterTests.s_converter.ConvertFrom(TypeConverterTests.s_context, null, "aaa"));
        }

        [Fact]
        [ActiveIssue(846, PlatformID.AnyUnix)]
        public static void ConvertTo_WithContext()
        {
            DateTimeFormatInfo formatInfo = (DateTimeFormatInfo)CultureInfo.CurrentCulture.GetFormat(typeof(DateTimeFormatInfo));
            string formatWithTime = formatInfo.ShortDatePattern + " " + formatInfo.ShortTimePattern + " zzz";
            string format = formatInfo.ShortDatePattern + " zzz";
            DateTimeOffset testDateAndTime = new DateTimeOffset(new DateTime(1998, 12, 5, 22, 30, 30));

            ConvertTo_WithContext(new object[5, 3]
                {
                    { DateTimeOffsetConverterTests.s_testOffset, DateTimeOffsetConverterTests.s_testOffset.ToString(format, CultureInfo.CurrentCulture), null },
                    { testDateAndTime, testDateAndTime.ToString(formatWithTime, CultureInfo.CurrentCulture), null },
                    { DateTimeOffset.MinValue, string.Empty, null },
                    { DateTimeOffsetConverterTests.s_testOffset, DateTimeOffsetConverterTests.s_testOffset.ToString("yyyy-MM-dd zzz", CultureInfo.InvariantCulture), CultureInfo.InvariantCulture },
                    { testDateAndTime, testDateAndTime.ToString(CultureInfo.InvariantCulture), CultureInfo.InvariantCulture }
                },
                DateTimeOffsetConverterTests.s_converter);
        }
    }
}
