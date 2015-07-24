// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Security.Cryptography
{
    /// <summary>
    /// Specifies the padding mode to use with RSA signature creation or verification operations.
    /// </summary>
    public enum RSASignaturePaddingMode
    {
        /// <summary>
        /// PKCS #1 v1.5.
        /// </summary>
        /// <remarks>
        /// This corresponds to the RSASSA-PKCS1-v1.5 signature scheme of the PKCS #1 RSA Encryption Standard.
        /// It is supported for compatibility with existing applications.
        /// </remarks>
        Pkcs1,

        /// <summary>
        /// Probabilistic Signature Scheme.
        /// </summary>
        /// <remarks>
        /// This corresponds to the RSASSA-PKCS1-v1.5 signature scheme of the PKCS #1 RSA Encryption Standard.
        /// It is recommended for new applications.
        /// </remarks>
        Pss,
    }
}