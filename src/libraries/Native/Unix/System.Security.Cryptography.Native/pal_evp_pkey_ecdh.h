// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "pal_types.h"
#include "pal_compiler.h"
#include "opensslshim.h"

DLLEXPORT EVP_PKEY_CTX* CryptoNative_EvpPKeyCtxCreate(EVP_PKEY* pkey, EVP_PKEY* peerkey, uint32_t* secretLength);

DLLEXPORT int32_t CryptoNative_EvpPKeyDeriveSecretAgreement(uint8_t* secret, uint32_t secretLength, EVP_PKEY_CTX* ctx);

DLLEXPORT void CryptoNative_EvpPKeyCtxDestroy(EVP_PKEY_CTX* ctx);
