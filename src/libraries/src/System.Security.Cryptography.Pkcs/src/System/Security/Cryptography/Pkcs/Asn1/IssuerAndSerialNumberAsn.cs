// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using System.Security.Cryptography.Asn1;

namespace System.Security.Cryptography.Pkcs.Asn1
{
    // https://tools.ietf.org/html/rfc5652#section-10.2.4
    //
    // IssuerAndSerialNumber ::= SEQUENCE {
    //   issuer Name,
    //   serialNumber CertificateSerialNumber }
    //
    // CertificateSerialNumber::= INTEGER
    //
    [StructLayout(LayoutKind.Sequential)]
    internal struct IssuerAndSerialNumberAsn
    {
        // X.500 name is a difficult type for the serialization engine.
        // Just throw this at X500DistinguishedName.
        [AnyValue]
        public ReadOnlyMemory<byte> Issuer;

        [Integer]
        public ReadOnlyMemory<byte> SerialNumber;
    }
}
