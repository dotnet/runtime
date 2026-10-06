// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace System.Security.Cryptography
{
    /// <summary>
    ///   Specifies a key derivation function (KDF) for an HPKE cipher suite.
    /// </summary>
    /// <seealso cref="HpkeSuite" />
    [Experimental(Experimentals.HpkeExperimentalDiagId, UrlFormat = Experimentals.SharedUrlFormat)]
    public enum HpkeKdf
    {
        /// <summary>
        ///   Indicates that key derivation uses HKDF with SHA-256.
        /// </summary>
        HKDF_SHA256 = 0x0001,

        /// <summary>
        ///   Indicates that key derivation uses HKDF with SHA-384.
        /// </summary>
        HKDF_SHA384 = 0x0002,

        /// <summary>
        ///   Indicates that key derivation uses HKDF with SHA-512.
        /// </summary>
        HKDF_SHA512 = 0x0003,

        /// <summary>
        ///   Indicates that key derivation uses SHAKE128.
        /// </summary>
        SHAKE128 = 0x0010,

        /// <summary>
        ///   Indicates that key derivation uses SHAKE256.
        /// </summary>
        SHAKE256 = 0x0011
    }
}
