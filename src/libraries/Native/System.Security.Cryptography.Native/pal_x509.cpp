// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#include "pal_x509.h"

extern "C" EVP_PKEY* GetX509EvpPublicKey(X509* x509)
{
    if (!x509)
    {
        return nullptr;
    }

    // X509_get_X509_PUBKEY returns an interior pointer, so should not be freed
    return X509_PUBKEY_get(X509_get_X509_PUBKEY(x509));
}
