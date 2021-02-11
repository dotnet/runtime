// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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

        protected override int UncheckedTransformBlock(ReadOnlySpan<byte> inputBuffer, Span<byte> outputBuffer)
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
                    int depadDecryptLength = BasicSymmetricCipher.Transform(_heldoverCipher, outputBuffer);
                    outputBuffer = outputBuffer.Slice(depadDecryptLength);
                    decryptedBytes += depadDecryptLength;
                }
                else
                {
                    _heldoverCipher = new byte[InputBlockSize];
                }

                // Postpone the last block to the next round.
                Debug.Assert(inputBuffer.Length >= _heldoverCipher.Length, "inputBuffer.Length >= _heldoverCipher.Length");
                inputBuffer.Slice(inputBuffer.Length - _heldoverCipher.Length).CopyTo(_heldoverCipher);
                inputBuffer = inputBuffer.Slice(0, inputBuffer.Length - _heldoverCipher.Length);
                Debug.Assert(inputBuffer.Length % InputBlockSize == 0, "Did not remove whole blocks for depadding");
            }

            if (inputBuffer.Length > 0)
            {
                decryptedBytes += BasicSymmetricCipher.Transform(inputBuffer, outputBuffer);
            }

            return decryptedBytes;
        }

        protected override unsafe int UncheckedTransformFinalBlock(ReadOnlySpan<byte> inputBuffer, Span<byte> outputBuffer)
        {
            // We can't complete decryption on a partial block
            if (inputBuffer.Length % PaddingSizeBytes != 0)
                throw new CryptographicException(SR.Cryptography_PartialBlock);

            //
            // If we have postponed cipher bits from the prior round, copy that into the decryption buffer followed by the input data.
            // Otherwise the decryption buffer is just the input data.
            //

            ReadOnlySpan<byte> inputCiphertext;
            Span<byte> ciphertext;
            byte[]? rentedCiphertext = null;
            int rentedCiphertextSize = 0;

            try
            {
                if (_heldoverCipher == null)
                {
                    rentedCiphertextSize = inputBuffer.Length;
                    rentedCiphertext = CryptoPool.Rent(inputBuffer.Length);
                    ciphertext = rentedCiphertext.AsSpan(0, inputBuffer.Length);
                    inputCiphertext = inputBuffer;
                }
                else
                {
                    rentedCiphertextSize = _heldoverCipher.Length + inputBuffer.Length;
                    rentedCiphertext = CryptoPool.Rent(rentedCiphertextSize);
                    ciphertext = rentedCiphertext.AsSpan(0, rentedCiphertextSize);
                    _heldoverCipher.AsSpan().CopyTo(ciphertext);
                    inputBuffer.CopyTo(ciphertext.Slice(_heldoverCipher.Length));

                    // Decrypt in-place
                    inputCiphertext = ciphertext;
                }

                int unpaddedLength = 0;

                fixed (byte* pCiphertext = ciphertext)
                {
                    // Decrypt the data, then strip the padding to get the final decrypted data. Note that even if the cipherText length is 0, we must
                    // invoke TransformFinal() so that the cipher object knows to reset for the next cipher operation.
                    int decryptWritten = BasicSymmetricCipher.TransformFinal(inputCiphertext, ciphertext);
                    Span<byte> decryptedBytes = ciphertext.Slice(0, decryptWritten);

                    if (decryptedBytes.Length > 0)
                    {
                        unpaddedLength = GetPaddingLength(decryptedBytes);
                        decryptedBytes.Slice(0, unpaddedLength).CopyTo(outputBuffer);
                    }
                }

                Reset();
                return unpaddedLength;
            }
            finally
            {
                if (rentedCiphertext != null)
                {
                    CryptoPool.Return(rentedCiphertext, clearSize: rentedCiphertextSize);
                }
            }
        }

        protected override unsafe byte[] UncheckedTransformFinalBlock(byte[] inputBuffer, int inputOffset, int inputCount)
        {
            if (DepaddingRequired)
            {
                byte[] rented = CryptoPool.Rent(inputCount + InputBlockSize);
                int written = 0;

                fixed (byte* pRented = rented)
                {
                    try
                    {
                        written = UncheckedTransformFinalBlock(inputBuffer.AsSpan(inputOffset, inputCount), rented);
                        return rented.AsSpan(0, written).ToArray();
                    }
                    finally
                    {
                        CryptoPool.Return(rented, clearSize: written);
                    }
                }
            }
            else
            {
#if NETSTANDARD || NETFRAMEWORK || NETCOREAPP3_0
                byte[] buffer = new byte[inputCount];
#else
                byte[] buffer = GC.AllocateUninitializedArray<byte>(inputCount);
#endif
                int written = UncheckedTransformFinalBlock(inputBuffer.AsSpan(inputOffset, inputCount), buffer);
                Debug.Assert(written == buffer.Length);
                return buffer;
            }
        }

        protected sealed override void Dispose(bool disposing)
        {
            if (disposing)
            {
                byte[]? heldoverCipher = _heldoverCipher;
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
                        Debug.Fail($"Unknown padding mode {PaddingMode}.");
                        throw new CryptographicException(SR.Cryptography_UnknownPaddingMode);
                }
            }
        }

        /// <summary>
        ///     Gets the length of the padding applied to the block, and validates
        ///     the padding, if possible.
        /// </summary>
        private int GetPaddingLength(ReadOnlySpan<byte> block)
        {
            int padBytes = 0;

            // See PadBlock for a description of the padding modes.
            switch (PaddingMode)
            {
                case PaddingMode.ANSIX923:
                    padBytes = block[^1];

                    // Verify the amount of padding is reasonable
                    if (padBytes <= 0 || padBytes > InputBlockSize)
                    {
                        throw new CryptographicException(SR.Cryptography_InvalidPadding);
                    }

                    // Verify that all the padding bytes are 0s
                    for (int i = block.Length - padBytes; i < block.Length - 1; i++)
                    {
                        if (block[i] != 0)
                        {
                            throw new CryptographicException(SR.Cryptography_InvalidPadding);
                        }
                    }

                    break;

                case PaddingMode.ISO10126:
                    padBytes = block[^1];

                    // Verify the amount of padding is reasonable
                    if (padBytes <= 0 || padBytes > InputBlockSize)
                    {
                        throw new CryptographicException(SR.Cryptography_InvalidPadding);
                    }

                    // Since the padding consists of random bytes, we cannot verify the actual pad bytes themselves
                    break;

                case PaddingMode.PKCS7:
                    padBytes = block[^1];

                    // Verify the amount of padding is reasonable
                    if (padBytes <= 0 || padBytes > InputBlockSize)
                        throw new CryptographicException(SR.Cryptography_InvalidPadding);

                    // Verify all the padding bytes match the amount of padding
                    for (int i = block.Length - padBytes; i < block.Length - 1; i++)
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

            return block.Length - padBytes;
        }

        //
        // For padding modes that support automatic depadding, TransformBlock() leaves the last block it is given undone since it has no way of knowing
        // whether this is the final block that needs depadding. This block is held (in encrypted form) in _heldoverCipher. The next call to TransformBlock
        // or TransformFinalBlock must include the decryption of _heldoverCipher in the results.
        //
        private byte[]? _heldoverCipher;
    }
}
