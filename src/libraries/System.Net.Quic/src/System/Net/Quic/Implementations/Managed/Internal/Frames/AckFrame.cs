using System.Diagnostics;

namespace System.Net.Quic.Implementations.Managed.Internal.Frames
{
    /// <summary>
    ///     Informs receiver about received and processed packets.
    /// </summary>
    internal readonly ref struct AckFrame
    {
        /// <summary>
        ///     Largest packet number being acknowledged; this is usually the largest packet number that was received
        ///     prior to generating the frame.
        /// </summary>
        internal readonly long LargestAcknowledged;

        /// <summary>
        ///     Time delta in microseconds between when this frame was sent and when the largest acknowledged packet, as
        ///     indicated in <see cref="LargestAcknowledged" />, was received. The value of this field is scaled by
        ///     multiplying the value by 2 to the power of the <see cref="TransportParameters.AckDelayExponent" />
        ///     transport parameter set by the sender.
        /// </summary>
        internal readonly long AckDelay;

        /// <summary>
        ///     Number of ack range fields in the frame.
        /// </summary>
        internal readonly long AckRangeCount;

        /// <summary>
        ///     Number of contiguous packets preceding the <see cref="LargestAcknowledged" /> that are being acknowledged.
        /// </summary>
        internal readonly long FirstAckRange;

        /// <summary>
        ///     Span with data about additional ranges, each item has two varint fields: gap and range.
        /// </summary>
        internal readonly ReadOnlySpan<byte> AckRangesRaw;

        /// <summary>
        ///     Flag indicating that <see cref="Ect0Count" />, <see cref="Ect1Count" /> and <see cref="CeCount" /> fields contain
        ///     valid values.
        /// </summary>
        internal readonly bool HasEcnCounts;

        /// <summary>
        ///     Total number of packets received with the ECT(0) codepoint in the packet number space of this frame. Contains valid
        ///     number only if <see cref="HasEcnCounts" /> is true.
        /// </summary>
        internal readonly long Ect0Count;

        /// <summary>
        ///     Total number of packets received with the ECT(1) codepoint in the packet number space of this frame.  Contains
        ///     valid number only if <see cref="HasEcnCounts" /> is true.
        /// </summary>
        internal readonly long Ect1Count;

        /// <summary>
        ///     Total number of packets received with the CE codepoint in the packet number space of this frame.  Contains valid
        ///     number only if <see cref="HasEcnCounts" /> is true.
        /// </summary>
        internal readonly long CeCount;

        /// <summary>
        ///     Returns number of bytes needed to serialize this frame.
        /// </summary>
        internal readonly int GetSerializedLength()
        {
            return GetOverhead(LargestAcknowledged, AckDelay, AckRangeCount, FirstAckRange) +
                   AckRangesRaw.Length +
                   (HasEcnCounts
                       ? QuicPrimitives.GetVarIntLength(Ect0Count) +
                         QuicPrimitives.GetVarIntLength(Ect1Count) +
                         QuicPrimitives.GetVarIntLength(CeCount)
                       : 0);

        }

        internal static int GetOverhead(long largestAcknowledged, long ackDelay, long ackRangeCount,
            long firstAckRange)
        {
            return 1 + // type
                   QuicPrimitives.GetVarIntLength(largestAcknowledged) +
                   QuicPrimitives.GetVarIntLength(ackDelay) +
                   QuicPrimitives.GetVarIntLength(ackRangeCount) +
                   QuicPrimitives.GetVarIntLength(firstAckRange);
        }

        internal AckFrame(long largestAcknowledged, long ackDelay, long ackRangeCount, long firstAckRange,
            ReadOnlySpan<byte> ackRangesRaw, bool hasEcnCounts, long ect0Count, long ect1Count, long ceCount)
        {
            LargestAcknowledged = largestAcknowledged;
            AckDelay = ackDelay;
            AckRangeCount = ackRangeCount;
            FirstAckRange = firstAckRange;
            AckRangesRaw = ackRangesRaw;
            HasEcnCounts = hasEcnCounts;
            Ect0Count = ect0Count;
            Ect1Count = ect1Count;
            CeCount = ceCount;
        }

        internal static bool Read(QuicReader reader, out AckFrame frame)
        {
            var type = reader.ReadFrameType();
            Debug.Assert(type == FrameType.Ack || type == FrameType.AckWithEcn);

            long ect0 = 0;
            long ect1 = 0;
            long ce = 0;

            if (!reader.TryReadVarInt(out long largest) ||
                !reader.TryReadVarInt(out long ackDelay) ||
                !reader.TryReadVarInt(out long rangeCount) ||
                !reader.TryReadVarInt(out long firstRange))
            {
                goto fail;
            }

            ReadOnlySpan<byte> rawRanges = reader.PeekSpan(reader.BytesLeft);
            int start = reader.BytesRead;
            for (uint i = 0; i < rangeCount; i++)
            {
                if (!reader.TryReadVarInt(out _) ||
                    !reader.TryReadVarInt(out _))
                {
                    goto fail;
                }
            }


            rawRanges = rawRanges.Slice(0, reader.BytesRead - start);

            if (type == FrameType.AckWithEcn &&
                (!reader.TryReadVarInt(out ect0) ||
                 !reader.TryReadVarInt(out ect1) ||
                 !reader.TryReadVarInt(out ce)))
            {
                goto fail;
            }

            frame = new AckFrame(largest, ackDelay, rangeCount, firstRange, rawRanges,
                type == FrameType.AckWithEcn,
                ect0, ect1, ce);
            return true;

            fail:
            frame = default;
            return false;
        }

        internal static void Write(QuicWriter writer, in AckFrame frame)
        {
            Debug.Assert(writer.BytesAvailable >= frame.GetSerializedLength());

            writer.WriteFrameType(frame.HasEcnCounts ? FrameType.AckWithEcn : FrameType.Ack);

            writer.WriteVarInt(frame.LargestAcknowledged);
            writer.WriteVarInt(frame.AckDelay);
            writer.WriteVarInt(frame.AckRangeCount);
            writer.WriteVarInt(frame.FirstAckRange);
            writer.WriteSpan(frame.AckRangesRaw);

            if (frame.HasEcnCounts)
            {
                writer.WriteVarInt(frame.Ect0Count);
                writer.WriteVarInt(frame.Ect1Count);
                writer.WriteVarInt(frame.CeCount);
            }
        }
    }
}
