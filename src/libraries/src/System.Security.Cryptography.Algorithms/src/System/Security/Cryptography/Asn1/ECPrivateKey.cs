// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;

namespace System.Security.Cryptography.Asn1
{
    // https://www.secg.org/sec1-v2.pdf, C.4
    //
    // ECPrivateKey ::= SEQUENCE {
    //   version INTEGER { ecPrivkeyVer1(1) } (ecPrivkeyVer1),
    //   privateKey OCTET STRING,
    //   parameters [0] ECDomainParameters {{ SECGCurveNames }} OPTIONAL,
    //   publicKey [1] BIT STRING OPTIONAL
    // }
    [StructLayout(LayoutKind.Sequential)]
    internal struct ECPrivateKey
    {
        public byte Version;

        [OctetString]
        public ReadOnlyMemory<byte> PrivateKey;

        [OptionalValue]
        [ExpectedTag(0, ExplicitTag = true)]
        public ECDomainParameters? Parameters;

        [BitString, OptionalValue]
        [ExpectedTag(1, ExplicitTag = true)]
        public ReadOnlyMemory<byte>? PublicKey;
    }
}
