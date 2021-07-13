// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
        public static UniversalCryptoTransform Create(
            PaddingMode paddingMode,
            BasicSymmetricCipher cipher,
            bool encrypting)
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

        protected int PaddingSizeBytes
        {
            get { return BasicSymmetricCipher.PaddingSizeInBytes; }
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

        public abstract bool TransformOneShot(ReadOnlySpan<byte> input, Span<byte> output, out int bytesWritten);

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                BasicSymmetricCipher.Dispose();
            }
        }

        protected int UncheckedTransformBlock(byte[] inputBuffer, int inputOffset, int inputCount, byte[] outputBuffer, int outputOffset)
        {
            return UncheckedTransformBlock(inputBuffer.AsSpan(inputOffset, inputCount), outputBuffer.AsSpan(outputOffset));
        }

        protected abstract int UncheckedTransformBlock(ReadOnlySpan<byte> inputBuffer, Span<byte> outputBuffer);

        // For final block, encryption and decryption can give better context for the returning byte size, so we
        // don't provide an implementation here.
        protected abstract byte[] UncheckedTransformFinalBlock(byte[] inputBuffer, int inputOffset, int inputCount);
        protected abstract int UncheckedTransformFinalBlock(ReadOnlySpan<byte> inputBuffer, Span<byte> outputBuffer);

        protected PaddingMode PaddingMode { get; private set; }
        protected BasicSymmetricCipher BasicSymmetricCipher { get; private set; }
    }
}
