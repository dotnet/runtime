// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using System.Security.Cryptography.Asn1;

namespace System.Security.Cryptography.Pkcs.Asn1
{
    // https://tools.ietf.org/html/rfc5652#section-5.3
    //
    // SignerIdentifier ::= CHOICE {
    //   issuerAndSerialNumber IssuerAndSerialNumber,
    //   subjectKeyIdentifier[0] SubjectKeyIdentifier }
    //
    // SubjectKeyIdentifier ::= OCTET STRING
    //
    [Choice]
    [StructLayout(LayoutKind.Sequential)]
    internal struct SignerIdentifierAsn
    {
        public IssuerAndSerialNumberAsn? IssuerAndSerialNumber;

        [OctetString]
        [ExpectedTag(0)]
        public ReadOnlyMemory<byte>? SubjectKeyIdentifier;
    }
}
