// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Win32.SafeHandles;
using System.Runtime.InteropServices;

internal partial class Interop
{
    internal partial class Kernel32
    {
        internal const int MAXDWORD = -1; // This is 0xfffffff, or UInt32.MaxValue, here used as an int

        [DllImport(Libraries.Kernel32, SetLastError=true, CharSet=CharSet.Auto)]
        internal static extern bool SetCommTimeouts(
            SafeFileHandle hFile,
            ref COMMTIMEOUTS lpCommTimeouts);
    }
}