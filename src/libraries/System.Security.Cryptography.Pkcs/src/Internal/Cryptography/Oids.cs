// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Text;
using System.Diagnostics;

namespace Internal.Cryptography
{
    internal static class Oids
    {
        // Symmetric encryption algorithms
        public const string TripleDesCbc = "1.2.840.113549.3.7";

        // Asymmetric encryption algorithms
        public const string Rsa = "1.2.840.113549.1.1.1";
        public const string RsaPss = "1.2.840.113549.1.1.10";
        public const string Esdh = "1.2.840.113549.1.9.16.3.5";

        // Cryptographic Attribute Types
        public const string SigningTime = "1.2.840.113549.1.9.5";
        public const string ContentType = "1.2.840.113549.1.9.3";
        public const string DocumentDescription = "1.3.6.1.4.1.311.88.2.2";
        public const string MessageDigest = "1.2.840.113549.1.9.4";
        public const string CounterSigner = "1.2.840.113549.1.9.6";
        public const string DocumentName = "1.3.6.1.4.1.311.88.2.1";

        // Key wrap algorithms
        public const string CmsRc2Wrap = "1.2.840.113549.1.9.16.3.7";
        public const string Cms3DesWrap = "1.2.840.113549.1.9.16.3.6";

        // PKCS7 Content Types.
        public const string Pkcs7Data = "1.2.840.113549.1.7.1";
        public const string Pkcs7Signed = "1.2.840.113549.1.7.2";
        public const string Pkcs7Enveloped = "1.2.840.113549.1.7.3";
        public const string Pkcs7SignedEnveloped = "1.2.840.113549.1.7.4";
        public const string Pkcs7Hashed = "1.2.840.113549.1.7.5";
        public const string Pkcs7Encrypted = "1.2.840.113549.1.7.6";

        public const string Md5 = "1.2.840.113549.2.5";
        public const string Sha1 = "1.3.14.3.2.26";
        public const string Sha256 = "2.16.840.1.101.3.4.2.1";
        public const string Sha384 = "2.16.840.1.101.3.4.2.2";
        public const string Sha512 = "2.16.840.1.101.3.4.2.3";

        // DSA CMS uses the combined signature+digest OID
        public const string DsaPublicKey = "1.2.840.10040.4.1";
        public const string DsaWithSha1 = "1.2.840.10040.4.3";
        public const string DsaWithSha256 = "2.16.840.1.101.3.4.3.2";
        public const string DsaWithSha384 = "2.16.840.1.101.3.4.3.3";
        public const string DsaWithSha512 = "2.16.840.1.101.3.4.3.4";

        // ECDSA CMS uses the combined signature+digest OID
        // https://tools.ietf.org/html/rfc5753#section-2.1.1
        public const string EcPublicKey = "1.2.840.10045.2.1";
        public const string ECDsaWithSha1 = "1.2.840.10045.4.1";
        public const string ECDsaWithSha256 = "1.2.840.10045.4.3.2";
        public const string ECDsaWithSha384 = "1.2.840.10045.4.3.3";
        public const string ECDsaWithSha512 = "1.2.840.10045.4.3.4";

        public const string Mgf1 = "1.2.840.113549.1.1.8";

        // Cert Extensions
        public const string SubjectKeyIdentifier = "2.5.29.14";
        public const string KeyUsage = "2.5.29.15";
    }
}
