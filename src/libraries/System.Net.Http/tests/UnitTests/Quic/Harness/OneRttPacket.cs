using System.Collections.Generic;
using System.Linq;
using System.Net.Quic.Implementations.Managed.Internal;
using System.Net.Quic.Implementations.Managed.Internal.Crypto;
using System.Net.Quic.Implementations.Managed.Internal.Headers;

namespace System.Net.Quic.Tests.Harness
{
    internal class OneRttPacket : PacketBase
    {
        internal override PacketType PacketType => PacketType.OneRtt;

        internal bool KeyPhase;

        internal bool SpinBit;

        internal List<FrameBase> Frames { get; } = new List<FrameBase>();

        protected override string GetAdditionalInfo() => $": {string.Join(", ", Frames)}";

        internal override void Serialize(QuicWriter writer, TestHarness context)
        {
            ShortPacketHeader.Write(writer, new ShortPacketHeader(SpinBit, KeyPhase, PacketNumberLength, new ConnectionId(DestinationConnectionId)));

            SerializePayloadWithFrames(writer, context, Frames);
        }

        internal override void Deserialize(QuicReader reader, TestHarness context)
        {
            ShortPacketHeader.Read(reader, context.ConnectionIdCollection, out var header);

            DestinationConnectionId = header.DestinationConnectionId.Data;

            (PacketNumberLength, PacketNumber) = DeserializePayloadWithFrames(reader, context, Frames, PacketType, reader.BytesLeft);

            // read these fields after decryption
            KeyPhase = HeaderHelpers.GetKeyPhase(reader.Buffer[0]);
            FixedBit = HeaderHelpers.GetFixedBit(reader.Buffer[0]);
            SpinBit = HeaderHelpers.GetSpinBit(reader.Buffer[0]);
            ReservedBits = HeaderHelpers.GetShortHeaderReservedBits(reader.Buffer[0]);
        }
    }
}
