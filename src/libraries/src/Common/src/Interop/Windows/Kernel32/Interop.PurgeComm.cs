// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Win32.SafeHandles;
using System.Runtime.InteropServices;

internal partial class Interop
{
    internal partial class Kernel32
    {
        internal static class PurgeFlags
        {
            internal const uint PURGE_TXABORT = 0x0001;  // Kill the pending/current writes to the comm port.
            internal const uint PURGE_RXABORT = 0x0002;  // Kill the pending/current reads to the comm port.
            internal const uint PURGE_TXCLEAR = 0x0004;  // Kill the transmit queue if there.
            internal const uint PURGE_RXCLEAR = 0x0008;  // Kill the typeahead buffer if there.
        }

        [DllImport(Libraries.Kernel32, SetLastError=true, CharSet=CharSet.Auto)]
        internal static extern bool PurgeComm(
            SafeFileHandle hFile,
            uint dwFlags);
    }
}