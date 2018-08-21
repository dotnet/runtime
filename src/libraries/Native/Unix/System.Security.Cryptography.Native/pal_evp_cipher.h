// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "pal_types.h"
#include "pal_compiler.h"
#include "opensslshim.h"

/*
Creates and initializes an EVP_CIPHER_CTX with the given args.

Implemented by:
   1) allocating a new EVP_CIPHER_CTX
   2) calling EVP_CIPHER_CTX_init on the new EVP_CIPHER_CTX
   3) calling EVP_CipherInit_ex with the new EVP_CIPHER_CTX and the given args.

Returns new EVP_CIPHER_CTX on success, nullptr on failure.
*/
DLLEXPORT EVP_CIPHER_CTX*
CryptoNative_EvpCipherCreate(const EVP_CIPHER* type, uint8_t* key, unsigned char* iv, int32_t enc);

DLLEXPORT EVP_CIPHER_CTX*
CryptoNative_EvpCipherCreate2(const EVP_CIPHER* type, uint8_t* key, int32_t keyLength, int32_t effectiveKeyLength, unsigned char* iv, int32_t enc);

DLLEXPORT EVP_CIPHER_CTX*
CryptoNative_EvpCipherCreatePartial(const EVP_CIPHER* type);

DLLEXPORT int32_t CryptoNative_EvpCipherSetKeyAndIV(EVP_CIPHER_CTX* ctx, uint8_t* key, unsigned char* iv, int32_t enc);

DLLEXPORT int32_t CryptoNative_EvpCipherSetGcmNonceLength(EVP_CIPHER_CTX* ctx, int32_t ivLength);
DLLEXPORT int32_t CryptoNative_EvpCipherSetCcmNonceLength(EVP_CIPHER_CTX* ctx, int32_t ivLength);

/*
Cleans up and deletes an EVP_CIPHER_CTX instance created by EvpCipherCreate.

Implemented by:
  1) Calling EVP_CIPHER_CTX_cleanup
  2) Deleting the EVP_CIPHER_CTX instance.

No-op if ctx is null.
The given EVP_CIPHER_CTX pointer is invalid after this call.
Always succeeds.
*/
DLLEXPORT void CryptoNative_EvpCipherDestroy(EVP_CIPHER_CTX* ctx);

/*
Function:
EvpCipherReset

Resets an EVP_CIPHER_CTX instance for a new computation.
*/
DLLEXPORT int32_t CryptoNative_EvpCipherReset(EVP_CIPHER_CTX* ctx);

/*
Function:
EvpCipherCtxSetPadding

Direct shim to EVP_CIPHER_CTX_set_padding.
*/
DLLEXPORT int32_t CryptoNative_EvpCipherCtxSetPadding(EVP_CIPHER_CTX* x, int32_t padding);

/*
Function:
EvpCipherUpdate

Direct shim to EVP_CipherUpdate.
*/
DLLEXPORT int32_t
CryptoNative_EvpCipherUpdate(EVP_CIPHER_CTX* ctx, uint8_t* out, int32_t* outl, unsigned char* in, int32_t inl);

/*
Function:
EvpCipherFinalEx

Direct shim to EVP_CipherFinal_ex.
*/
DLLEXPORT int32_t CryptoNative_EvpCipherFinalEx(EVP_CIPHER_CTX* ctx, uint8_t* outm, int32_t* outl);

/*
Function:
EvpAesGcmGetTag

Retrieves tag for authenticated encryption
*/
DLLEXPORT int32_t CryptoNative_EvpCipherGetGcmTag(EVP_CIPHER_CTX* ctx, uint8_t* tag, int32_t tagLength);

/*
Function:
EvpAesGcmSetTag

Sets tag for authenticated decryption
*/
DLLEXPORT int32_t CryptoNative_EvpCipherSetGcmTag(EVP_CIPHER_CTX* ctx, uint8_t* tag, int32_t tagLength);

/*
Function:
EvpAesCcmGetTag

Retrieves tag for authenticated encryption
*/
DLLEXPORT int32_t CryptoNative_EvpCipherGetCcmTag(EVP_CIPHER_CTX* ctx, uint8_t* tag, int32_t tagLength);

/*
Function:
EvpAesCcmSetTag

Sets tag for authenticated decryption
*/
DLLEXPORT int32_t CryptoNative_EvpCipherSetCcmTag(EVP_CIPHER_CTX* ctx, uint8_t* tag, int32_t tagLength);

/*
Function:
EvpAes128Ecb

Direct shim to EVP_aes_128_ecb.
*/
DLLEXPORT const EVP_CIPHER* CryptoNative_EvpAes128Ecb(void);

/*
Function:
EvpAes128Cbc

Direct shim to EVP_aes_128_cbc.
*/
DLLEXPORT const EVP_CIPHER* CryptoNative_EvpAes128Cbc(void);

/*
Function:
EvpAes128Gcm

Direct shim to EVP_aes_128_gcm.
*/
DLLEXPORT const EVP_CIPHER* CryptoNative_EvpAes128Gcm(void);

/*
Function:
EvpAes128Ccm

Direct shim to EVP_aes_128_ccm.
*/
DLLEXPORT const EVP_CIPHER* CryptoNative_EvpAes128Ccm(void);

/*
Function:
EvpAes192Ecb

Direct shim to EVP_aes_192_ecb.
*/
DLLEXPORT const EVP_CIPHER* CryptoNative_EvpAes192Ecb(void);

/*
Function:
EvpAes192Cbc

Direct shim to EVP_aes_192_cbc.
*/
DLLEXPORT const EVP_CIPHER* CryptoNative_EvpAes192Cbc(void);

/*
Function:
EvpAes192Gcm

Direct shim to EVP_aes_192_gcm.
*/
DLLEXPORT const EVP_CIPHER* CryptoNative_EvpAes192Gcm(void);

/*
Function:
EvpAes192Ccm

Direct shim to EVP_aes_192_ccm.
*/
DLLEXPORT const EVP_CIPHER* CryptoNative_EvpAes192Ccm(void);

/*
Function:
EvpAes256Ecb

Direct shim to EVP_aes_256_ecb.
*/
DLLEXPORT const EVP_CIPHER* CryptoNative_EvpAes256Ecb(void);

/*
Function:
EvpAes256Cbc

Direct shim to EVP_aes_256_cbc.
*/
DLLEXPORT const EVP_CIPHER* CryptoNative_EvpAes256Cbc(void);

/*
Function:
EvpAes256Gcm

Direct shim to EVP_aes_256_gcm.
*/
DLLEXPORT const EVP_CIPHER* CryptoNative_EvpAes256Gcm(void);

/*
Function:
EvpAes256Ccm

Direct shim to EVP_aes_256_ccm.
*/
DLLEXPORT const EVP_CIPHER* CryptoNative_EvpAes256Ccm(void);

/*
Function:
EvpDes3Ecb

Direct shim to EVP_des_ede3.
*/
DLLEXPORT const EVP_CIPHER* CryptoNative_EvpDes3Ecb(void);

/*
Function:
EvpDes3Cbc

Direct shim to EVP_des_ede3_cbc.
*/
DLLEXPORT const EVP_CIPHER* CryptoNative_EvpDes3Cbc(void);

/*
Function:
EvpDesEcb

Direct shim to EVP_des_ecb.
*/
DLLEXPORT const EVP_CIPHER* CryptoNative_EvpDesEcb(void);

/*
Function:
EvpDesCbc

Direct shim to EVP_des_ede_cbc.
*/
DLLEXPORT const EVP_CIPHER* CryptoNative_EvpDesCbc(void);

/*
Function:
EvpRC2Ecb

Direct shim to EVP_rc2_ecb.
*/
DLLEXPORT const EVP_CIPHER* CryptoNative_EvpRC2Ecb(void);

/*
Function:
EvpRC2Cbc

Direct shim to EVP_des_rc2_cbc.
*/
DLLEXPORT const EVP_CIPHER* CryptoNative_EvpRC2Cbc(void);
