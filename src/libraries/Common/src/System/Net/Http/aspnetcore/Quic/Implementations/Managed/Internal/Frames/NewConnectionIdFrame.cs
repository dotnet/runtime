using System.Buffers.Binary;
using System.Diagnostics;
using System.Net.Quic.Implementations.Managed.Internal.Crypto;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Net.Quic.Implementations.Managed.Internal.Frames
{
    /// <summary>
    ///     Token used to authorize a Connection id when attempting to connect to a known host.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal readonly struct StatelessResetToken
    {
        /// <summary>
        ///     Length of the reset token.
        /// </summary>
        internal const int Length = 128/8;

        /// <summary>
        ///     First 8 bytes of the reset token.
        /// </summary>
        internal readonly ulong LowerHalf;

        /// <summary>
        ///     Second 8 bytes of the reset token.
        /// </summary>
        internal readonly ulong UpperHalf;

        public StatelessResetToken(ulong lowerHalf, ulong upperHalf)
        {
            LowerHalf = lowerHalf;
            UpperHalf = upperHalf;
        }

        internal static StatelessResetToken FromSpan(ReadOnlySpan<byte> token)
        {
            return MemoryMarshal.Read<StatelessResetToken>(token);
        }

        internal static void ToSpan(Span<byte> destination, in StatelessResetToken token)
        {
            MemoryMarshal.AsRef<StatelessResetToken>(destination) = token;
        }
    }

    /// <summary>
    ///     Provides peer with an alternative connection IDs that can be used to break linkability when migrating connections.
    /// </summary>
    internal readonly ref struct NewConnectionIdFrame
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
        internal readonly StatelessResetToken StatelessResetToken;

        public NewConnectionIdFrame(ulong sequenceNumber, ulong retirePriorTo, ReadOnlySpan<byte> connectionId,
            StatelessResetToken statelessResetToken)
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
