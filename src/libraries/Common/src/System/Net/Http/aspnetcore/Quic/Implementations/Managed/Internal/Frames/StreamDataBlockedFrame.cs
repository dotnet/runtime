using System.Diagnostics;

namespace System.Net.Quic.Implementations.Managed.Internal.Frames
{
    /// <summary>
    ///     Indicates that the peer has data to send on particular stream but is unable to do so due to stream-level flow
    ///     control.
    /// </summary>
    internal readonly struct StreamDataBlockedFrame
    {
        /// <summary>
        ///     Id of the blocked stream.
        /// </summary>
        internal readonly ulong StreamId;

        /// <summary>
        ///     Offset in the stream at which the blocking occured.
        /// </summary>
        internal readonly ulong StreamDataLimit;

        internal StreamDataBlockedFrame(ulong streamId, ulong streamDataLimit)
        {
            StreamId = streamId;
            StreamDataLimit = streamDataLimit;
        }

        internal static bool Read(QuicReader reader, out StreamDataBlockedFrame frame)
        {
            var type = reader.ReadFrameType();
            Debug.Assert(type == FrameType.StreamDataBlocked);

            if (!reader.TryReadVarInt(out ulong streamId) ||
                !reader.TryReadVarInt(out ulong limit))
            {
                frame = default;
                return false;
            }

            frame = new StreamDataBlockedFrame(streamId, limit);
            return true;
        }

        internal static void Write(QuicWriter writer, in StreamDataBlockedFrame frame)
        {
            writer.WriteFrameType(FrameType.StreamDataBlocked);

            writer.WriteVarInt(frame.StreamId);
            writer.WriteVarInt(frame.StreamDataLimit);
        }
    }
}
