// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "pal_pkcs12.h"

extern "C" PKCS12* CryptoNative_DecodePkcs12(const uint8_t* buf, int32_t len)
{
    if (!buf || !len)
    {
        return nullptr;
    }

    return d2i_PKCS12(nullptr, &buf, len);
}

extern "C" PKCS12* CryptoNative_DecodePkcs12FromBio(BIO* bio)
{
    return d2i_PKCS12_bio(bio, nullptr);
}

extern "C" void CryptoNative_Pkcs12Destroy(PKCS12* p12)
{
    if (p12 != nullptr)
    {
        PKCS12_free(p12);
    }
}

extern "C" PKCS12* CryptoNative_Pkcs12Create(char* pass, EVP_PKEY* pkey, X509* cert, X509Stack* ca)
{
    return PKCS12_create(
        pass, nullptr, pkey, cert, ca, NID_undef, NID_undef, PKCS12_DEFAULT_ITER, PKCS12_DEFAULT_ITER, 0);
}

extern "C" int32_t CryptoNative_GetPkcs12DerSize(PKCS12* p12)
{
    return i2d_PKCS12(p12, nullptr);
}

extern "C" int32_t CryptoNative_EncodePkcs12(PKCS12* p12, uint8_t* buf)
{
    return i2d_PKCS12(p12, &buf);
}

extern "C" int32_t CryptoNative_Pkcs12Parse(PKCS12* p12, const char* pass, EVP_PKEY** pkey, X509** cert, X509Stack** ca)
{
    int32_t ret = PKCS12_parse(p12, pass, pkey, cert, ca);

    if (ret)
    {
        // PKCS12_parse's main loop can put a lot of spurious errors into the
        // error queue.  If we're returning success, clear the error queue.
        ERR_clear_error();
    }
    else
    {
        // If PKCS12_parse encounters an error it will free the handles it 
        // created, but it does not clear the output parameters they were 
        // placed in.
        // If those handles make it back into managed code they will crash 
        // the coreclr when Disposed.
        *pkey = nullptr;
        *cert = nullptr;
    }

    return ret;
}
