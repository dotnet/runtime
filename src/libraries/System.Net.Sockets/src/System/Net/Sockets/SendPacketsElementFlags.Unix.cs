// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Net.Sockets
{
    // NOTE: these values are arbitrary. The APIs that consume SendPacketsElement values are not
    // supported on *nix.
    internal enum SendPacketsElementFlags : uint
    {
        File = 0,
        Memory = 1,
        EndOfPacket = 2,
    }
}
