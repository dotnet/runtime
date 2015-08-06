// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.IO;

using Microsoft.Win32.SafeHandles;

using Internal.Cryptography;

using ErrorCode = Interop.NCrypt.ErrorCode;
using AsymmetricPaddingMode = Interop.NCrypt.AsymmetricPaddingMode;
using BCRYPT_OAEP_PADDING_INFO = Interop.BCrypt.BCRYPT_OAEP_PADDING_INFO;

namespace System.Security.Cryptography
{
    public sealed partial class RSACng : RSA
    {
        /// <summary>
        ///     Encrypts data using the public key.
        /// </summary>
        public override byte[] Encrypt(byte[] data, RSAEncryptionPadding padding)
        {
            unsafe
            {
                return EncryptOrDecrypt(data, padding, Interop.NCrypt.NCryptEncrypt);
            }
        }

        /// <summary>
        ///     Decrypts data using the private key.
        /// </summary>
        public override byte[] Decrypt(byte[] data, RSAEncryptionPadding padding)
        {
            unsafe
            {
                return EncryptOrDecrypt(data, padding, Interop.NCrypt.NCryptDecrypt);
            }
        }

        //
        // Conveniently, Encrypt() and Decrypt() are identical save for the actual P/Invoke call to CNG. Thus, both
        // api's invoke this common helper with the "transform" parameter determining whether encryption or decryption is done.
        //
        private byte[] EncryptOrDecrypt(byte[] data, RSAEncryptionPadding padding, EncryptOrDecryptAction encryptOrDecrypt)
        {
            if (data == null)
                throw new ArgumentNullException("data");

            if (padding == null)
                throw new ArgumentNullException("padding");

            unsafe
            {
                SafeNCryptKeyHandle keyHandle = Key.Handle;
                switch (padding.Mode)
                {
                    case RSAEncryptionPaddingMode.Pkcs1:
                        return EncryptOrDecrypt(keyHandle, data, AsymmetricPaddingMode.NCRYPT_PAD_PKCS1_FLAG, null, encryptOrDecrypt);

                    case RSAEncryptionPaddingMode.Oaep:
                        {
                            using (SafeUnicodeStringHandle safeHashAlgorithmName = new SafeUnicodeStringHandle(padding.OaepHashAlgorithm.Name))
                            {
                                BCRYPT_OAEP_PADDING_INFO paddingInfo = new BCRYPT_OAEP_PADDING_INFO()
                                {
                                    pszAlgId = safeHashAlgorithmName.DangerousGetHandle(),

                                    // It would nice to put randomized data here but RSAEncryptionPadding does not at this point provide support for this.
                                    pbLabel = IntPtr.Zero,
                                    cbLabel = 0, 
                                };
                                return EncryptOrDecrypt(keyHandle, data, AsymmetricPaddingMode.NCRYPT_PAD_OAEP_FLAG, &paddingInfo, encryptOrDecrypt);
                            }
                        }

                    default:
                        throw new CryptographicException(SR.Cryptography_UnsupportedPaddingMode);
                }
            }
        }

        //
        // Now that the padding mode and information have been marshaled to their native counterparts, perform the encryption or decryption.
        //
        private static unsafe byte[] EncryptOrDecrypt(SafeNCryptKeyHandle key, byte[] input, AsymmetricPaddingMode paddingMode, void* paddingInfo, EncryptOrDecryptAction encryptOrDecrypt)
        {
            int numBytesNeeded;
            ErrorCode errorCode = encryptOrDecrypt(key, input, input.Length, paddingInfo, null, 0, out numBytesNeeded, paddingMode);
            if (errorCode != ErrorCode.ERROR_SUCCESS)
                throw errorCode.ToCryptographicException();

            byte[] output = new byte[numBytesNeeded];
            errorCode = encryptOrDecrypt(key, input, input.Length, paddingInfo, output, numBytesNeeded, out numBytesNeeded, paddingMode);
            if (errorCode != ErrorCode.ERROR_SUCCESS)
                throw errorCode.ToCryptographicException();

            return output;
        }

        // Delegate binds to either NCryptEncrypt() or NCryptDecrypt() depending on which api was called.
        private unsafe delegate ErrorCode EncryptOrDecryptAction(SafeNCryptKeyHandle hKey, byte[] pbInput, int cbInput, void* pPaddingInfo, byte[] pbOutput, int cbOutput, out int pcbResult, AsymmetricPaddingMode dwFlags);
    }
}

