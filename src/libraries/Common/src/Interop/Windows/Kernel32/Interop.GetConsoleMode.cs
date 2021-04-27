// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Kernel32
    {
        [DllImport(Libraries.Kernel32, SetLastError = true)]
        internal static extern bool GetConsoleMode(IntPtr handle, out int mode);

        internal static bool IsGetConsoleModeCallSuccessful(IntPtr handle)
        {
            int mode;
            return GetConsoleMode(handle, out mode);
        }

        [DllImport(Libraries.Kernel32, SetLastError = true)]
        internal static extern bool SetConsoleMode(IntPtr handle, int mode);

        internal const int ENABLE_PROCESSED_INPUT = 0x0001;
        internal const uint ENABLE_VIRTUAL_TERMINAL_PROCESSING = 0x0004;
        internal const int STD_OUTPUT_HANDLE = -11;
    }
}
