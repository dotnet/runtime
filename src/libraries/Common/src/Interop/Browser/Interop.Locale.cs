// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

internal static partial class Interop
{
    internal static unsafe partial class JsGlobalization
    {
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern unsafe int GetCultureInfo(in string culture, char* buffer, int bufferLength, out int exceptionalResult, out object result);
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern unsafe int GetFirstDayOfWeek(in string culture, out int exceptionalResult, out object result);
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern unsafe int GetFirstWeekOfYear(in string culture, out int exceptionalResult, out object result);
    }
}
