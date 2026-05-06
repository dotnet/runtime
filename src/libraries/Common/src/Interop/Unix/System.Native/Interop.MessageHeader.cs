// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net.Sockets;

internal static partial class Interop
{
    internal static partial class Sys
    {
        internal unsafe struct MessageHeader
        {
            public byte* SocketAddress;
            public IOVector* IOVectors;
            public byte* ControlBuffer;
            public int SocketAddressLen;
            public int IOVectorCount;
            public int ControlBufferLen;
            public SocketFlags Flags;
        }
    }
}
