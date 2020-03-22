namespace System.Net.Quic.Implementations.Managed.Internal.Crypto
{
    internal enum CipherAlgorithm
    {
        AEAD_AES_128_GCM,
        AEAD_AES_128_CCM,
        AEAD_AES_256_GCM,
        AEAD_CHACHA20_POLY1305,
    }
}
