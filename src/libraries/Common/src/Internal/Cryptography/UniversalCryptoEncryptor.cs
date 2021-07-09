// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Security.Cryptography;

namespace Internal.Cryptography
{
    //
    // A cross-platform ICryptoTransform implementation for encryption.
    //
    //  - Implements the various padding algorithms (as we support padding algorithms that the underlying native apis don't.)
    //
    //  - Parameterized by a BasicSymmetricCipher which encapsulates the algorithm, key, IV, chaining mode, direction of encryption
    //    and the underlying native apis implementing the encryption.
    //
    internal sealed class UniversalCryptoEncryptor : UniversalCryptoTransform
    {
        public UniversalCryptoEncryptor(PaddingMode paddingMode, BasicSymmetricCipher basicSymmetricCipher)
            : base(paddingMode, basicSymmetricCipher)
        {
        }

        protected override int UncheckedTransformBlock(ReadOnlySpan<byte> inputBuffer, Span<byte> outputBuffer)
        {
            return BasicSymmetricCipher.Transform(inputBuffer, outputBuffer);
        }

        protected override int UncheckedTransformFinalBlock(ReadOnlySpan<byte> inputBuffer, Span<byte> outputBuffer)
        {
            // The only caller of this method is the array-allocating overload, outputBuffer is
            // always new memory, not a user-provided buffer.
            Debug.Assert(!inputBuffer.Overlaps(outputBuffer));

            int padWritten = PadBlock(inputBuffer, outputBuffer);
            int transformWritten = BasicSymmetricCipher.TransformFinal(outputBuffer.Slice(0, padWritten), outputBuffer);

            // After padding, we should have an even number of blocks, and the same applies
            // to the transform.
            Debug.Assert(padWritten == transformWritten);

            return transformWritten;
        }

        protected override byte[] UncheckedTransformFinalBlock(byte[] inputBuffer, int inputOffset, int inputCount)
        {
            byte[] buffer;
#if NET5_0_OR_GREATER
            buffer = GC.AllocateUninitializedArray<byte>(GetCiphertextLength(inputCount));
#else
            buffer = new byte[GetCiphertextLength(inputCount)];
#endif
            int written = UncheckedTransformFinalBlock(inputBuffer.AsSpan(inputOffset, inputCount), buffer);
            Debug.Assert(written == buffer.Length);
            return buffer;
        }

        public override bool TransformOneShot(ReadOnlySpan<byte> input, Span<byte> output, out int bytesWritten)
        {
            int ciphertextLength = GetCiphertextLength(input.Length);

            if (output.Length < ciphertextLength)
            {
                bytesWritten = 0;
                return false;
            }

            // Copy the input to the output, and apply padding if required. This will not throw since the
            // output length has already been checked, and PadBlock will not copy from input to output
            // until it has checked that it will be able to apply padding correctly.
            int padWritten = PadBlock(input, output);

            // Do an in-place encrypt. All of our implementations support this, either natively
            // or making a temporary buffer themselves if in-place is not supported by the native
            // implementation.
            Span<byte> paddedOutput = output.Slice(0, padWritten);
            bytesWritten = BasicSymmetricCipher.TransformFinal(paddedOutput, paddedOutput);

            // After padding, we should have an even number of blocks, and the same applies
            // to the transform.
            Debug.Assert(padWritten == bytesWritten);
            return true;
        }

        private int GetCiphertextLength(int plaintextLength)
        {
            Debug.Assert(plaintextLength >= 0);

             //divisor and factor are same and won't overflow.
            int wholeBlocks = Math.DivRem(plaintextLength, PaddingSizeBytes, out int remainder) * PaddingSizeBytes;

            switch (PaddingMode)
            {
                case PaddingMode.None when (remainder != 0):
                    throw new CryptographicException(SR.Cryptography_PartialBlock);
                case PaddingMode.None:
                case PaddingMode.Zeros when (remainder == 0):
                    return plaintextLength;
                case PaddingMode.Zeros:
                case PaddingMode.PKCS7:
                case PaddingMode.ANSIX923:
                case PaddingMode.ISO10126:
                    return checked(wholeBlocks + PaddingSizeBytes);
                default:
                    Debug.Fail($"Unknown padding mode {PaddingMode}.");
                    throw new CryptographicException(SR.Cryptography_UnknownPaddingMode);
            }
        }

        private int PadBlock(ReadOnlySpan<byte> block, Span<byte> destination)
        {
            int count = block.Length;
            int paddingRemainder = count % PaddingSizeBytes;
            int padBytes = PaddingSizeBytes - paddingRemainder;

            switch (PaddingMode)
            {
                case PaddingMode.None when (paddingRemainder != 0):
                    throw new CryptographicException(SR.Cryptography_PartialBlock);

                case PaddingMode.None:
                    if (destination.Length < count)
                    {
                        throw new ArgumentException(SR.Argument_DestinationTooShort, nameof(destination));
                    }

                    block.CopyTo(destination);
                    return count;

                // ANSI padding fills the blocks with zeros and adds the total number of padding bytes as
                // the last pad byte, adding an extra block if the last block is complete.
                //
                // xx 00 00 00 00 00 00 07
                case PaddingMode.ANSIX923:
                    int ansiSize = count + padBytes;

                    if (destination.Length < ansiSize)
                    {
                        throw new ArgumentException(SR.Argument_DestinationTooShort, nameof(destination));
                    }

                    block.CopyTo(destination);
                    destination.Slice(count, padBytes - 1).Clear();
                    destination[count + padBytes - 1] = (byte)padBytes;
                    return ansiSize;

                // ISO padding fills the blocks up with random bytes and adds the total number of padding
                // bytes as the last pad byte, adding an extra block if the last block is complete.
                //
                // xx rr rr rr rr rr rr 07
                case PaddingMode.ISO10126:
                    int isoSize = count + padBytes;

                    if (destination.Length < isoSize)
                    {
                        throw new ArgumentException(SR.Argument_DestinationTooShort, nameof(destination));
                    }

                    block.CopyTo(destination);
                    RandomNumberGenerator.Fill(destination.Slice(count, padBytes - 1));
                    destination[count + padBytes - 1] = (byte)padBytes;
                    return isoSize;

                // PKCS padding fills the blocks up with bytes containing the total number of padding bytes
                // used, adding an extra block if the last block is complete.
                //
                // xx xx 06 06 06 06 06 06
                case PaddingMode.PKCS7:
                    int pkcsSize = count + padBytes;

                    if (destination.Length < pkcsSize)
                    {
                        throw new ArgumentException(SR.Argument_DestinationTooShort, nameof(destination));
                    }

                    block.CopyTo(destination);
                    destination.Slice(count, padBytes).Fill((byte)padBytes);
                    return pkcsSize;

                // Zeros padding fills the last partial block with zeros, and does not add a new block to
                // the end if the last block is already complete.
                //
                //  xx 00 00 00 00 00 00 00
                case PaddingMode.Zeros:
                    if (padBytes == PaddingSizeBytes)
                    {
                        padBytes = 0;
                    }

                    int zeroSize = count + padBytes;

                    if (destination.Length < zeroSize)
                    {
                        throw new ArgumentException(SR.Argument_DestinationTooShort, nameof(destination));
                    }

                    block.CopyTo(destination);
                    destination.Slice(count, padBytes).Clear();
                    return zeroSize;

                default:
                    throw new CryptographicException(SR.Cryptography_UnknownPaddingMode);
            }
        }
    }
}
