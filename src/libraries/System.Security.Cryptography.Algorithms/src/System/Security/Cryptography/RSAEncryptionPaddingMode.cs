// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Security.Cryptography
{
    /// <summary>
    /// Specifies the padding mode to use with RSA encryption or decryption operations.
    /// </summary>
    public enum RSAEncryptionPaddingMode
    {
        /// <summary>
        /// PKCS #1 v1.5.
        /// </summary>
        /// <remarks>
        /// This mode correpsonds to the RSAES-PKCS1-v1_5 encryption scheme described in the PKCS #1 RSA Encryption Standard.
        /// It is supported for compatibility with existing applications.
        /// </remarks>
        Pkcs1,

        /// <summary>
        /// Optimal Asymmetric Encryption Padding.
        /// </summary>
        /// <remarks>
        /// This mode corresponds to the RSAES-OEAP encryption scheme described in the PKCS #1 RSA Encryption Standard.
        /// It is recommended for new applications.
        /// </remarks>
        Oaep,
    }
}
