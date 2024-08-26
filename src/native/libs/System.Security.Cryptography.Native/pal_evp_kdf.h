// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal_compiler.h"
#include <stdint.h>
#include "opensslshim.h"

/*
Shims the EVP_KDF_free function.

kdf: The KDF to free.
note: This method will assert that the platform has EVP_KDF_free. Callers are
      responsible for ensuring the platform supports EVP_KDF_free.
*/
PALEXPORT void CryptoNative_EvpKdfFree(EVP_KDF* kdf);

/*
Shims the EVP_KDF_fetch function.

algorithm: The name of the algorithm to fetch.
haveFeature: A pointer to an int32_t. When this function returns, the value will
             contain an integer to determine if the platform supports EVP_KDF_fetch.
             0 indicates that the platform does not support EVP_KDF_fetch or the algorithm.
             1 indicates that the platform does support EVP_KDF_fetch and the algorithm.

return: A pointer to an EVP_KDF. This pointer may be NULL if OpenSSL failed to allocate internally,
        or, if the platform does not support EVP_KDF_fetch or the algorithm.
        Use the haveFeature value to determine if the NULL value is due to allocation failure
        or lack of platform support.
*/
PALEXPORT EVP_KDF* CryptoNative_EvpKdfFetch(const char* algorithm, int32_t* haveFeature);

/*
Performs a one-shot key derivation using KBKDF-HMAC.

kdf: A handle to the KBKDF algorithm.
key: A pointer to a key. This value is set using OSSL_KDF_PARAM_KEY. This value
     may be NULL if the keyLength parameter is 0.
keyLength: The length of the key in the key parameter. This value must be zero or positive.
algorithm: A null-terminated UTF-8 string representation of the HMAC algorithm to use.
           this value is set using OSSL_KDF_PARAM_DIGEST.
label: A pointer to the label. This value may be NULL if the labelLength is 0. This
       value is set using OSSL_KDF_PARAM_SALT.
labelLength: The length of the label. This value must be zero or positive.
context: A pointer to the context. This value may be NULL if the contextLength is 0. This
       value is set using OSSL_KDF_PARAM_INFO.
contextLength: The length of the context. This value must be zero or positive.
destination: The buffer which receives the derived key. This value may not be NULL, a destination
             is required. Callers are expected to early exit for empty destinations.
destinationLength: The length of the destination buffer, and the number of bytes to
                   derive from the KDF. This value must be positive.
*/
PALEXPORT int32_t CryptoNative_KbkdfHmacOneShot(
    EVP_KDF* kdf,
    uint8_t* key,
    int32_t keyLength,
    char* algorithm,
    uint8_t* label,
    int32_t labelLength,
    uint8_t* context,
    int32_t contextLength,
    uint8_t* destination,
    int32_t destinationLength);

PALEXPORT int32_t CryptoNative_HkdfDeriveKey(
    EVP_KDF* kdf,
    uint8_t* ikm,
    int32_t ikmLength,
    char* algorithm,
    uint8_t* salt,
    int32_t saltLength,
    uint8_t* info,
    int32_t infoLength,
    uint8_t* destination,
    int32_t destinationLength);

PALEXPORT int32_t CryptoNative_HkdfExpand(
    EVP_KDF* kdf,
    uint8_t* prk,
    int32_t prkLength,
    char* algorithm,
    uint8_t* info,
    int32_t infoLength,
    uint8_t* destination,
    int32_t destinationLength);

PALEXPORT int32_t CryptoNative_HkdfExtract(
    EVP_KDF* kdf,
    uint8_t* ikm,
    int32_t ikmLength,
    char* algorithm,
    uint8_t* salt,
    int32_t saltLength,
    uint8_t* destination,
    int32_t destinationLength);
