// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Formats.Asn1
{
    /// <summary>
    ///   The tag class for a particular ASN.1 tag.
    /// </summary>
    // Uses a masked overlay of the tag class encoding.
    // T-REC-X.690-201508 sec 8.1.2.2
    public enum TagClass
    {
        /// <summary>
        ///   The Universal tag class
        /// </summary>
        Universal = 0,

        /// <summary>
        ///   The Application tag class
        /// </summary>
        Application = 0b0100_0000,

        /// <summary>
        ///   The Context-Specific tag class
        /// </summary>
        ContextSpecific = 0b1000_0000,

        /// <summary>
        ///   The Private tag class
        /// </summary>
        Private = 0b1100_0000,
    }
}
