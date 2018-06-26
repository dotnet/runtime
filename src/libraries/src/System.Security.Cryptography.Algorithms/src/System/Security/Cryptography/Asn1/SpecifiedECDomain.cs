// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;

namespace System.Security.Cryptography.Asn1
{
    // https://www.secg.org/sec1-v2.pdf, C.2
    //
    // SpecifiedECDomain ::= SEQUENCE {
    //   version SpecifiedECDomainVersion(ecdpVer1 | ecdpVer2 | ecdpVer3, ...),
    //   fieldID FieldID {{FieldTypes}},
    //   curve Curve,
    //   base ECPoint,
    //   order INTEGER,
    //   cofactor INTEGER OPTIONAL,
    //   hash HashAlgorithm OPTIONAL,
    //   ...
    // }
    //
    // HashAlgorithm ::= AlgorithmIdentifier {{ HashFunctions }}
    // ECPoint ::= OCTET STRING
    [StructLayout(LayoutKind.Sequential)]
    internal struct SpecifiedECDomain
    {
        public byte Version;

        public FieldID FieldID;

        public CurveAsn Curve;

        [OctetString]
        public ReadOnlyMemory<byte> Base;

        [Integer]
        public ReadOnlyMemory<byte> Order;

        [Integer, OptionalValue]
        public ReadOnlyMemory<byte>? Cofactor;

        [OptionalValue]
        [ObjectIdentifier(PopulateFriendlyName = true)]
        public Oid Hash;
    }
}
