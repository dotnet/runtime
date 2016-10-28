// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Text;
using System.Diagnostics;
using System.Globalization;
using System.Security.Cryptography;

namespace Internal.Cryptography
{
    //
    // Well-known oids
    //
    internal static class Oids
    {
        public const string BasicConstraints            = "2.5.29.10";
        public const string SubjectKeyIdentifier        = "2.5.29.14";
        public const string KeyUsage                    = "2.5.29.15";
        public const string BasicConstraints2           = "2.5.29.19";
        public const string CrlDistributionPoints       = "2.5.29.31";
        public const string CertPolicies                = "2.5.29.32";
        public const string AnyCertPolicy               = "2.5.29.32.0";
        public const string CertPolicyMappings          = "2.5.29.33";
        public const string CertPolicyConstraints       = "2.5.29.36";
        public const string EnhancedKeyUsage            = "2.5.29.37";
        public const string InhibitAnyPolicyExtension   = "2.5.29.54";
        public const string Ecc                         = "1.2.840.10045.2.1";
        public const string RsaRsa                      = "1.2.840.113549.1.1.1";
        public const string DsaDsa                      = "1.2.840.10040.4.1";
        public const string EmailAddress                = "1.2.840.113549.1.9.1";
        public const string EnrollCertTypeExtension     = "1.3.6.1.4.1.311.20.2";
        public const string CertificateTemplate         = "1.3.6.1.4.1.311.21.7";
        public const string ApplicationCertPolicies     = "1.3.6.1.4.1.311.21.10";
        public const string AuthorityInformationAccess  = "1.3.6.1.5.5.7.1.1";
        public const string CertificateAuthorityIssuers = "1.3.6.1.5.5.7.48.2";
    }
}
