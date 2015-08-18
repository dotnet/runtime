// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Runtime.InteropServices;

using Microsoft.Win32.SafeHandles;

using ErrorCode = Interop.NCrypt.ErrorCode;
using AsymmetricPaddingMode = Interop.NCrypt.AsymmetricPaddingMode;

namespace Internal.Cryptography
{
    //
    // Common infrastructure for AsymmetricAlgorithm-derived classes that accept CngKeys.
    //
    internal struct CngAsymmetricAlgorithmCore
    {
        public static CngKey Duplicate(CngKey key)
        {
            return CngKey.Open(key.Handle, key.IsEphemeral ? CngKeyHandleOpenOptions.EphemeralKey : CngKeyHandleOpenOptions.None);
        }

        public CngKey GetOrGenerateKey(int keySize, CngAlgorithm algorithm)
        {
            // If our key size was changed from the key we're using, we need to generate a new key.
            if (_lazyKey != null && _lazyKey.KeySize != keySize)
            {
                _lazyKey.Dispose();
                _lazyKey = null;
            }

            // If we don't have a key yet, we need to generate a random one now.
            if (_lazyKey == null)
            {
                CngKeyCreationParameters creationParameters = new CngKeyCreationParameters()
                {
                    ExportPolicy = CngExportPolicies.AllowPlaintextExport,
                };

                CngProperty keySizeProperty = new CngProperty(KeyPropertyName.Length, BitConverter.GetBytes(keySize), CngPropertyOptions.None);
                creationParameters.Parameters.Add(keySizeProperty);
                _lazyKey = CngKey.Create(algorithm, null, creationParameters);
            }

            return _lazyKey;
        }

        public void SetKey(CngKey key)
        {
            Debug.Assert(key != null);

            // If we already have a key, clear it out.
            if (_lazyKey != null)
            {
                _lazyKey.Dispose();
            }

            _lazyKey = key;
        }

        public static byte[] HashData(byte[] data, int offset, int count, HashAlgorithmName hashAlgorithm)
        {
            // The classes that call us are sealed and their base class has checked this already.
            Debug.Assert(data != null);
            Debug.Assert(offset >= 0 && offset <= data.Length);
            Debug.Assert(count >= 0 && count <= data.Length);
            Debug.Assert(!string.IsNullOrEmpty(hashAlgorithm.Name));

            using (HashProviderCng hashProvider = new HashProviderCng(hashAlgorithm.Name, null))
            {
                hashProvider.AppendHashData(data, offset, count);
                byte[] hash = hashProvider.FinalizeHashAndReset();
                return hash;
            }
        }

        public static byte[] HashData(Stream data, HashAlgorithmName hashAlgorithm)
        {
            // The classes that call us are sealed and their base class has checked this already.
            Debug.Assert(data != null);
            Debug.Assert(!string.IsNullOrEmpty(hashAlgorithm.Name));

            using (HashProviderCng hashProvider = new HashProviderCng(hashAlgorithm.Name, null))
            {
                // Default the buffer size to 4K.
                byte[] buffer = new byte[4096];
                int bytesRead;
                while ((bytesRead = data.Read(buffer, 0, buffer.Length)) > 0)
                {
                    hashProvider.AppendHashData(buffer, 0, bytesRead);
                }
                byte[] hash = hashProvider.FinalizeHashAndReset();
                return hash;
            }
        }

        public static unsafe byte[] SignHash(CngKey key, byte[] hash, AsymmetricPaddingMode paddingMode, void* pPaddingInfo, int estimatedSize)
        {
#if DEBUG
            estimatedSize = 2;  // Make sure the NTE_BUFFER_TOO_SMALL scenario gets exercised.
#endif
            SafeNCryptKeyHandle keyHandle = key.Handle;

            byte[] signature = new byte[estimatedSize];
            int numBytesNeeded;
            ErrorCode errorCode = Interop.NCrypt.NCryptSignHash(keyHandle, pPaddingInfo, hash, hash.Length, signature, signature.Length, out numBytesNeeded, paddingMode);
            if (errorCode == ErrorCode.NTE_BUFFER_TOO_SMALL)
            {
                signature = new byte[numBytesNeeded];
                errorCode = Interop.NCrypt.NCryptSignHash(keyHandle, pPaddingInfo, hash, hash.Length, signature, signature.Length, out numBytesNeeded, paddingMode);
            }
            if (errorCode != ErrorCode.ERROR_SUCCESS)
                throw errorCode.ToCryptographicException();

            Array.Resize(ref signature, numBytesNeeded);
            return signature;
        }

        public static unsafe bool VerifyHash(CngKey key, byte[] hash, byte[] signature, AsymmetricPaddingMode paddingMode, void* pPaddingInfo)
        {
            SafeNCryptKeyHandle keyHandle = key.Handle;
            ErrorCode errorCode = Interop.NCrypt.NCryptVerifySignature(keyHandle, pPaddingInfo, hash, hash.Length, signature, signature.Length, paddingMode);
            bool verified = (errorCode == ErrorCode.ERROR_SUCCESS);  // For consistency with other AsymmetricAlgorithm-derived classes, return "false" for any error code rather than making the caller catch an exception.
            return verified;
        }

        public void Dispose()
        {
            if (_lazyKey != null)
            {
                _lazyKey.Dispose();
            }
        }

        private CngKey _lazyKey;
    }
}


