// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;

internal partial class Interop
{
    internal partial class mincore
    {
        [StructLayoutAttribute(LayoutKind.Sequential)]
        internal struct CONSOLE_CURSOR_INFO
        {
            internal int dwSize;
            internal bool bVisible;
        }

        [DllImport(Libraries.Console_L2, SetLastError = true)]
        internal static extern bool GetConsoleCursorInfo(IntPtr hConsoleOutput, out CONSOLE_CURSOR_INFO cci);

        [DllImport(Libraries.Console_L2, SetLastError = true)]
        internal static extern bool SetConsoleCursorInfo(IntPtr hConsoleOutput, ref CONSOLE_CURSOR_INFO cci);
    }
}
