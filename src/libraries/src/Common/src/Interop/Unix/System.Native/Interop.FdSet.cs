// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Sys
    {
        public unsafe struct FdSet
        {
            const int FDSET_MAX_FDS = 1024;
            const int FDSET_NFD_BITS = FDSET_MAX_FDS / (8 * sizeof(uint));

            private fixed uint _bits[FDSET_MAX_FDS / FDSET_NFD_BITS];
  
            public void Set(int fd)
            {
                fixed (uint* bits = _bits)
                {
                    bits[fd / FDSET_NFD_BITS] |= (1u << (fd % FDSET_NFD_BITS));
                }
            }

            public bool IsSet(int fd)
            {
                fixed (uint* bits = _bits)
                {
                    return (bits[fd / FDSET_NFD_BITS] & (1U << (fd % FDSET_NFD_BITS))) != 0;
                }
            }
        }
    }
}
