// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class libc
    {
        public const uint EPOLLIN = 0x001;
        public const uint EPOLLOUT = 0x004;
        public const uint EPOLLERR = 0x008;
        public const uint EPOLLHUP = 0x010;
        public const uint EPOLLRDHUP = 0x2000;
        public const uint EPOLLET = 0x80000000;

        public const int EPOLL_CTL_ADD = 1;
        public const int EPOLL_CTL_DEL = 2;
        public const int EPOLL_CTL_MOD = 3;

        public const int EPOLL_CLOEXEC = 0x80000;

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public unsafe struct epoll_event
        {
            public uint events;
            public IntPtr data;
        }
    }
}
