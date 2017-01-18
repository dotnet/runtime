// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;

internal partial class Interop
{
    internal partial class Kernel32
    {
        [DllImport(Libraries.Kernel32, SetLastError = true)]
        internal extern static bool GetConsoleMode(IntPtr handle, out int mode);

        internal static bool IsGetConsoleModeCallSuccessful(IntPtr handle)
        {
            int mode;
            return GetConsoleMode(handle, out mode);
        }

        [DllImport(Libraries.Kernel32, SetLastError = true)]
        internal static extern bool SetConsoleMode(IntPtr handle, int mode);

        internal const int ENABLE_PROCESSED_INPUT = 0x0001;
    }
}
