// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using System.Security.Cryptography.Asn1;

namespace System.Security.Cryptography.Pkcs.Asn1
{
    // https://tools.ietf.org/html/rfc5652#section-5.3
    //
    // SignerInfo ::= SEQUENCE {
    //   version CMSVersion,
    //   sid SignerIdentifier,
    //   digestAlgorithm DigestAlgorithmIdentifier,
    //   signedAttrs[0] IMPLICIT SignedAttributes OPTIONAL,
    //   signatureAlgorithm SignatureAlgorithmIdentifier,
    //   signature SignatureValue,
    //   unsignedAttrs[1] IMPLICIT UnsignedAttributes OPTIONAL }
    //
    // CMSVersion ::= INTEGER { v0(0), v1(1), v2(2), v3(3), v4(4), v5(5) }
    // DigestAlgorithmIdentifier ::= AlgorithmIdentifier
    // SignedAttributes ::= SET SIZE (1..MAX) OF Attribute
    // SignatureAlgorithmIdentifier ::= AlgorithmIdentifier
    // SignatureValue ::= OCTET STRING
    // UnsignedAttributes ::= SET SIZE(1..MAX) OF Attribute
    //
    [StructLayout(LayoutKind.Sequential)]
    internal struct SignerInfoAsn
    {
        public int Version;

        public SignerIdentifierAsn Sid;

        public AlgorithmIdentifierAsn DigestAlgorithm;

        [ExpectedTag(0)]
        [SetOf]
        [OptionalValue]
        public AttributeAsn[] SignedAttributes;

        public AlgorithmIdentifierAsn SignatureAlgorithm;

        [OctetString]
        public ReadOnlyMemory<byte> SignatureValue;

        [ExpectedTag(1)]
        [SetOf]
        [OptionalValue]
        public AttributeAsn[] UnsignedAttributes;
    }
}
