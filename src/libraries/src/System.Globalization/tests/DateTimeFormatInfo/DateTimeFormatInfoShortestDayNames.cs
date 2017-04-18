// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Globalization.Tests
{
    public class DateTimeFormatInfoShortestDayNames
    {
        [Fact]
        public void ShortestDayNames_InvariantInfo()
        {
            Assert.Equal(new string[] { "Su", "Mo", "Tu", "We", "Th", "Fr", "Sa" }, DateTimeFormatInfo.InvariantInfo.ShortestDayNames);
        }

        [Fact]
        public void ShortestDayNames_Set()
        {
            string[] newShortestDayNames = new string[] { "1", "2", "3", "4", "5", "6", "7" };
            var format = new DateTimeFormatInfo();
            format.ShortestDayNames = newShortestDayNames;
            Assert.Equal(newShortestDayNames, format.ShortestDayNames);
        }

        [Fact]
        public void ShortestDayNames_Set_Invalid()
        {
            AssertExtensions.Throws<ArgumentNullException>("value", () => new DateTimeFormatInfo().ShortestDayNames = null); // Value is null
            AssertExtensions.Throws<ArgumentNullException>("value", () => new DateTimeFormatInfo().ShortestDayNames = new string[] { "1", "2", "3", null, "5", "6", "7" }); // Value has null
            AssertExtensions.Throws<ArgumentException>("value", () => new DateTimeFormatInfo().ShortestDayNames = new string[] { "su" }); // Value.Length is not 7

            // DateTimeFormatInfo.InvariantInfo is read only
            Assert.Throws<InvalidOperationException>(() => DateTimeFormatInfo.InvariantInfo.ShortestDayNames = new string[] { "1", "2", "3", "4", "5", "6", "7" });
        }
    }
}
