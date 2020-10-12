// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Net.Quic.Implementations.Managed.Internal.Streams;

namespace System.Net.Quic.Implementations.Managed.Internal.Frames
{
    /// <summary>
    ///     Indicates that the peer wishes to open a stream but is unable to due to current maximum stream limit.
    /// </summary>
    internal readonly struct StreamsBlockedFrame
    {
        /// <summary>
        ///     Stream limit at the time the frame was sent.
        /// </summary>
        internal readonly long StreamLimit;

        /// <summary>
        ///     Indicates that the <see cref="StreamLimit" /> is meant for bidirectional streams. Otherwise unidirectional streams.
        /// </summary>
        internal readonly bool Bidirectional;

        public StreamsBlockedFrame(long streamLimit, bool bidirectional)
        {
            StreamLimit = streamLimit;
            Bidirectional = bidirectional;
        }

        internal int GetSerializedLength()
        {
            return 1 +
                   QuicPrimitives.GetVarIntLength(StreamLimit);
        }

        internal static bool Read(QuicReader reader, out StreamsBlockedFrame frame)
        {
            var type = reader.ReadFrameType();
            Debug.Assert(
                type == FrameType.StreamsBlockedBidirectional || type == FrameType.StreamsBlockedUnidirectional);

            if (!reader.TryReadVarInt(out long limit) ||
                limit > StreamHelpers.MaxStreamIndex) // FRAME_ENCODING_ERROR is fine for this
            {
                frame = default;
                return false;
            }

            frame = new StreamsBlockedFrame(limit, type == FrameType.StreamsBlockedBidirectional);
            return true;
        }

        internal static void Write(QuicWriter writer, in StreamsBlockedFrame frame)
        {
            Debug.Assert(writer.BytesAvailable >= frame.GetSerializedLength());

            writer.WriteFrameType(frame.Bidirectional
                ? FrameType.StreamsBlockedBidirectional
                : FrameType.StreamsBlockedUnidirectional);

            writer.WriteVarInt(frame.StreamLimit);
        }
    }
}
