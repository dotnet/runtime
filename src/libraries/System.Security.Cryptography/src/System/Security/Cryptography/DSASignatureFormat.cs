// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Security.Cryptography
{
    /// <summary>
    /// Specifies the data format for signatures with the DSA family of algorithms.
    /// </summary>
    public enum DSASignatureFormat
    {
        /// <summary>
        ///   The signature format from IEEE P1363, which produces a fixed size signature for a given key.
        /// </summary>
        /// <remarks>
        ///   This signature format encodes the `(r, s)` tuple as the concatenation of the
        ///   big-endian representation of `r` and the big-endian representation of `s`, each
        ///   value encoding using the number of bytes required for encoding the maximum integer
        ///   value in the key's mathematical field. For example, an ECDSA signature from the curve
        ///   `secp521r1`, a 521-bit field, will encode each of `r` and `s` as 66 bytes, producing
        ///   a signature output of 132 bytes.
        /// </remarks>
        IeeeP1363FixedFieldConcatenation,

        /// <summary>
        ///   The signature format from IETF RFC 3279, which produces a variably-sized signature.
        /// </summary>
        /// <remarks>
        ///   This signature format encodes the `(r, s)` tuple as the DER encoding of
        ///   `SEQUENCE(INTEGER(r), INTEGER(s))`. Because the length of a DER INTEGER encoding
        ///   varies according to the value being encoded, this signature format does not produce
        ///   a consistent signature length. Signatures in this format always start with `0x30`,
        ///   and on average are 7 bytes longer than signatures in the
        ///   <see cref="IeeeP1363FixedFieldConcatenation"/> format.
        /// </remarks>
        Rfc3279DerSequence,
    }

    internal static class DSASignatureFormatHelpers
    {
        internal static bool IsKnownValue(this DSASignatureFormat signatureFormat) =>
            signatureFormat >= DSASignatureFormat.IeeeP1363FixedFieldConcatenation &&
            signatureFormat <= DSASignatureFormat.Rfc3279DerSequence;

        internal static Exception CreateUnknownValueException(DSASignatureFormat signatureFormat) =>
            new ArgumentOutOfRangeException(
                nameof(signatureFormat),
                SR.Format(SR.Cryptography_UnknownSignatureFormat, signatureFormat));
    }
}
