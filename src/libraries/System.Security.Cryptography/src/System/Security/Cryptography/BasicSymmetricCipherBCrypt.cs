// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using Internal.Cryptography;
using Internal.NativeCrypto;

namespace System.Security.Cryptography
{
    internal sealed class BasicSymmetricCipherBCrypt : BasicSymmetricCipher
    {
        private readonly BasicSymmetricCipherLiteBCrypt _cipherLite;

        public BasicSymmetricCipherBCrypt(SafeAlgorithmHandle algorithm, CipherMode cipherMode, int blockSizeInBytes, int paddingSizeInBytes, ReadOnlySpan<byte> key, bool ownsParentHandle, byte[]? iv, bool encrypting)
            : base(cipherMode.GetCipherIv(iv), blockSizeInBytes, paddingSizeInBytes)
        {
            _cipherLite = new BasicSymmetricCipherLiteBCrypt(
                algorithm,
                blockSizeInBytes,
                paddingSizeInBytes,
                key,
                ownsParentHandle,
                IV, //implicit 'null' to empty span
                encrypting);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _cipherLite.Dispose();
            }

            base.Dispose(disposing);
        }

        public override int Transform(ReadOnlySpan<byte> input, Span<byte> output) =>
            _cipherLite.Transform(input, output);

        public override int TransformFinal(ReadOnlySpan<byte> input, Span<byte> output)
        {
            int numBytesWritten = _cipherLite.TransformFinal(input, output);
            _cipherLite.Reset(IV);
            return numBytesWritten;
        }
    }
}
