// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using Internal.Cryptography;
using Internal.NativeCrypto;
using Microsoft.Win32.SafeHandles;
using AsymmetricPaddingMode = Interop.NCrypt.AsymmetricPaddingMode;
using ErrorCode = Interop.NCrypt.ErrorCode;

namespace System.Security.Cryptography
{
    internal sealed class BasicSymmetricCipherLiteNCrypt : ILiteSymmetricCipher
    {
        private static readonly CngProperty s_ECBMode =
            new CngProperty(KeyPropertyName.ChainingMode, Encoding.Unicode.GetBytes(Cng.BCRYPT_CHAIN_MODE_ECB + "\0"), CngPropertyOptions.None);
        private static readonly CngProperty s_CBCMode =
            new CngProperty(KeyPropertyName.ChainingMode, Encoding.Unicode.GetBytes(Cng.BCRYPT_CHAIN_MODE_CBC + "\0"), CngPropertyOptions.None);
        private static readonly CngProperty s_CFBMode =
            new CngProperty(KeyPropertyName.ChainingMode, Encoding.Unicode.GetBytes(Cng.BCRYPT_CHAIN_MODE_CFB + "\0"), CngPropertyOptions.None);

        private readonly bool _encrypting;
        private CngKey _key;

        public int BlockSizeInBytes { get; }
        public int PaddingSizeInBytes { get; }

        public BasicSymmetricCipherLiteNCrypt(
            Func<CngKey> cngKeyFactory,
            CipherMode cipherMode,
            int blockSizeInBytes,
            ReadOnlySpan<byte> iv,
            bool encrypting,
            int paddingSizeInBytes)
        {
            BlockSizeInBytes = blockSizeInBytes;
            PaddingSizeInBytes = paddingSizeInBytes;
            _encrypting = encrypting;
            _key = cngKeyFactory();
            CngProperty chainingModeProperty = cipherMode switch
            {
                CipherMode.ECB => s_ECBMode,
                CipherMode.CBC => s_CBCMode,
                CipherMode.CFB => s_CFBMode,
                _ => throw new CryptographicException(SR.Cryptography_InvalidCipherMode),
            };
            _key.SetProperty(chainingModeProperty);

            Reset(iv);
        }

        public int Transform(ReadOnlySpan<byte> input, Span<byte> output)
        {
            Debug.Assert(input.Length > 0);
            Debug.Assert((input.Length % PaddingSizeInBytes) == 0);

            int numBytesWritten = 0;

            // NCryptEncrypt and NCryptDecrypt can do in place encryption, but if the buffers overlap
            // the offset must be zero. In that case, we need to copy to a temporary location.
            if (input.Overlaps(output, out int offset) && offset != 0)
            {
                byte[] rented = CryptoPool.Rent(output.Length);

                try
                {
                    numBytesWritten = NCryptTransform(input, rented);
                    rented.AsSpan(0, numBytesWritten).CopyTo(output);
                }
                finally
                {
                    CryptoPool.Return(rented, clearSize: numBytesWritten);
                }
            }
            else
            {
                numBytesWritten = NCryptTransform(input, output);
            }

            if (numBytesWritten != input.Length)
            {
                // CNG gives us no way to tell NCryptDecrypt() that we're decrypting the final block, nor is it performing any
                // padding /depadding for us. So there's no excuse for a provider to hold back output for "future calls." Though
                // this isn't technically our problem to detect, we might as well detect it now for easier diagnosis.
                throw new CryptographicException(SR.Cryptography_UnexpectedTransformTruncation);
            }

            return numBytesWritten;

            int NCryptTransform(ReadOnlySpan<byte> input, Span<byte> output)
            {
                int bytesWritten;

                // The Handle property duplicates the handle.
                using (SafeNCryptKeyHandle keyHandle = _key.Handle)
                {
                    unsafe
                    {
                        ErrorCode errorCode = _encrypting ?
                            Interop.NCrypt.NCryptEncrypt(keyHandle, input, input.Length, null, output, output.Length, out bytesWritten, AsymmetricPaddingMode.None) :
                            Interop.NCrypt.NCryptDecrypt(keyHandle, input, input.Length, null, output, output.Length, out bytesWritten, AsymmetricPaddingMode.None);

                        if (errorCode != ErrorCode.ERROR_SUCCESS)
                        {
                            throw errorCode.ToCryptographicException();
                        }
                    }
                }

                return bytesWritten;
            }
        }

        public unsafe void Reset(ReadOnlySpan<byte> iv)
        {
            if (!iv.IsEmpty)
            {
                fixed (byte* pIv = &MemoryMarshal.GetReference(iv))
                {
                    // The Handle property duplicates the handle.
                    using (SafeNCryptKeyHandle keyHandle = _key.Handle)
                    {
                        ErrorCode errorCode = Interop.NCrypt.NCryptSetProperty(
                            keyHandle,
                            KeyPropertyName.InitializationVector,
                            pIv,
                            iv.Length,
                            CngPropertyOptions.None);

                        if (errorCode != ErrorCode.ERROR_SUCCESS)
                        {
                            throw errorCode.ToCryptographicException();
                        }
                    }
                }
            }
        }

        public int TransformFinal(ReadOnlySpan<byte> input, Span<byte> output)
        {
            Debug.Assert((input.Length % PaddingSizeInBytes) == 0);

            int numBytesWritten = 0;

            if (input.Length != 0)
            {
                numBytesWritten = Transform(input, output);
                Debug.Assert(numBytesWritten == input.Length); // Our implementation of Transform() guarantees this. See comment above.
            }

            return numBytesWritten;
        }

        public void Dispose()
        {
            _key?.Dispose();
            _key = null!;
        }

    }
}
