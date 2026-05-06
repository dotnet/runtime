// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "opensslshim.h"
#include "pal_compiler.h"
#include "pal_types.h"

/*
Determines if the X25519 algorithm is supported.

Returns 1 if the algorithm is available, 0 otherwise.
*/
PALEXPORT int32_t CryptoNative_X25519Available(void);

/*
Exports the raw private key material from an X25519 EVP_PKEY.

Returns 1 on success, 0 on failure, -1 if the exported key length does not match destinationLength.
*/
PALEXPORT int32_t CryptoNative_X25519ExportPrivateKey(const EVP_PKEY* key, uint8_t* destination, int32_t destinationLength);

/*
Exports the raw public key material from an X25519 EVP_PKEY.

Returns 1 on success, 0 on failure, -1 if the exported key length does not match destinationLength.
*/
PALEXPORT int32_t CryptoNative_X25519ExportPublicKey(const EVP_PKEY* key, uint8_t* destination, int32_t destinationLength);

/*
Imports a raw private key and returns a new X25519 EVP_PKEY.

Returns the new EVP_PKEY on success, NULL on failure.
*/
PALEXPORT EVP_PKEY* CryptoNative_X25519ImportPrivateKey(const uint8_t* source, int32_t sourceLength);

/*
Imports a raw public key and returns a new X25519 EVP_PKEY.

Returns the new EVP_PKEY on success, NULL on failure.
*/
PALEXPORT EVP_PKEY* CryptoNative_X25519ImportPublicKey(const uint8_t* source, int32_t sourceLength);

/*
Generates a new X25519 key pair and returns it as an EVP_PKEY.

Returns the new EVP_PKEY on success, NULL on failure.
*/
PALEXPORT EVP_PKEY* CryptoNative_X25519GenerateKey(void);

/*
Determines if an EVP_PKEY is an X25519 key, and whether it contains private key material.

Returns 1 if the key is an X25519 key, 0 otherwise.
*/
PALEXPORT int32_t CryptoNative_X25519IsValidHandle(const EVP_PKEY* key, int32_t* hasPrivateKey);
