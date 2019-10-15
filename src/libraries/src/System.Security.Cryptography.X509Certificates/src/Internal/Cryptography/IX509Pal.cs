// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Internal.Cryptography.Pal
{
    internal interface IX509Pal
    {
        AsymmetricAlgorithm DecodePublicKey(Oid oid, byte[] encodedKeyValue, byte[] encodedParameters, ICertificatePal certificatePal);
        string X500DistinguishedNameDecode(byte[] encodedDistinguishedName, X500DistinguishedNameFlags flag);
        byte[] X500DistinguishedNameEncode(string distinguishedName, X500DistinguishedNameFlags flag);
        string X500DistinguishedNameFormat(byte[] encodedDistinguishedName, bool multiLine);
        X509ContentType GetCertContentType(byte[] rawData);
        X509ContentType GetCertContentType(string fileName);
        byte[] EncodeX509KeyUsageExtension(X509KeyUsageFlags keyUsages);
        void DecodeX509KeyUsageExtension(byte[] encoded, out X509KeyUsageFlags keyUsages);
        bool SupportsLegacyBasicConstraintsExtension { get; }
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
