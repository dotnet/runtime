// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#include "pal_ecdsa.h"
#include "pal_utilities.h"

// TODO: temporarily keeping the un-prefixed signature of this method  
// to keep tests running in CI. This will be removed once the managed assemblies  
// are synced up with the native assemblies.
extern "C" int32_t EcDsaSign(const uint8_t* dgst, int32_t dgstlen, uint8_t* sig, int32_t* siglen, EC_KEY* key)
{
    return CryptoNative_EcDsaSign(dgst, dgstlen, sig, siglen, key);
}

extern "C" int32_t
CryptoNative_EcDsaSign(const uint8_t* dgst, int32_t dgstlen, uint8_t* sig, int32_t* siglen, EC_KEY* key)
{
    if (!siglen)
    {
        return 0;
    }

    unsigned int unsignedSigLength = UnsignedCast(*siglen);
    int ret = ECDSA_sign(0, dgst, dgstlen, sig, &unsignedSigLength, key);
    *siglen = SignedCast(unsignedSigLength);
    return ret;
}

// TODO: temporarily keeping the un-prefixed signature of this method  
// to keep tests running in CI. This will be removed once the managed assemblies  
// are synced up with the native assemblies.
extern "C" int32_t
EcDsaVerify(const uint8_t* dgst, int32_t dgstlen, const uint8_t* sig, int32_t siglen, EC_KEY* key)
{
    return CryptoNative_EcDsaVerify(dgst, dgstlen, sig, siglen, key);
}

extern "C" int32_t
CryptoNative_EcDsaVerify(const uint8_t* dgst, int32_t dgstlen, const uint8_t* sig, int32_t siglen, EC_KEY* key)
{
    return ECDSA_verify(0, dgst, dgstlen, sig, siglen, key);
}

// TODO: temporarily keeping the un-prefixed signature of this method  
// to keep tests running in CI. This will be removed once the managed assemblies  
// are synced up with the native assemblies.
extern "C" int32_t EcDsaSize(const EC_KEY* key)
{
    return CryptoNative_EcDsaSize(key);
}

extern "C" int32_t CryptoNative_EcDsaSize(const EC_KEY* key)
{
    return ECDSA_size(key);
}
