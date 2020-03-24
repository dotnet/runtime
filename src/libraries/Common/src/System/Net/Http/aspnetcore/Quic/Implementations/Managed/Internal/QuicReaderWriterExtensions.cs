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
    }
}
