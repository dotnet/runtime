// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;

internal partial class Interop
{
    internal partial class mincore
    {
        [DllImport(Libraries.Console_L1, SetLastError = true)]
        private extern static bool GetConsoleMode(IntPtr handle, out int mode);

        internal static bool IsGetConsoleModeCallSuccessful(IntPtr handle)
        {
            int mode;
            return GetConsoleMode(handle, out mode);
        }
    }
}
