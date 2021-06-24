// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal_crypto_types.h"
#include "pal_compiler.h"
#include "opensslshim.h"

/*
These values should be kept in sync with System.Security.Authentication.SslProtocols.
*/
typedef enum
{
    PAL_SSL_NONE = 0,
    PAL_SSL_SSL2 = 12,
    PAL_SSL_SSL3 = 48,
    PAL_SSL_TLS = 192,
    PAL_SSL_TLS11 = 768,
    PAL_SSL_TLS12 = 3072,
    PAL_SSL_TLS13 = 12288,
} SslProtocols;

/*
These values should be kept in sync with System.Net.Security.EncryptionPolicy.
*/
typedef enum
{
    RequireEncryption = 0,
    AllowNoEncryption,
    NoEncryption
} EncryptionPolicy;

/*
These values should be kept in sync with System.Security.Authentication.CipherAlgorithmType.
*/
typedef enum
{
    CipherAlgorithmType_None = 0,
    Null = 24576,
    Des = 26113,
    Rc2 = 26114,
    TripleDes = 26115,
    Aes128 = 26126,
    Aes192 = 26127,
    Aes256 = 26128,
    Aes = 26129,
    Rc4 = 26625,

    // Algorithm constants which are not present in the managed CipherAlgorithmType enum.
    SSL_IDEA = 229380,
    SSL_CAMELLIA128 = 229381,
    SSL_CAMELLIA256 = 229382,
    SSL_eGOST2814789CNT = 229383,
    SSL_SEED = 229384,
} CipherAlgorithmType;

/*
These values should be kept in sync with System.Security.Authentication.ExchangeAlgorithmType.
*/
typedef enum
{
    ExchangeAlgorithmType_None,
    RsaSign = 9216,
    RsaKeyX = 41984,
    DiffieHellman = 43522,

    // ExchangeAlgorithm constants which are not present in the managed ExchangeAlgorithmType enum.
    SSL_ECDH = 43525,
    SSL_ECDSA = 41475,
    SSL_ECDHE = 44550,
    SSL_kPSK = 229390,
    SSL_kGOST = 229391,
    SSL_kSRP = 229392,
    SSL_kKRB5 = 229393,
} ExchangeAlgorithmType;

/*
These values should be kept in sync with System.Security.Authentication.HashAlgorithmType.
*/
typedef enum
{
    HashAlgorithmType_None = 0,
    Md5 = 32771,
    Sha1 = 32772,

    // HashAlgorithm constants which are not present in the managed HashAlgorithmType enum.
    SSL_SHA256 = 32780,
    SSL_SHA384 = 32781,
    SSL_GOST94 = 229410,
    SSL_GOST89 = 229411,
    SSL_AEAD = 229412,
} HashAlgorithmType;

typedef enum
{
    MD5_HashKeySize = 8 * MD5_DIGEST_LENGTH,
    SHA1_HashKeySize = 8 * SHA_DIGEST_LENGTH,
    SHA256_HashKeySize = 8 * SHA256_DIGEST_LENGTH,
    SHA384_HashKeySize = 8 * SHA384_DIGEST_LENGTH,
    GOST_HashKeySize = 256,
    Default = 0,
} DataHashSize;

typedef enum
{
    PAL_SSL_ERROR_NONE = 0,
    PAL_SSL_ERROR_SSL = 1,
    PAL_SSL_ERROR_WANT_READ = 2,
    PAL_SSL_ERROR_WANT_WRITE = 3,
    PAL_SSL_ERROR_SYSCALL = 5,
    PAL_SSL_ERROR_ZERO_RETURN = 6,
} SslErrorCode;

// the function pointer definition for the callback used in SslCtxSetVerify
typedef int32_t (*SslCtxSetVerifyCallback)(int32_t, X509_STORE_CTX*);

// the function pointer definition for the callback used in SslCtxSetAlpnSelectCb
typedef int32_t (*SslCtxSetAlpnCallback)(SSL* ssl,
    const uint8_t** out,
    uint8_t* outlen,
    const uint8_t* in,
    uint32_t inlen,
    void* arg);
/*
Ensures that libssl is correctly initialized and ready to use.
*/
PALEXPORT void CryptoNative_EnsureLibSslInitialized(void);

/*
Shims the SSLv23_method method.

Returns the requested SSL_METHOD.
*/
PALEXPORT const SSL_METHOD* CryptoNative_SslV2_3Method(void);

/*
Shims the SSL_CTX_new method.

Returns the new SSL_CTX instance.
*/
PALEXPORT SSL_CTX* CryptoNative_SslCtxCreate(const SSL_METHOD* method);

/*
Sets the specified protocols in the SSL_CTX options.
*/
PALEXPORT void CryptoNative_SetProtocolOptions(SSL_CTX* ctx, SslProtocols protocols);

/*
Shims the SSL_new method.

Returns the new SSL instance.
*/
PALEXPORT SSL* CryptoNative_SslCreate(SSL_CTX* ctx);

/*
Shims the SSL_get_error method.

Returns the error code for the specified result.
*/
PALEXPORT int32_t CryptoNative_SslGetError(SSL* ssl, int32_t ret);

/*
Cleans up and deletes an SSL instance.

Implemented by calling SSL_free.

No-op if ssl is null.
The given X509 SSL is invalid after this call.
Always succeeds.
*/
PALEXPORT void CryptoNative_SslDestroy(SSL* ssl);

/*
Cleans up and deletes an SSL_CTX instance.

Implemented by calling SSL_CTX_free.

No-op if ctx is null.
The given X509 SSL_CTX is invalid after this call.
Always succeeds.
*/
PALEXPORT void CryptoNative_SslCtxDestroy(SSL_CTX* ctx);

/*
Shims the SSL_set_connect_state method.
*/
PALEXPORT void CryptoNative_SslSetConnectState(SSL* ssl);

/*
Shims the SSL_set_accept_state method.
*/
PALEXPORT void CryptoNative_SslSetAcceptState(SSL* ssl);

/*
Shims the SSL_get_version method.

Returns the protocol version string for the SSL instance.
*/
PALEXPORT const char* CryptoNative_SslGetVersion(SSL* ssl);

/*
Shims the SSL_write method.

Returns the positive number of bytes written when successful, 0 or a negative number
when an error is encountered.
*/
PALEXPORT int32_t CryptoNative_SslWrite(SSL* ssl, const void* buf, int32_t num);

/*
Shims the SSL_read method.

Returns the positive number of bytes read when successful, 0 or a negative number
when an error is encountered.
*/
PALEXPORT int32_t CryptoNative_SslRead(SSL* ssl, void* buf, int32_t num);

/*
Shims the SSL_renegotiate method.

Returns 1 when renegotiation started; 0 on error.
*/
PALEXPORT int32_t CryptoNative_SslRenegotiate(SSL* ssl);

/*
Shims the SSL_renegotiate_pending method.

Returns 1 when negotiation is requested; 0 once a handshake has finished.
*/
PALEXPORT int32_t CryptoNative_IsSslRenegotiatePending(SSL* ssl);

/*
Shims the SSL_shutdown method.

Returns:
1 if the shutdown was successfully completed;
0 if the shutdown is not yet finished;
<0 if the shutdown was not successful because a fatal error.
*/
PALEXPORT int32_t CryptoNative_SslShutdown(SSL* ssl);

/*
Shims the SSL_set_bio method.
*/
PALEXPORT void CryptoNative_SslSetBio(SSL* ssl, BIO* rbio, BIO* wbio);

/*
Shims the SSL_do_handshake method.

Returns:
1 if the handshake was successful;
0 if the handshake was not successful but was shut down controlled
and by the specifications of the TLS/SSL protocol;
<0 if the handshake was not successful because of a fatal error.
*/
PALEXPORT int32_t CryptoNative_SslDoHandshake(SSL* ssl);

/*
Gets a value indicating whether the SSL_state is SSL_ST_OK.

Returns 1 if the state is OK, otherwise 0.
*/
PALEXPORT int32_t CryptoNative_IsSslStateOK(SSL* ssl);

/*
Shims the SSL_get_peer_certificate method.

Returns the certificate presented by the peer.
*/
PALEXPORT X509* CryptoNative_SslGetPeerCertificate(SSL* ssl);

/*
Shims the SSL_get_peer_cert_chain method.

Returns the certificate chain presented by the peer.
*/
PALEXPORT X509Stack* CryptoNative_SslGetPeerCertChain(SSL* ssl);

/*
Shims the SSL_CTX_use_certificate method.

Returns 1 upon success, otherwise 0.
*/
PALEXPORT int32_t CryptoNative_SslCtxUseCertificate(SSL_CTX* ctx, X509* x);

/*
Shims the SSL_CTX_use_PrivateKey method.

Returns 1 upon success, otherwise 0.
*/
PALEXPORT int32_t CryptoNative_SslCtxUsePrivateKey(SSL_CTX* ctx, EVP_PKEY* pkey);

/*
Shims the SSL_CTX_check_private_key method.

Returns 1 upon success, otherwise 0.
*/
PALEXPORT int32_t CryptoNative_SslCtxCheckPrivateKey(SSL_CTX* ctx);

/*
Shims the SSL_CTX_set_quiet_shutdown method.
*/
PALEXPORT void CryptoNative_SslCtxSetQuietShutdown(SSL_CTX* ctx);

/*
Shims the SSL_set_quiet_shutdown method.
*/
PALEXPORT void CryptoNative_SslSetQuietShutdown(SSL* ctx, int mode);

/*
Shims the SSL_get_client_CA_list method.

Returns the list of CA names explicity set.
*/
PALEXPORT X509NameStack* CryptoNative_SslGetClientCAList(SSL* ssl);

/*
Shims the SSL_CTX_set_verify method.
*/
PALEXPORT void CryptoNative_SslCtxSetVerify(SSL_CTX* ctx, SslCtxSetVerifyCallback callback);

/*
Sets the specified encryption policy on the SSL_CTX.
*/
PALEXPORT int32_t CryptoNative_SetEncryptionPolicy(SSL_CTX* ctx, EncryptionPolicy policy);

/*
Sets ciphers (< TLS 1.3) and cipher suites (TLS 1.3) on the SSL_CTX
*/
PALEXPORT int32_t CryptoNative_SetCiphers(SSL_CTX* ctx, const char* cipherList, const char* cipherSuites);

/*
Determines if TLS 1.3 is supported by this OpenSSL implementation
*/
PALEXPORT int32_t CryptoNative_Tls13Supported(void);

/*
Shims the SSL_get_finished method.
*/
PALEXPORT int32_t CryptoNative_SslGetFinished(SSL* ssl, void* buf, int32_t count);

/*
Shims the SSL_get_peer_finished method.
*/
PALEXPORT int32_t CryptoNative_SslGetPeerFinished(SSL* ssl, void* buf, int32_t count);

/*
Returns true/false based on if existing ssl session was re-used or not.
Shims the SSL_session_reused macro.
*/
PALEXPORT int32_t CryptoNative_SslSessionReused(SSL* ssl);

/*
adds the given certificate to the extra chain certificates associated with ctx that is associated with the ssl.

libssl frees the x509 object.
Returns 1 if success and 0 in case of failure
*/
PALEXPORT int32_t CryptoNative_SslAddExtraChainCert(SSL* ssl, X509* x509);

/*
Shims the ssl_ctx_set_alpn_select_cb method.
*/
PALEXPORT void CryptoNative_SslCtxSetAlpnSelectCb(SSL_CTX* ctx, SslCtxSetAlpnCallback cb, void *arg);

/*
Shims the ssl_ctx_set_alpn_protos method.
Returns 0 on success, non-zero on failure.
*/
PALEXPORT int32_t CryptoNative_SslCtxSetAlpnProtos(SSL_CTX* ctx, const uint8_t* protos, uint32_t protos_len);

/*
Shims the ssl_get0_alpn_selected method.
*/
PALEXPORT void CryptoNative_SslGet0AlpnSelected(SSL* ssl, const uint8_t** protocol, uint32_t* len);

/*
Shims the SSL_set_tlsext_host_name method.
*/
PALEXPORT int32_t CryptoNative_SslSetTlsExtHostName(SSL* ssl, uint8_t* name);

/*
Shims the SSL_get_current_cipher and SSL_CIPHER_get_id.
*/
PALEXPORT int32_t CryptoNative_SslGetCurrentCipherId(SSL* ssl, int32_t* cipherId);

/*
Looks up a cipher by the IANA identifier, returns a shared string for the OpenSSL name for the cipher,
and emits a value indicating if the cipher belongs to the SSL2-TLS1.2 list, or the TLS1.3+ list.
*/
PALEXPORT const char* CryptoNative_GetOpenSslCipherSuiteName(SSL* ssl, int32_t cipherSuite, int32_t* isTls12OrLower);

/*
Checks if given protocol version is supported.
*/
PALEXPORT int32_t CryptoNative_OpenSslGetProtocolSupport(SslProtocols protocol);
