// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Security.Cryptography {
    [System.Runtime.InteropServices.ComVisible(true)]
    public class HMACSHA512 : HMAC {

        private bool m_useLegacyBlockSize = Utils._ProduceLegacyHmacValues();

        //
        // public constructors
        //

        public HMACSHA512 () : this (Utils.GenerateRandom(128)) {}

        [System.Security.SecuritySafeCritical]  // auto-generated
        public HMACSHA512 (byte[] key) {
            m_hashName = "SHA512";
            m_hash1 = GetHashAlgorithmWithFipsFallback(() => new SHA512Managed(), () => HashAlgorithm.Create("System.Security.Cryptography.SHA512CryptoServiceProvider"));
            m_hash2 = GetHashAlgorithmWithFipsFallback(() => new SHA512Managed(), () => HashAlgorithm.Create("System.Security.Cryptography.SHA512CryptoServiceProvider"));
            HashSizeValue = 512;
            BlockSizeValue = BlockSize;
            base.InitializeKey(key);
        }

        private int BlockSize {
            get { return m_useLegacyBlockSize ? 64 : 128; }
        }

        /// <summary>
        ///     In Whidbey we incorrectly used a block size of 64 bytes for HMAC-SHA-384 and HMAC-SHA-512,
        ///     rather than using the correct value of 128 bytes.  Setting this to true causes us to fall
        ///     back to the Whidbey mode which produces incorrect HMAC values.
        ///     
        ///     This value should be set only once, before hashing has begun, since we need to reset the key
        ///     buffer for the block size change to take effect.
        ///     
        ///     The default vaue is off, however this can be toggled for the application by setting the
        ///     legacyHMACMode config switch.
        /// </summary>
        public bool ProduceLegacyHmacValues {
            get { return m_useLegacyBlockSize; }

            set {
                m_useLegacyBlockSize = value;

                BlockSizeValue = BlockSize;
                InitializeKey(KeyValue);
            }
        }
    }
}
