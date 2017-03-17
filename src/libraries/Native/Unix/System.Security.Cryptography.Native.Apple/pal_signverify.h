// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#pragma once

#include "pal_digest.h"
#include "pal_seckey.h"

#include <Security/Security.h>

/*
Generate a signature for algorithms which require only the data hash blob, like DSA and ECDSA.

Follows pal_seckey return conventions.
*/
extern "C" int32_t AppleCryptoNative_GenerateSignature(
    SecKeyRef privateKey, uint8_t* pbDataHash, int32_t cbDataHash, CFDataRef* pSignatureOut, CFErrorRef* pErrorOut);

/*
Generate a signature for algorithms which require the pair of (dataHash, algorithmId), like RSA.

Follows pal_seckey return conventions.
*/
extern "C" int32_t AppleCryptoNative_GenerateSignatureWithHashAlgorithm(SecKeyRef privateKey,
                                                                        uint8_t* pbDataHash,
                                                                        int32_t cbDataHash,
                                                                        PAL_HashAlgorithm hashAlgorithm,
                                                                        CFDataRef* pSignatureOut,
                                                                        CFErrorRef* pErrorOut);

/*
Verify a signature for algorithms which only require the data hash blob, like DSA and ECDSA.

Returns 1 when the signature is correct, 0 when it is incorrect, and otherwise
follows pal_seckey return conventions.
*/
extern "C" int32_t AppleCryptoNative_VerifySignatureWithHashAlgorithm(SecKeyRef publicKey,
                                                                      uint8_t* pbDataHash,
                                                                      int32_t cbDataHash,
                                                                      uint8_t* pbSignature,
                                                                      int32_t cbSignature,
                                                                      PAL_HashAlgorithm hashAlgorithm,
                                                                      CFErrorRef* pErrorOut);

/*
Verify a signature for algorithms which require the pair of (dataHash, algorithmId), like RSA.

Returns 1 when the signature is correct, 0 when it is incorrect, and otherwise
follows pal_seckey return conventions.
*/
extern "C" int32_t AppleCryptoNative_VerifySignature(SecKeyRef publicKey,
                                                     uint8_t* pbDataHash,
                                                     int32_t cbDataHash,
                                                     uint8_t* pbSignature,
                                                     int32_t cbSignature,
                                                     CFErrorRef* pErrorOut);
