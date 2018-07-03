// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;

namespace System.Security.Cryptography.Asn1
{
    // https://www.secg.org/sec1-v2.pdf, C.2
    //
    // Curve ::= SEQUENCE {
    //   a FieldElement,
    //   b FieldElement,
    //   seed BIT STRING OPTIONAL
    //   -- Shall be present if used in SpecifiedECDomain
    //   -- with version equal to ecdpVer2 or ecdpVer3
    // }
    //
    // FieldElement ::= OCTET STRING
    [StructLayout(LayoutKind.Sequential)]
    internal struct CurveAsn
    {
        [OctetString]
        public ReadOnlyMemory<byte> A;

        [OctetString]
        public ReadOnlyMemory<byte> B;

        [BitString, OptionalValue]
        public ReadOnlyMemory<byte>? Seed;
    }
}
