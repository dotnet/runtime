// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using System.Security.Cryptography.Asn1;

namespace System.Security.Cryptography.Pkcs.Asn1
{
    // https://tools.ietf.org/html/rfc5652#section-6.1
    //
    // OriginatorInfo ::= SEQUENCE {
    //   certs[0] IMPLICIT CertificateSet OPTIONAL,
    //   crls[1] IMPLICIT RevocationInfoChoices OPTIONAL }
    [StructLayout(LayoutKind.Sequential)]
    internal sealed class OriginatorInfoAsn
    {
        [OptionalValue]
        [ExpectedTag(0)]
        [SetOf]
        public CertificateChoiceAsn[] CertificateSet;

        [OptionalValue]
        [ExpectedTag(1)]
        [AnyValue]
        public ReadOnlyMemory<byte>? RevocationInfoChoices;
    }
}
