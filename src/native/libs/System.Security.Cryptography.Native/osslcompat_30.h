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

typedef struct ossl_provider_st OSSL_PROVIDER;
typedef struct ossl_lib_ctx_st OSSL_LIB_CTX;

void ERR_new(void);
void ERR_set_debug(const char *file, int line, const char *func);
void ERR_set_error(int lib, int reason, const char *fmt, ...);
int EVP_CIPHER_get_nid(const EVP_CIPHER *e);
int EVP_MD_get_size(const EVP_MD* md);
int EVP_PKEY_CTX_set_rsa_keygen_bits(EVP_PKEY_CTX* ctx, int bits);
int EVP_PKEY_CTX_set_rsa_oaep_md(EVP_PKEY_CTX* ctx, const EVP_MD* md);
int EVP_PKEY_CTX_set_rsa_padding(EVP_PKEY_CTX* ctx, int pad_mode);
int EVP_PKEY_CTX_set_rsa_pss_saltlen(EVP_PKEY_CTX* ctx, int saltlen);
int EVP_PKEY_CTX_set_signature_md(EVP_PKEY_CTX* ctx, const EVP_MD* md);
int EVP_PKEY_get_base_id(const EVP_PKEY* pkey);
int EVP_PKEY_get_size(const EVP_PKEY* pkey);
OSSL_PROVIDER* OSSL_PROVIDER_try_load(OSSL_LIB_CTX* , const char* name, int retain_fallbacks);
X509* SSL_get1_peer_certificate(const SSL* ssl);
