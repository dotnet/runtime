// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal_compiler.h"
#include <stdint.h>
#include "opensslshim.h"

PALEXPORT void CryptoNative_EvpKemFree(EVP_KEM* kem);

PALEXPORT EVP_KEM* CryptoNative_EvpKemFetch(const char* algorithm, int32_t* haveFeature);
