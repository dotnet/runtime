using System.Diagnostics;
using System.Net.Quic.Implementations.Managed.Internal;
using System.Text;

namespace System.Net.Quic.Tests.Harness
{
    using ImplFrame = Implementations.Managed.Internal.Frames.ConnectionCloseFrame;

    /// <summary>
    ///     Used to notify the peer that the connection is being closed.
    /// </summary>
    internal class ConnectionCloseFrame : FrameBase
    {
        /// <summary>
        ///     Error code which indicates the reason for closing this connection. If <see cref="IsQuicError" /> is true, then the
        ///     value is one of the <see cref="TransportErrorCode" />. Otherwise it is the application defined error.
        /// </summary>
        internal TransportErrorCode ErrorCode;

        /// <summary>
        ///     If true, the error reported comes from the QUIC layer, otherwise from the application layer.
        /// </summary>
        internal bool IsQuicError;

        /// <summary>
        ///     Frame type that triggered the error. Value of <see cref="ErrorFrameType.Padding" /> is used when frame type is unknown.
        ///     Only used if <see cref="IsQuicError" /> is true.
        /// </summary>
        internal FrameType ErrorFrameType;

        /// <summary>
        ///     Human readable explanation why the connection was closed.
        /// </summary>
        internal string ReasonPhrase;

        protected override string GetAdditionalInfo() => $"[{ErrorCode}: {(IsQuicError ? ErrorFrameType + ", " : "")}{ReasonPhrase}]";

        internal override FrameType FrameType =>
            IsQuicError ? FrameType.ConnectionCloseQuic : FrameType.ConnectionCloseApplication;

        internal override void Serialize(QuicWriter writer)
        {
            ImplFrame.Write(writer, new ImplFrame((long) ErrorCode, IsQuicError, ErrorFrameType, ReasonPhrase));
        }

        internal override bool Deserialize(QuicReader reader)
        {
            if (!ImplFrame.Read(reader, out var frame))
                return false;

            ErrorCode = (TransportErrorCode)frame.ErrorCode;
            IsQuicError = frame.IsQuicError;
            ErrorFrameType = frame.FrameType;
            ReasonPhrase = frame.ReasonPhrase;

            return true;
        }
    }
}
