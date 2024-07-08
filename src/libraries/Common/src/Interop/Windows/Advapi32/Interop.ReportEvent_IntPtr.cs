// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Advapi32
    {
        [LibraryImport(Libraries.Advapi32, EntryPoint = "ReportEventW", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool ReportEvent(
            IntPtr hEventLog,
            short wType,
            ushort wcategory,
            uint dwEventID,
            byte[] lpUserSid,
            short wNumStrings,
            int dwDataSize,
            IntPtr lpStrings,
            byte[] lpRawData);
    }
}
