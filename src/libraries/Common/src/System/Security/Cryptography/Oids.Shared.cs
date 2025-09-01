// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Formats.Asn1;

namespace System.Security.Cryptography
{
    internal static partial class Oids
    {
        internal static Oid RsaOid => field ??= InitializeOid(Rsa);
        internal static Oid EcPublicKeyOid => field ??= InitializeOid(EcPublicKey);
        internal static Oid TripleDesCbcOid => field ??= InitializeOid(TripleDesCbc);
        internal static Oid Aes256CbcOid => field ??= InitializeOid(Aes256Cbc);
        internal static Oid secp256r1Oid => field ??= new Oid(secp256r1, nameof(ECCurve.NamedCurves.nistP256));
        internal static Oid secp384r1Oid => field ??= new Oid(secp384r1, nameof(ECCurve.NamedCurves.nistP384));
        internal static Oid secp521r1Oid => field ??= new Oid(secp521r1, nameof(ECCurve.NamedCurves.nistP521));
        internal static Oid Sha256Oid => field ??= InitializeOid(Sha256);

        internal static Oid Pkcs7DataOid => field ??= InitializeOid(Pkcs7Data);
        internal static Oid ContentTypeOid => field ??= InitializeOid(ContentType);
        internal static Oid DocumentDescriptionOid => field ??= InitializeOid(DocumentDescription);
        internal static Oid DocumentNameOid => field ??= InitializeOid(DocumentName);
        internal static Oid LocalKeyIdOid => field ??= InitializeOid(LocalKeyId);
        internal static Oid MessageDigestOid => field ??= InitializeOid(MessageDigest);
        internal static Oid SigningTimeOid => field ??= InitializeOid(SigningTime);
        internal static Oid Pkcs9ExtensionRequestOid => field ??= InitializeOid(Pkcs9ExtensionRequest);

        internal static Oid BasicConstraints2Oid => field ??= InitializeOid(BasicConstraints2);
        internal static Oid EnhancedKeyUsageOid => field ??= InitializeOid(EnhancedKeyUsage);
        internal static Oid KeyUsageOid => field ??= InitializeOid(KeyUsage);
        internal static Oid AuthorityKeyIdentifierOid => field ??= InitializeOid(AuthorityKeyIdentifier);
        internal static Oid SubjectKeyIdentifierOid => field ??= InitializeOid(SubjectKeyIdentifier);
        internal static Oid SubjectAltNameOid => field ??= InitializeOid(SubjectAltName);
        internal static Oid AuthorityInformationAccessOid => field ??= InitializeOid(AuthorityInformationAccess);
        internal static Oid CrlNumberOid => field ??= InitializeOid(CrlNumber);
        internal static Oid CrlDistributionPointsOid => field ??= InitializeOid(CrlDistributionPoints);

        internal static Oid CommonNameOid => field ??= InitializeOid(CommonName);
        internal static Oid CountryOrRegionNameOid => field ??= InitializeOid(CountryOrRegionName);
        internal static Oid LocalityNameOid => field ??= InitializeOid(LocalityName);
        internal static Oid StateOrProvinceNameOid => field ??= InitializeOid(StateOrProvinceName);
        internal static Oid OrganizationOid => field ??= InitializeOid(Organization);
        internal static Oid OrganizationalUnitOid => field ??= InitializeOid(OrganizationalUnit);
        internal static Oid EmailAddressOid => field ??= InitializeOid(EmailAddress);

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
