// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "pal_ecdsa.h"
#include "pal_utilities.h"

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

extern "C" int32_t
CryptoNative_EcDsaVerify(const uint8_t* dgst, int32_t dgstlen, const uint8_t* sig, int32_t siglen, EC_KEY* key)
{
    return ECDSA_verify(0, dgst, dgstlen, sig, siglen, key);
}

extern "C" int32_t CryptoNative_EcDsaSize(const EC_KEY* key)
{
    return ECDSA_size(key);
}
