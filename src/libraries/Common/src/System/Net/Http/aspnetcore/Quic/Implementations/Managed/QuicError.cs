using System.Net.Quic.Implementations.Managed.Internal;

namespace System.Net.Quic.Implementations.Managed
{
    internal class QuicError
    {
        internal TransportErrorCode ErrorCode { get; }
        internal FrameType? FrameType { get; }
        internal string? ReasonPhrase { get; }
        internal bool IsQuicError { get; }

        public QuicError(TransportErrorCode errorCode, FrameType? frameType = null, string? reasonPhrase = null, bool isQuicError = true)
        {
            ErrorCode = errorCode;
            FrameType = frameType;
            ReasonPhrase = reasonPhrase;
            IsQuicError = isQuicError;
        }
    }
}
