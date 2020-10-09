// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Net.Quic.Implementations.Managed.Internal.Frames
{
    /// <summary>
    ///     Provides peer with an alternative connection IDs that can be used to break linkability when migrating connections.
    /// </summary>
    internal readonly ref struct NewConnectionIdFrame
    {
        /// <summary>
        ///     Sequence number assigned to the connection ID by the sender.
        /// </summary>
        internal readonly long SequenceNumber;

        /// <summary>
        ///     Indicator which connection ids should be retired.
        /// </summary>
        internal readonly long RetirePriorTo;

        /// <summary>
        ///     Connection Id.
        /// </summary>
        internal readonly ReadOnlySpan<byte> ConnectionId;

        /// <summary>
        ///     Stateless reset token to be used when <see cref="ConnectionId" /> is used.
        /// </summary>
        internal readonly StatelessResetToken StatelessResetToken;

        public NewConnectionIdFrame(long sequenceNumber, long retirePriorTo, ReadOnlySpan<byte> connectionId,
            StatelessResetToken statelessResetToken)
        {
            SequenceNumber = sequenceNumber;
            RetirePriorTo = retirePriorTo;
            ConnectionId = connectionId;
            StatelessResetToken = statelessResetToken;
        }

        internal int GetSerializedLength()
        {
            return 1 +
                   QuicPrimitives.GetVarIntLength(SequenceNumber) +
                   QuicPrimitives.GetVarIntLength(RetirePriorTo) +
                   QuicPrimitives.GetVarIntLength(ConnectionId.Length) +
                   ConnectionId.Length +
                   StatelessResetToken.Length;
        }

        internal static bool Read(QuicReader reader, out NewConnectionIdFrame frame)
        {
            var type = reader.ReadFrameType();
            Debug.Assert(type == FrameType.NewConnectionId);

            if (!reader.TryReadVarInt(out long sequenceNumber) ||
                !reader.TryReadVarInt(out long retirePriorTo) || retirePriorTo > sequenceNumber ||
                !reader.TryReadUInt8(out byte length) || length > HeaderHelpers.MaxConnectionIdLength ||
                !reader.TryReadSpan(length, out var connectionId) ||
                !reader.TryReadStatelessResetToken(out var token))
            {
                frame = default;
                return false;
            }

            frame = new NewConnectionIdFrame(sequenceNumber, retirePriorTo, connectionId, token);
            return true;
        }

        internal static void Write(QuicWriter writer, in NewConnectionIdFrame frame)
        {
            Debug.Assert(writer.BytesAvailable >= frame.GetSerializedLength());

            writer.WriteFrameType(FrameType.NewConnectionId);

            writer.WriteVarInt(frame.SequenceNumber);
            writer.WriteVarInt(frame.RetirePriorTo);
            Debug.Assert(frame.ConnectionId.Length <= HeaderHelpers.MaxConnectionIdLength);
            writer.WriteUInt8((byte)frame.ConnectionId.Length);
            writer.WriteSpan(frame.ConnectionId);
            writer.WriteStatelessResetToken(frame.StatelessResetToken);
        }
    }
}
