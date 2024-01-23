// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Globalization
    {
        [LibraryImport(Libraries.HybridGlobalizationNative, EntryPoint = "GlobalizationNative_GetTimeZoneDisplayNameNative", StringMarshalling = StringMarshalling.Utf16)]
        internal static unsafe partial ResultCode GetTimeZoneDisplayNameNative(
            string localeName,
            int lNameLength,
            string timeZoneId,
            int idLength,
            TimeZoneDisplayNameType type,
            char* result,
            int resultLength);
    }
}
