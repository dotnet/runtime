// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using System.Security.Cryptography.Asn1;

namespace System.Security.Cryptography.Pkcs.Asn1
{
    // https://tools.ietf.org/html/rfc2634#section-5.4
    //
    // SigningCertificate ::=  SEQUENCE {
    //   certs SEQUENCE OF ESSCertID,
    //   policies     SEQUENCE OF PolicyInformation OPTIONAL
    // }
    [StructLayout(LayoutKind.Sequential)]
    internal struct SigningCertificateAsn
    {
        public EssCertId[] Certs;

        [OptionalValue]
        public PolicyInformation[] Policies;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal sealed class EssCertId
    {
        [OctetString]
        public ReadOnlyMemory<byte> Hash;

        [OptionalValue]
        public CadesIssuerSerial? IssuerSerial;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct CadesIssuerSerial
    {
        public GeneralName[] Issuer;

        [Integer]
        public ReadOnlyMemory<byte> SerialNumber;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct PolicyInformation
    {
        [ObjectIdentifier]
        public string PolicyIdentifier;

        [OptionalValue]
        public PolicyQualifierInfo[] PolicyQualifiers;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct PolicyQualifierInfo
    {
        [ObjectIdentifier]
        public string PolicyQualifierId;

        [AnyValue]
        public ReadOnlyMemory<byte> Qualifier;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct SigningCertificateV2Asn
    {
        public EssCertIdV2[] Certs;


        [OptionalValue]
        public PolicyInformation[] Policies;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal sealed class EssCertIdV2
    {
#pragma warning disable CS3016 // Arrays as attribute arguments is not CLS-compliant
        // SEQUENCE(OID(2.16.840.1.101.3.4.2.1))
        [DefaultValue(0x30, 0x0B, 0x06, 0x09, 0x60, 0x86, 0x48, 0x01, 0x65, 0x03, 0x04, 0x02, 0x01)]
#pragma warning restore CS3016 // Arrays as attribute arguments is not CLS-compliant
        public AlgorithmIdentifierAsn HashAlgorithm;

        [OctetString]
        public ReadOnlyMemory<byte> Hash;

        [OptionalValue]
        public CadesIssuerSerial? IssuerSerial;
    }
}
