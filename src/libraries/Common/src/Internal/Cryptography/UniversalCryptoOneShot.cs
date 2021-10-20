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

            // If there is no padding that needs to be removed, and the output buffer is large enough to hold
            // the resulting plaintext, we can decrypt directly in to the output buffer.
            // We do not do this for modes that require padding removal.
            //
            // This is not done for padded ciphertexts because we don't know if the padding is valid
            // until it's been decrypted. We don't want to decrypt in to a user-supplied buffer and then throw
            // a padding exception after we've already filled the user buffer with plaintext. We should only
            // release the plaintext to the caller once we know the padding is valid.
            if (!SymmetricPadding.DepaddingRequired(paddingMode))
            {
                if (output.Length >= input.Length)
                {
                    bytesWritten = cipher.TransformFinal(input, output);
                    return true;
                }

                // If no padding is going to be removed, we know the buffer is too small and we can bail out.
                bytesWritten = 0;
                return false;
            }

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
