// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
        public const String BasicConstraints          = "2.5.29.10";
        public const String SubjectKeyIdentifier      = "2.5.29.14";
        public const String KeyUsage                  = "2.5.29.15";
        public const String BasicConstraints2         = "2.5.29.19";
        public const String CertPolicies              = "2.5.29.32";
        public const String EnhancedKeyUsage          = "2.5.29.37";
        public const String RsaRsa                    = "1.2.840.113549.1.1.1";
        public const String EnrollCertTypeExtension   = "1.3.6.1.4.1.311.20.2";
        public const String CertificateTemplate       = "1.3.6.1.4.1.311.21.7";
    }
}
