using System.Diagnostics;
using System.Net.Quic.Implementations.Managed.Internal;

namespace System.Net.Quic.Tests.Harness
{
    using ImplFrame = Implementations.Managed.Internal.Frames.StreamFrame;

    /// <summary>
    ///     Carries the application data to the peer.
    /// </summary>
    internal class StreamFrame : FrameBase
    {
        /// <summary>
        ///     Id of the data stream.
        /// </summary>
        internal ulong StreamId;

        /// <summary>
        ///     Byte offset of the carried data in the stream.
        /// </summary>
        internal ulong Offset;

        /// <summary>
        ///     Flag indicating that this frame marks the end of the stream.
        /// </summary>
        internal bool Fin;

        /// <summary>
        ///     Bytes from the designated stream to be delivered.
        /// </summary>
        internal byte[] StreamData;

        internal override FrameType FrameType
        {
            get
            {
                var type = FrameType.Stream | FrameType.StreamLenBit;
                if (Offset != 0) type |= FrameType.StreamOffBit;
                if (Fin) type |= FrameType.StreamFinBit;

                return type;
            }
        }

        internal override void Serialize(QuicWriter writer)
        {
            ImplFrame.Write(writer, new ImplFrame(StreamId, Offset, Fin, StreamData));
        }

        internal override bool Deserialize(QuicReader reader)
        {
            if (!ImplFrame.Read(reader, out var frame))
                return false;

            StreamId = frame.StreamId;
            Offset = frame.Offset;
            Fin = frame.Fin;
            StreamData = frame.StreamData.ToArray();

            return true;
        }
    }
}
