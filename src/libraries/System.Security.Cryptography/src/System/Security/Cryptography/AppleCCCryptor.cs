// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Internal.Cryptography;

namespace System.Security.Cryptography
{
    internal sealed class AppleCCCryptor : BasicSymmetricCipher
    {
        private readonly bool _encrypting;
        private AppleCCCryptorLite _cryptor;

        // Reset operation is not supported on stream cipher
        private readonly bool _supportsReset;

        private Interop.AppleCrypto.PAL_SymmetricAlgorithm _algorithm;
        private CipherMode _cipherMode;
        private byte[] _key;
        private int _feedbackSizeInBytes;

        public AppleCCCryptor(
            Interop.AppleCrypto.PAL_SymmetricAlgorithm algorithm,
            CipherMode cipherMode,
            int blockSizeInBytes,
            byte[] key,
            byte[]? iv,
            bool encrypting,
            int feedbackSizeInBytes,
            int paddingSizeInBytes)
            : base(cipherMode.GetCipherIv(iv), blockSizeInBytes, paddingSizeInBytes)
        {
            _encrypting = encrypting;

            // CFB is streaming cipher, calling CCCryptorReset is not implemented (and is effectively noop)
            _supportsReset = cipherMode != CipherMode.CFB;

            _algorithm = algorithm;
            _cipherMode = cipherMode;
            _key = key;
            _feedbackSizeInBytes = feedbackSizeInBytes;

            OpenCryptor();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _cryptor?.Dispose();
                _cryptor = null!;
            }

            base.Dispose(disposing);
        }

        public override int Transform(ReadOnlySpan<byte> input, Span<byte> output)
        {
            Debug.Assert(input.Length > 0);
            Debug.Assert((input.Length % PaddingSizeInBytes) == 0);

            return _cryptor.Transform(input, output);
        }

        public override int TransformFinal(ReadOnlySpan<byte> input, Span<byte> output)
        {
            int written = _cryptor.TransformFinal(input, output);
            Reset();
            return written;
        }

        [MemberNotNull(nameof(_cryptor))]
        private unsafe void OpenCryptor()
        {
            _cryptor = new AppleCCCryptorLite(
                _algorithm,
                _cipherMode,
                BlockSizeInBytes,
                _key,
                IV,
                _encrypting,
                _feedbackSizeInBytes,
                PaddingSizeInBytes);
        }

        private unsafe void Reset()
        {
            if (!_supportsReset)
            {
                // when CryptorReset is not supported,
                // dispose & reopen
                _cryptor?.Dispose();
                OpenCryptor();
            }
            else
            {
                _cryptor.Reset(IV);
            }
        }
    }
}
