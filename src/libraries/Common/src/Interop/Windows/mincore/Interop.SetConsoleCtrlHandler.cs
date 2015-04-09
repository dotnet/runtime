// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Win32.SafeHandles;
using System;
using System.Runtime.InteropServices;

internal partial class Interop
{
    internal partial class mincore
    {
        internal const int CTRL_C_EVENT = 0;
        internal const int CTRL_BREAK_EVENT = 1;

        internal delegate bool ConsoleCtrlHandlerRoutine(int controlType);

        [DllImport(Libraries.Console_L1, SetLastError = true)]
        internal static extern bool SetConsoleCtrlHandler(ConsoleCtrlHandlerRoutine handler, bool addOrRemove);
    }
}
