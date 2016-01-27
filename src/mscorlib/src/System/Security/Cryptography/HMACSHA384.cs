// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Security.Cryptography {
    [System.Runtime.InteropServices.ComVisible(true)]
    public class HMACSHA384 : HMAC {

        private bool m_useLegacyBlockSize = Utils._ProduceLegacyHmacValues();

        //
        // public constructors
        //

        public HMACSHA384 () : this (Utils.GenerateRandom(128)) {}

        [System.Security.SecuritySafeCritical]  // auto-generated
        public HMACSHA384 (byte[] key) {
            m_hashName = "SHA384";
            m_hash1 = GetHashAlgorithmWithFipsFallback(() => new SHA384Managed(), () => HashAlgorithm.Create("System.Security.Cryptography.SHA384CryptoServiceProvider"));
            m_hash2 = GetHashAlgorithmWithFipsFallback(() => new SHA384Managed(), () => HashAlgorithm.Create("System.Security.Cryptography.SHA384CryptoServiceProvider"));
            HashSizeValue = 384;
            BlockSizeValue = BlockSize;
            base.InitializeKey(key);
        }

        private int BlockSize {
            get { return m_useLegacyBlockSize ? 64 : 128; }
        }

        // See code:System.Security.Cryptography.HMACSHA512.ProduceLegacyHmacValues
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
