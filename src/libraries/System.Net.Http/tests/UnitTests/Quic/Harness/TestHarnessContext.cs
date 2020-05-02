using System.Collections.Generic;
using System.Net.Quic.Implementations.Managed;
using System.Net.Quic.Implementations.Managed.Internal;
using System.Net.Quic.Implementations.Managed.Internal.Crypto;

namespace System.Net.Quic.Tests.Harness
{
    internal class TestHarnessContext
    {
        public TestHarnessContext(ManagedQuicConnection sender, Dictionary<(ManagedQuicConnection, PacketType), CryptoSeal> sealMap)
        {
            Sender = sender;
            _sealMap = sealMap;
            ConnectionIdCollection.Add(sender.SourceConnectionId);
            ConnectionIdCollection.Add(sender.DestinationConnectionId);
        }

        private readonly Dictionary<(ManagedQuicConnection, PacketType), CryptoSeal> _sealMap;

        internal ManagedQuicConnection Sender { get; }

        internal CryptoSeal GetRecvSeal(PacketType packetType)
        {
            // encryption is symmetric
            return GetSendSeal(packetType);
        }

        internal CryptoSeal GetSendSeal(PacketType packetType)
        {
            if (!_sealMap.TryGetValue((Sender, packetType), out var seal))
            {
                // encryption is symmetric
                _sealMap[(Sender, packetType)] = seal = GetSenderPacketNumberSpace(packetType).SendCryptoSeal;
            }

            return seal;
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
