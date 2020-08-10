// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal_ssl.h"
#include "openssl.h"

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
    /* Temporary TLS 1.0/1.1 compat after this line */\
    "ECDHE-ECDSA-AES256-SHA:" \
    "ECDHE-RSA-AES256-SHA:" \

int32_t CryptoNative_EnsureOpenSslInitialized(void);

#ifdef NEED_OPENSSL_1_0
static void EnsureLibSsl10Initialized()
{
    SSL_library_init();
    SSL_load_error_strings();
}
#endif

static int32_t g_config_specified_ciphersuites = 0;

static void DetectCiphersuiteConfiguration()
{
    // This routine will always produce g_config_specified_ciphersuites = 0 on OpenSSL 1.0.x,
    // so if we're building direct for 1.0.x (the only time NEED_OPENSSL_1_1 is undefined) then
    // just omit all the code here.
    //
    // The method uses OpenSSL 1.0.x API, except for the fallback function SSL_CTX_config, to
    // make the portable version easier.
#ifdef NEED_OPENSSL_1_1

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

#elif !defined(FEATURE_DISTRO_AGNOSTIC_SSL) && defined(SSL_SYSTEM_DEFAULT_CIPHER_LIST)

    // The Fedora, RHEL, and CentOS builds replace the normal defaults (with a configuration model).
    // Consider their non-portable builds to always have specified ciphersuites in config.
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
    const SSL_METHOD* method = TLS_method();
    assert(method != NULL);
    return method;
}

SSL_CTX* CryptoNative_SslCtxCreate(SSL_METHOD* method)
{
    SSL_CTX* ctx = SSL_CTX_new(method);

    if (ctx != NULL)
    {
        // As of OpenSSL 1.1.0, compression is disabled by default. In case an older build
        // is used, ensure it's disabled.
        //
        // The other .NET platforms are server-preference, and the common consensus seems
        // to be to use server preference (as of June 2020), so just always assert that.
        SSL_CTX_set_options(ctx, SSL_OP_NO_COMPRESSION | SSL_OP_CIPHER_SERVER_PREFERENCE);

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

static void ResetProtocolRestrictions(SSL_CTX* ctx)
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

void CryptoNative_SetProtocolOptions(SSL_CTX* ctx, SslProtocols protocols)
{
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
    ResetProtocolRestrictions(ctx);

    // OpenSSL 1.0 calls this long, OpenSSL 1.1 calls it unsigned long.
#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Wsign-conversion"
    SSL_CTX_set_options(ctx, protocolOptions);
#pragma clang diagnostic pop
}

SSL* CryptoNative_SslCreate(SSL_CTX* ctx)
{
    return SSL_new(ctx);
}

int32_t CryptoNative_SslGetError(SSL* ssl, int32_t ret)
{
    // This pops off "old" errors left by other operations
    // until the first error is equal to the last one,
    // this should be looked at again when OpenSsl 1.1 is migrated to
    while (ERR_peek_error() != ERR_peek_last_error())
    {
        ERR_get_error();
    }

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
    SSL_set_connect_state(ssl);
}

void CryptoNative_SslSetAcceptState(SSL* ssl)
{
    SSL_set_accept_state(ssl);
}

const char* CryptoNative_SslGetVersion(SSL* ssl)
{
    return SSL_get_version(ssl);
}

int32_t CryptoNative_SslGetFinished(SSL* ssl, void* buf, int32_t count)
{
    size_t result = SSL_get_finished(ssl, buf, (size_t)count);
    assert(result <= INT32_MAX);
    return (int32_t)result;
}

int32_t CryptoNative_SslGetPeerFinished(SSL* ssl, void* buf, int32_t count)
{
    size_t result = SSL_get_peer_finished(ssl, buf, (size_t)count);
    assert(result <= INT32_MAX);
    return (int32_t)result;
}

int32_t CryptoNative_SslSessionReused(SSL* ssl)
{
    return SSL_session_reused(ssl) == 1;
}

int32_t CryptoNative_SslWrite(SSL* ssl, const void* buf, int32_t num)
{
    return SSL_write(ssl, buf, num);
}

int32_t CryptoNative_SslRead(SSL* ssl, void* buf, int32_t num)
{
    return SSL_read(ssl, buf, num);
}

int32_t CryptoNative_IsSslRenegotiatePending(SSL* ssl)
{
    return SSL_renegotiate_pending(ssl) != 0;
}

int32_t CryptoNative_SslShutdown(SSL* ssl)
{
    ERR_clear_error();
    return SSL_shutdown(ssl);
}

void CryptoNative_SslSetBio(SSL* ssl, BIO* rbio, BIO* wbio)
{
    SSL_set_bio(ssl, rbio, wbio);
}

int32_t CryptoNative_SslDoHandshake(SSL* ssl)
{
    ERR_clear_error();
    return SSL_do_handshake(ssl);
}

int32_t CryptoNative_IsSslStateOK(SSL* ssl)
{
    return SSL_is_init_finished(ssl);
}

X509* CryptoNative_SslGetPeerCertificate(SSL* ssl)
{
    return SSL_get_peer_certificate(ssl);
}

X509Stack* CryptoNative_SslGetPeerCertChain(SSL* ssl)
{
    return SSL_get_peer_cert_chain(ssl);
}

int32_t CryptoNative_SslCtxUseCertificate(SSL_CTX* ctx, X509* x)
{
    return SSL_CTX_use_certificate(ctx, x);
}

int32_t CryptoNative_SslCtxUsePrivateKey(SSL_CTX* ctx, EVP_PKEY* pkey)
{
    return SSL_CTX_use_PrivateKey(ctx, pkey);
}

int32_t CryptoNative_SslCtxCheckPrivateKey(SSL_CTX* ctx)
{
    return SSL_CTX_check_private_key(ctx);
}

void CryptoNative_SslCtxSetQuietShutdown(SSL_CTX* ctx)
{
    SSL_CTX_set_quiet_shutdown(ctx, 1);
}

void CryptoNative_SslSetQuietShutdown(SSL* ssl, int mode)
{
    SSL_set_quiet_shutdown(ssl, mode);
}

X509NameStack* CryptoNative_SslGetClientCAList(SSL* ssl)
{
    return SSL_get_client_CA_list(ssl);
}

void CryptoNative_SslCtxSetVerify(SSL_CTX* ctx, SslCtxSetVerifyCallback callback)
{
    int mode = SSL_VERIFY_PEER;

    SSL_CTX_set_verify(ctx, mode, callback);
}

void
CryptoNative_SslCtxSetCertVerifyCallback(SSL_CTX* ctx, SslCtxSetCertVerifyCallbackCallback callback, void* arg)
{
    SSL_CTX_set_cert_verify_callback(ctx, callback, arg);
}

int32_t CryptoNative_SetEncryptionPolicy(SSL_CTX* ctx, EncryptionPolicy policy)
{
    switch (policy)
    {
        case AllowNoEncryption:
        case NoEncryption:
            // No minimum security policy, same as OpenSSL 1.0
            SSL_CTX_set_security_level(ctx, 0);
            ResetProtocolRestrictions(ctx);
            return true;
        case RequireEncryption:
            return true;
    }

    return false;
}

int32_t CryptoNative_SetCiphers(SSL_CTX* ctx, const char* cipherList, const char* cipherSuites)
{
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

const char* CryptoNative_GetOpenSslCipherSuiteName(SSL* ssl, int32_t cipherSuite, int32_t* isTls12OrLower)
{
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
#if HAVE_OPENSSL_SET_CIPHERSUITES
    return API_EXISTS(SSL_CTX_set_ciphersuites);
#else
    return false;
#endif
}

void CryptoNative_SslCtxSetClientCertCallback(SSL_CTX* ctx, SslClientCertCallback callback)
{
    SSL_CTX_set_client_cert_cb(ctx, callback);
}

int32_t CryptoNative_SslAddExtraChainCert(SSL* ssl, X509* x509)
{
    if (!x509 || !ssl)
    {
        return 0;
    }

    SSL_CTX* ssl_ctx = SSL_get_SSL_CTX(ssl);
    if (SSL_CTX_add_extra_chain_cert(ssl_ctx, x509) == 1)
    {
        return 1;
    }

    return 0;
}

void CryptoNative_SslCtxSetAlpnSelectCb(SSL_CTX* ctx, SslCtxSetAlpnCallback cb, void* arg)
{
#if HAVE_OPENSSL_ALPN
    if (API_EXISTS(SSL_CTX_set_alpn_select_cb))
    {
        SSL_CTX_set_alpn_select_cb(ctx, cb, arg);
    }
#else
    (void)ctx;
    (void)cb;
    (void)arg;
#endif
}

int32_t CryptoNative_SslCtxSetAlpnProtos(SSL_CTX* ctx, const uint8_t* protos, uint32_t protos_len)
{
#if HAVE_OPENSSL_ALPN
    if (API_EXISTS(SSL_CTX_set_alpn_protos))
    {
        return SSL_CTX_set_alpn_protos(ctx, protos, protos_len);
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
    return (int32_t)SSL_set_tlsext_host_name(ssl, name);
}

int32_t CryptoNative_SslGetCurrentCipherId(SSL* ssl, int32_t* cipherId)
{
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
