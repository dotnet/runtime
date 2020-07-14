// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Security.Cryptography;
using Internal.NativeCrypto;
using Microsoft.Win32.SafeHandles;
using static Internal.NativeCrypto.CapiHelper;

namespace Internal.Cryptography
{
    internal sealed class BasicSymmetricCipherCsp : BasicSymmetricCipher
    {
        private readonly bool _encrypting;
        private SafeProvHandle _hProvider;
        private SafeKeyHandle _hKey;

        public BasicSymmetricCipherCsp(int algId, CipherMode cipherMode, int blockSizeInBytes, byte[] key, int effectiveKeyLength, bool addNoSaltFlag, byte[]? iv, bool encrypting)
            : base(cipherMode.GetCipherIv(iv), blockSizeInBytes)
        {
            _encrypting = encrypting;

            _hProvider = AcquireSafeProviderHandle();
            _hKey = ImportCspBlob(_hProvider, algId, key, addNoSaltFlag);

            SetKeyParameter(_hKey, CryptGetKeyParamQueryType.KP_MODE, (int)cipherMode);

            byte[]? currentIv = cipherMode.GetCipherIv(iv);
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
                _hKey = null!;
                if (hKey != null)
                {
                    hKey.Dispose();
                }

                SafeProvHandle hProvider = _hProvider;
                _hProvider = null!;
                if (hProvider != null)
                {
                    hProvider.Dispose();
                }
            }

            base.Dispose(disposing);
        }

        public override int Transform(ReadOnlySpan<byte> input, Span<byte> output)
        {
            return Transform(input, output, false);
        }

        public override int TransformFinal(ReadOnlySpan<byte> input, Span<byte> output)
        {
            Debug.Assert((input.Length % BlockSizeInBytes) == 0);

            int numBytesWritten = 0;

            if (input.Length != 0)
            {
                numBytesWritten = Transform(input, output, true);
                Debug.Assert(numBytesWritten == input.Length);  // Our implementation of Transform() guarantees this.
            }

            Reset();

            return numBytesWritten;
        }

        private void Reset()
        {
            // Ensure we've called CryptEncrypt with the final=true flag so the handle is reset property
            EncryptData(_hKey, default, default, true);
        }

        private int Transform(ReadOnlySpan<byte> input, Span<byte> output, bool isFinal)
        {
            Debug.Assert(input.Length > 0);
            Debug.Assert((input.Length % BlockSizeInBytes) == 0);

            int numBytesWritten;
            if (_encrypting)
            {
                numBytesWritten = EncryptData(_hKey, input, output, isFinal);
            }
            else
            {
                numBytesWritten = DecryptData(_hKey, input, output);
            }

            return numBytesWritten;
        }

        private static SafeKeyHandle ImportCspBlob(SafeProvHandle safeProvHandle, int algId, byte[] rawKey, bool addNoSaltFlag)
        {
            SafeKeyHandle safeKeyHandle;
            byte[] keyBlob = ToPlainTextKeyBlob(algId, rawKey);
            ImportKeyBlob(safeProvHandle, (CspProviderFlags)0, addNoSaltFlag, keyBlob, out safeKeyHandle);
            // Note if plain text import fails, .NET Framework falls back to "ExponentOfOneImport" which is not handled here
            return safeKeyHandle;
        }

        private static SafeProvHandle AcquireSafeProviderHandle()
        {
            SafeProvHandle safeProvHandle;
            var cspParams = new CspParameters((int)ProviderType.PROV_RSA_FULL);
            CapiHelper.AcquireCsp(cspParams, out safeProvHandle);
            return safeProvHandle;
        }
    }
}
