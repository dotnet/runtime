using System.Diagnostics;
using System.Net.Quic.Implementations.Managed.Internal.Crypto;

namespace System.Net.Quic.Implementations.Managed.Internal.Frames
{
    /// <summary>
    ///     Provides peer with an alternative connection IDs that can be used to break linkability when migrating connections.
    /// </summary>
    internal ref readonly struct NewConnectionIdFrame
    {
        /// <summary>
        ///     Sequence number assigned to the connection ID by the sender.
        /// </summary>
        internal readonly ulong SequenceNumber;

        /// <summary>
        ///     Indicator which connection ids should be retired.
        /// </summary>
        internal readonly ulong RetirePriorTo;

        /// <summary>
        ///     Connection Id.
        /// </summary>
        internal readonly ReadOnlySpan<byte> ConnectionId;

        /// <summary>
        ///     Stateless reset token to be used when <see cref="ConnectionId" /> is used.
        /// </summary>
        internal readonly ReadOnlySpan<byte> StatelessResetToken;

        public NewConnectionIdFrame(ulong sequenceNumber, ulong retirePriorTo, ReadOnlySpan<byte> connectionId,
            ReadOnlySpan<byte> statelessResetToken)
        {
            SequenceNumber = sequenceNumber;
            RetirePriorTo = retirePriorTo;
            ConnectionId = connectionId;
            StatelessResetToken = statelessResetToken;
        }

        internal static bool Read(QuicReader reader, out NewConnectionIdFrame frame)
        {
            var type = reader.ReadFrameType();
            Debug.Assert(type == FrameType.NewConnectionId);

            if (!reader.TryReadVarInt(out ulong sequenceNumber) ||
                !reader.TryReadVarInt(out ulong retirePriorTo) || retirePriorTo > sequenceNumber ||
                !reader.TryReadUInt8(out byte length) || length > HeaderHelpers.MaxConnectionIdLength ||
                !reader.TryReadSpan(length, out var connectionId) ||
                !reader.TryReadSpan(HeaderHelpers.StatelessResetTokenLength, out var token))
            {
                frame = default;
                return false;
            }

            frame = new NewConnectionIdFrame(sequenceNumber, retirePriorTo, connectionId, token);
            return true;
        }

        internal static void Write(QuicWriter writer, in NewConnectionIdFrame frame)
        {
            writer.WriteFrameType(FrameType.NewConnectionId);

            writer.WriteVarInt(frame.SequenceNumber);
            writer.WriteVarInt(frame.RetirePriorTo);
            Debug.Assert(frame.ConnectionId.Length <= HeaderHelpers.MaxConnectionIdLength);
            writer.WriteUInt8((byte)frame.ConnectionId.Length);
            writer.WriteSpan(frame.ConnectionId);
            Debug.Assert(frame.StatelessResetToken.Length == HeaderHelpers.StatelessResetTokenLength);
            writer.WriteSpan(frame.StatelessResetToken);
        }
    }
}
