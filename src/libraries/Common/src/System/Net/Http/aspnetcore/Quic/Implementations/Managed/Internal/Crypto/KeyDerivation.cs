using System.Security.Cryptography;
using System.Text;

namespace System.Net.Quic.Implementations.Managed.Internal.Crypto
{
    internal static class KeyDerivation
    {
        private static readonly HashAlgorithmName hashAlgorithm = HashAlgorithmName.SHA256;

        public static readonly byte[] initialSalt =
        {
            0xc3, 0xee, 0xf7, 0x12, 0xc7, 0x2e, 0xbb, 0x5a, 0x11, 0xa7, 0xd2, 0x43,
            0x2b, 0xb4, 0x63, 0x65, 0xbe, 0xf9, 0xf5, 0x02
        };

        public static byte[] ExpandLabel(byte[] prk, string label, ushort length)
        {
            var hkdfLabel = CreateHkdfLabel(label, length);

            return HKDF.Expand(hashAlgorithm, prk, length, hkdfLabel);
        }

        public static byte[] CreateHkdfLabel(string label, ushort length)
        {
            const string prefix = "tls13 ";
            var hkdfLabel = new byte[sizeof(short) + 1 + prefix.Length + label.Length + 1];
            hkdfLabel[0] = (byte) (length >> 8);
            hkdfLabel[1] = (byte) length;

            hkdfLabel[2] = (byte) (label.Length + prefix.Length);

            Encoding.ASCII.GetBytes(prefix, hkdfLabel.AsSpan(3));
            Encoding.ASCII.GetBytes(label, hkdfLabel.AsSpan(3 + prefix.Length));
            return hkdfLabel;
        }

        public static byte[] DeriveInitialSecret(byte[] dcid)
        {
            return HKDF.Extract(hashAlgorithm, dcid, initialSalt);
        }

        public static byte[] DeriveClientInitialSecret(byte[] prk)
        {
            return ExpandLabel(prk, "client in", 32);
        }

        public static byte[] DeriveServerInitialSecret(byte[] prk)
        {
            return ExpandLabel(prk, "server in", 32);
        }

        public static byte[] DeriveKey(byte[] prk)
        {
            return ExpandLabel(prk, "quic key", 16);
        }

        public static byte[] DeriveIv(byte[] prk)
        {
            return ExpandLabel(prk, "quic iv", 12);
        }

        public static byte[] DeriveHp(byte[] prk)
        {
            return ExpandLabel(prk, "quic hp", 16);
        }
    }
}
