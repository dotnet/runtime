using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;

namespace System.Net.Quic.Implementations.Managed.Internal.Crypto
{
    internal static class KeyDerivation
    {
        private static readonly HashAlgorithmName hashAlgorithm = HashAlgorithmName.SHA256;
        private const int InitialSecretLength = 256 / 8;

        public static readonly byte[] initialSalt =
        {
            0xc3, 0xee, 0xf7, 0x12, 0xc7, 0x2e, 0xbb, 0x5a, 0x11, 0xa7, 0xd2, 0x43, 0x2b, 0xb4, 0x63, 0x65, 0xbe,
            0xf9, 0xf5, 0x02
        };

        public static byte[] ExpandLabel(ReadOnlySpan<byte> prk, string label, ushort length)
        {
            // create the hkdfLabel structure locally, as defined by the RFC8446, Section 7.1
            // Context is empty string (with null terminator) in our case
            // struct {
            //     uint16 length = Length;
            //     opaque label<7..255> = "tls13 " + Label;
            //     opaque context<0..255> = Context;
            // } HkdfLabel;

            const string prefix = "tls13 ";
            Span<byte> hkdfLabel = stackalloc byte[sizeof(short) + 1 + prefix.Length + label.Length + 1];
            // length is in big endian
            BinaryPrimitives.WriteUInt16BigEndian(hkdfLabel, length);

            // label is length prefixed
            hkdfLabel[2] = (byte)(label.Length + prefix.Length);
            Encoding.ASCII.GetBytes(prefix, hkdfLabel.Slice(3));
            Encoding.ASCII.GetBytes(label, hkdfLabel.Slice(3 + prefix.Length));

            var result = new byte[length];

            HKDF.Expand(hashAlgorithm, prk, result, hkdfLabel);
            return result;
        }

        private static void DeriveInitialSecret(ReadOnlySpan<byte> dcid, Span<byte> secret)
        {
            HKDF.Extract(hashAlgorithm, dcid, initialSalt, secret);
        }

        public static byte[] DeriveClientInitialSecret(ReadOnlySpan<byte> dcid)
        {
            Span<byte> initial = stackalloc byte[InitialSecretLength];
            DeriveInitialSecret(dcid, initial);
            return ExpandLabel(initial, "client in", 32);
        }

        public static byte[] DeriveServerInitialSecret(ReadOnlySpan<byte> dcid)
        {
            Span<byte> initial = stackalloc byte[InitialSecretLength];
            DeriveInitialSecret(dcid, initial);
            return ExpandLabel(initial, "server in", 32);
        }

        public static byte[] DeriveKey(ReadOnlySpan<byte> prk)
        {
            return ExpandLabel(prk, "quic key", 16);
        }

        public static byte[] DeriveIv(ReadOnlySpan<byte> prk)
        {
            return ExpandLabel(prk, "quic iv", 12);
        }

        public static byte[] DeriveHp(ReadOnlySpan<byte> prk)
        {
            return ExpandLabel(prk, "quic hp", 16);
        }
    }
}
