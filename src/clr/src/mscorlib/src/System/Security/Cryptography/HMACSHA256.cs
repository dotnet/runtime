// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Security.Cryptography {
    [System.Runtime.InteropServices.ComVisible(true)]
    public class HMACSHA256 : HMAC {
        //
        // public constructors
        //

        public HMACSHA256 () : this (Utils.GenerateRandom(64)) {}

        public HMACSHA256 (byte[] key) {
            m_hashName = "SHA256";

#if FEATURE_CRYPTO
            m_hash1 = GetHashAlgorithmWithFipsFallback(() => new SHA256Managed(), () => HashAlgorithm.Create("System.Security.Cryptography.SHA256CryptoServiceProvider"));
            m_hash2 = GetHashAlgorithmWithFipsFallback(() => new SHA256Managed(), () => HashAlgorithm.Create("System.Security.Cryptography.SHA256CryptoServiceProvider"));
#else
            m_hash1 = new SHA256Managed();
            m_hash2 = new SHA256Managed();
#endif // FEATURE_CRYPTO

            HashSizeValue = 256;
            base.InitializeKey(key);
        }
    }
}
