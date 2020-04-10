using System.Diagnostics;
using System.Net.Quic.Implementations.Managed.Internal;
using System.Text;

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
        internal long StreamId;

        /// <summary>
        ///     Byte offset of the carried data in the stream.
        /// </summary>
        internal long Offset;

        /// <summary>
        ///     Flag indicating that this frame marks the end of the stream.
        /// </summary>
        internal bool Fin;

        /// <summary>
        ///     Bytes from the designated stream to be delivered.
        /// </summary>
        internal byte[] StreamData;

        public override string ToString()
        {
            StringBuilder builder = new StringBuilder($"Stream[{StreamId}");
            if (Offset != 0)
            {
                builder.Append($", Off={Offset}");
            }

            if (StreamData.Length > 0)
            {
                builder.Append(($", Len={StreamData.Length}"));
            }

            if (Fin)
            {
                builder.Append((", Fin"));
            }

            builder.Append("]");
            return builder.ToString();
        }

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
