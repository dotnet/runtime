using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Quic.Implementations.Managed.Internal;

namespace System.Net.Quic.Tests.Harness
{
    using ImplFrame = Implementations.Managed.Internal.Frames.AckFrame;

    /// <summary>
    ///     Informs receiver about received and processed packets.
    /// </summary>
    internal class AckFrame : FrameBase
    {
        internal struct AckRange
        {
            internal ulong Gap;
            internal ulong Acked;
        }

        /// <summary>
        ///     Largest packet number being acknowledged; this is usually the largest packet number that was received
        ///     prior to generating the frame.
        /// </summary>
        internal ulong LargestAcknowledged;

        /// <summary>
        ///     Time delta in microseconds between when this frame was sent and when the largest acknowledged packet, as
        ///     indicated in <see cref="LargestAcknowledged" />, was received. The value of this field is scaled by
        ///     multiplying the value by 2 to the power of the <see cref="TransportParameters.AckDelayExponent" />
        ///     transport parameter set by the sender.
        /// </summary>
        internal ulong AckDelay;

        /// <summary>
        ///     Number of contiguous packets preceding the <see cref="LargestAcknowledged" /> that are being acknowledged.
        /// </summary>
        internal ulong FirstAckRange;

        /// <summary>
        ///     Additional ack ranges.
        /// </summary>
        internal List<AckRange> AckRanges = new List<AckRange>();

        /// <summary>
        ///     Flag indicating that <see cref="Ect0Count" />, <see cref="Ect1Count" /> and <see cref="CeCount" /> fields contain
        ///     valid values.
        /// </summary>
        internal bool HasEcnCounts;

        /// <summary>
        ///     Total number of packets received with the ECT(0) codepoint in the packet number space of this frame. Contains valid
        ///     number only if <see cref="HasEcnCounts" /> is true.
        /// </summary>
        internal ulong Ect0Count;

        /// <summary>
        ///     Total number of packets received with the ECT(1) codepoint in the packet number space of this frame.  Contains
        ///     valid number only if <see cref="HasEcnCounts" /> is true.
        /// </summary>
        internal ulong Ect1Count;

        /// <summary>
        ///     Total number of packets received with the CE codepoint in the packet number space of this frame.  Contains valid
        ///     number only if <see cref="HasEcnCounts" /> is true.
        /// </summary>
        internal ulong CeCount;

        public override string ToString() =>
            $"{(HasEcnCounts ? nameof(FrameType.AckWithEcn) : nameof(FrameType.Ack))}[{LargestAcknowledged}]";

        internal override FrameType FrameType => HasEcnCounts ? FrameType.AckWithEcn : FrameType.Ack;

        internal override void Serialize(QuicWriter writer)
        {
            ImplFrame.Write(writer, new ImplFrame(
                LargestAcknowledged, AckDelay, (ulong) AckRanges.Count, FirstAckRange, ReadOnlySpan<byte>.Empty, HasEcnCounts, Ect0Count, Ect1Count, CeCount
                ));
        }

        internal override bool Deserialize(QuicReader reader)
        {
            if (!ImplFrame.Read(reader, out var frame))
            {
                return false;
            }

            LargestAcknowledged = frame.LargestAcknowledged;
            AckDelay = frame.AckDelay;
            FirstAckRange = frame.FirstAckRange;

            int read = 0;
            while (read < frame.AckRangesRaw.Length)
            {
                read += QuicPrimitives.ReadVarInt(frame.AckRangesRaw.Slice(read), out ulong gap);
                read += QuicPrimitives.ReadVarInt(frame.AckRangesRaw.Slice(read), out ulong ack);
                AckRanges.Add(new AckRange{ Acked = ack, Gap = gap});
            }

            HasEcnCounts = frame.HasEcnCounts;
            Ect0Count = frame.Ect0Count;
            Ect1Count = frame.Ect1Count;
            CeCount = frame.CeCount;

            return true;
        }
    }
}
