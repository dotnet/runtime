using System.Collections.Generic;
using System.Linq;
using System.Net.Quic.Implementations.Managed.Internal;
using System.Net.Quic.Implementations.Managed.Internal.Crypto;
using System.Net.Quic.Implementations.Managed.Internal.Headers;
using Xunit;

namespace System.Net.Quic.Tests.Harness
{
    internal class OneRttPacket : PacketBase
    {
        internal override PacketType PacketType => PacketType.OneRtt;

        internal bool KeyPhase;

        internal bool SpinBit;

        internal List<FrameBase> Frames { get; } = new List<FrameBase>();

        protected override string GetAdditionalInfo() => $": {string.Join(", ", Frames)}";

        internal override void Serialize(QuicWriter writer, TestHarnessContext context)
        {
            ShortPacketHeader.Write(writer, new ShortPacketHeader(SpinBit, KeyPhase, PacketNumberLength, new ConnectionId(DestinationConnectionId)));

            SerializePayloadWithFrames(writer, context, Frames);
        }

        internal override void Deserialize(QuicReader reader, TestHarnessContext context)
        {
            Assert.True(ShortPacketHeader.Read(reader, context.ConnectionIdCollection, out var header));

            DestinationConnectionId = header.DestinationConnectionId.Data;

            (PacketNumberLength, PacketNumber) = DeserializePayloadWithFrames(reader, context, Frames, PacketType, reader.BytesLeft);

            // read these fields after decryption
            byte firstByte = reader.Buffer.Span[0];
            KeyPhase = HeaderHelpers.GetKeyPhase(firstByte);
            FixedBit = HeaderHelpers.GetFixedBit(firstByte);
            SpinBit = HeaderHelpers.GetSpinBit(firstByte);
            ReservedBits = HeaderHelpers.GetShortHeaderReservedBits(firstByte);
        }
    }
}
