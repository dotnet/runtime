// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Globalization.Tests
{
    public class DateTimeFormatInfoMonthNames
    {
        [Fact]
        public void MonthGenitiveNames_InvariantInfo()
        {
            Assert.Equal(new string[] { "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December", "" }, DateTimeFormatInfo.InvariantInfo.MonthNames);
        }

        [Fact]
        public void MonthNames_Set()
        {
            string[] newMonthNames = new string[] { "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12", "" };
            var format = new DateTimeFormatInfo();
            format.MonthNames = newMonthNames;
            Assert.Equal(newMonthNames, format.MonthNames);
        }

        [Fact]
        public void MonthNames_Set_Invalid()
        {
            AssertExtensions.Throws<ArgumentNullException>("value", () => new DateTimeFormatInfo().MonthNames = null); // Value is null
            AssertExtensions.Throws<ArgumentNullException>("value", () => new DateTimeFormatInfo().MonthNames = new string[] { "1", "2", "3", null, "5", "6", "7", "8", "9", "10", "11", "12", "" }); // Value has null
            AssertExtensions.Throws<ArgumentException>("value", () => new DateTimeFormatInfo().MonthNames = new string[] { "Jan" }); // Value.Length is not 13

            // DateTimeFormatInfo.InvariantInfo is read only
            Assert.Throws<InvalidOperationException>(() => DateTimeFormatInfo.InvariantInfo.MonthNames = new string[] { "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12", "" });
        }
    }
}
