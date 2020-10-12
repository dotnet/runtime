// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Quic.Implementations.Managed.Internal.Frames;

namespace System.Net.Quic.Implementations.Managed.Internal
{
    internal static class QuicReaderWriterExtensions
    {
        internal static bool TryReadLengthPrefixedSpan(this QuicReader reader, out ReadOnlySpan<byte> result)
        {
            if (reader.TryReadVarInt(out long length) &&
                reader.TryReadSpan((int)length, out result))
            {
                return true;
            }

            result = default;
            return false;
        }

        internal static void WriteLengthPrefixedSpan(this QuicWriter reader, in ReadOnlySpan<byte> data)
        {
            reader.WriteVarInt(data.Length);
            reader.WriteSpan(data);
        }

        internal static FrameType PeekFrameType(this QuicReader reader)
        {
            return (FrameType)reader.PeekVarInt();
        }

        internal static FrameType ReadFrameType(this QuicReader reader)
        {
            return (FrameType)reader.ReadVarInt();
        }

        internal static bool TryReadFrameType(this QuicReader reader, out FrameType frameType)
        {
            bool success = reader.TryReadVarInt(out long typeAsLong);
            frameType = (FrameType)typeAsLong;
            return success;
        }

        internal static void WriteFrameType(this QuicWriter writer, FrameType frameType)
        {
            writer.WriteVarInt((long)frameType);
        }

        internal static bool TryReadTransportParameterName(this QuicReader reader, out TransportParameterName name)
        {
            bool success = reader.TryReadVarInt(out long nameAsLong);
            name = (TransportParameterName)nameAsLong;
            return success;
        }

        internal static void WriteTransportParameterName(this QuicWriter writer, TransportParameterName name)
        {
            writer.WriteVarInt((long)name);
        }

        internal static bool TryReadStatelessResetToken(this QuicReader reader, out StatelessResetToken token)
        {
            if (!reader.TryReadSpan(StatelessResetToken.Length, out var data))
            {
                token = default;
                return false;
            }

            token = StatelessResetToken.FromSpan(data);
            return true;
        }

        internal static void WriteStatelessResetToken(this QuicWriter writer, in StatelessResetToken token)
        {
            StatelessResetToken.ToSpan(writer.GetWritableSpan(StatelessResetToken.Length), token);
        }

        internal static bool TryReadQuicVersion(this QuicReader reader, out QuicVersion version)
        {
            if (!reader.TryReadUInt32(out uint ver))
            {
                version = default;
                return false;
            }

            version = (QuicVersion)ver;
            return true;
        }

        internal static void WriteQuicVersion(this QuicWriter writer, QuicVersion version)
        {
            writer.WriteInt32((int) version);
        }

        internal static bool TryReadPacketNumber(this QuicReader reader, int length, long largestAcked, out long packetNumber)
        {
            bool success;

            long truncatedPn;

            switch (length)
            {
                case 1:
                {
                    success = reader.TryReadUInt8(out byte res);
                    truncatedPn = res;
                    break;
                }
                case 2:
                {
                    success = reader.TryReadUInt16(out ushort res);
                    truncatedPn = res;
                    break;
                }
                case 3:
                {
                    success = reader.TryReadInt24(out int res);
                    truncatedPn = res;
                    break;
                }
                case 4:
                {
                    success = reader.TryReadUInt32(out uint res);
                    truncatedPn = res;
                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException(nameof(length));
            }

            packetNumber = QuicPrimitives.DecodePacketNumber(largestAcked, truncatedPn, length);
            return success;
        }

        internal static void WriteTruncatedPacketNumber(this QuicWriter writer, int length, int truncatedPn)
        {
            switch (length)
            {
                case 1:
                    writer.WriteUInt8((byte) truncatedPn);
                    break;
                case 2:
                    writer.WriteInt16((short) truncatedPn);
                    break;
                case 3:
                    writer.WriteInt24(truncatedPn);
                    break;
                case 4:
                    writer.WriteInt32(truncatedPn);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(length));
            }
        }
    }
}
