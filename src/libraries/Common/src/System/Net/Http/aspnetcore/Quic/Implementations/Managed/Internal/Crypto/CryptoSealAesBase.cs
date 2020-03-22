using System.Security.Cryptography;

namespace System.Net.Quic.Implementations.Managed.Internal.Crypto
{
    /// <summary>
    ///     Base class for AES-based protection seals
    /// </summary>
    internal abstract class CryptoSealAesBase : CryptoSealAlgorithm
    {
        // All AES-based seals use AES in ECB mode for header protection mask computation, only with differing key size
        // (128 or 256 bits)
        private readonly ICryptoTransform _aesEcb;

        internal override int SampleLength => 16;

        protected CryptoSealAesBase(byte[] headerKey)
        {
            _aesEcb = new AesManaged {KeySize = headerKey.Length * 8, Mode = CipherMode.ECB, Key = headerKey}
                .CreateEncryptor();
        }

        internal override void CreateProtectionMask(ReadOnlySpan<byte> payloadSample, Span<byte> mask)
        {
            // TODO-RZ: use AES-ECB implementation with allocation-less interface
            var arr = _aesEcb.TransformFinalBlock(payloadSample.ToArray(), 0, payloadSample.Length);
            arr.AsSpan(0, 5).CopyTo(mask);
        }
    }
}