// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;
using System.Text;

// NOTE: this is only correct for 64-bit platforms!
using __fd_mask = System.UInt64;

internal static partial class Interop
{
    internal static partial class libc
    {
        public unsafe struct fd_set
        {
            const int __FD_SETSIZE = 1024;
            const int __NFDBITS = 8 * sizeof(__fd_mask);

            private fixed __fd_mask __fds_bits[__FD_SETSIZE / __NFDBITS];

            public void Set(int fd)
            {
                fixed (__fd_mask* bits = __fds_bits)
                {
                    bits[fd / __NFDBITS] |= (__fd_mask)(1UL << (fd % __NFDBITS));
                }
            }

            public bool IsSet(int fd)
            {
                fixed (__fd_mask* bits = __fds_bits)
                {
                    return (bits[fd / __NFDBITS] & (__fd_mask)(1UL << (fd % __NFDBITS))) != 0;
                }
            }
        }
    }
}
