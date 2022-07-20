// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class IpHlpApi
    {
        [StructLayout(LayoutKind.Sequential)]
        public unsafe struct IP_ADDR_STRING
        {
            public IP_ADDR_STRING* Next;
            public fixed byte IpAddress[16];
            public fixed byte IpMask[16];
            public uint Context;
        }
    }
}
