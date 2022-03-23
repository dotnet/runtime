// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.Cryptography;

namespace System.Security.Cryptography
{
    internal sealed class BasicSymmetricCipherNCrypt : BasicSymmetricCipher
    {
        private BasicSymmetricCipherLiteNCrypt _cipher;

        //
        // The first parameter is a delegate that instantiates a CngKey rather than a CngKey itself. That's because CngKeys are stateful objects
        // and concurrent encryptions on the same CngKey will corrupt each other.
        //
        // The delegate must instantiate a new CngKey, based on a new underlying NCryptKeyHandle, each time is called.
        //
        public BasicSymmetricCipherNCrypt(Func<CngKey> cngKeyFactory, CipherMode cipherMode, int blockSizeInBytes, byte[]? iv, bool encrypting, int paddingSizeInBytes)
            : base(iv, blockSizeInBytes, paddingSizeInBytes)
        {
            _cipher = new BasicSymmetricCipherLiteNCrypt(cngKeyFactory, cipherMode, blockSizeInBytes, iv, encrypting, paddingSizeInBytes);
        }

        public sealed override int Transform(ReadOnlySpan<byte> input, Span<byte> output) => _cipher.Transform(input, output);

        public sealed override int TransformFinal(ReadOnlySpan<byte> input, Span<byte> output)
        {
            int written = _cipher.TransformFinal(input, output);
            Reset();
            return written;
        }

        protected sealed override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _cipher?.Dispose();
                _cipher = null!;
            }

            base.Dispose(disposing);
        }

        private void Reset()
        {
            if (IV is not null)
            {
                _cipher.Reset(IV);
            }
        }
    }
}
