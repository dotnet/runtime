// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Sys
    {
        [DllImport(Libraries.SystemNative, EntryPoint = "SystemNative_FdSetSize")]
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
            Debug.Assert(fdset != null, "fdset was null");

            fdset[fd / FD_SETSIZE_UINTS] |= (1u << (fd % FD_SETSIZE_UINTS));
        }

        internal static unsafe bool FD_ISSET(int fd, uint* fdset)
        {
            if (fd >= FD_SETSIZE_BITS)
            {
                ThrowInvalidFileDescriptor(fd);
            }
            Debug.Assert(fdset != null, "fdset was null");

            return (fdset[fd / FD_SETSIZE_UINTS] & (1u << (fd % FD_SETSIZE_UINTS))) != 0;
        }

        internal static unsafe void FD_ZERO(uint* fdset)
        {
            Debug.Assert(fdset != null, "fdset was null");
            Interop.Sys.MemSet(fdset, 0, (UIntPtr)FD_SETSIZE_BYTES);
        }

        private static void ThrowInvalidFileDescriptor(int fd)
        {
            throw new ArgumentOutOfRangeException(nameof(fd), fd, SR.net_InvalidSocketHandle);
        }
    }
}
