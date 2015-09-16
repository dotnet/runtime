// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Internal.Cryptography.Pal
{
    internal interface IX509Pal
    {
        AsymmetricAlgorithm DecodePublicKey(Oid oid, byte[] encodedKeyValue, byte[] encodedParameters, ICertificatePal certificatePal);
        String X500DistinguishedNameDecode(byte[] encodedDistinguishedName, X500DistinguishedNameFlags flag);
        byte[] X500DistinguishedNameEncode(String distinguishedName, X500DistinguishedNameFlags flag);
        String X500DistinguishedNameFormat(byte[] encodedDistinguishedName, bool multiLine);
        X509ContentType GetCertContentType(byte[] rawData);
        X509ContentType GetCertContentType(String fileName);
        byte[] EncodeX509KeyUsageExtension(X509KeyUsageFlags keyUsages);
        void DecodeX509KeyUsageExtension(byte[] encoded, out X509KeyUsageFlags keyUsages);
        byte[] EncodeX509BasicConstraints2Extension(bool certificateAuthority, bool hasPathLengthConstraint, int pathLengthConstraint);
        void DecodeX509BasicConstraintsExtension(byte[] encoded, out bool certificateAuthority, out bool hasPathLengthConstraint, out int pathLengthConstraint);
        void DecodeX509BasicConstraints2Extension(byte[] encoded, out bool certificateAuthority, out bool hasPathLengthConstraint, out int pathLengthConstraint);
        byte[] EncodeX509EnhancedKeyUsageExtension(OidCollection usages);
        void DecodeX509EnhancedKeyUsageExtension(byte[] encoded, out OidCollection usages);
        byte[] EncodeX509SubjectKeyIdentifierExtension(byte[] subjectKeyIdentifier);
        void DecodeX509SubjectKeyIdentifierExtension(byte[] encoded, out byte[] subjectKeyIdentifier);
        byte[] ComputeCapiSha1OfPublicKey(PublicKey key);
    }
}
