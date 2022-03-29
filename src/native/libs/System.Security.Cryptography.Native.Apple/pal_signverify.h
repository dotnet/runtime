// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include "pal_digest.h"
#include "pal_seckey.h"
#include "pal_compiler.h"

#include <Security/Security.h>

enum
{
    PAL_SignatureAlgorithm_Unknown = 0,
    PAL_SignatureAlgorithm_DSA = 1,
    PAL_SignatureAlgorithm_RSA_Pkcs1 = 2,
    PAL_SignatureAlgorithm_RSA_Pss = 3,
    PAL_SignatureAlgorithm_RSA_Raw = 4,
    PAL_SignatureAlgorithm_EC = 5,
};
typedef uint32_t PAL_SignatureAlgorithm;

/*
Generate a DSA, RSA or ECDsa signature.

For DSA and ECDsa the hashAlgorithm parameter is ignored and should be set to PAL_Unknown.

Follows pal_seckey return conventions.
*/
PALEXPORT int32_t AppleCryptoNative_SecKeyCreateSignature(SecKeyRef privateKey,
                                                          uint8_t* pbDataHash,
                                                          int32_t cbDataHash,
                                                          PAL_HashAlgorithm hashAlgorithm,
                                                          PAL_SignatureAlgorithm signatureAlgorithm,
                                                          CFDataRef* pSignatureOut,
                                                          CFErrorRef* pErrorOut);

/*
Verify a DSA, RSA or ECDsa signature.

For DSA and ECDsa the hashAlgorithm parameter is ignored and should be set to PAL_Unknown.

Follows pal_seckey return conventions.
*/
PALEXPORT int32_t AppleCryptoNative_SecKeyVerifySignature(SecKeyRef publicKey,
                                                          uint8_t* pbDataHash,
                                                          int32_t cbDataHash,
                                                          uint8_t* pbSignature,
                                                          int32_t cbSignature,
                                                          PAL_HashAlgorithm hashAlgorithm,
                                                          PAL_SignatureAlgorithm signatureAlgorithm,
                                                          CFErrorRef* pErrorOut);
