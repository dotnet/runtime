// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Security.Cryptography;

namespace Internal.Cryptography
{
    //
    // The common base class for the cross-platform CreateEncryptor()/CreateDecryptor() implementations. 
    //
    //  - Implements the various padding algorithms (as we support padding algorithms that the underlying native apis don't.)
    //
    //  - Parameterized by a BasicSymmetricCipher which encapsulates the algorithm, key, IV, chaining mode, direction of encryption
    //    and the underlying native apis implementing the encryption.
    //
    internal abstract class UniversalCryptoTransform : ICryptoTransform
    {
        public static ICryptoTransform Create(PaddingMode paddingMode, BasicSymmetricCipher cipher, bool encrypting)
        {
            if (encrypting)
                return new UniversalCryptoEncryptor(paddingMode, cipher);
            else
                return new UniversalCryptoDecryptor(paddingMode, cipher);
        }

        protected UniversalCryptoTransform(PaddingMode paddingMode, BasicSymmetricCipher basicSymmetricCipher)
        {
            PaddingMode = paddingMode;
            BasicSymmetricCipher = basicSymmetricCipher;
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
            get { return BasicSymmetricCipher.BlockSizeInBytes; }
        }

        public int OutputBlockSize
        {
            get { return BasicSymmetricCipher.BlockSizeInBytes; }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public int TransformBlock(byte[] inputBuffer, int inputOffset, int inputCount, byte[] outputBuffer, int outputOffset)
        {
            if (inputBuffer == null)
                throw new ArgumentNullException(nameof(inputBuffer));
            if (inputOffset < 0)
                throw new ArgumentOutOfRangeException(nameof(inputOffset));
            if (inputOffset > inputBuffer.Length)
                throw new ArgumentOutOfRangeException(nameof(inputOffset));
            if (inputCount <= 0)
                throw new ArgumentOutOfRangeException(nameof(inputCount));
            if (inputCount % InputBlockSize != 0)
                throw new ArgumentOutOfRangeException(nameof(inputCount), SR.Cryptography_MustTransformWholeBlock);
            if (inputCount > inputBuffer.Length - inputOffset)
                throw new ArgumentOutOfRangeException(nameof(inputCount), SR.Cryptography_TransformBeyondEndOfBuffer);
            if (outputBuffer == null)
                throw new ArgumentNullException(nameof(outputBuffer));
            if (outputOffset > outputBuffer.Length)
                throw new ArgumentOutOfRangeException(nameof(outputOffset));
            if (inputCount > outputBuffer.Length - outputOffset)
                throw new ArgumentOutOfRangeException(nameof(outputOffset), SR.Cryptography_TransformBeyondEndOfBuffer);
            
            int numBytesWritten = UncheckedTransformBlock(inputBuffer, inputOffset, inputCount, outputBuffer, outputOffset);
            Debug.Assert(numBytesWritten >= 0 && numBytesWritten <= inputCount);
            return numBytesWritten;
        }

        public byte[] TransformFinalBlock(byte[] inputBuffer, int inputOffset, int inputCount)
        {
            if (inputBuffer == null)
                throw new ArgumentNullException(nameof(inputBuffer));
            if (inputOffset < 0)
                throw new ArgumentOutOfRangeException(nameof(inputOffset));
            if (inputCount < 0)
                throw new ArgumentOutOfRangeException(nameof(inputCount));
            if (inputOffset > inputBuffer.Length)
                throw new ArgumentOutOfRangeException(nameof(inputOffset));
            if (inputCount > inputBuffer.Length - inputOffset)
                throw new ArgumentOutOfRangeException(nameof(inputCount), SR.Cryptography_TransformBeyondEndOfBuffer);

            byte[] output = UncheckedTransformFinalBlock(inputBuffer, inputOffset, inputCount);
            return output;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                BasicSymmetricCipher.Dispose();
            }
        }

        protected abstract int UncheckedTransformBlock(byte[] inputBuffer, int inputOffset, int inputCount, byte[] outputBuffer, int outputOffset);
        protected abstract byte[] UncheckedTransformFinalBlock(byte[] inputBuffer, int inputOffset, int inputCount);

        protected PaddingMode PaddingMode { get; private set; }
        protected BasicSymmetricCipher BasicSymmetricCipher { get; private set; }
    }
}

