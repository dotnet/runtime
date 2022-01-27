// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Internal.Cryptography;
using Microsoft.Win32.SafeHandles;

namespace System.Security.Cryptography
{
    internal sealed class OpenSslCipherLite : ILiteSymmetricCipher
    {
        private readonly SafeEvpCipherCtxHandle _ctx;

#if DEBUG
        private bool _isFinalized;
#endif

        public int BlockSizeInBytes { get; }
        public int PaddingSizeInBytes { get; }

        public OpenSslCipherLite(
            IntPtr algorithm,
            CipherMode cipherMode,
            int blockSizeInBytes,
            int paddingSizeInBytes,
            ReadOnlySpan<byte> key,
            ReadOnlySpan<byte> iv,
            bool encrypting)
        {
            Debug.Assert(algorithm != IntPtr.Zero);

            BlockSizeInBytes = blockSizeInBytes;
            PaddingSizeInBytes = paddingSizeInBytes;
            _ctx = Interop.Crypto.EvpCipherCreate(
                algorithm,
                ref MemoryMarshal.GetReference(key),
                key.Length * 8,
                ref MemoryMarshal.GetReference(iv),
                encrypting ? 1 : 0);

            Interop.Crypto.CheckValidOpenSslHandle(_ctx);

            // OpenSSL will happily do PKCS#7 padding for us, but since we support padding modes
            // that it doesn't (PaddingMode.Zeros) we'll just always pad the blocks ourselves.
            CheckBoolReturn(Interop.Crypto.EvpCipherCtxSetPadding(_ctx, 0));
        }

        public int TransformFinal(ReadOnlySpan<byte> input, Span<byte> output)
        {
#if DEBUG
            if (_isFinalized)
            {
                Debug.Fail("Cipher was reused without being reset.");
                throw new CryptographicException();
            }

            _isFinalized = true;
#endif

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

        public unsafe int Transform(ReadOnlySpan<byte> input, Span<byte> output)
        {
#if DEBUG
            if (_isFinalized)
            {
                Debug.Fail("Cipher was reused without being reset.");
                throw new CryptographicException();
            }
#endif

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

        public void Reset(ReadOnlySpan<byte> iv)
        {
            bool status = Interop.Crypto.EvpCipherReset(_ctx);
            CheckBoolReturn(status);

#if DEBUG
            _isFinalized = false;
#endif
        }

        public void Dispose()
        {
            _ctx.Dispose();
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

        private static void CheckBoolReturn(bool returnValue)
        {
            if (!returnValue)
            {
                throw Interop.Crypto.CreateOpenSslCryptographicException();
            }
        }
    }
}
