using System.Diagnostics;
using System.Security.Cryptography;

namespace System.Net.Quic.Implementations.Managed.Internal.Crypto
{
    /// <summary>
    ///     Adapter for using AEAD_AES_128_GCM and AEAD_AES_256_GCM for header protection.
    /// </summary>
    internal class CryptoSealAesGcm : CryptoSealAesBase
    {
        internal const int IntegrityTagLength = 16;
        // AES-128 and AES-256 implementation for actual packet payload protection
        private readonly AesGcm _aesGcm;

        internal CryptoSealAesGcm(byte[] key, byte[] headerKey) : base(headerKey)
        {
            Debug.Assert(key.Length == 16 || key.Length == 32);
            Debug.Assert(headerKey.Length == 16);

            _aesGcm = new AesGcm(key);
        }

        internal override int TagLength => IntegrityTagLength;

        internal override void Encrypt(ReadOnlySpan<byte> nonce, Span<byte> buffer, Span<byte> tag,
            ReadOnlySpan<byte> aad)
        {
            _aesGcm.Encrypt(nonce, buffer, buffer, tag, aad);
        }

        internal override bool Decrypt(ReadOnlySpan<byte> nonce, Span<byte> buffer, ReadOnlySpan<byte> tag, ReadOnlySpan<byte> aad)
        {
            try
            {
                _aesGcm.Decrypt(nonce, buffer, tag, buffer, aad);
                return true;
            }
            catch (CryptographicException)
            {
                return false;
            }
        }
    }
}
