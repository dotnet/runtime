// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Globalization
    {
        [LibraryImport(Libraries.GlobalizationNative, EntryPoint = "GlobalizationNative_GetTimeZoneDisplayName", StringMarshalling = StringMarshalling.Utf16)]
        internal static unsafe partial ResultCode GetTimeZoneDisplayName(
            string localeName,
            string timeZoneId,
            TimeZoneDisplayNameType type,
            char* result,
            int resultLength);

        [LibraryImport(Libraries.GlobalizationNative, EntryPoint = "GlobalizationNative_WindowsIdToIanaId", StringMarshalling = StringMarshalling.Utf16)]
        internal static unsafe partial int WindowsIdToIanaId(string windowsId, IntPtr region, char* ianaId, int ianaIdLength);

        [LibraryImport(Libraries.GlobalizationNative, EntryPoint = "GlobalizationNative_IanaIdToWindowsId", StringMarshalling = StringMarshalling.Utf16)]
        internal static unsafe partial int IanaIdToWindowsId(string ianaId, char* windowsId, int windowsIdLength);
    }
}
