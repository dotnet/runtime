using System.Diagnostics;
using System.Net.Quic.Implementations.Managed.Internal;

namespace System.Net.Quic.Tests.Harness
{
    using ImplFrame = Implementations.Managed.Internal.Frames.StopSendingFrame;

    /// <summary>
    ///     Indicates that peer is discarding the data at application request and that transmission of a particular stream
    ///     should be ceased.
    /// </summary>
    internal class StopSendingFrame : FrameBase
    {
        /// <summary>
        ///     Id of the stream being ignored.
        /// </summary>
        internal ulong StreamId;

        /// <summary>
        ///     Application-specific reason for ignoring the stream.
        /// </summary>
        internal ulong ApplicationErrorCode;

        internal override FrameType FrameType => FrameType.StopSending;

        internal override void Serialize(QuicWriter writer)
        {
            ImplFrame.Write(writer, new ImplFrame(StreamId, ApplicationErrorCode));
        }

        internal override bool Deserialize(QuicReader reader)
        {
            if (!ImplFrame.Read(reader, out var frame))
                return false;

            StreamId = frame.StreamId;
            ApplicationErrorCode = frame.ApplicationErrorCode;

            return true;
        }
    }
}
