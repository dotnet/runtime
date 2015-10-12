// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#include "pal_x509ext.h"

extern "C" BASIC_CONSTRAINTS* DecodeBasicConstraints(const unsigned char* buf, int32_t len)
{
    if (!buf || !len)
    {
        return nullptr;
    }

    return d2i_BASIC_CONSTRAINTS(nullptr, &buf, len);
}

extern "C" EXTENDED_KEY_USAGE* DecodeExtendedKeyUsage(const unsigned char* buf, int32_t len)
{
    if (!buf || !len)
    {
        return nullptr;
    }

    return d2i_EXTENDED_KEY_USAGE(nullptr, &buf, len);
}
