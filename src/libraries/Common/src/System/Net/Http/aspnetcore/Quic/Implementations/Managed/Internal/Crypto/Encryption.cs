using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;

namespace System.Net.Quic.Implementations.Managed.Internal.Crypto
{
    internal enum Algorithm
    {
        AEAD_AES_128_GCM,
    }

    internal static class HeaderHelpers
    {
        public static bool IsLongHeader(byte firstByte)
        {
            return (firstByte & 0x80) != 0;
        }
    }

    internal class Encryption
    {
        public static byte[] GetHeaderProtectionMask(Algorithm alg, byte[] key, ReadOnlySpan<byte> sample)
        {
            var aead = new AesManaged()
            {
                KeySize = 128,
                Mode = CipherMode.ECB,
                Key = key,
            };

            return aead.CreateEncryptor().TransformFinalBlock(sample.ToArray(), 0, sample.Length).AsSpan(0, 5).ToArray();
        }

        public static void ProtectHeader(Span<byte> header, Span<byte> mask)
        {
            Debug.Assert(mask.Length == 5);
            int pnLength = (header[0] & 0x03) + 1;

            if (HeaderHelpers.IsLongHeader(header[0]))
            {
                header[0] ^= (byte)(mask[0] & 0x1f);
            }
            else
            {
                header[0] ^= (byte)(mask[0] & 0x0f);
            }

            int ii = 1;
            for (int i = header.Length - pnLength; i < header.Length; i++)
            {
                header[i] ^= mask[ii++ % mask.Length];
            }
        }
        public static int UnprotectHeader(Span<byte> header, Span<byte> mask, int pnOffset)
        {
            Debug.Assert(mask.Length == 5);

            if (HeaderHelpers.IsLongHeader(header[0]))
            {
                header[0] ^= (byte)(mask[0] & 0x1f);
            }
            else
            {
                header[0] ^= (byte)(mask[0] & 0x0f);
            }

            int pnLength = (header[0] & 0x03) + 1;
            int ii = 1;
            // header span is as if the packet number had full 4 bytes, so we need to adjust the boundary
            for (int i = pnOffset; i < pnOffset + pnLength; i++)
            {
                header[i] ^= mask[ii++ % mask.Length];
            }

            return pnLength;
        }
    }
}
