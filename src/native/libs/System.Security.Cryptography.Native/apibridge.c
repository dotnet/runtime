// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "opensslshim.h"
#include "pal_crypto_types.h"
#include "pal_types.h"

#include "../Common/pal_safecrt.h"
#include <assert.h>

#ifdef NEED_OPENSSL_1_0

#include "apibridge.h"

// Minimally define the structs from 1.0.x which went opaque in 1.1.0 for the
// portable build building against the 1.1.x headers
#if OPENSSL_VERSION_NUMBER >= OPENSSL_VERSION_1_1_0_RTM
#include "openssl_1_0_structs.h"

#define CRYPTO_LOCK_X509 3
#define CRYPTO_LOCK_EVP_PKEY 10
#define CRYPTO_LOCK_BIO 21

#define SSL_CTRL_GET_SESSION_REUSED 8
#define SSL_CTRL_OPTIONS 32

#define SSL_ST_OK 3
#endif

c_static_assert(X509_CHECK_FLAG_NO_PARTIAL_WILDCARDS == 4);

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

int local_X509_set1_notBefore(X509* x509, const ASN1_TIME* time)
{
    if (x509 && x509->cert_info && x509->cert_info->validity)
    {
        if (time != x509->cert_info->validity->notBefore)
        {
            if (x509->cert_info->validity->notBefore)
            {
                ASN1_TIME_free(x509->cert_info->validity->notBefore);
            }

            x509->cert_info->validity->notBefore = ASN1_STRING_dup(time);
        }

        return x509->cert_info->validity->notBefore != NULL;
    }

    return 0;
}

int local_X509_set1_notAfter(X509* x509, const ASN1_TIME* time)
{
    if (x509 && x509->cert_info && x509->cert_info->validity)
    {
        if (time != x509->cert_info->validity->notAfter)
        {
            if (x509->cert_info->validity->notAfter)
            {
                ASN1_TIME_free(x509->cert_info->validity->notAfter);
            }

            x509->cert_info->validity->notAfter = ASN1_STRING_dup(time);
        }

        return x509->cert_info->validity->notAfter != NULL;
    }

    return 0;
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

long local_OpenSSL_version_num(void)
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

RSA* local_EVP_PKEY_get0_RSA(EVP_PKEY* pkey)
{
    if (pkey == NULL)
    {
        return NULL;
    }

    RSA* rsa = EVP_PKEY_get1_RSA(pkey);

    if (rsa != NULL)
    {
        RSA_free(rsa);
    }

    return rsa;
}

int32_t local_EVP_PKEY_up_ref(EVP_PKEY* pkey)
{
    if (!pkey)
    {
        return 0;
    }

    return CRYPTO_add_lock(&pkey->references, 1, CRYPTO_LOCK_EVP_PKEY, __FILE__, __LINE__) > 1;
}

EVP_CIPHER_CTX* local_EVP_CIPHER_CTX_new(void)
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

    // OpenSSL 1.1 returns success 1 on a NULL input
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

HMAC_CTX* local_HMAC_CTX_new(void)
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

int32_t local_SSL_CTX_config(SSL_CTX* ctx, const char* name)
{
    (void)ctx;
    (void)name;

    // 1.0.x didn't load config in the same manner as 1.1.x,
    // so the appropriate answer is "section not found".
    return 0;
}

int32_t local_SSL_is_init_finished(const SSL* ssl)
{
    return SSL_state(ssl) == SSL_ST_OK;
}

/*
Function:
CheckX509HostnameMatch

Checks if a particular ASN1_STRING represents the entry in a certificate which would match against
the requested hostname.

Parameter sanRules: 0 for match rules against the subject CN, 1 for match rules against a SAN entry

Return values:
1 if the hostname is a match
0 if the hostname is not a match
Any negative number indicates an error in the arguments.
*/
static int CheckX509HostnameMatch(ASN1_STRING* candidate, const char* hostname, int cchHostname, int typeMatch)
{
    assert(candidate != NULL);
    assert(hostname != NULL);

    if (!candidate->data || !candidate->length)
    {
        return 0;
    }

    // If the candidate is *.example.org then the smallest we would match is a.example.org, which is the same
    // length. So anything longer than what we're matching against isn't valid.

    // Since the IDNA punycode conversion was applied already this holds even
    // in Unicode requests.
    if (candidate->length > cchHostname)
    {
        return 0;
    }

    char* candidateStr;
    int i;
    int hostnameFirstDot = -1;

    if (candidate->type != typeMatch)
    {
        return 0;
    }

    // Great, candidateStr is just candidate->data!
    candidateStr = (char*)(candidate->data);

    // First, verify that the string is alphanumeric, plus hyphens or periods and maybe starting with an asterisk.
    for (i = 0; i < candidate->length; ++i)
    {
        char c = candidateStr[i];

        if ((c < 'A' || c > 'Z') && (c < 'a' || c > 'z') && (c < '0' || c > '9') && (c != '.') && (c != '-') &&
            (c != '*' || i != 0))
        {
            return 0;
        }
    }

    if (candidateStr[0] != '*')
    {
        if (candidate->length != cchHostname)
        {
            return 0;
        }

        return !strncasecmp((const char*)candidateStr, hostname, (size_t)cchHostname);
    }

    for (i = 0; i < cchHostname; ++i)
    {
        if (hostname[i] == '.')
        {
            hostnameFirstDot = i;
            break;
        }
    }

    if (hostnameFirstDot < 0)
    {
        // It's possible that this should be considered a match if the entire SAN entry is '*',
        // aka candidate->length == 1; but nothing talks about this case.
        return 0;
    }

    int foundSecondDot = 0;

    for (i = hostnameFirstDot + 1; i < cchHostname; ++i)
    {
        if (hostname[i] == '.')
        {
            foundSecondDot = 1;
            break;
        }
    }

    // OpenSSL requires two dots for their hostname match.
    if (!foundSecondDot)
    {
        return 0;
    }

    {
        // Determine how many characters exist after the portion the wildcard would match. For example,
        // if hostname is 10 bytes long, and the '.' was at index 3, then we eliminate the first 3
        // characters (www) from the match constraint.  This forces the wildcard to be the last
        // character before the . in its match group.
        int matchLength = cchHostname - hostnameFirstDot;

        // If what's left over from hostname isn't as long as what's left over from the candidate
        // after the first character was an asterisk, it can't match.
        if (matchLength != (candidate->length - 1))
        {
            return 0;
        }

        return !strncasecmp(candidateStr + 1, hostname + hostnameFirstDot, (size_t)matchLength);
    }
}

int32_t local_X509_check_host(X509* x509, const char* name, size_t namelen, unsigned int flags, char** peername)
{
    assert(peername == NULL);
    assert(flags == X509_CHECK_FLAG_NO_PARTIAL_WILDCARDS);
    (void)flags;
    (void)peername;

    GENERAL_NAMES* san = (GENERAL_NAMES*)(X509_get_ext_d2i(x509, NID_subject_alt_name, NULL, NULL));
    int readSubject = 1;
    int success = 0;

    // RFC2818 says that if ANY dNSName alternative name field is present then
    // we should ignore the subject common name.

    if (san != NULL)
    {
        int count = sk_GENERAL_NAME_num(san);

        for (int i = 0; i < count; ++i)
        {
            GENERAL_NAME* sanEntry = sk_GENERAL_NAME_value(san, i);

            if (sanEntry->type != GEN_DNS)
            {
                continue;
            }

            readSubject = 0;

            // A GEN_DNS name is supposed to be a V_ASN1_IA5STRING.
            // If it isn't, we don't know how to read it.
            if (CheckX509HostnameMatch(sanEntry->d.dNSName, name, (int)namelen, V_ASN1_IA5STRING))
            {
                success = 1;
                break;
            }
        }

        GENERAL_NAMES_free(san);
    }

    if (readSubject)
    {
        assert(success == 0);

        // This is a shared/interor pointer, do not free!
        X509_NAME* subject = X509_get_subject_name(x509);

        if (subject != NULL)
        {
            int i = -1;

            while ((i = X509_NAME_get_index_by_NID(subject, NID_commonName, i)) >= 0)
            {
                // Shared/interior pointers, do not free!
                X509_NAME_ENTRY* nameEnt = X509_NAME_get_entry(subject, i);
                ASN1_STRING* cn = X509_NAME_ENTRY_get_data(nameEnt);

                // For compatibility with previous .NET builds, allow any type of
                // string for CN, provided it ended up with a single-byte encoding (otherwise
                // strncasecmp simply won't match).
                if (CheckX509HostnameMatch(cn, name, (int)namelen, cn->type))
                {
                    success = 1;
                    break;
                }
            }
        }
    }

    return success;
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

unsigned long local_SSL_set_options(SSL* ssl, unsigned long options)
{
   // SSL_ctrl is signed long in and signed long out; but SSL_set_options,
   // which was a macro call to SSL_ctrl in 1.0, is unsigned/unsigned.
   return (unsigned long)SSL_ctrl(ssl, SSL_CTRL_OPTIONS, (long)options, NULL);
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

void local_SSL_set_security_level(SSL* ssl, int32_t level)
{
    (void)ssl;
    (void)level;
}

int local_BIO_up_ref(BIO *bio)
{
    if (!bio)
    {
        return 0;
    }

    return CRYPTO_add_lock(&bio->references, 1, CRYPTO_LOCK_BIO, __FILE__, __LINE__) > 1;
}

int32_t local_RSA_pkey_ctx_ctrl(EVP_PKEY_CTX* ctx, int32_t optype, int32_t cmd, int32_t p1, void* p2)
{
    // On OpenSSL 1.0.2 there aren't two different identifiers for RSA,
    // so just pass the request on th EVP_PKEY_CTX_ctrl with the only identifier defined.
    return EVP_PKEY_CTX_ctrl(ctx, EVP_PKEY_RSA, optype, cmd, p1, p2);
}

int local_RSA_test_flags(const RSA *r, int flags)
{
    return r->flags & flags;
}

int local_EVP_PKEY_check(EVP_PKEY_CTX* ctx)
{
    EVP_PKEY* pkey = EVP_PKEY_CTX_get0_pkey(ctx);

    if (pkey == NULL)
    {
        ERR_put_error(ERR_LIB_EVP, 0, EVP_R_INPUT_NOT_INITIALIZED, __FILE__, __LINE__);
        return -1;
    }

    int id = EVP_PKEY_get_base_id(pkey);

    switch (id)
    {
        case NID_rsaEncryption:
        {
            const RSA* rsa = EVP_PKEY_get0_RSA(pkey);

            if (rsa != NULL)
            {
                return RSA_check_key(rsa);
            }

            break;
        }
        default:
            ERR_put_error(ERR_LIB_EVP, 0, EVP_R_UNSUPPORTED_ALGORITHM, __FILE__, __LINE__);
            return -1;
    }

    ERR_put_error(ERR_LIB_EVP, 0, EVP_R_NO_KEY_SET, __FILE__, __LINE__);
    return -1;
}

int local_EVP_PKEY_public_check(EVP_PKEY_CTX* ctx)
{
    EVP_PKEY* pkey = EVP_PKEY_CTX_get0_pkey(ctx);

    if (pkey == NULL)
    {
        ERR_put_error(ERR_LIB_EVP, 0, EVP_R_INPUT_NOT_INITIALIZED, __FILE__, __LINE__);
        return -1;
    }

    int id = EVP_PKEY_get_base_id(pkey);

    switch (id)
    {
        case NID_rsaEncryption:
        {
            ERR_put_error(ERR_LIB_EVP, 0, EVP_R_OPERATION_NOT_SUPPORTED_FOR_THIS_KEYTYPE, __FILE__, __LINE__);
            return -2;
        }
        default:
            ERR_put_error(ERR_LIB_EVP, 0, EVP_R_UNSUPPORTED_ALGORITHM, __FILE__, __LINE__);
            return -1;
    }
}


int local_ASN1_TIME_to_tm(const ASN1_TIME* s, struct tm* tm)
{
    (void)s;
    (void)tm;

    return 0;
}
#endif
