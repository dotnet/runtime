// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using System.Security.Cryptography.Asn1;

namespace System.Security.Cryptography.Pkcs.Asn1
{
    // https://tools.ietf.org/html/rfc5652#section-6.2.2
    //
    // OriginatorIdentifierOrKey ::= CHOICE {
    //   issuerAndSerialNumber IssuerAndSerialNumber,
    //   subjectKeyIdentifier[0] SubjectKeyIdentifier,
    //   originatorKey[1] OriginatorPublicKey }
    //
    // DEFINITIONS IMPLICIT TAGS, so [0] is [0] IMPLICIT, and [1] is [1] IMPLICIT
    [StructLayout(LayoutKind.Sequential)]
    [Choice]
    internal struct OriginatorIdentifierOrKeyAsn
    {
        internal IssuerAndSerialNumberAsn? IssuerAndSerialNumber;

        [OctetString]
        [ExpectedTag(0)]
        internal ReadOnlyMemory<byte>? SubjectKeyIdentifier;

        [ExpectedTag(1)]
        internal OriginatorPublicKeyAsn OriginatorKey;
    }
}
