// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Security.Cryptography;

namespace Internal.Cryptography
{
    //
    // A cross-platform ICryptoTransform implementation for decryption. 
    //
    //  - Implements the various padding algorithms (as we support padding algorithms that the underlying native apis don't.)
    //
    //  - Parameterized by a BasicSymmetricCipher which encapsulates the algorithm, key, IV, chaining mode, direction of encryption
    //    and the underlying native apis implementing the encryption.
    //
    internal sealed class UniversalCryptoDecryptor : UniversalCryptoTransform
    {
        public UniversalCryptoDecryptor(PaddingMode paddingMode, BasicSymmetricCipher basicSymmetricCipher)
            : base(paddingMode, basicSymmetricCipher)
        {
        }

        protected sealed override int UncheckedTransformBlock(byte[] inputBuffer, int inputOffset, int inputCount, byte[] outputBuffer, int outputOffset)
        {
            //
            // If we're decrypting, it's possible to be called with the last blocks of the data, and then
            // have TransformFinalBlock called with an empty array. Since we don't know if this is the case,
            // we won't decrypt the last block of the input until either TransformBlock or
            // TransformFinalBlock is next called.
            //
            // We don't need to do this for PaddingMode.None because there is no padding to strip, and
            // we also don't do this for PaddingMode.Zeros since there is no way for us to tell if the
            // zeros at the end of a block are part of the plaintext or the padding.
            //
            int decryptedBytes = 0;
            if (DepaddingRequired)
            {
                // If we have data saved from a previous call, decrypt that into the output first
                if (_heldoverCipher != null)
                {
                    int depadDecryptLength = BasicSymmetricCipher.Transform(_heldoverCipher, 0, _heldoverCipher.Length, outputBuffer, outputOffset);
                    outputOffset += depadDecryptLength;
                    decryptedBytes += depadDecryptLength;
                }
                else
                {
                    _heldoverCipher = new byte[InputBlockSize];
                }

                // Postpone the last block to the next round.
                Debug.Assert(inputCount >= _heldoverCipher.Length, "inputCount >= _heldoverCipher.Length");
                int startOfLastBlock = inputOffset + inputCount - _heldoverCipher.Length;
                Buffer.BlockCopy(inputBuffer, startOfLastBlock, _heldoverCipher, 0, _heldoverCipher.Length);
                inputCount -= _heldoverCipher.Length;
                Debug.Assert(inputCount % InputBlockSize == 0, "Did not remove whole blocks for depadding");
            }

            if (inputCount > 0)
            {
                decryptedBytes += BasicSymmetricCipher.Transform(inputBuffer, inputOffset, inputCount, outputBuffer, outputOffset);
            }

            return decryptedBytes;
        }

        protected sealed override byte[] UncheckedTransformFinalBlock(byte[] inputBuffer, int inputOffset, int inputCount)
        {
            // We can't complete decryption on a partial block
            if (inputCount % InputBlockSize != 0)
                throw new CryptographicException(SR.Cryptography_PartialBlock);

            //
            // If we have postponed cipher bits from the prior round, copy that into the decryption buffer followed by the input data.
            // Otherwise the decryption buffer is just the input data.
            //

            byte[] ciphertext = null;

            if (_heldoverCipher == null)
            {
                ciphertext = new byte[inputCount];
                Buffer.BlockCopy(inputBuffer, inputOffset, ciphertext, 0, inputCount);
            }
            else
            {
                ciphertext = new byte[_heldoverCipher.Length + inputCount];
                Buffer.BlockCopy(_heldoverCipher, 0, ciphertext, 0, _heldoverCipher.Length);
                Buffer.BlockCopy(inputBuffer, inputOffset, ciphertext, _heldoverCipher.Length, inputCount);
            }

            // Decrypt the data, then strip the padding to get the final decrypted data. Note that even if the cipherText length is 0, we must
            // invoke TransformFinal() so that the cipher object knows to reset for the next cipher operation.
            byte[] decryptedBytes = BasicSymmetricCipher.TransformFinal(ciphertext, 0, ciphertext.Length);
            byte[] outputData;
            if (ciphertext.Length > 0)
            {
                outputData = DepadBlock(decryptedBytes, 0, decryptedBytes.Length);
            }
            else
            {
                outputData = Array.Empty<byte>();
            }

            Reset();
            return outputData;
        }

        protected sealed override void Dispose(bool disposing)
        {
            if (disposing)
            {
                byte[] heldoverCipher = _heldoverCipher;
                _heldoverCipher = null;
                if (heldoverCipher != null)
                {
                    Array.Clear(heldoverCipher, 0, heldoverCipher.Length);
                }
            }

            base.Dispose(disposing);
        }

        private void Reset()
        {
            if (_heldoverCipher != null)
            {
                Array.Clear(_heldoverCipher, 0, _heldoverCipher.Length);
                _heldoverCipher = null;
            }
        }

        private bool DepaddingRequired
        {
            get
            {
                // Some padding modes encode sufficient information to allow for automatic depadding to happen.
                switch (PaddingMode)
                {
                    case PaddingMode.PKCS7:
                    case PaddingMode.ANSIX923:
                    case PaddingMode.ISO10126:
                        return true;
                    case PaddingMode.Zeros:
                    case PaddingMode.None:
                        return false;
                    default:
                        Debug.Fail($"Invalid padding mode {PaddingMode}.");
                        throw new CryptographicException(SR.Cryptography_InvalidPadding);
                }
            }
        }

        /// <summary>
        ///     Remove the padding from the last blocks being decrypted
        /// </summary>
        private byte[] DepadBlock(byte[] block, int offset, int count)
        {
            Debug.Assert(block != null && count >= block.Length - offset);
            Debug.Assert(0 <= offset);
            Debug.Assert(0 <= count);

            int padBytes = 0;

            // See PadBlock for a description of the padding modes.
            switch (PaddingMode)
            {
                case PaddingMode.ANSIX923:
                    padBytes = block[offset + count - 1];

                    // Verify the amount of padding is reasonable
                    if (padBytes <= 0 || padBytes > InputBlockSize)
                    {
                        throw new CryptographicException(SR.Cryptography_InvalidPadding);
                    }

                    // Verify that all the padding bytes are 0s
                    for (int i = offset + count - padBytes; i < offset + count - 1; i++)
                    {
                        if (block[i] != 0)
                        {
                            throw new CryptographicException(SR.Cryptography_InvalidPadding);
                        }
                    }

                    break;

                case PaddingMode.ISO10126:
                    padBytes = block[offset + count - 1];

                    // Verify the amount of padding is reasonable
                    if (padBytes <= 0 || padBytes > InputBlockSize)
                    {
                        throw new CryptographicException(SR.Cryptography_InvalidPadding);
                    }

                    // Since the padding consists of random bytes, we cannot verify the actual pad bytes themselves
                    break;

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

        //
        // For padding modes that support automatic depadding, TransformBlock() leaves the last block it is given undone since it has no way of knowing
        // whether this is the final block that needs depadding. This block is held (in encrypted form) in _heldoverCipher. The next call to TransformBlock
        // or TransformFinalBlock must include the decryption of _heldoverCipher in the results.
        //
        private byte[] _heldoverCipher;
    }
}
