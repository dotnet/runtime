// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
