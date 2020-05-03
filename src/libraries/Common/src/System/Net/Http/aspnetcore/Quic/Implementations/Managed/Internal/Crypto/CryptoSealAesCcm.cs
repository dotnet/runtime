using System.Diagnostics;
using System.Net.Security;
using System.Security.Cryptography;

namespace System.Net.Quic.Implementations.Managed.Internal.Crypto
{
    /// <summary>
    ///     Adapter for using AEAD_AES_128_CCM
    /// </summary>
    internal class CryptoSealAesCcm : CryptoSealAesBase
    {
        // AES-128 and AES-256 implementation for actual packet payload protection
        private readonly AesCcm _aesCcm;

        internal CryptoSealAesCcm(byte[] key, byte[] headerKey) : base(headerKey)
        {
            Debug.Assert(key.Length == 16);
            Debug.Assert(headerKey.Length == 16);

            _aesCcm = new AesCcm(key);
        }

        internal override TlsCipherSuite CipherSuite => TlsCipherSuite.TLS_AES_128_CCM_SHA256;
        internal override int TagLength => 16;

        internal override void Encrypt(ReadOnlySpan<byte> nonce, Span<byte> buffer, Span<byte> tag,
            ReadOnlySpan<byte> aad)
        {
            _aesCcm.Encrypt(nonce, buffer, buffer, tag, aad);
        }

        internal override bool Decrypt(ReadOnlySpan<byte> nonce, Span<byte> buffer, ReadOnlySpan<byte> tag, ReadOnlySpan<byte> aad)
        {
            try
            {
                _aesCcm.Decrypt(nonce, buffer, tag, buffer, aad);
                return true;
            }
            catch (CryptographicException)
            {
                return false;
            }
        }
    }
}
