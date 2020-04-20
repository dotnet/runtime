using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Quic.Implementations.Managed.Internal;
using System.Text;

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
            internal long Gap;
            internal long Acked;

            public override string ToString() => $"(Gap={Gap}, Acked={Acked})";
        }

        /// <summary>
        ///     Largest packet number being acknowledged; this is usually the largest packet number that was received
        ///     prior to generating the frame.
        /// </summary>
        internal long LargestAcknowledged;

        /// <summary>
        ///     Time delta in microseconds between when this frame was sent and when the largest acknowledged packet, as
        ///     indicated in <see cref="LargestAcknowledged" />, was received. The value of this field is scaled by
        ///     multiplying the value by 2 to the power of the <see cref="TransportParameters.AckDelayExponent" />
        ///     transport parameter set by the sender.
        /// </summary>
        internal long AckDelay;

        /// <summary>
        ///     Number of contiguous packets preceding the <see cref="LargestAcknowledged" /> that are being acknowledged.
        /// </summary>
        internal long FirstAckRange;

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
        internal long Ect0Count;

        /// <summary>
        ///     Total number of packets received with the ECT(1) codepoint in the packet number space of this frame.  Contains
        ///     valid number only if <see cref="HasEcnCounts" /> is true.
        /// </summary>
        internal long Ect1Count;

        /// <summary>
        ///     Total number of packets received with the CE codepoint in the packet number space of this frame.  Contains valid
        ///     number only if <see cref="HasEcnCounts" /> is true.
        /// </summary>
        internal long CeCount;

        public override string ToString() =>
            $"{(HasEcnCounts ? nameof(FrameType.AckWithEcn) : nameof(FrameType.Ack))}[{GetRangesString()}]";

        private string GetRangesString()
        {
            StringBuilder builder = new StringBuilder();
            builder.Append(LargestAcknowledged);
            var lastAcked = LargestAcknowledged - FirstAckRange;
            if (lastAcked != LargestAcknowledged)
            {
                builder.Append('-');
                builder.Append(lastAcked);
            }

            foreach (var range in AckRanges)
            {
                builder.Append(lastAcked - range.Gap - 2);
                long nextLast = lastAcked - range.Gap - range.Acked - 2;

                if (lastAcked != nextLast)
                {
                    builder.Append('-');
                    builder.Append(nextLast);
                    lastAcked = nextLast;
                }
            }

            return builder.ToString();
        }

        internal override FrameType FrameType => HasEcnCounts ? FrameType.AckWithEcn : FrameType.Ack;

        internal override void Serialize(QuicWriter writer)
        {
            ImplFrame.Write(writer, new ImplFrame(
                LargestAcknowledged, AckDelay, AckRanges.Count, FirstAckRange, ReadOnlySpan<byte>.Empty, HasEcnCounts, Ect0Count, Ect1Count, CeCount
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
                read += QuicPrimitives.TryReadVarInt(frame.AckRangesRaw.Slice(read), out long gap);
                read += QuicPrimitives.TryReadVarInt(frame.AckRangesRaw.Slice(read), out long ack);
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
