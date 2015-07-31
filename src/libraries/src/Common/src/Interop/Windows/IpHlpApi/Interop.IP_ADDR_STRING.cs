// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class IpHlpApi
    {
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct IP_ADDR_STRING
        {
            public IntPtr Next; // struct _IpAddressList*
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
            public string IpAddress;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
            public string IpMask;
            public uint Context;
        }
    }
}
