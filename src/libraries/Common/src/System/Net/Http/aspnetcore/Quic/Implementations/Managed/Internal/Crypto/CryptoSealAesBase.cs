using System.Diagnostics;
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

        // ICryptoTransform is not span-based, so we need intermediate copies in order to avoid allocation.
        private byte[] _sampleArray = new byte[16];
        private byte[] _maskArray = new byte[16];

        protected CryptoSealAesBase(byte[] headerKey)
        {
            _aesEcb = new AesManaged {KeySize = headerKey.Length * 8, Mode = CipherMode.ECB, Key = headerKey}
                .CreateEncryptor();
        }

        internal override void CreateProtectionMask(ReadOnlySpan<byte> payloadSample, Span<byte> mask)
        {
            Debug.Assert(payloadSample.Length == SampleLength);
            Debug.Assert(mask.Length == 5);

            // TODO-RZ: we could use span-based implementation of AES-ECB
            payloadSample.CopyTo(_sampleArray);
            _aesEcb.TransformBlock(_sampleArray, 0, SampleLength, _maskArray, 0);
            _maskArray.AsSpan(0, 5).CopyTo(mask);
        }
    }
}
