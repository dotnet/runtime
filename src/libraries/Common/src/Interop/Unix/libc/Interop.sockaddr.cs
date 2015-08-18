// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;
using System.Text;

using in_port_t = System.UInt16;
using sa_family_t = System.UInt16;

internal static partial class Interop
{
    internal static partial class libc
    {
        public const sa_family_t AF_UNSPEC = 0; // Unspecified.
        public const sa_family_t AF_UNIX = 1;   // Local to host.
        public const sa_family_t AF_INET = 2;   // IP protocol family.
        public const sa_family_t AF_INET6 = 10; // IP version 6.

        // Disable CS0169 (The field 'Interop.libc.sockaddr_in.padding' is never used) and CS0649
        // (Field 'Interop.libc.sockaddr.sa_family' is never assigned to, and will always have its
        // default value 0)
#pragma warning disable 169, 649

        // NOTE: this type is incomplete, and its values should never be used directly.
        // Specific sockaddr types (e.g. sockaddr_in or sockaddr_in6) should be used instead.
        public unsafe struct sockaddr
        {
            public sa_family_t sa_family;
        }

        public struct in_addr
        {
            public uint s_addr; // Address in network byte order.
        }

        public struct sockaddr_in
        {
            public const int Size = 16;

            public sa_family_t sin_family; // Address family: AF_INET
            public in_port_t sin_port;     // Port in network byte order
            public in_addr sin_addr;       // Internet address
            private ulong padding;         // 8 bytes of padding
        }

        public unsafe struct in6_addr
        {
            public fixed byte s6_addr[16]; // IPv6 address
        }

        public struct sockaddr_in6
        {
            public const int Size = 28;

            public sa_family_t sin6_family; // AF_INET6
            public in_port_t sin6_port;     // Port number
            public uint sin6_flowinfo;      // IPv6 flow information
            public in6_addr sin6_addr;      // IPv6 address
            public uint sin6_scope_id;      // Scope ID
        }

#pragma warning restore 169, 649
    }
}
