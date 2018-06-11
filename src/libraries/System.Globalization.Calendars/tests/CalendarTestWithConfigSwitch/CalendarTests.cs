// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using Xunit;

namespace System.Globalization.Tests
{
    public static class CalendarTests
    {
        [Fact]
        public static void TestJapaneseCalendarDateParsing()
        {
            CultureInfo ciJapanese = new CultureInfo("ja-JP") { DateTimeFormat = { Calendar = new JapaneseCalendar() } };

            DateTime dt = new DateTime(1970, 1, 1);
            string eraName = dt.ToString("gg", ciJapanese);

            // Legacy behavior which we used to throw when using a year number exceeding the era max year.
            Assert.ThrowsAny<FormatException>(() => DateTime.Parse(eraName + " 70/1/1 0:00:00", ciJapanese));
        }
    }
}
