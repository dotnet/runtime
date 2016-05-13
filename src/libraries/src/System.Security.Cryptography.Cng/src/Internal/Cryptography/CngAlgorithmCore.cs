// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Security.Cryptography;
using Microsoft.Win32.SafeHandles;

namespace Internal.Cryptography
{
    //
    // Common infrastructure for crypto algorithms that accept CngKeys.
    //
    internal struct CngAlgorithmCore
    {
        public static CngKey Duplicate(CngKey key)
        {
            using (SafeNCryptKeyHandle keyHandle = key.Handle)
            {
                return CngKey.Open(keyHandle, key.IsEphemeral ? CngKeyHandleOpenOptions.EphemeralKey : CngKeyHandleOpenOptions.None);
            }
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

