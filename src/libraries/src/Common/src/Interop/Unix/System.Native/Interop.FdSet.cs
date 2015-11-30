// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Sys
    {
        [DllImport(Libraries.SystemNative)]
        private static extern int FdSetSize();

        private static readonly int FD_SETSIZE_BITS = FdSetSize();

        private const int BitsPerByte = 8;
        private static readonly int FD_SETSIZE_BYTES = FD_SETSIZE_BITS / BitsPerByte;

        internal static readonly int FD_SETSIZE_UINTS = FD_SETSIZE_BYTES / sizeof(uint);

        internal static unsafe void FD_SET(int fd, uint* fdset)
        {
            if (fd >= FD_SETSIZE_BITS)
            {
                ThrowInvalidFileDescriptor(fd);
            }
            Debug.Assert(fdset != null);

            fdset[fd / FD_SETSIZE_UINTS] |= (1u << (fd % FD_SETSIZE_UINTS));
        }

        internal static unsafe bool FD_ISSET(int fd, uint* fdset)
        {
            if (fd >= FD_SETSIZE_BITS)
            {
                ThrowInvalidFileDescriptor(fd);
            }
            Debug.Assert(fdset != null);

            return (fdset[fd / FD_SETSIZE_UINTS] & (1u << (fd % FD_SETSIZE_UINTS))) != 0;
        }

        internal static unsafe void FD_ZERO(uint* fdset)
        {
            Debug.Assert(fdset != null);
            Interop.Sys.MemSet(fdset, 0, (UIntPtr)FD_SETSIZE_BYTES);
        }

        private static void ThrowInvalidFileDescriptor(int fd)
        {
            throw new ArgumentOutOfRangeException("fd", fd, SR.net_InvalidSocketHandle);
        }
    }
}
