namespace System.Net.Quic.Implementations.Managed.Internal.Crypto
{
    internal abstract class CryptoSealAlgorithm
    {
        internal static CryptoSealAlgorithm Create(CipherAlgorithm alg, byte[] key, byte[] headerKey)
        {
            switch (alg)
            {
                case CipherAlgorithm.AEAD_AES_128_GCM:
                case CipherAlgorithm.AEAD_AES_256_GCM:
                    return new CryptoSealAesGcm(key, headerKey);
                case CipherAlgorithm.AEAD_AES_128_CCM:
                    return new CryptoSealAesGcm(key, headerKey);
                case CipherAlgorithm.AEAD_CHACHA20_POLY1305:
                    // TODO-RZ: Add CHACHA20_POLY1305 support
                    throw new NotSupportedException("ChaCha20_Poly1305 is not implemented in .NET");
                default:
                    throw new ArgumentOutOfRangeException(nameof(alg), alg, null);
            }
        }

        internal abstract int TagLength { get; }
        internal abstract int SampleLength { get; }

        internal abstract void Encrypt(ReadOnlySpan<byte> nonce, Span<byte> buffer, Span<byte> tag, ReadOnlySpan<byte> aad);

        internal abstract bool Decrypt(ReadOnlySpan<byte> nonce, Span<byte> buffer, ReadOnlySpan<byte> tag,
            ReadOnlySpan<byte> aad);

        internal abstract void CreateProtectionMask(ReadOnlySpan<byte> payloadSample, Span<byte> mask);
    }
}
