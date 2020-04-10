using System.Diagnostics;
using System.Net.Quic.Implementations.Managed.Internal;

namespace System.Net.Quic.Tests.Harness
{
    using ImplFrame = Implementations.Managed.Internal.Frames.MaxStreamDataFrame;

    /// <summary>
    ///     Used in flow control to inform the peer of the maximmum amount of data that can be sent on a given stream.
    /// </summary>
    internal class MaxStreamDataFrame : FrameBase
    {
        /// <summary>
        ///     The ID of the stream.
        /// </summary>
        internal long StreamId;

        /// <summary>
        ///     Maximum amount of data that can be sent on the stream identified by <see cref="StreamId" />.
        /// </summary>
        internal long MaximumStreamData;

        internal override FrameType FrameType => FrameType.MaxStreamData;

        internal override void Serialize(QuicWriter writer)
        {
            ImplFrame.Write(writer, new ImplFrame(StreamId, MaximumStreamData));
        }

        internal override bool Deserialize(QuicReader reader)
        {
            if (!ImplFrame.Read(reader, out var frame))
                return false;

            StreamId = frame.StreamId;
            MaximumStreamData = frame.MaximumStreamData;

            return true;
        }
    }
}
