// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using Internal.NativeCrypto;

namespace System.Security.Cryptography
{
    internal sealed class BasicSymmetricCipherLiteBCrypt : ILiteSymmetricCipher
    {
        private readonly bool _encrypting;
        private SafeKeyHandle _hKey;
        private byte[]? _currentIv;

        public int BlockSizeInBytes { get; }
        public int PaddingSizeInBytes { get; }

        public BasicSymmetricCipherLiteBCrypt(
            SafeAlgorithmHandle algorithm,
            CipherMode cipherMode,
            int blockSizeInBytes,
            int paddingSizeInBytes,
            ReadOnlySpan<byte> key,
            bool ownsParentHandle,
            ReadOnlySpan<byte> iv,
            bool encrypting)
        {
            if (!iv.IsEmpty)
            {
                // Must copy the input IV
                _currentIv = iv.ToArray();
            }

            BlockSizeInBytes = blockSizeInBytes;
            PaddingSizeInBytes = paddingSizeInBytes;
            _encrypting = encrypting;
            _hKey = Interop.BCrypt.BCryptImportKey(algorithm, key);

            if (ownsParentHandle)
            {
                _hKey.SetParentHandle(algorithm);
            }
        }

        public int Transform(ReadOnlySpan<byte> input, Span<byte> output)
        {
            Debug.Assert(input.Length > 0);
            Debug.Assert((input.Length % PaddingSizeInBytes) == 0);

            int numBytesWritten = 0;

            // BCryptEncrypt and BCryptDecrypt can do in place encryption, but if the buffers overlap
            // the offset must be zero. In that case, we need to copy to a temporary location.
            if (input.Overlaps(output, out int offset) && offset != 0)
            {
                byte[] rented = CryptoPool.Rent(output.Length);

                try
                {
                    numBytesWritten = BCryptTransform(input, rented);
                    rented.AsSpan(0, numBytesWritten).CopyTo(output);
                }
                finally
                {
                    CryptoPool.Return(rented, clearSize: numBytesWritten);
                }
            }
            else
            {
                numBytesWritten = BCryptTransform(input, output);
            }

            if (numBytesWritten != input.Length)
            {
                // CNG gives us no way to tell BCryptDecrypt() that we're decrypting the final block, nor is it performing any
                // padding /depadding for us. So there's no excuse for a provider to hold back output for "future calls." Though
                // this isn't technically our problem to detect, we might as well detect it now for easier diagnosis.
                throw new CryptographicException(SR.Cryptography_UnexpectedTransformTruncation);
            }

            return numBytesWritten;

            int BCryptTransform(ReadOnlySpan<byte> input, Span<byte> output)
            {
                return _encrypting ?
                    Interop.BCrypt.BCryptEncrypt(_hKey, input, _currentIv, output) :
                    Interop.BCrypt.BCryptDecrypt(_hKey, input, _currentIv, output);
            }
        }

        public void Reset(ReadOnlySpan<byte> iv)
        {
            if (_currentIv is not null)
            {
                iv.CopyTo(_currentIv);
            }
        }

        public int TransformFinal(ReadOnlySpan<byte> input, Span<byte> output)
        {
            Debug.Assert((input.Length % PaddingSizeInBytes) == 0);

            int numBytesWritten = 0;

            if (input.Length != 0)
            {
                numBytesWritten = Transform(input, output);
                Debug.Assert(numBytesWritten == input.Length); // Our implementation of Transform() guarantees this. See comment above.
            }

            return numBytesWritten;
        }

        public void Dispose()
        {
            if (_currentIv is not null)
            {
                CryptographicOperations.ZeroMemory(_currentIv);
            }

            _hKey?.Dispose();
            _hKey = null!;
        }

    }
}
