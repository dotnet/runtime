// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;

internal partial class Interop
{
    internal partial class mincore
    {
        [DllImport(Libraries.Console_L2, SetLastError = true)]
        internal static extern bool SetConsoleScreenBufferSize(IntPtr hConsoleOutput, Interop.mincore.COORD size);
    }
}
