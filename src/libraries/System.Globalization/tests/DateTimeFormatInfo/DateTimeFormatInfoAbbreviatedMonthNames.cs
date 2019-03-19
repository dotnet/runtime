// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Globalization.Tests
{
    public class DateTimeFormatInfoAbbreviatedMonthNames
    {
        [Fact]
        public void AbbreviatedMonthNames_GetInvariantInfo_ReturnsExpected()
        {
            Assert.Equal(new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec", "" }, DateTimeFormatInfo.InvariantInfo.AbbreviatedMonthNames);
        }

        [Fact]
        public void AbbreviatedMonthNames_Get_ReturnsClone()
        {
            var format = new DateTimeFormatInfo();
            Assert.Equal(format.AbbreviatedMonthNames, format.AbbreviatedMonthNames);
            Assert.NotSame(format.AbbreviatedMonthNames, format.AbbreviatedMonthNames);
        }

        public static IEnumerable<object[]> AbbreviatedMonthNames_Set_TestData()
        {
            yield return new object[] { new string[] { "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12", "" } };
            yield return new object[] { new string[] { "", "", "", "", "", "", "", "", "", "", "", "", "" } };
        }

        [Theory]
        [MemberData(nameof(AbbreviatedMonthNames_Set_TestData))]
        public void AbbreviatedMonthNames_Set_GetReturnsExpected(string[] value)
        {
            var format = new DateTimeFormatInfo();
            format.AbbreviatedMonthNames = value;
            Assert.Equal(value, format.AbbreviatedMonthNames);

            // Does not clone in setter, only in getter.
            value[0] = null;
            Assert.NotSame(value, format.AbbreviatedMonthNames);
            Assert.Equal(value, format.AbbreviatedMonthNames);
        }

        [Fact]
        public void AbbreviatedMonthNames_SetNullValue_ThrowsArgumentNullException()
        {
            var format = new DateTimeFormatInfo();
            AssertExtensions.Throws<ArgumentNullException>("value", () => format.AbbreviatedMonthNames = null);
        }

        [Fact]
        public void AbbreviatedMonthNames_SetNullValueInValues_ThrowsArgumentNullException()
        {
            var format = new DateTimeFormatInfo();
            AssertExtensions.Throws<ArgumentNullException>("value", () => format.AbbreviatedMonthNames = new string[] { "1", "2", "3", null, "5", "6", "7", "8", "9", "10", "11", "12", "" });
        }

        public static IEnumerable<object[]> AbbreviatedMonthNames_SetInvalidLength_TestData()
        {
            yield return new object[] { new string[] { "Jan" } };
            yield return new object[] { new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec", "", "Additional" } };
        }

        [Theory]
        [MemberData(nameof(AbbreviatedMonthNames_SetInvalidLength_TestData))]
        public void AbbreviatedMonthNames_SetNullValueInValues_ThrowsArgumentNullException(string[] value)
        {
            var format = new DateTimeFormatInfo();
            AssertExtensions.Throws<ArgumentException>("value", () => format.AbbreviatedMonthNames = value);
        }

        [Fact]
        public void AbbreviatedMonthNames_SetReadOnly_ThrowsInvalidOperationException()
        {
            Assert.Throws<InvalidOperationException>(() => DateTimeFormatInfo.InvariantInfo.AbbreviatedMonthNames = new string[] { "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12", "" });
        }
        
        [Fact]
        public void AbbreviatedMonthNames_Format_ReturnsExpected()
        {
            var format = new DateTimeFormatInfo();
            format.AbbreviatedMonthNames = new string[] { "Jan.", "Feb.", "Mar.", "Apr.", "May.", "Jun.", "Jul.", "Aug.", "Sep.", "Oct.", "Nov.", "Dec.", "." };
            Assert.Equal("Jun. 76", new DateTime(1976, 6, 19).ToString("MMM yy", format));
        }

        [Fact]
        public void AbbreviatedMonthNames_FormatWithNull_ThrowsNullReferenceException()
        {
            var value = new string[] { "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12", "13" };
            var format = new DateTimeFormatInfo
            {
                AbbreviatedMonthNames = value
            };
            value[0] = null;

            var dateTime = new DateTime(2014, 1, 28);
            Assert.Throws<NullReferenceException>(() => dateTime.ToString("MMM", format));
        }
    }
}
