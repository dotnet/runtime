// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Net.Quic.Implementations.Managed.Internal.Frames
{
    internal enum FrameType : ulong
    {
        // placing these here so they are not used by built in ToString on the FrameType when visualizing in debugger
        StreamOffBit = 0x04,
        StreamLenBit = 0x02,
        StreamFinBit = 0x01,

        Padding = 0x00,
        Ping = 0x01,
        Ack = 0x02,
        AckWithEcn = 0x03,
        ResetStream = 0x04,
        StopSending = 0x05,
        Crypto = 0x06,
        NewToken = 0x07,
        Stream = 0x08,
        StreamMask = Stream | StreamOffBit | StreamLenBit | StreamFinBit,
        MaxData = 0x10,
        MaxStreamData = 0x11,
        MaxStreamsBidirectional = 0x12,
        MaxStreamsUnidirectional = 0x13,
        DataBlocked = 0x14,
        StreamDataBlocked = 0x15,
        StreamsBlockedBidirectional = 0x16,
        StreamsBlockedUnidirectional = 0x17,
        NewConnectionId = 0x18,
        RetireConnectionId = 0x19,
        PathChallenge = 0x1a,
        PathResponse = 0x1b,
        ConnectionCloseQuic = 0x1c,
        ConnectionCloseApplication = 0x1d,
        HandshakeDone = 0x1e,
    }
}
