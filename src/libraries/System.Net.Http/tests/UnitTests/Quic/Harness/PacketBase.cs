using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Quic.Implementations.Managed.Internal;
using System.Net.Quic.Implementations.Managed.Internal.Crypto;
using Xunit;

namespace System.Net.Quic.Tests.Harness
{
    internal abstract class PacketBase
    {
        internal byte[] DestinationConnectionId;

        internal abstract PacketType PacketType { get; }

        internal ulong PacketNumber;

        internal bool FixedBit = true;

        internal byte ReservedBits = 0;

        internal int PacketNumberLength;

        public override string ToString() => $"{PacketType}[{PacketNumber}]{GetAdditionalInfo()}";

        protected virtual string GetAdditionalInfo()
        {
            return "";
        }

        internal abstract void Serialize(QuicWriter writer, TestHarness context);

        internal abstract void Deserialize(QuicReader reader, TestHarness context);

        internal static List<PacketBase> ParseMany(byte[] buffer, int count, TestHarness context)
        {
            var segment = new ArraySegment<byte>(buffer, 0, count);
            var reader = new QuicReader(segment);

            var packets = new List<PacketBase>();

            while (reader.BytesLeft > 0)
            {
                packets.Add(Parse(reader, context));

                segment = segment.Slice(reader.BytesRead);
                reader.Reset(segment);
            }

            return packets;
        }

        internal static PacketBase Parse(QuicReader reader, TestHarness context)
        {
            var type = HeaderHelpers.GetPacketType(reader.PeekUInt8());
            PacketBase packet = type switch
            {
                PacketType.Initial => new InitialPacket(),
                PacketType.ZeroRtt => new ZeroRttPacket(),
                PacketType.Handshake => new HandShakePacket(),
                PacketType.Retry => throw new NotImplementedException(),
                PacketType.OneRtt => new OneRttPacket(),
                PacketType.VersionNegotiation => throw new NotImplementedException(),
                _ => throw new InvalidOperationException("Invalid packet type")
            };

            packet.Deserialize(reader, context);
            return packet;
        }

        protected void SerializePayloadWithFrames(QuicWriter writer, TestHarness context, IEnumerable<FrameBase> frames)
        {
            var seal = context.GetSenderEpoch(PacketType).SendCryptoSeal;

            // this more or less duplicates code inside ManagedQuicConnection

            int pnOffset = writer.BytesWritten;

            writer.WriteTruncatedPacketNumber(PacketNumberLength, (uint) PacketNumber);
            var payloadLengthSpan = writer.Buffer.AsSpan(writer.BytesWritten - 2 - PacketNumberLength, 2);

            foreach (FrameBase frame in frames)
            {
                frame.Serialize(writer);
            }

            if (PacketType == PacketType.Initial)
            {
                // Pad initial packets to the minimum size
                int paddingLength = QuicConstants.MinimumClientInitialDatagramSize - seal.TagLength - writer.BytesWritten;
                if (paddingLength > 0)
                    // zero bytes are equivalent to PADDING frames
                    writer.GetWritableSpan(paddingLength).Clear();
            }

            // reserve space for AEAD integrity tag
            writer.GetWritableSpan(seal.TagLength);
            int payloadLength = writer.BytesWritten - pnOffset;

            // fill in the payload length retrospectively
            if (PacketType != PacketType.OneRtt)
            {
                QuicPrimitives.WriteVarInt(payloadLengthSpan, (ulong) payloadLength, 2);
            }

            seal.EncryptPacket(writer.Buffer, pnOffset, payloadLength, (uint) PacketNumber);
        }

        protected (int pnLength, ulong packetNumber) DeserializePayloadWithFrames(QuicReader reader, TestHarness harness, List<FrameBase> frames, PacketType packetType, int payloadLength)
        {
            // this more or less duplicates code inside ManagedQuicConnection
            int pnOffset = reader.BytesRead;

            // first, strip packet protection.
            var epoch = harness.GetSenderEpoch(packetType);

            var seal = harness.GetRecvSeal(packetType);

            // guess largest acked packet number to make deserialization work
            Assert.True(seal.DecryptPacket(reader.Buffer, pnOffset, payloadLength, (ulong) Math.Max(0, (int) epoch.NextPacketNumber - 3)));

            int pnLength = HeaderHelpers.GetPacketNumberLength(reader.Buffer[0]);
            reader.TryReadTruncatedPacketNumber(pnLength, out uint truncatedPn);

            var originalSegment = reader.Buffer;
            reader.Reset(reader.Buffer.Slice(reader.BytesRead, payloadLength - pnLength - seal.TagLength));
            while (reader.BytesLeft > 0)
            {
                frames.Add(FrameBase.Parse(reader));
            }
            reader.Reset(originalSegment, pnOffset + payloadLength);

            return (pnLength, QuicPrimitives.DecodePacketNumber(epoch.NextPacketNumber, truncatedPn, pnLength));
        }
    }
}
