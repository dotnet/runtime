// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Globalization.Tests
{
    public class DateTimeFormatInfoDayNames
    {
        [Fact]
        public void DayNames_GetInvariantInfo_ReturnsExpected()
        {
            Assert.Equal(new string[] { "Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday" }, DateTimeFormatInfo.InvariantInfo.DayNames);
        }

        [Fact]
        public void DayNames_Get_ReturnsClone()
        {
            var format = new DateTimeFormatInfo();
            Assert.Equal(format.DayNames, format.DayNames);
            Assert.NotSame(format.DayNames, format.DayNames);
        }

        public static IEnumerable<object[]> DayNames_Set_TestData()
        {
            yield return new object[] { new string[] { "1", "2", "3", "4", "5", "6", "7" } };
            yield return new object[] { new string[] { "", "", "", "", "", "", "" } };
        }

        [Theory]
        [MemberData(nameof(DayNames_Set_TestData))]
        public void DayNames_Set_GetReturnsExpected(string[] value)
        {
            var format = new DateTimeFormatInfo();
            format.DayNames = value;
            Assert.Equal(value, format.DayNames);

            // Does not clone in setter, only in getter.
            value[0] = null;
            Assert.NotSame(value, format.DayNames);
            Assert.Equal(value, format.DayNames);
        }

        [Fact]
        public void DayNames_SetNulValue_ThrowsArgumentNullException()
        {
            var format = new DateTimeFormatInfo();
            AssertExtensions.Throws<ArgumentNullException>("value", () => format.DayNames = null);
        }

        [Fact]
        public void DayNames_SetNulValueInValue_ThrowsArgumentNullException()
        {
            var format = new DateTimeFormatInfo();
            AssertExtensions.Throws<ArgumentNullException>("value", () => format.DayNames = new string[] { "1", "2", "3", null, "5", "6", "7" });
        }

        public static IEnumerable<object[]> DayNames_SetInvalidLength_TestData()
        {
            yield return new object[] { new string[] { "Sun" } };
            yield return new object[] { new string[] { "Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Additional" } };
        }

        [Theory]
        [MemberData(nameof(DayNames_SetInvalidLength_TestData))]
        public void DayNames_SetInvalidLength_ThrowsArgumentException(string[] value)
        {
            var format = new DateTimeFormatInfo();
            AssertExtensions.Throws<ArgumentException>("value", (() => format.DayNames = value));
        }

        [Fact]
        public void DayNames_SetReadOnly_ThrowsInvalidOperationException()
        {
            Assert.Throws<InvalidOperationException>(() => DateTimeFormatInfo.InvariantInfo.DayNames = new string[] { "1", "2", "3", "4", "5", "6", "7" });
        }

        [Fact]
        public void DayNames_FormatWithNull_ThrowsNullReferenceException()
        {
            var value = new string[] { "1", "2", "3", "4", "5", "6", "7" };
            var format = new DateTimeFormatInfo
            {
                DayNames = value
            };
            value[0] = null;

            var dateTime = new DateTime(2014, 5, 28);
            Assert.Throws<NullReferenceException>(() => dateTime.ToString("dddd MMM yy", format));
        }
    }
}
