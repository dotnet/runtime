// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Net.Quic.Implementations.Managed.Internal.Frames
{
    /// <summary>
    ///     Indicates that peer will no longer use a connection ID that it has issued. It also serves as a request to the peer
    ///     to send additional connection IDs for future use.
    /// </summary>
    internal readonly struct RetireConnectionIdFrame
    {
        /// <summary>
        ///     Sequence number of the connection id being retired.
        /// </summary>
        internal readonly long SequenceNumber;

        internal RetireConnectionIdFrame(long sequenceNumber) => SequenceNumber = sequenceNumber;

        internal int GetSerializedLength()
        {
            return 1 +
                   QuicPrimitives.GetVarIntLength(SequenceNumber);
        }

        internal static bool Read(QuicReader reader, out RetireConnectionIdFrame frame)
        {
            var type = reader.ReadFrameType();
            Debug.Assert(type == FrameType.RetireConnectionId);

            if (!reader.TryReadVarInt(out long sequenceNumber))
            {
                frame = default;
                return false;
            }

            frame = new RetireConnectionIdFrame(sequenceNumber);
            return true;
        }

        internal static void Write(QuicWriter writer, RetireConnectionIdFrame frame)
        {
            Debug.Assert(writer.BytesAvailable >= frame.GetSerializedLength());

            writer.WriteFrameType(FrameType.RetireConnectionId);

            writer.WriteVarInt(frame.SequenceNumber);
        }
    }
}
