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

        internal static readonly string InitialPacketTooShort = "Initial packet too short";
        internal static readonly string InvalidAckRange = "Invalid ack range";
        internal static readonly string UnknownFrameType = "Unknown frame type";
        internal static readonly string NotInRecvState = "Stream is not in Recv state";
    }
}
