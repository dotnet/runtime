// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Net.Quic.Implementations.Managed.Internal.Streams
{
    /// <summary>
    ///     Type of the stream.
    /// </summary>
    internal enum StreamType : uint
    {
        ClientInitiatedBidirectional = 0x0,
        ServerInitiatedBidirectional = 0x1,
        ClientInitiatedUnidirectional = 0x2,
        ServerInitiatedUnidirectional = 0x3,
    }
}
