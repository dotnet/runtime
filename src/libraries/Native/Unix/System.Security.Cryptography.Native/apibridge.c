// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "opensslshim.h"
#include "pal_crypto_types.h"
#include "pal_types.h"

#ifdef NEED_OPENSSL_1_0

#include "apibridge.h"

// Minimally define the structs from 1.0.x which went opaque in 1.1.0 for the
// portable build building against the 1.1.x headers
#if OPENSSL_VERSION_NUMBER >= OPENSSL_VERSION_1_1_0_RTM
#include "openssl_1_0_structs.h"

#define CRYPTO_LOCK_X509 3
#define CRYPTO_LOCK_EVP_PKEY 10

#define SSL_CTRL_GET_SESSION_REUSED 8
#define SSL_CTRL_OPTIONS 32

#define SSL_ST_OK 3
#endif

const ASN1_TIME* local_X509_get0_notBefore(const X509* x509)
{
    if (x509 && x509->cert_info && x509->cert_info->validity)
    {
        return x509->cert_info->validity->notBefore;
    }

    return NULL;
}

const ASN1_TIME* local_X509_get0_notAfter(const X509* x509)
{
    if (x509 && x509->cert_info && x509->cert_info->validity)
    {
        return x509->cert_info->validity->notAfter;
    }

    return NULL;
}

const ASN1_TIME* local_X509_CRL_get0_nextUpdate(const X509_CRL* crl)
{
    if (crl && crl->crl)
    {
        return crl->crl->nextUpdate;
    }

    return NULL;
}

int32_t local_X509_get_version(const X509* x509)
{
    if (x509 && x509->cert_info)
    {
        long ver = ASN1_INTEGER_get(x509->cert_info->version);
        return (int32_t)ver;
    }

    return -1;
}

X509_PUBKEY* local_X509_get_X509_PUBKEY(const X509* x509)
{
    if (x509)
    {
        return x509->cert_info->key;
    }

    return NULL;
}

int32_t local_X509_PUBKEY_get0_param(
    ASN1_OBJECT** palgOid, const uint8_t** pkeyBytes, int* pkeyBytesLen, X509_ALGOR** palg, X509_PUBKEY* pubkey)
{
    if (palgOid)
    {
        *palgOid = pubkey->algor->algorithm;
    }

    if (pkeyBytes)
    {
        *pkeyBytes = pubkey->public_key->data;
        *pkeyBytesLen = pubkey->public_key->length;
    }

    if (palg)
    {
        *palg = pubkey->algor;
    }

    return 1;
}

const X509_ALGOR* local_X509_get0_tbs_sigalg(const X509* x509)
{
    if (x509 && x509->cert_info)
    {
        return x509->cert_info->signature;
    }

    return NULL;
}

ASN1_BIT_STRING* local_X509_get0_pubkey_bitstr(const X509* x509)
{
    if (x509 && x509->cert_info && x509->cert_info->key)
    {
        return x509->cert_info->key->public_key;
    }

    return NULL;
}

int32_t local_X509_NAME_get0_der(X509_NAME* x509Name, const uint8_t** pder, size_t* pderlen)
{
    if (!x509Name || !x509Name->bytes)
    {
        return 0;
    }

    if (pder)
    {
        *pder = (unsigned char*)x509Name->bytes->data;
    }

    if (pderlen)
    {
        *pderlen = x509Name->bytes->length;
    }

    return 1;
}

long local_OpenSSL_version_num()
{
    return (long)SSLeay();
}

const DSA_METHOD* local_DSA_get_method(const DSA* dsa)
{
    if (dsa)
    {
        return dsa->meth;
    }

    return NULL;
}

void local_DSA_get0_pqg(const DSA* dsa, const BIGNUM** p, const BIGNUM** q, const BIGNUM** g)
{
    if (!dsa)
    {
        return;
    }

    if (p)
    {
        *p = dsa->p;
    }

    if (q)
    {
        *q = dsa->q;
    }

    if (g)
    {
        *g = dsa->g;
    }
}

const BIGNUM* local_DSA_get0_key(const DSA* dsa, const BIGNUM** pubKey, const BIGNUM** privKey)
{
    if (dsa)
    {
        if (pubKey)
        {
            *pubKey = dsa->pub_key;
        }

        if (privKey)
        {
            *privKey = dsa->priv_key;
        }
    }

    return NULL;
}

int32_t local_DSA_set0_pqg(DSA* dsa, BIGNUM* bnP, BIGNUM* bnQ, BIGNUM* bnG)
{
    if (!dsa)
    {
        return 0;
    }

    if ((dsa->p == NULL && bnP == NULL) || (dsa->q == NULL && bnQ == NULL) || (dsa->g == NULL && bnG == NULL))
    {
        return 0;
    }

    if (bnP)
    {
        BN_free(dsa->p);
        dsa->p = bnP;
    }

    if (bnQ)
    {
        BN_free(dsa->q);
        dsa->q = bnQ;
    }

    if (bnG)
    {
        BN_free(dsa->g);
        dsa->g = bnG;
    }

    return 1;
}

int32_t local_DSA_set0_key(DSA* dsa, BIGNUM* bnY, BIGNUM* bnX)
{
    if (!dsa)
    {
        return 0;
    }

    if (dsa->pub_key == NULL && bnY == NULL)
    {
        return 0;
    }

    if (bnY)
    {
        BN_free(dsa->pub_key);
        dsa->pub_key = bnY;
    }

    if (bnX)
    {
        BN_free(dsa->priv_key);
        dsa->priv_key = bnX;
    }

    return 1;
}

int32_t local_EVP_PKEY_up_ref(EVP_PKEY* pkey)
{
    if (!pkey)
    {
        return 0;
    }

    return CRYPTO_add_lock(&pkey->references, 1, CRYPTO_LOCK_EVP_PKEY, __FILE__, __LINE__) > 1;
}

EVP_CIPHER_CTX* local_EVP_CIPHER_CTX_new()
{
    EVP_CIPHER_CTX* ctx = (EVP_CIPHER_CTX*)calloc(1, sizeof(EVP_CIPHER_CTX));
    return ctx;
}

int32_t local_EVP_CIPHER_CTX_reset(EVP_CIPHER_CTX* ctx)
{
    if (ctx)
    {
        int ret = EVP_CIPHER_CTX_cleanup(ctx);
        EVP_CIPHER_CTX_init(ctx);
        return ret;
    }

    // OpenSSL 1.1 returns succes 1 on a NULL input
    return 1;
}

void local_EVP_CIPHER_CTX_free(EVP_CIPHER_CTX* ctx)
{
    if (ctx)
    {
        local_EVP_CIPHER_CTX_reset(ctx);
        free(ctx);
    }
}

HMAC_CTX* local_HMAC_CTX_new()
{
    HMAC_CTX* ctx = (HMAC_CTX*)calloc(1, sizeof(HMAC_CTX));

    if (ctx)
    {
        HMAC_CTX_init(ctx);
    }

    return ctx;
}

void local_HMAC_CTX_free(HMAC_CTX* ctx)
{
    if (ctx != NULL)
    {
        HMAC_CTX_cleanup(ctx);
        free(ctx);
    }
}

int32_t local_RSA_meth_get_flags(const RSA_METHOD* meth)
{
    if (meth)
    {
        return meth->flags;
    }

    return 0;
}

void local_RSA_get0_key(const RSA* rsa, const BIGNUM** n, const BIGNUM** e, const BIGNUM** d)
{
    if (rsa)
    {
        if (n)
        {
            *n = rsa->n;
        }

        if (e)
        {
            *e = rsa->e;
        }

        if (d)
        {
            *d = rsa->d;
        }
    }
}

void local_RSA_get0_factors(const RSA* rsa, const BIGNUM** p, const BIGNUM** q)
{
    if (rsa)
    {
        if (p)
        {
            *p = rsa->p;
        }

        if (q)
        {
            *q = rsa->q;
        }
    }
}

void local_RSA_get0_crt_params(const RSA* rsa, const BIGNUM** dmp1, const BIGNUM** dmq1, const BIGNUM** iqmp)
{
    if (rsa)
    {
        if (dmp1)
        {
            *dmp1 = rsa->dmp1;
        }

        if (dmq1)
        {
            *dmq1 = rsa->dmq1;
        }

        if (iqmp)
        {
            *iqmp = rsa->iqmp;
        }
    }
}

int32_t local_RSA_set0_key(RSA* rsa, BIGNUM* n, BIGNUM* e, BIGNUM* d)
{
    if (rsa == NULL)
    {
        return 0;
    }

    if ((rsa->n == NULL && n == NULL) || (rsa->e == NULL && e == NULL))
    {
        return 0;
    }

    if (n != NULL)
    {
        BN_free(rsa->n);
        rsa->n = n;
    }

    if (e != NULL)
    {
        BN_free(rsa->e);
        rsa->e = e;
    }

    if (d != NULL)
    {
        BN_free(rsa->d);
        rsa->d = d;
    }

    return 1;
}

int32_t local_RSA_set0_factors(RSA* rsa, BIGNUM* p, BIGNUM* q)
{
    if (rsa == NULL)
    {
        return 0;
    }

    if ((rsa->p == NULL && p == NULL) || (rsa->q == NULL && q == NULL))
    {
        return 0;
    }

    if (p != NULL)
    {
        BN_free(rsa->p);
        rsa->p = p;
    }

    if (q != NULL)
    {
        BN_free(rsa->q);
        rsa->q = q;
    }

    return 1;
}

int32_t local_RSA_set0_crt_params(RSA* rsa, BIGNUM* dmp1, BIGNUM* dmq1, BIGNUM* iqmp)
{
    if (rsa == NULL)
    {
        return 0;
    }

    if ((rsa->dmp1 == NULL && dmp1 == NULL) || (rsa->dmq1 == NULL && dmq1 == NULL) ||
        (rsa->iqmp == NULL && iqmp == NULL))
    {
        return 0;
    }

    if (dmp1 != NULL)
    {
        BN_free(rsa->dmp1);
        rsa->dmp1 = dmp1;
    }

    if (dmq1 != NULL)
    {
        BN_free(rsa->dmq1);
        rsa->dmq1 = dmq1;
    }

    if (iqmp != NULL)
    {
        BN_free(rsa->iqmp);
        rsa->iqmp = iqmp;
    }

    return 1;
}

int32_t local_SSL_is_init_finished(const SSL* ssl)
{
    return SSL_state(ssl) == SSL_ST_OK;
}

X509Stack* local_X509_STORE_CTX_get0_chain(X509_STORE_CTX* ctx)
{
    return ctx ? ctx->chain : NULL;
}

X509_STORE* local_X509_STORE_CTX_get0_store(X509_STORE_CTX* ctx)
{
    return ctx ? ctx->ctx: NULL;
}

X509Stack* local_X509_STORE_CTX_get0_untrusted(X509_STORE_CTX* ctx)
{
    return ctx ? ctx->untrusted : NULL;
}

X509* local_X509_STORE_CTX_get0_cert(X509_STORE_CTX* ctx)
{
    return ctx ? ctx->cert : NULL;
}

X509_VERIFY_PARAM* local_X509_STORE_get0_param(X509_STORE* ctx)
{
    return ctx ? ctx->param: NULL;
}

int32_t local_X509_up_ref(X509* x509)
{
    if (x509 != NULL)
    {
        return CRYPTO_add_lock(&x509->references, 1, CRYPTO_LOCK_X509, __FILE__, __LINE__) > 1;
    }

    return 0;
}

unsigned long local_SSL_CTX_set_options(SSL_CTX* ctx, unsigned long options)
{
    // SSL_CTX_ctrl is signed long in and signed long out; but SSL_CTX_set_options,
    // which was a macro call to SSL_CTX_ctrl in 1.0, is unsigned/unsigned.
    return (unsigned long)SSL_CTX_ctrl(ctx, SSL_CTRL_OPTIONS, (long)options, NULL);
}

int local_SSL_session_reused(SSL* ssl)
{
    return (int)SSL_ctrl(ssl, SSL_CTRL_GET_SESSION_REUSED, 0, NULL);
}

void local_SSL_CTX_set_security_level(SSL_CTX* ctx, int32_t level)
{
    (void)ctx;
    (void)level;
}
#endif
