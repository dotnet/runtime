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
Creates an RSA key of the requested size.
*/
PALEXPORT EVP_PKEY* CryptoNative_RsaGenerateKey(int32_t keySize);

/*
Decrypt source into destination using the specified RSA key (wrapped in an EVP_PKEY) and padding/digest options.

Returns the number of bytes written to destination, -1 on error.
*/
PALEXPORT int32_t CryptoNative_RsaDecrypt(EVP_PKEY* pkey,
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
                                          const uint8_t* source,
                                          int32_t sourceLen,
                                          RsaPaddingMode padding,
                                          const EVP_MD* digest,
                                          uint8_t* destination,
                                          int32_t destinationLen);

/*
Complete the RSA signature generation for the specified hash using the provided RSA key
(wrapped in an EVP_PKEY) and padding/digest options.

Returns the number of bytes written to destination, -1 on error.
*/
PALEXPORT int32_t CryptoNative_RsaSignHash(EVP_PKEY* pkey,
                                           RsaPaddingMode padding,
                                           const EVP_MD* digest,
                                           const uint8_t* hash,
                                           int32_t hashLen,
                                           uint8_t* destination,
                                           int32_t destinationLen);

/*
Verify an RSA signature for the specified hash using the provided RSA key (wrapped in an EVP_PKEY)
and padding/digest options.

Returns 1 on a verified signature, 0 on a mismatched signature, -1 on error.
*/
PALEXPORT int32_t CryptoNative_RsaVerifyHash(EVP_PKEY* pkey,
                                             RsaPaddingMode padding,
                                             const EVP_MD* digest,
                                             const uint8_t* hash,
                                             int32_t hashLen,
                                             const uint8_t* signature,
                                             int32_t signatureLen);

/*
Shims the EVP_PKEY_get1_RSA method.

Returns the RSA instance for the EVP_PKEY.
*/
PALEXPORT RSA* CryptoNative_EvpPkeyGetRsa(EVP_PKEY* pkey);

/*
Shims the EVP_PKEY_set1_RSA method to set the RSA
instance on the EVP_KEY.

Returns 1 upon success, otherwise 0.
*/
PALEXPORT int32_t CryptoNative_EvpPkeySetRsa(EVP_PKEY* pkey, RSA* rsa);
