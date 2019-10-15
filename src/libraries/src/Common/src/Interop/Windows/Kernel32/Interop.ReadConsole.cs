// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;

internal partial class Interop
{
    internal partial class Kernel32
    {
        [DllImport(Libraries.Kernel32, CharSet = CharSet.Unicode, SetLastError = true, EntryPoint = "ReadConsoleW")]
        internal static extern unsafe bool ReadConsole(
            IntPtr hConsoleInput,
            byte* lpBuffer,
            int nNumberOfCharsToRead,
            out int lpNumberOfCharsRead,
            IntPtr pInputControl);
    }
}
