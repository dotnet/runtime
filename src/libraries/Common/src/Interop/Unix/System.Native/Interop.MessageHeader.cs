// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Net.Sockets;

internal static partial class Interop
{
    internal static partial class Sys
    {
        internal unsafe struct IOVector
        {
            public byte* Base;
            public UIntPtr Count;
        }

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
