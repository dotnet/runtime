// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Globalization;

internal static partial class Interop
{
    internal static partial class Globalization
    {
        // Mono-WASM doesn't support managed callbacks for pinvokes yet
        internal static bool EnumCalendarInfo(EnumCalendarInfoCallback callback, string localeName, CalendarId calendarId, CalendarDataType calendarDataType, IntPtr context) => false;
    }
}
