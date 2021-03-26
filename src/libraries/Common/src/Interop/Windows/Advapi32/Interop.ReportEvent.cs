// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class Advapi32
    {
        [DllImport(Libraries.Advapi32, CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool ReportEvent(
            SafeEventLogWriteHandle hEventLog,
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
