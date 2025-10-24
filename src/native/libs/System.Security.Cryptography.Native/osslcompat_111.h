// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Function prototypes unique to OpenSSL 1.1.x

#pragma once
#include "pal_types.h"

typedef struct ossl_init_settings_st OPENSSL_INIT_SETTINGS;
typedef struct stack_st OPENSSL_STACK;

#define OPENSSL_INIT_LOAD_CRYPTO_STRINGS 0x00000002L
#define OPENSSL_INIT_ADD_ALL_CIPHERS 0x00000004L
#define OPENSSL_INIT_ADD_ALL_DIGESTS 0x00000008L
#define OPENSSL_INIT_LOAD_CONFIG 0x00000040L
#define OPENSSL_INIT_LOAD_SSL_STRINGS 0x00200000L

void EVP_MD_CTX_free(EVP_MD_CTX* ctx);
EVP_MD_CTX* EVP_MD_CTX_new(void);
int OPENSSL_init_ssl(uint64_t opts, const OPENSSL_INIT_SETTINGS* settings);
void OPENSSL_sk_free(OPENSSL_STACK*);
OPENSSL_STACK* OPENSSL_sk_new_null(void);
int OPENSSL_sk_num(const OPENSSL_STACK*);
void* OPENSSL_sk_pop(OPENSSL_STACK* st);
void OPENSSL_sk_pop_free(OPENSSL_STACK* st, void (*func)(void*));
int OPENSSL_sk_push(OPENSSL_STACK* st, const void* data);
void* OPENSSL_sk_value(const OPENSSL_STACK*, int);
const RSA_METHOD* RSA_PKCS1_OpenSSL(void);
unsigned long SSL_set_options(SSL* ctx, unsigned long options);
void SSL_set_post_handshake_auth(SSL *s, int val);
int32_t SSL_set_post_handshake_auth(SSL *s, int val);
int SSL_verify_client_post_handshake(SSL *s);
const SSL_METHOD* TLS_method(void);
const char *SSL_SESSION_get0_hostname(const SSL_SESSION *s);
int SSL_SESSION_set1_hostname(SSL_SESSION *s, const char *hostname);

#endif
