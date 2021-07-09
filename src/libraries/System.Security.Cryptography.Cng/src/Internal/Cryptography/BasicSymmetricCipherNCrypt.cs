// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Win32.SafeHandles;

using ErrorCode = Interop.NCrypt.ErrorCode;
using AsymmetricPaddingMode = Interop.NCrypt.AsymmetricPaddingMode;

namespace Internal.Cryptography
{
    internal sealed class BasicSymmetricCipherNCrypt : BasicSymmetricCipher
    {
        //
        // The first parameter is a delegate that instantiates a CngKey rather than a CngKey itself. That's because CngKeys are stateful objects
        // and concurrent encryptions on the same CngKey will corrupt each other.
        //
        // The delegate must instantiate a new CngKey, based on a new underlying NCryptKeyHandle, each time is called.
        //
        public BasicSymmetricCipherNCrypt(Func<CngKey> cngKeyFactory, CipherMode cipherMode, int blockSizeInBytes, byte[]? iv, bool encrypting, int feedbackSizeInBytes, int paddingSize)
            : base(iv, blockSizeInBytes, paddingSize)
        {
            _encrypting = encrypting;
            _cngKey = cngKeyFactory();
            CngProperty chainingModeProperty = cipherMode switch
            {
                CipherMode.ECB => s_ECBMode,
                CipherMode.CBC => s_CBCMode,
                CipherMode.CFB => s_CFBMode,
                _ => throw new CryptographicException(SR.Cryptography_InvalidCipherMode),
            };
            _cngKey.SetProperty(chainingModeProperty);

            Reset();
        }

        public sealed override int Transform(ReadOnlySpan<byte> input, Span<byte> output)
        {
            Debug.Assert(input.Length > 0);
            Debug.Assert((input.Length % PaddingSizeInBytes) == 0);

            int numBytesWritten;
            ErrorCode errorCode;
            using (SafeNCryptKeyHandle keyHandle = _cngKey!.Handle)
            {
                unsafe
                {
                    errorCode = _encrypting ?
                        Interop.NCrypt.NCryptEncrypt(keyHandle, input, input.Length, null, output, output.Length, out numBytesWritten, AsymmetricPaddingMode.None) :
                        Interop.NCrypt.NCryptDecrypt(keyHandle, input, input.Length, null, output, output.Length, out numBytesWritten, AsymmetricPaddingMode.None);
                }
            }
            if (errorCode != ErrorCode.ERROR_SUCCESS)
            {
                throw errorCode.ToCryptographicException();
            }

            if (numBytesWritten != input.Length)
            {
                // CNG gives us no way to tell NCryptDecrypt() that we're decrypting the final block, nor is it performing any padding/depadding for us.
                // So there's no excuse for a provider to hold back output for "future calls." Though this isn't technically our problem to detect, we might as well
                // detect it now for easier diagnosis.
                throw new CryptographicException(SR.Cryptography_UnexpectedTransformTruncation);
            }

            return numBytesWritten;
        }

        public sealed override int TransformFinal(ReadOnlySpan<byte> input, Span<byte> output)
        {
            Debug.Assert((input.Length % PaddingSizeInBytes) == 0);

            int numBytesWritten = 0;

            if (input.Length != 0)
            {
                numBytesWritten = Transform(input, output);
                Debug.Assert(numBytesWritten == input.Length);  // Our implementation of Transform() guarantees this. See comment above.
            }

            Reset();
            return numBytesWritten;
        }

        protected sealed override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_cngKey != null)
                {
                    _cngKey.Dispose();
                    _cngKey = null!;
                }
            }

            base.Dispose(disposing);
        }

        private void Reset()
        {
            if (IV != null)
            {
                CngProperty prop = new CngProperty(Interop.NCrypt.NCRYPT_INITIALIZATION_VECTOR, IV, CngPropertyOptions.None);
                _cngKey!.SetProperty(prop);
            }
        }

        private CngKey _cngKey;
        private readonly bool _encrypting;

        private static readonly CngProperty s_ECBMode =
            new CngProperty(Interop.NCrypt.NCRYPT_CHAINING_MODE_PROPERTY, Encoding.Unicode.GetBytes(Interop.BCrypt.BCRYPT_CHAIN_MODE_ECB + "\0"), CngPropertyOptions.None);
        private static readonly CngProperty s_CBCMode =
            new CngProperty(Interop.NCrypt.NCRYPT_CHAINING_MODE_PROPERTY, Encoding.Unicode.GetBytes(Interop.BCrypt.BCRYPT_CHAIN_MODE_CBC + "\0"), CngPropertyOptions.None);
        private static readonly CngProperty s_CFBMode =
            new CngProperty(Interop.NCrypt.NCRYPT_CHAINING_MODE_PROPERTY, Encoding.Unicode.GetBytes(Interop.BCrypt.BCRYPT_CHAIN_MODE_CFB + "\0"), CngPropertyOptions.None);
    }
}
