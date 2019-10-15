// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "pal_bignum.h"

void CryptoNative_BigNumDestroy(BIGNUM* a)
{
    if (a != NULL)
    {
        BN_clear_free(a);
    }
}

BIGNUM* CryptoNative_BigNumFromBinary(const uint8_t* s, int32_t len)
{
    if (!s || !len)
    {
        return NULL;
    }

    return BN_bin2bn(s, len, NULL);
}

int32_t CryptoNative_BigNumToBinary(const BIGNUM* a, uint8_t* to)
{
    if (!a || !to)
    {
        return 0;
    }

    return BN_bn2bin(a, to);
}

int32_t CryptoNative_GetBigNumBytes(const BIGNUM* a)
{
    if (!a)
    {
        return 0;
    }

    return BN_num_bytes(a);
}
