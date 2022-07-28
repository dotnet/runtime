// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal_types.h"
#include "pal_x509_root.h"

#include <assert.h>

const char* CryptoNative_GetX509RootStorePath(uint8_t* defaultPath)
{
    assert(defaultPath != NULL);

    // No error queue impact.

    const char* dir = getenv(X509_get_default_cert_dir_env());
    *defaultPath = 0;

    if (!dir)
    {
        dir = X509_get_default_cert_dir();
        *defaultPath = 1;
    }

    return dir;
}

const char* CryptoNative_GetX509RootStoreFile(uint8_t* defaultPath)
{
    assert(defaultPath != NULL);

    // No error queue impact.

    const char* file = getenv(X509_get_default_cert_file_env());
    *defaultPath = 0;

    if (!file)
    {
        file = X509_get_default_cert_file();
        *defaultPath = 1;
    }

    return file;
}
