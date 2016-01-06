// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#include "pal_pkcs12.h"

// TODO: temporarily keeping the un-prefixed signature of this method  
// to keep tests running in CI. This will be removed once the managed assemblies  
// are synced up with the native assemblies.
extern "C" PKCS12* DecodePkcs12(const uint8_t* buf, int32_t len)
{
    return CryptoNative_DecodePkcs12(buf, len);
}

extern "C" PKCS12* CryptoNative_DecodePkcs12(const uint8_t* buf, int32_t len)
{
    if (!buf || !len)
    {
        return nullptr;
    }

    return d2i_PKCS12(nullptr, &buf, len);
}

// TODO: temporarily keeping the un-prefixed signature of this method  
// to keep tests running in CI. This will be removed once the managed assemblies  
// are synced up with the native assemblies.
extern "C" PKCS12* DecodePkcs12FromBio(BIO* bio)
{
    return CryptoNative_DecodePkcs12FromBio(bio);
}

extern "C" PKCS12* CryptoNative_DecodePkcs12FromBio(BIO* bio)
{
    return d2i_PKCS12_bio(bio, nullptr);
}

// TODO: temporarily keeping the un-prefixed signature of this method  
// to keep tests running in CI. This will be removed once the managed assemblies  
// are synced up with the native assemblies.
extern "C" void Pkcs12Destroy(PKCS12* p12)
{
    return CryptoNative_Pkcs12Destroy(p12);
}

extern "C" void CryptoNative_Pkcs12Destroy(PKCS12* p12)
{
    if (p12 != nullptr)
    {
        PKCS12_free(p12);
    }
}

// TODO: temporarily keeping the un-prefixed signature of this method  
// to keep tests running in CI. This will be removed once the managed assemblies  
// are synced up with the native assemblies.
extern "C" PKCS12* Pkcs12Create(char* pass, EVP_PKEY* pkey, X509* cert, X509Stack* ca)
{
    return CryptoNative_Pkcs12Create(pass, pkey, cert, ca);
}

extern "C" PKCS12* CryptoNative_Pkcs12Create(char* pass, EVP_PKEY* pkey, X509* cert, X509Stack* ca)
{
    return PKCS12_create(
        pass, nullptr, pkey, cert, ca, NID_undef, NID_undef, PKCS12_DEFAULT_ITER, PKCS12_DEFAULT_ITER, 0);
}

// TODO: temporarily keeping the un-prefixed signature of this method  
// to keep tests running in CI. This will be removed once the managed assemblies  
// are synced up with the native assemblies.
extern "C" int32_t GetPkcs12DerSize(PKCS12* p12)
{
    return CryptoNative_GetPkcs12DerSize(p12);
}

extern "C" int32_t CryptoNative_GetPkcs12DerSize(PKCS12* p12)
{
    return i2d_PKCS12(p12, nullptr);
}

// TODO: temporarily keeping the un-prefixed signature of this method  
// to keep tests running in CI. This will be removed once the managed assemblies  
// are synced up with the native assemblies.
extern "C" int32_t EncodePkcs12(PKCS12* p12, uint8_t* buf)
{
    return CryptoNative_EncodePkcs12(p12, buf);
}

extern "C" int32_t CryptoNative_EncodePkcs12(PKCS12* p12, uint8_t* buf)
{
    return i2d_PKCS12(p12, &buf);
}

// TODO: temporarily keeping the un-prefixed signature of this method  
// to keep tests running in CI. This will be removed once the managed assemblies  
// are synced up with the native assemblies.
extern "C" int32_t Pkcs12Parse(PKCS12* p12, const char* pass, EVP_PKEY** pkey, X509** cert, X509Stack** ca)
{
    return CryptoNative_Pkcs12Parse(p12, pass, pkey, cert, ca);
}

extern "C" int32_t CryptoNative_Pkcs12Parse(PKCS12* p12, const char* pass, EVP_PKEY** pkey, X509** cert, X509Stack** ca)
{
    return PKCS12_parse(p12, pass, pkey, cert, ca);
}
