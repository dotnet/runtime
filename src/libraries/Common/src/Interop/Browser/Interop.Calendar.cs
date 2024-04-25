// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Runtime.CompilerServices;

internal static partial class Interop
{
    internal static unsafe partial class JsGlobalization
    {
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern unsafe nint GetCalendarInfo(char* culture, int cultureLength, CalendarId calendarId, char* buffer, int bufferMaxLength, out int bufferLength);
    }
}
