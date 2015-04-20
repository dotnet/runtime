// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;

using size_t = System.IntPtr;

internal static partial class Interop
{
    internal static partial class libc
    {
        [DllImport(Libraries.Libc, SetLastError = true)]
        internal static extern int madvise(IntPtr addr, size_t length, MemoryMappedAdvice advice);

        internal enum MemoryMappedAdvice
        {
            MADV_NORMAL = 0,
            MADV_RANDOM = 1,
            MADV_SEQUENTIAL = 2,
            MADV_WILLNEED = 3,
            MADV_DONTNEED = 4,
            MADV_REMOVE = 9,
            MADV_DONTFORK = 10,
            MADV_DOFORK = 11
        }

    }
}
