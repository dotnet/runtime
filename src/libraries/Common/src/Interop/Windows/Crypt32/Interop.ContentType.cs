// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

internal static partial class Interop
{
    internal static partial class Crypt32
    {
        internal enum ContentType : int
        {
            //encoded single certificate
            CERT_QUERY_CONTENT_CERT                = 1,
            //encoded single CTL
            CERT_QUERY_CONTENT_CTL                 = 2,
            //encoded single CRL
            CERT_QUERY_CONTENT_CRL                 = 3,
            //serialized store
            CERT_QUERY_CONTENT_SERIALIZED_STORE    = 4,
            //serialized single certificate
            CERT_QUERY_CONTENT_SERIALIZED_CERT     = 5,
            //serialized single CTL
            CERT_QUERY_CONTENT_SERIALIZED_CTL      = 6,
            //serialized single CRL
            CERT_QUERY_CONTENT_SERIALIZED_CRL      = 7,
            //a PKCS#7 signed message
            CERT_QUERY_CONTENT_PKCS7_SIGNED        = 8,
            //a PKCS#7 message, such as enveloped message.  But it is not a signed message,
            CERT_QUERY_CONTENT_PKCS7_UNSIGNED      = 9,
            //a PKCS7 signed message embedded in a file
            CERT_QUERY_CONTENT_PKCS7_SIGNED_EMBED  = 10,
            //an encoded PKCS#10
            CERT_QUERY_CONTENT_PKCS10              = 11,
            //an encoded PFX BLOB
            CERT_QUERY_CONTENT_PFX                 = 12,
            //an encoded CertificatePair (contains forward and/or reverse cross certs)
            CERT_QUERY_CONTENT_CERT_PAIR           = 13,
            //an encoded PFX BLOB, which was loaded to phCertStore
            CERT_QUERY_CONTENT_PFX_AND_LOAD        = 14,
        }
    }
}
