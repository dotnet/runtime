// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Xunit;

namespace System.Globalization.Tests
{
    public static class CalendarTests
    {
        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/36883", TestPlatforms.iOS | TestPlatforms.MacCatalyst | TestPlatforms.tvOS | TestPlatforms.Android)]
        public static void TestJapaneseCalendarDateParsing()
        {
            CultureInfo ciJapanese = new CultureInfo("ja-JP") { DateTimeFormat = { Calendar = new JapaneseCalendar() } };

            DateTime dt = new DateTime(1970, 1, 1);
            string eraName = dt.ToString("gg", ciJapanese);

            // Legacy behavior which we used to throw when using a year number exceeding the era max year.
            //
            // On mobile, this does not throw, but instead produces a DateTime w/ 95/01/01
            Assert.ThrowsAny<FormatException>(() => DateTime.Parse(eraName + " 70/1/1 0:00:00", ciJapanese));
        }
    }
}
