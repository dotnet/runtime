// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Internal.Cryptography
{
    //
    // Well-known oids
    //
    internal static class Oids
    {
        public const string CommonName                  = "2.5.4.3";
        public const string Organization                = "2.5.4.10";
        public const string OrganizationalUnit          = "2.5.4.11";
        public const string BasicConstraints            = "2.5.29.10";
        public const string SubjectKeyIdentifier        = "2.5.29.14";
        public const string SubjectAltName              = "2.5.29.17";
        public const string IssuerAltName               = "2.5.29.18";
        public const string KeyUsage                    = "2.5.29.15";
        public const string BasicConstraints2           = "2.5.29.19";
        public const string CrlDistributionPoints       = "2.5.29.31";
        public const string CertPolicies                = "2.5.29.32";
        public const string AnyCertPolicy               = "2.5.29.32.0";
        public const string CertPolicyMappings          = "2.5.29.33";
        public const string CertPolicyConstraints       = "2.5.29.36";
        public const string EnhancedKeyUsage            = "2.5.29.37";
        public const string InhibitAnyPolicyExtension   = "2.5.29.54";
        public const string Sha256                      = "2.16.840.1.101.3.4.2.1";
        public const string Sha384                      = "2.16.840.1.101.3.4.2.2";
        public const string Sha512                      = "2.16.840.1.101.3.4.2.3";
        public const string EccCurveSecp384r1           = "1.3.132.0.34";
        public const string EccCurveSecp521r1           = "1.3.132.0.35";
        public const string Ecc                         = "1.2.840.10045.2.1";
        public const string EccCurveSecp256r1           = "1.2.840.10045.3.1.7";
        public const string ECDsaSha256                 = "1.2.840.10045.4.3.2";
        public const string ECDsaSha384                 = "1.2.840.10045.4.3.3";
        public const string ECDsaSha512                 = "1.2.840.10045.4.3.4";
        public const string RsaRsa                      = "1.2.840.113549.1.1.1";
        public const string Mgf1                        = "1.2.840.113549.1.1.8";
        public const string RsaSsaPss                   = "1.2.840.113549.1.1.10";
        public const string RsaPkcs1Sha256              = "1.2.840.113549.1.1.11";
        public const string RsaPkcs1Sha384              = "1.2.840.113549.1.1.12";
        public const string RsaPkcs1Sha512              = "1.2.840.113549.1.1.13";
        public const string Pkcs9ExtensionRequest       = "1.2.840.113549.1.9.14";
        public const string DsaDsa                      = "1.2.840.10040.4.1";
        public const string EmailAddress                = "1.2.840.113549.1.9.1";
        public const string EnrollCertTypeExtension     = "1.3.6.1.4.1.311.20.2";
        public const string UserPrincipalName           = "1.3.6.1.4.1.311.20.2.3";
        public const string CertificateTemplate         = "1.3.6.1.4.1.311.21.7";
        public const string ApplicationCertPolicies     = "1.3.6.1.4.1.311.21.10";
        public const string AuthorityInformationAccess  = "1.3.6.1.5.5.7.1.1";
        public const string CertificateAuthorityIssuers = "1.3.6.1.5.5.7.48.2";
    }
}
