// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Internal.NativeCrypto;
using System;
using System.Diagnostics;
using System.Security.Cryptography;
using static Internal.NativeCrypto.CapiHelper;

namespace Internal.Cryptography
{
    internal sealed class BasicSymmetricCipherCsp : BasicSymmetricCipher
    {
        private readonly bool _encrypting;
        private SafeProvHandle _hProvider;
        private SafeKeyHandle _hKey;

        public BasicSymmetricCipherCsp(int algId, CipherMode cipherMode, int blockSizeInBytes, byte[] key, int effectiveKeyLength, bool addNoSaltFlag, byte[] iv, bool encrypting)
            : base(cipherMode.GetCipherIv(iv), blockSizeInBytes)
        {
            _encrypting = encrypting;

            _hProvider = AcquireSafeProviderHandle();
            _hKey = ImportCspBlob(_hProvider, algId, key, addNoSaltFlag);

            SetKeyParameter(_hKey, CryptGetKeyParamQueryType.KP_MODE, (int)cipherMode);

            byte[] currentIv = cipherMode.GetCipherIv(iv);
            if (currentIv != null)
            {
                SetKeyParameter(_hKey, CryptGetKeyParamQueryType.KP_IV, currentIv);
            }

            if (effectiveKeyLength != 0)
            {
                SetKeyParameter(_hKey, CryptGetKeyParamQueryType.KP_EFFECTIVE_KEYLEN, effectiveKeyLength);
            }
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

                SafeProvHandle hProvider = _hProvider;
                _hProvider = null;
                if (hProvider != null)
                {
                    hProvider.Dispose();
                }
            }

            base.Dispose(disposing);
        }

        public override int Transform(byte[] input, int inputOffset, int count, byte[] output, int outputOffset)
        {
            return Transform(input, inputOffset, count, output, outputOffset, false);
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
                int numBytesWritten = Transform(input, inputOffset, count, output, 0, true);
                Debug.Assert(numBytesWritten == count);  // Our implementation of Transform() guarantees this.
            }

            Reset();

            return output;
        }

        private void Reset()
        {
            // Ensure we've called CryptEncrypt with the final=true flag so the handle is reset property
            EncryptData(_hKey, Array.Empty<Byte>(), 0, 0, Array.Empty<Byte>(), 0, 0, true);
        }

        private int Transform(byte[] input, int inputOffset, int count, byte[] output, int outputOffset, bool isFinal)
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
                numBytesWritten = EncryptData(_hKey, input, inputOffset, count, output, outputOffset, output.Length - outputOffset, isFinal);
            }
            else
            {
                numBytesWritten = DecryptData(_hKey, input, inputOffset, count, output, outputOffset, output.Length - outputOffset);
            }

            return numBytesWritten;
        }

        private static SafeKeyHandle ImportCspBlob(SafeProvHandle safeProvHandle, int algId, byte[] rawKey, bool addNoSaltFlag)
        {
            SafeKeyHandle safeKeyHandle;
            byte[] keyBlob = ToPlainTextKeyBlob(algId, rawKey);
            ImportKeyBlob(safeProvHandle, (CspProviderFlags)0, addNoSaltFlag, keyBlob, out safeKeyHandle);
            // Note if plain text import fails, desktop falls back to "ExponentOfOneImport" which is not handled here
            return safeKeyHandle;
        }

        private static SafeProvHandle AcquireSafeProviderHandle()
        {
            SafeProvHandle safeProvHandle = null;
            var cspParams = new CspParameters((int)ProviderType.PROV_RSA_FULL);
            CapiHelper.AcquireCsp(cspParams, out safeProvHandle);
            return safeProvHandle;
        }
    }
}
