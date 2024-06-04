// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Xunit;

namespace System.Tests
{
    public class ConvertToDateTimeTests : ConvertTestBase<DateTime>
    {
        private static readonly DateTimeFormatInfo s_dateTimeFormatInfo = new DateTimeFormatInfo();

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/95338", typeof(PlatformDetection), nameof(PlatformDetection.IsHybridGlobalizationOnApplePlatform))]
        public void FromString()
        {
            DateTime[] expectedValues = { new DateTime(1999, 12, 31, 23, 59, 59), new DateTime(100, 1, 1, 0, 0, 0), new DateTime(2216, 2, 29, 0, 0, 0), new DateTime(1, 1, 1, 0, 0, 0) };

            // Generate test values. Note that some calendars have very restricted date ranges.
            var dateTimeFormat = CultureInfo.CurrentCulture.DateTimeFormat;
            if (expectedValues[1] < dateTimeFormat.Calendar.MinSupportedDateTime)
                expectedValues[1] = dateTimeFormat.Calendar.MinSupportedDateTime.AddYears(100);
            if (expectedValues[2] > dateTimeFormat.Calendar.MaxSupportedDateTime)
                expectedValues[2] = dateTimeFormat.Calendar.MaxSupportedDateTime.Date;
            if (expectedValues[3] < dateTimeFormat.Calendar.MinSupportedDateTime)
                expectedValues[3] = dateTimeFormat.Calendar.MinSupportedDateTime;

            string pattern = dateTimeFormat.LongDatePattern + ' ' + dateTimeFormat.LongTimePattern;
            string[] testValues = new string[expectedValues.Length];
            for (int i = 0; i < expectedValues.Length; i++)
            {
                testValues[i] = expectedValues[i].ToString(pattern, dateTimeFormat);
            }

            VerifyFromString(Convert.ToDateTime, Convert.ToDateTime, testValues, expectedValues);
            VerifyFromObject(Convert.ToDateTime, Convert.ToDateTime, testValues, expectedValues);

            string[] formatExceptionValues =
            {
            "null",
            // Regression test for case which was throwing IndexOutOfRangeException
            "20-5-14T00:00:00"
        };

            VerifyFromStringThrows<FormatException>(Convert.ToDateTime, Convert.ToDateTime, formatExceptionValues);
        }

        [Fact]
        public void FromStringWithCustomFormatProvider()
        {
            string[] testValues = { null, "12/31/1999 11:59:59 PM", "0100/01/01 12:00:00 AM", "1492/02/29 12:00:00 AM", "0001/01/01 12:00:00 AM" };
            DateTime[] expectedValues = { DateTime.MinValue, new DateTime(1999, 12, 31, 23, 59, 59), new DateTime(100, 1, 1, 0, 0, 0), new DateTime(1492, 2, 29, 0, 0, 0), new DateTime(1, 1, 1, 0, 0, 0) };
            Assert.Equal(testValues.Length, expectedValues.Length);

            for (int i = 0; i < testValues.Length; i++)
            {
                DateTime result = Convert.ToDateTime(testValues[i], s_dateTimeFormatInfo);
                Assert.Equal(expectedValues[i], result);
                result = Convert.ToDateTime((object)testValues[i], s_dateTimeFormatInfo);
                Assert.Equal(expectedValues[i], result);
            }
        }

        [Fact]
        public void FromDateTime()
        {
            DateTime[] expectedValues = { new DateTime(1999, 12, 31, 23, 59, 59), new DateTime(100, 1, 1, 0, 0, 0), new DateTime(1492, 2, 29, 0, 0, 0), new DateTime(1, 1, 1, 0, 0, 0) };
            for (int i = 0; i < expectedValues.Length; i++)
            {
                DateTime result = Convert.ToDateTime(expectedValues[i]);
                Assert.Equal(expectedValues[i], result);
            }
        }

        [Fact]
        public void FromObject()
        {
            Assert.Throws<InvalidCastException>(() => Convert.ToDateTime(new object()));
            Assert.Throws<InvalidCastException>(() => Convert.ToDateTime(new object(), s_dateTimeFormatInfo));
        }

        [Fact]
        public void FromBoolean()
        {
            Assert.Throws<InvalidCastException>(() => Convert.ToDateTime(false));
        }

        [Fact]
        public void FromChar()
        {
            Assert.Throws<InvalidCastException>(() => Convert.ToDateTime('a'));
        }

        [Fact]
        public void FromInt16()
        {
            Assert.Throws<InvalidCastException>(() => Convert.ToDateTime((short)5));
        }

        [Fact]
        public void FromInt32()
        {
            Assert.Throws<InvalidCastException>(() => Convert.ToDateTime(6));
        }

        [Fact]
        public void FromInt64()
        {
            Assert.Throws<InvalidCastException>(() => Convert.ToDateTime((long)5));
        }

        [Fact]
        public void FromUInt16()
        {
            Assert.Throws<InvalidCastException>(() => Convert.ToDateTime((ushort)5));
        }

        [Fact]
        public void FromUInt32()
        {
            Assert.Throws<InvalidCastException>(() => Convert.ToDateTime((uint)5));
        }

        [Fact]
        public void FromUInt64()
        {
            Assert.Throws<InvalidCastException>(() => Convert.ToDateTime((ulong)5));
        }

        [Fact]
        public void FromSingle()
        {
            Assert.Throws<InvalidCastException>(() => Convert.ToDateTime(1.0f));
        }

        [Fact]
        public void FromDouble()
        {
            Assert.Throws<InvalidCastException>(() => Convert.ToDateTime(1.1));
        }

        [Fact]
        public void FromDecimal()
        {
            Assert.Throws<InvalidCastException>(() => Convert.ToDateTime(1.0m));
        }
    }
}
