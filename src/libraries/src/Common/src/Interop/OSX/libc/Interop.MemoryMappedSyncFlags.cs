// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

internal static partial class Interop
{
    internal static partial class libc
    {
        [Flags]
        internal enum MemoryMappedSyncFlags
        {
            MS_ASYNC = 0x1,
            MS_INVALIDATE = 0x2,
            MS_SYNC = 0x10
        }
    }
}
