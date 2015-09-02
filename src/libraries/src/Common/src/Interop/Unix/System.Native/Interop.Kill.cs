// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Sys
    {
        internal enum Signals : int
        {
            None = 0,
            SIGKILL = 9,
        }

        [DllImport(Libraries.SystemNative, SetLastError = true)]
        internal static extern int Kill(int pid, Signals signal);
    }
}
