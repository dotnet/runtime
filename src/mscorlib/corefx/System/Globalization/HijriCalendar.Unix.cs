// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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