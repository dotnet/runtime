// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#include "pal_rsa.h"

extern "C" RSA* DecodeRsaPublicKey(const unsigned char* buf, int32_t len)
{
    if (!buf || !len)
    {
        return nullptr;
    }

    return d2i_RSAPublicKey(nullptr, &buf, len);
}
