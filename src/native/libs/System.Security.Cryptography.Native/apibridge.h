// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Functions based on OpenSSL 1.1 API, used when building against/running with OpenSSL 1.0

#pragma once
#include "pal_types.h"

int local_ASN1_TIME_to_tm(const ASN1_TIME* s, struct tm* tm);
int local_BN_abs_is_word(const BIGNUM *a, const BN_ULONG w);
int local_BN_is_zero(const BIGNUM* a);
int local_BN_is_odd(const BIGNUM* a);
int local_BN_is_one(const BIGNUM* a);
int local_BIO_up_ref(BIO *a);
const BIGNUM* local_DSA_get0_key(const DSA* dsa, const BIGNUM** pubKey, const BIGNUM** privKey);
void local_DSA_get0_pqg(const DSA* dsa, const BIGNUM** p, const BIGNUM** q, const BIGNUM** g);
const DSA_METHOD* local_DSA_get_method(const DSA* dsa);
int32_t local_DSA_set0_key(DSA* dsa, BIGNUM* bnY, BIGNUM* bnX);
int32_t local_DSA_set0_pqg(DSA* dsa, BIGNUM* bnP, BIGNUM* bnQ, BIGNUM* bnG);
void local_EVP_CIPHER_CTX_free(EVP_CIPHER_CTX* ctx);
EVP_CIPHER_CTX* local_EVP_CIPHER_CTX_new(void);
int32_t local_EVP_CIPHER_CTX_reset(EVP_CIPHER_CTX* ctx);
int local_EVP_PKEY_check(EVP_PKEY_CTX* ctx);
RSA* local_EVP_PKEY_get0_RSA(EVP_PKEY* pkey);
int local_EVP_PKEY_public_check(EVP_PKEY_CTX* ctx);
int32_t local_EVP_PKEY_up_ref(EVP_PKEY* pkey);
void local_HMAC_CTX_free(HMAC_CTX* ctx);
HMAC_CTX* local_HMAC_CTX_new(void);
long local_OpenSSL_version_num(void);
void local_RSA_get0_crt_params(const RSA* rsa, const BIGNUM** dmp1, const BIGNUM** dmq1, const BIGNUM** iqmp);
void local_RSA_get0_factors(const RSA* rsa, const BIGNUM** p, const BIGNUM** q);
void local_RSA_get0_key(const RSA* rsa, const BIGNUM** n, const BIGNUM** e, const BIGNUM** d);
int local_RSA_get_multi_prime_extra_count(const RSA* r);
int32_t local_RSA_meth_get_flags(const RSA_METHOD* meth);
int32_t local_RSA_set0_crt_params(RSA* rsa, BIGNUM* dmp1, BIGNUM* dmq1, BIGNUM* iqmp);
int32_t local_RSA_set0_factors(RSA* rsa, BIGNUM* p, BIGNUM* q);
int32_t local_RSA_set0_key(RSA* rsa, BIGNUM* n, BIGNUM* e, BIGNUM* d);
int local_RSA_test_flags(const RSA *r, int flags);
int32_t local_RSA_pkey_ctx_ctrl(EVP_PKEY_CTX* ctx, int32_t optype, int32_t cmd, int32_t p1, void* p2);
int32_t local_SSL_is_init_finished(const SSL* ssl);
int32_t local_SSL_CTX_config(SSL_CTX* ctx, const char* name);
unsigned long local_SSL_CTX_set_options(SSL_CTX* ctx, unsigned long options);
unsigned long local_SSL_set_options(SSL* ssl, unsigned long options);
void local_SSL_CTX_set_security_level(SSL_CTX* ctx, int32_t level);
void local_SSL_set_security_level(SSL* ssl, int32_t level);
int local_SSL_session_reused(SSL* ssl);
int32_t local_X509_check_host(X509* x509, const char* name, size_t namelen, unsigned int flags, char** peername);
const ASN1_TIME* local_X509_CRL_get0_nextUpdate(const X509_CRL* crl);
int32_t local_X509_NAME_get0_der(X509_NAME* x509Name, const uint8_t** pder, size_t* pderlen);
int32_t local_X509_PUBKEY_get0_param(
    ASN1_OBJECT** palgOid, const uint8_t** pkeyBytes, int* pkeyBytesLen, X509_ALGOR** palg, X509_PUBKEY* pubkey);
STACK_OF(X509)* local_X509_STORE_CTX_get0_chain(X509_STORE_CTX* ctx);
X509* local_X509_STORE_CTX_get0_cert(X509_STORE_CTX* ctx);
X509_STORE* local_X509_STORE_CTX_get0_store(X509_STORE_CTX* ctx);
STACK_OF(X509)* local_X509_STORE_CTX_get0_untrusted(X509_STORE_CTX* ctx);
X509_VERIFY_PARAM* local_X509_STORE_get0_param(X509_STORE* ctx);
const ASN1_TIME* local_X509_get0_notAfter(const X509* x509);
const ASN1_TIME* local_X509_get0_notBefore(const X509* x509);
ASN1_BIT_STRING* local_X509_get0_pubkey_bitstr(const X509* x509);
int local_X509_set1_notBefore(X509* x509, const ASN1_TIME*);
int local_X509_set1_notAfter(X509* x509, const ASN1_TIME*);
const X509_ALGOR* local_X509_get0_tbs_sigalg(const X509* x509);
X509_PUBKEY* local_X509_get_X509_PUBKEY(const X509* x509);
int32_t local_X509_get_version(const X509* x509);
int32_t local_X509_up_ref(X509* x509);
typedef void (*SSL_CTX_keylog_cb_func)(const SSL *ssl, const char *line);
void local_SSL_CTX_set_keylog_callback(SSL_CTX *ctx, SSL_CTX_keylog_cb_func cb);
