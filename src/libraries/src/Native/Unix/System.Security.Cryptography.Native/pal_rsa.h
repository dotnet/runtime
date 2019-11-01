// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "pal_types.h"
#include "pal_compiler.h"
#include "opensslshim.h"

/*
Padding options for RsaPublicEncrypt and RsaPrivateDecrypt.
These values should be kept in sync with Interop.Crypto.RsaPadding.
*/
typedef enum
{
    Pkcs1 = 0,
    OaepSHA1 = 1,
    NoPadding = 2,
} RsaPadding;

/*
Shims the RSA_new method.

Returns the new RSA instance.
*/
DLLEXPORT RSA* CryptoNative_RsaCreate(void);

/*
Shims the RSA_up_ref method.

Returns 1 upon success, otherwise 0.
*/
DLLEXPORT int32_t CryptoNative_RsaUpRef(RSA* rsa);

/*
Cleans up and deletes a RSA instance.

Implemented by calling RSA_free

No-op if rsa is null.
The given RSA pointer is invalid after this call.
Always succeeds.
*/
DLLEXPORT void CryptoNative_RsaDestroy(RSA* rsa);

/*
Shims the d2i_RSAPublicKey method and makes it easier to invoke from managed code.
*/
DLLEXPORT RSA* CryptoNative_DecodeRsaPublicKey(const uint8_t* buf, int32_t len);

/*
Shims the RSA_public_encrypt method.

Returns the size of the signature, or -1 on error.
*/
DLLEXPORT int32_t
CryptoNative_RsaPublicEncrypt(int32_t flen, const uint8_t* from, uint8_t* to, RSA* rsa, RsaPadding padding);

/*
Shims the RSA_private_decrypt method.

Returns the size of the signature, or -1 on error.
*/
DLLEXPORT int32_t
CryptoNative_RsaPrivateDecrypt(int32_t flen, const uint8_t* from, uint8_t* to, RSA* rsa, RsaPadding padding);

/*
Shims RSA_private_encrypt with a fixed value of RSA_NO_PADDING.

Requires that the input be the size of the key.
Returns the number of bytes written (which should be flen), or -1 on error.
*/
DLLEXPORT int32_t CryptoNative_RsaSignPrimitive(int32_t flen, const uint8_t* from, uint8_t* to, RSA* rsa);

/*
Shims RSA_public_decrypt with a fixed value of RSA_NO_PADDING.

Requires that the input be the size of the key.
Returns the number of bytes written (which should be flen), or -1 on error.
*/
DLLEXPORT int32_t CryptoNative_RsaVerificationPrimitive(int32_t flen, const uint8_t* from, uint8_t* to, RSA* rsa);

/*
Shims the RSA_size method.

Returns the RSA modulus size in bytes.
*/
DLLEXPORT int32_t CryptoNative_RsaSize(RSA* rsa);

/*
Shims the RSA_generate_key_ex method.

Returns 1 upon success, otherwise 0.
*/
DLLEXPORT int32_t CryptoNative_RsaGenerateKeyEx(RSA* rsa, int32_t bits, BIGNUM* e);

/*
Shims the RSA_sign method.

Returns 1 upon success, otherwise 0.
*/
DLLEXPORT int32_t
CryptoNative_RsaSign(int32_t type, const uint8_t* m, int32_t mlen, uint8_t* sigret, int32_t* siglen, RSA* rsa);

/*
Shims the RSA_verify method.

Returns 1 upon success, otherwise 0.
*/
DLLEXPORT int32_t
CryptoNative_RsaVerify(int32_t type, const uint8_t* m, int32_t mlen, uint8_t* sigbuf, int32_t siglen, RSA* rsa);

/*
Gets all the parameters from the RSA instance.

Returns 1 upon success, otherwise 0.
*/
DLLEXPORT int32_t CryptoNative_GetRsaParameters(const RSA* rsa,
                                                const BIGNUM** n,
                                                const BIGNUM** e,
                                                const BIGNUM** d,
                                                const BIGNUM** p,
                                                const BIGNUM** dmp1,
                                                const BIGNUM** q,
                                                const BIGNUM** dmq1,
                                                const BIGNUM** iqmp);

/*
Sets all the parameters on the RSA instance.
*/
DLLEXPORT int32_t CryptoNative_SetRsaParameters(RSA* rsa,
                                              uint8_t* n,
                                              int32_t nLength,
                                              uint8_t* e,
                                              int32_t eLength,
                                              uint8_t* d,
                                              int32_t dLength,
                                              uint8_t* p,
                                              int32_t pLength,
                                              uint8_t* dmp1,
                                              int32_t dmp1Length,
                                              uint8_t* q,
                                              int32_t qLength,
                                              uint8_t* dmq1,
                                              int32_t dmq1Length,
                                              uint8_t* iqmp,
                                              int32_t iqmpLength);
