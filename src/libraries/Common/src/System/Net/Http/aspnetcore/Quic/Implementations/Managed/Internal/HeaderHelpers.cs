using System.Diagnostics;
using System.Net.Security;

namespace System.Net.Quic.Implementations.Managed.Internal
{
    internal static class QuicConstants
    {
        internal const int MinimumClientInitialDatagramSize = 1200;

        internal const TlsCipherSuite InitialCipherSuite = TlsCipherSuite.TLS_AES_128_GCM_SHA256;

        internal const int MinimumPacketSize = 21;
    }

    internal static class HeaderHelpers
    {
        // shared
        private const byte FormBitMask = 0x80;
        private const byte FixedBitMask = 0x40;
        private const byte PacketNumberLengthMask = 0x03;

        // long header bits
        private const byte TypeBitsMask = 0x30;
        private const byte LongReservedBitsMask = 0x0c;

        // short header bits
        private const byte SpinBitMask = 0x20;
        private const byte ShortReservedBitsMask = 0x18;
        private const byte KeyPhaseBitMask = 0x04;

        internal const int MaxConnectionIdLength = 160/8;

        internal static bool IsLongHeader(byte firstByte)
        {
            return (firstByte & FormBitMask) != 0;
        }

        internal static bool HasPacketTypeEncryption(PacketType type)
        {
            return type switch
            {
                PacketType.Initial => true,
                PacketType.ZeroRtt => true,
                PacketType.Handshake => true,
                PacketType.OneRtt => true,
                _ => false
            };
        }

        internal static PacketType GetLongPacketType(byte firstByte)
        {
            Debug.Assert(IsLongHeader(firstByte));
            return (PacketType) ((firstByte & TypeBitsMask) >> 4);
        }

        internal static PacketType GetPacketType(byte firstByte)
        {
            return IsLongHeader(firstByte)
                ? GetLongPacketType(firstByte)
                : PacketType.OneRtt; // short packets are only used for 1-RTT
        }

        internal static int GetPacketNumberLength(byte firstByte)
        {
            return (firstByte & PacketNumberLengthMask) + 1;
        }

        internal static byte ComposeShortHeaderByte(bool spin, bool keyPhase, int packetNumberLength)
        {
            Debug.Assert((uint) packetNumberLength - 1 <= 3, "Too large packet number encoding");

            // Fixed bit is always 1, reserved bits always 0
            int firstByte = (packetNumberLength - 1) | FixedBitMask;
            if (spin) firstByte |= SpinBitMask;
            if (keyPhase) firstByte |= KeyPhaseBitMask;

            return (byte)firstByte;
        }

        internal static byte ComposeLongHeaderByte(PacketType packetType, int packetNumberLength)
        {
            Debug.Assert((uint) packetType <= 3, "Wrong type of packet when creating long header.");
            Debug.Assert((uint) packetNumberLength - 1 <= 3, "Too large packet number encoding");

            // first two bits are always set (form + fixed bit)
            return (byte)(0xc0 | ((int)packetType << 4) | (packetNumberLength - 1));
        }

        internal static bool GetFixedBit(byte firstByte)
        {
            return (firstByte & FixedBitMask) != 0;
        }

        internal static bool GetSpinBit(byte firstByte)
        {
            return (firstByte & SpinBitMask) != 0;
        }

        internal static byte GetShortHeaderReservedBits(byte firstByte)
        {
            return (byte)((firstByte & ShortReservedBitsMask) >> 3);
        }

        internal static byte GetLongHeaderReservedBits(byte firstByte)
        {
            return (byte)((firstByte & LongReservedBitsMask) >> 2);
        }

        internal static bool GetKeyPhase(byte firstByte)
        {
            return (firstByte & KeyPhaseBitMask) != 0;
        }
    }
}
