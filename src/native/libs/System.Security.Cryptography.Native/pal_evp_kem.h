// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal_compiler.h"
#include <stdint.h>
#include "opensslshim.h"

PALEXPORT void CryptoNative_EvpKemFree(EVP_KEM* kem);

PALEXPORT EVP_KEM* CryptoNative_EvpKemFetch(const char* algorithm, int32_t* haveFeature);
PALEXPORT EVP_PKEY* CryptoNative_EvpKemGeneratePkey(const EVP_KEM* kem, uint8_t* seed, int32_t seedLength);
PALEXPORT int32_t CryptoNative_EvpKemExportPrivateSeed(const EVP_PKEY* pKey, uint8_t* destination, int32_t destinationLength);
PALEXPORT int32_t CryptoNative_EvpKemExportDecapsulationKey(const EVP_PKEY* pKey, uint8_t* destination, int32_t destinationLength);
PALEXPORT int32_t CryptoNative_EvpKemExportEncapsulationKey(const EVP_PKEY* pKey, uint8_t* destination, int32_t destinationLength);
PALEXPORT int32_t CryptoNative_EvpKemEncapsulate(EVP_PKEY* pKey,
                                                 uint8_t* ciphertext,
                                                 int32_t ciphertextLength,
                                                 uint8_t* sharedSecret,
                                                 int32_t sharedSecretLength);
PALEXPORT int32_t CryptoNative_EvpKemDecapsulate(EVP_PKEY* pKey,
                                                 const uint8_t* ciphertext,
                                                 int32_t ciphertextLength,
                                                 uint8_t* sharedSecret,
                                                 int32_t sharedSecretLength);
