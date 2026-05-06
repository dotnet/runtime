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
