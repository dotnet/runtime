using System.Diagnostics;
using System.Net.Quic.Implementations.Managed.Internal;

namespace System.Net.Quic.Tests.Harness
{
    using ImplFrame = Implementations.Managed.Internal.Frames.RetireConnectionIdFrame;

    /// <summary>
    ///     Indicates that peer will no longer use a connection ID that it has issued. It also serves as a request to the peer
    ///     to send additional connection IDs for future use.
    /// </summary>
    internal class RetireConnectionIdFrame : FrameBase
    {
        /// <summary>
        ///     Sequence number of the connection id being retired.
        /// </summary>
        internal ulong SequenceNumber;

        internal override FrameType FrameType => FrameType.RetireConnectionId;

        internal override void Serialize(QuicWriter writer)
        {
            ImplFrame.Write(writer, new ImplFrame(SequenceNumber));
        }

        internal override bool Deserialize(QuicReader reader)
        {
            if (!ImplFrame.Read(reader, out var frame))
                return false;

            SequenceNumber = frame.SequenceNumber;

            return true;
        }
    }
}
