// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Internal.Cryptography;

namespace System.Security.Cryptography
{
    internal sealed class OpenSslCipher : BasicSymmetricCipher
    {
        private readonly OpenSslCipherLite _cipherLite;

        public OpenSslCipher(IntPtr algorithm, CipherMode cipherMode, int blockSizeInBytes, int paddingSizeInBytes, byte[] key, byte[]? iv, bool encrypting)
            : base(cipherMode.GetCipherIv(iv), blockSizeInBytes, paddingSizeInBytes)
        {
            _cipherLite = new OpenSslCipherLite(
                algorithm,
                blockSizeInBytes,
                paddingSizeInBytes,
                key,
                iv,
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

        public override unsafe int Transform(ReadOnlySpan<byte> input, Span<byte> output)
        {
            Debug.Assert(input.Length > 0);
            Debug.Assert((input.Length % PaddingSizeInBytes) == 0);
            return _cipherLite.Transform(input, output);
        }

        public override int TransformFinal(ReadOnlySpan<byte> input, Span<byte> output)
        {
            Debug.Assert((input.Length % PaddingSizeInBytes) == 0);
            Debug.Assert(input.Length <= output.Length);

            int written = _cipherLite.TransformFinal(input, output);
            _cipherLite.Reset(IV);
            return written;
        }
    }
}
