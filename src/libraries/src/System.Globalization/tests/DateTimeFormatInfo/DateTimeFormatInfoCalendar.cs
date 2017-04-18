// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Globalization.Tests
{
    public class DateTimeFormatInfoCalendar
    {
        [Fact]
        public void Calendar_InvariantInfo()
        {
            Calendar calendar = DateTimeFormatInfo.InvariantInfo.Calendar;
            Assert.IsType<GregorianCalendar>(calendar);
            Assert.True(calendar.IsReadOnly);
        }

        [Fact]
        public void Calendar_Set()
        {
            Calendar newCalendar = new GregorianCalendar(GregorianCalendarTypes.Localized);
            var format = new DateTimeFormatInfo();
            format.Calendar = newCalendar;
            Assert.Equal(newCalendar, format.Calendar);

            format.Calendar = newCalendar;
            Assert.Equal(newCalendar, format.Calendar);
        }

        [Fact]
        public void Calendar_Set_Invalid()
        {
            AssertExtensions.Throws<ArgumentNullException>("value", () => new DateTimeFormatInfo().Calendar = null); // Value is null
            AssertExtensions.Throws<ArgumentOutOfRangeException>("value", () => new DateTimeFormatInfo().Calendar = new ThaiBuddhistCalendar()); // Value is invalid
            Assert.Throws<InvalidOperationException>(() => DateTimeFormatInfo.InvariantInfo.Calendar = new GregorianCalendar()); // DateTimeFormatInfo.InvariantInfo is read only
        }
    }
}
