// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Sys
    {
        internal const int AF_INET = 2;
        internal const int AF_INET6 = 10;

        [StructLayout(LayoutKind.Sequential)]
        internal unsafe struct sockaddr_in
        {
            public ushort sin_family;
            public ushort sin_port;
            public fixed byte sin_addr[4];

            public void SetAddressFamily(int family)
            {
                sin_family = (ushort)family;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        internal unsafe struct sockaddr_in6
        {
            public ushort sin6_family;
            public ushort sin6_port;
            public uint sin6_flowinfo;
            public fixed byte sin6_addr[16];
            public uint sin6_scope_id;

            public void SetAddressFamily(int family)
            {
                sin6_family = (ushort)family;
            }
        }
    }
}
