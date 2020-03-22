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
        internal readonly ulong StreamId;

        /// <summary>
        ///     Maximum amount of data that can be sent on the stream identified by <see cref="StreamId"/>.
        /// </summary>
        internal readonly ulong MaximumStreamData;

        internal MaxStreamDataFrame(ulong streamId, ulong maximumStreamData)
        {
            StreamId = streamId;
            MaximumStreamData = maximumStreamData;
        }

        internal static bool Read(QuicReader reader, out MaxStreamDataFrame frame)
        {
            var type = reader.ReadFrameType();
            Debug.Assert(type == FrameType.MaxStreamData);

            if (!reader.TryReadVarInt(out ulong streamId) ||
                !reader.TryReadVarInt(out ulong maxData))
            {
                frame = default;
                return false;
            }

            frame = new MaxStreamDataFrame(streamId, maxData);
            return true;
        }

        internal static void Write(QuicWriter writer, MaxStreamDataFrame frame)
        {
            writer.WriteFrameType(FrameType.MaxStreamData);

            writer.WriteVarInt(frame.StreamId);
            writer.WriteVarInt(frame.MaximumStreamData);
        }
    }
}
