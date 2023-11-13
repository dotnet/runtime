// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class Kernel32
    {
        internal static class CommModemState
        {
            internal const int MS_CTS_ON = 0x10;
            internal const int MS_DSR_ON = 0x20;
            internal const int MS_RLSD_ON = 0x80;
        }

        [LibraryImport(Libraries.Kernel32, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool GetCommModemStatus(
            SafeFileHandle hFile,
            ref int lpModemStat);
    }
}
