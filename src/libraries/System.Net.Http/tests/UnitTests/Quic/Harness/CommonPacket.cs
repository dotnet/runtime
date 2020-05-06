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

    internal abstract class CommonPacket : LongHeaderPacket, IFramePacket
    {
        internal byte[] Token;

        public List<FrameBase> Frames { get; } = new List<FrameBase>();

        internal long Length;

        protected override string GetAdditionalInfo() => $": {string.Join(", ", Frames)}";

        internal override void Serialize(QuicWriter writer, ITestHarnessContext context)
        {
            base.Serialize(writer, context);

            SharedPacketData.Write(writer, new SharedPacketData(HeaderHelpers.ComposeLongHeaderByte(PacketType, PacketNumberLength), Token ?? Array.Empty<byte>(), 1000 /* anything with 2B encoding */));

            SerializePayloadWithFrames(writer, context, Frames);
        }

        internal override void Deserialize(QuicReader reader, ITestHarnessContext context)
        {
            base.Deserialize(reader, context);

            SharedPacketData.Read(reader, reader.Buffer.Span[0], out var data);

            Token = data.Token.IsEmpty ? null : data.Token.ToArray();
            Length = data.Length;

            (PacketNumberLength, PacketNumber) = DeserializePayloadWithFrames(reader, context, Frames, PacketType, (int) Length);

            // read these fields after decryption
            byte firstByte = reader.Buffer.Span[0];
            FixedBit = HeaderHelpers.GetFixedBit(firstByte);
            ReservedBits = HeaderHelpers.GetShortHeaderReservedBits(firstByte);
        }
    }
}
