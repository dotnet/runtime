using System.Diagnostics;
using System.Net.Quic.Implementations.Managed.Internal;
using System.Net.Quic.Implementations.Managed.Internal.Crypto;
using System.Net.Quic.Implementations.Managed.Internal.Frames;
using System.Runtime.InteropServices;

namespace System.Net.Quic.Tests.Harness
{
    using ImplFrame = Implementations.Managed.Internal.Frames.NewConnectionIdFrame;

    /// <summary>
    ///     Provides peer with an alternative connection IDs that can be used to break linkability when migrating connections.
    /// </summary>
    internal class NewConnectionIdFrame : FrameBase
    {
        /// <summary>
        ///     Sequence number assigned to the connection ID by the sender.
        /// </summary>
        internal ulong SequenceNumber;

        /// <summary>
        ///     Indicator which connection ids should be retired.
        /// </summary>
        internal ulong RetirePriorTo;

        /// <summary>
        ///     Connection Id.
        /// </summary>
        internal byte[] ConnectionId;

        /// <summary>
        ///     Stateless reset token to be used when <see cref="ConnectionId" /> is used.
        /// </summary>
        internal StatelessResetToken StatelessResetToken;

        internal override FrameType FrameType => FrameType.NewConnectionId;

        internal override void Serialize(QuicWriter writer)
        {
            ImplFrame.Write(writer, new ImplFrame(SequenceNumber, RetirePriorTo, ConnectionId, StatelessResetToken));
        }

        internal override bool Deserialize(QuicReader reader)
        {
            if (!ImplFrame.Read(reader, out var frame))
                return false;

            SequenceNumber = frame.SequenceNumber;
            RetirePriorTo = frame.RetirePriorTo;
            ConnectionId = frame.ConnectionId.ToArray();
            StatelessResetToken = frame.StatelessResetToken;

            return true;
        }
    }
}
