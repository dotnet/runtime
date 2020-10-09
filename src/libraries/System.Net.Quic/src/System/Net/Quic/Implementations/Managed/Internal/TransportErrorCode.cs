// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Net.Quic.Implementations.Managed.Internal
{
    /// <summary>
    ///     Defines error codes which can be used within CONNECTION_CLOSE frame. These errors apply to entire connection.
    /// </summary>
    internal enum TransportErrorCode
    {
        /// <summary>No error</summary>
        NoError = 0x0,

        /// <summary>Implementation error</summary>
        InternalError = 0x1,

        /// <summary>Server currently busy</summary>
        ServerBusy = 0x2,

        /// <summary>Flow control error</summary>
        FlowControlError = 0x3,

        /// <summary>Too many streams opened</summary>
        StreamLimitError = 0x4,

        /// <summary>Frame received in invalid stream state</summary>
        StreamStateError = 0x5,

        /// <summary>Change to final size</summary>
        FinalSizeError = 0x6,

        /// <summary>Frame encoding error</summary>
        FrameEncodingError = 0x7,

        /// <summary>Error in transport parameters</summary>
        TransportParameterError = 0x8,

        /// <summary>Too many connection IDs received</summary>
        ConnectionIdLimitError = 0x9,

        /// <summary>Generic protocol violation</summary>
        ProtocolViolation = 0xA,

        /// <summary>Invalid token received</summary>
        InvalidToken = 0xB,

        /// <summary>CRYPTO frame data buffer overflowed</summary>
        CryptoBufferExceeded = 0xD
    }
}
