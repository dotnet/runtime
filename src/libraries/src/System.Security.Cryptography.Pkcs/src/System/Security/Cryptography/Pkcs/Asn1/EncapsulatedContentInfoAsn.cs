// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using System.Security.Cryptography.Asn1;

namespace System.Security.Cryptography.Pkcs.Asn1
{
    // https://tools.ietf.org/html/rfc5652#section-5.2
    //
    // EncapsulatedContentInfo ::= SEQUENCE {
    //   eContentType ContentType,
    //   eContent[0] EXPLICIT OCTET STRING OPTIONAL }
    //
    // ContentType::= OBJECT IDENTIFIER
    [StructLayout(LayoutKind.Sequential)]
    internal struct EncapsulatedContentInfoAsn
    {
        [ObjectIdentifier]
        public string ContentType;

        [OptionalValue]
        [ExpectedTag(0, ExplicitTag = true)]
        [OctetString]
        public ReadOnlyMemory<byte>? Content;
    }
}
