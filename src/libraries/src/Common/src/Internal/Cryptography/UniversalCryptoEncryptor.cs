// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

        protected sealed override int UncheckedTransformBlock(byte[] inputBuffer, int inputOffset, int inputCount, byte[] outputBuffer, int outputOffset)
        {
            return BasicSymmetricCipher.Transform(inputBuffer, inputOffset, inputCount, outputBuffer, outputOffset);
        }

        protected sealed override unsafe byte[] UncheckedTransformFinalBlock(byte[] inputBuffer, int inputOffset, int inputCount)
        {
            byte[] paddedBlock = PadBlock(inputBuffer, inputOffset, inputCount);

            fixed (byte* paddedBlockPtr = paddedBlock)
            {
                byte[] output = BasicSymmetricCipher.TransformFinal(paddedBlock, 0, paddedBlock.Length);

                if (paddedBlock != inputBuffer)
                {
                    CryptographicOperations.ZeroMemory(paddedBlock);
                }

                return output;
            }
        }

        private byte[] PadBlock(byte[] block, int offset, int count)
        {
            byte[] result;
            int padBytes = InputBlockSize - (count % InputBlockSize);

            switch (PaddingMode)
            {
                case PaddingMode.None:
                    if (count % InputBlockSize != 0)
                        throw new CryptographicException(SR.Cryptography_PartialBlock);

                    result = new byte[count];
                    Buffer.BlockCopy(block, offset, result, 0, result.Length);
                    break;

                // ANSI padding fills the blocks with zeros and adds the total number of padding bytes as
                // the last pad byte, adding an extra block if the last block is complete.
                //
                // x 00 00 00 00 00 00 07
                case PaddingMode.ANSIX923:
                    result = new byte[count + padBytes];

                    Buffer.BlockCopy(block, offset, result, 0, count);
                    result[result.Length - 1] = (byte)padBytes;

                    break;

                // ISO padding fills the blocks up with random bytes and adds the total number of padding
                // bytes as the last pad byte, adding an extra block if the last block is complete.
                //
                // xx rr rr rr rr rr rr 07
                case PaddingMode.ISO10126:
                    result = new byte[count + padBytes];
                    
                    Buffer.BlockCopy(block, offset, result, 0, count);
                    RandomNumberGenerator.Fill(result.AsSpan(count + 1, padBytes - 1));
                    result[result.Length - 1] = (byte)padBytes;

                    break;

                // PKCS padding fills the blocks up with bytes containing the total number of padding bytes
                // used, adding an extra block if the last block is complete.
                //
                // xx xx 06 06 06 06 06 06
                case PaddingMode.PKCS7:
                    result = new byte[count + padBytes];
                    Buffer.BlockCopy(block, offset, result, 0, count);

                    for (int i = count; i < result.Length; i++)
                    {
                        result[i] = (byte)padBytes;
                    }
                    break;

                // Zeros padding fills the last partial block with zeros, and does not add a new block to
                // the end if the last block is already complete.
                //
                //  xx 00 00 00 00 00 00 00
                case PaddingMode.Zeros:
                    if (padBytes == InputBlockSize)
                    {
                        padBytes = 0;
                    }

                    result = new byte[count + padBytes];
                    Buffer.BlockCopy(block, offset, result, 0, count);
                    break;

                default:
                    throw new CryptographicException(SR.Cryptography_UnknownPaddingMode);
            }

            return result;
        }
    }
}
