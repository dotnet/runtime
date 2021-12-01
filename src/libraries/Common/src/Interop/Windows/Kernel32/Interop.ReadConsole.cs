// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Kernel32
    {
        [GeneratedDllImport(Libraries.Kernel32, EntryPoint = "ReadConsoleW", CharSet = CharSet.Unicode, SetLastError = true)]
        internal static unsafe partial bool ReadConsole(
            IntPtr hConsoleInput,
            byte* lpBuffer,
            int nNumberOfCharsToRead,
            out int lpNumberOfCharsRead,
            IntPtr pInputControl);
    }
}
