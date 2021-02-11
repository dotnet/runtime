// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Security.Cryptography;
using Internal.NativeCrypto;

namespace Internal.Cryptography
{
    internal sealed class BasicSymmetricCipherBCrypt : BasicSymmetricCipher
    {
        private readonly bool _encrypting;
        private SafeKeyHandle _hKey;
        private byte[]? _currentIv;  // CNG mutates this with the updated IV for the next stage on each Encrypt/Decrypt call.
                                     // The base IV holds a copy of the original IV for Reset(), until it is cleared by Dispose().

        public BasicSymmetricCipherBCrypt(SafeAlgorithmHandle algorithm, CipherMode cipherMode, int blockSizeInBytes, int paddingSizeInBytes, byte[] key, bool ownsParentHandle, byte[]? iv, bool encrypting)
            : base(cipherMode.GetCipherIv(iv), blockSizeInBytes, paddingSizeInBytes)
        {
            Debug.Assert(algorithm != null);

            _encrypting = encrypting;

            if (IV != null)
            {
                _currentIv = new byte[IV.Length];
            }

            _hKey = Interop.BCrypt.BCryptImportKey(algorithm, key);

            if (ownsParentHandle)
            {
                _hKey.SetParentHandle(algorithm);
            }

            Reset();
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

                byte[]? currentIv = _currentIv;
                _currentIv = null;
                if (currentIv != null)
                {
                    Array.Clear(currentIv, 0, currentIv.Length);
                }
            }

            base.Dispose(disposing);
        }

        public override int Transform(ReadOnlySpan<byte> input, Span<byte> output)
        {
            Debug.Assert(input.Length > 0);
            Debug.Assert((input.Length % PaddingSizeInBytes) == 0);

            int numBytesWritten;

            if (_encrypting)
            {
                numBytesWritten = Interop.BCrypt.BCryptEncrypt(_hKey, input, _currentIv, output);
            }
            else
            {
                numBytesWritten = Interop.BCrypt.BCryptDecrypt(_hKey, input, _currentIv, output);
            }

            if (numBytesWritten != input.Length)
            {
                // CNG gives us no way to tell BCryptDecrypt() that we're decrypting the final block, nor is it performing any
                // padding /depadding for us. So there's no excuse for a provider to hold back output for "future calls." Though
                // this isn't technically our problem to detect, we might as well detect it now for easier diagnosis.
                throw new CryptographicException(SR.Cryptography_UnexpectedTransformTruncation);
            }

            return numBytesWritten;
        }

        public override int TransformFinal(ReadOnlySpan<byte> input, Span<byte> output)
        {
            Debug.Assert((input.Length % PaddingSizeInBytes) == 0);

            int numBytesWritten = 0;

            if (input.Length != 0)
            {
                numBytesWritten = Transform(input, output);
                Debug.Assert(numBytesWritten == input.Length);  // Our implementation of Transform() guarantees this. See comment above.
            }

            Reset();
            return numBytesWritten;
        }

        private void Reset()
        {
            if (IV != null)
            {
                Buffer.BlockCopy(IV, 0, _currentIv!, 0, IV.Length);
            }
        }
    }
}
