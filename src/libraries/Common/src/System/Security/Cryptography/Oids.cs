// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Security.Cryptography
{
    internal static partial class Oids
    {
        // Symmetric encryption algorithms
        internal const string Rc2Cbc = "1.2.840.113549.3.2";
        internal const string Rc4 = "1.2.840.113549.3.4";
        internal const string TripleDesCbc = "1.2.840.113549.3.7";
        internal const string DesCbc = "1.3.14.3.2.7";
        internal const string Aes128Cbc = "2.16.840.1.101.3.4.1.2";
        internal const string Aes192Cbc = "2.16.840.1.101.3.4.1.22";
        internal const string Aes256Cbc = "2.16.840.1.101.3.4.1.42";

        // Asymmetric encryption algorithms
        internal const string Dsa = "1.2.840.10040.4.1";
        internal const string Rsa = "1.2.840.113549.1.1.1";
        internal const string RsaOaep = "1.2.840.113549.1.1.7";
        internal const string RsaPss = "1.2.840.113549.1.1.10";
        internal const string RsaPkcs1Md5 = "1.2.840.113549.1.1.4";
        internal const string RsaPkcs1Sha1 = "1.2.840.113549.1.1.5";
        internal const string RsaPkcs1Sha256 = "1.2.840.113549.1.1.11";
        internal const string RsaPkcs1Sha384 = "1.2.840.113549.1.1.12";
        internal const string RsaPkcs1Sha512 = "1.2.840.113549.1.1.13";
        internal const string RsaPkcs1Sha3_256 = "2.16.840.1.101.3.4.3.14";
        internal const string RsaPkcs1Sha3_384 = "2.16.840.1.101.3.4.3.15";
        internal const string RsaPkcs1Sha3_512 = "2.16.840.1.101.3.4.3.16";
        internal const string Esdh = "1.2.840.113549.1.9.16.3.5";
        internal const string EcDiffieHellman = "1.3.132.1.12";
        internal const string DiffieHellman = "1.2.840.10046.2.1";
        internal const string DiffieHellmanPkcs3 = "1.2.840.113549.1.3.1";

        // Cryptographic Attribute Types
        internal const string SigningTime = "1.2.840.113549.1.9.5";
        internal const string ContentType = "1.2.840.113549.1.9.3";
        internal const string DocumentDescription = "1.3.6.1.4.1.311.88.2.2";
        internal const string MessageDigest = "1.2.840.113549.1.9.4";
        internal const string CounterSigner = "1.2.840.113549.1.9.6";
        internal const string SigningCertificate = "1.2.840.113549.1.9.16.2.12";
        internal const string SigningCertificateV2 = "1.2.840.113549.1.9.16.2.47";
        internal const string DocumentName = "1.3.6.1.4.1.311.88.2.1";
        internal const string Pkcs9FriendlyName = "1.2.840.113549.1.9.20";
        internal const string LocalKeyId = "1.2.840.113549.1.9.21";
        internal const string EnrollCertTypeExtension = "1.3.6.1.4.1.311.20.2";
        internal const string UserPrincipalName = "1.3.6.1.4.1.311.20.2.3";
        internal const string CertificateTemplate = "1.3.6.1.4.1.311.21.7";
        internal const string ApplicationCertPolicies = "1.3.6.1.4.1.311.21.10";
        internal const string AuthorityInformationAccess = "1.3.6.1.5.5.7.1.1";
        internal const string OcspEndpoint = "1.3.6.1.5.5.7.48.1";
        internal const string CertificateAuthorityIssuers = "1.3.6.1.5.5.7.48.2";
        internal const string Pkcs9ExtensionRequest = "1.2.840.113549.1.9.14";
        internal const string MsPkcs12KeyProviderName = "1.3.6.1.4.1.311.17.1";
        internal const string MsPkcs12MachineKeySet = "1.3.6.1.4.1.311.17.2";

        // Key wrap algorithms
        internal const string CmsRc2Wrap = "1.2.840.113549.1.9.16.3.7";
        internal const string Cms3DesWrap = "1.2.840.113549.1.9.16.3.6";

        // PKCS7 Content Types.
        internal const string Pkcs7Data = "1.2.840.113549.1.7.1";
        internal const string Pkcs7Signed = "1.2.840.113549.1.7.2";
        internal const string Pkcs7Enveloped = "1.2.840.113549.1.7.3";
        internal const string Pkcs7SignedEnveloped = "1.2.840.113549.1.7.4";
        internal const string Pkcs7Hashed = "1.2.840.113549.1.7.5";
        internal const string Pkcs7Encrypted = "1.2.840.113549.1.7.6";

        internal const string Md5 = "1.2.840.113549.2.5";
        internal const string Sha1 = "1.3.14.3.2.26";
        internal const string Sha256 = "2.16.840.1.101.3.4.2.1";
        internal const string Sha384 = "2.16.840.1.101.3.4.2.2";
        internal const string Sha512 = "2.16.840.1.101.3.4.2.3";
        internal const string Sha3_256 = "2.16.840.1.101.3.4.2.8";
        internal const string Sha3_384 = "2.16.840.1.101.3.4.2.9";
        internal const string Sha3_512 = "2.16.840.1.101.3.4.2.10";

        // DSA CMS uses the combined signature+digest OID
        internal const string DsaWithSha1 = "1.2.840.10040.4.3";
        internal const string DsaWithSha256 = "2.16.840.1.101.3.4.3.2";
        internal const string DsaWithSha384 = "2.16.840.1.101.3.4.3.3";
        internal const string DsaWithSha512 = "2.16.840.1.101.3.4.3.4";

        // ECDSA CMS uses the combined signature+digest OID
        // https://tools.ietf.org/html/rfc5753#section-2.1.1
        internal const string EcPrimeField = "1.2.840.10045.1.1";
        internal const string EcChar2Field = "1.2.840.10045.1.2";
        internal const string EcChar2TrinomialBasis = "1.2.840.10045.1.2.3.2";
        internal const string EcChar2PentanomialBasis = "1.2.840.10045.1.2.3.3";
        internal const string EcPublicKey = "1.2.840.10045.2.1";
        internal const string ECDsaWithSha1 = "1.2.840.10045.4.1";
        internal const string ECDsaWithSha256 = "1.2.840.10045.4.3.2";
        internal const string ECDsaWithSha384 = "1.2.840.10045.4.3.3";
        internal const string ECDsaWithSha512 = "1.2.840.10045.4.3.4";

        internal const string ECDsaWithSha3_256 = "2.16.840.1.101.3.4.3.10";
        internal const string ECDsaWithSha3_384 = "2.16.840.1.101.3.4.3.11";
        internal const string ECDsaWithSha3_512 = "2.16.840.1.101.3.4.3.12";

        internal const string MLDsa44 = "2.16.840.1.101.3.4.3.17";
        internal const string MLDsa65 = "2.16.840.1.101.3.4.3.18";
        internal const string MLDsa87 = "2.16.840.1.101.3.4.3.19";
        internal const string MLDsa44PreHashSha512 = "2.16.840.1.101.3.4.3.32";
        internal const string MLDsa65PreHashSha512 = "2.16.840.1.101.3.4.3.33";
        internal const string MLDsa87PreHashSha512 = "2.16.840.1.101.3.4.3.34";

        internal const string Mgf1 = "1.2.840.113549.1.1.8";
        internal const string PSpecified = "1.2.840.113549.1.1.9";

        // PKCS#7
        internal const string NoSignature = "1.3.6.1.5.5.7.6.2";

        // X500 Names - T-REC X.520-201910
        internal const string KnowledgeInformation = "2.5.4.2"; // 6.1.1 - id-at-knowledgeInformation
        internal const string CommonName = "2.5.4.3"; // 6.2.2 - id-at-commonName
        internal const string Surname = "2.5.4.4"; // 6.2.3 - id-at-surname
        internal const string SerialNumber = "2.5.4.5"; // 6.2.9 - id-at-serialNumber
        internal const string CountryOrRegionName = "2.5.4.6"; // 6.3.1 - id-at-countryName
        internal const string LocalityName = "2.5.4.7"; // 6.3.4 - id-at-localityName
        internal const string StateOrProvinceName = "2.5.4.8"; // 6.3.5 - id-at-stateOrProvinceName
        internal const string StreetAddress = "2.5.4.9"; // 6.3.6 - id-at-streetAddress
        internal const string Organization = "2.5.4.10"; // 6.4.1 - id-at-organizationName
        internal const string OrganizationalUnit = "2.5.4.11"; // 6.4.2 - id-at-organizationalUnitName
        internal const string Title = "2.5.4.12"; // 6.4.3 - id-at-title
        internal const string Description = "2.5.4.13"; // 6.5.1 - id-at-description
        internal const string BusinessCategory = "2.5.4.15"; // 6.5.4 - id-at-businessCategory
        internal const string PostalCode = "2.5.4.17"; // 6.6.2 - id-at-postalCode
        internal const string PostOfficeBox = "2.5.4.18"; // 6.6.3 - id-at-postOfficeBox
        internal const string PhysicalDeliveryOfficeName = "2.5.4.19"; // 6.6.4 - id-at-physicalDeliveryOfficeName
        internal const string TelephoneNumber = "2.5.4.20"; // 6.7.1 - id-at-telephoneNumber
        internal const string X121Address = "2.5.4.24"; // 6.7.5 - id-at-x121Address
        internal const string InternationalISDNNumber = "2.5.4.25"; // 6.7.6 - id-at-internationalISDNNumber
        internal const string DestinationIndicator = "2.5.4.27"; // 6.7.8 - id-at-destinationIndicator
        internal const string Name = "2.5.4.41"; // 6.2.1 - id-at-name
        internal const string GivenName = "2.5.4.42"; // 6.2.4 - id-at-givenName
        internal const string Initials = "2.5.4.43"; // 6.2.5 - id-at-initials
        internal const string GenerationQualifier = "2.5.4.44"; // 6.2.6 - id-at-generationQualifier
        internal const string DnQualifier = "2.5.4.46"; // 6.2.8 - id-at-dnQualifier
        internal const string HouseIdentifier = "2.5.4.51"; // 6.3.7 - id-at-houseIdentifier
        internal const string DmdName = "2.5.4.54"; // 6.11.1 - id-at-dmdName
        internal const string Pseudonym = "2.5.4.65"; // 6.2.10 - id-at-pseudonym
        internal const string UiiInUrn = "2.5.4.80"; // 6.13.3 - id-at-uiiInUrn
        internal const string ContentUrl = "2.5.4.81"; // 6.13.4 - id-at-contentUrl
        internal const string Uri = "2.5.4.83"; // 6.2.12 - id-at-uri
        internal const string Urn = "2.5.4.86"; // 6.2.13 - id-at-urn
        internal const string Url = "2.5.4.87"; // 6.2.14 - id-at-url
        internal const string UrnC = "2.5.4.89"; // 6.12.4 - id-at-urnC
        internal const string EpcInUrn = "2.5.4.94"; // 6.13.9 - id-at-epcInUrn
        internal const string LdapUrl = "2.5.4.95"; // 6.13.10 - id-at-ldapUrl
        internal const string OrganizationIdentifier = "2.5.4.97"; // 6.4.4 - id-at-organizationIdentifier
        internal const string CountryOrRegionName3C = "2.5.4.98"; // 6.3.2 - id-at-countryCode3c
        internal const string CountryOrRegionName3N = "2.5.4.99"; // 6.3.3 - id-at-countryCode3n
        internal const string DnsName = "2.5.4.100"; // 6.2.15 - id-at-dnsName
        internal const string IntEmail = "2.5.4.104"; // 6.2.16 - id-at-intEmail
        internal const string JabberId = "2.5.4.105"; // 6.2.17 - id-at-jid

        // RFC 2985
        internal const string EmailAddress = "1.2.840.113549.1.9.1"; //  B.3.5

        // Cert Extensions
        internal const string BasicConstraints = "2.5.29.10";
        internal const string SubjectKeyIdentifier = "2.5.29.14";
        internal const string KeyUsage = "2.5.29.15";
        internal const string SubjectAltName = "2.5.29.17";
        internal const string IssuerAltName = "2.5.29.18";
        internal const string BasicConstraints2 = "2.5.29.19";
        internal const string CrlNumber = "2.5.29.20";
        internal const string CrlReasons = "2.5.29.21";
        internal const string CrlDistributionPoints = "2.5.29.31";
        internal const string CertPolicies = "2.5.29.32";
        internal const string AnyCertPolicy = "2.5.29.32.0";
        internal const string CertPolicyMappings = "2.5.29.33";
        internal const string AuthorityKeyIdentifier = "2.5.29.35";
        internal const string CertPolicyConstraints = "2.5.29.36";
        internal const string EnhancedKeyUsage = "2.5.29.37";
        internal const string InhibitAnyPolicyExtension = "2.5.29.54";

        // RFC3161 Timestamping
        internal const string TstInfo = "1.2.840.113549.1.9.16.1.4";
        internal const string TimeStampingPurpose = "1.3.6.1.5.5.7.3.8";

        // PKCS#12
        private const string Pkcs12Prefix = "1.2.840.113549.1.12.";
        private const string Pkcs12PbePrefix = Pkcs12Prefix + "1.";
        internal const string Pkcs12PbeWithShaAnd3Key3Des = Pkcs12PbePrefix + "3";
        internal const string Pkcs12PbeWithShaAnd2Key3Des = Pkcs12PbePrefix + "4";
        internal const string Pkcs12PbeWithShaAnd128BitRC2 = Pkcs12PbePrefix + "5";
        internal const string Pkcs12PbeWithShaAnd40BitRC2 = Pkcs12PbePrefix + "6";
        private const string Pkcs12BagTypesPrefix = Pkcs12Prefix + "10.1.";
        internal const string Pkcs12KeyBag = Pkcs12BagTypesPrefix + "1";
        internal const string Pkcs12ShroudedKeyBag = Pkcs12BagTypesPrefix + "2";
        internal const string Pkcs12CertBag = Pkcs12BagTypesPrefix + "3";
        internal const string Pkcs12CrlBag = Pkcs12BagTypesPrefix + "4";
        internal const string Pkcs12SecretBag = Pkcs12BagTypesPrefix + "5";
        internal const string Pkcs12SafeContentsBag = Pkcs12BagTypesPrefix + "6";
        internal const string Pkcs12X509CertBagType = "1.2.840.113549.1.9.22.1";
        internal const string Pkcs12SdsiCertBagType = "1.2.840.113549.1.9.22.2";

        // PKCS#5
        private const string Pkcs5Prefix = "1.2.840.113549.1.5.";
        internal const string PbeWithMD5AndDESCBC = Pkcs5Prefix + "3";
        internal const string PbeWithMD5AndRC2CBC = Pkcs5Prefix + "6";
        internal const string PbeWithSha1AndDESCBC = Pkcs5Prefix + "10";
        internal const string PbeWithSha1AndRC2CBC = Pkcs5Prefix + "11";
        internal const string Pbkdf2 = Pkcs5Prefix + "12";
        internal const string PasswordBasedEncryptionScheme2 = Pkcs5Prefix + "13";

        private const string RsaDsiDigestAlgorithmPrefix = "1.2.840.113549.2.";
        internal const string HmacWithSha1 = RsaDsiDigestAlgorithmPrefix + "7";
        internal const string HmacWithSha256 = RsaDsiDigestAlgorithmPrefix + "9";
        internal const string HmacWithSha384 = RsaDsiDigestAlgorithmPrefix + "10";
        internal const string HmacWithSha512 = RsaDsiDigestAlgorithmPrefix + "11";

        // Elliptic Curve curve identifiers
        internal const string secp256r1 = "1.2.840.10045.3.1.7";
        internal const string secp384r1 = "1.3.132.0.34";
        internal const string secp521r1 = "1.3.132.0.35";

        // LDAP
        internal const string DomainComponent = "0.9.2342.19200300.100.1.25";
    }
}
