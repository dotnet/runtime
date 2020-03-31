using System.Diagnostics;
using System.Net.Quic.Implementations.Managed.Internal;

namespace System.Net.Quic.Tests.Harness
{
    using ImplFrame = Implementations.Managed.Internal.Frames.StreamsBlockedFrame;

    /// <summary>
    ///     Indicates that the peer wishes to open a stream but is unable to due to current maximum stream limit.
    /// </summary>
    internal class StreamsBlockedFrame : FrameBase
    {
        /// <summary>
        ///     Stream limit at the time the frame was sent.
        /// </summary>
        internal ulong StreamLimit;

        /// <summary>
        ///     Indicates that the <see cref="StreamLimit" /> is meant for bidirectional streams. Otherwise unidirectional streams.
        /// </summary>
        internal bool Bidirectional;

        internal override FrameType FrameType =>
            Bidirectional ? FrameType.StreamsBlockedBidirectional : FrameType.StreamsBlockedUnidirectional;

        internal override void Serialize(QuicWriter writer)
        {
            ImplFrame.Write(writer, new ImplFrame(StreamLimit, Bidirectional));
        }

        internal override bool Deserialize(QuicReader reader)
        {
            if (!ImplFrame.Read(reader, out var frame))
                return false;

            StreamLimit = frame.StreamLimit;
            Bidirectional = frame.Bidirectional;

            return true;
        }
    }
}
