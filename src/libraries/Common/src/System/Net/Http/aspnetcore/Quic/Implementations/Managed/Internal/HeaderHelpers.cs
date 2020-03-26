using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;

namespace System.Net.Quic.Implementations.Managed.Internal.Crypto
{
    internal static class HeaderHelpers
    {
        internal const byte FormBitMask = 0x80;
        private const byte TypeBitsMask = 0x30;

        internal const int MaxConnectionIdLength = 160/8;

        public static bool IsLongHeader(byte firstByte)
        {
            return (firstByte & FormBitMask) != 0;
        }

        public static PacketType GetPacketType(byte firstByte)
        {
            return (PacketType) (firstByte & FormBitMask >> 4);
        }
    }
}
