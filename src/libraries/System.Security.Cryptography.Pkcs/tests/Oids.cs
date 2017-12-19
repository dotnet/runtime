// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Text;
using System.Diagnostics;

using Test.Cryptography;

namespace System.Security.Cryptography.Pkcs.Tests
{
    internal static class Oids
    {
        // Symmetric encryption algorithms
        public const string Rc2 = "1.2.840.113549.3.2";          //RC2
        public const string Rc4 = "1.2.840.113549.3.4";          //RC4
        public const string Des = "1.3.14.3.2.7";                //DES
        public const string TripleDesCbc = "1.2.840.113549.3.7"; //3DES-CBC
        public const string Aes128 = "2.16.840.1.101.3.4.1.2";   //AES128
        public const string Aes192 = "2.16.840.1.101.3.4.1.22";  //AES192
        public const string Aes256 = "2.16.840.1.101.3.4.1.42";  //AES256

        // Asymmetric encryption algorithms
        public const string Rsa = "1.2.840.113549.1.1.1";
        public const string Esdh = "1.2.840.113549.1.9.16.3.5";
        public const string Dh = "1.2.840.10046.2.1";

        // Cryptographic Attribute Types
        public const string SigningTime = "1.2.840.113549.1.9.5";
        public const string ContentType = "1.2.840.113549.1.9.3";
        public const string DocumentDescription = "1.3.6.1.4.1.311.88.2.2";
        public const string MessageDigest = "1.2.840.113549.1.9.4";
        public const string DocumentName = "1.3.6.1.4.1.311.88.2.1";
        public const string CounterSigner = "1.2.840.113549.1.9.6";


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

        // X509 extensions
        public const string SubjectKeyIdentifier = "2.5.29.14";
        public const string BasicConstraints2 = "2.5.29.19";

        // Hash algorithms
        public const string Md5 = "1.2.840.113549.2.5";
        public const string Sha1 = "1.3.14.3.2.26";
        public const string Sha256 = "2.16.840.1.101.3.4.2.1";
        public const string Sha384 = "2.16.840.1.101.3.4.2.2";
        public const string Sha512 = "2.16.840.1.101.3.4.2.3";
    }
}
