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
                CngKey key = _core.GetOrGenerateKey(KeySize, CngAlgorithm.Rsa);
                return key;
            }

            private set
            {
                CngKey key = value;
                Debug.Assert(key != null, "key != null");
                if (key.AlgorithmGroup != CngAlgorithmGroup.Rsa)
                    throw new ArgumentException(SR.Cryptography_ArgRSAaRequiresRSAKey, "value");
                _core.SetKey(key);
                KeySize = key.KeySize;
            }
        }
    }
}

