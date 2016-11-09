// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "pal_ssl.h"
#include "pal_crypto_config.h"

#include <assert.h>
#include <string.h>
#include <openssl/err.h>

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
#ifdef OPENSSL_NO_SSL3_METHOD
    return nullptr;
#else
    const SSL_METHOD* method = SSLv3_method();
    assert(method != nullptr);
    return method;
#endif
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
    SSL_CTX* ctx = SSL_CTX_new(method);

    if (ctx != nullptr)
    {
        // As of OpenSSL 1.1.0, compression is disabled by default. In case an older build
        // is used, ensure it's disabled.
        SSL_CTX_set_options(ctx, SSL_OP_NO_COMPRESSION);
    }

    return ctx;
}

extern "C" void CryptoNative_SetProtocolOptions(SSL_CTX* ctx, SslProtocols protocols)
{
    // protocols may be 0 (default). Less secure protocols should be excluded in this case.    
    long protocolOptions = 0;

    if ((protocols & PAL_SSL_SSL2) != PAL_SSL_SSL2)
    {
        protocolOptions |= SSL_OP_NO_SSLv2;
    }
#ifndef OPENSSL_NO_SSL3
    if ((protocols & PAL_SSL_SSL3) != PAL_SSL_SSL3)
    {
        protocolOptions |= SSL_OP_NO_SSLv3;
    }
#endif
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
        return CipherAlgorithmType::Des;
    if (StringSpanEquals(encryption, "3DES(168)", encryptionLength))
        return CipherAlgorithmType::TripleDes;
    if (StringSpanEquals(encryption, "RC4(128)", encryptionLength))
        return CipherAlgorithmType::Rc4;
    if (StringSpanEquals(encryption, "RC2(128)", encryptionLength))
        return CipherAlgorithmType::Rc2;
    if (StringSpanEquals(encryption, "None", encryptionLength))
        return CipherAlgorithmType::Null;
    if (StringSpanEquals(encryption, "IDEA(128)", encryptionLength))
        return CipherAlgorithmType::SSL_IDEA;
    if (StringSpanEquals(encryption, "SEED(128)", encryptionLength))
        return CipherAlgorithmType::SSL_SEED;
    if (StringSpanEquals(encryption, "AES(128)", encryptionLength))
        return CipherAlgorithmType::Aes128;
    if (StringSpanEquals(encryption, "AES(256)", encryptionLength))
        return CipherAlgorithmType::Aes256;
    if (StringSpanEquals(encryption, "Camellia(128)", encryptionLength))
        return CipherAlgorithmType::SSL_CAMELLIA128;
    if (StringSpanEquals(encryption, "Camellia(256)", encryptionLength))
        return CipherAlgorithmType::SSL_CAMELLIA256;
    if (StringSpanEquals(encryption, "GOST89(256)", encryptionLength))
        return CipherAlgorithmType::SSL_eGOST2814789CNT;
    if (StringSpanEquals(encryption, "AESGCM(128)", encryptionLength))
        return CipherAlgorithmType::Aes128;
    if (StringSpanEquals(encryption, "AESGCM(256)", encryptionLength))
        return CipherAlgorithmType::Aes256;

    return CipherAlgorithmType::None;
}

static ExchangeAlgorithmType MapExchangeAlgorithmType(const char* keyExchange, size_t keyExchangeLength)
{
    if (StringSpanEquals(keyExchange, "RSA", keyExchangeLength))
        return ExchangeAlgorithmType::RsaKeyX;
    if (StringSpanEquals(keyExchange, "DH/RSA", keyExchangeLength))
        return ExchangeAlgorithmType::DiffieHellman;
    if (StringSpanEquals(keyExchange, "DH/DSS", keyExchangeLength))
        return ExchangeAlgorithmType::DiffieHellman;
    if (StringSpanEquals(keyExchange, "DH", keyExchangeLength))
        return ExchangeAlgorithmType::DiffieHellman;
    if (StringSpanEquals(keyExchange, "KRB5", keyExchangeLength))
        return ExchangeAlgorithmType::SSL_kKRB5;
    if (StringSpanEquals(keyExchange, "ECDH", keyExchangeLength))
        return ExchangeAlgorithmType::SSL_ECDHE;
    if (StringSpanEquals(keyExchange, "ECDH/RSA", keyExchangeLength))
        return ExchangeAlgorithmType::SSL_ECDH;
    if (StringSpanEquals(keyExchange, "ECDH/ECDSA", keyExchangeLength))
        return ExchangeAlgorithmType::SSL_ECDSA;
    if (StringSpanEquals(keyExchange, "PSK", keyExchangeLength))
        return ExchangeAlgorithmType::SSL_kPSK;
    if (StringSpanEquals(keyExchange, "GOST", keyExchangeLength))
        return ExchangeAlgorithmType::SSL_kGOST;
    if (StringSpanEquals(keyExchange, "SRP", keyExchangeLength))
        return ExchangeAlgorithmType::SSL_kSRP;

    return ExchangeAlgorithmType::None;
}

static void GetHashAlgorithmTypeAndSize(const char* mac, size_t macLength, HashAlgorithmType& dataHashAlg, DataHashSize& hashKeySize)
{
    if (StringSpanEquals(mac, "MD5", macLength))
    {
        dataHashAlg = HashAlgorithmType::Md5;
        hashKeySize = DataHashSize::MD5_HashKeySize;
        return;
    }
    if (StringSpanEquals(mac, "SHA1", macLength))
    {
        dataHashAlg = HashAlgorithmType::Sha1;
        hashKeySize = DataHashSize::SHA1_HashKeySize;
        return;
    }
    if (StringSpanEquals(mac, "GOST94", macLength))
    {
        dataHashAlg = HashAlgorithmType::SSL_GOST94;
        hashKeySize = DataHashSize::GOST_HashKeySize;
        return;
    }
    if (StringSpanEquals(mac, "GOST89", macLength))
    {
        dataHashAlg = HashAlgorithmType::SSL_GOST89;
        hashKeySize = DataHashSize::GOST_HashKeySize;
        return;
    }
    if (StringSpanEquals(mac, "SHA256", macLength))
    {
        dataHashAlg = HashAlgorithmType::SSL_SHA256;
        hashKeySize = DataHashSize::SHA256_HashKeySize;
        return;
    }
    if (StringSpanEquals(mac, "SHA384", macLength))
    {
        dataHashAlg = HashAlgorithmType::SSL_SHA384;
        hashKeySize = DataHashSize::SHA384_HashKeySize;
        return;
    }
    if (StringSpanEquals(mac, "AEAD", macLength))
    {
        dataHashAlg = HashAlgorithmType::SSL_AEAD;
        hashKeySize = DataHashSize::Default;
        return;
    }

    dataHashAlg = HashAlgorithmType::None;
    hashKeySize = DataHashSize::Default;
}

/*
Given a keyName string like "Enc=XXX", parses the description string and returns the
'XXX' into value and valueLength return variables.

Returns a value indicating whether the pattern starting with keyName was found in description.
*/
static bool GetDescriptionValue(const char* description, const char* keyName, size_t keyNameLength, const char** value, size_t& valueLength)
{
    // search for keyName in description
    const char* keyNameStart = strstr(description, keyName);
    if (keyNameStart != nullptr)
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
        valueLength = index;
        return true;
    }

    return false;
}

/*
Parses the Kx, Enc, and Mac values out of the SSL_CIPHER_description and
maps the values to the corresponding .NET enum value.
*/
static bool GetSslConnectionInfoFromDescription(const SSL_CIPHER* cipher,
                                                CipherAlgorithmType& dataCipherAlg,
                                                ExchangeAlgorithmType& keyExchangeAlg,
                                                HashAlgorithmType& dataHashAlg,
                                                DataHashSize& hashKeySize)
{
    const int descriptionLength = 256;
    char description[descriptionLength] = {};
    SSL_CIPHER_description(cipher, description, descriptionLength - 1); // ensure description is NULL-terminated

    const char* keyExchange;
    size_t keyExchangeLength;
    if (!GetDescriptionValue(description, "Kx=", 3, &keyExchange, keyExchangeLength))
    {
        return false;
    }

    const char* encryption;
    size_t encryptionLength;
    if (!GetDescriptionValue(description, "Enc=", 4, &encryption, encryptionLength))
    {
        return false;
    }

    const char* mac;
    size_t macLength;
    if (!GetDescriptionValue(description, "Mac=", 4, &mac, macLength))
    {
        return false;
    }

    keyExchangeAlg = MapExchangeAlgorithmType(keyExchange, keyExchangeLength);
    dataCipherAlg = MapCipherAlgorithmType(encryption, encryptionLength);
    GetHashAlgorithmTypeAndSize(mac, macLength, dataHashAlg, hashKeySize);
    return true;
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

    *dataKeySize = cipher->alg_bits;
    if (GetSslConnectionInfoFromDescription(cipher, *dataCipherAlg, *keyExchangeAlg, *dataHashAlg, *hashKeySize))
    {
        return 1;
    }

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
    ERR_clear_error();
    return SSL_write(ssl, buf, num);
}

extern "C" int32_t CryptoNative_SslRead(SSL* ssl, void* buf, int32_t num)
{
    ERR_clear_error();
    return SSL_read(ssl, buf, num);
}

extern "C" int32_t CryptoNative_IsSslRenegotiatePending(SSL* ssl)
{
    return SSL_renegotiate_pending(ssl) != 0;
}

extern "C" int32_t CryptoNative_SslShutdown(SSL* ssl)
{
    ERR_clear_error();
    return SSL_shutdown(ssl);
}

extern "C" void CryptoNative_SslSetBio(SSL* ssl, BIO* rbio, BIO* wbio)
{
    SSL_set_bio(ssl, rbio, wbio);
}

extern "C" int32_t CryptoNative_SslDoHandshake(SSL* ssl)
{
    ERR_clear_error();
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
    int mode = SSL_VERIFY_PEER;

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

extern "C" int32_t CryptoNative_SetEncryptionPolicy(SSL_CTX* ctx, EncryptionPolicy policy)
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

    return SSL_CTX_set_cipher_list(ctx, cipherString);
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
    // This function is kept for compatibility with RC2 builds on a jagged upgrade path.
    // Removal is tracked via issue #8504.
    if (header)
    {
        *header = SSL3_RT_HEADER_LENGTH;
    }

    if (trailer)
    {
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
