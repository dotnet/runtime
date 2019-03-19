// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Globalization.Tests
{
    public class DateTimeFormatInfoMonthNames
    {
        [Fact]
        public void MonthNames_GetInvariantInfo_ReturnsExpected()
        {
            Assert.Equal(new string[] { "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December", "" }, DateTimeFormatInfo.InvariantInfo.MonthNames);
        }

        [Fact]
        public void MonthNames_Get_ReturnsClone()
        {
            var format = new DateTimeFormatInfo();
            Assert.Equal(format.MonthNames, format.MonthNames);
            Assert.NotSame(format.MonthNames, format.MonthNames);
        }

        public static IEnumerable<object[]> MonthNames_Set_TestData()
        {
            yield return new object[] { new string[] { "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12", "" } };
            yield return new object[] { new string[] { "", "", "", "", "", "", "", "", "", "", "", "", "" } };
        }

        [Theory]
        [MemberData(nameof(MonthNames_Set_TestData))]
        public void MonthNames_Set_GetReturnsExpected(string[] value)
        {
            var format = new DateTimeFormatInfo();
            format.MonthNames = value;
            Assert.Equal(value, format.MonthNames);

            // Does not clone in setter, only in getter.
            value[0] = null;
            Assert.NotSame(value, format.MonthNames);
            Assert.Equal(value, format.MonthNames);
        }

        [Fact]
        public void MonthNames_SetNullValue_ThrowsArgumentNullException()
        {
            var format = new DateTimeFormatInfo();
            AssertExtensions.Throws<ArgumentNullException>("value", () => format.MonthNames = null);
        }

        [Fact]
        public void MonthNames_SetNullValueInValues_ThrowsArgumentNullException()
        {
            var format = new DateTimeFormatInfo();
            AssertExtensions.Throws<ArgumentNullException>("value", () => format.MonthNames = new string[] { "1", "2", "3", null, "5", "6", "7", "8", "9", "10", "11", "12", "" });
        }

        public static IEnumerable<object[]> MonthNames_SetInvalidLength_TestData()
        {
            yield return new object[] { new string[] { "Jan" } };
            yield return new object[] { new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec", "", "Additional" } };
        }

        [Theory]
        [MemberData(nameof(MonthNames_SetInvalidLength_TestData))]
        public void MonthNames_SetNullValueInValues_ThrowsArgumentNullException(string[] value)
        {
            var format = new DateTimeFormatInfo();
            AssertExtensions.Throws<ArgumentException>("value", () => format.MonthNames = value);
        }

        [Fact]
        public void MonthNames_SetReadOnly_ThrowsInvalidOperationException()
        {
            Assert.Throws<InvalidOperationException>(() => DateTimeFormatInfo.InvariantInfo.MonthNames = new string[] { "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12", "" });
        }
        
        [Fact]
        public void MonthNames_Format_ReturnsExpected()
        {
            var format = new DateTimeFormatInfo();
            format.MonthNames = new string[] { "Jan.", "Feb.", "Mar.", "Apr.", "May.", "Jun.", "Jul.", "Aug.", "Sep.", "Oct.", "Nov.", "Dec.", "." };
            Assert.Equal("Jun. 76", new DateTime(1976, 6, 19).ToString("MMMM yy", format));
        }

        [Fact]
        public void MonthNames_FormatWithNull_ThrowsNullReferenceException()
        {
            var value = new string[] { "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12", "13" };
            var format = new DateTimeFormatInfo
            {
                MonthNames = value
            };
            value[0] = null;

            var dateTime = new DateTime(2014, 1, 28);
            Assert.Throws<NullReferenceException>(() => dateTime.ToString("MMMM", format));
        }
    }
}
