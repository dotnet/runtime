using System.Net.Quic.Implementations.Managed.Internal;
using System.Net.Quic.Implementations.Managed.Internal.Crypto;
using Xunit;

namespace System.Net.Quic.Tests
{
    public class QuicReaderWriterTest
    {
        [Theory]
        [InlineData(37, new byte[] {0x25})]
        [InlineData(15293, new byte[] {0x7b, 0xbd})]
        [InlineData(494878333, new byte[] {0x9d, 0x7f, 0x3e, 0x7d})]
        [InlineData(151288809941952652, new byte[] {0xc2, 0x19, 0x7c, 0x5e, 0xff, 0x14, 0xe8, 0x8c})]
        public void TestVarintEncoding(ulong value, byte[] expected)
        {
            var actual = new byte[expected.Length];

            var writer = new QuicWriter(actual);
            writer.WriteVarInt(value);

            Assert.Equal(expected.Length, writer.BytesWritten);
            Assert.Equal(expected, actual);

            var reader = new QuicReader(actual);
            Assert.True(reader.TryReadVarInt(out ulong decoded));

            Assert.Equal(writer.BytesWritten, reader.BytesRead);
            Assert.Equal(value, decoded);
        }

        [Fact]
        public void TestPacketNumberEncoding()
        {
            ulong packetNumber = 0xa82f9b32;
            ulong lastAcked = 0xa82f30ea;
            ulong truncated = 0x9b32;

            int bytes = QuicEncoding.GetPacketNumberByteCount(lastAcked, packetNumber);
            Assert.Equal(2, bytes);

            ulong actual = QuicEncoding.DecodePacketNumber(lastAcked, truncated, 2);

            Assert.Equal(packetNumber, actual);
        }
    }
}
