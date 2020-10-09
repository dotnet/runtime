#include "library.h"

#include <openssl/ssl.h>
#include <openssl/pkcs12.h>

QUIC_NATIVE_API const void* QuicNative_TLS_method()
{
    return TLS_method();
}

QUIC_NATIVE_API int QuicNative_CRYPTO_get_ex_new_index(int classIndex, long argl, void* argp, void* new_func, void* dup_func, void* free_func)
{
    return CRYPTO_get_ex_new_index(classIndex, argl, argp, new_func, dup_func, free_func);
}

QUIC_NATIVE_API void* QuicNative_SSL_CTX_new(const void* method)
{
    return SSL_CTX_new(method);
}

QUIC_NATIVE_API void QuicNative_SSL_CTX_free(void* ctx)
{
    SSL_CTX_free(ctx);
}

QUIC_NATIVE_API void* QuicNative_SSL_new(void* ctx)
{
    return SSL_new(ctx);
}

QUIC_NATIVE_API void QuicNative_SSL_free(void* ssl)
{
    SSL_free(ssl);
}

QUIC_NATIVE_API int QuicNative_SSL_use_certificate_file(void* ssl, const char* file, int fileType)
{
    return SSL_use_certificate_file(ssl, file, fileType);
}

QUIC_NATIVE_API int QuicNative_SSL_use_PrivateKey_file(void* ssl, const char* file, int fileType)
{
    return SSL_use_PrivateKey_file(ssl, file, fileType);
}

QUIC_NATIVE_API int QuicNative_SSL_use_cert_and_key(void* ssl, void* x509, void* privateKey, void* caChain, int override)
{
    return SSL_use_cert_and_key(ssl, x509, privateKey, caChain, override);
}

QUIC_NATIVE_API int QuicNative_SSL_use_certificate(void* ssl, void* x509)
{
    return SSL_use_certificate(ssl, x509);
}

QUIC_NATIVE_API int QuicNative_SSL_set_quic_method(void* ssl, const void* method)
{
    return SSL_set_quic_method(ssl, method);
}

QUIC_NATIVE_API void QuicNative_SSL_set_accept_state(void* ssl)
{
    SSL_set_accept_state(ssl);
}

QUIC_NATIVE_API void QuicNative_SSL_set_connect_state(void* ssl)
{
    SSL_set_connect_state(ssl);
}

QUIC_NATIVE_API int QuicNative_SSL_do_handshake(void* ssl)
{
    return SSL_do_handshake(ssl);
}

QUIC_NATIVE_API int QuicNative_SSL_ctrl(void* ssl, int cmd, long larg, void* parg)
{
    return SSL_ctrl(ssl, cmd, larg, parg);
}

QUIC_NATIVE_API int QuicNative_SSL_callback_ctrl(void* ssl, int cmd, void* fp)
{
    return SSL_callback_ctrl(ssl, cmd, fp);
}

QUIC_NATIVE_API int QuicNative_SSL_get_error(const void* ssl, int code)
{
    return SSL_get_error(ssl, code);
}

QUIC_NATIVE_API int QuicNative_SSL_provide_quic_data(void* ssl, int level, const uint8_t* data, size_t len)
{
    return SSL_provide_quic_data(ssl, level, data, len);
}

QUIC_NATIVE_API int QuicNative_SSL_set_ex_data(void* ssl, int idx, void* data)
{
    return SSL_set_ex_data(ssl, idx, data);
}

QUIC_NATIVE_API void* QuicNative_SSL_get_ex_data(const void* ssl, int idx)
{
    return SSL_get_ex_data(ssl, idx);
}

QUIC_NATIVE_API int QuicNative_SSL_set_quic_transport_params(void* ssl, const uint8_t* param, size_t len)
{
    return SSL_set_quic_transport_params(ssl, param, len);
}

QUIC_NATIVE_API void QuicNative_SSL_get_peer_quic_transport_params(const void* ssl, const uint8_t** param, size_t *len)
{
    SSL_get_peer_quic_transport_params(ssl, param, len);
}

QUIC_NATIVE_API int QuicNative_SSL_quic_write_level(const void* ssl)
{
    return SSL_quic_write_level(ssl);
}

QUIC_NATIVE_API int QuicNative_SSL_is_init_finished(const void* ssl)
{
    return SSL_is_init_finished(ssl);
}

QUIC_NATIVE_API const void* QuicNative_SSL_get_current_cipher(const void* ssl)
{
    return SSL_get_current_cipher(ssl);
}

QUIC_NATIVE_API int16_t QuicNative_SSL_CIPHER_get_protocol_id(const void* cipher)
{
    return SSL_CIPHER_get_protocol_id(cipher);
}

QUIC_NATIVE_API int QuicNative_SSL_set_ciphersuites(void* ssl, const char* list)
{
    return SSL_set_ciphersuites(ssl, list);
}

QUIC_NATIVE_API int QuicNative_SSL_set_cipher_list(void* ssl, const char* list)
{
    return SSL_set_cipher_list(ssl, list);
}

QUIC_NATIVE_API const void* QuicNative_SSL_get_cipher_list(const void* ssl, int priority)
{
    return SSL_get_cipher_list(ssl, priority);
}

QUIC_NATIVE_API int QuicNative_SSL_set_alpn_protos(void* ssl, const unsigned char* protos, int len)
{
    return SSL_set_alpn_protos(ssl, protos, len);
}

QUIC_NATIVE_API void QuicNative_SSL_CTX_set_alpn_select_cb(void* ctx, void* cb, void* arg)
{
    SSL_CTX_set_alpn_select_cb(ctx, cb, arg);
}

QUIC_NATIVE_API void QuicNative_SSL_get0_alpn_selected(const void* ssl, const unsigned char** data, unsigned int* len)
{
    SSL_get0_alpn_selected(ssl, data, len);
}

QUIC_NATIVE_API void* QuicNative_BIO_new_mem_buf(const uint8_t* buf, int len)
{
    return BIO_new_mem_buf(buf, len);
}

QUIC_NATIVE_API void QuicNative_BIO_free(void* bio)
{
    BIO_free(bio);
}

QUIC_NATIVE_API void* QuicNative_d2i_PKCS12(void* out, const uint8_t** buf, int len)
{
    return d2i_PKCS12(out, buf, len);
}

QUIC_NATIVE_API int QuicNative_PKCS12_parse(void* pkcs, const char* pass, EVP_PKEY ** outKey, X509** outCert, STACK_OF(X509)** outCa)
{
    return PKCS12_parse(pkcs, pass, outKey, outCert, outCa);
}

QUIC_NATIVE_API void QuicNative_PKCS12_free(void* pkcs)
{
    PKCS12_free(pkcs);
}

QUIC_NATIVE_API void QuicNative_X509_free(void* x509)
{
    X509_free(x509);
}

QUIC_NATIVE_API void QuicNative_EVP_PKEY_free(void* evp)
{
    EVP_PKEY_free(evp);
}

QUIC_NATIVE_API const char* QuicNative_SSL_get_servername(const void* ssl, int type)
{
    return SSL_get_servername(ssl, type);
}

