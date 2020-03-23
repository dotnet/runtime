using System.Diagnostics;

namespace System.Net.Quic.Implementations.Managed.Internal.Frames
{
    /// <summary>
    ///     Indicates that peer is discarding the data at application request and that transmission of a particular stream
    ///     should be ceased.
    /// </summary>
    internal readonly struct StopSendingFrame
    {
        /// <summary>
        ///     Id of the stream being ignored.
        /// </summary>
        internal readonly ulong StreamId;

        internal StopSendingFrame(ulong streamId, ulong applicationErrorCode)
        {
            StreamId = streamId;
            ApplicationErrorCode = applicationErrorCode;
        }

        /// <summary>
        ///     Application-specific reason for ignoring the stream.
        /// </summary>
        internal readonly ulong ApplicationErrorCode;

        internal static bool Read(QuicReader reader, out StopSendingFrame frame)
        {
            var type = reader.ReadFrameType();
            Debug.Assert(type == FrameType.StopSending);

            if (!reader.TryReadVarInt(out ulong streamId) ||
                !reader.TryReadVarInt(out ulong error))
            {
                frame = default;
                return false;
            }

            frame = new StopSendingFrame(streamId, error);
            return true;
        }

        internal static void Write(QuicWriter writer, in StopSendingFrame frame)
        {
            writer.WriteFrameType(FrameType.StopSending);

            writer.WriteVarInt(frame.StreamId);
            writer.WriteVarInt(frame.ApplicationErrorCode);
        }
    }
}
