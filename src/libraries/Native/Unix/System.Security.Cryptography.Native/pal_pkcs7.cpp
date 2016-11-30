// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "pal_pkcs7.h"

extern "C" PKCS7* CryptoNative_PemReadBioPkcs7(BIO* bp)
{
    return PEM_read_bio_PKCS7(bp, nullptr, nullptr, nullptr);
}

extern "C" PKCS7* CryptoNative_DecodePkcs7(const uint8_t* buf, int32_t len)
{
    if (!buf || !len)
    {
        return nullptr;
    }

    return d2i_PKCS7(nullptr, &buf, len);
}

extern "C" PKCS7* CryptoNative_D2IPkcs7Bio(BIO* bp)
{
    return d2i_PKCS7_bio(bp, nullptr);
}

extern "C" PKCS7* CryptoNative_Pkcs7CreateSigned()
{
    PKCS7* pkcs7 = PKCS7_new();

    if (pkcs7 == nullptr)
    {
        return nullptr;
    }

    if (!PKCS7_set_type(pkcs7, NID_pkcs7_signed) || !PKCS7_content_new(pkcs7, NID_pkcs7_data))
    {
        PKCS7_free(pkcs7);
        return nullptr;
    }

    return pkcs7;
}

extern "C" void CryptoNative_Pkcs7Destroy(PKCS7* p7)
{
    if (p7 != nullptr)
    {
        PKCS7_free(p7);
    }
}

extern "C" int32_t CryptoNative_GetPkcs7Certificates(PKCS7* p7, X509Stack** certs)
{
    if (!p7 || !certs)
    {
        return 0;
    }

    switch (OBJ_obj2nid(p7->type))
    {
        case NID_pkcs7_signed:
            *certs = p7->d.sign->cert;
            return 1;
        case NID_pkcs7_signedAndEnveloped:
            *certs = p7->d.signed_and_enveloped->cert;
            return 1;
    }

    return 0;
}

extern "C" int32_t CryptoNative_Pkcs7AddCertificate(PKCS7* p7, X509* x509)
{
    if (p7 == nullptr || x509 == nullptr)
    {
        return 0;
    }

    return PKCS7_add_certificate(p7, x509);
}

extern "C" int32_t CryptoNative_GetPkcs7DerSize(PKCS7* p7)
{
    return i2d_PKCS7(p7, nullptr);
}

extern "C" int32_t CryptoNative_EncodePkcs7(PKCS7* p7, uint8_t* buf)
{
    return i2d_PKCS7(p7, &buf);
}
