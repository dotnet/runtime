// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using Xunit;

namespace System.Globalization.Tests
{
    public class CultureInfoCalendar
    {
        [Theory]
        [InlineData("en-US", "System.Globalization.GregorianCalendar")]
        [InlineData("th-TH", "System.Globalization.ThaiBuddhistCalendar")]
        public void TestDefaultCalendar(string locale, string defaultCalendarName)
        {
            CultureInfo cultureInfo = new CultureInfo(locale);
            Assert.Equal(defaultCalendarName, cultureInfo.Calendar.ToString());
        }
    }
}
