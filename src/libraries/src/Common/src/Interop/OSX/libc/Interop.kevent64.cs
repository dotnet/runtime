// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class libc
    {
        public const ushort EV_ADD = 0x0001;
        public const ushort EV_ENABLE = 0x0004;
        public const ushort EV_DISABLE = 0x0008;
        public const ushort EV_DELETE = 0x0002;
        public const ushort EV_RECEIPT = 0x0040;
        public const ushort EV_ONESHOT = 0x0010;
        public const ushort EV_CLEAR = 0x0020;
        public const ushort EV_EOF = 0x8000;
        public const ushort EV_OOBAND = 0x2000;
        public const ushort EV_ERROR = 0x4000;

        public const short EVFILT_READ = -1;
        public const short EVFILT_WRITE = -2;

        public unsafe struct kevent64_s
        {
            public ulong ident;        // identifier for this event
            public short filter;       // filter for event
            public ushort flags;       // general flags
            public uint fflags;        // filter-specific flags
            public long data;          // filter-specific data
            public ulong udata;        // opaque user data identifier
            public fixed ulong ext[2]; // filter-specific extensions
        }

        [DllImport(Libraries.Libc, SetLastError = true)]
        public static extern unsafe int kevent64(int kq, kevent64_s* changelist, int nchanges, kevent64_s* eventlist, int nevents, uint flags, timespec* timeout);
    }
}
