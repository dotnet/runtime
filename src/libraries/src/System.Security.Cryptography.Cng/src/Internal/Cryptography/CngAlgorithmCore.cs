// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Security.Cryptography;

namespace Internal.Cryptography
{
    //
    // Common infrastructure for crypto algorithms that accept CngKeys.
    //
    internal struct CngAlgorithmCore
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

