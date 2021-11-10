// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Globalization
    {
        [DllImport(Libraries.GlobalizationNative, CharSet = CharSet.Unicode, EntryPoint = "GlobalizationNative_GetTimeZoneDisplayName")]
        internal static extern unsafe ResultCode GetTimeZoneDisplayName(
            string localeName,
            string timeZoneId,
            TimeZoneDisplayNameType type,
            char* result,
            int resultLength);

        [DllImport(Libraries.GlobalizationNative, CharSet = CharSet.Unicode, EntryPoint = "GlobalizationNative_WindowsIdToIanaId")]
        internal static extern unsafe int WindowsIdToIanaId(string windowsId, IntPtr region, char* ianaId, int ianaIdLength);

        [DllImport(Libraries.GlobalizationNative, CharSet = CharSet.Unicode, EntryPoint = "GlobalizationNative_IanaIdToWindowsId")]
        internal static extern unsafe int IanaIdToWindowsId(string ianaId, char* windowsId, int windowsIdLength);
    }
}
