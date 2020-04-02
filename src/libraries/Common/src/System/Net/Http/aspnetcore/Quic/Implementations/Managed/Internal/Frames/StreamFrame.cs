using System.Diagnostics;

namespace System.Net.Quic.Implementations.Managed.Internal.Frames
{
    /// <summary>
    ///     Carries the application data to the peer.
    /// </summary>
    internal readonly ref struct StreamFrame
    {
        /// <summary>
        ///     Id of the data stream.
        /// </summary>
        internal readonly ulong StreamId;

        /// <summary>
        ///     Byte offset of the carried data in the stream.
        /// </summary>
        internal readonly ulong Offset;

        /// <summary>
        ///     Flag indicating that this frame marks the end of the stream.
        /// </summary>
        internal readonly bool Fin;

        /// <summary>
        ///     Bytes from the designated stream to be delivered.
        /// </summary>
        internal readonly ReadOnlySpan<byte> StreamData;

        internal StreamFrame(ulong streamId, ulong offset, bool fin, ReadOnlySpan<byte> streamData)
        {
            StreamId = streamId;
            Offset = offset;
            Fin = fin;
            StreamData = streamData;
        }

        internal int GetSerializedLength()
        {
            return 1 +
                   QuicPrimitives.GetVarIntLength(StreamId) +
                   (Offset > 0 ? QuicPrimitives.GetVarIntLength(Offset) : 0) +
                   QuicPrimitives.GetVarIntLength((ulong)StreamData.Length) + // TODO-RZ: length is not mandatory
                   StreamData.Length;


        }

        internal static bool Read(QuicReader reader, out StreamFrame frame)
        {
            var type = reader.ReadFrameType();
            // Stream type is a bit special, it is in form 0b0000_1XXX
            Debug.Assert((type & FrameType.StreamMask) == type);

            bool hasOffset = (type & FrameType.StreamOffBit) != 0;
            bool hasLength = (type & FrameType.StreamLenBit) != 0;
            bool hasFin = (type & FrameType.StreamFinBit) != 0;

            ulong length = 0;
            ulong offset = 0;
            if (!reader.TryReadVarInt(out ulong streamId) ||
                hasOffset && !reader.TryReadVarInt(out offset) ||
                // TODO-RZ: is zero length allowed here?
                hasLength && !reader.TryReadVarInt(out length) && length == 0 ||
                // Read to end if length not set
                !reader.TryReadSpan(hasLength ? (int)length : reader.BytesLeft,
                    out var data))
            {
                frame = default;
                return false;
            }

            frame = new StreamFrame(streamId, offset, hasFin, data);
            return true;
        }

        internal static void Write(QuicWriter writer, in StreamFrame frame)
        {
            Debug.Assert(writer.BytesAvailable >= frame.GetSerializedLength());

            // TODO-RZ: leave out length if this is the last frame
            var type = FrameType.Stream | FrameType.StreamLenBit;
            if (frame.Offset != 0) type |= FrameType.StreamOffBit;
            if (frame.Fin) type |= FrameType.StreamFinBit;

            writer.WriteFrameType(type);

            writer.WriteVarInt(frame.StreamId);
            if (frame.Offset != 0)
                writer.WriteVarInt(frame.Offset);
            writer.WriteLengthPrefixedSpan(frame.StreamData);
        }
    }
}
