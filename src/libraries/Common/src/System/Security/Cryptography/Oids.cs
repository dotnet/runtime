// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
        internal const string RsaPkcs1Sha1 = "1.2.840.113549.1.1.5";
        internal const string RsaPkcs1Sha256 = "1.2.840.113549.1.1.11";
        internal const string RsaPkcs1Sha384 = "1.2.840.113549.1.1.12";
        internal const string RsaPkcs1Sha512 = "1.2.840.113549.1.1.13";
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
        internal const string LocalKeyId = "1.2.840.113549.1.9.21";
        internal const string EnrollCertTypeExtension = "1.3.6.1.4.1.311.20.2";
        internal const string UserPrincipalName = "1.3.6.1.4.1.311.20.2.3";
        internal const string CertificateTemplate = "1.3.6.1.4.1.311.21.7";
        internal const string ApplicationCertPolicies = "1.3.6.1.4.1.311.21.10";
        internal const string AuthorityInformationAccess = "1.3.6.1.5.5.7.1.1";
        internal const string OcspEndpoint = "1.3.6.1.5.5.7.48.1";
        internal const string CertificateAuthorityIssuers = "1.3.6.1.5.5.7.48.2";
        internal const string Pkcs9ExtensionRequest = "1.2.840.113549.1.9.14";

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

        // DSA CMS uses the combined signature+digest OID
        internal const string DsaWithSha1 = "1.2.840.10040.4.3";
        internal const string DsaWithSha256 = "2.16.840.1.101.3.4.3.2";
        internal const string DsaWithSha384 = "2.16.840.1.101.3.4.3.3";
        internal const string DsaWithSha512 = "2.16.840.1.101.3.4.3.4";

        // ECDSA CMS uses the combined signature+digest OID
        // https://tools.ietf.org/html/rfc5753#section-2.1.1
        internal const string EcPublicKey = "1.2.840.10045.2.1";
        internal const string ECDsaWithSha1 = "1.2.840.10045.4.1";
        internal const string ECDsaWithSha256 = "1.2.840.10045.4.3.2";
        internal const string ECDsaWithSha384 = "1.2.840.10045.4.3.3";
        internal const string ECDsaWithSha512 = "1.2.840.10045.4.3.4";

        internal const string Mgf1 = "1.2.840.113549.1.1.8";
        internal const string PSpecified = "1.2.840.113549.1.1.9";

        // PKCS#7
        internal const string NoSignature = "1.3.6.1.5.5.7.6.2";

        // X500 Names
        internal const string CommonName = "2.5.4.3";
        internal const string Organization = "2.5.4.10";
        internal const string OrganizationalUnit = "2.5.4.11";
        internal const string EmailAddress = "1.2.840.113549.1.9.1";

        // Cert Extensions
        internal const string BasicConstraints = "2.5.29.10";
        internal const string SubjectKeyIdentifier = "2.5.29.14";
        internal const string KeyUsage = "2.5.29.15";
        internal const string SubjectAltName = "2.5.29.17";
        internal const string IssuerAltName = "2.5.29.18";
        internal const string BasicConstraints2 = "2.5.29.19";
        internal const string CrlDistributionPoints = "2.5.29.31";
        internal const string CertPolicies = "2.5.29.32";
        internal const string AnyCertPolicy = "2.5.29.32.0";
        internal const string CertPolicyMappings = "2.5.29.33";
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
    }
}
