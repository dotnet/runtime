// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "opensslshim.h"
#include "pal_compiler.h"
#include "pal_types.h"

/*
Creates an EVP_PKEY* from an existing DSA*
*/
PALEXPORT EVP_PKEY* CryptoNative_EvpPKeyCreateDsa(DSA* currentKey);

/*
Generate a new DSA key whose P/G/Y values are of size keySize, and the Q/X values
are appropriate for the key size.
*/
PALEXPORT EVP_PKEY* CryptoNative_DsaGenerateKey(int32_t keySize);

/*
Returns the size of the q parameter in bytes.
*/
PALEXPORT int32_t CryptoNative_DsaSizeQ(EVP_PKEY* pkey);

/*
Complete the DSA signature generation for the specified hash using the provided DSA key
(wrapped in an EVP_PKEY).

Returns the number of bytes written to destination, -1 on error.
*/
PALEXPORT int32_t CryptoNative_DsaSignHash(
    EVP_PKEY* pkey, const uint8_t* hash, int32_t hashLen, uint8_t* destination, int32_t destinationLen);
