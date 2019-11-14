// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
