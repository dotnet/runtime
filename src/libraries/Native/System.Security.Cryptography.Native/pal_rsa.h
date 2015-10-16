// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#include "pal_types.h"

#include <openssl/rsa.h>

/*
Shims the RSA_new method.

Returns the new RSA instance.
*/
extern "C" RSA* RsaCreate();

/*
Shims the RSA_up_ref method.

Returns 1 upon success, otherwise 0.
*/
extern "C" int32_t RsaUpRef(RSA* rsa);

/*
Cleans up and deletes a RSA instance.

Implemented by calling RSA_free

No-op if rsa is null.
The given RSA pointer is invalid after this call.
Always succeeds.
*/
extern "C" void RsaDestroy(RSA* rsa);

/*
Shims the d2i_RSAPublicKey method and makes it easier to invoke from managed code.
*/
extern "C" RSA* DecodeRsaPublicKey(const uint8_t* buf, int32_t len);

/*
Shims the RSA_public_encrypt method.

Returns the size of the signature, or -1 on error.
*/
extern "C" int32_t RsaPublicEncrypt(int32_t flen, const uint8_t* from, uint8_t* to, RSA* rsa, int32_t useOaepPadding);

/*
Shims the RSA_private_decrypt method.

Returns the size of the signature, or -1 on error.
*/
extern "C" int32_t RsaPrivateDecrypt(int32_t flen, const uint8_t* from, uint8_t* to, RSA* rsa, int32_t useOaepPadding);

/*
Shims the RSA_size method.

Returns the RSA modulus size in bytes.
*/
extern "C" int32_t RsaSize(RSA* rsa);

/*
Shims the RSA_generate_key_ex method.

Returns 1 upon success, otherwise 0.
*/
extern "C" int32_t RsaGenerateKeyEx(RSA* rsa, int32_t bits, BIGNUM* e);

/*
Shims the RSA_sign method.

Returns 1 upon success, otherwise 0.
*/
extern "C" int32_t RsaSign(int32_t type, const uint8_t* m, int32_t m_len, uint8_t* sigret, int32_t* siglen, RSA* rsa);

/*
Shims the RSA_verify method.

Returns 1 upon success, otherwise 0.
*/
extern "C" int32_t RsaVerify(int32_t type, const uint8_t* m, int32_t m_len, uint8_t* sigbuf, int32_t siglen, RSA* rsa);
