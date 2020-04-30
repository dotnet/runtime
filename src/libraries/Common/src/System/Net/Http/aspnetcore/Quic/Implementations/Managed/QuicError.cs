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
        internal const string UnknownFrameType = "Unknown frame type";
        internal const string NotInRecvState = "Stream is not in Recv state";
        internal const string StreamsLimitViolated = "Streams limit exceeded";
        internal const string StreamMaxDataViolated = "Streams max data limit violated";
        internal const string StreamNotWritable = "Stream is not writable";
        internal const string InconsistentFinalSize = "Inconsistent stream final size";
        internal const string WritingPastFinalSize = "Writing data past stream final size";
        internal const string UnableToDeserialize = "Unable to deserialize";
    }
}
