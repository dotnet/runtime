// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Globalization.Tests
{
    public class DateTimeFormatInfoAbbreviatedMonthGenitiveNames
    {
        [Fact]
        public void AbbreviatedMonthGenitiveNames_GetInvariantInfo_ReturnsExpected()
        {
            Assert.Equal(new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec", "" }, DateTimeFormatInfo.InvariantInfo.AbbreviatedMonthGenitiveNames);
        }

        [Fact]
        public void AbbreviatedMonthGenitiveNames_Get_ReturnsClone()
        {
            var format = new DateTimeFormatInfo();
            Assert.Equal(format.AbbreviatedMonthGenitiveNames, format.AbbreviatedMonthGenitiveNames);
            Assert.NotSame(format.AbbreviatedMonthGenitiveNames, format.AbbreviatedMonthGenitiveNames);
        }

        public static IEnumerable<object[]> AbbreviatedMonthGenitiveNames_Set_TestData()
        {
            yield return new object[] { new string[] { "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12", "" } };
            yield return new object[] { new string[] { "", "", "", "", "", "", "", "", "", "", "", "", "" } };
        }

        [Theory]
        [MemberData(nameof(AbbreviatedMonthGenitiveNames_Set_TestData))]
        public void AbbreviatedMonthGenitiveNames_Set_GetReturnsExpected(string[] value)
        {
            var format = new DateTimeFormatInfo();
            format.AbbreviatedMonthGenitiveNames = value;
            Assert.Equal(value, format.AbbreviatedMonthGenitiveNames);

            // Does not clone in setter, only in getter.
            value[0] = null;
            Assert.NotSame(value, format.AbbreviatedMonthGenitiveNames);
            Assert.Equal(value, format.AbbreviatedMonthGenitiveNames);
        }

        [Fact]
        public void AbbreviatedMonthGenitiveNames_SetNullValue_ThrowsArgumentNullException()
        {
            var format = new DateTimeFormatInfo();
            AssertExtensions.Throws<ArgumentNullException>("value", () => format.AbbreviatedMonthGenitiveNames = null);
        }

        [Fact]
        public void AbbreviatedMonthGenitiveNames_SetNullValueInValues_ThrowsArgumentNullException()
        {
            var format = new DateTimeFormatInfo();
            AssertExtensions.Throws<ArgumentNullException>("value", () => format.AbbreviatedMonthGenitiveNames = new string[] { "1", "2", "3", null, "5", "6", "7", "8", "9", "10", "11", "12", "" });
        }

        public static IEnumerable<object[]> AbbreviatedMonthGenitiveNames_SetInvalidLength_TestData()
        {
            yield return new object[] { new string[] { "Jan" } };
            yield return new object[] { new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec", "", "Additional" } };
        }

        [Theory]
        [MemberData(nameof(AbbreviatedMonthGenitiveNames_SetInvalidLength_TestData))]
        public void AbbreviatedMonthGenitiveNames_SetNullValueInValues_ThrowsArgumentNullException(string[] value)
        {
            var format = new DateTimeFormatInfo();
            AssertExtensions.Throws<ArgumentException>("value", () => format.AbbreviatedMonthGenitiveNames = value);
        }

        [Fact]
        public void AbbreviatedMonthGenitiveNames_SetReadOnly_ThrowsInvalidOperationException()
        {
            Assert.Throws<InvalidOperationException>(() => DateTimeFormatInfo.InvariantInfo.AbbreviatedMonthGenitiveNames = new string[] { "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12", "" });
        }
        
        [Fact]
        public void AbbreviatedMonthGenitiveNames_Format_ReturnsExpected()
        {
            var format = new DateTimeFormatInfo();
            format.AbbreviatedMonthGenitiveNames = new string[] { "GenJan", "GenFeb", "GenMar", "GenApr", "GenMay", "GenJun", "GenJul", "GenAug", "GenSep", "GenOct", "GenNov", "GenDec", "Gen" };
            
            var dateTime = new DateTime(1976, 6, 19);
            Assert.Equal("19 GenJun 76", dateTime.ToString("d MMM yy", format));
        }

        [Fact]
        public void AbbreviatedMonthGenitiveNames_FormatWithNull_ThrowsNullReferenceException()
        {
            var value = new string[] { "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12", "13" };
            var format = new DateTimeFormatInfo
            {
                AbbreviatedMonthGenitiveNames = value
            };
            value[0] = null;

            var dateTime = new DateTime(2014, 1, 28);
            Assert.Throws<NullReferenceException>(() => dateTime.ToString("dd MMM yy", format));
        }
    }
}
