/*
 * Native part of the System.Net.Quic library
 */

#ifndef SYSTEM_NET_QUIC_NATIVE_LIBRARY_H
#define SYSTEM_NET_QUIC_NATIVE_LIBRARY_H

#include <stdint.h>
#include <stddef.h>
#include <openssl/ssl.h>

#ifdef _MSC_VER
#define QUIC_NATIVE_API __declspec(dllexport)
#else
// linux does not require explicit dllexport annotations
#define QUIC_NATIVE_API
#endif

QUIC_NATIVE_API const void* QuicNative_TLS_method();
QUIC_NATIVE_API int QuicNative_CRYPTO_get_ex_new_index(int classIndex, long argl, void* argp, void* new_func, void* dup_func, void* free_func);
QUIC_NATIVE_API void* QuicNative_SSL_CTX_new(const void* method);
QUIC_NATIVE_API void QuicNative_SSL_CTX_free(void* ctx);
QUIC_NATIVE_API void* QuicNative_SSL_new(void* ctx);
QUIC_NATIVE_API void QuicNative_SSL_free(void* ssl);
QUIC_NATIVE_API int QuicNative_SSL_use_certificate_file(void* ssl, const char* file, int fileType);
QUIC_NATIVE_API int QuicNative_SSL_use_PrivateKey_file(void* ssl, const char* file, int fileType);
QUIC_NATIVE_API int QuicNative_SSL_use_cert_and_key(void* ssl, void* x509, void* privateKey, void* caChain, int override);
QUIC_NATIVE_API int QuicNative_SSL_use_certificate(void* ssl, void* x509);
QUIC_NATIVE_API int QuicNative_SSL_set_quic_method(void* ssl, const void* method);
QUIC_NATIVE_API void QuicNative_SSL_set_accept_state(void* ssl);
QUIC_NATIVE_API void QuicNative_SSL_set_connect_state(void* ssl);
QUIC_NATIVE_API int QuicNative_SSL_do_handshake(void* ssl);
QUIC_NATIVE_API int QuicNative_SSL_ctrl(void* ssl, int cmd, long larg, void* parg);
QUIC_NATIVE_API int QuicNative_SSL_callback_ctrl(void* ssl, int cmd, void* fp);
QUIC_NATIVE_API int QuicNative_SSL_get_error(const void* ssl, int code);
QUIC_NATIVE_API int QuicNative_SSL_provide_quic_data(void* ssl, int level, const uint8_t* data, size_t len);
QUIC_NATIVE_API int QuicNative_SSL_set_ex_data(void* ssl, int idx, void* data);
QUIC_NATIVE_API void* QuicNative_SSL_get_ex_data(const void* ssl, int idx);
QUIC_NATIVE_API int QuicNative_SSL_set_quic_transport_params(void* ssl, const uint8_t* param, size_t len);
QUIC_NATIVE_API void QuicNative_SSL_get_peer_quic_transport_params(const void* ssl, const uint8_t** param, size_t *len);
QUIC_NATIVE_API int QuicNative_SSL_quic_write_level(const void* ssl);
QUIC_NATIVE_API int QuicNative_SSL_is_init_finished(const void* ssl);
QUIC_NATIVE_API const void* QuicNative_SSL_get_current_cipher(const void* ssl);
QUIC_NATIVE_API int16_t QuicNative_SSL_CIPHER_get_protocol_id(const void* cipher);
QUIC_NATIVE_API int QuicNative_SSL_set_ciphersuites(void* ssl, const char* list);
QUIC_NATIVE_API int QuicNative_SSL_set_cipher_list(void* ssl, const char* list);
QUIC_NATIVE_API const void* QuicNative_SSL_get_cipher_list(const void* ssl, int priority);
QUIC_NATIVE_API int QuicNative_SSL_set_alpn_protos(void* ssl, const unsigned char* protos, int len);
QUIC_NATIVE_API void QuicNative_SSL_get0_alpn_selected(const void* ssl, const unsigned char** data, unsigned int* len);
QUIC_NATIVE_API void* QuicNative_BIO_new_mem_buf(const uint8_t* buf, int len);
QUIC_NATIVE_API void QuicNative_BIO_free(void* bio);
QUIC_NATIVE_API void* QuicNative_d2i_PKCS12(void* out, const uint8_t** buf, int len);
QUIC_NATIVE_API int QuicNative_PKCS12_parse(void* pkcs, const char* pass, EVP_PKEY ** outKey, X509** outCert, STACK_OF(X509)** outCa);
QUIC_NATIVE_API void QuicNative_PKCS12_free(void* pkcs);
QUIC_NATIVE_API void QuicNative_X509_free(void* x509);
QUIC_NATIVE_API void QuicNative_EVP_PKEY_free(void* evp);
QUIC_NATIVE_API const char* QuicNative_SSL_get_servername(const void* ssl, int type);

#endif //SYSTEM_NET_QUIC_NATIVE_LIBRARY_H
