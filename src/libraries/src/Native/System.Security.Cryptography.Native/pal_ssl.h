// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#include "pal_types.h"

#include <openssl/ssl.h>

/*
These values should be kept in sync with System.Security.Authentication.CipherAlgorithmType.
*/
enum class CipherAlgorithmType : int32_t
{
    None = 0,
    Null = 24576,
    Des = 26113,
    Rc2 = 26114,
    TripleDes = 26115,
    Aes128 = 26126,
    Aes192 = 26127,
    Aes256 = 26128,
    Aes = 26129,
    Rc4 = 26625,

    //Algorithm constants which are not present in the managed CipherAlgorithmType enum.
    SSL_IDEA = 229380,
    SSL_CAMELLIA128 = 229381,
    SSL_CAMELLIA256 = 229382,
    SSL_eGOST2814789CNT = 229383,
    SSL_SEED = 229384,
};

/*
The values used in OpenSSL for SSL_CIPHER algorithm_enc.
*/
enum class SSL_CipherAlgorithm : int64_t
{
#ifdef HAVE_SSL_CIPHER_ALGORITHMS
    SSL_DES = 0x00008000L,
    SSL_3DES = 0x00010000L,
    SSL_RC4 = 0x00020000L,
    SSL_RC2 = 0x00040000L,
    SSL_IDEA = 0x00080000L,
    //SSL_eFZA = 0x00100000L,  this value is defined in ssl_locl.h, but has no match
    SSL_eNULL = 0x00200000L,
    SSL_AES = 0x04000000L,
    SSL_CAMELLIA = 0x08000000L,
    SSL_SEED = 0x10000000L,
#else
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
#endif
};

/*
These values should be kept in sync with System.Security.Authentication.ExchangeAlgorithmType.
*/
enum class ExchangeAlgorithmType : int32_t
{
    None,
    RsaSign = 9216,
    RsaKeyX = 41984,
    DiffieHellman = 43522,

    //ExchangeAlgorithm constants which are not present in the managed ExchangeAlgorithmType enum.
    SSL_ECDH = 43525,
    SSL_ECDSA = 41475,
    SSL_kPSK = 229390,
    SSL_kGOST = 229391,
    SSL_kSRP = 229392,
    SSL_kKRB5 = 229393,
};

/*
The values used in OpenSSL for SSL_CIPHER algorithm_mkey.
*/
enum class SSL_KeyExchangeAlgorithm : int64_t
{
#ifdef HAVE_SSL_CIPHER_ALGORITHMS
    SSL_kRSA = 0x00000001L,
    SSL_kDHr = 0x00000002L,
    SSL_kDHd = 0x00000004L,
    //SSL_kFZA = 0x00000008L,  this value is defined in ssl_locl.h, but has no match
    SSL_kEDH = 0x00000010L,
    SSL_kKRB5 = 0x00000020L,
    SSL_kECDH = 0x00000040L,
    SSL_kECDHE = 0x00000080L,
#else
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
#endif
};

/*
These values should be kept in sync with System.Security.Authentication.HashAlgorithmType.
*/
enum class HashAlgorithmType : int32_t
{
    None = 0,
    Md5 = 32771,
    Sha1 = 32772,

    //HashAlgorithm constants which are not present in the managed HashAlgorithmType enum.
    SSL_SHA256 = 32780,
    SSL_SHA384 = 32781,
    SSL_GOST94 = 229410,
    SSL_GOST89 = 229411,
    SSL_AEAD = 229412,
};

/*
The values used in OpenSSL for SSL_CIPHER algorithm_mac.
*/
enum class SSL_DataHashAlgorithm : int64_t
{
#ifdef HAVE_SSL_CIPHER_ALGORITHMS
    SSL_MD5 = 0x00400000L,
    SSL_SHA1 = 0x00800000L,
#else
    SSL_MD5 = 1,
    SSL_SHA1 = 2,
    SSL_GOST94 = 4,
    SSL_GOST89MAC = 8,
    SSL_SHA256 = 16,
    SSL_SHA384 = 32,
    SSL_AEAD = 64
#endif
};

/*
Shims the SSLv23_method method.

Returns the requested SSL_METHOD.
*/
extern "C" const SSL_METHOD* SslV2_3Method();

/*
Shims the SSLv3_method method.

Returns the requested SSL_METHOD.
*/
extern "C" const SSL_METHOD* SslV3Method();

/*
Shims the TLSv1_method method.

Returns the requested SSL_METHOD.
*/
extern "C" const SSL_METHOD* TlsV1Method();

/*
Shims the TLSv1_1_method method.

Returns the requested SSL_METHOD.
*/
extern "C" const SSL_METHOD* TlsV1_1Method();

/*
Shims the TLSv1_2_method method.

Returns the requested SSL_METHOD.
*/
extern "C" const SSL_METHOD* TlsV1_2Method();

/*
Shims the SSL_CTX_new method.

Returns the new SSL_CTX instance.
*/
extern "C" SSL_CTX* SslCtxCreate(SSL_METHOD* method);

/*
Shims the SSL_CTX_ctrl method.

The return value of the SSL_CTX_ctrl() function depends on the command supplied via the cmd parameter.
*/
extern "C" int64_t SslCtxCtrl(SSL_CTX* ctx, int32_t cmd, int64_t larg, void* parg);

/*
Shims the SSL_new method.

Returns the new SSL instance.
*/
extern "C" SSL* SslCreate(SSL_CTX* ctx);

/*
Shims the SSL_get_error method.

Returns the error code for the specified result.
*/
extern "C" int32_t SslGetError(SSL* ssl, int32_t ret);

/*
Cleans up and deletes an SSL instance.

Implemented by calling SSL_free.

No-op if ssl is null.
The given X509 SSL is invalid after this call.
Always succeeds.
*/
extern "C" void SslDestroy(SSL* ssl);

/*
Cleans up and deletes an SSL_CTX instance.

Implemented by calling SSL_CTX_free.

No-op if ctx is null.
The given X509 SSL_CTX is invalid after this call.
Always succeeds.
*/
extern "C" void SslCtxDestroy(SSL_CTX* ctx);

/*
Shims the SSL_get_version method.

Returns the protocol version string for the SSL instance.
*/
extern "C" const char* SSLGetVersion(SSL* ssl);

/*
Returns the connection information for the SSL instance.

Returns 1 upon success, otherwise 0.
*/
extern "C" int32_t GetSslConnectionInfo(SSL* ssl, CipherAlgorithmType* dataCipherAlg, ExchangeAlgorithmType* keyExchangeAlg, HashAlgorithmType* dataHashAlg, int32_t* dataKeySize);

/*
Shims the SSL_write method.

Returns the positive number of bytes written when successful, 0 or a negative number
when an error is encountered.
*/
extern "C" int32_t SslWrite(SSL* ssl, const void* buf, int32_t num);

/*
Shims the SSL_read method.

Returns the positive number of bytes read when successful, 0 or a negative number
when an error is encountered.
*/
extern "C" int32_t SslRead(SSL* ssl, void* buf, int32_t num);
