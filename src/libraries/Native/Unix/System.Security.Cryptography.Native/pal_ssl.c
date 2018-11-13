// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

int32_t CryptoNative_EnsureOpenSslInitialized(void);

#ifdef NEED_OPENSSL_1_0
static void EnsureLibSsl10Initialized()
{
    SSL_library_init();
    SSL_load_error_strings();
}
#endif

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
        SSL_CTX_set_options(ctx, SSL_OP_NO_COMPRESSION);
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

static bool StringSpanEquals(const char* lhs, const char* rhs, size_t lhsLength)
{
    if (lhsLength != strlen(rhs))
    {
        return false;
    }

    return strncmp(lhs, rhs, lhsLength) == 0;
}

static CipherAlgorithmType MapCipherAlgorithmType(const char* encryption, size_t encryptionLength)
{
    if (StringSpanEquals(encryption, "DES(56)", encryptionLength))
        return Des;
    if (StringSpanEquals(encryption, "3DES(168)", encryptionLength))
        return TripleDes;
    if (StringSpanEquals(encryption, "RC4(128)", encryptionLength))
        return Rc4;
    if (StringSpanEquals(encryption, "RC2(128)", encryptionLength))
        return Rc2;
    if (StringSpanEquals(encryption, "None", encryptionLength))
        return Null;
    if (StringSpanEquals(encryption, "IDEA(128)", encryptionLength))
        return SSL_IDEA;
    if (StringSpanEquals(encryption, "SEED(128)", encryptionLength))
        return SSL_SEED;
    if (StringSpanEquals(encryption, "AES(128)", encryptionLength))
        return Aes128;
    if (StringSpanEquals(encryption, "AES(256)", encryptionLength))
        return Aes256;
    if (StringSpanEquals(encryption, "Camellia(128)", encryptionLength))
        return SSL_CAMELLIA128;
    if (StringSpanEquals(encryption, "Camellia(256)", encryptionLength))
        return SSL_CAMELLIA256;
    if (StringSpanEquals(encryption, "GOST89(256)", encryptionLength))
        return SSL_eGOST2814789CNT;
    if (StringSpanEquals(encryption, "AESGCM(128)", encryptionLength))
        return Aes128;
    if (StringSpanEquals(encryption, "AESGCM(256)", encryptionLength))
        return Aes256;

    return CipherAlgorithmType_None;
}

static ExchangeAlgorithmType MapExchangeAlgorithmType(const char* keyExchange, size_t keyExchangeLength)
{
    if (StringSpanEquals(keyExchange, "RSA", keyExchangeLength))
        return RsaKeyX;
    if (StringSpanEquals(keyExchange, "DH/RSA", keyExchangeLength))
        return DiffieHellman;
    if (StringSpanEquals(keyExchange, "DH/DSS", keyExchangeLength))
        return DiffieHellman;
    if (StringSpanEquals(keyExchange, "DH", keyExchangeLength))
        return DiffieHellman;
    if (StringSpanEquals(keyExchange, "KRB5", keyExchangeLength))
        return SSL_kKRB5;
    if (StringSpanEquals(keyExchange, "ECDH", keyExchangeLength))
        return SSL_ECDHE;
    if (StringSpanEquals(keyExchange, "ECDH/RSA", keyExchangeLength))
        return SSL_ECDH;
    if (StringSpanEquals(keyExchange, "ECDH/ECDSA", keyExchangeLength))
        return SSL_ECDSA;
    if (StringSpanEquals(keyExchange, "PSK", keyExchangeLength))
        return SSL_kPSK;
    if (StringSpanEquals(keyExchange, "GOST", keyExchangeLength))
        return SSL_kGOST;
    if (StringSpanEquals(keyExchange, "SRP", keyExchangeLength))
        return SSL_kSRP;

    return ExchangeAlgorithmType_None;
}

static void GetHashAlgorithmTypeAndSize(const char* mac,
                                        size_t macLength,
                                        HashAlgorithmType* dataHashAlg,
                                        DataHashSize* hashKeySize)
{
    if (StringSpanEquals(mac, "MD5", macLength))
    {
        *dataHashAlg = Md5;
        *hashKeySize = MD5_HashKeySize;
        return;
    }
    if (StringSpanEquals(mac, "SHA1", macLength))
    {
        *dataHashAlg = Sha1;
        *hashKeySize = SHA1_HashKeySize;
        return;
    }
    if (StringSpanEquals(mac, "GOST94", macLength))
    {
        *dataHashAlg = SSL_GOST94;
        *hashKeySize = GOST_HashKeySize;
        return;
    }
    if (StringSpanEquals(mac, "GOST89", macLength))
    {
        *dataHashAlg = SSL_GOST89;
        *hashKeySize = GOST_HashKeySize;
        return;
    }
    if (StringSpanEquals(mac, "SHA256", macLength))
    {
        *dataHashAlg = SSL_SHA256;
        *hashKeySize = SHA256_HashKeySize;
        return;
    }
    if (StringSpanEquals(mac, "SHA384", macLength))
    {
        *dataHashAlg = SSL_SHA384;
        *hashKeySize = SHA384_HashKeySize;
        return;
    }
    if (StringSpanEquals(mac, "AEAD", macLength))
    {
        *dataHashAlg = SSL_AEAD;
        *hashKeySize = Default;
        return;
    }

    *dataHashAlg = HashAlgorithmType_None;
    *hashKeySize = Default;
}

/*
Given a keyName string like "Enc=XXX", parses the description string and returns the
'XXX' into value and valueLength return variables.

Returns a value indicating whether the pattern starting with keyName was found in description.
*/
static bool GetDescriptionValue(
    const char* description, const char* keyName, size_t keyNameLength, const char** value, size_t* valueLength)
{
    // search for keyName in description
    const char* keyNameStart = strstr(description, keyName);
    if (keyNameStart != NULL)
    {
        // set valueStart to the beginning of the value
        const char* valueStart = keyNameStart + keyNameLength;
        size_t index = 0;

        // the value ends when we hit a space or the end of the string
        while (valueStart[index] != ' ' && valueStart[index] != '\0')
        {
            index++;
        }

        *value = valueStart;
        *valueLength = index;
        return true;
    }

    return false;
}

#define descriptionLength 256

/*
Parses the Kx, Enc, and Mac values out of the SSL_CIPHER_description and
maps the values to the corresponding .NET enum value.
*/
static bool GetSslConnectionInfoFromDescription(const SSL_CIPHER* cipher,
                                                CipherAlgorithmType* dataCipherAlg,
                                                ExchangeAlgorithmType* keyExchangeAlg,
                                                HashAlgorithmType* dataHashAlg,
                                                DataHashSize* hashKeySize)
{
    char description[descriptionLength] = { 0 };
    SSL_CIPHER_description(cipher, description, descriptionLength - 1); // ensure description is NULL-terminated

    const char* keyExchange;
    size_t keyExchangeLength;
    if (!GetDescriptionValue(description, "Kx=", 3, &keyExchange, &keyExchangeLength))
    {
        return false;
    }

    const char* encryption;
    size_t encryptionLength;
    if (!GetDescriptionValue(description, "Enc=", 4, &encryption, &encryptionLength))
    {
        return false;
    }

    const char* mac;
    size_t macLength;
    if (!GetDescriptionValue(description, "Mac=", 4, &mac, &macLength))
    {
        return false;
    }

    *keyExchangeAlg = MapExchangeAlgorithmType(keyExchange, keyExchangeLength);
    *dataCipherAlg = MapCipherAlgorithmType(encryption, encryptionLength);
    GetHashAlgorithmTypeAndSize(mac, macLength, dataHashAlg, hashKeySize);
    return true;
}

int32_t CryptoNative_GetSslConnectionInfo(SSL* ssl,
                                                     CipherAlgorithmType* dataCipherAlg,
                                                     ExchangeAlgorithmType* keyExchangeAlg,
                                                     HashAlgorithmType* dataHashAlg,
                                                     int32_t* dataKeySize,
                                                     DataHashSize* hashKeySize)
{
    const SSL_CIPHER* cipher;

    if (!ssl || !dataCipherAlg || !keyExchangeAlg || !dataHashAlg || !dataKeySize || !hashKeySize)
    {
        goto err;
    }

    cipher = SSL_get_current_cipher(ssl);
    if (!cipher)
    {
        goto err;
    }

    SSL_CIPHER_get_bits(cipher, dataKeySize);

    if (GetSslConnectionInfoFromDescription(cipher, dataCipherAlg, keyExchangeAlg, dataHashAlg, hashKeySize))
    {
        return 1;
    }

err:
    assert(false);

    if (dataCipherAlg)
        *dataCipherAlg = CipherAlgorithmType_None;
    if (keyExchangeAlg)
        *keyExchangeAlg = ExchangeAlgorithmType_None;
    if (dataHashAlg)
        *dataHashAlg = HashAlgorithmType_None;
    if (dataKeySize)
        *dataKeySize = 0;
    if (hashKeySize)
        *hashKeySize = Default;

    return 0;
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

// delimiter ":" is used to allow more than one strings
// below string is corresponding to "AllowNoEncryption"
#define SSL_TXT_Separator ":"
#define SSL_TXT_Exclusion "!"
#define SSL_TXT_AllIncludingNull SSL_TXT_ALL SSL_TXT_Separator SSL_TXT_eNULL
#define SSL_TXT_NotAnon SSL_TXT_Separator SSL_TXT_Exclusion SSL_TXT_aNULL

int32_t CryptoNative_SetEncryptionPolicy(SSL_CTX* ctx, EncryptionPolicy policy)
{
    const char* cipherString = NULL;
    bool clearSecLevel = false;

    switch (policy)
    {
        case RequireEncryption:
            cipherString = SSL_TXT_ALL SSL_TXT_NotAnon;
            break;

        case AllowNoEncryption:
            cipherString = SSL_TXT_AllIncludingNull;
            clearSecLevel = true;
            break;

        case NoEncryption:
            cipherString = SSL_TXT_eNULL;
            clearSecLevel = true;
            break;
    }

    assert(cipherString != NULL);

    if (clearSecLevel)
    {
        // No minimum security policy, same as OpenSSL 1.0
        SSL_CTX_set_security_level(ctx, 0);
    }

    return SSL_CTX_set_cipher_list(ctx, cipherString);
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

