using System.Collections.Generic;
using System.Linq;
using System.Net.Quic.Implementations.Managed.Internal;
using System.Net.Quic.Implementations.Managed.Internal.Crypto;
using System.Net.Quic.Implementations.Managed.Internal.Headers;

namespace System.Net.Quic.Tests.Harness
{
    internal class InitialPacket : CommonPacket
    {
        internal override PacketType PacketType => PacketType.Initial;
    }

    internal class HandShakePacket : CommonPacket
    {
        internal override PacketType PacketType => PacketType.Handshake;
    }

    internal class ZeroRttPacket : CommonPacket
    {
        internal override PacketType PacketType => PacketType.ZeroRtt;
    }

    internal abstract class CommonPacket : LongHeaderPacket
    {
        internal byte[] Token;

        internal List<FrameBase> Frames { get; } = new List<FrameBase>();

        internal ulong Length;

        protected override string GetAdditionalInfo() => $": {string.Join(", ", Frames.Where(f => f.FrameType != FrameType.Padding))}";

        internal override void Serialize(QuicWriter writer, TestHarness context)
        {
            base.Serialize(writer, context);

            SharedPacketData.Write(writer, new SharedPacketData(HeaderHelpers.ComposeLongHeaderByte(PacketType, PacketNumberLength), Token ?? Array.Empty<byte>(), Length));

            SerializePayloadWithFrames(writer, context, Frames);
        }

        internal override void Deserialize(QuicReader reader, TestHarness context)
        {
            base.Deserialize(reader, context);

            SharedPacketData.Read(reader, reader.Buffer[0], out var data);

            Token = data.Token.IsEmpty ? null : data.Token.ToArray();
            Length = data.Length;

            (PacketNumberLength, PacketNumber) = DeserializePayloadWithFrames(reader, context, Frames, PacketType, (int) Length);

            // read these fields after decryption
            FixedBit = HeaderHelpers.GetFixedBit(reader.Buffer[0]);
            ReservedBits = HeaderHelpers.GetShortHeaderReservedBits(reader.Buffer[0]);
        }
    }
}
