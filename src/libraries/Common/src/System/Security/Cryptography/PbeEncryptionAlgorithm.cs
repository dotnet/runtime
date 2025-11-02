// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Security.Cryptography
{
    /// <summary>
    ///   Specifies encryption algorithms to be used with Password-Based Encryption (PBE).
    /// </summary>
    public enum PbeEncryptionAlgorithm
    {
        /// <summary>
        ///   Indicates that no encryption algorithm has been selected.
        /// </summary>
        Unknown = 0,

        /// <summary>
        ///   Indicates the encryption should be performed with the AES-128 algorithm in CBC mode with PKCS#7 padding.
        /// </summary>
        Aes128Cbc = 1,

        /// <summary>
        ///   Indicates the encryption should be performed with the AES-192 algorithm in CBC mode with PKCS#7 padding.
        /// </summary>
        Aes192Cbc = 2,

        /// <summary>
        ///   Indicates the encryption should be performed with the AES-256 algorithm in CBC mode with PKCS#7 padding.
        /// </summary>
        Aes256Cbc = 3,

        /// <summary>
        ///   Indicates the encryption should be performed with the TripleDES algorithm in CBC mode with a 192-bit key
        ///   derived using the Key Derivation Function (KDF) from PKCS#12.
        /// </summary>
        TripleDes3KeyPkcs12 = 4,
    }
}
