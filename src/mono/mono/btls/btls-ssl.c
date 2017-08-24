//
//  btls-ssl.c
//  MonoBtls
//
//  Created by Martin Baulig on 14/11/15.
//  Copyright (c) 2015 Xamarin. All rights reserved.
//

#include <btls-ssl.h>
#include <btls-x509-verify-param.h>

struct MonoBtlsSsl {
	MonoBtlsSslCtx *ctx;
	SSL *ssl;
};

#define debug_print(ptr,message) \
do { if (mono_btls_ssl_ctx_is_debug_enabled(ptr->ctx)) \
mono_btls_ssl_ctx_debug_printf (ptr->ctx, "%s:%d:%s(): " message, __FILE__, __LINE__, \
__func__); } while (0)

#define debug_printf(ptr,fmt, ...) \
do { if (mono_btls_ssl_ctx_is_debug_enabled(ptr->ctx)) \
mono_btls_ssl_ctx_debug_printf (ptr->ctx, "%s:%d:%s(): " fmt, __FILE__, __LINE__, \
__func__, __VA_ARGS__); } while (0)

STACK_OF(SSL_CIPHER) *ssl_bytes_to_cipher_list (SSL *s, const CBS *cbs);

MONO_API MonoBtlsSsl *
mono_btls_ssl_new (MonoBtlsSslCtx *ctx)
{
	MonoBtlsSsl *ptr;

	ptr = calloc (1, sizeof (MonoBtlsSsl));

	ptr->ctx = mono_btls_ssl_ctx_up_ref (ctx);
	ptr->ssl = SSL_new (mono_btls_ssl_ctx_get_ctx (ptr->ctx));

	return ptr;
}

MONO_API void
mono_btls_ssl_destroy (MonoBtlsSsl *ptr)
{
	mono_btls_ssl_close (ptr);
	if (ptr->ssl) {
		SSL_free (ptr->ssl);
		ptr->ssl = NULL;
	}
	if (ptr->ctx) {
		mono_btls_ssl_ctx_free (ptr->ctx);
		ptr->ctx = NULL;
	}
	free (ptr);
}

MONO_API void
mono_btls_ssl_close (MonoBtlsSsl *ptr)
{
	;
}

MONO_API int
mono_btls_ssl_shutdown (MonoBtlsSsl *ptr)
{
    return SSL_shutdown (ptr->ssl);
}

MONO_API void
mono_btls_ssl_set_quiet_shutdown (MonoBtlsSsl *ptr, int mode)
{
    SSL_set_quiet_shutdown (ptr->ssl, mode);
}

MONO_API void
mono_btls_ssl_set_bio (MonoBtlsSsl *ptr, BIO *bio)
{
	BIO_up_ref (bio);
	SSL_set_bio (ptr->ssl, bio, bio);
}

MONO_API void
mono_btls_ssl_print_errors_cb (ERR_print_errors_callback_t callback, void *ctx)
{
	ERR_print_errors_cb (callback, ctx);
}

MONO_API int
mono_btls_ssl_use_certificate (MonoBtlsSsl *ptr, X509 *x509)
{
	return SSL_use_certificate (ptr->ssl, x509);
}

MONO_API int
mono_btls_ssl_use_private_key (MonoBtlsSsl *ptr, EVP_PKEY *key)
{
	return SSL_use_PrivateKey (ptr->ssl, key);
}

MONO_API int
mono_btls_ssl_add_chain_certificate (MonoBtlsSsl *ptr, X509 *x509)
{
	return SSL_add1_chain_cert (ptr->ssl, x509);
}

MONO_API int
mono_btls_ssl_accept (MonoBtlsSsl *ptr)
{
	return SSL_accept (ptr->ssl);
}

MONO_API int
mono_btls_ssl_connect (MonoBtlsSsl *ptr)
{
	return SSL_connect (ptr->ssl);
}

MONO_API int
mono_btls_ssl_handshake (MonoBtlsSsl *ptr)
{
	return SSL_do_handshake (ptr->ssl);
}

MONO_API int
mono_btls_ssl_read (MonoBtlsSsl *ptr, void *buf, int count)
{
	return SSL_read (ptr->ssl, buf, count);
}

MONO_API int
mono_btls_ssl_write (MonoBtlsSsl *ptr, void *buf, int count)
{
	return SSL_write (ptr->ssl, buf, count);
}

MONO_API int
mono_btls_ssl_get_version (MonoBtlsSsl *ptr)
{
	return SSL_version (ptr->ssl);
}

MONO_API void
mono_btls_ssl_set_min_version (MonoBtlsSsl *ptr, int version)
{
	SSL_set_min_version (ptr->ssl, version);
}

MONO_API void
mono_btls_ssl_set_max_version (MonoBtlsSsl *ptr, int version)
{
	SSL_set_max_version (ptr->ssl, version);
}

MONO_API int
mono_btls_ssl_get_cipher (MonoBtlsSsl *ptr)
{
	const SSL_CIPHER *cipher;

	cipher = SSL_get_current_cipher (ptr->ssl);
	if (!cipher)
		return 0;
	return (uint16_t)SSL_CIPHER_get_id (cipher);
}

MONO_API int
mono_btls_ssl_set_cipher_list (MonoBtlsSsl *ptr, const char *str)
{
	return SSL_set_cipher_list(ptr->ssl, str);
}

MONO_API int
mono_btls_ssl_get_ciphers (MonoBtlsSsl *ptr, uint16_t **data)
{
	STACK_OF(SSL_CIPHER) *ciphers;
	int count, i;

	*data = NULL;

	ciphers = SSL_get_ciphers (ptr->ssl);
	if (!ciphers)
		return 0;

	count = (int)sk_SSL_CIPHER_num (ciphers);

	*data = OPENSSL_malloc (2 * count);
	if (!*data)
		return 0;

	for (i = 0; i < count; i++) {
		const SSL_CIPHER *cipher = sk_SSL_CIPHER_value (ciphers, i);
		(*data) [i] = (uint16_t) SSL_CIPHER_get_id (cipher);
	}

	return count;
}

MONO_API X509 *
mono_btls_ssl_get_peer_certificate (MonoBtlsSsl *ptr)
{
	return SSL_get_peer_certificate (ptr->ssl);
}

MONO_API int
mono_btls_ssl_get_error (MonoBtlsSsl *ptr, int ret_code)
{
	return SSL_get_error (ptr->ssl, ret_code);
}

MONO_API int
mono_btls_ssl_set_verify_param (MonoBtlsSsl *ptr, const MonoBtlsX509VerifyParam *param)
{
	return SSL_set1_param (ptr->ssl, mono_btls_x509_verify_param_peek_param (param));
}

MONO_API int
mono_btls_ssl_set_server_name (MonoBtlsSsl *ptr, const char *name)
{
	return SSL_set_tlsext_host_name (ptr->ssl, name);
}

MONO_API const char *
mono_btls_ssl_get_server_name (MonoBtlsSsl *ptr)
{
	return SSL_get_servername (ptr->ssl, TLSEXT_NAMETYPE_host_name);
}

MONO_API void
mono_btls_ssl_set_renegotiate_mode (MonoBtlsSsl *ptr, MonoBtlsSslRenegotiateMode mode)
{
    SSL_set_renegotiate_mode (ptr->ssl, (enum ssl_renegotiate_mode_t)mode);
}

MONO_API int
mono_btls_ssl_renegotiate_pending (MonoBtlsSsl *ptr)
{
    return SSL_renegotiate_pending (ptr->ssl);
}

