// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;

namespace System.Security.Cryptography.Asn1
{
    // https://tools.ietf.org/html/rfc5208#section-5
    //
    // PrivateKeyInfo ::= SEQUENCE {
    //   version                   Version,
    //   privateKeyAlgorithm       PrivateKeyAlgorithmIdentifier,
    //   privateKey                PrivateKey,
    //   attributes           [0]  IMPLICIT Attributes OPTIONAL }
    // 
    // Version ::= INTEGER
    // PrivateKeyAlgorithmIdentifier ::= AlgorithmIdentifier
    // PrivateKey ::= OCTET STRING
    // Attributes ::= SET OF Attribute
    [StructLayout(LayoutKind.Sequential)]
    internal struct PrivateKeyInfoAsn
    {
        public byte Version;

        public AlgorithmIdentifierAsn PrivateKeyAlgorithm;

        [OctetString]
        public ReadOnlyMemory<byte> PrivateKey;

        [ExpectedTag(0)]
        [OptionalValue]
        public AttributeAsn[] Attributes;
    }
}
