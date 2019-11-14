// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Security.Cryptography.Asn1
{
    /// <summary>
    ///   The encoding ruleset for an <see cref="AsnReader"/> or <see cref="AsnWriter"/>.
    /// </summary>
    // ITU-T-REC.X.680-201508 sec 4.
    internal enum AsnEncodingRules
    {
        /// <summary>
        /// ITU-T X.690 Basic Encoding Rules
        /// </summary>
        BER,

        /// <summary>
        /// ITU-T X.690 Canonical Encoding Rules
        /// </summary>
        CER,

        /// <summary>
        /// ITU-T X.690 Distinguished Encoding Rules
        /// </summary>
        DER,
    }
}
