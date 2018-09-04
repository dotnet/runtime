// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "pal_types.h"
#include "pal_x509_root.h"

#include <assert.h>

const char* CryptoNative_GetX509RootStorePath()
{
    const char* dir = getenv(X509_get_default_cert_dir_env());

    if (!dir)
    {
        dir = X509_get_default_cert_dir();
    }

    return dir;
}

const char* CryptoNative_GetX509RootStoreFile()
{
    const char* file = getenv(X509_get_default_cert_file_env());

    if (!file)
    {
        file = X509_get_default_cert_file();
    }

    return file;
}
