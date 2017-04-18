// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Globalization.Tests
{
    public class DateTimeFormatInfoDayNames
    {
        [Fact]
        public void DayNames_InvariantInfo()
        {
            Assert.Equal(new string[] { "Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday" }, DateTimeFormatInfo.InvariantInfo.DayNames);
        }

        [Fact]
        public void DayNames_Set()
        {
            string[] newDayNames = new string[] { "1", "2", "3", "4", "5", "6", "7" };
            var format = new DateTimeFormatInfo();
            format.DayNames = newDayNames;
            Assert.Equal(newDayNames, format.DayNames);
        }

        [Fact]
        public void DayNames_Set_Invalid()
        {
            AssertExtensions.Throws<ArgumentNullException>("value", () => new DateTimeFormatInfo().DayNames = null); // Value is null
            AssertExtensions.Throws<ArgumentNullException>("value", () => new DateTimeFormatInfo().DayNames = new string[] { "1", "2", "3", null, "5", "6", "7" }); // Value has null
            AssertExtensions.Throws<ArgumentException>("value", () => new DateTimeFormatInfo().DayNames = new string[] { "sun" }); // Value.Length is not 7

            // DateTimeFormatInfo.InvariantInfo is read only
            Assert.Throws<InvalidOperationException>(() => DateTimeFormatInfo.InvariantInfo.DayNames = new string[] { "1", "2", "3", "4", "5", "6", "7" });
        }
    }
}
