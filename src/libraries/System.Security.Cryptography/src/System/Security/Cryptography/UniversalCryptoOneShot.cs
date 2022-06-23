// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Security.Cryptography;
using Internal.Cryptography;

namespace System.Security.Cryptography
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
            // The second condition is where the output length is short by more than a whole block.
            // All valid padding is at most one complete block. If the difference between the
            // output and the input is more than a whole block then we know the output is too small.
            if (!SymmetricPadding.DepaddingRequired(paddingMode) ||
                input.Length - cipher.BlockSizeInBytes > output.Length)
            {
                bytesWritten = 0;
                return false;
            }

            // At this point the destination might be big enough but we don't know until we've
            // transformed the last block, and input is within one block of being the right
            // size.
            // For sufficiently small ciphertexts, do them on the stack.
            // This buffer needs to be at least twice as big as the largest block size
            // we support, which is 16 bytes for AES.
            const int MaxInStackDecryptionBuffer = 128;
            Span<byte> stackBuffer = stackalloc byte[MaxInStackDecryptionBuffer];

            if (input.Length <= MaxInStackDecryptionBuffer)
            {
                int stackTransformFinal = cipher.TransformFinal(input, stackBuffer);
                int depaddedLength = SymmetricPadding.GetPaddingLength(
                    stackBuffer.Slice(0, stackTransformFinal),
                    paddingMode,
                    cipher.BlockSizeInBytes);
                Span<byte> writtenDepadded = stackBuffer.Slice(0, depaddedLength);

                if (output.Length < depaddedLength)
                {
                    CryptographicOperations.ZeroMemory(writtenDepadded);
                    bytesWritten = 0;
                    return false;
                }

                writtenDepadded.CopyTo(output);
                CryptographicOperations.ZeroMemory(writtenDepadded);
                bytesWritten = depaddedLength;
                return true;
            }

            // If the source and destination do not overlap, we can decrypt directly in to the user buffer.
            if (!input.Overlaps(output, out int overlap) || overlap == 0)
            {
                // At this point we know that we have multiple blocks that need to be decrypted.
                // So we decrypt all but the last block directly in to the buffer. The final
                // block we decrypt in to a stack buffer, and if it fits, copy the last block to
                // the output.

                // We should only get here if we have multiple blocks to transform. The single
                // block case should have happened on the stack.
                Debug.Assert(input.Length > cipher.BlockSizeInBytes);

                int writtenToOutput = 0;
                int finalTransformWritten = 0;

                // CFB8 means this may not be an exact multiple of the block size.
                // If the an AES CFB8 ciphertext length is 129 with PKCS7 padding, then
                // we'll have 113 bytes in the unpaddedBlocks and 16 in the paddedBlock.
                // We still need to do this on block size, not padding size. The CFB8
                // padding byte might be block size. We don't want unpaddedBlocks to
                // contain removable padding, so split on block size.
                ReadOnlySpan<byte> unpaddedBlocks = input[..^cipher.BlockSizeInBytes];
                ReadOnlySpan<byte> paddedBlock = input[^cipher.BlockSizeInBytes..];
                Debug.Assert(paddedBlock.Length % cipher.BlockSizeInBytes == 0);
                Debug.Assert(paddedBlock.Length <= MaxInStackDecryptionBuffer);

                try
                {
                    writtenToOutput = cipher.Transform(unpaddedBlocks, output);
                    finalTransformWritten = cipher.TransformFinal(paddedBlock, stackBuffer);

                    // This will throw on invalid padding.
                    int depaddedLength = SymmetricPadding.GetPaddingLength(
                        stackBuffer.Slice(0, finalTransformWritten),
                        paddingMode,
                        cipher.BlockSizeInBytes);
                    Span<byte> depaddedFinalTransform = stackBuffer.Slice(0, depaddedLength);

                    if (output.Length - writtenToOutput < depaddedLength)
                    {
                        CryptographicOperations.ZeroMemory(depaddedFinalTransform);
                        CryptographicOperations.ZeroMemory(output.Slice(0, writtenToOutput));
                        bytesWritten = 0;
                        return false;
                    }

                    depaddedFinalTransform.CopyTo(output.Slice(writtenToOutput));
                    CryptographicOperations.ZeroMemory(depaddedFinalTransform);
                    bytesWritten = writtenToOutput + depaddedLength;
                    return true;
                }
                catch (CryptographicException)
                {
                    CryptographicOperations.ZeroMemory(output.Slice(0, writtenToOutput));
                    CryptographicOperations.ZeroMemory(stackBuffer.Slice(0, finalTransformWritten));
                    throw;
                }
            }

            // If we get here, then we have multiple blocks with overlapping buffers that don't fit in the stack.
            // We need to rent and copy for this.
            byte[] rentedBuffer = CryptoPool.Rent(input.Length);
            Span<byte> buffer = rentedBuffer.AsSpan(0, input.Length);
            Span<byte> decryptedBuffer = default;

            // Keep our buffer fixed so the GC doesn't move it around before we clear and return to the pool.
            fixed (byte* pBuffer = buffer)
            {
                try
                {
                    int transformWritten = cipher.TransformFinal(input, buffer);
                    decryptedBuffer = buffer.Slice(0, transformWritten);

                    // This intentionally passes in BlockSizeInBytes instead of PaddingSizeInBytes. This is so that
                    // "extra padded" CFB data can still be decrypted. The .NET Framework always padded CFB8 to the
                    // block size, not the feedback size. We want the one-shot to be able to continue to decrypt
                    // those ciphertexts, so for CFB8 we are more lenient on the number of allowed padding bytes.
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
