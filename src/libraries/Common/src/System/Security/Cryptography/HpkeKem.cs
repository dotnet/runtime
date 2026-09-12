// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace System.Security.Cryptography
{
    /// <summary>
    ///   Specifies a key encapsulation mechanism (KEM) for an HPKE cipher suite.
    /// </summary>
    /// <seealso cref="HpkeSuite" />
    [Experimental(Experimentals.HpkeExperimentalDiagId, UrlFormat = Experimentals.SharedUrlFormat)]
    public enum HpkeKem
    {
        /// <summary>
        ///   Indicates that key encapsulation uses DHKEM with the NIST P-256 curve and HKDF-SHA-256.
        /// </summary>
        DHKEM_P256_HKDF_SHA256 = 16,

        /// <summary>
        ///   Indicates that key encapsulation uses DHKEM with the NIST P-384 curve and HKDF-SHA-384.
        /// </summary>
        DHKEM_P384_HKDF_SHA384 = 17,

        /// <summary>
        ///   Indicates that key encapsulation uses DHKEM with the NIST P-521 curve and HKDF-SHA-512.
        /// </summary>
        DHKEM_P521_HKDF_SHA512 = 18,

        /// <summary>
        ///   Indicates that key encapsulation uses DHKEM with X25519 and HKDF-SHA-256.
        /// </summary>
        DHKEM_X25519_HKDF_SHA256 = 32,

        /// <summary>
        ///   Indicates that key encapsulation uses ML-KEM-512.
        /// </summary>
        MLKEM_512 = 64,

        /// <summary>
        ///   Indicates that key encapsulation uses ML-KEM-768.
        /// </summary>
        MLKEM_768 = 65,

        /// <summary>
        ///   Indicates that key encapsulation uses ML-KEM-1024.
        /// </summary>
        MLKEM_1024 = 66,

        /// <summary>
        ///   Indicates that key encapsulation combines ML-KEM-768 with ECDH using the NIST P-256 curve.
        /// </summary>
        MLKEM768_P256 = 80,

        /// <summary>
        ///   Indicates that key encapsulation combines ML-KEM-1024 with ECDH using the NIST P-384 curve.
        /// </summary>
        MLKEM1024_P384 = 81,
    }
}
