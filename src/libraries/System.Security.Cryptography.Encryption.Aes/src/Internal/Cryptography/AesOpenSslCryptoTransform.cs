// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Security.Cryptography;

using Microsoft.Win32.SafeHandles;

namespace Internal.Cryptography
{
    internal sealed class AesOpenSslCryptoTransform : AesNativeCryptoTransform
    {
        private readonly bool _encryptor;
        private SafeEvpCipherCtxHandle _ctx;
        private byte[] _decryptBuffer;

        private static readonly Tuple<int, CipherMode, Func<IntPtr>>[] s_algorithmInitializers =
        {
            // Neither OpenSSL nor AesCngCryptoTransform support CTS mode.
            // AesCngCryptoTransform doesn't seem to support CFB mode, and that would
            // require passing in the feedback size.  Since Windows doesn't support it,
            // we can skip it here, too.
            Tuple.Create(128, CipherMode.CBC, (Func<IntPtr>)Interop.libcrypto.EVP_aes_128_cbc),
            Tuple.Create(128, CipherMode.ECB, (Func<IntPtr>)Interop.libcrypto.EVP_aes_128_ecb),

            Tuple.Create(192, CipherMode.CBC, (Func<IntPtr>)Interop.libcrypto.EVP_aes_192_cbc),
            Tuple.Create(192, CipherMode.ECB, (Func<IntPtr>)Interop.libcrypto.EVP_aes_192_ecb),

            Tuple.Create(256, CipherMode.CBC, (Func<IntPtr>)Interop.libcrypto.EVP_aes_256_cbc),
            Tuple.Create(256, CipherMode.ECB, (Func<IntPtr>)Interop.libcrypto.EVP_aes_256_ecb),
        };

        internal AesOpenSslCryptoTransform(
            CipherMode cipherMode,
            PaddingMode paddingMode,
            byte[] key,
            byte[] iv,
            int blockSize,
            bool encryptor)
            : base(cipherMode, paddingMode, blockSize)
        {
            byte[] cipherIv = GetCipherIv(iv);

            _encryptor = encryptor;
            OpenKey(key, cipherIv);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_ctx != null)
                {
                    _ctx.Dispose();
                    _ctx = null;
                }

                if (_decryptBuffer != null)
                {
                    Array.Clear(_decryptBuffer, 0, _decryptBuffer.Length);
                    _decryptBuffer = null;
                }
            }

            base.Dispose(disposing);
        }

        protected override int UncheckedTransformBlock(
            byte[] inputBuffer,
            int inputOffset,
            int inputCount,
            byte[] outputBuffer,
            int outputOffset)
        {
            if (_encryptor)
            {
                return CipherUpdate(inputBuffer, inputOffset, inputCount, outputBuffer, outputOffset);
            }

            // Based on the caller we're sure that
            // A) The data is a multiple of InputBlockSize
            // B) That multiple isn't 0.

            int bytesWritten = 0;

            if (_decryptBuffer != null)
            {
                bytesWritten = CipherUpdate(_decryptBuffer, 0, _decryptBuffer.Length, outputBuffer, outputOffset);
            }
            else
            {
                _decryptBuffer = new byte[OutputBlockSize];
            }

            if (inputCount > InputBlockSize)
            {
                bytesWritten += CipherUpdate(
                    inputBuffer,
                    inputOffset,
                    inputCount - InputBlockSize,
                    outputBuffer,
                    outputOffset + bytesWritten);
            }

            Buffer.BlockCopy(
                inputBuffer,
                inputOffset + inputCount - InputBlockSize,
                _decryptBuffer,
                0,
                InputBlockSize);

            return bytesWritten;
        }
        
        protected override byte[] UncheckedTransformFinalBlock(byte[] inputBuffer, int inputOffset, int inputCount)
        {
            return _encryptor ?
                EncryptFinalBlock(inputBuffer, inputOffset, inputCount) :
                DecryptFinalBlock(inputBuffer, inputOffset, inputCount);
        }

        private void OpenKey(byte[] key, byte[] iv)
        {
            Func<IntPtr> algorithmFunc = FindAlgorithmSelector(key.Length * 8);

            _ctx = SafeEvpCipherCtxHandle.Create();

            // The algorithm pointer is a static pointer, so not having any cleanup code is correct.
            IntPtr algorithm = algorithmFunc();

            bool status = Interop.libcrypto.EVP_CipherInit_ex(
                _ctx,
                algorithm,
                IntPtr.Zero,
                key,
                iv,
                _encryptor ? 1 : 0);

            CheckBoolReturn(status);

            // OpenSSL will happily do PKCS#7 padding for us, but since we support padding modes
            // that it doesn't (PaddingMode.Zeros) we'll just always pad the blocks ourselves.
            status = Interop.libcrypto.EVP_CIPHER_CTX_set_padding(_ctx, 0);

            CheckBoolReturn(status);
        }

        private Func<IntPtr> FindAlgorithmSelector(int keySize)
        {
            bool foundKeysize = false;

            foreach (var triplet in s_algorithmInitializers)
            {
                if (triplet.Item1 == keySize && triplet.Item2 == CipherMode)
                {
                    return triplet.Item3;
                }

                if (triplet.Item1 == keySize)
                {
                    foundKeysize = true;
                }
            }

            if (!foundKeysize)
            {
                throw new CryptographicException(SR.Cryptography_InvalidKeySize);
            }

            // This is what AesCngCryptoTransform::SetCipherMode throws when it doesn't understand the value.
            throw new NotSupportedException();
        }

        protected override void Reset()
        {
            const int ENC_DIR_CURRENT = -1;

            // CipherInit with all nulls preserves the algorithm, resets the IV,
            // and maintains the key.
            //
            // The only thing that you can't do is change the encryption direction,
            // that requires passing the key and IV in again.
            //
            // But since we have a different object returned for CreateEncryptor
            // and CreateDecryptor we don't need to worry about that.
            bool status = Interop.libcrypto.EVP_CipherInit_ex(
                 _ctx,
                 IntPtr.Zero,
                 IntPtr.Zero,
                 null,
                 null,
                 ENC_DIR_CURRENT);

            CheckBoolReturn(status);
        }

        private byte[] EncryptFinalBlock(byte[] inputBuffer, int inputOffset, int inputCount)
        {
            byte[] paddedBlock = PadBlock(inputBuffer, inputOffset, inputCount);

            // PadBlock already skips anything before inputOffset, giving us back an array
            // which should be consumed in its entirety.
            return ProcessFinalBlock(paddedBlock, 0, paddedBlock.Length);
        }

        private byte[] DecryptFinalBlock(byte[] inputBuffer, int inputOffset, int inputCount)
        {
            // We can't complete decryption on a partial block
            if (inputCount % InputBlockSize != 0)
            {
                throw new CryptographicException(SR.Cryptography_PartialBlock);
            }

            byte[] decrypt = inputBuffer;
            int decryptOffset = inputOffset;
            int decryptCount = inputCount;

            if (_decryptBuffer != null)
            {
                decryptOffset = 0;
                decryptCount = inputCount + _decryptBuffer.Length;

                decrypt = new byte[decryptCount];
                Buffer.BlockCopy(_decryptBuffer, 0, decrypt, 0, _decryptBuffer.Length);
                Buffer.BlockCopy(inputBuffer, inputOffset, decrypt, _decryptBuffer.Length, inputCount);

                _decryptBuffer = null;
            }

            byte[] rawDecrypt = ProcessFinalBlock(decrypt, decryptOffset, decryptCount);

            if (rawDecrypt.Length == 0)
            {
                return Array.Empty<byte>();
            }

            return DepadBlock(rawDecrypt, 0, rawDecrypt.Length);
        }

        private unsafe byte[] ProcessFinalBlock(byte[] paddedBlock, int offset, int length)
        {
            bool status;
            int bytesWritten;

            byte[] output = new byte[length];
            int outputBytes = CipherUpdate(paddedBlock, offset, length, output, 0);

            fixed (byte* outputStart = output)
            {
                byte* outputCurrent = outputStart + outputBytes;

                status = Interop.libcrypto.EVP_CipherFinal_ex(_ctx, outputCurrent, out bytesWritten);
            }

            CheckBoolReturn(status);
            outputBytes += bytesWritten;

            if (outputBytes == output.Length)
            {
                return output;
            }

            if (outputBytes == 0)
            {
                return Array.Empty<byte>();
            }

            byte[] userData = new byte[outputBytes];
            Buffer.BlockCopy(output, 0, userData, 0, outputBytes);
            return userData;
        }

        private unsafe int CipherUpdate(
            byte[] inputBuffer,
            int inputOffset,
            int inputCount,
            byte[] outputBuffer,
            int outputOffset)
        {
            bool status;
            int bytesWritten;

            fixed (byte* inputStart = inputBuffer)
            fixed (byte* outputStart = outputBuffer)
            {
                byte* inputCurrent = inputStart + inputOffset;
                byte* outputCurrent = outputStart + outputOffset;

                status = Interop.libcrypto.EVP_CipherUpdate(
                    _ctx,
                    outputCurrent,
                    out bytesWritten,
                    inputCurrent,
                    inputCount);
            }

            CheckBoolReturn(status);
            return bytesWritten;
        }

        private static void CheckBoolReturn(bool returnValue)
        {
            if (returnValue)
            {
                return;
            }

            throw Interop.libcrypto.CreateOpenSslCryptographicException();
        }
    }
}
