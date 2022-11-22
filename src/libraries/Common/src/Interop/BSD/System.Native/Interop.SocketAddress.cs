// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Sys
    {
        [StructLayout(LayoutKind.Sequential)]
        internal unsafe struct sockaddr_in
        {
            public byte sin_len;
            public byte sin_family;
            public ushort sin_port;
            public fixed byte sin_addr[4];
            private fixed byte sin_zero[8];

            public void SetAddressFamily(int family)
            {
                sin_family = (byte)family;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        internal unsafe struct sockaddr_in6
        {
            public byte sin_len;
            public byte sin6_family;
            public ushort sin6_port;
            public uint sin6_flowinfo;
            public fixed byte sin6_addr[16];
            public uint sin6_scope_id;

            public void SetAddressFamily(int family)
            {
                sin6_family = (byte)family;
            }
        }
    }
}
