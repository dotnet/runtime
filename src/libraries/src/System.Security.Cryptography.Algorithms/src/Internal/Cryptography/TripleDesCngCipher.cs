﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Security.Cryptography;
using Internal.NativeCrypto;

namespace Internal.Cryptography
{
    internal sealed class TripleDesCngCipher : BasicSymmetricCipher
    {
        private static readonly SafeAlgorithmHandle s_hAlgCbc = Open3DesAlgorithm(Cng.BCRYPT_CHAIN_MODE_CBC);
        private static readonly SafeAlgorithmHandle s_hAlgEcb = Open3DesAlgorithm(Cng.BCRYPT_CHAIN_MODE_ECB);

        private readonly bool _encrypting;
        private SafeKeyHandle _hKey;
        private byte[] _currentIv;  // CNG mutates this with the updated IV for the next stage on each Encrypt/Decrypt call.
                                    // The base IV holds a copy of the original IV for Reset(), until it is cleared by Dispose().

        public TripleDesCngCipher(CipherMode cipherMode, int blockSizeInBytes, byte[] key, byte[] iv, bool encrypting)
            : base(cipherMode.GetCipherIv(iv), blockSizeInBytes)
        {
            _encrypting = encrypting;

            if (IV != null)
            {
                _currentIv = new byte[IV.Length];
            }

            SafeAlgorithmHandle hAlg = GetCipherAlgorithm(cipherMode);
            _hKey = hAlg.BCryptImportKey(key);
            Reset();
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

                byte[] currentIv = _currentIv;
                _currentIv = null;
                if (currentIv != null)
                {
                    Array.Clear(currentIv, 0, currentIv.Length);
                }
            }

            base.Dispose(disposing);
        }

        public override int Transform(byte[] input, int inputOffset, int count, byte[] output, int outputOffset)
        {
            Debug.Assert(input != null);
            Debug.Assert(inputOffset >= 0);
            Debug.Assert(count > 0);
            Debug.Assert((count % BlockSizeInBytes) == 0);
            Debug.Assert(input.Length - inputOffset >= count);
            Debug.Assert(output != null);
            Debug.Assert(outputOffset >= 0);
            Debug.Assert(output.Length - outputOffset >= count);

            int numBytesWritten;
            if (_encrypting)
            {
                numBytesWritten = _hKey.BCryptEncrypt(input, inputOffset, count, _currentIv, output, outputOffset, output.Length - outputOffset);
            }
            else
            {
                numBytesWritten = _hKey.BCryptDecrypt(input, inputOffset, count, _currentIv, output, outputOffset, output.Length - outputOffset);
            }

            if (numBytesWritten != count)
            {
                // CNG gives us no way to tell BCryptDecrypt() that we're decrypting the final block, nor is it performing any
                // padding /depadding for us. So there's no excuse for a provider to hold back output for "future calls." Though
                // this isn't technically our problem to detect, we might as well detect it now for easier diagnosis.
                throw new CryptographicException(SR.Cryptography_UnexpectedTransformTruncation);
            }

            return numBytesWritten;
        }

        public override byte[] TransformFinal(byte[] input, int inputOffset, int count)
        {
            Debug.Assert(input != null);
            Debug.Assert(inputOffset >= 0);
            Debug.Assert(count >= 0);
            Debug.Assert((count % BlockSizeInBytes) == 0);
            Debug.Assert(input.Length - inputOffset >= count);

            byte[] output = new byte[count];
            if (count != 0)
            {
                int numBytesWritten = Transform(input, inputOffset, count, output, 0);
                Debug.Assert(numBytesWritten == count);  // Our implementation of Transform() guarantees this. See comment above.
            }

            Reset();
            return output;
        }

        private void Reset()
        {
            if (IV != null)
            {
                Buffer.BlockCopy(IV, 0, _currentIv, 0, IV.Length);
            }
        }

        private static SafeAlgorithmHandle GetCipherAlgorithm(CipherMode cipherMode)
        {
            // Windows 8 added support to set the CipherMode value on a key,
            // but Windows 7 requires that it be set on the algorithm before key creation.
            switch (cipherMode)
            {
                case CipherMode.CBC:
                    return s_hAlgCbc;
                case CipherMode.ECB:
                    return s_hAlgEcb;
                default:
                    // This is what AesCngCryptoTransform::GetCipherAlgorithm throws when it doesn't understand the value.
                    throw new NotSupportedException();
            }
        }

        private static SafeAlgorithmHandle Open3DesAlgorithm(string cipherMode)
        {
            SafeAlgorithmHandle hAlg = Cng.BCryptOpenAlgorithmProvider(Cng.BCRYPT_3DES_ALGORITHM, null, Cng.OpenAlgorithmProviderFlags.NONE);
            hAlg.SetCipherMode(cipherMode);

            return hAlg;
        }
    }
}
