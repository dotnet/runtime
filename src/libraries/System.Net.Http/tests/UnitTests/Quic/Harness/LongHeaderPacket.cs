using System.Net.Quic.Implementations.Managed.Internal;
using System.Net.Quic.Implementations.Managed.Internal.Headers;
using System.Security.Cryptography;

namespace System.Net.Quic.Tests.Harness
{
    internal abstract class LongHeaderPacket : PacketBase
    {
        internal byte[] SourceConnectionId;

        internal QuicVersion Version;

        internal override void Serialize(QuicWriter writer, ITestHarnessContext context)
        {
            LongPacketHeader.Write(writer, new LongPacketHeader(PacketType, PacketNumberLength, Version, DestinationConnectionId, SourceConnectionId));
        }

        internal override void Deserialize(QuicReader reader, ITestHarnessContext context)
        {
            LongPacketHeader.Read(reader, out var header);
            SourceConnectionId = header.SourceConnectionId.ToArray();
            DestinationConnectionId = header.DestinationConnectionId.ToArray();
            Version = header.Version;
        }
    }
}
