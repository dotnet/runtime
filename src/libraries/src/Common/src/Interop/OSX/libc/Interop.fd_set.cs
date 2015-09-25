// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

internal static partial class Interop
{
    internal static partial class libc
    {
        public unsafe struct fd_set
        {
            const int __FD_SETSIZE = 1024;
            const int __NFDBITS = 8 * sizeof(uint);

            private fixed uint __fds_bits[((__FD_SETSIZE % __NFDBITS) == 0) ? (__FD_SETSIZE / __NFDBITS) : (__FD_SETSIZE / __NFDBITS) + 1];

            public void Set(int fd)
            {
                fixed (uint* bits = __fds_bits)
                {
                    bits[fd / __NFDBITS] |= (uint)(1 << (fd % __NFDBITS));
                }
            }

            public bool IsSet(int fd)
            {
                fixed (uint* bits = __fds_bits)
                {
                    return (bits[fd / __NFDBITS] & (uint)(1 << (fd % __NFDBITS))) != 0;
                }
            }
        }
    }
}
