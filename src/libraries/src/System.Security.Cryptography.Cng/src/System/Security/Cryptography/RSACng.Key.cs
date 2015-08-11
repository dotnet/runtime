// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;

using Internal.Cryptography;

namespace System.Security.Cryptography
{
    public sealed partial class RSACng : RSA
    {
        /// <summary>
        ///     Gets the key that will be used by the RSA object for any cryptographic operation that it uses.
        ///     This key object will be disposed if the key is reset, for instance by changing the KeySize
        ///     property, using ImportParamers to create a new key, or by Disposing of the parent RSA object.
        ///     Therefore, you should make sure that the key object is no longer used in these scenarios. This
        ///     object will not be the same object as the CngKey passed to the RSACng constructor if that
        ///     constructor was used, however it will point at the same CNG key.
        /// </summary>
        public CngKey Key
        {
            get
            {
                // If our key size was changed from the key we're using, we need to generate a new key.
                if (_lazyKey != null && _lazyKey.KeySize != KeySize)
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

                    CngProperty keySizeProperty = new CngProperty(KeyPropertyName.Length, BitConverter.GetBytes(KeySize), CngPropertyOptions.None);
                    creationParameters.Parameters.Add(keySizeProperty);
                    _lazyKey = CngKey.Create(CngAlgorithm.Rsa, null, creationParameters);
                }

                return _lazyKey;
            }

            private set
            {
                Debug.Assert(value != null, "value != null");
                if (value.AlgorithmGroup != CngAlgorithmGroup.Rsa)
                    throw new ArgumentException(SR.Cryptography_ArgRSAaRequiresRSAKey, "value");

                // If we already have a key, clear it out.
                if (_lazyKey != null)
                {
                    _lazyKey.Dispose();
                }

                _lazyKey = value;
                KeySize = _lazyKey.KeySize;
            }
        }
    }
}

