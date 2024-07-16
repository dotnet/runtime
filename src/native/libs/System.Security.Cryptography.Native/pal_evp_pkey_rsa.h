// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "opensslshim.h"
#include "pal_compiler.h"
#include "pal_types.h"

/*
Padding options for RSA.
Matches RSAEncryptionPaddingMode / RSASignaturePaddingMode.
*/
typedef enum
{
    RsaPaddingPkcs1,
    RsaPaddingOaepOrPss,
} RsaPaddingMode;

/*
Create a new EVP_PKEY* wrapping an existing RSA key.
*/
PALEXPORT EVP_PKEY* CryptoNative_EvpPKeyCreateRsa(RSA* currentKey);

/*
Creates an RSA key of the requested size.
*/
PALEXPORT EVP_PKEY* CryptoNative_RsaGenerateKey(int32_t keySize);

/*
Decrypt source into destination using the specified RSA key (wrapped in an EVP_PKEY) and padding/digest options.

Returns the number of bytes written to destination, -1 on error.
*/
PALEXPORT int32_t CryptoNative_RsaDecrypt(EVP_PKEY* pkey,
                                          void* extraHandle,
                                          const uint8_t* source,
                                          int32_t sourceLen,
                                          RsaPaddingMode padding,
                                          const EVP_MD* digest,
                                          uint8_t* destination,
                                          int32_t destinationLen);

/*
Encrypt source into destination using the specified RSA key (wrapped in an EVP_PKEY) and padding/digest options.

Returns the number of bytes written to destination, -1 on error.
*/
PALEXPORT int32_t CryptoNative_RsaEncrypt(EVP_PKEY* pkey,
                                          void* extraHandle,
                                          const uint8_t* source,
                                          int32_t sourceLen,
                                          RsaPaddingMode padding,
                                          const EVP_MD* digest,
                                          uint8_t* destination,
                                          int32_t destinationLen);

/*
Configures the EVP_PKEY_CTX for use in an RSA sign operation.
Returns 1 on success, 0 on failure.
*/
PALEXPORT int32_t CryptoNative_EvpPKeyCtxConfigureForRsaSign(EVP_PKEY_CTX* ctx, RsaPaddingMode padding, const EVP_MD* digest);

/*
Configures the EVP_PKEY_CTX for use in an RSA verify operation.
Returns 1 on success, 0 on failure.
*/
PALEXPORT int32_t CryptoNative_EvpPKeyCtxConfigureForRsaVerify(EVP_PKEY_CTX* ctx, RsaPaddingMode padding, const EVP_MD* digest);
