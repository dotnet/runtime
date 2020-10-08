using System.Buffers.Binary;

namespace System.Net.Quic.Implementations.Managed.Internal.Headers
{
    /// <summary>
    ///     Data present in the version negotiation packet
    /// </summary>
    internal readonly ref struct VersionNegotiationPacketData
    {
        internal readonly ref struct QuicVersionCollection
        {
            private readonly ReadOnlySpan<byte> rawData;

            public QuicVersionCollection(ReadOnlySpan<byte> rawData)
            {
                this.rawData = rawData;
            }

            internal QuicVersion this[int i]
            {
                get => (QuicVersion)BinaryPrimitives.ReadUInt32BigEndian(rawData.Slice(sizeof(uint) * i));
            }

            internal int Count => rawData.Length / sizeof(uint);
        }

        /// <summary>
        ///     List of versions the peer supports.
        /// </summary>
        internal readonly QuicVersionCollection SupportedVersions;

        internal VersionNegotiationPacketData(ReadOnlySpan<byte> rawData)
        {
            SupportedVersions = new QuicVersionCollection(rawData);
        }

        internal static bool Read(QuicReader reader, out VersionNegotiationPacketData data)
        {
            // Cast automatically trims the excess bytes to multiple of QuicVersion size
            data = new VersionNegotiationPacketData(reader.ReadSpan(reader.BytesLeft));
            return true;
        }

        internal static void Write(QuicWriter writer, ReadOnlySpan<QuicVersion> supportedVersions)
        {
            foreach (var version in supportedVersions)
            {
                writer.WriteQuicVersion(version);
            }
        }
    }
}
