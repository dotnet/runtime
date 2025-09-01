// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "opensslshim.h"
#include "pal_compiler.h"
#include "pal_types.h"

typedef enum
{
    PalMLDsaId_Unknown = 0,
    PalMLDsaId_MLDsa44 = 1,
    PalMLDsaId_MLDsa65 = 2,
    PalMLDsaId_MLDsa87 = 3,
} PalMLDsaId;

PALEXPORT int32_t CryptoNative_MLDsaGetPalId(const EVP_PKEY* pKey, int32_t* mldsaId, int32_t* hasSeed, int32_t* hasSecretKey);

/*
Generates a new EVP_PKEY with random parameters or if seed is not NULL, uses the seed to generate the key.
The keyType is the type of the key (e.g., "ML-DSA-65").
*/
PALEXPORT EVP_PKEY* CryptoNative_MLDsaGenerateKey(const char* keyType, uint8_t* seed, int32_t seedLen);

/*
Sign a message using the provided ML-DSA key.

Returns 1 on success, 0 on signing failure, -1 on other error.
*/
PALEXPORT int32_t CryptoNative_MLDsaSignPure(EVP_PKEY *pkey,
                                             void* extraHandle,
                                             uint8_t* msg, int32_t msgLen,
                                             uint8_t* context, int32_t contextLen,
                                             uint8_t* destination, int32_t destinationLen);

/*
Verify a message using the provided ML-DSA key.

Returns 1 on a verified signature, 0 on a mismatched signature, -1 on error.
*/
PALEXPORT int32_t CryptoNative_MLDsaVerifyPure(EVP_PKEY *pkey,
                                               void* extraHandle,
                                               uint8_t* msg, int32_t msgLen,
                                               uint8_t* context, int32_t contextLen,
                                               uint8_t* sig, int32_t sigLen);

/*
Sign an encoded message using the provided ML-DSA key.

Returns 1 on success, 0 on signing failure, -1 on other error.
*/
PALEXPORT int32_t CryptoNative_MLDsaSignPreEncoded(EVP_PKEY *pkey,
                                                   void* extraHandle,
                                                   uint8_t* msg, int32_t msgLen,
                                                   uint8_t* destination, int32_t destinationLen);

/*
Verify an encoded message using the provided ML-DSA key.

Returns 1 on a verified signature, 0 on a mismatched signature, -1 on error.
*/   
PALEXPORT int32_t CryptoNative_MLDsaVerifyPreEncoded(EVP_PKEY *pkey,
                                                     void* extraHandle,
                                                     uint8_t* msg, int32_t msgLen,
                                                     uint8_t* sig, int32_t sigLen);

/*
Sign an externally produced signature mu with the provided ML-DSA key.

Returns 1 on success, 0 on signing failure, -1 on other error.
*/
PALEXPORT int32_t CryptoNative_MLDsaSignExternalMu(EVP_PKEY* pKey,
                                                   void* extraHandle,
                                                   uint8_t* mu, int32_t muLen,
                                                   uint8_t* destination, int32_t destinationLen);

/*
Verifies an externally produced signature mu with the provided ML-DSA key.

Returns 1 on success, 0 on mismatched signature, -1 on error.
*/
PALEXPORT int32_t CryptoNative_MLDsaVerifyExternalMu(EVP_PKEY* pKey,
                                                     void* extraHandle,
                                                     uint8_t* mu, int32_t muLen,
                                                     uint8_t* sig, int32_t sigLen);

/*
Export the secret key from the given ML-DSA key.
*/
PALEXPORT int32_t CryptoNative_MLDsaExportSecretKey(const EVP_PKEY* pKey, uint8_t* destination, int32_t destinationLength);

/*
Export the seed from the given ML-DSA key which can be used to generate secret key.
*/
PALEXPORT int32_t CryptoNative_MLDsaExportSeed(const EVP_PKEY* pKey, uint8_t* destination, int32_t destinationLength);

/*
Export the public key from the given ML-DSA key.
*/
PALEXPORT int32_t CryptoNative_MLDsaExportPublicKey(const EVP_PKEY* pKey, uint8_t* destination, int32_t destinationLength);
