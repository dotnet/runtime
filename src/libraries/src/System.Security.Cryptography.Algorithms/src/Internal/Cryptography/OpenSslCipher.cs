// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Buffers;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Microsoft.Win32.SafeHandles;

namespace Internal.Cryptography
{
    internal class OpenSslCipher : BasicSymmetricCipher
    {
        private readonly bool _encrypting;
        private SafeEvpCipherCtxHandle _ctx;

        public OpenSslCipher(IntPtr algorithm, CipherMode cipherMode, int blockSizeInBytes, byte[] key, int effectiveKeyLength, byte[] iv, bool encrypting)
            : base(cipherMode.GetCipherIv(iv), blockSizeInBytes)
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
                    _ctx = null;
                }
            }

            base.Dispose(disposing);
        }

        public override unsafe int Transform(byte[] input, int inputOffset, int count, byte[] output, int outputOffset)
        {
            Debug.Assert(input != null);
            Debug.Assert(inputOffset >= 0);
            Debug.Assert(count > 0);
            Debug.Assert((count % BlockSizeInBytes) == 0);
            Debug.Assert(input.Length - inputOffset >= count);
            Debug.Assert(output != null);
            Debug.Assert(outputOffset >= 0);
            Debug.Assert(output.Length - outputOffset >= count);

            // OpenSSL 1.1 does not allow partial overlap.
            if (input == output && inputOffset != outputOffset)
            {
                byte[] tmp = ArrayPool<byte>.Shared.Rent(count);

                try
                {
                    int written = CipherUpdate(input, inputOffset, count, tmp, 0);
                    Buffer.BlockCopy(tmp, 0, output, outputOffset, written);
                    return written;
                }
                finally
                {
                    CryptographicOperations.ZeroMemory(tmp.AsSpan(0, count));
                    ArrayPool<byte>.Shared.Return(tmp);
                }
            }

            return CipherUpdate(input, inputOffset, count, output, outputOffset);
        }

        public override byte[] TransformFinal(byte[] input, int inputOffset, int count)
        {
            Debug.Assert(input != null);
            Debug.Assert(inputOffset >= 0);
            Debug.Assert(count >= 0);
            Debug.Assert((count % BlockSizeInBytes) == 0);
            Debug.Assert(input.Length - inputOffset >= count);

            byte[] output = ProcessFinalBlock(input, inputOffset, count);
            Reset();
            return output;
        }

        private byte[] ProcessFinalBlock(byte[] input, int inputOffset, int count)
        {
            byte[] output = new byte[count];
            int outputBytes = CipherUpdate(input, inputOffset, count, output, 0);

            Span<byte> outputSpan = output.AsSpan(outputBytes);
            int bytesWritten;
            CheckBoolReturn(Interop.Crypto.EvpCipherFinalEx(_ctx, outputSpan, out bytesWritten));
            outputBytes += bytesWritten;

            if (outputBytes == output.Length)
            {
                return output;
            }

            if (outputBytes == 0)
            {
                return Array.Empty<byte>();
            }

            byte[] userData = new byte[outputBytes];
            Buffer.BlockCopy(output, 0, userData, 0, outputBytes);
            return userData;
        }

        private int CipherUpdate(byte[] input, int inputOffset, int count, byte[] output, int outputOffset)
        {
            int bytesWritten;

            ReadOnlySpan<byte> inputSpan = input.AsSpan(inputOffset, count);
            Span<byte> outputSpan = output.AsSpan(outputOffset);

            Interop.Crypto.EvpCipherUpdate(
                _ctx,
                outputSpan,
                out bytesWritten,
                inputSpan);

            return bytesWritten;
        }

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
