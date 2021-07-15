// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Function prototypes unique to OpenSSL 1.1.x

#pragma once
#include "pal_types.h"

#undef SSL_CTX_set_options
#undef SSL_set_options
#undef SSL_session_reused

typedef struct ossl_init_settings_st OPENSSL_INIT_SETTINGS;
typedef struct stack_st OPENSSL_STACK;

#define OPENSSL_INIT_LOAD_CRYPTO_STRINGS 0x00000002L
#define OPENSSL_INIT_ADD_ALL_CIPHERS 0x00000004L
#define OPENSSL_INIT_ADD_ALL_DIGESTS 0x00000008L
#define OPENSSL_INIT_LOAD_CONFIG 0x00000040L
#define OPENSSL_INIT_LOAD_SSL_STRINGS 0x00200000L

int BIO_up_ref(BIO* a);
const BIGNUM* DSA_get0_key(const DSA* dsa, const BIGNUM** pubKey, const BIGNUM** privKey);
void DSA_get0_pqg(const DSA* dsa, const BIGNUM** p, const BIGNUM** q, const BIGNUM** g);
const DSA_METHOD* DSA_get_method(const DSA* dsa);
int32_t DSA_set0_key(DSA* dsa, BIGNUM* bnY, BIGNUM* bnX);
int32_t DSA_set0_pqg(DSA* dsa, BIGNUM* bnP, BIGNUM* bnQ, BIGNUM* bnG);
void EVP_CIPHER_CTX_free(EVP_CIPHER_CTX* ctx);
EVP_CIPHER_CTX* EVP_CIPHER_CTX_new(void);
int32_t EVP_CIPHER_CTX_reset(EVP_CIPHER_CTX* ctx);
void EVP_MD_CTX_free(EVP_MD_CTX* ctx);
EVP_MD_CTX* EVP_MD_CTX_new(void);
RSA* EVP_PKEY_get0_RSA(EVP_PKEY* pkey);
int EVP_PKEY_check(EVP_PKEY_CTX* ctx);
int EVP_PKEY_public_check(EVP_PKEY_CTX* ctx);
int32_t EVP_PKEY_up_ref(EVP_PKEY* pkey);
void HMAC_CTX_free(HMAC_CTX* ctx);
HMAC_CTX* HMAC_CTX_new(void);
int OPENSSL_init_ssl(uint64_t opts, const OPENSSL_INIT_SETTINGS* settings);
void OPENSSL_sk_free(OPENSSL_STACK*);
OPENSSL_STACK* OPENSSL_sk_new_null(void);
int OPENSSL_sk_num(const OPENSSL_STACK*);
void* OPENSSL_sk_pop(OPENSSL_STACK* st);
void OPENSSL_sk_pop_free(OPENSSL_STACK* st, void (*func)(void*));
int OPENSSL_sk_push(OPENSSL_STACK* st, const void* data);
void* OPENSSL_sk_value(const OPENSSL_STACK*, int);
long OpenSSL_version_num(void);
const RSA_METHOD* RSA_PKCS1_OpenSSL(void);
void RSA_get0_crt_params(const RSA* rsa, const BIGNUM** dmp1, const BIGNUM** dmq1, const BIGNUM** iqmp);
void RSA_get0_factors(const RSA* rsa, const BIGNUM** p, const BIGNUM** q);
void RSA_get0_key(const RSA* rsa, const BIGNUM** n, const BIGNUM** e, const BIGNUM** d);
int32_t RSA_meth_get_flags(const RSA_METHOD* meth);
int32_t RSA_pkey_ctx_ctrl(EVP_PKEY_CTX* ctx, int32_t optype, int32_t cmd, int32_t p1, void* p2);
int32_t RSA_set0_crt_params(RSA* rsa, BIGNUM* dmp1, BIGNUM* dmq1, BIGNUM* iqmp);
int32_t RSA_set0_factors(RSA* rsa, BIGNUM* p, BIGNUM* q);
int32_t RSA_set0_key(RSA* rsa, BIGNUM* n, BIGNUM* e, BIGNUM* d);
int RSA_test_flags(const RSA *r, int flags);
int SSL_CTX_config(SSL_CTX* ctx, const char* name);
unsigned long SSL_CTX_set_options(SSL_CTX* ctx, unsigned long options);
void SSL_CTX_set_security_level(SSL_CTX* ctx, int32_t level);
int32_t SSL_is_init_finished(SSL* ssl);
unsigned long SSL_set_options(SSL* ctx, unsigned long options);
int SSL_session_reused(SSL* ssl);
const SSL_METHOD* TLS_method(void);
const ASN1_TIME* X509_CRL_get0_nextUpdate(const X509_CRL* crl);
int32_t X509_NAME_get0_der(X509_NAME* x509Name, const uint8_t** pder, size_t* pderlen);
int32_t X509_PUBKEY_get0_param(
    ASN1_OBJECT** palgOid, const uint8_t** pkeyBytes, int* pkeyBytesLen, X509_ALGOR** palg, X509_PUBKEY* pubkey);
X509* X509_STORE_CTX_get0_cert(X509_STORE_CTX* ctx);
STACK_OF(X509) * X509_STORE_CTX_get0_chain(X509_STORE_CTX* ctx);
STACK_OF(X509) * X509_STORE_CTX_get0_untrusted(X509_STORE_CTX* ctx);
X509_VERIFY_PARAM* X509_STORE_get0_param(X509_STORE* ctx);
const ASN1_TIME* X509_get0_notAfter(const X509* x509);
const ASN1_TIME* X509_get0_notBefore(const X509* x509);
ASN1_BIT_STRING* X509_get0_pubkey_bitstr(const X509* x509);
const X509_ALGOR* X509_get0_tbs_sigalg(const X509* x509);
X509_PUBKEY* X509_get_X509_PUBKEY(const X509* x509);
int32_t X509_get_version(const X509* x509);
int X509_set1_notAfter(X509* x509, const ASN1_TIME*);
int X509_set1_notBefore(X509* x509, const ASN1_TIME*);
int32_t X509_up_ref(X509* x509);

#if OPENSSL_VERSION_NUMBER < OPENSSL_VERSION_1_0_2_RTM
int32_t X509_check_host(X509* x509, const char* name, size_t namelen, unsigned int flags, char** peername);
X509_STORE* X509_STORE_CTX_get0_store(X509_STORE_CTX* ctx);
#define X509_CHECK_FLAG_NO_PARTIAL_WILDCARDS 4

#endif
