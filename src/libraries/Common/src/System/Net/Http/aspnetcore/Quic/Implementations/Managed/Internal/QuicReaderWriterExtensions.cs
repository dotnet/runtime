using System.Net.Quic.Implementations.Managed.Internal.Frames;
using System.Runtime.InteropServices;

namespace System.Net.Quic.Implementations.Managed.Internal
{
    internal static class QuicReaderWriterExtensions
    {
        internal static bool TryReadLengthPrefixedSpan(this QuicReader reader, out ReadOnlySpan<byte> result)
        {
            if (reader.TryReadVarInt(out ulong length) &&
                reader.TryReadSpan((int)length, out result))
            {
                return true;
            }

            result = default;
            return false;
        }

        internal static void WriteLengthPrefixedSpan(this QuicWriter reader, in ReadOnlySpan<byte> data)
        {
            reader.WriteVarInt((ulong)data.Length);
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
            bool success = reader.TryReadVarInt(out ulong typeAsUlong);
            frameType = (FrameType)typeAsUlong;
            return success;
        }

        internal static void WriteFrameType(this QuicWriter writer, FrameType frameType)
        {
            writer.WriteVarInt((ulong)frameType);
        }

        internal static bool TryReadTranportParameterName(this QuicReader reader, out TransportParameterName name)
        {
            bool success = reader.TryReadVarInt(out ulong nameAsUlong);
            name = (TransportParameterName)nameAsUlong;
            return success;
        }

        internal static void WriteTransportParameterName(this QuicWriter writer, TransportParameterName name)
        {
            writer.WriteVarInt((ulong)name);
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
            writer.WriteUInt32((uint) version);
        }

        internal static bool TryReadTruncatedPacketNumber(this QuicReader reader, int length, out uint truncatedPn)
        {
            bool success;

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
                    success = reader.TryReadUInt24(out uint res);
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

            return success;
        }

        internal static void WriteTruncatedPacketNumber(this QuicWriter writer, int length, uint truncatedPn)
        {
            switch (length)
            {
                case 1:
                    writer.WriteUInt8((byte) truncatedPn);
                    break;
                case 2:
                    writer.WriteUInt16((ushort) truncatedPn);
                    break;
                case 3:
                    writer.WriteUInt24(truncatedPn);
                    break;
                case 4:
                    writer.WriteUInt32(truncatedPn);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(length));
            }
        }
    }
}
