// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Win32.SafeHandles;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Kernel32
    {
        internal const int MAXDWORD = -1; // This is 0xfffffff, or UInt32.MaxValue, here used as an int

        [GeneratedDllImport(Libraries.Kernel32, SetLastError = true)]
        internal static partial bool SetCommTimeouts(
            SafeFileHandle hFile,
            ref COMMTIMEOUTS lpCommTimeouts);
    }
}
