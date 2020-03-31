using System.Net.Quic.Implementations.Managed;
using System.Net.Quic.Implementations.Managed.Internal;
using System.Net.Quic.Implementations.Managed.Internal.Crypto;

namespace System.Net.Quic.Tests.Harness
{
    internal class TestHarness
    {
        public TestHarness(ManagedQuicConnection sender)
        {
            Sender = sender;
            ConnectionIdCollection.Add(sender.SourceConnectionId.Data);
            ConnectionIdCollection.Add(sender.DestinationConnectionId.Data);
        }

        internal ManagedQuicConnection Sender { get; }

        internal CryptoSeal GetRecvSeal(PacketType packetType)
        {
            // encryption is symmetric
            return GetSenderEpoch(packetType).SendCryptoSeal;
        }

        internal ConnectionIdCollection ConnectionIdCollection { get; } = new ConnectionIdCollection();

        internal EpochData GetSenderEpoch(PacketType packetType)
        {
            var level = packetType switch
            {
                PacketType.Initial => EncryptionLevel.Initial,
                PacketType.ZeroRtt => EncryptionLevel.EarlyData,
                PacketType.Handshake => EncryptionLevel.Handshake,
                PacketType.OneRtt => EncryptionLevel.Application,
                _ => throw new InvalidOperationException()
            };

            return Sender.GetEpoch(level);
        }
    }
}
