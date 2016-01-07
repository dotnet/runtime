// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#include "pal_bignum.h"

// TODO: temporarily keeping the un-prefixed signature of this method  
// to keep tests running in CI. This will be removed once the managed assemblies  
// are synced up with the native assemblies.
extern "C" void BigNumDestroy(BIGNUM* a)
{
    return CryptoNative_BigNumDestroy(a);
}

extern "C" void CryptoNative_BigNumDestroy(BIGNUM* a)
{
    if (a != nullptr)
    {
        BN_clear_free(a);
    }
}

// TODO: temporarily keeping the un-prefixed signature of this method  
// to keep tests running in CI. This will be removed once the managed assemblies  
// are synced up with the native assemblies.
extern "C" BIGNUM* BigNumFromBinary(const uint8_t* s, int32_t len)
{
    return CryptoNative_BigNumFromBinary(s, len);
}

extern "C" BIGNUM* CryptoNative_BigNumFromBinary(const uint8_t* s, int32_t len)
{
    if (!s || !len)
    {
        return nullptr;
    }

    return BN_bin2bn(s, len, nullptr);
}

// TODO: temporarily keeping the un-prefixed signature of this method  
// to keep tests running in CI. This will be removed once the managed assemblies  
// are synced up with the native assemblies.
extern "C" int32_t BigNumToBinary(const BIGNUM* a, uint8_t* to)
{
    return CryptoNative_BigNumToBinary(a, to);
}

extern "C" int32_t CryptoNative_BigNumToBinary(const BIGNUM* a, uint8_t* to)
{
    if (!a || !to)
    {
        return 0;
    }

    return BN_bn2bin(a, to);
}

// TODO: temporarily keeping the un-prefixed signature of this method  
// to keep tests running in CI. This will be removed once the managed assemblies  
// are synced up with the native assemblies.
extern "C" int32_t GetBigNumBytes(const BIGNUM* a)
{
    return CryptoNative_GetBigNumBytes(a);
}

extern "C" int32_t CryptoNative_GetBigNumBytes(const BIGNUM* a)
{
    if (!a)
    {
        return 0;
    }

    return BN_num_bytes(a);
}
