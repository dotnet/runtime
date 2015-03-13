// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;

internal partial class Interop
{
    internal partial class mincore
    {
        [DllImport(Libraries.Console_L1, CharSet = CharSet.Unicode, SetLastError = true, EntryPoint = "ReadConsoleW")]
        internal static unsafe extern bool ReadConsole(
            IntPtr hConsoleInput,
            Byte* lpBuffer,
            Int32 nNumberOfCharsToRead,
            out Int32 lpNumberOfCharsRead,
            IntPtr pInputControl);
    }
}
