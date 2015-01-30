// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Internal.Runtime.Augments;

namespace System.Globalization
{
    public partial class HijriCalendar : Calendar
    {
        public static int GetHijriDateAdjustment()
        {
            return WinRTInterop.Callbacks.GetHijriDateAdjustment();
        }
    }
}