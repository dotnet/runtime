// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace System.Security.Cryptography
{
    /// <summary>
    ///   Specifies an authenticated encryption with associated data (AEAD) algorithm for an HPKE cipher suite.
    /// </summary>
    /// <seealso cref="HpkeSuite" />
    [Experimental(Experimentals.HpkeExperimentalDiagId, UrlFormat = Experimentals.SharedUrlFormat)]
    public enum HpkeAead
    {
        /// <summary>
        ///   Indicates that authenticated encryption uses AES-GCM with a 128-bit key.
        /// </summary>
        AES_128_GCM = 0x0001,

        /// <summary>
        ///   Indicates that authenticated encryption uses AES-GCM with a 256-bit key.
        /// </summary>
        AES_256_GCM = 0x0002,

        /// <summary>
        ///   Indicates that authenticated encryption uses ChaCha20-Poly1305.
        /// </summary>
        ChaCha20Poly1305 = 0x0003,
    }
}
