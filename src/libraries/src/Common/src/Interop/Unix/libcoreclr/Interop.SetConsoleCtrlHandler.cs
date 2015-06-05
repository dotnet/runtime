// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Win32.SafeHandles;
using System;
using System.Runtime.InteropServices;

internal partial class Interop
{
    internal partial class libcoreclr
    {
        internal const int CTRL_C_EVENT = 0;
        internal const int CTRL_BREAK_EVENT = 1;

        internal const int ERROR_NOT_ENOUGH_MEMORY = 0x8;
        internal const int ERROR_INVALID_PARAMETER = 0x57;

        internal delegate bool ConsoleCtrlHandlerRoutine(int controlType);

        [DllImport(Libraries.LibCoreClr, SetLastError = true)]
        internal static extern bool SetConsoleCtrlHandler(ConsoleCtrlHandlerRoutine handler, bool addOrRemove);
    }
}
