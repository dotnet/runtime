using System.Diagnostics;

namespace System.Net.Quic.Implementations.Managed.Internal.Frames
{
    /// <summary>
    ///     Indicates that the peer has data to send but is blocked by the flow control limits.
    /// </summary>
    internal readonly struct DataBlockedFrame
    {
        /// <summary>
        ///     Connection-level limit at which the blocking occured.
        /// </summary>
        internal readonly ulong DataLimit;

        public DataBlockedFrame(ulong dataLimit)
        {
            DataLimit = dataLimit;
        }

        internal static bool Read(QuicReader reader, out DataBlockedFrame frame)
        {
            var type = reader.ReadFrameType();
            Debug.Assert(type == FrameType.DataBlocked);

            if (!reader.TryReadVarInt(out ulong limit))
            {
                frame = default;
                return false;
            }

            frame = new DataBlockedFrame(limit);
            return true;
        }

        internal static void Write(QuicWriter writer, DataBlockedFrame frame)
        {
            writer.WriteFrameType(FrameType.DataBlocked);

            writer.WriteVarInt(frame.DataLimit);
        }
    }
}
