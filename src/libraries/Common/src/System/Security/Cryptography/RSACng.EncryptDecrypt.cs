// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using Internal.Cryptography;

using ErrorCode = Interop.NCrypt.ErrorCode;
using AsymmetricPaddingMode = Interop.NCrypt.AsymmetricPaddingMode;
using BCRYPT_OAEP_PADDING_INFO = Interop.BCrypt.BCRYPT_OAEP_PADDING_INFO;

namespace System.Security.Cryptography
{
    public sealed partial class RSACng : RSA
    {
        private const int Pkcs1PaddingOverhead = 11;
        private const int StatusUnsuccessfulRetryCount = 1;

        /// <summary>Encrypts data using the public key.</summary>
        public override byte[] Encrypt(byte[] data, RSAEncryptionPadding padding) =>
            EncryptOrDecrypt(data, padding, encrypt: true);

        /// <summary>Decrypts data using the private key.</summary>
        public override byte[] Decrypt(byte[] data, RSAEncryptionPadding padding) =>
            EncryptOrDecrypt(data, padding, encrypt: false);

        /// <summary>Encrypts data using the public key.</summary>
        public override bool TryEncrypt(ReadOnlySpan<byte> data, Span<byte> destination, RSAEncryptionPadding padding, out int bytesWritten) =>
            TryEncryptOrDecrypt(data, destination, padding, encrypt: true, bytesWritten: out bytesWritten);

        /// <summary>Decrypts data using the private key.</summary>
        public override bool TryDecrypt(ReadOnlySpan<byte> data, Span<byte> destination, RSAEncryptionPadding padding, out int bytesWritten) =>
            TryEncryptOrDecrypt(data, destination, padding, encrypt: false, bytesWritten: out bytesWritten);

        // Conveniently, Encrypt() and Decrypt() are identical save for the actual P/Invoke call to CNG. Thus, both
        // array-based APIs invoke this common helper with the "encrypt" parameter determining whether encryption or decryption is done.
        private unsafe byte[] EncryptOrDecrypt(byte[] data, RSAEncryptionPadding padding, bool encrypt)
        {
            ArgumentNullException.ThrowIfNull(data);
            ArgumentNullException.ThrowIfNull(padding);

            int modulusSizeInBytes = RsaPaddingProcessor.BytesRequiredForBitCount(KeySize);

            if (!encrypt && data.Length != modulusSizeInBytes)
            {
                throw new CryptographicException(SR.Cryptography_RSA_DecryptWrongSize);
            }

            if (encrypt &&
                padding.Mode == RSAEncryptionPaddingMode.Pkcs1 &&
                data.Length > modulusSizeInBytes - Pkcs1PaddingOverhead)
            {
                throw new CryptographicException(
                    SR.Format(SR.Cryptography_Encryption_MessageTooLong, modulusSizeInBytes - Pkcs1PaddingOverhead));
            }

            using (SafeNCryptKeyHandle keyHandle = GetDuplicatedKeyHandle())
            {
                if (encrypt && data.Length == 0)
                {
                    byte[] rented = CryptoPool.Rent(modulusSizeInBytes);
                    Span<byte> paddedMessage = new Span<byte>(rented, 0, modulusSizeInBytes);

                    try
                    {
                        if (padding == RSAEncryptionPadding.Pkcs1)
                        {
                            RsaPaddingProcessor.PadPkcs1Encryption(data, paddedMessage);
                        }
                        else if (padding.Mode == RSAEncryptionPaddingMode.Oaep)
                        {
                            RsaPaddingProcessor.PadOaep(padding.OaepHashAlgorithm, data, paddedMessage);
                        }
                        else
                        {
                            throw new CryptographicException(SR.Cryptography_UnsupportedPaddingMode);
                        }

                        return EncryptOrDecrypt(keyHandle, paddedMessage, AsymmetricPaddingMode.NCRYPT_NO_PADDING_FLAG, null, encrypt);
                    }
                    finally
                    {
                        CryptographicOperations.ZeroMemory(paddedMessage);
                        CryptoPool.Return(rented, clearSize: 0);
                    }
                }

                switch (padding.Mode)
                {
                    case RSAEncryptionPaddingMode.Pkcs1:
                        return EncryptOrDecrypt(keyHandle, data, AsymmetricPaddingMode.NCRYPT_PAD_PKCS1_FLAG, null, encrypt);

                    case RSAEncryptionPaddingMode.Oaep:
                        IntPtr namePtr = Marshal.StringToHGlobalUni(padding.OaepHashAlgorithm.Name);
                        try
                        {
                            var paddingInfo = new BCRYPT_OAEP_PADDING_INFO()
                            {
                                pszAlgId = namePtr,

                                // It would nice to put randomized data here but RSAEncryptionPadding does not at this point provide support for this.
                                pbLabel = IntPtr.Zero,
                                cbLabel = 0,
                            };
                            return EncryptOrDecrypt(keyHandle, data, AsymmetricPaddingMode.NCRYPT_PAD_OAEP_FLAG, &paddingInfo, encrypt);
                        }
                        finally
                        {
                            Marshal.FreeHGlobal(namePtr);
                        }

                    default:
                        throw new CryptographicException(SR.Cryptography_UnsupportedPaddingMode);
                }
            }
        }

        // Conveniently, Encrypt() and Decrypt() are identical save for the actual P/Invoke call to CNG. Thus, both
        // span-based APIs invoke this common helper with the "encrypt" parameter determining whether encryption or decryption is done.
        private unsafe bool TryEncryptOrDecrypt(ReadOnlySpan<byte> data, Span<byte> destination, RSAEncryptionPadding padding, bool encrypt, out int bytesWritten)
        {
            ArgumentNullException.ThrowIfNull(padding);

            int modulusSizeInBytes = RsaPaddingProcessor.BytesRequiredForBitCount(KeySize);

            if (!encrypt && data.Length != modulusSizeInBytes)
            {
                throw new CryptographicException(SR.Cryptography_RSA_DecryptWrongSize);
            }

            if (encrypt &&
                padding.Mode == RSAEncryptionPaddingMode.Pkcs1 &&
                data.Length > modulusSizeInBytes - Pkcs1PaddingOverhead)
            {
                throw new CryptographicException(
                    SR.Format(SR.Cryptography_Encryption_MessageTooLong, modulusSizeInBytes - Pkcs1PaddingOverhead));
            }

            using (SafeNCryptKeyHandle keyHandle = GetDuplicatedKeyHandle())
            {
                if (encrypt && data.Length == 0)
                {
                    byte[] rented = CryptoPool.Rent(modulusSizeInBytes);
                    Span<byte> paddedMessage = new Span<byte>(rented, 0, modulusSizeInBytes);

                    try
                    {
                        if (padding == RSAEncryptionPadding.Pkcs1)
                        {
                            RsaPaddingProcessor.PadPkcs1Encryption(data, paddedMessage);
                        }
                        else if (padding.Mode == RSAEncryptionPaddingMode.Oaep)
                        {
                            RsaPaddingProcessor.PadOaep(padding.OaepHashAlgorithm, data, paddedMessage);
                        }
                        else
                        {
                            throw new CryptographicException(SR.Cryptography_UnsupportedPaddingMode);
                        }

                        return TryEncryptOrDecrypt(keyHandle, paddedMessage, destination, AsymmetricPaddingMode.NCRYPT_NO_PADDING_FLAG, null, encrypt, out bytesWritten);
                    }
                    finally
                    {
                        CryptographicOperations.ZeroMemory(paddedMessage);
                        CryptoPool.Return(rented, clearSize: 0);
                    }
                }

                switch (padding.Mode)
                {
                    case RSAEncryptionPaddingMode.Pkcs1:
                        return TryEncryptOrDecrypt(keyHandle, data, destination, AsymmetricPaddingMode.NCRYPT_PAD_PKCS1_FLAG, null, encrypt, out bytesWritten);

                    case RSAEncryptionPaddingMode.Oaep:
                        IntPtr namePtr = Marshal.StringToHGlobalUni(padding.OaepHashAlgorithm.Name);
                        try
                        {
                            var paddingInfo = new BCRYPT_OAEP_PADDING_INFO()
                            {
                                pszAlgId = namePtr,
                                pbLabel = IntPtr.Zero, // It would nice to put randomized data here but RSAEncryptionPadding does not at this point provide support for this.
                                cbLabel = 0,
                            };
                            return TryEncryptOrDecrypt(keyHandle, data, destination, AsymmetricPaddingMode.NCRYPT_PAD_OAEP_FLAG, &paddingInfo, encrypt, out bytesWritten);
                        }
                        finally
                        {
                            Marshal.FreeHGlobal(namePtr);
                        }

                    default:
                        throw new CryptographicException(SR.Cryptography_UnsupportedPaddingMode);
                }
            }
        }

        // Now that the padding mode and information have been marshaled to their native counterparts, perform the encryption or decryption.
        private unsafe byte[] EncryptOrDecrypt(SafeNCryptKeyHandle key, ReadOnlySpan<byte> input, AsymmetricPaddingMode paddingMode, void* paddingInfo, bool encrypt)
        {
            int estimatedSize = KeySize / 8;
#if DEBUG
            estimatedSize = 2;  // Make sure the NTE_BUFFER_TOO_SMALL scenario gets exercised.
#endif

            byte[] output = new byte[estimatedSize];
            int numBytesNeeded = 0;

            ErrorCode errorCode = 0;

            for (int i = 0; i <= StatusUnsuccessfulRetryCount; i++)
            {
                errorCode =
                    EncryptOrDecrypt(key, input, output, paddingMode, paddingInfo, encrypt, out numBytesNeeded);

                if (errorCode != ErrorCode.STATUS_UNSUCCESSFUL)
                {
                    break;
                }
            }

            if (errorCode == ErrorCode.NTE_BUFFER_TOO_SMALL)
            {
                CryptographicOperations.ZeroMemory(output);
                output = new byte[numBytesNeeded];

                for (int i = 0; i <= StatusUnsuccessfulRetryCount; i++)
                {
                    errorCode =
                        EncryptOrDecrypt(key, input, output, paddingMode, paddingInfo, encrypt, out numBytesNeeded);

                    if (errorCode != ErrorCode.STATUS_UNSUCCESSFUL)
                    {
                        break;
                    }
                }
            }

            if (errorCode != ErrorCode.ERROR_SUCCESS)
            {
                throw errorCode.ToCryptographicException();
            }

            if (numBytesNeeded != output.Length)
            {
                byte[] ret = output.AsSpan(0, numBytesNeeded).ToArray();
                CryptographicOperations.ZeroMemory(output);
                output = ret;
            }

            return output;
        }

        // Now that the padding mode and information have been marshaled to their native counterparts, perform the encryption or decryption.
        private static unsafe bool TryEncryptOrDecrypt(SafeNCryptKeyHandle key, ReadOnlySpan<byte> input, Span<byte> output, AsymmetricPaddingMode paddingMode, void* paddingInfo, bool encrypt, out int bytesWritten)
        {
            for (int i = 0; i <= StatusUnsuccessfulRetryCount; i++)
            {
                int numBytesNeeded;
                ErrorCode errorCode =
                    EncryptOrDecrypt(key, input, output, paddingMode, paddingInfo, encrypt, out numBytesNeeded);

                switch (errorCode)
                {
                    case ErrorCode.ERROR_SUCCESS:
                        bytesWritten = numBytesNeeded;
                        return true;
                    case ErrorCode.NTE_BUFFER_TOO_SMALL:
                        bytesWritten = 0;
                        return false;
                    case ErrorCode.STATUS_UNSUCCESSFUL:
                        break;
                    default:
                        throw errorCode.ToCryptographicException();
                }
            }

            throw ErrorCode.STATUS_UNSUCCESSFUL.ToCryptographicException();
        }

        private static unsafe ErrorCode EncryptOrDecrypt(
            SafeNCryptKeyHandle key,
            ReadOnlySpan<byte> input,
            Span<byte> output,
            AsymmetricPaddingMode paddingMode,
            void* paddingInfo,
            bool encrypt,
            out int bytesNeeded)
        {
            ErrorCode errorCode = encrypt ?
                Interop.NCrypt.NCryptEncrypt(key, input, input.Length, paddingInfo, output, output.Length, out bytesNeeded, paddingMode) :
                Interop.NCrypt.NCryptDecrypt(key, input, input.Length, paddingInfo, output, output.Length, out bytesNeeded, paddingMode);

            // Windows 10.1903 can return success when it meant NTE_BUFFER_TOO_SMALL.
            if (errorCode == ErrorCode.ERROR_SUCCESS && bytesNeeded > output.Length)
            {
                errorCode = ErrorCode.NTE_BUFFER_TOO_SMALL;
            }

            return errorCode;
        }
    }
}
