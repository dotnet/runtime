// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
