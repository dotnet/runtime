// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "pal_types.h"
#include "opensslshim.h"

/*
Creates and initializes an EVP_CIPHER_CTX with the given args.

Implemented by:
   1) allocating a new EVP_CIPHER_CTX
   2) calling EVP_CIPHER_CTX_init on the new EVP_CIPHER_CTX
   3) calling EVP_CipherInit_ex with the new EVP_CIPHER_CTX and the given args.

Returns new EVP_CIPHER_CTX on success, nullptr on failure.
*/
extern "C" EVP_CIPHER_CTX*
CryptoNative_EvpCipherCreate(const EVP_CIPHER* type, uint8_t* key, unsigned char* iv, int32_t enc);

extern "C" EVP_CIPHER_CTX*
CryptoNative_EvpCipherCreate2(const EVP_CIPHER* type, uint8_t* key, int32_t keyLength, int32_t effectiveKeyLength, unsigned char* iv, int32_t enc);
/*
Cleans up and deletes an EVP_CIPHER_CTX instance created by EvpCipherCreate.

Implemented by:
  1) Calling EVP_CIPHER_CTX_cleanup
  2) Deleting the EVP_CIPHER_CTX instance.

No-op if ctx is null.
The given EVP_CIPHER_CTX pointer is invalid after this call.
Always succeeds.
*/
extern "C" void CryptoNative_EvpCipherDestroy(EVP_CIPHER_CTX* ctx);

/*
Function:
EvpCipherReset

Resets an EVP_CIPHER_CTX instance for a new computation.
*/
extern "C" int32_t CryptoNative_EvpCipherReset(EVP_CIPHER_CTX* ctx);

/*
Function:
EvpCipherCtxSetPadding

Direct shim to EVP_CIPHER_CTX_set_padding.
*/
extern "C" int32_t CryptoNative_EvpCipherCtxSetPadding(EVP_CIPHER_CTX* x, int32_t padding);

/*
Function:
EvpCipherUpdate

Direct shim to EVP_CipherUpdate.
*/
extern "C" int32_t
CryptoNative_EvpCipherUpdate(EVP_CIPHER_CTX* ctx, uint8_t* out, int32_t* outl, unsigned char* in, int32_t inl);

/*
Function:
EvpCipherFinalEx

Direct shim to EVP_CipherFinal_ex.
*/
extern "C" int32_t CryptoNative_EvpCipherFinalEx(EVP_CIPHER_CTX* ctx, uint8_t* outm, int32_t* outl);

/*
Function:
EvpAes128Ecb

Direct shim to EVP_aes_128_ecb.
*/
extern "C" const EVP_CIPHER* CryptoNative_EvpAes128Ecb();

/*
Function:
EvpAes128Cbc

Direct shim to EVP_aes_128_cbc.
*/
extern "C" const EVP_CIPHER* CryptoNative_EvpAes128Cbc();

/*
Function:
EvpAes192Ecb

Direct shim to EVP_aes_192_ecb.
*/
extern "C" const EVP_CIPHER* CryptoNative_EvpAes192Ecb();

/*
Function:
EvpAes192Cbc

Direct shim to EVP_aes_192_cbc.
*/
extern "C" const EVP_CIPHER* CryptoNative_EvpAes192Cbc();

/*
Function:
EvpAes256Ecb

Direct shim to EVP_aes_256_ecb.
*/
extern "C" const EVP_CIPHER* CryptoNative_EvpAes256Ecb();

/*
Function:
EvpAes256Cbc

Direct shim to EVP_aes_256_cbc.
*/
extern "C" const EVP_CIPHER* CryptoNative_EvpAes256Cbc();

/*
Function:
EvpDes3Ecb

Direct shim to EVP_des_ede3.
*/
extern "C" const EVP_CIPHER* CryptoNative_EvpDes3Ecb();

/*
Function:
EvpDes3Cbc

Direct shim to EVP_des_ede3_cbc.
*/
extern "C" const EVP_CIPHER* CryptoNative_EvpDes3Cbc();

/*
Function:
EvpDesEcb

Direct shim to EVP_des_ecb.
*/
extern "C" const EVP_CIPHER* CryptoNative_EvpDesEcb();

/*
Function:
EvpDesCbc

Direct shim to EVP_des_ede_cbc.
*/
extern "C" const EVP_CIPHER* CryptoNative_EvpDesCbc();

/*
Function:
EvpRC2Ecb

Direct shim to EVP_rc2_ecb.
*/
extern "C" const EVP_CIPHER* CryptoNative_EvpRC2Ecb();

/*
Function:
EvpRC2Cbc

Direct shim to EVP_des_rc2_cbc.
*/
extern "C" const EVP_CIPHER* CryptoNative_EvpRC2Cbc();
