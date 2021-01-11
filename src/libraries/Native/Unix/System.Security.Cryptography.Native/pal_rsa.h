// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "opensslshim.h"
#include "pal_compiler.h"
#include "pal_types.h"

/*
Padding options for RSA sign/verify and encrypt/decrypt
These values should be kept in sync with Interop.Crypto.RsaPadding.
*/
typedef enum
{
    Pkcs1 = 0,
    OaepOrPss = 1,
} RsaPadding;

/*
Imports a SubjectPublicKeyInfo blob as an RSA-based EVP_PKEY
*/
PALEXPORT EVP_PKEY* CryptoNative_DecodeRsaSpki(const uint8_t* buf, int32_t len);

/*
Imports a PKCS#8 blob as an RSA-based EVP_PKEY.
*/
PALEXPORT EVP_PKEY* CryptoNative_DecodeRsaPkcs8(const uint8_t* buf, int32_t len);

/*
Encrypt data with an RSA key.

Returns a negative number on error, otherwise the number of bytes written to destination.
*/
PALEXPORT int32_t CryptoNative_RsaEncrypt(EVP_PKEY* pkey,
                                          const uint8_t* data,
                                          int32_t dataLen,
                                          RsaPadding padding,
                                          const EVP_MD* digest,
                                          uint8_t* destination);

/*
Decrypt data with an RSA key.

Returns a negative number on error, otherwise the number of bytes written to destination.
*/
PALEXPORT int32_t CryptoNative_RsaDecrypt(EVP_PKEY* pkey,
                                          const uint8_t* data,
                                          int32_t dataLen,
                                          RsaPadding padding,
                                          const EVP_MD* digest,
                                          uint8_t* destination);

/*
Generates an RSA-based EVP_PKEY public/private pair with a modulus of the specified size (in bits).
The public exponent of this key is F4 (0x010001)
*/
PALEXPORT EVP_PKEY* CryptoNative_RsaGenerateKey(int32_t keySize);

/*
Signs a hash using the specified padding algorithm (RSASSA-PKCS1_v1.5 or RSASSA-PSS).

For PSS, the salt length is the digest length, and the MGF1 digest is the same as the data digest.

Returns 1 upon success, otherwise 0.
*/
PALEXPORT int32_t CryptoNative_RsaSignHash(EVP_PKEY* pkey,
                                           RsaPadding padding,
                                           const EVP_MD* digest,
                                           const uint8_t* hash,
                                           int32_t hashLen,
                                           uint8_t* dest,
                                           int32_t* sigLen);

/*
Verifies a hash using the specified padding algorithm (RSASSA-PKCS1_v1.5 or RSASSA-PSS).

For PSS, the salt length is the digest length, and the MGF1 digest is the same as the data digest.

Returns 1 on success, 0 on signature failure, INT_MIN on a usage error, -1 on an OpenSSL error.
*/
PALEXPORT int32_t CryptoNative_RsaVerifyHash(EVP_PKEY* pkey,
                                             RsaPadding padding,
                                             const EVP_MD* digest,
                                             const uint8_t* hash,
                                             int32_t hashLen,
                                             uint8_t* signature,
                                             int32_t sigLen);

/*
Returns a BIO containing the RSAPublicKey format of the provided key, or NULL on error.
*/
PALEXPORT BIO* CryptoNative_ExportRSAPublicKey(EVP_PKEY* pkey);

/*
Returns a BIO containing the RSAPublicKey format of the provided key, or NULL on error.
*/
PALEXPORT BIO* CryptoNative_ExportRSAPrivateKey(EVP_PKEY* pkey);
