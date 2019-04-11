// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Win32.SafeHandles;
using System.Runtime.InteropServices;

internal partial class Interop
{
    internal partial class Kernel32
    {
        internal static class CommEvents
        {
            internal const int EV_RXCHAR = 0x01;
            internal const int EV_ERR = 0x80;
            internal const int ALL_EVENTS = 0x1fb;
        }

        [DllImport(Libraries.Kernel32, SetLastError=true, CharSet=CharSet.Auto)]
        internal static extern bool SetCommMask(
            SafeFileHandle hFile,
            int dwEvtMask
        );
    }
}