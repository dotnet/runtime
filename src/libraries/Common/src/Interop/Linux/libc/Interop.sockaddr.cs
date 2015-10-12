// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;
using System.Text;

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
        public struct in_addr
        {
            public uint s_addr; // Address in network byte order.
        }

        public unsafe struct in6_addr
        {
            public fixed byte s6_addr[16]; // IPv6 address
        }
#pragma warning restore 169, 649
    }
}
