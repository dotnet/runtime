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
        internal static extern int msync(IntPtr __addr, size_t __len, MemoryMappedSyncFlags __flags);

        [Flags]
        internal enum MemoryMappedSyncFlags
        {
            MS_ASYNC = 1,
            MS_INVALIDATE = 2,
            MS_SYNC = 4
        }
    }
}
