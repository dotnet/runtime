using System.Diagnostics;

namespace System.Net.Quic.Implementations.Managed.Internal.Frames
{
    /// <summary>
    ///     Informs endpoint that the peer abruptly terminates their sending part of the stream.
    /// </summary>
    internal readonly struct ResetStreamFrame
    {
        /// <summary>
        ///     Stream ID of the stream being terminated.
        /// </summary>
        internal readonly long StreamId;

        /// <summary>
        ///     Application-level error code indicating why the stream is being closed.
        /// </summary>
        internal readonly long ApplicationErrorCode;

        /// <summary>
        ///     Final size of the stream reset by this frame in bytes.
        /// </summary>
        internal readonly long FinalSize;

        internal int GetSerializedLength()
        {
            return 1 +
                   QuicPrimitives.GetVarIntLength(StreamId) +
                   QuicPrimitives.GetVarIntLength(ApplicationErrorCode) +
                   QuicPrimitives.GetVarIntLength(FinalSize);
        }

        internal ResetStreamFrame(long streamId, long applicationErrorCode, long finalSize)
        {
            StreamId = streamId;
            ApplicationErrorCode = applicationErrorCode;
            FinalSize = finalSize;
        }

        internal static bool Read(QuicReader reader, out ResetStreamFrame frame)
        {
            var type = reader.ReadFrameType();
            Debug.Assert(type == FrameType.ResetStream);

            if (reader.TryReadVarInt(out long streamId) &&
                reader.TryReadVarInt(out long error) &&
                reader.TryReadVarInt(out long size))
            {
                frame = new ResetStreamFrame(streamId, error, size);
                return true;
            }

            frame = default;
            return false;
        }

        internal static void Write(QuicWriter writer, in ResetStreamFrame frame)
        {
            Debug.Assert(writer.BytesAvailable >= frame.GetSerializedLength());

            writer.WriteFrameType(FrameType.ResetStream);

            writer.WriteVarInt(frame.StreamId);
            writer.WriteVarInt(frame.ApplicationErrorCode);
            writer.WriteVarInt(frame.FinalSize);
        }
    }
}
