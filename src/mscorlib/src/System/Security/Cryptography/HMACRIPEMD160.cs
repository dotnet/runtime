// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Security.Cryptography {
    [System.Runtime.InteropServices.ComVisible(true)]
    public class HMACRIPEMD160 : HMAC {
        //
        // public constructors
        //

        public HMACRIPEMD160 () : this (Utils.GenerateRandom(64)) {}

        public HMACRIPEMD160 (byte[] key) {
            m_hashName = "RIPEMD160";
            m_hash1 = new RIPEMD160Managed();
            m_hash2 = new RIPEMD160Managed();
            HashSizeValue = 160;
            base.InitializeKey(key);
        }
    }
}
