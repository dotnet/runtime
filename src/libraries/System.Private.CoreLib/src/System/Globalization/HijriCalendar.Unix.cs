// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Globalization
{
    public partial class HijriCalendar : Calendar
    {
        private static int GetHijriDateAdjustment()
        {
            // this setting is not supported on Unix, so always return 0
            return 0;
        }
    }
}
