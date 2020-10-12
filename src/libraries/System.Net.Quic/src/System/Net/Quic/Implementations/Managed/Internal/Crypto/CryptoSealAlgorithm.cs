// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Security;

namespace System.Net.Quic.Implementations.Managed.Internal.Crypto
{
    internal abstract class CryptoSealAlgorithm
    {
        internal static CryptoSealAlgorithm Create(TlsCipherSuite alg, byte[] key, byte[] headerKey)
        {
            if (Environment.GetEnvironmentVariable("DOTNETQUIC_NOENCRYPT") != null)
            {
                return new NullCryptoSealAlgorithm(key);
            }

            switch (alg)
            {
                case TlsCipherSuite.TLS_AES_128_GCM_SHA256:
                case TlsCipherSuite.TLS_AES_256_GCM_SHA384:
                    return new CryptoSealAesGcm(key, headerKey, alg);
                case TlsCipherSuite.TLS_AES_128_CCM_SHA256:
                    return new CryptoSealAesCcm(key, headerKey);
                case TlsCipherSuite.TLS_CHACHA20_POLY1305_SHA256:
                    // TODO-RZ: Add CHACHA20_POLY1305 support
                    throw new NotSupportedException("ChaCha20_Poly1305 is not implemented in .NET");
                default:
                    throw new ArgumentOutOfRangeException(nameof(alg), alg, null);
            }
        }

        internal abstract TlsCipherSuite CipherSuite { get; }
        internal abstract int TagLength { get; }
        internal abstract int SampleLength { get; }

        internal abstract void Protect(ReadOnlySpan<byte> nonce, Span<byte> buffer, Span<byte> tag, ReadOnlySpan<byte> aad);

        internal abstract bool Unprotect(ReadOnlySpan<byte> nonce, Span<byte> buffer, ReadOnlySpan<byte> tag,
            ReadOnlySpan<byte> aad);

        internal abstract void CreateHeaderProtectionMask(ReadOnlySpan<byte> payloadSample, Span<byte> mask);
    }
}
