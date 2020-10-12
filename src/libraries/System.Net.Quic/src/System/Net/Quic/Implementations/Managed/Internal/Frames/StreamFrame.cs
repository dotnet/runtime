// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Net.Quic.Implementations.Managed.Internal.Streams;

namespace System.Net.Quic.Implementations.Managed.Internal.Frames
{
    /// <summary>
    ///     Carries the application data to the peer.
    /// </summary>
    internal readonly ref struct StreamFrame
    {
        /// <summary>
        ///     Minimum useful Stream frame size.
        /// </summary>
        internal const int MinSize = 5;

        /// <summary>
        ///     Id of the data stream.
        /// </summary>
        internal readonly long StreamId;

        /// <summary>
        ///     Byte offset of the carried data in the stream.
        /// </summary>
        internal readonly long Offset;

        /// <summary>
        ///     Flag indicating that this frame marks the end of the stream.
        /// </summary>
        internal readonly bool Fin;

        /// <summary>
        ///     Bytes from the designated stream to be delivered.
        /// </summary>
        internal readonly ReadOnlySpan<byte> StreamData;

        internal StreamFrame(long streamId, long offset, bool fin, ReadOnlySpan<byte> streamData)
        {
            StreamId = streamId;
            Offset = offset;
            Fin = fin;
            StreamData = streamData;
        }

        internal static int GetOverheadLength(long streamId, long offset, long length)
        {
            return 1 +
                   QuicPrimitives.GetVarIntLength(streamId) +
                   (offset > 0 ? QuicPrimitives.GetVarIntLength(offset) : 0) +
                   QuicPrimitives.GetVarIntLength(length);
        }

        internal int GetSerializedLength()
        {
            return GetOverheadLength(StreamId, Offset, StreamData.Length) +
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

            long length = 0;
            long offset = 0;
            if (!reader.TryReadVarInt(out long streamId) ||
                hasOffset && !reader.TryReadVarInt(out offset) ||
                hasLength && !reader.TryReadVarInt(out length) ||
                offset + length > StreamHelpers.MaxStreamOffset ||
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

        internal static Span<byte> ReservePayloadBuffer(QuicWriter writer, long streamId, long offset, int length, bool fin)
        {
            // We always include length in the frame, regardless whether this is the last frame or not
            var type = FrameType.Stream | FrameType.StreamLenBit;

            if (offset != 0) type |= FrameType.StreamOffBit;
            if (fin) type |= FrameType.StreamFinBit;

            writer.WriteFrameType(type);
            writer.WriteVarInt(streamId);
            if (offset != 0)
                writer.WriteVarInt(offset);
            writer.WriteVarInt(length);

            return writer.GetWritableSpan(length);
        }

        internal static void Write(QuicWriter writer, in StreamFrame frame)
        {
            Debug.Assert(writer.BytesAvailable >= frame.GetSerializedLength());

            frame.StreamData.CopyTo(ReservePayloadBuffer(writer, frame.StreamId, frame.Offset, frame.StreamData.Length, frame.Fin));
        }
    }
}
