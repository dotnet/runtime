// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal_types.h"
#include "pal_compiler.h"
#include "opensslshim.h"

PALEXPORT int32_t CryptoNative_EvpPKeyDeriveSecretAgreement(EVP_PKEY* pkey, void* extraHandle, EVP_PKEY* peerKey, uint8_t* secret, uint32_t secretLength);
