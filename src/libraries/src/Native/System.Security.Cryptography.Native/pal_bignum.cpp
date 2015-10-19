// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#include "pal_bignum.h"

extern "C" void BigNumDestroy(BIGNUM* a)
{
    if (a != nullptr)
    {
        BN_clear_free(a);
    }
}

extern "C" BIGNUM* BigNumFromBinary(const uint8_t* s, int32_t len)
{
    if (!s || !len)
    {
        return nullptr;
    }

    return BN_bin2bn(s, len, nullptr);
}

extern "C" int32_t BigNumToBinary(const BIGNUM* a, uint8_t* to)
{
    if (!a || !to)
    {
        return 0;
    }

    return BN_bn2bin(a, to);
}

extern "C" int32_t GetBigNumBytes(const BIGNUM* a)
{
    if (!a)
    {
        return 0;
    }

    return BN_num_bytes(a);
}
