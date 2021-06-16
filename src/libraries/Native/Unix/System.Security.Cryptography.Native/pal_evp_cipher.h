// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal_types.h"
#include "pal_compiler.h"
#include "opensslshim.h"

PALEXPORT EVP_CIPHER_CTX*
CryptoNative_EvpCipherCreate2(const EVP_CIPHER* type, uint8_t* key, int32_t keyLength, int32_t effectiveKeyLength, unsigned char* iv, int32_t enc);

PALEXPORT EVP_CIPHER_CTX*
CryptoNative_EvpCipherCreatePartial(const EVP_CIPHER* type);

PALEXPORT int32_t CryptoNative_EvpCipherSetKeyAndIV(EVP_CIPHER_CTX* ctx, uint8_t* key, unsigned char* iv, int32_t enc);

PALEXPORT int32_t CryptoNative_EvpCipherSetGcmNonceLength(EVP_CIPHER_CTX* ctx, int32_t ivLength);
PALEXPORT int32_t CryptoNative_EvpCipherSetCcmNonceLength(EVP_CIPHER_CTX* ctx, int32_t ivLength);

/*
Cleans up and deletes an EVP_CIPHER_CTX instance created by EvpCipherCreate.

Implemented by:
  1) Calling EVP_CIPHER_CTX_cleanup
  2) Deleting the EVP_CIPHER_CTX instance.

No-op if ctx is null.
The given EVP_CIPHER_CTX pointer is invalid after this call.
Always succeeds.
*/
PALEXPORT void CryptoNative_EvpCipherDestroy(EVP_CIPHER_CTX* ctx);

/*
Function:
EvpCipherReset

Resets an EVP_CIPHER_CTX instance for a new computation.
*/
PALEXPORT int32_t CryptoNative_EvpCipherReset(EVP_CIPHER_CTX* ctx);

/*
Function:
EvpCipherCtxSetPadding

Direct shim to EVP_CIPHER_CTX_set_padding.
*/
PALEXPORT int32_t CryptoNative_EvpCipherCtxSetPadding(EVP_CIPHER_CTX* x, int32_t padding);

/*
Function:
EvpCipherUpdate

Direct shim to EVP_CipherUpdate.
*/
PALEXPORT int32_t
CryptoNative_EvpCipherUpdate(EVP_CIPHER_CTX* ctx, uint8_t* out, int32_t* outl, unsigned char* in, int32_t inl);

/*
Function:
EvpCipherFinalEx

Direct shim to EVP_CipherFinal_ex.
*/
PALEXPORT int32_t CryptoNative_EvpCipherFinalEx(EVP_CIPHER_CTX* ctx, uint8_t* outm, int32_t* outl);

/*
Function:
EvpAesGcmGetTag

Retrieves tag for authenticated encryption
*/
PALEXPORT int32_t CryptoNative_EvpCipherGetGcmTag(EVP_CIPHER_CTX* ctx, uint8_t* tag, int32_t tagLength);

/*
Function:
EvpAesGcmSetTag

Sets tag for authenticated decryption
*/
PALEXPORT int32_t CryptoNative_EvpCipherSetGcmTag(EVP_CIPHER_CTX* ctx, uint8_t* tag, int32_t tagLength);

/*
Function:
EvpAesCcmGetTag

Retrieves tag for authenticated encryption
*/
PALEXPORT int32_t CryptoNative_EvpCipherGetCcmTag(EVP_CIPHER_CTX* ctx, uint8_t* tag, int32_t tagLength);

/*
Function:
EvpAesCcmSetTag

Sets tag for authenticated decryption
*/
PALEXPORT int32_t CryptoNative_EvpCipherSetCcmTag(EVP_CIPHER_CTX* ctx, uint8_t* tag, int32_t tagLength);

/*
Function:
EvpCipherGetAeadTag

Retrieves tag for authenticated encryption
*/
PALEXPORT int32_t CryptoNative_EvpCipherGetAeadTag(EVP_CIPHER_CTX* ctx, uint8_t* tag, int32_t tagLength);

/*
Function:
EvpCipherSetAeadTag

Sets tag for authenticated decryption
*/
PALEXPORT int32_t CryptoNative_EvpCipherSetAeadTag(EVP_CIPHER_CTX* ctx, uint8_t* tag, int32_t tagLength);

/*
Function:
EvpAes128Ecb

Direct shim to EVP_aes_128_ecb.
*/
PALEXPORT const EVP_CIPHER* CryptoNative_EvpAes128Ecb(void);

/*
Function:
EvpAes128Cbc

Direct shim to EVP_aes_128_cbc.
*/
PALEXPORT const EVP_CIPHER* CryptoNative_EvpAes128Cbc(void);

/*
Function:
EvpAes128Cfb8

Direct shim to EVP_aes_128_cfb8.
*/
PALEXPORT const EVP_CIPHER* CryptoNative_EvpAes128Cfb8(void);

/*
Function:
EvpAes128Cfb128

Direct shim to EVP_aes_128_cfb128.
*/
PALEXPORT const EVP_CIPHER* CryptoNative_EvpAes128Cfb128(void);

/*
Function:
EvpAes128Gcm

Direct shim to EVP_aes_128_gcm.
*/
PALEXPORT const EVP_CIPHER* CryptoNative_EvpAes128Gcm(void);

/*
Function:
EvpAes128Ccm

Direct shim to EVP_aes_128_ccm.
*/
PALEXPORT const EVP_CIPHER* CryptoNative_EvpAes128Ccm(void);

/*
Function:
EvpAes192Ecb

Direct shim to EVP_aes_192_ecb.
*/
PALEXPORT const EVP_CIPHER* CryptoNative_EvpAes192Ecb(void);

/*
Function:
EvpAes192Cbc

Direct shim to EVP_aes_192_cbc.
*/
PALEXPORT const EVP_CIPHER* CryptoNative_EvpAes192Cbc(void);

/*
Function:
EvpAes192Cfb8

Direct shim to EVP_aes_192_cfb8.
*/
PALEXPORT const EVP_CIPHER* CryptoNative_EvpAes192Cfb8(void);

/*
Function:
EvpAes192Cfb128

Direct shim to EVP_aes_192_cfb128.
*/
PALEXPORT const EVP_CIPHER* CryptoNative_EvpAes192Cfb128(void);

/*
Function:
EvpAes192Gcm

Direct shim to EVP_aes_192_gcm.
*/
PALEXPORT const EVP_CIPHER* CryptoNative_EvpAes192Gcm(void);

/*
Function:
EvpAes192Ccm

Direct shim to EVP_aes_192_ccm.
*/
PALEXPORT const EVP_CIPHER* CryptoNative_EvpAes192Ccm(void);

/*
Function:
EvpAes256Ecb

Direct shim to EVP_aes_256_ecb.
*/
PALEXPORT const EVP_CIPHER* CryptoNative_EvpAes256Ecb(void);

/*
Function:
EvpAes256Cbc

Direct shim to EVP_aes_256_cbc.
*/
PALEXPORT const EVP_CIPHER* CryptoNative_EvpAes256Cbc(void);

/*
Function:
EvpAes256Cfb8

Direct shim to EVP_aes_256_cfb8.
*/
PALEXPORT const EVP_CIPHER* CryptoNative_EvpAes256Cfb8(void);

/*
Function:
EvpAes256Cfb128

Direct shim to EVP_aes_256_cfb128.
*/
PALEXPORT const EVP_CIPHER* CryptoNative_EvpAes256Cfb128(void);

/*
Function:
EvpAes256Gcm

Direct shim to EVP_aes_256_gcm.
*/
PALEXPORT const EVP_CIPHER* CryptoNative_EvpAes256Gcm(void);

/*
Function:
EvpAes256Ccm

Direct shim to EVP_aes_256_ccm.
*/
PALEXPORT const EVP_CIPHER* CryptoNative_EvpAes256Ccm(void);

/*
Function:
EvpDes3Ecb

Direct shim to EVP_des_ede3.
*/
PALEXPORT const EVP_CIPHER* CryptoNative_EvpDes3Ecb(void);

/*
Function:
EvpDes3Cbc

Direct shim to EVP_des_ede3_cbc.
*/
PALEXPORT const EVP_CIPHER* CryptoNative_EvpDes3Cbc(void);

/*
Function:
EvpDes3Cfb8

Direct shim to EVP_des_ede3_cfb8.
*/
PALEXPORT const EVP_CIPHER* CryptoNative_EvpDes3Cfb8(void);

/*
Function:
EvpDes3Cfb64

Direct shim to EVP_des_ede3_cfb64.
*/
PALEXPORT const EVP_CIPHER* CryptoNative_EvpDes3Cfb64(void);

/*
Function:
EvpDesEcb

Direct shim to EVP_des_ecb.
*/
PALEXPORT const EVP_CIPHER* CryptoNative_EvpDesEcb(void);

/*
Function:
EvpDesCfb8

Direct shim to EVP_des_cfb8.
*/
PALEXPORT const EVP_CIPHER* CryptoNative_EvpDesCfb8(void);

/*
Function:
EvpDesCbc

Direct shim to EVP_des_ede_cbc.
*/
PALEXPORT const EVP_CIPHER* CryptoNative_EvpDesCbc(void);

/*
Function:
EvpRC2Ecb

Direct shim to EVP_rc2_ecb.
*/
PALEXPORT const EVP_CIPHER* CryptoNative_EvpRC2Ecb(void);

/*
Function:
EvpRC2Cbc

Direct shim to EVP_des_rc2_cbc.
*/
PALEXPORT const EVP_CIPHER* CryptoNative_EvpRC2Cbc(void);

/*
Function:
EvpChaCha20Poly1305

Direct shim to EVP_chacha20_poly1305. Returns NULL if not available
on the current platform.
*/
PALEXPORT const EVP_CIPHER* CryptoNative_EvpChaCha20Poly1305(void);
