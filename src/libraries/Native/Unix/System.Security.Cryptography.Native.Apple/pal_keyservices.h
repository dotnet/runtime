// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#pragma once

#include "pal_digest.h"
#include "pal_seckey.h"
#include "pal_compiler.h"

#include <Security/Security.h>

enum
{
    PAL_SignatureAlgorithm_Unknown = 0,
    PAL_SignatureAlgorithm_RSA_Pkcs1 = 1,
    PAL_SignatureAlgorithm_EC = 2,
    PAL_SignatureAlgorithm_DSA = 3,
};
typedef uint32_t PAL_SignatureAlgorithm;

enum
{
    PAL_KeyAlgorithm_Unknown = 0,
    PAL_KeyAlgorithm_EC = 1,
    PAL_KeyAlgorithm_RSA = 2,
};
typedef uint32_t PAL_KeyAlgorithm;

PALEXPORT int32_t AppleCryptoNative_CreateDataKey(uint8_t* pKey,
                                                  int32_t cbKey,
                                                  PAL_KeyAlgorithm keyAlgorithm,
                                                  int32_t isPublic,
                                                  SecKeyRef* pKeyOut,
                                                  CFErrorRef* pErrorOut);

PALEXPORT int32_t AppleCryptoNative_SecKeyCreateSignature(SecKeyRef privateKey,
                                                          uint8_t* pbDataHash,
                                                          int32_t cbDataHash,
                                                          PAL_HashAlgorithm hashAlgorithm,
                                                          PAL_SignatureAlgorithm signatureAlgorithm,
                                                          int32_t digest,
                                                          CFDataRef* pSignatureOut,
                                                          CFErrorRef* pErrorOut);

PALEXPORT int32_t AppleCryptoNative_SecKeyVerifySignature(SecKeyRef publicKey,
                                                          uint8_t* pbDataHash,
                                                          int32_t cbDataHash,
                                                          uint8_t* pbSignature,
                                                          int32_t cbSignature,
                                                          PAL_HashAlgorithm hashAlgorithm,
                                                          PAL_SignatureAlgorithm signatureAlgorithm,
                                                          int digest,
                                                          CFErrorRef* pErrorOut);
