// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

internal static partial class Interop
{
    internal static unsafe partial class JsGlobalization
    {
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern unsafe nint GetCultureInfo(in string culture, char* buffer, int bufferMaxLength, out int resultLength);
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern unsafe nint GetFirstDayOfWeek(in string culture, out int resultPtr);
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern unsafe nint GetFirstWeekOfYear(in string culture, out int resultPtr);
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern unsafe nint GetLocaleInfo(in string locale, in string culture, char* buffer, int bufferLength, out int resultLength);
    }
}
