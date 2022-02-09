// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Security.Cryptography;

namespace Internal.Cryptography
{
    internal static class UniversalCryptoOneShot
    {
        public static unsafe bool OneShotDecrypt(
            ILiteSymmetricCipher cipher,
            PaddingMode paddingMode,
            ReadOnlySpan<byte> input,
            Span<byte> output,
            out int bytesWritten)
        {
            if (input.Length % cipher.PaddingSizeInBytes != 0)
                throw new CryptographicException(SR.Cryptography_PartialBlock);

            // The internal implementation of the one-shots are never expected to create
            // a plaintext larger than the ciphertext. If the buffer supplied is large enough
            // to do the transform, use it directly.
            // This does mean that the TransformFinal will write more than what is reported in
            // bytesWritten when padding needs to be removed. The padding is "removed" simply
            // by reporting less of the amount written, then zeroing out the extra padding.
            if (output.Length >= input.Length)
            {
                int bytesTransformed = cipher.TransformFinal(input, output);
                Span<byte> transformBuffer = output.Slice(0, bytesTransformed);

                try
                {
                    // validates padding
                    // This intentionally passes in BlockSizeInBytes instead of PaddingSizeInBytes. This is so that
                    // "extra padded" CFB data can still be decrypted. The .NET Framework always padded CFB8 to the
                    // block size, not the feedback size. We want the one-shot to be able to continue to decrypt
                    // those ciphertexts, so for CFB8 we are more lenient on the number of allowed padding bytes.
                    bytesWritten = SymmetricPadding.GetPaddingLength(transformBuffer, paddingMode, cipher.BlockSizeInBytes);

                    // Zero out the padding so that the buffer does not contain the padding data "after" the bytesWritten.
                    CryptographicOperations.ZeroMemory(transformBuffer.Slice(bytesWritten));
                    return true;
                }
                catch (CryptographicException)
                {
                    // The padding is invalid, but don't leave the plaintext in the buffer.
                    CryptographicOperations.ZeroMemory(transformBuffer);
                    throw;
                }
            }

            // If no padding is going to removed, then we already know the buffer is too small
            // since that requires a buffer at-least the size of the ciphertext. Bail out early.
            if (!SymmetricPadding.DepaddingRequired(paddingMode))
            {
                bytesWritten = 0;
                return false;
            }

            // At this point the output is smaller than the input, but after padding removal
            // it might fit in the output. Decrypt in to a temporary buffer and copy it if
            // after padding removal it would fit.
            byte[] rentedBuffer = CryptoPool.Rent(input.Length);
            Span<byte> buffer = rentedBuffer.AsSpan(0, input.Length);
            Span<byte> decryptedBuffer = default;

            fixed (byte* pBuffer = buffer)
            {
                try
                {
                    int transformWritten = cipher.TransformFinal(input, buffer);
                    decryptedBuffer = buffer.Slice(0, transformWritten);

                    int unpaddedLength = SymmetricPadding.GetPaddingLength(decryptedBuffer, paddingMode, cipher.BlockSizeInBytes); // validates padding

                    if (unpaddedLength > output.Length)
                    {
                        bytesWritten = 0;
                        return false;
                    }

                    decryptedBuffer.Slice(0, unpaddedLength).CopyTo(output);
                    bytesWritten = unpaddedLength;
                    return true;
                }
                finally
                {
                    CryptographicOperations.ZeroMemory(decryptedBuffer);
                    CryptoPool.Return(rentedBuffer, clearSize: 0); // ZeroMemory clears the part of the buffer that was written to.
                }
            }
        }

        public static bool OneShotEncrypt(
            ILiteSymmetricCipher cipher,
            PaddingMode paddingMode,
            ReadOnlySpan<byte> input,
            Span<byte> output,
            out int bytesWritten)
        {
            int ciphertextLength = SymmetricPadding.GetCiphertextLength(input.Length, cipher.PaddingSizeInBytes, paddingMode);

            if (output.Length < ciphertextLength)
            {
                bytesWritten = 0;
                return false;
            }

            // Copy the input to the output, and apply padding if required. This will not throw since the
            // output length has already been checked, and PadBlock will not copy from input to output
            // until it has checked that it will be able to apply padding correctly.
            int padWritten = SymmetricPadding.PadBlock(input, output, cipher.PaddingSizeInBytes, paddingMode);

            // Do an in-place encrypt. All of our implementations support this, either natively
            // or making a temporary buffer themselves if in-place is not supported by the native
            // implementation.
            Span<byte> paddedOutput = output.Slice(0, padWritten);
            bytesWritten = cipher.TransformFinal(paddedOutput, paddedOutput);

            // After padding, we should have an even number of blocks, and the same applies
            // to the transform.
            Debug.Assert(padWritten == bytesWritten);
            return true;
        }
    }
}
