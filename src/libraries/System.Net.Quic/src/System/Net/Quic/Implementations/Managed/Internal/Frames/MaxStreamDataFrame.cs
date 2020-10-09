// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Net.Quic.Implementations.Managed.Internal.Frames
{
    /// <summary>
    ///     Used in flow control to inform the peer of the maximmum amount of data that can be sent on a given stream.
    /// </summary>
    internal readonly struct MaxStreamDataFrame
    {
        /// <summary>
        ///     The ID of the stream.
        /// </summary>
        internal readonly long StreamId;

        /// <summary>
        ///     Maximum amount of data that can be sent on the stream identified by <see cref="StreamId" />.
        /// </summary>
        internal readonly long MaximumStreamData;

        internal MaxStreamDataFrame(long streamId, long maximumStreamData)
        {
            StreamId = streamId;
            MaximumStreamData = maximumStreamData;
        }

        internal int GetSerializedLength()
        {
            return 1 +
                   QuicPrimitives.GetVarIntLength(StreamId) +
                   QuicPrimitives.GetVarIntLength(MaximumStreamData);
        }

        internal static bool Read(QuicReader reader, out MaxStreamDataFrame frame)
        {
            var type = reader.ReadFrameType();
            Debug.Assert(type == FrameType.MaxStreamData);

            if (!reader.TryReadVarInt(out long streamId) ||
                !reader.TryReadVarInt(out long maxData))
            {
                frame = default;
                return false;
            }

            frame = new MaxStreamDataFrame(streamId, maxData);
            return true;
        }

        internal static void Write(QuicWriter writer, MaxStreamDataFrame frame)
        {
            Debug.Assert(writer.BytesAvailable >= frame.GetSerializedLength());

            writer.WriteFrameType(FrameType.MaxStreamData);

            writer.WriteVarInt(frame.StreamId);
            writer.WriteVarInt(frame.MaximumStreamData);
        }
    }
}
