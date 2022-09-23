// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal_x509ext.h"

#include <stdbool.h>
#include <assert.h>

X509_EXTENSION*
CryptoNative_X509ExtensionCreateByObj(ASN1_OBJECT* obj, int32_t isCritical, ASN1_OCTET_STRING* data)
{
    ERR_clear_error();
    return X509_EXTENSION_create_by_OBJ(NULL, obj, isCritical, data);
}

void CryptoNative_X509ExtensionDestroy(X509_EXTENSION* a)
{
    if (a != NULL)
    {
        X509_EXTENSION_free(a);
    }
}

int32_t CryptoNative_X509V3ExtPrint(BIO* out, X509_EXTENSION* ext)
{
    ERR_clear_error();
    return X509V3_EXT_print(out, ext, X509V3_EXT_DEFAULT, /*indent*/ 0);
}

int32_t CryptoNative_DecodeX509BasicConstraints2Extension(const uint8_t* encoded,
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

    ERR_clear_error();

    BASIC_CONSTRAINTS* constraints = d2i_BASIC_CONSTRAINTS(NULL, &encoded, encodedLength);
    if (constraints)
    {
        *certificateAuthority = constraints->ca != 0;

        if (constraints->pathlen != NULL)
        {
            *hasPathLengthConstraint = true;
            long pathLength = ASN1_INTEGER_get(constraints->pathlen);

            // pathLengthConstraint needs to be in the Int32 range
            assert(pathLength <= INT32_MAX);
            *pathLengthConstraint = (int32_t)pathLength;
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

EXTENDED_KEY_USAGE* CryptoNative_DecodeExtendedKeyUsage(const uint8_t* buf, int32_t len)
{
    ERR_clear_error();

    if (!buf || !len)
    {
        return NULL;
    }

    return d2i_EXTENDED_KEY_USAGE(NULL, &buf, len);
}

void CryptoNative_ExtendedKeyUsageDestroy(EXTENDED_KEY_USAGE* a)
{
    if (a != NULL)
    {
        EXTENDED_KEY_USAGE_free(a);
    }
}
