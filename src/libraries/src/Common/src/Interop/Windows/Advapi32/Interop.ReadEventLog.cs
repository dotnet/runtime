// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

internal partial class Interop
{
    internal partial class Advapi32
    {
        internal const int SEEK_READ = 0x2;
        internal const int FORWARDS_READ = 0x4;
        internal const int BACKWARDS_READ = 0x8;

        [DllImport(Libraries.Advapi32, CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool ReadEventLog(
            SafeEventLogReadHandle hEventLog,
            int dwReadFlags,
            int dwRecordOffset,
            byte[] lpBuffer,
            int nNumberOfBytesRead,
            out int pnBytesRead,
            out int pnMinNumberOfBytesNeeded);
    }
}
