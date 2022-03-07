// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

internal static partial class Interop
{
    internal static partial class Crypt32
    {
        [Flags]
        internal enum ExpectedContentTypeFlags : int
        {
            //encoded single certificate
            CERT_QUERY_CONTENT_FLAG_CERT = 1 << ContentType.CERT_QUERY_CONTENT_CERT,

            //encoded single CTL
            CERT_QUERY_CONTENT_FLAG_CTL = 1 << ContentType.CERT_QUERY_CONTENT_CTL,

            //encoded single CRL
            CERT_QUERY_CONTENT_FLAG_CRL = 1 << ContentType.CERT_QUERY_CONTENT_CRL,

            //serialized store
            CERT_QUERY_CONTENT_FLAG_SERIALIZED_STORE = 1 << ContentType.CERT_QUERY_CONTENT_SERIALIZED_STORE,

            //serialized single certificate
            CERT_QUERY_CONTENT_FLAG_SERIALIZED_CERT = 1 << ContentType.CERT_QUERY_CONTENT_SERIALIZED_CERT,

            //serialized single CTL
            CERT_QUERY_CONTENT_FLAG_SERIALIZED_CTL = 1 << ContentType.CERT_QUERY_CONTENT_SERIALIZED_CTL,

            //serialized single CRL
            CERT_QUERY_CONTENT_FLAG_SERIALIZED_CRL = 1 << ContentType.CERT_QUERY_CONTENT_SERIALIZED_CRL,

            //an encoded PKCS#7 signed message
            CERT_QUERY_CONTENT_FLAG_PKCS7_SIGNED = 1 << ContentType.CERT_QUERY_CONTENT_PKCS7_SIGNED,

            //an encoded PKCS#7 message.  But it is not a signed message
            CERT_QUERY_CONTENT_FLAG_PKCS7_UNSIGNED = 1 << ContentType.CERT_QUERY_CONTENT_PKCS7_UNSIGNED,

            //the content includes an embedded PKCS7 signed message
            CERT_QUERY_CONTENT_FLAG_PKCS7_SIGNED_EMBED = 1 << ContentType.CERT_QUERY_CONTENT_PKCS7_SIGNED_EMBED,

            //an encoded PKCS#10
            CERT_QUERY_CONTENT_FLAG_PKCS10 = 1 << ContentType.CERT_QUERY_CONTENT_PKCS10,

            //an encoded PFX BLOB
            CERT_QUERY_CONTENT_FLAG_PFX = 1 << ContentType.CERT_QUERY_CONTENT_PFX,

            //an encoded CertificatePair (contains forward and/or reverse cross certs)
            CERT_QUERY_CONTENT_FLAG_CERT_PAIR = 1 << ContentType.CERT_QUERY_CONTENT_CERT_PAIR,

            //an encoded PFX BLOB, and we do want to load it (not included in
            //CERT_QUERY_CONTENT_FLAG_ALL)
            CERT_QUERY_CONTENT_FLAG_PFX_AND_LOAD = 1 << ContentType.CERT_QUERY_CONTENT_PFX_AND_LOAD,

            CERT_QUERY_CONTENT_FLAG_ALL =
                CERT_QUERY_CONTENT_FLAG_CERT |
                CERT_QUERY_CONTENT_FLAG_CTL |
                CERT_QUERY_CONTENT_FLAG_CRL |
                CERT_QUERY_CONTENT_FLAG_SERIALIZED_STORE |
                CERT_QUERY_CONTENT_FLAG_SERIALIZED_CERT |
                CERT_QUERY_CONTENT_FLAG_SERIALIZED_CTL |
                CERT_QUERY_CONTENT_FLAG_SERIALIZED_CRL |
                CERT_QUERY_CONTENT_FLAG_PKCS7_SIGNED |
                CERT_QUERY_CONTENT_FLAG_PKCS7_UNSIGNED |
                CERT_QUERY_CONTENT_FLAG_PKCS7_SIGNED_EMBED |
                CERT_QUERY_CONTENT_FLAG_PKCS10 |
                CERT_QUERY_CONTENT_FLAG_PFX |
                CERT_QUERY_CONTENT_FLAG_CERT_PAIR,
        }
    }
}
