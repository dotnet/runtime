// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Kernel32
    {
        [StructLayoutAttribute(LayoutKind.Sequential)]
        internal struct CONSOLE_CURSOR_INFO
        {
            internal int dwSize;
            internal BOOL bVisible;
        }

        [GeneratedDllImport(Libraries.Kernel32, SetLastError = true)]
        internal static partial bool GetConsoleCursorInfo(IntPtr hConsoleOutput, out CONSOLE_CURSOR_INFO cci);

        [GeneratedDllImport(Libraries.Kernel32, SetLastError = true)]
        internal static partial bool SetConsoleCursorInfo(IntPtr hConsoleOutput, ref CONSOLE_CURSOR_INFO cci);
    }
}
