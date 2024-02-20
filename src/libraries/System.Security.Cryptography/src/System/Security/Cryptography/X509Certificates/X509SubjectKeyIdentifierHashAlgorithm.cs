// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;

namespace System.Security.Cryptography.X509Certificates
{
    public enum X509SubjectKeyIdentifierHashAlgorithm
    {
        Sha1 = 0,
        ShortSha1 = 1,
        CapiSha1 = 2,

        /// <summary>
        /// The SHA-256 hash over the SubjectPublicKeyInfo as described in RFC 7093.
        /// </summary>
        Sha256 = 3,

        /// <summary>
        /// The SHA-384 hash over the SubjectPublicKeyInfo as described in RFC 7093.
        /// </summary>
        Sha384 = 4,

        /// <summary>
        /// The SHA-512 hash over the SubjectPublicKeyInfo as described in RFC 7093.
        /// </summary>
        Sha512 = 5,

        /// <summary>
        /// The SHA-256 hash over the subjectPublicKey truncated to the leftmost 160-bits as described in RFC 7093.
        /// </summary>
        ShortSha256 = 6,

        /// <summary>
        /// The SHA-384 hash over the subjectPublicKey truncated to the leftmost 160-bits as described in RFC 7093.
        /// </summary>
        ShortSha384 = 7,

        /// <summary>
        /// The SHA-512 hash over the subjectPublicKey truncated to the leftmost 160-bits as described in RFC 7093.
        /// </summary>
        ShortSha512 = 8,
    }
}
