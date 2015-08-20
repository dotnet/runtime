// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class libc
    {
        [Flags]
        internal enum Pipe2Flags
        {
            O_CLOEXEC   = 0x80000,
        }

        [DllImport(Libraries.Libc, SetLastError = true)]
        internal static extern unsafe int pipe2(int* pipefd, Pipe2Flags flags);
    }
}
