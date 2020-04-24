using System.Net.Quic.Implementations.Managed.Internal;

namespace System.Net.Quic.Implementations.Managed
{
    // TODO-RZ: replace by the base class later
    internal class QuicErrorException : QuicConnectionAbortedException
    {
        public QuicErrorException(QuicError error)
            : base((long) error.ErrorCode)
        {
            Error = error;
        }

        internal QuicError Error { get; }

        public override string Message =>
            $"Connection was terminated by the peer: {Error.ErrorCode} - {Error.ReasonPhrase}{(Error.FrameType != FrameType.Padding ? $" ({Error.FrameType})" : "")}";
    }
}
