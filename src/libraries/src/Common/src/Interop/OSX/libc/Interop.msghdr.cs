// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;
using System.Text;

using socklen_t = System.UInt32;

internal static partial class Interop
{
    internal static partial class libc
    {
        public unsafe struct msghdr
        {
            public void* msg_name;
            public socklen_t msg_namelen;
            public iovec* msg_iov;
            public int msg_iovlen;
            public void* msg_control;
            public socklen_t msg_controllen;
            public int msg_flags;

            public msghdr(void* name, uint namelen, iovec* iov, int iovlen, void* control, int controllen, int flags)
            {
                msg_name = name;
                msg_namelen = (socklen_t)namelen;
                msg_iov = iov;
                msg_iovlen = iovlen;
                msg_control = control;
                msg_controllen = (socklen_t)controllen;
                msg_flags = flags;
            }
        }
    }
}
