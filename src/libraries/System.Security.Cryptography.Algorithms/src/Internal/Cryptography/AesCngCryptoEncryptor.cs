// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;

using Internal.NativeCrypto;

namespace Internal.Cryptography
{
    internal sealed class AesCngCryptoEncryptor : AesCngCryptoTransform
    {
        public AesCngCryptoEncryptor(CipherMode cipherMode, PaddingMode paddingMode, byte[] key, byte[] iv, int blockSize)
            : base(cipherMode, paddingMode, key, iv, blockSize)
        {
        }

        protected sealed override int UncheckedTransformBlock(SafeKeyHandle hKey, byte[] currentIv, byte[] inputBuffer, int inputOffset, int inputCount, byte[] outputBuffer, int outputOffset)
        {
            int numBytesWritten = hKey.BCryptEncrypt(inputBuffer, inputOffset, inputCount, currentIv, outputBuffer, outputOffset, outputBuffer.Length);
            return numBytesWritten;
        }

        protected sealed override byte[] UncheckedTransformFinalBlock(SafeKeyHandle hKey, byte[] currentIv, byte[] inputBuffer, int inputOffset, int inputCount)
        {
            byte[] paddedBlock = PadBlock(inputBuffer, inputOffset, inputCount);
            byte[] output = new byte[paddedBlock.Length];
            hKey.BCryptEncrypt(paddedBlock, 0, paddedBlock.Length, currentIv, output, 0, output.Length);
            return output;
        }
    }
}
