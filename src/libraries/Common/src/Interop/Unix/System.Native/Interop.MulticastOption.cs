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
        internal enum MulticastOption : int
        {
            MULTICAST_ADD = 0,
            MULTICAST_DROP = 1,
            MULTICAST_IF = 2
        }

        internal struct IPv4MulticastOption
        {
            public uint MulticastAddress;
            public uint LocalAddress;
            public int InterfaceIndex;
            private int _padding;
        }

        internal struct IPv6MulticastOption
        {
            public IPAddress Address;
            public int InterfaceIndex;
            private int _padding;
        }
       
        [DllImport(Libraries.SystemNative, EntryPoint = "SystemNative_GetIPv4MulticastOption")]
        internal static extern unsafe Error GetIPv4MulticastOption(SafeHandle socket, MulticastOption multicastOption, IPv4MulticastOption* option);

        [DllImport(Libraries.SystemNative, EntryPoint = "SystemNative_SetIPv4MulticastOption")]
        internal static extern unsafe Error SetIPv4MulticastOption(SafeHandle socket, MulticastOption multicastOption, IPv4MulticastOption* option);

        [DllImport(Libraries.SystemNative, EntryPoint = "SystemNative_GetIPv6MulticastOption")]
        internal static extern unsafe Error GetIPv6MulticastOption(SafeHandle socket, MulticastOption multicastOption, IPv6MulticastOption* option);

        [DllImport(Libraries.SystemNative, EntryPoint = "SystemNative_SetIPv6MulticastOption")]
        internal static extern unsafe Error SetIPv6MulticastOption(SafeHandle socket, MulticastOption multicastOption, IPv6MulticastOption* option);
    }
}
