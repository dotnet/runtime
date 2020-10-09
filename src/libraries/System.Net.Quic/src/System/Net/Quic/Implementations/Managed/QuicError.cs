// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Net.Quic.Implementations.Managed.Internal;

namespace System.Net.Quic.Implementations.Managed
{
    internal class QuicError
    {
        internal TransportErrorCode ErrorCode { get; }
        internal FrameType FrameType { get; }
        internal string? ReasonPhrase { get; }
        internal bool IsQuicError { get; }

        public QuicError(TransportErrorCode errorCode, string? reasonPhrase = null, FrameType frameType = FrameType.Padding,
            bool isQuicError = true)
        {
            ErrorCode = errorCode;
            FrameType = frameType;
            ReasonPhrase = reasonPhrase;
            IsQuicError = isQuicError;
        }

        internal const string InitialPacketTooShort = "Initial packet too short";
        internal const string InvalidAckRange = "Invalid ack range";
        internal const string FrameNotAllowed = "Frame type not allowed in given packet";
        internal const string UnknownFrameType = "Unknown frame type";
        internal const string NotInRecvState = "Stream is not in Recv state";
        internal const string StreamsLimitViolated = "Streams limit exceeded";
        internal const string StreamMaxDataViolated = "Streams max data limit violated";
        internal const string MaxDataViolated = "Connection max data limit violated";
        internal const string StreamNotWritable = "Stream is not writable";
        internal const string StreamNotReadable = "Stream is not readable";
        internal const string StreamNotCreated = "Stream was not created";
        internal const string InconsistentFinalSize = "Inconsistent stream final size";
        internal const string WritingPastFinalSize = "Writing data past stream final size";
        internal const string UnableToDeserialize = "Unable to deserialize";
        internal const string NewConnectionIdFrameWhenZeroLengthCIDUsed = "Cannot issue new connection ids when zero-length CID is used";
        internal const string ConnectionIdTooLong = "Connection id too long";
        internal const string UnexpectedToken = "Server may not send Token in initial packet";
        internal const string InvalidReservedBits = "Invalid value for ReservedBits";

        internal const string InconsistentNewConnectionIdFrame = "Inconsistent NEW_CONNECTION_ID frame contents";
    }
}
