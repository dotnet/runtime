// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Net.Security;
using System.Security.Cryptography;

namespace System.Net.Quic.Implementations.Managed.Internal.Crypto
{
    /// <summary>
    ///     Null implementation of cryptographic seal. Although this implementation does not encrypt the data, it
    ///     produces a tag which can still be used for integrity check.
    /// </summary>
    internal sealed class NullCryptoSealAlgorithm : CryptoSealAlgorithm
    {
        private readonly HMAC _hmac;

        public NullCryptoSealAlgorithm(byte[] key)
        {
// method uses a broken cryptographic algorithm HMACMD5
// this class is not really intended to provide super security, so we are fine ignoring the warning
#pragma warning disable CA5351
            _hmac = new HMACMD5(key);
#pragma warning restore CA5351
            _hmac.Initialize();

            Debug.Assert(_hmac.HashSize / 8 == TagLength);
        }

        internal override TlsCipherSuite CipherSuite => (TlsCipherSuite)(short.MaxValue);
        internal override int TagLength => 16;
        internal override int SampleLength => 16;
        internal override void Protect(ReadOnlySpan<byte> nonce, Span<byte> buffer, Span<byte> tag, ReadOnlySpan<byte> aad)
        {
            tag.Clear();
            AddDigest(nonce, tag);
            AddDigest(buffer, tag);
            AddDigest(aad, tag);
        }

        private void AddDigest(ReadOnlySpan<byte> source, Span<byte> destination)
        {
            Span<byte> digest = stackalloc byte[TagLength];
            if (!_hmac.TryComputeHash(source, digest, out int written))
            {
                Debug.Fail("Failed to compute hash");
            }
            Debug.Assert(written == TagLength);

            for (int i = 0; i < digest.Length; i++)
            {
                destination[i] ^= digest[i];
            }
        }

        internal override bool Unprotect(ReadOnlySpan<byte> nonce, Span<byte> buffer, ReadOnlySpan<byte> tag, ReadOnlySpan<byte> aad)
        {
            Span<byte> expectedTag = stackalloc byte[tag.Length];
            expectedTag.Clear();

            AddDigest(nonce, expectedTag);
            AddDigest(buffer, expectedTag);
            AddDigest(aad, expectedTag);

            return expectedTag.SequenceEqual(tag);
        }

        internal override void CreateHeaderProtectionMask(ReadOnlySpan<byte> payloadSample, Span<byte> mask)
        {
            // don't use any kind of header protection
            mask.Clear();
        }
    }
}
