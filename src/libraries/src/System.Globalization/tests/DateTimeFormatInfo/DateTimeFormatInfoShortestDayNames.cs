// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
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
        public void ShortestDayNames_Get_ReturnsClone()
        {
            var format = new DateTimeFormatInfo();
            Assert.Equal(format.ShortestDayNames, format.ShortestDayNames);
            Assert.NotSame(format.ShortestDayNames, format.ShortestDayNames);
        }

        public static IEnumerable<object[]> ShortestDayNames_Set_TestData()
        {
            yield return new object[] { new string[] { "1", "2", "3", "4", "5", "6", "7" } };
            yield return new object[] { new string[] { "", "", "", "", "", "", "" } };
        }

        [Theory]
        [MemberData(nameof(ShortestDayNames_Set_TestData))]
        public void ShortestDayNames_Set_GetReturnsExpected(string[] value)
        {
            var format = new DateTimeFormatInfo();
            format.ShortestDayNames = value;
            Assert.Equal(value, format.ShortestDayNames);

            // Does not clone in setter, only in getter.
            value[0] = null;
            Assert.NotSame(value, format.ShortestDayNames);
            Assert.Equal(value, format.ShortestDayNames);
        }

        [Fact]
        public void ShortestDayNames_SetNulValue_ThrowsArgumentNullException()
        {
            var format = new DateTimeFormatInfo();
            AssertExtensions.Throws<ArgumentNullException>("value", () => format.ShortestDayNames = null);
        }

        [Fact]
        public void ShortestDayNames_SetNulValueInValue_ThrowsArgumentNullException()
        {
            var format = new DateTimeFormatInfo();
            AssertExtensions.Throws<ArgumentNullException>("value", () => format.ShortestDayNames = new string[] { "1", "2", "3", null, "5", "6", "7" });
        }

        public static IEnumerable<object[]> ShortestDayNames_SetInvalidLength_TestData()
        {
            yield return new object[] { new string[] { "Sun" } };
            yield return new object[] { new string[] { "Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Additional" } };
        }

        [Theory]
        [MemberData(nameof(ShortestDayNames_SetInvalidLength_TestData))]
        public void ShortestDayNames_SetInvalidLength_ThrowsArgumentException(string[] value)
        {
            var format = new DateTimeFormatInfo();
            AssertExtensions.Throws<ArgumentException>("value", (() => format.ShortestDayNames = value));
        }

        [Fact]
        public void ShortestDayNames_SetReadOnly_ThrowsInvalidOperationException()
        {
            Assert.Throws<InvalidOperationException>(() => DateTimeFormatInfo.InvariantInfo.ShortestDayNames = new string[] { "1", "2", "3", "4", "5", "6", "7" });
        }
    }
}
