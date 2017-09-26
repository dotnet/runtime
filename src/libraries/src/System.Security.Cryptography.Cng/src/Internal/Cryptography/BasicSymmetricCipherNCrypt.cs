// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
        public BasicSymmetricCipherNCrypt(Func<CngKey> cngKeyFactory, CipherMode cipherMode, int blockSizeInBytes, byte[] iv, bool encrypting)
            : base(iv, blockSizeInBytes)
        {
            _encrypting = encrypting;
            _cngKey = cngKeyFactory();

            CngProperty chainingModeProperty;
            switch (cipherMode)
            {
                case CipherMode.ECB:
                    chainingModeProperty = s_ECBMode;
                    break;
                case CipherMode.CBC:
                    chainingModeProperty = s_CBCMode;
                    break;
                default:
                    throw new CryptographicException(SR.Cryptography_InvalidCipherMode);
            }
            _cngKey.SetProperty(chainingModeProperty);

            Reset();
        }

        public sealed override int Transform(byte[] input, int inputOffset, int count, byte[] output, int outputOffset)
        {
            Debug.Assert(input != null);
            Debug.Assert(inputOffset >= 0);
            Debug.Assert(count > 0);
            Debug.Assert((count % BlockSizeInBytes) == 0);
            Debug.Assert(input.Length - inputOffset >= count);
            Debug.Assert(output != null);
            Debug.Assert(outputOffset >= 0);
            Debug.Assert(output.Length - outputOffset >= count);

            int numBytesWritten;
            ErrorCode errorCode;
            using (SafeNCryptKeyHandle keyHandle = _cngKey.Handle)
            {
                var inputSpan = new ReadOnlySpan<byte>(input, inputOffset, count);
                var outputSpan = new Span<byte>(output, outputOffset, count);
                unsafe
                {
                    errorCode = _encrypting ?
                        Interop.NCrypt.NCryptEncrypt(keyHandle, inputSpan, inputSpan.Length, null, outputSpan, outputSpan.Length, out numBytesWritten, AsymmetricPaddingMode.None) :
                        Interop.NCrypt.NCryptDecrypt(keyHandle, inputSpan, inputSpan.Length, null, outputSpan, outputSpan.Length, out numBytesWritten, AsymmetricPaddingMode.None);
                }
            }
            if (errorCode != ErrorCode.ERROR_SUCCESS)
            {
                throw errorCode.ToCryptographicException();
            }

            if (numBytesWritten != count)
            {
                // CNG gives us no way to tell NCryptDecrypt() that we're decrypting the final block, nor is it performing any padding/depadding for us.
                // So there's no excuse for a provider to hold back output for "future calls." Though this isn't technically our problem to detect, we might as well
                // detect it now for easier diagnosis.
                throw new CryptographicException(SR.Cryptography_UnexpectedTransformTruncation);
            }

            return numBytesWritten;
        }

        public sealed override byte[] TransformFinal(byte[] input, int inputOffset, int count)
        {
            Debug.Assert(input != null);
            Debug.Assert(inputOffset >= 0);
            Debug.Assert(count >= 0);
            Debug.Assert((count % BlockSizeInBytes) == 0);
            Debug.Assert(input.Length - inputOffset >= count);

            byte[] output = new byte[count];
            if (count != 0)
            {
                int numBytesWritten = Transform(input, inputOffset, count, output, 0);
                Debug.Assert(numBytesWritten == count);  // Our implementation of Transform() guarantees this. See comment above.
            }

            Reset();
            return output;
        }

        protected sealed override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_cngKey != null)
                {
                    _cngKey.Dispose();
                    _cngKey = null;
                }
            }

            base.Dispose(disposing);
        }

        private void Reset()
        {
            if (IV != null)
            {
                CngProperty prop = new CngProperty(Interop.NCrypt.NCRYPT_INITIALIZATION_VECTOR, IV, CngPropertyOptions.None);
                _cngKey.SetProperty(prop);
            }
        }

        private static CngProperty CreateCngPropertyForCipherMode(string cipherMode)
        {
            byte[] cipherModeBytes = Encoding.Unicode.GetBytes((cipherMode + "\0").ToCharArray());
            return new CngProperty(Interop.NCrypt.NCRYPT_CHAINING_MODE_PROPERTY, cipherModeBytes, CngPropertyOptions.None);
        }

        private CngKey _cngKey;
        private readonly bool _encrypting;

        private static readonly CngProperty s_ECBMode = CreateCngPropertyForCipherMode(Interop.BCrypt.BCRYPT_CHAIN_MODE_ECB);
        private static readonly CngProperty s_CBCMode = CreateCngPropertyForCipherMode(Interop.BCrypt.BCRYPT_CHAIN_MODE_CBC);
    }
}
