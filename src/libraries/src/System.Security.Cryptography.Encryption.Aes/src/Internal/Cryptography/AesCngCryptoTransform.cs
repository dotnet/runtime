// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Security.Cryptography;

using Internal.NativeCrypto;

namespace Internal.Cryptography
{
    internal abstract class AesCngCryptoTransform : AesNativeCryptoTransform
    {
        protected AesCngCryptoTransform(CipherMode cipherMode, PaddingMode paddingMode, byte[] key, byte[] iv, int blockSize)
            : base(cipherMode, paddingMode, blockSize)
        {
            byte[] cipherIv = GetCipherIv(iv);

            if (cipherIv != null)
            {
                _iv = cipherIv.CloneByteArray();
                _currentIv = new byte[cipherIv.Length];
            }
            _hKey = s_hAlg.BCryptImportKey(key);
            _hKey.SetCipherMode(cipherMode);
            Reset();
        }

        protected sealed override int UncheckedTransformBlock(byte[] inputBuffer, int inputOffset, int inputCount, byte[] outputBuffer, int outputOffset)
        {
            return UncheckedTransformBlock(_hKey, _currentIv, inputBuffer, inputOffset, inputCount, outputBuffer, outputOffset);
        }

        protected sealed override byte[] UncheckedTransformFinalBlock(byte[] inputBuffer, int inputOffset, int inputCount)
        {
            return UncheckedTransformFinalBlock(_hKey, _currentIv, inputBuffer, inputOffset, inputCount);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                SafeKeyHandle hKey = _hKey;
                _hKey = null;
                if (hKey != null)
                {
                    hKey.Dispose();
                }

                byte[] iv = _iv;
                _iv = null;
                if (iv != null)
                {
                    Array.Clear(iv, 0, iv.Length);
                }

                byte[] currentIv = _currentIv;
                _currentIv = null;
                if (currentIv != null)
                {
                    Array.Clear(currentIv, 0, currentIv.Length);
                }
            }

            base.Dispose(disposing);
        }

        protected override void Reset()
        {
            if (_iv != null)
            {
                Buffer.BlockCopy(_iv, 0, _currentIv, 0, _iv.Length);
            }
        }

        protected abstract int UncheckedTransformBlock(SafeKeyHandle hKey, byte[] currentIv, byte[] inputBuffer, int inputOffset, int inputCount, byte[] outputBuffer, int outputOffset);

        protected abstract byte[] UncheckedTransformFinalBlock(SafeKeyHandle hKey, byte[] currentIv, byte[] inputBuffer, int inputOffset, int inputCount);

        private SafeKeyHandle _hKey;
        private byte[] _iv;         // _iv holds a copy of the original IV for Reset(), until it is cleared by Dispose().
        private byte[] _currentIv;  // CNG mutates this with the updated IV for the next stage on each Encrypt/Decrypt call.

        private static readonly SafeAlgorithmHandle s_hAlg = Cng.BCryptOpenAlgorithmProvider(Cng.BCRYPT_AES_ALGORITHM, null, Cng.OpenAlgorithmProviderFlags.NONE);
    }
}
