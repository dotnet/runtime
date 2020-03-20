using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;

namespace System.Net.Quic.Implementations.Managed.Internal.Crypto
{
    internal enum Algorithm
    {
        AEAD_AES_128_GCM,
    }

    internal class Encryption
    {
        public static byte[] GetInitialNonce(byte[] iv, ulong packetNumber)
        {
            var nonce = (byte[]) iv.Clone();

            for (int i = 0; i < 8; i++)
            {
                nonce[nonce.Length - 1 - i] ^= (byte) (packetNumber >> (i * 8));
            }

            return nonce;
        }

        public static byte[] GetHeaderProtectionMask(Algorithm alg, byte[] key, byte[] sample)
        {
            var aead = new AesManaged()
            {
                KeySize = 128,
                Mode = CipherMode.ECB,
                Key = key,
            };

            return aead.CreateEncryptor().TransformFinalBlock(sample, 0, sample.Length).AsSpan(0, 5).ToArray();
        }

        public static void ProtectHeader(Span<byte> header, Span<byte> mask, int startOffset)
        {
            Debug.Assert(mask.Length == 5);
            Debug.Assert((uint) startOffset < header.Length);

            if ((header[0] & 0x80) != 0)
            {
                header[0] ^= (byte) (mask[0] & 0x1f);

                int ii = 1;
                for (int i = startOffset; i < header.Length; i++)
                {
                    header[i] ^= mask[ii++ % mask.Length];
                }
            }
            else
            {
                throw new NotImplementedException("Short header not implemented");
            }
        }
    }
}
