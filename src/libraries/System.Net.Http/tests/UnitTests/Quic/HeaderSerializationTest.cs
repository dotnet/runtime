using System.Net.Quic.Implementations.Managed.Internal;
using System.Net.Quic.Implementations.Managed.Internal.Crypto;
using System.Net.Quic.Implementations.Managed.Internal.Headers;
using Xunit;

namespace System.Net.Quic.Tests
{
    public class HeaderSerializationTest
    {
        [Fact]
        public void DeserializeReferenceClientInitialPacket()
        {
            var reader = new QuicReader(HexHelpers.FromHexString(ReferenceData.ClientInitialPacketHeaderHex));

            Assert.True(HeaderHelpers.IsLongHeader(HexHelpers.FromHexString(ReferenceData.ClientInitialPacketHeaderHex)[0]));
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
            Assert.Equal(ReferenceData.ClientInitialPayloadLength, (int) data.Length - CryptoSealAesGcm.IntegrityTagLength - data.PacketNumberLength);
            Assert.Equal(2ul, data.TruncatedPacketNumber);
        }
    }
}
