// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#include "pal_ssl.h"
#include "pal_crypto_config.h"

#include <assert.h>

extern "C" const SSL_METHOD* SslV2_3Method()
{
    return SSLv23_method();
}

extern "C" const SSL_METHOD* SslV3Method()
{
    return SSLv3_method();
}

extern "C" const SSL_METHOD* TlsV1Method()
{
    return TLSv1_method();
}

extern "C" const SSL_METHOD* TlsV1_1Method()
{
#if HAVE_TLS_V1_1
    return TLSv1_1_method();
#else
    return nullptr;
#endif
}

extern "C" const SSL_METHOD* TlsV1_2Method()
{
#if HAVE_TLS_V1_2
    return TLSv1_2_method();
#else
    return nullptr;
#endif
}

extern "C" SSL_CTX* SslCtxCreate(SSL_METHOD* method)
{
    return SSL_CTX_new(method);
}

extern "C" int64_t SslCtxCtrl(SSL_CTX* ctx, int32_t cmd, int64_t larg, void* parg)
{
    return SSL_CTX_ctrl(ctx, cmd, larg, parg);
}

extern "C" SSL* SslCreate(SSL_CTX* ctx)
{
    return SSL_new(ctx);
}

extern "C" int32_t SslGetError(SSL* ssl, int32_t ret)
{
    return SSL_get_error(ssl, ret);
}

extern "C" void SslDestroy(SSL* ssl)
{
    if (ssl)
    {
        SSL_free(ssl);
    }
}

extern "C" void SslCtxDestroy(SSL_CTX* ctx)
{
    if (ctx)
    {
        SSL_CTX_free(ctx);
    }
}

extern "C" const char* SSLGetVersion(SSL* ssl)
{
    return SSL_get_version(ssl);
}

static CipherAlgorithmType MapCipherAlgorithmType(const SSL_CIPHER* cipher)
{
    unsigned long enc;

#ifdef HAVE_SSL_CIPHER_ALGORITHMS
    const unsigned long SSL_ENC_MASK = 0x1C3F8000L;
    enc = cipher->algorithms & SSL_ENC_MASK;
#else
    enc = cipher->algorithm_enc;
#endif

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

#ifdef HAVE_SSL_CIPHER_ALGORITHMS
        case SSL_CipherAlgorithm::SSL_AES:
            switch (cipher->alg_bits)
            {
                case 128:
                    return CipherAlgorithmType::Aes128;
                case 256:
                    return CipherAlgorithmType::Aes256;
            }
        case SSL_CipherAlgorithm::SSL_CAMELLIA:
            switch (cipher->alg_bits)
            {
                case 128:
                    return CipherAlgorithmType::SSL_CAMELLIA128;
                case 256:
                    return CipherAlgorithmType::SSL_CAMELLIA256;
            }
#else
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
#endif
    }

    return CipherAlgorithmType::None;
}

static ExchangeAlgorithmType MapExchangeAlgorithmType(const SSL_CIPHER* cipher)
{
    unsigned long mkey;
#ifdef HAVE_SSL_CIPHER_ALGORITHMS
    const unsigned long SSL_MKEY_MASK = 0x000000FFL;
    mkey = cipher->algorithms & SSL_MKEY_MASK;
#else
    mkey = cipher->algorithm_mkey;
#endif

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

#ifdef HAVE_SSL_CIPHER_ALGORITHMS
        case SSL_KeyExchangeAlgorithm::SSL_kECDH:
            return ExchangeAlgorithmType::SSL_ECDH;

        case SSL_KeyExchangeAlgorithm::SSL_kECDHE:
            return ExchangeAlgorithmType::SSL_ECDSA;
#else
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
#endif
    }

    return ExchangeAlgorithmType::None;
}

static HashAlgorithmType MapHashAlgorithmType(const SSL_CIPHER* cipher)
{
    unsigned long mac;
#ifdef HAVE_SSL_CIPHER_ALGORITHMS
    const unsigned long SSL_MAC_MASK = 0x00c00000L;
    mac = cipher->algorithms & SSL_MAC_MASK;
#else
    mac = cipher->algorithm_mac;
#endif

    SSL_DataHashAlgorithm sslMac = static_cast<SSL_DataHashAlgorithm>(mac);
    switch (sslMac)
    {
        case SSL_DataHashAlgorithm::SSL_MD5:
            return HashAlgorithmType::Md5;

        case SSL_DataHashAlgorithm::SSL_SHA1:
            return HashAlgorithmType::Sha1;

#ifndef HAVE_SSL_CIPHER_ALGORITHMS
        case SSL_DataHashAlgorithm::SSL_GOST94:
            return HashAlgorithmType::SSL_GOST94;

        case SSL_DataHashAlgorithm::SSL_GOST89MAC:
            return HashAlgorithmType::SSL_GOST89;

        case SSL_DataHashAlgorithm::SSL_SHA256:
            return HashAlgorithmType::SSL_SHA256;

        case SSL_DataHashAlgorithm::SSL_SHA384:
            return HashAlgorithmType::SSL_SHA384;

        case SSL_DataHashAlgorithm::SSL_AEAD:
            return HashAlgorithmType::SSL_AEAD;
#endif
    }

    return HashAlgorithmType::None;
}

extern "C" int32_t GetSslConnectionInfo(SSL* ssl, CipherAlgorithmType* dataCipherAlg, ExchangeAlgorithmType* keyExchangeAlg, HashAlgorithmType* dataHashAlg, int32_t* dataKeySize)
{
    const SSL_CIPHER* cipher;

    if (!ssl || !dataCipherAlg || !keyExchangeAlg || !dataHashAlg || !dataKeySize)
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
    *dataHashAlg = MapHashAlgorithmType(cipher);
    *dataKeySize = cipher->alg_bits;

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

    return 0;
}

extern "C" int32_t SslWrite(SSL* ssl, const void* buf, int32_t num)
{
    return SSL_write(ssl, buf, num);
}

extern "C" int32_t SslRead(SSL* ssl, void* buf, int32_t num)
{
    return SSL_read(ssl, buf, num);
}
