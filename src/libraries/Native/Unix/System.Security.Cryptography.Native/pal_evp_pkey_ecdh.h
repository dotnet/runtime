// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal_types.h"
#include "pal_compiler.h"
#include "opensslshim.h"

PALEXPORT EVP_PKEY_CTX* CryptoNative_EvpPKeyCtxCreate(EVP_PKEY* pkey, EVP_PKEY* peerkey, uint32_t* secretLength);

PALEXPORT int32_t CryptoNative_EvpPKeyDeriveSecretAgreement(uint8_t* secret, uint32_t secretLength, EVP_PKEY_CTX* ctx);

PALEXPORT void CryptoNative_EvpPKeyCtxDestroy(EVP_PKEY_CTX* ctx);
