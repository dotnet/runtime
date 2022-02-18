// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Security.Cryptography;
using Internal.Cryptography;

namespace System.Security.Cryptography
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

            int padWritten = SymmetricPadding.PadBlock(inputBuffer, outputBuffer, PaddingSizeBytes, PaddingMode);
            int transformWritten = BasicSymmetricCipher.TransformFinal(outputBuffer.Slice(0, padWritten), outputBuffer);

            // After padding, we should have an even number of blocks, and the same applies
            // to the transform.
            Debug.Assert(padWritten == transformWritten);

            return transformWritten;
        }

        protected override byte[] UncheckedTransformFinalBlock(byte[] inputBuffer, int inputOffset, int inputCount)
        {
            int ciphertextLength = SymmetricPadding.GetCiphertextLength(inputCount, PaddingSizeBytes, PaddingMode);
            byte[] buffer = GC.AllocateUninitializedArray<byte>(ciphertextLength);
            int written = UncheckedTransformFinalBlock(inputBuffer.AsSpan(inputOffset, inputCount), buffer);
            Debug.Assert(written == buffer.Length);
            return buffer;
        }
    }
}
