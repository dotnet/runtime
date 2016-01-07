// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#include "pal_x509ext.h"

#include <assert.h>

// TODO: temporarily keeping the un-prefixed signature of this method  
// to keep tests running in CI. This will be removed once the managed assemblies  
// are synced up with the native assemblies.
extern "C" X509_EXTENSION*
X509ExtensionCreateByObj(ASN1_OBJECT* obj, int32_t isCritical, ASN1_OCTET_STRING* data)
{
    return CryptoNative_X509ExtensionCreateByObj(obj, isCritical, data);
}

extern "C" X509_EXTENSION*
CryptoNative_X509ExtensionCreateByObj(ASN1_OBJECT* obj, int32_t isCritical, ASN1_OCTET_STRING* data)
{
    return X509_EXTENSION_create_by_OBJ(nullptr, obj, isCritical, data);
}

// TODO: temporarily keeping the un-prefixed signature of this method  
// to keep tests running in CI. This will be removed once the managed assemblies  
// are synced up with the native assemblies.
extern "C" void X509ExtensionDestroy(X509_EXTENSION* a)
{
    return CryptoNative_X509ExtensionDestroy(a);
}

extern "C" void CryptoNative_X509ExtensionDestroy(X509_EXTENSION* a)
{
    if (a != nullptr)
    {
        X509_EXTENSION_free(a);
    }
}

// TODO: temporarily keeping the un-prefixed signature of this method  
// to keep tests running in CI. This will be removed once the managed assemblies  
// are synced up with the native assemblies.
extern "C" int32_t X509V3ExtPrint(BIO* out, X509_EXTENSION* ext)
{
    return CryptoNative_X509V3ExtPrint(out, ext);
}

extern "C" int32_t CryptoNative_X509V3ExtPrint(BIO* out, X509_EXTENSION* ext)
{
    return X509V3_EXT_print(out, ext, X509V3_EXT_DEFAULT, /*indent*/ 0);
}

extern "C" int32_t DecodeX509BasicConstraints2Extension(const uint8_t* encoded,
                                                        int32_t encodedLength,
                                                        int32_t* certificateAuthority,
                                                        int32_t* hasPathLengthConstraint,
                                                        int32_t* pathLengthConstraint)
{
    return CryptoNative_DecodeX509BasicConstraints2Extension(encoded,
        encodedLength,
        certificateAuthority,
        hasPathLengthConstraint,
        pathLengthConstraint);
}

// TODO: temporarily keeping the un-prefixed signature of this method  
// to keep tests running in CI. This will be removed once the managed assemblies  
// are synced up with the native assemblies.
extern "C" int32_t CryptoNative_DecodeX509BasicConstraints2Extension(const uint8_t* encoded,
                                                                     int32_t encodedLength,
                                                                     int32_t* certificateAuthority,
                                                                     int32_t* hasPathLengthConstraint,
                                                                     int32_t* pathLengthConstraint)
{
    if (!certificateAuthority || !hasPathLengthConstraint || !pathLengthConstraint)
    {
        return false;
    }

    *certificateAuthority = false;
    *hasPathLengthConstraint = false;
    *pathLengthConstraint = 0;
    int32_t result = false;

    BASIC_CONSTRAINTS* constraints = d2i_BASIC_CONSTRAINTS(nullptr, &encoded, encodedLength);
    if (constraints)
    {
        *certificateAuthority = constraints->ca != 0;

        if (constraints->pathlen != nullptr)
        {
            *hasPathLengthConstraint = true;
            long pathLength = ASN1_INTEGER_get(constraints->pathlen);

            // pathLengthConstraint needs to be in the Int32 range
            assert(pathLength <= INT32_MAX);
            *pathLengthConstraint = static_cast<int32_t>(pathLength);
        }
        else
        {
            *hasPathLengthConstraint = false;
            *pathLengthConstraint = 0;
        }

        BASIC_CONSTRAINTS_free(constraints);
        result = true;
    }

    return result;
}

// TODO: temporarily keeping the un-prefixed signature of this method  
// to keep tests running in CI. This will be removed once the managed assemblies  
// are synced up with the native assemblies.
extern "C" EXTENDED_KEY_USAGE* DecodeExtendedKeyUsage(const uint8_t* buf, int32_t len)
{
    return CryptoNative_DecodeExtendedKeyUsage(buf, len);
}

extern "C" EXTENDED_KEY_USAGE* CryptoNative_DecodeExtendedKeyUsage(const uint8_t* buf, int32_t len)
{
    if (!buf || !len)
    {
        return nullptr;
    }

    return d2i_EXTENDED_KEY_USAGE(nullptr, &buf, len);
}

// TODO: temporarily keeping the un-prefixed signature of this method  
// to keep tests running in CI. This will be removed once the managed assemblies  
// are synced up with the native assemblies.
extern "C" void ExtendedKeyUsageDestory(EXTENDED_KEY_USAGE* a)
{
    return CryptoNative_ExtendedKeyUsageDestory(a);
}

extern "C" void CryptoNative_ExtendedKeyUsageDestory(EXTENDED_KEY_USAGE* a)
{
    if (a != nullptr)
    {
        EXTENDED_KEY_USAGE_free(a);
    }
}
