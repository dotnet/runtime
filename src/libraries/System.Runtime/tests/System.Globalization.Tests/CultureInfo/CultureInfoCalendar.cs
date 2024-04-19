// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Globalization.Tests
{
    public class CultureInfoCalendar
    {
        [Fact]
        public void Calendar_Get_InvariantInfo()
        {
            CultureInfo cultureInfo = CultureInfo.InvariantCulture;
            Assert.IsType<GregorianCalendar>(cultureInfo.Calendar);
            Assert.Same(cultureInfo.Calendar, cultureInfo.Calendar);
        }

        [Fact]
        public void OptionalCalendars_Get_InvariantInfo()
        {
            CultureInfo cultureInfo = CultureInfo.InvariantCulture;
            Assert.Equal(1, cultureInfo.OptionalCalendars.Length);
            Assert.IsType<GregorianCalendar>(cultureInfo.OptionalCalendars[0]);
        }
    }
}
