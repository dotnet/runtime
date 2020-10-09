// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
        internal readonly long StreamId;

        /// <summary>
        ///     Offset in the stream at which the blocking occured.
        /// </summary>
        internal readonly long StreamDataLimit;

        internal StreamDataBlockedFrame(long streamId, long streamDataLimit)
        {
            StreamId = streamId;
            StreamDataLimit = streamDataLimit;
        }

        internal int GetSerializedLength()
        {
            return 1 +
                   QuicPrimitives.GetVarIntLength(StreamId) +
                   QuicPrimitives.GetVarIntLength(StreamDataLimit);
        }

        internal static bool Read(QuicReader reader, out StreamDataBlockedFrame frame)
        {
            var type = reader.ReadFrameType();
            Debug.Assert(type == FrameType.StreamDataBlocked);

            if (!reader.TryReadVarInt(out long streamId) ||
                !reader.TryReadVarInt(out long limit))
            {
                frame = default;
                return false;
            }

            frame = new StreamDataBlockedFrame(streamId, limit);
            return true;
        }

        internal static void Write(QuicWriter writer, in StreamDataBlockedFrame frame)
        {
            Debug.Assert(writer.BytesAvailable >= frame.GetSerializedLength());

            writer.WriteFrameType(FrameType.StreamDataBlocked);

            writer.WriteVarInt(frame.StreamId);
            writer.WriteVarInt(frame.StreamDataLimit);
        }
    }
}
