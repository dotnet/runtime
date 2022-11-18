// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Formats.Asn1;

namespace System.Security.Cryptography
{
    internal static partial class Oids
    {
        private static volatile Oid? s_rsaOid;
        private static volatile Oid? s_ecPublicKeyOid;
        private static volatile Oid? s_tripleDesCbcOid;
        private static volatile Oid? s_aes256CbcOid;
        private static volatile Oid? s_secp256R1Oid;
        private static volatile Oid? s_secp384R1Oid;
        private static volatile Oid? s_secp521R1Oid;
        private static volatile Oid? s_sha256Oid;
        private static volatile Oid? s_pkcs7DataOid;
        private static volatile Oid? s_contentTypeOid;
        private static volatile Oid? s_documentDescriptionOid;
        private static volatile Oid? s_documentNameOid;
        private static volatile Oid? s_localKeyIdOid;
        private static volatile Oid? s_messageDigestOid;
        private static volatile Oid? s_signingTimeOid;
        private static volatile Oid? s_pkcs9ExtensionRequestOid;
        private static volatile Oid? s_basicConstraints2Oid;
        private static volatile Oid? s_enhancedKeyUsageOid;
        private static volatile Oid? s_keyUsageOid;
        private static volatile Oid? s_subjectAltNameOid;
        private static volatile Oid? s_subjectKeyIdentifierOid;
        private static volatile Oid? s_authorityKeyIdentifierOid;
        private static volatile Oid? s_authorityInformationAccessOid;
        private static volatile Oid? s_crlNumberOid;
        private static volatile Oid? s_crlDistributionPointOid;
        private static volatile Oid? s_commonNameOid;
        private static volatile Oid? s_countryOrRegionOid;
        private static volatile Oid? s_localityNameOid;
        private static volatile Oid? s_stateOrProvinceNameOid;
        private static volatile Oid? s_organizationOid;
        private static volatile Oid? s_organizationalUnitOid;
        private static volatile Oid? s_emailAddressOid;

        internal static Oid RsaOid => s_rsaOid ??= InitializeOid(Rsa);
        internal static Oid EcPublicKeyOid => s_ecPublicKeyOid ??= InitializeOid(EcPublicKey);
        internal static Oid TripleDesCbcOid => s_tripleDesCbcOid ??= InitializeOid(TripleDesCbc);
        internal static Oid Aes256CbcOid => s_aes256CbcOid ??= InitializeOid(Aes256Cbc);
        internal static Oid secp256r1Oid => s_secp256R1Oid ??= new Oid(secp256r1, nameof(ECCurve.NamedCurves.nistP256));
        internal static Oid secp384r1Oid => s_secp384R1Oid ??= new Oid(secp384r1, nameof(ECCurve.NamedCurves.nistP384));
        internal static Oid secp521r1Oid => s_secp521R1Oid ??= new Oid(secp521r1, nameof(ECCurve.NamedCurves.nistP521));
        internal static Oid Sha256Oid => s_sha256Oid ??= InitializeOid(Sha256);

        internal static Oid Pkcs7DataOid => s_pkcs7DataOid ??= InitializeOid(Pkcs7Data);
        internal static Oid ContentTypeOid => s_contentTypeOid ??= InitializeOid(ContentType);
        internal static Oid DocumentDescriptionOid => s_documentDescriptionOid ??= InitializeOid(DocumentDescription);
        internal static Oid DocumentNameOid => s_documentNameOid ??= InitializeOid(DocumentName);
        internal static Oid LocalKeyIdOid => s_localKeyIdOid ??= InitializeOid(LocalKeyId);
        internal static Oid MessageDigestOid => s_messageDigestOid ??= InitializeOid(MessageDigest);
        internal static Oid SigningTimeOid => s_signingTimeOid ??= InitializeOid(SigningTime);
        internal static Oid Pkcs9ExtensionRequestOid => s_pkcs9ExtensionRequestOid ??= InitializeOid(Pkcs9ExtensionRequest);

        internal static Oid BasicConstraints2Oid => s_basicConstraints2Oid ??= InitializeOid(BasicConstraints2);
        internal static Oid EnhancedKeyUsageOid => s_enhancedKeyUsageOid ??= InitializeOid(EnhancedKeyUsage);
        internal static Oid KeyUsageOid => s_keyUsageOid ??= InitializeOid(KeyUsage);
        internal static Oid AuthorityKeyIdentifierOid => s_authorityKeyIdentifierOid ??= InitializeOid(AuthorityKeyIdentifier);
        internal static Oid SubjectKeyIdentifierOid => s_subjectKeyIdentifierOid ??= InitializeOid(SubjectKeyIdentifier);
        internal static Oid SubjectAltNameOid => s_subjectAltNameOid ??= InitializeOid(SubjectAltName);
        internal static Oid AuthorityInformationAccessOid => s_authorityInformationAccessOid ??= InitializeOid(AuthorityInformationAccess);
        internal static Oid CrlNumberOid => s_crlNumberOid ??= InitializeOid(CrlNumber);
        internal static Oid CrlDistributionPointsOid => s_crlDistributionPointOid ??= InitializeOid(CrlDistributionPoints);

        internal static Oid CommonNameOid => s_commonNameOid ??= InitializeOid(CommonName);
        internal static Oid CountryOrRegionNameOid => s_countryOrRegionOid ??= InitializeOid(CountryOrRegionName);
        internal static Oid LocalityNameOid => s_localityNameOid ??= InitializeOid(LocalityName);
        internal static Oid StateOrProvinceNameOid = s_stateOrProvinceNameOid ??= InitializeOid(StateOrProvinceName);
        internal static Oid OrganizationOid = s_organizationOid ??= InitializeOid(Organization);
        internal static Oid OrganizationalUnitOid = s_organizationalUnitOid ??= InitializeOid(OrganizationalUnit);
        internal static Oid EmailAddressOid = s_emailAddressOid ??= InitializeOid(EmailAddress);

        private static Oid InitializeOid(string oidValue)
        {
            Debug.Assert(oidValue != null);
            Oid oid = new Oid(oidValue, null);

            // Do not remove - the FriendlyName property get has side effects.
            // On read, it initializes the friendly name based on the value and
            // locks it to prevent any further changes.
            _ = oid.FriendlyName;

            return oid;
        }

        internal static Oid GetSharedOrNewOid(ref AsnValueReader asnValueReader)
        {
            Oid? ret = GetSharedOrNullOid(ref asnValueReader);

            if (ret is not null)
            {
                return ret;
            }

            string oidValue = asnValueReader.ReadObjectIdentifier();
            return new Oid(oidValue, null);
        }

        internal static Oid? GetSharedOrNullOid(ref AsnValueReader asnValueReader, Asn1Tag? expectedTag = null)
        {
#if NET
            Asn1Tag tag = asnValueReader.PeekTag();

            // This isn't a valid OID, so return null and let whatever's going to happen happen.
            if (tag.IsConstructed)
            {
                return null;
            }

            Asn1Tag expected = expectedTag.GetValueOrDefault(Asn1Tag.ObjectIdentifier);

            Debug.Assert(
                expected.TagClass != TagClass.Universal ||
                expected.TagValue == (int)UniversalTagNumber.ObjectIdentifier,
                $"{nameof(GetSharedOrNullOid)} was called with the wrong Universal class tag: {expectedTag}");

            // Not the tag we're expecting, so don't match.
            if (!tag.HasSameClassAndValue(expected))
            {
                return null;
            }

            ReadOnlySpan<byte> contentBytes = asnValueReader.PeekContentBytes();

            Oid? ret = contentBytes switch
            {
                [0x2A, 0x86, 0x48, 0x86, 0xF7, 0x0D, 0x01, 0x09, 0x01] => EmailAddressOid,
                [0x55, 0x04, 0x03] => CommonNameOid,
                [0x55, 0x04, 0x06] => CountryOrRegionNameOid,
                [0x55, 0x04, 0x07] => LocalityNameOid,
                [0x55, 0x04, 0x08] => StateOrProvinceNameOid,
                [0x55, 0x04, 0x0A] => OrganizationOid,
                [0x55, 0x04, 0x0B] => OrganizationalUnitOid,
                [0x55, 0x1D, 0x14] => CrlNumberOid,
                _ => null,
            };

            if (ret is not null)
            {
                // Move to the next item.
                asnValueReader.ReadEncodedValue();
            }

            return ret;
#else
            // The list pattern isn't available in System.Security.Cryptography.Pkcs for the
            // netstandard2.0 or netfx builds.  Any OIDs that it's important to optimize in
            // those contexts can be matched on here, but using a longer form of matching.

            return null;
#endif
        }

        internal static bool ValueEquals(this Oid oid, Oid? other)
        {
            Debug.Assert(oid is not null);

            if (ReferenceEquals(oid, other))
            {
                return true;
            }

            if (other is null)
            {
                return false;
            }

            return oid.Value is not null && oid.Value.Equals(other.Value);
        }
    }
}
