// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using System.Security.Cryptography.Asn1;

namespace System.Security.Cryptography.Pkcs.Asn1
{
    // https://tools.ietf.org/html/rfc5652#section-5.1
    //
    // SignedData ::= SEQUENCE {
    //   version CMSVersion,
    //   digestAlgorithms DigestAlgorithmIdentifiers,
    //   encapContentInfo EncapsulatedContentInfo,
    //   certificates[0] IMPLICIT CertificateSet OPTIONAL,
    //   crls[1] IMPLICIT RevocationInfoChoices OPTIONAL,
    //   signerInfos SignerInfos }
    //
    // DigestAlgorithmIdentifiers::= SET OF DigestAlgorithmIdentifier
    // SignerInfos ::= SET OF SignerInfo
    // CertificateSet ::= SET OF CertificateChoices
    //
    [StructLayout(LayoutKind.Sequential)]
    internal struct SignedDataAsn
    {
        public int Version;

        [SetOf]
        public AlgorithmIdentifierAsn[] DigestAlgorithms;

        public EncapsulatedContentInfoAsn EncapContentInfo;

        [OptionalValue]
        [ExpectedTag(0)]
        [SetOf]
        public CertificateChoiceAsn[] CertificateSet;

        [OptionalValue]
        [ExpectedTag(1)]
        [AnyValue]
        public ReadOnlyMemory<byte>? RevocationInfoChoices;

        [SetOf]
        public SignerInfoAsn[] SignerInfos;
    }
}
