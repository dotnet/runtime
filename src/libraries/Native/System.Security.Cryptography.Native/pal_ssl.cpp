// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#include "pal_ssl.h"
#include "pal_crypto_config.h"

#include <assert.h>

static_assert(PAL_SSL_ERROR_NONE == SSL_ERROR_NONE, "");
static_assert(PAL_SSL_ERROR_SSL == SSL_ERROR_SSL, "");
static_assert(PAL_SSL_ERROR_WANT_READ == SSL_ERROR_WANT_READ, "");
static_assert(PAL_SSL_ERROR_WANT_WRITE == SSL_ERROR_WANT_WRITE, "");
static_assert(PAL_SSL_ERROR_SYSCALL == SSL_ERROR_SYSCALL, "");
static_assert(PAL_SSL_ERROR_ZERO_RETURN == SSL_ERROR_ZERO_RETURN, "");

extern "C" void CryptoNative_EnsureLibSslInitialized()
{
    SSL_library_init();
    SSL_load_error_strings();
}

extern "C" const SSL_METHOD* CryptoNative_SslV2_3Method()
{
    const SSL_METHOD* method = SSLv23_method();
    assert(method != nullptr);
    return method;
}

extern "C" const SSL_METHOD* CryptoNative_SslV3Method()
{
    const SSL_METHOD* method = SSLv3_method();
    assert(method != nullptr);
    return method;
}

extern "C" const SSL_METHOD* CryptoNative_TlsV1Method()
{
    const SSL_METHOD* method = TLSv1_method();
    assert(method != nullptr);
    return method;
}

extern "C" const SSL_METHOD* CryptoNative_TlsV1_1Method()
{
#if HAVE_TLS_V1_1
    const SSL_METHOD* method = TLSv1_1_method();
    assert(method != nullptr);
    return method;
#else
    return nullptr;
#endif
}

extern "C" const SSL_METHOD* CryptoNative_TlsV1_2Method()
{
#if HAVE_TLS_V1_2
    const SSL_METHOD* method = TLSv1_2_method();
    assert(method != nullptr);
    return method;
#else
    return nullptr;
#endif
}

extern "C" SSL_CTX* CryptoNative_SslCtxCreate(SSL_METHOD* method)
{
    return SSL_CTX_new(method);
}

extern "C" void CryptoNative_SetProtocolOptions(SSL_CTX* ctx, SslProtocols protocols)
{
    long protocolOptions = 0;

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
#if HAVE_TLS_V1_1
    if ((protocols & PAL_SSL_TLS11) != PAL_SSL_TLS11)
    {
        protocolOptions |= SSL_OP_NO_TLSv1_1;
    }
#endif
#if HAVE_TLS_V1_2
    if ((protocols & PAL_SSL_TLS12) != PAL_SSL_TLS12)
    {
        protocolOptions |= SSL_OP_NO_TLSv1_2;
    }
#endif

    SSL_CTX_set_options(ctx, protocolOptions);
}

extern "C" SSL* CryptoNative_SslCreate(SSL_CTX* ctx)
{
    return SSL_new(ctx);
}

extern "C" int32_t CryptoNative_SslGetError(SSL* ssl, int32_t ret)
{
    return SSL_get_error(ssl, ret);
}

extern "C" void CryptoNative_SslDestroy(SSL* ssl)
{
    if (ssl)
    {
        SSL_free(ssl);
    }
}

extern "C" void CryptoNative_SslCtxDestroy(SSL_CTX* ctx)
{
    if (ctx)
    {
        SSL_CTX_free(ctx);
    }
}

extern "C" void CryptoNative_SslSetConnectState(SSL* ssl)
{
    SSL_set_connect_state(ssl);
}

extern "C" void CryptoNative_SslSetAcceptState(SSL* ssl)
{
    SSL_set_accept_state(ssl);
}

extern "C" const char* CryptoNative_SslGetVersion(SSL* ssl)
{
    return SSL_get_version(ssl);
}

extern "C" int32_t CryptoNative_SslGetFinished(SSL* ssl, void* buf, int32_t count)
{
    size_t result = SSL_get_finished(ssl, buf, size_t(count));
    assert(result <= INT32_MAX);
    return static_cast<int32_t>(result);
}

extern "C" int32_t CryptoNative_SslGetPeerFinished(SSL* ssl, void* buf, int32_t count)
{
    size_t result = SSL_get_peer_finished(ssl, buf, size_t(count));
    assert(result <= INT32_MAX);
    return static_cast<int32_t>(result);
}

extern "C" int32_t CryptoNative_SslSessionReused(SSL* ssl)
{
    return SSL_session_reused(ssl) == 1;
}

/*
The values used in OpenSSL for SSL_CIPHER algorithm_enc.
*/
enum class SSL_CipherAlgorithm : int64_t
{
    SSL_DES = 1,
    SSL_3DES = 2,
    SSL_RC4 = 4,
    SSL_RC2 = 8,
    SSL_IDEA = 16,
    SSL_eNULL = 32,
    SSL_AES128 = 64,
    SSL_AES256 = 128,
    SSL_CAMELLIA128 = 256,
    SSL_CAMELLIA256 = 512,
    SSL_eGOST2814789CNT = 1024,
    SSL_SEED = 2048,
    SSL_AES128GCM = 4096,
    SSL_AES256GCM = 8192
};

static CipherAlgorithmType MapCipherAlgorithmType(const SSL_CIPHER* cipher)
{
    unsigned long enc = cipher->algorithm_enc;

    SSL_CipherAlgorithm sslEnc = static_cast<SSL_CipherAlgorithm>(enc);
    switch (sslEnc)
    {
        case SSL_CipherAlgorithm::SSL_DES:
            return CipherAlgorithmType::Des;

        case SSL_CipherAlgorithm::SSL_3DES:
            return CipherAlgorithmType::TripleDes;

        case SSL_CipherAlgorithm::SSL_RC4:
            return CipherAlgorithmType::Rc4;

        case SSL_CipherAlgorithm::SSL_RC2:
            return CipherAlgorithmType::Rc2;

        case SSL_CipherAlgorithm::SSL_eNULL:
            return CipherAlgorithmType::Null;

        case SSL_CipherAlgorithm::SSL_IDEA:
            return CipherAlgorithmType::SSL_IDEA;

        case SSL_CipherAlgorithm::SSL_SEED:
            return CipherAlgorithmType::SSL_SEED;

        case SSL_CipherAlgorithm::SSL_AES128:
            return CipherAlgorithmType::Aes128;

        case SSL_CipherAlgorithm::SSL_AES256:
            return CipherAlgorithmType::Aes256;

        case SSL_CipherAlgorithm::SSL_CAMELLIA128:
            return CipherAlgorithmType::SSL_CAMELLIA128;

        case SSL_CipherAlgorithm::SSL_CAMELLIA256:
            return CipherAlgorithmType::SSL_CAMELLIA256;

        case SSL_CipherAlgorithm::SSL_eGOST2814789CNT:
            return CipherAlgorithmType::SSL_eGOST2814789CNT;

        case SSL_CipherAlgorithm::SSL_AES128GCM:
            return CipherAlgorithmType::Aes128;

        case SSL_CipherAlgorithm::SSL_AES256GCM:
            return CipherAlgorithmType::Aes256;
    }

    return CipherAlgorithmType::None;
}

/*
The values used in OpenSSL for SSL_CIPHER algorithm_mkey.
*/
enum class SSL_KeyExchangeAlgorithm : int64_t
{
    SSL_kRSA = 1,
    /* DH cert, RSA CA cert */
    SSL_kDHr = 2,
    /* DH cert, DSA CA cert */
    SSL_kDHd = 4,
    /* tmp DH key no DH cert */
    SSL_kEDH = 8,
    /* Kerberos5 key exchange */
    SSL_kKRB5 = 16,
    /* ECDH cert, RSA CA cert */
    SSL_kECDHr = 32,
    /* ECDH cert, ECDSA CA cert */
    SSL_kECDHe = 64,
    SSL_kEECDH = 128,
    SSL_kPSK = 256,
    SSL_kGOST = 512,
    SSL_kSRP = 1024,
};

static ExchangeAlgorithmType MapExchangeAlgorithmType(const SSL_CIPHER* cipher)
{
    unsigned long mkey = cipher->algorithm_mkey;

    SSL_KeyExchangeAlgorithm sslMkey = static_cast<SSL_KeyExchangeAlgorithm>(mkey);
    switch (sslMkey)
    {
        case SSL_KeyExchangeAlgorithm::SSL_kRSA:
            return ExchangeAlgorithmType::RsaKeyX;

        case SSL_KeyExchangeAlgorithm::SSL_kDHr:
            return ExchangeAlgorithmType::DiffieHellman;

        case SSL_KeyExchangeAlgorithm::SSL_kDHd:
            return ExchangeAlgorithmType::DiffieHellman;

        case SSL_KeyExchangeAlgorithm::SSL_kEDH:
            return ExchangeAlgorithmType::DiffieHellman;

        case SSL_KeyExchangeAlgorithm::SSL_kKRB5:
            return ExchangeAlgorithmType::SSL_kKRB5;

        case SSL_KeyExchangeAlgorithm::SSL_kECDHr:
            return ExchangeAlgorithmType::SSL_ECDH;

        case SSL_KeyExchangeAlgorithm::SSL_kECDHe:
            return ExchangeAlgorithmType::SSL_ECDSA;

        case SSL_KeyExchangeAlgorithm::SSL_kEECDH:
            return ExchangeAlgorithmType::SSL_ECDSA;

        case SSL_KeyExchangeAlgorithm::SSL_kPSK:
            return ExchangeAlgorithmType::SSL_kPSK;

        case SSL_KeyExchangeAlgorithm::SSL_kGOST:
            return ExchangeAlgorithmType::SSL_kGOST;

        case SSL_KeyExchangeAlgorithm::SSL_kSRP:
            return ExchangeAlgorithmType::SSL_kSRP;
    }

    return ExchangeAlgorithmType::None;
}

/*
The values used in OpenSSL for SSL_CIPHER algorithm_mac.
*/
enum class SSL_DataHashAlgorithm : int64_t
{
    SSL_MD5 = 1,
    SSL_SHA1 = 2,
    SSL_GOST94 = 4,
    SSL_GOST89MAC = 8,
    SSL_SHA256 = 16,
    SSL_SHA384 = 32,
    SSL_AEAD = 64
};

static void
GetHashAlgorithmTypeAndSize(const SSL_CIPHER* cipher, HashAlgorithmType* dataHashAlg, DataHashSize* hashKeySize)
{
    unsigned long mac = cipher->algorithm_mac;

    SSL_DataHashAlgorithm sslMac = static_cast<SSL_DataHashAlgorithm>(mac);
    switch (sslMac)
    {
        case SSL_DataHashAlgorithm::SSL_MD5:
            *dataHashAlg = HashAlgorithmType::Md5;
            *hashKeySize = DataHashSize::MD5_HashKeySize;
            return;

        case SSL_DataHashAlgorithm::SSL_SHA1:
            *dataHashAlg = HashAlgorithmType::Sha1;
            *hashKeySize = DataHashSize::SHA1_HashKeySize;
            return;

        case SSL_DataHashAlgorithm::SSL_GOST94:
            *dataHashAlg = HashAlgorithmType::SSL_GOST94;
            *hashKeySize = DataHashSize::GOST_HashKeySize;
            return;

        case SSL_DataHashAlgorithm::SSL_GOST89MAC:
            *dataHashAlg = HashAlgorithmType::SSL_GOST89;
            *hashKeySize = DataHashSize::GOST_HashKeySize;
            return;

        case SSL_DataHashAlgorithm::SSL_SHA256:
            *dataHashAlg = HashAlgorithmType::SSL_SHA256;
            *hashKeySize = DataHashSize::SHA256_HashKeySize;
            return;

        case SSL_DataHashAlgorithm::SSL_SHA384:
            *dataHashAlg = HashAlgorithmType::SSL_SHA384;
            *hashKeySize = DataHashSize::SHA384_HashKeySize;
            return;

        case SSL_DataHashAlgorithm::SSL_AEAD:
            *dataHashAlg = HashAlgorithmType::SSL_AEAD;
            *hashKeySize = DataHashSize::Default;
            return;
    }

    *dataHashAlg = HashAlgorithmType::None;
    *hashKeySize = DataHashSize::Default;
    return;
}

extern "C" int32_t CryptoNative_GetSslConnectionInfo(SSL* ssl,
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

    *dataCipherAlg = MapCipherAlgorithmType(cipher);
    *keyExchangeAlg = MapExchangeAlgorithmType(cipher);
    *dataKeySize = cipher->alg_bits;
    GetHashAlgorithmTypeAndSize(cipher, dataHashAlg, hashKeySize);

    return 1;

err:
    assert(false);

    if (dataCipherAlg)
        *dataCipherAlg = CipherAlgorithmType::None;
    if (keyExchangeAlg)
        *keyExchangeAlg = ExchangeAlgorithmType::None;
    if (dataHashAlg)
        *dataHashAlg = HashAlgorithmType::None;
    if (dataKeySize)
        *dataKeySize = 0;
    if (hashKeySize)
        *hashKeySize = DataHashSize::Default;

    return 0;
}

extern "C" int32_t CryptoNative_SslWrite(SSL* ssl, const void* buf, int32_t num)
{
    return SSL_write(ssl, buf, num);
}

extern "C" int32_t CryptoNative_SslRead(SSL* ssl, void* buf, int32_t num)
{
    return SSL_read(ssl, buf, num);
}

extern "C" int32_t CryptoNative_IsSslRenegotiatePending(SSL* ssl)
{
    return SSL_renegotiate_pending(ssl) != 0;
}

extern "C" int32_t CryptoNative_SslShutdown(SSL* ssl)
{
    return SSL_shutdown(ssl);
}

extern "C" void CryptoNative_SslSetBio(SSL* ssl, BIO* rbio, BIO* wbio)
{
    SSL_set_bio(ssl, rbio, wbio);
}

extern "C" int32_t CryptoNative_SslDoHandshake(SSL* ssl)
{
    return SSL_do_handshake(ssl);
}

extern "C" int32_t CryptoNative_IsSslStateOK(SSL* ssl)
{
    return SSL_state(ssl) == SSL_ST_OK;
}

extern "C" X509* CryptoNative_SslGetPeerCertificate(SSL* ssl)
{
    return SSL_get_peer_certificate(ssl);
}

extern "C" X509Stack* CryptoNative_SslGetPeerCertChain(SSL* ssl)
{
    return SSL_get_peer_cert_chain(ssl);
}

extern "C" int32_t CryptoNative_SslCtxUseCertificate(SSL_CTX* ctx, X509* x)
{
    return SSL_CTX_use_certificate(ctx, x);
}

extern "C" int32_t CryptoNative_SslCtxUsePrivateKey(SSL_CTX* ctx, EVP_PKEY* pkey)
{
    return SSL_CTX_use_PrivateKey(ctx, pkey);
}

extern "C" int32_t CryptoNative_SslCtxCheckPrivateKey(SSL_CTX* ctx)
{
    return SSL_CTX_check_private_key(ctx);
}

extern "C" void CryptoNative_SslCtxSetQuietShutdown(SSL_CTX* ctx)
{
    SSL_CTX_set_quiet_shutdown(ctx, 1);
}

extern "C" X509NameStack* CryptoNative_SslGetClientCAList(SSL* ssl)
{
    return SSL_get_client_CA_list(ssl);
}

extern "C" void CryptoNative_SslCtxSetVerify(SSL_CTX* ctx, SslCtxSetVerifyCallback callback)
{
    int mode = SSL_VERIFY_PEER | SSL_VERIFY_FAIL_IF_NO_PEER_CERT;

    SSL_CTX_set_verify(ctx, mode, callback);
}

extern "C" void
CryptoNative_SslCtxSetCertVerifyCallback(SSL_CTX* ctx, SslCtxSetCertVerifyCallbackCallback callback, void* arg)
{
    SSL_CTX_set_cert_verify_callback(ctx, callback, arg);
}

// delimiter ":" is used to allow more than one strings
// below string is corresponding to "AllowNoEncryption"
#define SSL_TXT_Separator ":"
#define SSL_TXT_AllIncludingNull SSL_TXT_ALL SSL_TXT_Separator SSL_TXT_eNULL

extern "C" void CryptoNative_SetEncryptionPolicy(SSL_CTX* ctx, EncryptionPolicy policy)
{
    const char* cipherString = nullptr;
    switch (policy)
    {
        case EncryptionPolicy::RequireEncryption:
            cipherString = SSL_TXT_ALL;
            break;

        case EncryptionPolicy::AllowNoEncryption:
            cipherString = SSL_TXT_AllIncludingNull;
            break;

        case EncryptionPolicy::NoEncryption:
            cipherString = SSL_TXT_eNULL;
            break;
    }

    assert(cipherString != nullptr);

    SSL_CTX_set_cipher_list(ctx, cipherString);
}

extern "C" void CryptoNative_SslCtxSetClientCAList(SSL_CTX* ctx, X509NameStack* list)
{
    SSL_CTX_set_client_CA_list(ctx, list);
}

extern "C" void CryptoNative_SslCtxSetClientCertCallback(SSL_CTX* ctx, SslClientCertCallback callback)
{
    SSL_CTX_set_client_cert_cb(ctx, callback);
}

extern "C" void CryptoNative_GetStreamSizes(int32_t* header, int32_t* trailer, int32_t* maximumMessage)
{
    if (header)
    {
        *header = SSL3_RT_HEADER_LENGTH;
    }

    if (trailer)
    {
        // TODO (Issue #4223) : Trailer size requirement is changing based on protocol
        //       SSL3/TLS1.0 - 68, TLS1.1 - 37 and TLS1.2 - 24
        //       Current usage is only to compute max input buffer size for
        //       encryption and so setting to the max

        *trailer = 68;
    }

    if (maximumMessage)
    {
        *maximumMessage = SSL3_RT_MAX_PLAIN_LENGTH;
    }
}

extern "C" int32_t CryptoNative_SslAddExtraChainCert(SSL* ssl, X509* x509)
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
