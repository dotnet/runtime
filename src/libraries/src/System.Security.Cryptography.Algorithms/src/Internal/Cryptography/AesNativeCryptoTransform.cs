// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Security.Cryptography;

namespace Internal.Cryptography
{
    internal abstract class AesNativeCryptoTransform : ICryptoTransform
    {
        private readonly int _blockSize;

        protected CipherMode CipherMode { get; private set; }
        protected PaddingMode PaddingMode { get; private set; }

        protected AesNativeCryptoTransform(CipherMode cipherMode, PaddingMode paddingMode, int blockSize)
        {
            _blockSize = blockSize;
            CipherMode = cipherMode;
            PaddingMode = paddingMode;
        }

        public bool CanReuseTransform
        {
            get { return true; }
        }

        public bool CanTransformMultipleBlocks
        {
            get { return true; }
        }

        public int InputBlockSize
        {
            get { return _blockSize; }
        }

        public int OutputBlockSize
        {
            get { return _blockSize; }
        }


        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public int TransformBlock(byte[] inputBuffer, int inputOffset, int inputCount, byte[] outputBuffer, int outputOffset)
        {
            if (inputBuffer == null)
                throw new ArgumentNullException("inputBuffer");
            if (inputOffset < 0)
                throw new ArgumentOutOfRangeException("inputOffset");
            if (inputOffset > inputBuffer.Length)
                throw new ArgumentOutOfRangeException("inputOffset");
            if (inputCount <= 0)
                throw new ArgumentOutOfRangeException("inputCount");
            if (inputCount % InputBlockSize != 0)
                throw new ArgumentOutOfRangeException("inputCount", SR.Cryptography_MustTransformWholeBlock);
            if (inputCount > inputBuffer.Length - inputOffset)
                throw new ArgumentOutOfRangeException("inputCount", SR.Cryptography_TransformBeyondEndOfBuffer);
            if (outputBuffer == null)
                throw new ArgumentNullException("outputBuffer");
            if (outputOffset > outputBuffer.Length)
                throw new ArgumentOutOfRangeException("outputOffset");
            if (inputCount > outputBuffer.Length - outputOffset)
                throw new ArgumentOutOfRangeException("outputOffset", SR.Cryptography_TransformBeyondEndOfBuffer);
            
            return UncheckedTransformBlock(inputBuffer, inputOffset, inputCount, outputBuffer, outputOffset);
        }

        public byte[] TransformFinalBlock(byte[] inputBuffer, int inputOffset, int inputCount)
        {
            if (inputBuffer == null)
                throw new ArgumentNullException("inputBuffer");
            if (inputOffset < 0)
                throw new ArgumentOutOfRangeException("inputOffset");
            if (inputCount < 0)
                throw new ArgumentOutOfRangeException("inputCount");
            if (inputOffset > inputBuffer.Length)
                throw new ArgumentOutOfRangeException("inputOffset");
            if (inputCount > inputBuffer.Length - inputOffset)
                throw new ArgumentOutOfRangeException("inputCount", SR.Cryptography_TransformBeyondEndOfBuffer);

            byte[] output = UncheckedTransformFinalBlock(inputBuffer, inputOffset, inputCount);
            Reset();
            return output;
        }

        protected virtual void Dispose(bool disposing)
        {
        }

        protected abstract int UncheckedTransformBlock(
            byte[] inputBuffer,
            int inputOffset,
            int inputCount,
            byte[] outputBuffer,
            int outputOffset);

        protected abstract byte[] UncheckedTransformFinalBlock(byte[] inputBuffer, int inputOffset, int inputCount);

        protected abstract void Reset();

        protected byte[] PadBlock(byte[] block, int offset, int count)
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

        /// <summary>
        ///     Remove the padding from the last blocks being decrypted
        /// </summary>
        protected byte[] DepadBlock(byte[] block, int offset, int count)
        {
            Debug.Assert(block != null && count >= block.Length - offset);
            Debug.Assert(0 <= offset);
            Debug.Assert(0 <= count);

            int padBytes = 0;

            // See PadBlock for a description of the padding modes.
            switch (PaddingMode)
            {
                case PaddingMode.PKCS7:
                    padBytes = block[offset + count - 1];

                    // Verify the amount of padding is reasonable
                    if (padBytes <= 0 || padBytes > InputBlockSize)
                        throw new CryptographicException(SR.Cryptography_InvalidPadding);

                    // Verify all the padding bytes match the amount of padding
                    for (int i = offset + count - padBytes; i < offset + count; i++)
                    {
                        if (block[i] != padBytes)
                            throw new CryptographicException(SR.Cryptography_InvalidPadding);
                    }

                    break;

                // We cannot remove Zeros padding because we don't know if the zeros at the end of the block
                // belong to the padding or the plaintext itself.
                case PaddingMode.Zeros:
                case PaddingMode.None:
                    padBytes = 0;
                    break;

                default:
                    throw new CryptographicException(SR.Cryptography_UnknownPaddingMode);
            }

            // Copy everything but the padding to the output
            byte[] depadded = new byte[count - padBytes];
            Buffer.BlockCopy(block, offset, depadded, 0, depadded.Length);
            return depadded;
        }

        protected byte[] GetCipherIv(byte[] iv)
        {
            if (CipherMode.UsesIv())
            {
                if (iv == null)
                {
                    throw new CryptographicException(SR.Cryptography_MissingIV);
                }

                return iv;
            }

            return null;
        }
    }
}
