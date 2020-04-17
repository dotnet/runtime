using System.Linq;
using System.Net.Quic.Implementations.Managed.Internal;
using System.Net.Quic.Implementations.Managed.Internal.Crypto;
using System.Net.Quic.Implementations.Managed.Internal.Headers;
using Xunit;

namespace System.Net.Quic.Tests
{
    public class PacketSerializationTest
    {
        public PacketSerializationTest()
        {
            buffer = new byte[2048];
            reader = new QuicReader(buffer);
            writer = new QuicWriter(buffer);
        }

        private readonly QuicReader reader;
        private readonly QuicWriter writer;

        private readonly byte[] buffer;

        [Fact]
        public void DeserializeReferenceClientInitialPacket()
        {
            // init packet data for reader
            HexHelpers.FromHexString(ReferenceData.ClientInitialPacketHeaderHex, buffer);
            reader.Reset(buffer.AsMemory(0, ReferenceData.ClientInitialPacketHeaderHex.Length / 2));

            Assert.True(HeaderHelpers.IsLongHeader(buffer[0]));

            Assert.True(LongPacketHeader.Read(reader, out var header));
            Assert.True(SharedPacketData.Read(reader, header.FirstByte, out var data));

            Assert.True(header.FixedBit);
            Assert.Equal(PacketType.Initial, header.PacketType);
            Assert.Equal(QuicVersion.Draft27, header.Version);
            Assert.Equal(ReferenceData.DcidHex, HexHelpers.ToHexString(header.DestinationConnectionId));
            Assert.True(header.SourceConnectionId.IsEmpty);

            Assert.Equal(0, data.ReservedBits);
            Assert.Equal(4, data.PacketNumberLength);
            // length includes packet number and integrity tag
            Assert.Equal(ReferenceData.ClientInitialPayloadLength,
                (int)data.Length - CryptoSealAesGcm.IntegrityTagLength - data.PacketNumberLength);

            // packet number left unread
            Assert.Equal(header.PacketNumberLength, reader.BytesLeft);
            Assert.True(reader.TryReadTruncatedPacketNumber(header.PacketNumberLength, out int pn));
            Assert.Equal(2, pn);
        }

        [Fact]
        public void SerializeLongPacketHeader()
        {
            int pnLength = 2;
            var scid = new byte[] {1, 2, 3, 4, 5, 6, 7, 8, 9, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 0};
            var dcid = scid.Select(i => (byte)(i * 10)).ToArray();
            var expected = new LongPacketHeader(PacketType.Initial, pnLength, QuicVersion.Draft27, dcid, scid);
            Assert.Equal(PacketType.Initial, expected.PacketType);

            LongPacketHeader.Write(writer, expected);
            reader.Reset(buffer.AsMemory(0, writer.BytesWritten));
            Assert.True(LongPacketHeader.Read(reader, out var actual));

            Assert.Equal(expected.FirstByte, actual.FirstByte);
            Assert.Equal(expected.FixedBit, actual.FixedBit);
            Assert.Equal(expected.PacketType, actual.PacketType);
            Assert.Equal(pnLength, actual.PacketNumberLength);
            Assert.Equal(0, actual.ReservedBits);
            Assert.Equal(expected.Version, actual.Version);
            Assert.True(actual.SourceConnectionId.SequenceEqual(scid));
            Assert.True(actual.DestinationConnectionId.SequenceEqual(dcid));

            Assert.Equal(0, reader.BytesLeft);
        }

        [Fact]
        public void SerializeRetryPacketData()
        {
            var token = new byte[] {1, 2, 3, 4, 5, 6, 7, 8, 9, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 0};
            var tag = new byte[] {1, 2, 3, 4, 5, 6, 7, 8, 9, 0, 1, 2, 3, 4, 5, 6};
            Assert.Equal(16, tag.Length);

            var expected = new RetryPacketData(token, tag);

            RetryPacketData.Write(writer, expected);
            reader.Reset(buffer.AsMemory(0, writer.BytesWritten));
            Assert.True(RetryPacketData.Read(reader, out var actual));

            Assert.True(actual.RetryToken.SequenceEqual(token));
            Assert.True(actual.RetryIntegrityTag.SequenceEqual(tag));

            Assert.Equal(0, reader.BytesLeft);
        }

        [Fact]
        public void SerializeSharedPacketData()
        {
            int pnLength = 2;
            var token = new byte[] {1, 2, 3, 4, 5, 6, 7, 8, 9, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 0};
            byte firstByte = HeaderHelpers.ComposeLongHeaderByte(PacketType.Initial, pnLength);

            var expected = new SharedPacketData(firstByte, token, 1234);

            SharedPacketData.Write(writer, expected);
            reader.Reset(buffer.AsMemory(0, writer.BytesWritten));
            Assert.True(SharedPacketData.Read(reader, firstByte, out var actual));

            Assert.Equal(expected.Length, actual.Length);
            Assert.Equal(expected.PacketNumberLength, actual.PacketNumberLength);
            Assert.Equal(expected.ReservedBits, actual.ReservedBits);
            Assert.Equal(0, actual.ReservedBits);
            Assert.True(actual.Token.SequenceEqual(token));

            Assert.Equal(0, reader.BytesLeft);
        }

        [Fact]
        public void SerializeShortPacketHeader()
        {
            var dcid = new ConnectionId(new byte[] {1, 2, 3, 4, 5, 6, 7, 8, 9, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 0});
            var idCollection = new ConnectionIdCollection();
            idCollection.Add(dcid);

            var expected = new ShortPacketHeader(true, false, 2, dcid);

            ShortPacketHeader.Write(writer, expected);
            reader.Reset(buffer.AsMemory(0, writer.BytesWritten));
            Assert.True(ShortPacketHeader.Read(reader, idCollection, out var actual));

            Assert.Equal(expected.FirstByte, actual.FirstByte);
            Assert.Equal(0, actual.ReservedBits);
            Assert.True(actual.SpinBit);
            Assert.False(actual.KeyPhaseBit);
            Assert.Equal(2, actual.PacketNumberLength);
            Assert.Equal(dcid.Data, actual.DestinationConnectionId.Data);

            Assert.Equal(0, reader.BytesLeft);
        }

        [Fact]
        public void SerializeVersionNegotiationPacketData()
        {
            QuicVersion[] versions = {QuicVersion.Draft27, QuicVersion.Negotiation, QuicVersion.Quic1};
            VersionNegotiationPacketData.Write(writer, versions);
            reader.Reset(buffer.AsMemory(0, writer.BytesWritten));
            Assert.True(VersionNegotiationPacketData.Read(reader, out var data));

            Assert.Equal(versions.Length, data.SupportedVersions.Count);
            for (int i = 0; i < versions.Length; i++)
            {
                Assert.Equal(versions[i], data.SupportedVersions[i]);
            }

            Assert.Equal(0, reader.BytesLeft);
        }
    }
}
