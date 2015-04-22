// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class libc
    {
        [Flags]
        internal enum MemoryMappedFlags
        {
            MAP_FILE = 0x0,
            MAP_SHARED = 0x01,
            MAP_PRIVATE = 0x02,
            MAP_FIXED = 0x10,
            MAP_ANONYMOUS = 0x1000,
        }
    }
}
