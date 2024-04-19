// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Globalization;
namespace DebuggerTests
{
    public class DateTimeTest
    {
        public static void LocaleTest(string locale)
        {
            CultureInfo.CurrentCulture = new CultureInfo(locale, false);
            Console.WriteLine("CurrentCulture is {0}", CultureInfo.CurrentCulture.Name);

            DateTimeFormatInfo dtfi = CultureInfo.GetCultureInfo(locale).DateTimeFormat;
            var fdtp = dtfi.FullDateTimePattern;
            var ldp = dtfi.LongDatePattern;
            var ltp = dtfi.LongTimePattern;
            var sdp = dtfi.ShortDatePattern;
            var stp = dtfi.ShortTimePattern;

            DateTime dt = new DateTime(2020, 1, 2, 3, 4, 5);

            Console.WriteLine("Current time is {0}", dt);

            return;
        }
    }
}
