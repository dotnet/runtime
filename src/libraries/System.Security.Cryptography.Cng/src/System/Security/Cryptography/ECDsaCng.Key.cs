// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;

using Internal.Cryptography;

namespace System.Security.Cryptography
{
    public sealed partial class ECDsaCng : ECDsa
    {
        /// <summary>
        ///     Gets the key that will be used by the ECDsa object for any cryptographic operation that it uses.
        ///     This key object will be disposed if the key is reset, for instance by changing the KeySize
        ///     property, using ImportParamers to create a new key, or by Disposing of the parent ECDsa object.
        ///     Therefore, you should make sure that the key object is no longer used in these scenarios. This
        ///     object will not be the same object as the CngKey passed to the ECDsaCng constructor if that
        ///     constructor was used, however it will point at the same CNG key.
        /// </summary>
        public CngKey Key
        {
            get
            {
                int keySize = KeySize;
                // Map the current key size to a CNG algorithm name
                CngAlgorithm algorithm = null;
                switch (keySize)
                {
                    case 256: algorithm = CngAlgorithm.ECDsaP256; break;
                    case 384: algorithm = CngAlgorithm.ECDsaP384; break;
                    case 521: algorithm = CngAlgorithm.ECDsaP521; break;
                    default:
                        Debug.Assert(false, "Illegal key size set");
                        break;
                }

                CngKey key = _core.GetOrGenerateKey(keySize, algorithm);
                return key;
            }

            private set
            {
                CngKey key = value;
                Debug.Assert(key != null, "key != null");
                if (!IsEccAlgorithmGroup(key.AlgorithmGroup))
                    throw new ArgumentException(SR.Cryptography_ArgECDsaRequiresECDsaKey, "value");
                _core.SetKey(key);
                KeySize = key.KeySize;
            }
        }
    }
}

