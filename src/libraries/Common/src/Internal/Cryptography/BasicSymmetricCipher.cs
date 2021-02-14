// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Security.Cryptography;

namespace Internal.Cryptography
{
    //
    // Represents a symmetric reusable cipher encryptor or decryptor. Underlying technology may be CNG or OpenSSL or anything else.
    // The key, IV, chaining mode, blocksize and direction of encryption are all locked in when the BasicSymmetricCipher is instantiated.
    //
    //  - Performs no padding. Padding is done by a higher-level layer.
    //
    //  - Transform and TransformFinal only accept blocks whose sizes are a multiple of the crypto algorithms block size.
    //
    //  - Transform() can do in-place encryption/decryption (input and output referencing the same array.)
    //
    //  - TransformFinal() resets the object for reuse.
    //
    internal abstract class BasicSymmetricCipher : IDisposable
    {
        protected BasicSymmetricCipher(byte[]? iv, int blockSizeInBytes, int paddingSizeInBytes)
        {
            IV = iv;
            BlockSizeInBytes = blockSizeInBytes;
            PaddingSizeInBytes = paddingSizeInBytes > 0 ? paddingSizeInBytes : blockSizeInBytes;
        }

        public abstract int Transform(ReadOnlySpan<byte> input, Span<byte> output);

        public abstract int TransformFinal(ReadOnlySpan<byte> input, Span<byte> output);

        public int BlockSizeInBytes { get; private set; }
        public int PaddingSizeInBytes { get; private set; }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (IV != null)
                {
                    Array.Clear(IV, 0, IV.Length);
                    IV = null;
                }
            }
        }

        protected byte[]? IV { get; private set; }
    }
}
