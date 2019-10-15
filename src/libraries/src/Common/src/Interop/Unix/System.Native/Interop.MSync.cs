// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Sys
    {
        [Flags]
        internal enum MemoryMappedSyncFlags
        {
            MS_ASYNC = 0x1,
            MS_SYNC  = 0x2,
            MS_INVALIDATE = 0x10,
        }

        [DllImport(Libraries.SystemNative, EntryPoint = "SystemNative_MSync", SetLastError = true)]
        internal static extern int MSync(IntPtr addr, ulong len, MemoryMappedSyncFlags flags);
    }
}
