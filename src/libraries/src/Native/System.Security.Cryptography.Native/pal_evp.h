// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#include <stdint.h>
#include <openssl/evp.h>

/*
Function:
EvpMdCtxCreate

Direct shim to EVP_MD_CTX_create.
*/
extern "C" EVP_MD_CTX* EvpMdCtxCreate();

/*
Function:
EvpDigestInitEx

Direct shim to EVP_DigestInit_ex.
*/
extern "C" int32_t EvpDigestInitEx(EVP_MD_CTX* ctx, const EVP_MD* type, ENGINE* impl);

/*
Function:
EvpDigestUpdate

Direct shim to EVP_DigestUpdate.
*/
extern "C" int32_t EvpDigestUpdate(EVP_MD_CTX* ctx, const void* d, size_t cnt);

/*
Function:
EvpDigestFinalEx

Direct shim to EVP_DigestFinal_ex.
*/
extern "C" int32_t EvpDigestFinalEx(EVP_MD_CTX* ctx, unsigned char* md, uint32_t* s);

/*
Function:
EvpMdCtxDestroy

Direct shim to EVP_MD_CTX_destroy.
*/
extern "C" void EvpMdCtxDestroy(EVP_MD_CTX* ctx);

/*
Function:
EvpMdSize

Direct shim to EVP_MD_size.
*/
extern "C" int32_t EvpMdSize(const EVP_MD* md);

/*
Function:
EvpMd5

Direct shim to EVP_md5.
*/
extern "C" const EVP_MD* EvpMd5();

/*
Function:
EvpSha1

Direct shim to EVP_sha1.
*/
extern "C" const EVP_MD* EvpSha1();

/*
Function:
EvpSha256

Direct shim to EVP_sha256.
*/
extern "C" const EVP_MD* EvpSha256();

/*
Function:
EvpSha384

Direct shim to EVP_sha384.
*/
extern "C" const EVP_MD* EvpSha384();

/*
Function:
EvpSha512

Direct shim to EVP_sha512.
*/
extern "C" const EVP_MD* EvpSha512();

/*
Function:
GetMaxMdSize

Returns the maxium bytes for a message digest.
*/
extern "C" int32_t GetMaxMdSize();
