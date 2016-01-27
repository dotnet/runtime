// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Net.Sockets
{
    internal enum SendPacketsElementFlags : uint
    {
        File = Interop.Winsock.TransmitPacketsElementFlags.File,
        Memory = Interop.Winsock.TransmitPacketsElementFlags.Memory,
        EndOfPacket = Interop.Winsock.TransmitPacketsElementFlags.EndOfPacket,
    }
}
