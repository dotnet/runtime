// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal_ssl.h"
#include "openssl.h"
#include "pal_evp_pkey.h"
#include "pal_evp_pkey_rsa.h"
#include "pal_x509.h"

#include <assert.h>
#include <string.h>
#include <stdbool.h>

c_static_assert(PAL_SSL_ERROR_NONE == SSL_ERROR_NONE);
c_static_assert(PAL_SSL_ERROR_SSL == SSL_ERROR_SSL);
c_static_assert(PAL_SSL_ERROR_WANT_READ == SSL_ERROR_WANT_READ);
c_static_assert(PAL_SSL_ERROR_WANT_WRITE == SSL_ERROR_WANT_WRITE);
c_static_assert(PAL_SSL_ERROR_SYSCALL == SSL_ERROR_SYSCALL);
c_static_assert(PAL_SSL_ERROR_ZERO_RETURN == SSL_ERROR_ZERO_RETURN);

#define DOTNET_DEFAULT_CIPHERSTRING \
    "ECDHE-ECDSA-AES256-GCM-SHA384:" \
    "ECDHE-ECDSA-AES128-GCM-SHA256:" \
    "ECDHE-RSA-AES256-GCM-SHA384:" \
    "ECDHE-RSA-AES128-GCM-SHA256:" \
    "ECDHE-ECDSA-AES256-SHA384:" \
    "ECDHE-ECDSA-AES128-SHA256:" \
    "ECDHE-RSA-AES256-SHA384:" \
    "ECDHE-RSA-AES128-SHA256:" \

int32_t CryptoNative_EnsureOpenSslInitialized(void);

#ifdef NEED_OPENSSL_1_0
static void EnsureLibSsl10Initialized()
{
    SSL_library_init();
    SSL_load_error_strings();
}
#endif

#ifdef FEATURE_DISTRO_AGNOSTIC_SSL
// redirect all SSL_CTX_set_options and SSL_set_options calls via dynamic shims
// to work around ABI breaking change between 1.1 and 3.0

#undef SSL_CTX_set_options
#define SSL_CTX_set_options SSL_CTX_set_options_dynamic
static uint64_t SSL_CTX_set_options_dynamic(SSL_CTX* ctx, uint64_t options)
{
#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Wcast-function-type"
    if (API_EXISTS(ERR_new)) // OpenSSL 3.0 sentinel function
    {
        // OpenSSL 3.0 and newer, use uint64_t for options
        uint64_t (*func)(SSL_CTX* ctx, uint64_t op) = (uint64_t(*)(SSL_CTX*, uint64_t))SSL_CTX_set_options_ptr;
        return func(ctx, options);
    }
    else
    {
        // OpenSSL 1.1 and earlier, use uint32_t for options
        uint32_t (*func)(SSL_CTX* ctx, uint32_t op) = (uint32_t(*)(SSL_CTX*, uint32_t))SSL_CTX_set_options_ptr;
        return func(ctx, (uint32_t)options);
    }
#pragma clang diagnostic pop
}

#undef SSL_set_options
#define SSL_set_options SSL_set_options_dynamic
static uint64_t SSL_set_options_dynamic(SSL* s, uint64_t options)
{
#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Wcast-function-type"
    if (API_EXISTS(ERR_new)) // OpenSSL 3.0 sentinel function
    {
        // OpenSSL 3.0 and newer, use uint64_t for options
        uint64_t (*func)(SSL* s, uint64_t op) = (uint64_t(*)(SSL*, uint64_t))SSL_set_options_ptr;
        return func(s, options);
    }
    else
    {
        // OpenSSL 1.1 and earlier, use uint32_t for options
        uint32_t (*func)(SSL* s, uint32_t op) = (uint32_t(*)(SSL*, uint32_t))SSL_set_options_ptr;
        return func(s, (uint32_t)options);
    }
#pragma clang diagnostic pop
}
#endif

static int32_t g_config_specified_ciphersuites = 0;
static char* g_emptyAlpn = "";

static void DetectCiphersuiteConfiguration()
{
#ifdef FEATURE_DISTRO_AGNOSTIC_SSL

    if (API_EXISTS(SSL_state))
    {
        // For portable builds NEED_OPENSSL_1_1 is always set.
        // OpenSSL 1.0 does not support CipherSuites so there is no way for caller to override default
        g_config_specified_ciphersuites = 1;
        return;
    }

#endif

    // This routine will always produce g_config_specified_ciphersuites = 1 on OpenSSL 1.0.x,
    // so if we're building direct for 1.0.x (the only time NEED_OPENSSL_1_1 is undefined) then
    // just omit all the code here.
    //
    // The method uses OpenSSL 1.0.x API, except for the fallback function SSL_CTX_config, to
    // make the portable version easier.
#if defined NEED_OPENSSL_1_1 || defined NEED_OPENSSL_3_0

    // Check to see if there's a registered default CipherString. If not, we will use our own.
    SSL_CTX* ctx = SSL_CTX_new(TLS_method());
    assert(ctx != NULL);

    // SSL_get_ciphers returns a shared pointer, no need to save/free it.
    // It gets invalidated every time we touch the configuration, so we can't ask just once, either.
    SSL* ssl = SSL_new(ctx);
    assert(ssl != NULL);
    int defaultCount = sk_SSL_CIPHER_num(SSL_get_ciphers(ssl));
    SSL_free(ssl);

    int rv = SSL_CTX_set_cipher_list(ctx, "ALL");
    assert(rv);

    ssl = SSL_new(ctx);
    assert(ssl != NULL);
    int allCount = sk_SSL_CIPHER_num(SSL_get_ciphers(ssl));
    SSL_free(ssl);

    // It isn't expected that the default list and the "ALL" list have the same cardinality,
    // but if that does happen (custom build, config, et cetera) then use the "RSA" list
    // instead of the "ALL" list. Since the RSA list doesn't include legacy ciphersuites
    // appropriate for ECDSA server certificates, it should be different than the ALL list.
    if (allCount == defaultCount)
    {
        rv = SSL_CTX_set_cipher_list(ctx, "RSA");
        assert(rv);
        ssl = SSL_new(ctx);
        assert(ssl != NULL);
        allCount = sk_SSL_CIPHER_num(SSL_get_ciphers(ssl));
        SSL_free(ssl);
        // If the implicit default, "ALL", and "RSA" all have the same cardinality, just fail.
        assert(allCount != defaultCount);
    }

    if (!SSL_CTX_config(ctx, "system_default"))
    {
        // There's no system_default configuration, so no default CipherString.
        ERR_clear_error();
    }
    else
    {
        ssl = SSL_new(ctx);
        assert(ssl != NULL);
        int after = sk_SSL_CIPHER_num(SSL_get_ciphers(ssl));
        SSL_free(ssl);

        g_config_specified_ciphersuites = (allCount != after);
    }

    SSL_CTX_free(ctx);

#else

    // OpenSSL 1.0 does not support CipherSuites so there is no way for caller to override default
    g_config_specified_ciphersuites = 1;

#endif
}

void CryptoNative_EnsureLibSslInitialized()
{
    CryptoNative_EnsureOpenSslInitialized();

    // If portable, call the 1.0 initializer when needed.
    // If 1.0, call it statically.
    // In 1.1 no action is required, since EnsureOpenSslInitialized does both libraries.
#ifdef FEATURE_DISTRO_AGNOSTIC_SSL
    if (API_EXISTS(SSL_state))
    {
        EnsureLibSsl10Initialized();
    }
#elif OPENSSL_VERSION_NUMBER < OPENSSL_VERSION_1_1_0_RTM
    EnsureLibSsl10Initialized();
#endif

    DetectCiphersuiteConfiguration();
}

const SSL_METHOD* CryptoNative_SslV2_3Method()
{
    // No error queue impact.
    const SSL_METHOD* method = TLS_method();
    assert(method != NULL);
    return method;
}

SSL_CTX* CryptoNative_SslCtxCreate(const SSL_METHOD* method)
{
    ERR_clear_error();

    SSL_CTX* ctx = SSL_CTX_new(method);

    if (ctx != NULL)
    {
        // As of OpenSSL 1.1.0, compression is disabled by default. In case an older build
        // is used, ensure it's disabled.
        //
        // The other .NET platforms are server-preference, and the common consensus seems
        // to be to use server preference (as of June 2020), so just always assert that.
        SSL_CTX_set_options(ctx, SSL_OP_NO_COMPRESSION | SSL_OP_CIPHER_SERVER_PREFERENCE);

#ifdef NEED_OPENSSL_3_0
        if (CryptoNative_OpenSslVersionNumber() >= OPENSSL_VERSION_3_0_RTM)
        {
            // OpenSSL 3.0 forbids client-initiated renegotiation by default. To avoid platform
            // differences, we explicitly enable it and handle AllowRenegotiation flag in managed
            // code as in previous versions
#ifndef SSL_OP_ALLOW_CLIENT_RENEGOTIATION
#define SSL_OP_ALLOW_CLIENT_RENEGOTIATION ((uint64_t)1 << (uint64_t)8)
#endif
            SSL_CTX_set_options(ctx, SSL_OP_ALLOW_CLIENT_RENEGOTIATION);
        }
#endif

        // If openssl.cnf doesn't have an opinion for CipherString, then use this value instead
        if (!g_config_specified_ciphersuites)
        {
            if (!SSL_CTX_set_cipher_list(ctx, DOTNET_DEFAULT_CIPHERSTRING))
            {
                SSL_CTX_free(ctx);
                return NULL;
            }
        }
    }

    return ctx;
}

/*
Openssl supports setting ecdh curves by default from version 1.1.0.
For lower versions, this is the recommended approach.
Returns 1 on success, 0 on failure.
*/
static long TrySetECDHNamedCurve(SSL_CTX* ctx)
{
#ifdef NEED_OPENSSL_1_0
    int64_t version = CryptoNative_OpenSslVersionNumber();
    long result = 0;

    if (version >= OPENSSL_VERSION_1_1_0_RTM)
    {
        // OpenSSL 1.1+ automatically set up ECDH
        result = 1;
    }
    else if (version >= OPENSSL_VERSION_1_0_2_RTM)
    {
#ifndef SSL_CTRL_SET_ECDH_AUTO
#define SSL_CTRL_SET_ECDH_AUTO 94
#endif
        // Expanded form of SSL_CTX_set_ecdh_auto(ctx, 1)
        result = SSL_CTX_ctrl(ctx, SSL_CTRL_SET_ECDH_AUTO, 1, NULL);
    }
    else
    {
        EC_KEY *ecdh = EC_KEY_new_by_curve_name(NID_X9_62_prime256v1);

        if (ecdh != NULL)
        {
            result = SSL_CTX_set_tmp_ecdh(ctx, ecdh);
            EC_KEY_free(ecdh);
        }
    }

    return result;
#else
    (void)ctx;
    return 1;
#endif
}

static void ResetCtxProtocolRestrictions(SSL_CTX* ctx)
{
#ifndef SSL_CTRL_SET_MIN_PROTO_VERSION
#define SSL_CTRL_SET_MIN_PROTO_VERSION 123
#endif
#ifndef SSL_CTRL_SET_MAX_PROTO_VERSION
#define SSL_CTRL_SET_MAX_PROTO_VERSION 124
#endif

    SSL_CTX_ctrl(ctx, SSL_CTRL_SET_MIN_PROTO_VERSION, 0, NULL);
    SSL_CTX_ctrl(ctx, SSL_CTRL_SET_MAX_PROTO_VERSION, 0, NULL);
}

void CryptoNative_SslCtxSetProtocolOptions(SSL_CTX* ctx, SslProtocols protocols)
{
    // void shim functions don't lead to exceptions, so skip the unconditional error clearing.

    // Ensure that ECDHE is available
    if (TrySetECDHNamedCurve(ctx) == 0)
    {
        ERR_clear_error();
    }

    // protocols may be 0, meaning system default, in which case let OpenSSL do what OpenSSL wants.
    if (protocols == 0)
    {
        return;
    }

    unsigned long protocolOptions = 0;

    if ((protocols & PAL_SSL_SSL2) != PAL_SSL_SSL2)
    {
        protocolOptions |= SSL_OP_NO_SSLv2;
    }
    if ((protocols & PAL_SSL_SSL3) != PAL_SSL_SSL3)
    {
        protocolOptions |= SSL_OP_NO_SSLv3;
    }
    if ((protocols & PAL_SSL_TLS) != PAL_SSL_TLS)
    {
        protocolOptions |= SSL_OP_NO_TLSv1;
    }
    if ((protocols & PAL_SSL_TLS11) != PAL_SSL_TLS11)
    {
        protocolOptions |= SSL_OP_NO_TLSv1_1;
    }
    if ((protocols & PAL_SSL_TLS12) != PAL_SSL_TLS12)
    {
        protocolOptions |= SSL_OP_NO_TLSv1_2;
    }

    // protocol options were specified, and there's no handler yet for TLS 1.3.
#ifndef SSL_OP_NO_TLSv1_3
#define SSL_OP_NO_TLSv1_3 0x20000000U
#endif
    if ((protocols & PAL_SSL_TLS13) != PAL_SSL_TLS13)
    {
        protocolOptions |= SSL_OP_NO_TLSv1_3;
    }

    // We manually set protocols - we need to reset OpenSSL restrictions
    // to a maximum possible range
    ResetCtxProtocolRestrictions(ctx);

    // OpenSSL 1.0 calls this long, OpenSSL 1.1 calls it unsigned long.
#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Wsign-conversion"
    SSL_CTX_set_options(ctx, protocolOptions);
#pragma clang diagnostic pop
}

SSL* CryptoNative_SslCreate(SSL_CTX* ctx)
{
    ERR_clear_error();
    return SSL_new(ctx);
}

int32_t CryptoNative_SslGetError(SSL* ssl, int32_t ret)
{
    // The error queue should be cleaned outside, if done here there will be no info
    // for managed exception.
    return SSL_get_error(ssl, ret);
}

void CryptoNative_SslDestroy(SSL* ssl)
{
    if (ssl)
    {
        SSL_free(ssl);
    }
}

void CryptoNative_SslCtxDestroy(SSL_CTX* ctx)
{
    if (ctx)
    {
        SSL_CTX_free(ctx);
    }
}

void CryptoNative_SslSetConnectState(SSL* ssl)
{
    // void shim functions don't lead to exceptions, so skip the unconditional error clearing.
    SSL_set_connect_state(ssl);
}

void CryptoNative_SslSetAcceptState(SSL* ssl)
{
    // void shim functions don't lead to exceptions, so skip the unconditional error clearing.
    SSL_set_accept_state(ssl);
}

const char* CryptoNative_SslGetVersion(SSL* ssl)
{
    // No error queue impact.
    return SSL_get_version(ssl);
}

int32_t CryptoNative_SslGetFinished(SSL* ssl, void* buf, int32_t count)
{
    // No error queue impact.

    size_t result = SSL_get_finished(ssl, buf, (size_t)count);
    assert(result <= INT32_MAX);
    return (int32_t)result;
}

int32_t CryptoNative_SslGetPeerFinished(SSL* ssl, void* buf, int32_t count)
{
    // No error queue impact.

    size_t result = SSL_get_peer_finished(ssl, buf, (size_t)count);
    assert(result <= INT32_MAX);
    return (int32_t)result;
}

int32_t CryptoNative_SslSessionReused(SSL* ssl)
{
    // No error queue impact.

    return SSL_session_reused(ssl) == 1;
}

int32_t CryptoNative_SslWrite(SSL* ssl, const void* buf, int32_t num, int32_t* error)
{
    ERR_clear_error();

    int32_t result = SSL_write(ssl, buf, num);

    if (result > 0)
    {
        *error = SSL_ERROR_NONE;
    }
    else
    {
        *error = CryptoNative_SslGetError(ssl, result);
    }

    return result;
}

int32_t CryptoNative_SslRead(SSL* ssl, void* buf, int32_t num, int32_t* error)
{
    ERR_clear_error();

    int32_t result = SSL_read(ssl, buf, num);

    if (result > 0)
    {
        *error = SSL_ERROR_NONE;
    }
    else
    {
        *error = CryptoNative_SslGetError(ssl, result);
    }

    return result;
}

static int verify_callback(int preverify_ok, X509_STORE_CTX* store)
{
    (void)preverify_ok;
    (void)store;
    // We don't care. Real verification happens in managed code.
    return 1;
}

int32_t CryptoNative_SslRenegotiate(SSL* ssl, int32_t* error)
{
    ERR_clear_error();

#ifdef NEED_OPENSSL_1_1
    // TLS1.3 uses different API for renegotiation/delayed client cert request
    #ifndef TLS1_3_VERSION
    #define TLS1_3_VERSION 0x0304
    #endif
    if (SSL_version(ssl) == TLS1_3_VERSION)
    {
        // this is just a sanity check, if TLS 1.3 was negotiated, then the function must be available
        if (API_EXISTS(SSL_verify_client_post_handshake))
        {
            // Post-handshake auth reqires SSL_VERIFY_PEER to be set
            CryptoNative_SslSetVerifyPeer(ssl);
            return SSL_verify_client_post_handshake(ssl);
        }
        else
        {
            return 0;
        }
    }
#endif

    // The openssl context is destroyed so we can't use ticket or session resumption.
    SSL_set_options(ssl, SSL_OP_NO_TICKET | SSL_OP_NO_SESSION_RESUMPTION_ON_RENEGOTIATION);

    int pending = SSL_renegotiate_pending(ssl);
    if (!pending)
    {
        SSL_set_verify(ssl, SSL_VERIFY_PEER, verify_callback);
        int ret = SSL_renegotiate(ssl);
        if(ret != 1)
        {
            *error = CryptoNative_SslGetError(ssl, ret);
            return ret;
        }

        return CryptoNative_SslDoHandshake(ssl, error);
    }

    *error = SSL_ERROR_NONE;
    return 0;
}

int32_t CryptoNative_IsSslRenegotiatePending(SSL* ssl)
{
    ERR_clear_error();

    SSL_peek(ssl, NULL, 0);
    return SSL_renegotiate_pending(ssl) != 0;
}

int32_t CryptoNative_SslShutdown(SSL* ssl)
{
    ERR_clear_error();
    return SSL_shutdown(ssl);
}

void CryptoNative_SslSetBio(SSL* ssl, BIO* rbio, BIO* wbio)
{
    // void shim functions don't lead to exceptions, so skip the unconditional error clearing.
    SSL_set_bio(ssl, rbio, wbio);
}

int32_t CryptoNative_SslDoHandshake(SSL* ssl, int32_t* error)
{
    ERR_clear_error();
    int32_t result = SSL_do_handshake(ssl);
    if (result == 1)
    {
        *error = SSL_ERROR_NONE;
    }
    else
    {
        *error = CryptoNative_SslGetError(ssl, result);
    }

    return result;
}

int32_t CryptoNative_IsSslStateOK(SSL* ssl)
{
    // No error queue impact.
    return SSL_is_init_finished(ssl);
}

X509* CryptoNative_SslGetPeerCertificate(SSL* ssl)
{
    // No error queue impact.
    return SSL_get1_peer_certificate(ssl);
}

X509Stack* CryptoNative_SslGetPeerCertChain(SSL* ssl)
{
    // No error queue impact.
    return SSL_get_peer_cert_chain(ssl);
}

int32_t CryptoNative_SslUseCertificate(SSL* ssl, X509* x)
{
    ERR_clear_error();
    return SSL_use_certificate(ssl, x);
}

int32_t CryptoNative_SslUsePrivateKey(SSL* ssl, EVP_PKEY* pkey)
{
    ERR_clear_error();
    return SSL_use_PrivateKey(ssl, pkey);
}

int32_t CryptoNative_SslCtxUseCertificate(SSL_CTX* ctx, X509* x)
{
    ERR_clear_error();
    return SSL_CTX_use_certificate(ctx, x);
}

int32_t CryptoNative_SslCtxUsePrivateKey(SSL_CTX* ctx, EVP_PKEY* pkey)
{
    ERR_clear_error();
    return SSL_CTX_use_PrivateKey(ctx, pkey);
}

int32_t CryptoNative_SslCtxCheckPrivateKey(SSL_CTX* ctx)
{
    ERR_clear_error();
    return SSL_CTX_check_private_key(ctx);
}

void CryptoNative_SslCtxSetQuietShutdown(SSL_CTX* ctx)
{
    // void shim functions don't lead to exceptions, so skip the unconditional error clearing.
    SSL_CTX_set_quiet_shutdown(ctx, 1);
}

void CryptoNative_SslSetQuietShutdown(SSL* ssl, int mode)
{
    // void shim functions don't lead to exceptions, so skip the unconditional error clearing.
    SSL_set_quiet_shutdown(ssl, mode);
}

X509NameStack* CryptoNative_SslGetClientCAList(SSL* ssl)
{
    // No error queue impact.
    return SSL_get_client_CA_list(ssl);
}

void CryptoNative_SslSetVerifyPeer(SSL* ssl)
{
    // void shim functions don't lead to exceptions, so skip the unconditional error clearing.
    SSL_set_verify(ssl, SSL_VERIFY_PEER, verify_callback);
}

void CryptoNative_SslCtxSetCaching(SSL_CTX* ctx, int mode)
{
    // void shim functions don't lead to exceptions, so skip the unconditional error clearing.

    // We never reuse same CTX for both client and server
    SSL_CTX_ctrl(ctx, SSL_CTRL_SET_SESS_CACHE_MODE,  mode ? SSL_SESS_CACHE_BOTH : SSL_SESS_CACHE_OFF, NULL);
    if (mode == 0)
    {
        SSL_CTX_set_options(ctx, SSL_OP_NO_TICKET);
    }
}

int32_t CryptoNative_SslCtxSetEncryptionPolicy(SSL_CTX* ctx, EncryptionPolicy policy)
{
    // No error queue impact.

    switch (policy)
    {
        case AllowNoEncryption:
        case NoEncryption:
            // No minimum security policy, same as OpenSSL 1.0
            SSL_CTX_set_security_level(ctx, 0);
            ResetCtxProtocolRestrictions(ctx);
            return true;
        case RequireEncryption:
            return true;
    }

    return false;
}

int32_t CryptoNative_SslCtxSetCiphers(SSL_CTX* ctx, const char* cipherList, const char* cipherSuites)
{
    ERR_clear_error();

    int32_t ret = true;

    // for < TLS 1.3
    if (cipherList != NULL)
    {
        ret &= SSL_CTX_set_cipher_list(ctx, cipherList);
        if (!ret)
        {
            return ret;
        }
    }

    // for TLS 1.3
#if HAVE_OPENSSL_SET_CIPHERSUITES
    if (CryptoNative_Tls13Supported() && cipherSuites != NULL)
    {
        ret &= SSL_CTX_set_ciphersuites(ctx, cipherSuites);
    }
#else
    (void)cipherSuites;
#endif

    return ret;
}

int32_t CryptoNative_SetCiphers(SSL* ssl, const char* cipherList, const char* cipherSuites)
{
    ERR_clear_error();

    int32_t ret = true;

    // for < TLS 1.3
    if (cipherList != NULL)
    {
        ret &= SSL_set_cipher_list(ssl, cipherList);
        if (!ret)
        {
            return ret;
        }
    }

    // for TLS 1.3
#if HAVE_OPENSSL_SET_CIPHERSUITES
    if (CryptoNative_Tls13Supported() && cipherSuites != NULL)
    {
        ret &= SSL_set_ciphersuites(ssl, cipherSuites);
    }
#else
    (void)cipherSuites;
#endif

    return ret;
}

const char* CryptoNative_GetOpenSslCipherSuiteName(SSL* ssl, int32_t cipherSuite, int32_t* isTls12OrLower)
{
    // No error queue impact.

#if HAVE_OPENSSL_SET_CIPHERSUITES
    unsigned char cs[2];
    const SSL_CIPHER* cipher;
    const char* ret;

    *isTls12OrLower = 0;
    cs[0] = (cipherSuite >> 8) & 0xFF;
    cs[1] = cipherSuite & 0xFF;
    cipher = SSL_CIPHER_find(ssl, cs);

    if (cipher == NULL)
        return NULL;

    ret = SSL_CIPHER_get_name(cipher);

    if (ret == NULL)
        return NULL;

    // we should get (NONE) only when cipher is NULL
    assert(strcmp("(NONE)", ret) != 0);

    const char* version = SSL_CIPHER_get_version(cipher);
    assert(version != NULL);
    assert(strcmp(version, "unknown") != 0);

    // same rules apply for DTLS as for TLS so just shortcut
    if (version[0] == 'D')
    {
        version++;
    }

    // check if tls1.2 or lower
    // check most common case first
    if (strncmp("TLSv1", version, 5) == 0)
    {
        const char* tlsver = version + 5;
        // true for TLSv1, TLSv1.0, TLSv1.1, TLS1.2, anything else is assumed to be newer
        *isTls12OrLower =
            tlsver[0] == 0 ||
            (tlsver[0] == '.' && tlsver[1] >= '0' && tlsver[1] <= '2' && tlsver[2] == 0);
    }
    else
    {
        // if we don't know it assume it is new
        // worst case scenario OpenSSL will ignore it
        *isTls12OrLower =
            strncmp("SSLv", version, 4) == 0;
    }

    return ret;
#else
    (void)ssl;
    (void)cipherSuite;
    *isTls12OrLower = 0;
    return NULL;
#endif
}

int32_t CryptoNative_Tls13Supported()
{
    // No error queue impact.

#if HAVE_OPENSSL_SET_CIPHERSUITES
    return API_EXISTS(SSL_CTX_set_ciphersuites);
#else
    return false;
#endif
}

int32_t CryptoNative_SslCtxAddExtraChainCert(SSL_CTX* ctx, X509* x509)
{
    ERR_clear_error();

    if (!x509 || !ctx)
    {
        return 0;
    }

    if (SSL_CTX_add_extra_chain_cert(ctx, x509) == 1)
    {
        return 1;
    }

    return 0;
}

int32_t CryptoNative_SslAddExtraChainCert(SSL* ssl, X509* x509)
{
    ERR_clear_error();

    if (!x509 || !ssl)
    {
        return 0;
    }

    if (SSL_ctrl(ssl, SSL_CTRL_CHAIN_CERT, 1,(void*)x509) == 1)
    {
        return 1;
    }

    return 0;
}

int32_t CryptoNative_SslAddClientCAs(SSL* ssl, X509** x509s, uint32_t count)
{
    if (!x509s || !ssl)
    {
        return 0;
    }

    for (uint32_t i = 0; i < count; i++)
    {
        int res = SSL_add_client_CA(ssl, x509s[i]);
        if (res != 1)
        {
            return res;
        }
    }

    return 1;
}

void CryptoNative_SslCtxSetAlpnSelectCb(SSL_CTX* ctx, SslCtxSetAlpnCallback cb, void* arg)
{
    // void shim functions don't lead to exceptions, so skip the unconditional error clearing.

#if HAVE_OPENSSL_ALPN
    if (API_EXISTS(SSL_CTX_set_alpn_select_cb))
    {
        (void)arg;
        SSL_CTX_set_alpn_select_cb(ctx, cb, g_emptyAlpn);
    }
#else
    (void)ctx;
    (void)cb;
    (void)arg;
#endif
}

static int client_certificate_cb(SSL *ssl, void* state)
{
    (void*)ssl;
    (void*)state;
    // if we return negative number handshake will pause with SSL_ERROR_WANT_X509_LOOKUP
    return -1;
}

void CryptoNative_SslSetClientCertCallback(SSL* ssl, int set)
{
    // void shim functions don't lead to exceptions, so skip the unconditional error clearing.

    SSL_set_cert_cb(ssl, set ? client_certificate_cb : NULL, NULL);
}

void CryptoNative_SslSetPostHandshakeAuth(SSL* ssl, int32_t val)
{
#ifdef NEED_OPENSSL_1_1
    if (API_EXISTS(SSL_set_post_handshake_auth))
    {
        SSL_set_post_handshake_auth(ssl, val);
    }
#else
    (void)ssl;
    (void)val;
#endif
}

int32_t CryptoNative_SslSetData(SSL* ssl, void *ptr)
{
    ERR_clear_error();
    return SSL_set_ex_data(ssl, 0, ptr);
}

void* CryptoNative_SslGetData(SSL* ssl)
{
    // No error queue impact.
    return SSL_get_ex_data(ssl, 0);
}

int32_t CryptoNative_SslSetAlpnProtos(SSL* ssl, const uint8_t* protos, uint32_t protos_len)
{
    ERR_clear_error();

#if HAVE_OPENSSL_ALPN
    if (API_EXISTS(SSL_CTX_set_alpn_protos))
    {
        return SSL_set_alpn_protos(ssl, protos, protos_len);
    }
    else
#else
    (void)ctx;
    (void)protos;
    (void)protos_len;
#endif
    {
        return 0;
    }
}

void CryptoNative_SslGet0AlpnSelected(SSL* ssl, const uint8_t** protocol, uint32_t* len)
{
    // void shim functions don't lead to exceptions, so skip the unconditional error clearing.

#if HAVE_OPENSSL_ALPN
    if (API_EXISTS(SSL_get0_alpn_selected))
    {
        SSL_get0_alpn_selected(ssl, protocol, len);
    }
    else
#else
    (void)ssl;
#endif
    {
        *protocol = NULL;
        *len = 0;
    }
}

int32_t CryptoNative_SslSetTlsExtHostName(SSL* ssl, uint8_t* name)
{
    ERR_clear_error();
    return (int32_t)SSL_set_tlsext_host_name(ssl, name);
}

int32_t CryptoNative_SslGetCurrentCipherId(SSL* ssl, int32_t* cipherId)
{
    // No error queue impact.

    const SSL_CIPHER* cipher = SSL_get_current_cipher(ssl);
    if (!cipher)
    {
        *cipherId = -1;
        return 0;
    }

    // OpenSSL uses its own identifier
    // lower 2 bytes of that ID contain IANA value
    *cipherId = SSL_CIPHER_get_id(cipher) & 0xFFFF;

    return 1;
}

// This function generates key pair and creates simple certificate.
static int MakeSelfSignedCertificate(X509 * cert, EVP_PKEY* evp)
{
    RSA* rsa = NULL;
    ASN1_TIME* time = ASN1_TIME_new();
    X509_NAME * asnName;
    unsigned char * name = (unsigned char*)"localhost";

    int ret = 0;

    EVP_PKEY* pkey = CryptoNative_RsaGenerateKey(2048);

    if (pkey != NULL)
    {
        rsa = EVP_PKEY_get1_RSA(pkey);
        EVP_PKEY_free(pkey);
    }

    if (rsa != NULL)
    {
        if (EVP_PKEY_set1_RSA(evp, rsa) == 1)
        {
            rsa = NULL;
        }

        X509_set_pubkey(cert, evp);

        asnName = X509_get_subject_name(cert);
        X509_NAME_add_entry_by_txt(asnName, "CN", MBSTRING_ASC, name, -1, -1, 0);

        asnName =  X509_get_issuer_name(cert);
        X509_NAME_add_entry_by_txt(asnName, "CN", MBSTRING_ASC, name, -1, -1, 0);

        ASN1_TIME_set(time, 0);
        X509_set1_notBefore(cert, time);
        X509_set1_notAfter(cert, time);

        ret = X509_sign(cert, evp, EVP_sha256());
    }

    if (rsa != NULL)
    {
        RSA_free(rsa);
    }

    if (time != NULL)
    {
        ASN1_TIME_free(time);
    }

    return ret;
}

int32_t CryptoNative_OpenSslGetProtocolSupport(SslProtocols protocol)
{
    // Many of these helpers already clear the error queue, and we unconditionally
    // clear it at the end.

    int ret = 0;

    SSL_CTX* clientCtx = CryptoNative_SslCtxCreate(TLS_method());
    SSL_CTX* serverCtx = CryptoNative_SslCtxCreate(TLS_method());
    X509 * cert = X509_new();
    EVP_PKEY* evp = CryptoNative_EvpPkeyCreate();
    BIO *bio1 = BIO_new(BIO_s_mem());
    BIO *bio2 = BIO_new(BIO_s_mem());

    SSL* client = NULL;
    SSL* server = NULL;

    if (clientCtx != NULL && serverCtx != NULL && cert != NULL && evp != NULL && bio1 != NULL && bio2 != NULL)
    {
        CryptoNative_SslCtxSetProtocolOptions(serverCtx, protocol);
        CryptoNative_SslCtxSetProtocolOptions(clientCtx, protocol);
        SSL_CTX_set_verify(clientCtx, SSL_VERIFY_NONE, NULL);
        SSL_CTX_set_verify(serverCtx, SSL_VERIFY_NONE, NULL);

        if (MakeSelfSignedCertificate(cert, evp))
        {
            CryptoNative_SslCtxUseCertificate(serverCtx, cert);
            CryptoNative_SslCtxUsePrivateKey(serverCtx, evp);

            server = CryptoNative_SslCreate(serverCtx);
            SSL_set_accept_state(server);

            client = CryptoNative_SslCreate(clientCtx);
            SSL_set_connect_state(client);

            // set BIOs in opposite
            SSL_set_bio(client, bio1, bio2);
            SSL_set_bio(server, bio2, bio1);
            // SSL_set_bio takes ownership so we need to up reference since same BIO is shared.
            BIO_up_ref(bio1);
            BIO_up_ref(bio2);
            bio1 = NULL;
            bio2 = NULL;

            // Try handshake, client side first.
            SSL* side = client;
            int sslError = 0;
            while (1)
            {
                ret = SSL_do_handshake(side);
                if (ret == 1)
                {
                    break;
                }

                sslError = SSL_get_error(side, ret);
                if (sslError != SSL_ERROR_WANT_READ)
                {
                    break;
                }

                side = side == client ? server : client;
            }
        }
    }

    if (cert != NULL)
    {
        X509_free(cert);
    }

    if (evp != NULL)
    {
        CryptoNative_EvpPkeyDestroy(evp);
    }

    if (bio1)
    {
        BIO_free(bio1);
    }

    if (bio2)
    {
        BIO_free(bio2);
    }

    if (client != NULL)
    {
        SSL_free(client);
    }

    if (server != NULL)
    {
        SSL_free(server);
    }

    ERR_clear_error();

    return ret == 1;
}
