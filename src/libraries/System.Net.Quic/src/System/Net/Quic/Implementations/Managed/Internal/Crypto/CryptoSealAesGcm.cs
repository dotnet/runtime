// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Net.Security;
using System.Security.Cryptography;

namespace System.Net.Quic.Implementations.Managed.Internal.Crypto
{
    /// <summary>
    ///     Adapter for using AEAD_AES_128_GCM and AEAD_AES_256_GCM for header protection.
    /// </summary>
    internal sealed class CryptoSealAesGcm : CryptoSealAesBase
    {
        internal const int IntegrityTagLength = 16;
        // AES-128 and AES-256 implementation for actual packet payload protection
        private readonly AesGcm _aesGcm;

        internal CryptoSealAesGcm(byte[] key, byte[] headerKey, TlsCipherSuite cipherSuite) : base(headerKey)
        {
            Debug.Assert(key.Length == 16 || key.Length == 32);
            Debug.Assert(headerKey.Length == 16);

            CipherSuite = cipherSuite;
            _aesGcm = new AesGcm(key);
        }

        internal override TlsCipherSuite CipherSuite { get; }
        internal override int TagLength => IntegrityTagLength;

        internal override void Protect(ReadOnlySpan<byte> nonce, Span<byte> buffer, Span<byte> tag,
            ReadOnlySpan<byte> aad)
        {
            _aesGcm.Encrypt(nonce, buffer, buffer, tag, aad);
        }

        internal override bool Unprotect(ReadOnlySpan<byte> nonce, Span<byte> buffer, ReadOnlySpan<byte> tag, ReadOnlySpan<byte> aad)
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
