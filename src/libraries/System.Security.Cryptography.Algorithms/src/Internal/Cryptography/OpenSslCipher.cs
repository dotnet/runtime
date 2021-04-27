// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Microsoft.Win32.SafeHandles;

namespace Internal.Cryptography
{
    internal sealed class OpenSslCipher : BasicSymmetricCipher
    {
        private readonly bool _encrypting;
        private SafeEvpCipherCtxHandle _ctx;

        public OpenSslCipher(IntPtr algorithm, CipherMode cipherMode, int blockSizeInBytes, int paddingSizeInBytes, byte[] key, int effectiveKeyLength, byte[]? iv, bool encrypting)
            : base(cipherMode.GetCipherIv(iv), blockSizeInBytes, paddingSizeInBytes)
        {
            Debug.Assert(algorithm != IntPtr.Zero);

            _encrypting = encrypting;

            OpenKey(algorithm, key, effectiveKeyLength);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_ctx != null)
                {
                    _ctx.Dispose();
                    _ctx = null!;
                }
            }

            base.Dispose(disposing);
        }

        public override unsafe int Transform(ReadOnlySpan<byte> input, Span<byte> output)
        {
            Debug.Assert(input.Length > 0);
            Debug.Assert((input.Length % PaddingSizeInBytes) == 0);

            // OpenSSL 1.1 does not allow partial overlap.
            if (input.Overlaps(output, out int overlapOffset) && overlapOffset != 0)
            {
                byte[] tmp = CryptoPool.Rent(input.Length);
                Span<byte> tmpSpan = tmp;
                int written = 0;

                try
                {
                    written = CipherUpdate(input, tmpSpan);
                    tmpSpan.Slice(0, written).CopyTo(output);
                    return written;
                }
                finally
                {
                    CryptoPool.Return(tmp, written);
                }
            }

            return CipherUpdate(input, output);
        }

        public override int TransformFinal(ReadOnlySpan<byte> input, Span<byte> output)
        {
            Debug.Assert((input.Length % PaddingSizeInBytes) == 0);
            Debug.Assert(input.Length <= output.Length);

            int written = ProcessFinalBlock(input, output);
            Reset();
            return written;
        }

        private int ProcessFinalBlock(ReadOnlySpan<byte> input, Span<byte> output)
        {
            // If input and output overlap but are not the same, we need to use a
            // temp buffer since openssl doesn't seem to like partial overlaps.
            if (input.Overlaps(output, out int offset) && offset != 0)
            {
                byte[] rented = CryptoPool.Rent(input.Length);
                int written = 0;

                try
                {
                    written = CipherUpdate(input, rented);
                    Span<byte> outputSpan = rented.AsSpan(written);
                    CheckBoolReturn(Interop.Crypto.EvpCipherFinalEx(_ctx, outputSpan, out int finalWritten));
                    written += finalWritten;
                    rented.AsSpan(0, written).CopyTo(output);
                    return written;
                }
                finally
                {
                    CryptoPool.Return(rented, clearSize: written);
                }
            }
            else
            {
                int written = CipherUpdate(input, output);
                Span<byte> outputSpan = output.Slice(written);
                CheckBoolReturn(Interop.Crypto.EvpCipherFinalEx(_ctx, outputSpan, out int finalWritten));
                written += finalWritten;
                return written;
            }
        }

        private int CipherUpdate(ReadOnlySpan<byte> input, Span<byte> output)
        {
            Interop.Crypto.EvpCipherUpdate(
                _ctx,
                output,
                out int bytesWritten,
                input);

            return bytesWritten;
        }

        [MemberNotNull(nameof(_ctx))]
        private void OpenKey(IntPtr algorithm, byte[] key, int effectiveKeyLength)
        {
            _ctx = Interop.Crypto.EvpCipherCreate(
                algorithm,
                ref MemoryMarshal.GetReference(key.AsSpan()),
                key.Length * 8,
                effectiveKeyLength,
                ref MemoryMarshal.GetReference(IV.AsSpan()),
                _encrypting ? 1 : 0);

            Interop.Crypto.CheckValidOpenSslHandle(_ctx);

            // OpenSSL will happily do PKCS#7 padding for us, but since we support padding modes
            // that it doesn't (PaddingMode.Zeros) we'll just always pad the blocks ourselves.
            CheckBoolReturn(Interop.Crypto.EvpCipherCtxSetPadding(_ctx, 0));
        }

        private void Reset()
        {
            bool status = Interop.Crypto.EvpCipherReset(_ctx);

            CheckBoolReturn(status);
        }

        private static void CheckBoolReturn(bool returnValue)
        {
            if (!returnValue)
            {
                throw Interop.Crypto.CreateOpenSslCryptographicException();
            }
        }
    }
}
