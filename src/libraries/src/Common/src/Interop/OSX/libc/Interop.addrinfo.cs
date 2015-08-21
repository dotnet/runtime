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
        public const int AI_CANONNAME = 0x0002;   // Request for canonical name.
        public const int AI_NUMERICHOST = 0x0004; // Don't use name resolution.
        public const int AI_NUMERICSERV = 0x1000; // Don't use name resolution.

        public const int NI_MAXHOST = 1025;
        public const int NI_NUMERICHOST = 2; // Don't try to look up hostname.
        public const int NI_NAMEREQD = 4;    // Don't return numeric addresses.

        public const int EAI_AGAIN = 2;    // Temporary failure in name resolution.
        public const int EAI_BADFLAGS = 3; // Invalid value for `ai_flags' field.
        public const int EAI_FAIL = 4;     // Non-recoverable failure in name resolution.
        public const int EAI_FAMILY = 5;   // 'ai_family' not supported.
        public const int EAI_NONAME = 8;   // NAME or SERVICE is unknown.

        public unsafe struct addrinfo
        {
            public int ai_flags;
            public int ai_family;
            public int ai_socktype;
            public int ai_protocol;
            public socklen_t ai_addrlen;
            public byte* ai_canonname;
            public sockaddr* ai_addr;
            public addrinfo* ai_next;
        }
    }
}
