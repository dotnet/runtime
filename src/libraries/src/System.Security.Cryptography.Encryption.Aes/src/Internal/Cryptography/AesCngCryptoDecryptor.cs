// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Security.Cryptography;

using Internal.NativeCrypto;

namespace Internal.Cryptography
{
    internal sealed class AesCngCryptoDecryptor : AesCngCryptoTransform
    {
        public AesCngCryptoDecryptor(CipherMode cipherMode, PaddingMode paddingMode, byte[] key, byte[] iv, int blockSize)
            : base(cipherMode, paddingMode, key, iv, blockSize)
        {
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

        protected sealed override void Reset()
        {
            base.Reset();
            if (_heldoverCipher != null)
            {
                Array.Clear(_heldoverCipher, 0, _heldoverCipher.Length);
                _heldoverCipher = null;
            }
        }

        protected sealed override int UncheckedTransformBlock(SafeKeyHandle hKey, byte[] currentIv, byte[] inputBuffer, int inputOffset, int inputCount, byte[] outputBuffer, int outputOffset)
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
                    int depadDecryptLength = DecryptInPlace(hKey, currentIv, _heldoverCipher, 0, _heldoverCipher.Length);
                    Buffer.BlockCopy(_heldoverCipher, 0, outputBuffer, outputOffset, depadDecryptLength);
                    Array.Clear(_heldoverCipher, 0, _heldoverCipher.Length);
                    outputOffset += depadDecryptLength;
                    decryptedBytes += depadDecryptLength;
                }
                else
                {
                    _heldoverCipher = new byte[InputBlockSize];
                }

                // Copy the last block of the input buffer into the depad buffer
                Debug.Assert(inputCount >= _heldoverCipher.Length, "inputCount >= _heldoverCipher.Length");
                Buffer.BlockCopy(inputBuffer,
                                 inputOffset + inputCount - _heldoverCipher.Length,
                                 _heldoverCipher,
                                 0,
                                 _heldoverCipher.Length);
                inputCount -= _heldoverCipher.Length;
                Debug.Assert(inputCount % InputBlockSize == 0, "Did not remove whole blocks for depadding");
            }

            // If after reserving the depad buffer there's still data to decrypt, make a copy of that in the output buffer to work on.
            if (inputCount > 0)
            {
                Buffer.BlockCopy(inputBuffer, inputOffset, outputBuffer, outputOffset, inputCount);
                decryptedBytes += DecryptInPlace(hKey, currentIv, outputBuffer, outputOffset, inputCount);
            }

            return decryptedBytes;
        }

        protected sealed override byte[] UncheckedTransformFinalBlock(SafeKeyHandle hKey, byte[] currentIv, byte[] inputBuffer, int inputOffset, int inputCount)
        {
            // We can't complete decryption on a partial block
            if (inputCount % InputBlockSize != 0)
                throw new CryptographicException(SR.Cryptography_PartialBlock);

            byte[] outputData;

            //
            // If we have a depad buffer, copy that into the decryption buffer followed by the input data.
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

            // Decrypt the data, then strip the padding to get the final decrypted data.
            if (ciphertext.Length > 0)
            {
                int decryptedBytes = DecryptInPlace(hKey, currentIv, ciphertext, 0, ciphertext.Length);
                outputData = DepadBlock(ciphertext, 0, decryptedBytes);
            }
            else
            {
                outputData = Array.Empty<byte>();
            }
            return outputData;
        }

        private static int DecryptInPlace(SafeKeyHandle hKey, byte[] currentIv, byte[] inOutBuffer, int offset, int count)
        {
            int numBytesWritten = Cng.BCryptDecrypt(hKey, inOutBuffer, offset, count, currentIv, inOutBuffer, offset, inOutBuffer.Length - offset);
            return numBytesWritten;
        }

        private bool DepaddingRequired
        {
            get
            {
                // Aes automatically strips padding after decryption when PKCS7 padding is in effect.
                // It does not do so when PaddingMode.Zeroes in effect as that padding mode is not sufficiently
                // self-describing to do the operation safely.
                return this.PaddingMode == PaddingMode.PKCS7;
            }
        }

        //
        // For padding modes that support automatic depadding, TransformBlock() leaves the last block it is given undone since it has no way of knowing
        // whether this is the final block that needs depadding. This block is held (in encrypted form) in _heldoverCipher. The next call to TransformBlock
        // or TransformFinalBlock must include the decryption of _heldoverCipher in the results.
        //
        private byte[] _heldoverCipher;
    }
}
