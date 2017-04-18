// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Globalization.Tests
{
    public class DateTimeFormatInfoAbbreviatedDayNames
    {
        [Fact]
        public void AbbreviatedDayNames_InvariantInfo()
        {
            Assert.Equal(new string[] { "Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat" }, DateTimeFormatInfo.InvariantInfo.AbbreviatedDayNames);
        }

        [Fact]
        public void AbbreviatedDayNames_Set()
        {
            string[] newAbbreviatedDayNames = new string[] { "1", "2", "3", "4", "5", "6", "7" };
            var format = new DateTimeFormatInfo();
            format.AbbreviatedDayNames = newAbbreviatedDayNames;
            Assert.Equal(newAbbreviatedDayNames, format.AbbreviatedDayNames);
        }

        [Fact]
        public void AbbreviatedDayNames_Set_Invalid()
        {
            AssertExtensions.Throws<ArgumentNullException>("value", () => new DateTimeFormatInfo().AbbreviatedDayNames = null); // Value is null
            AssertExtensions.Throws<ArgumentNullException>("value", () => new DateTimeFormatInfo().AbbreviatedDayNames = new string[] { "1", "2", "3", null, "5", "6", "7" }); // Value has null
            AssertExtensions.Throws<ArgumentException>("value", (() => new DateTimeFormatInfo().AbbreviatedDayNames = new string[] { "sun" })); // Value.Length is not 7

            // DateTimeFormatInfo.InvariantInfo is read only
            Assert.Throws<InvalidOperationException>(() => DateTimeFormatInfo.InvariantInfo.AbbreviatedDayNames = new string[] { "1", "2", "3", "4", "5", "6", "7" });
        }
    }
}
