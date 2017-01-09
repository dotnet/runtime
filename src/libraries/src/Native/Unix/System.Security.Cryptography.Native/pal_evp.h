// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include <stdint.h>
#include "opensslshim.h"

/*
Creates and initializes an EVP_MD_CTX with the given args.

Implemented by:
1) calling EVP_MD_CTX_create
2) calling EVP_DigestInit_ex on the new EVP_MD_CTX with the specified EVP_MD

Returns new EVP_MD_CTX on success, nullptr on failure.
*/
extern "C" EVP_MD_CTX* CryptoNative_EvpMdCtxCreate(const EVP_MD* type);

/*
Cleans up and deletes an EVP_MD_CTX instance created by EvpMdCtxCreate.

Implemented by:
1) Calling EVP_MD_CTX_destroy

No-op if ctx is null.
The given EVP_MD_CTX pointer is invalid after this call.
Always succeeds.
*/
extern "C" void CryptoNative_EvpMdCtxDestroy(EVP_MD_CTX* ctx);

/*
Resets an EVP_MD_CTX instance for a new computation.
*/
extern "C" int32_t CryptoNative_EvpDigestReset(EVP_MD_CTX* ctx, const EVP_MD* type);

/*
Function:
EvpDigestUpdate

Direct shim to EVP_DigestUpdate.
*/
extern "C" int32_t CryptoNative_EvpDigestUpdate(EVP_MD_CTX* ctx, const void* d, size_t cnt);

/*
Function:
EvpDigestFinalEx

Direct shim to EVP_DigestFinal_ex.
*/
extern "C" int32_t CryptoNative_EvpDigestFinalEx(EVP_MD_CTX* ctx, uint8_t* md, uint32_t* s);

/*
Function:
EvpMdSize

Direct shim to EVP_MD_size.
*/
extern "C" int32_t CryptoNative_EvpMdSize(const EVP_MD* md);

/*
Function:
EvpMd5

Direct shim to EVP_md5.
*/
extern "C" const EVP_MD* CryptoNative_EvpMd5();

/*
Function:
EvpSha1

Direct shim to EVP_sha1.
*/
extern "C" const EVP_MD* CryptoNative_EvpSha1();

/*
Function:
EvpSha256

Direct shim to EVP_sha256.
*/
extern "C" const EVP_MD* CryptoNative_EvpSha256();

/*
Function:
EvpSha384

Direct shim to EVP_sha384.
*/
extern "C" const EVP_MD* CryptoNative_EvpSha384();

/*
Function:
EvpSha512

Direct shim to EVP_sha512.
*/
extern "C" const EVP_MD* CryptoNative_EvpSha512();

/*
Function:
GetMaxMdSize

Returns the maxium bytes for a message digest.
*/
extern "C" int32_t CryptoNative_GetMaxMdSize();
