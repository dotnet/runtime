using System.Net.Quic.Implementations.Managed;
using System.Net.Quic.Implementations.Managed.Internal;
using System.Net.Quic.Implementations.Managed.Internal.Crypto;

namespace System.Net.Quic.Tests.Harness
{
    internal class TestHarnessContext
    {
        public TestHarnessContext(ManagedQuicConnection sender)
        {
            Sender = sender;
            ConnectionIdCollection.Add(sender.SourceConnectionId);
            ConnectionIdCollection.Add(sender.DestinationConnectionId);
        }

        internal ManagedQuicConnection Sender { get; }

        internal CryptoSeal GetRecvSeal(PacketType packetType)
        {
            // encryption is symmetric
            return GetSenderPacketNumberSpace(packetType).SendCryptoSeal;
        }

        internal ConnectionIdCollection ConnectionIdCollection { get; } = new ConnectionIdCollection();

        internal PacketNumberSpace GetSenderPacketNumberSpace(PacketType packetType)
        {
            var level = packetType switch
            {
                PacketType.Initial => EncryptionLevel.Initial,
                PacketType.ZeroRtt => EncryptionLevel.EarlyData,
                PacketType.Handshake => EncryptionLevel.Handshake,
                PacketType.OneRtt => EncryptionLevel.Application,
                _ => throw new InvalidOperationException()
            };

            return Sender.GetPacketNumberSpace(level);
        }
    }
}
