// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Function prototypes unique to OpenSSL 3.0

#pragma once
#include "pal_types.h"

#undef EVP_PKEY_CTX_set_rsa_keygen_bits
#undef EVP_PKEY_CTX_set_rsa_oaep_md
#undef EVP_PKEY_CTX_set_rsa_padding
#undef EVP_PKEY_CTX_set_rsa_pss_saltlen
#undef EVP_PKEY_CTX_set_signature_md

#define OSSL_MAC_PARAM_KEY    "key"
#define OSSL_MAC_PARAM_CUSTOM "custom"
#define OSSL_MAC_PARAM_XOF    "xof"
#define OSSL_MAC_PARAM_SIZE   "size"

typedef struct ossl_provider_st OSSL_PROVIDER;
typedef struct ossl_lib_ctx_st OSSL_LIB_CTX;
typedef struct ossl_param_st OSSL_PARAM;

struct ossl_param_st
{
    const char *key;
    unsigned char data_type;
    void *data;
    size_t data_size;
    size_t return_size;
};

void ERR_new(void);
void ERR_set_debug(const char *file, int line, const char *func);
void ERR_set_error(int lib, int reason, const char *fmt, ...);
int EVP_CIPHER_get_nid(const EVP_CIPHER *e);

int EVP_MAC_CTX_set_params(EVP_MAC_CTX *ctx, const OSSL_PARAM params[]);
EVP_MAC_CTX *EVP_MAC_CTX_new(EVP_MAC *mac);
void EVP_MAC_CTX_free(EVP_MAC_CTX *ctx);
EVP_MAC_CTX *EVP_MAC_CTX_dup(const EVP_MAC_CTX *src);
EVP_MAC* EVP_MAC_fetch(OSSL_LIB_CTX *libctx, const char *algorithm, const char *properties);
int EVP_MAC_init(EVP_MAC_CTX *ctx, const unsigned char *key, size_t keylen, const OSSL_PARAM params[]);
int EVP_MAC_update(EVP_MAC_CTX *ctx, const unsigned char *data, size_t datalen);
int EVP_MAC_final(EVP_MAC_CTX *ctx, unsigned char *out, size_t *outl, size_t outsize);
void EVP_MAC_free(EVP_MAC *mac);

EVP_MD* EVP_MD_fetch(OSSL_LIB_CTX *ctx, const char *algorithm, const char *properties);
int EVP_MD_get_size(const EVP_MD* md);
int EVP_PKEY_CTX_set_rsa_keygen_bits(EVP_PKEY_CTX* ctx, int bits);
int EVP_PKEY_CTX_set_rsa_oaep_md(EVP_PKEY_CTX* ctx, const EVP_MD* md);
int EVP_PKEY_CTX_set_rsa_padding(EVP_PKEY_CTX* ctx, int pad_mode);
int EVP_PKEY_CTX_set_rsa_pss_saltlen(EVP_PKEY_CTX* ctx, int saltlen);
int EVP_PKEY_CTX_set_signature_md(EVP_PKEY_CTX* ctx, const EVP_MD* md);
int EVP_PKEY_get_base_id(const EVP_PKEY* pkey);
int EVP_PKEY_get_size(const EVP_PKEY* pkey);

OSSL_PARAM OSSL_PARAM_construct_end(void);
OSSL_PARAM OSSL_PARAM_construct_int32(const char *key, int32_t *buf);
OSSL_PARAM OSSL_PARAM_construct_octet_string(const char *key, void *buf, size_t bsize);

OSSL_PROVIDER* OSSL_PROVIDER_try_load(OSSL_LIB_CTX* , const char* name, int retain_fallbacks);
X509* SSL_get1_peer_certificate(const SSL* ssl);
