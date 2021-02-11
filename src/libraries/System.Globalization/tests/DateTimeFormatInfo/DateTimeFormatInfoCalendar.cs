// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Globalization.Tests
{
    public class DateTimeFormatInfoCalendar
    {
        [Fact]
        public void Calendar_GetInvariantInfo_ReturnsExpected()
        {
            Calendar calendar = DateTimeFormatInfo.InvariantInfo.Calendar;
            Assert.IsType<GregorianCalendar>(calendar);
            Assert.True(calendar.IsReadOnly);
        }

        [Fact]
        public void Calendar_Set_GetReturnsExpected()
        {
            Calendar newCalendar = new GregorianCalendar(GregorianCalendarTypes.Localized);
            var format = new DateTimeFormatInfo();
            format.Calendar = newCalendar;
            Assert.Equal(newCalendar, format.Calendar);

            format.Calendar = newCalendar;
            Assert.Equal(newCalendar, format.Calendar);
        }

        [Fact]
        public void Calendar_SetNullValue_ThrowsArgumentNullException()
        {
            var format = new DateTimeFormatInfo();
            AssertExtensions.Throws<ArgumentNullException>("value", () => format.Calendar = null);
        }

        [Fact]
        public void Calendar_SetInvalidValue_ThrowsArgumentOutOfRangeException()
        {
            var format = new DateTimeFormatInfo();
            AssertExtensions.Throws<ArgumentOutOfRangeException>("value", () => format.Calendar = new ThaiBuddhistCalendar());
        }

        [Fact]
        public void Calendar_SetReadOnly_ThrowsInvalidOperationException()
        {
            Assert.Throws<InvalidOperationException>(() => DateTimeFormatInfo.InvariantInfo.Calendar = new GregorianCalendar());
        }
    }
}
