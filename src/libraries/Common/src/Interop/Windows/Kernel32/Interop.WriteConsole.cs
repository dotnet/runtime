// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Kernel32
    {
        [DllImport(Libraries.Kernel32, CharSet = CharSet.Unicode, SetLastError = true, EntryPoint = "WriteConsoleW")]
        internal static extern unsafe bool WriteConsole(
            IntPtr hConsoleOutput,
            byte* lpBuffer,
            int nNumberOfCharsToWrite,
            out int lpNumberOfCharsWritten,
            IntPtr lpReservedMustBeNull);
    }
}
