//
//  btls-ssl-ctx.c
//  MonoBtls
//
//  Created by Martin Baulig on 4/11/16.
//  Copyright © 2016 Xamarin. All rights reserved.
//

#include <btls-ssl-ctx.h>
#include <btls-x509-verify-param.h>

struct MonoBtlsSslCtx {
	CRYPTO_refcount_t references;
	SSL_CTX *ctx;
	BIO *bio;
	BIO *debug_bio;
	void *instance;
	MonoBtlsVerifyFunc verify_func;
	MonoBtlsSelectFunc select_func;
};

#define debug_print(ptr,message) \
do { if (mono_btls_ssl_ctx_is_debug_enabled(ptr)) \
mono_btls_ssl_ctx_debug_printf (ptr, "%s:%d:%s(): " message, __FILE__, __LINE__, \
	__func__); } while (0)

#define debug_printf(ptr,fmt, ...) \
do { if (mono_btls_ssl_ctx_is_debug_enabled(ptr)) \
mono_btls_ssl_ctx_debug_printf (ptr, "%s:%d:%s(): " fmt, __FILE__, __LINE__, \
	__func__, __VA_ARGS__); } while (0)

void ssl_cipher_preference_list_free (struct ssl_cipher_preference_list_st *cipher_list);

MONO_API int
mono_btls_ssl_ctx_is_debug_enabled (MonoBtlsSslCtx *ctx)
{
	return ctx->debug_bio != NULL;
}

MONO_API int
mono_btls_ssl_ctx_debug_printf (MonoBtlsSslCtx *ctx, const char *format, ...)
{
	va_list args;
	int ret;

	if (!ctx->debug_bio)
		return 0;

	va_start (args, format);
	ret = mono_btls_debug_printf (ctx->debug_bio, format, args);
	va_end (args);
	return ret;
}

MONO_API MonoBtlsSslCtx *
mono_btls_ssl_ctx_new (void)
{
	MonoBtlsSslCtx *ctx;

	ctx = OPENSSL_malloc (sizeof (MonoBtlsSslCtx));
	if (!ctx)
		return NULL;

	memset (ctx, 0, sizeof (MonoBtlsSslCtx));
	ctx->references = 1;
	ctx->ctx = SSL_CTX_new (TLS_method ());

	// enable the default ciphers but disable any RC4 based ciphers
	// since they're insecure: RFC 7465 "Prohibiting RC4 Cipher Suites"
	SSL_CTX_set_cipher_list (ctx->ctx, "DEFAULT:!RC4");

	// disable SSLv2 and SSLv3 by default, they are deprecated
	// and should generally not be used according to the openssl docs
	SSL_CTX_set_options (ctx->ctx, SSL_OP_NO_SSLv2 | SSL_OP_NO_SSLv3);

	return ctx;
}

MONO_API MonoBtlsSslCtx *
mono_btls_ssl_ctx_up_ref (MonoBtlsSslCtx *ctx)
{
	CRYPTO_refcount_inc (&ctx->references);
	return ctx;
}

MONO_API int
mono_btls_ssl_ctx_free (MonoBtlsSslCtx *ctx)
{
	if (!CRYPTO_refcount_dec_and_test_zero (&ctx->references))
		return 0;
	SSL_CTX_free (ctx->ctx);
	ctx->instance = NULL;
	OPENSSL_free (ctx);
	return 1;
}

MONO_API SSL_CTX *
mono_btls_ssl_ctx_get_ctx (MonoBtlsSslCtx *ctx)
{
	return ctx->ctx;
}

MONO_API void
mono_btls_ssl_ctx_set_debug_bio (MonoBtlsSslCtx *ctx, BIO *debug_bio)
{
	if (debug_bio)
		ctx->debug_bio = BIO_up_ref(debug_bio);
	else
		ctx->debug_bio = NULL;
}

MONO_API void
mono_btls_ssl_ctx_initialize (MonoBtlsSslCtx *ctx, void *instance)
{
	ctx->instance = instance;
}

static int
cert_verify_callback (X509_STORE_CTX *storeCtx, void *arg)
{
	MonoBtlsSslCtx *ptr = (MonoBtlsSslCtx*)arg;
	int ret;

	debug_printf (ptr, "cert_verify_callback(): %p\n", ptr->verify_func);
	ret = X509_verify_cert (storeCtx);
	debug_printf (ptr, "cert_verify_callback() #1: %d\n", ret);

	if (ptr->verify_func)
		ret = ptr->verify_func (ptr->instance, ret, storeCtx);

	return ret;
}

MONO_API void
mono_btls_ssl_ctx_set_cert_verify_callback (MonoBtlsSslCtx *ptr, MonoBtlsVerifyFunc func, int cert_required)
{
	int mode;

	ptr->verify_func = func;
	SSL_CTX_set_cert_verify_callback (ptr->ctx, cert_verify_callback, ptr);

	mode = SSL_VERIFY_PEER;
	if (cert_required)
		mode |= SSL_VERIFY_FAIL_IF_NO_PEER_CERT;

	SSL_CTX_set_verify (ptr->ctx, mode, NULL);
}

static int
cert_select_callback (SSL *ssl, void *arg)
{
	MonoBtlsSslCtx *ptr = (MonoBtlsSslCtx*)arg;
	int ret = 1;

	debug_printf (ptr, "cert_select_callback(): %p\n", ptr->select_func);
	if (ptr->select_func)
		ret = ptr->select_func (ptr->instance);
	debug_printf (ptr, "cert_select_callback() #1: %d\n", ret);

	return ret;
}

MONO_API void
mono_btls_ssl_ctx_set_cert_select_callback (MonoBtlsSslCtx *ptr, MonoBtlsSelectFunc func)
{
	ptr->select_func = func;
	SSL_CTX_set_cert_cb (ptr->ctx, cert_select_callback, ptr);
}

MONO_API X509_STORE *
mono_btls_ssl_ctx_peek_store (MonoBtlsSslCtx *ctx)
{
	return SSL_CTX_get_cert_store (ctx->ctx);
}

MONO_API void
mono_btls_ssl_ctx_set_min_version (MonoBtlsSslCtx *ctx, int version)
{
	SSL_CTX_set_min_version (ctx->ctx, version);
}

MONO_API void
mono_btls_ssl_ctx_set_max_version (MonoBtlsSslCtx *ctx, int version)
{
	SSL_CTX_set_max_version (ctx->ctx, version);
}

MONO_API int
mono_btls_ssl_ctx_is_cipher_supported (MonoBtlsSslCtx *ctx, uint16_t value)
{
	const SSL_CIPHER *cipher;

	cipher = SSL_get_cipher_by_value (value);
	return cipher != NULL;
}

MONO_API int
mono_btls_ssl_ctx_set_ciphers (MonoBtlsSslCtx *ctx, int count, const uint16_t *data,
				   int allow_unsupported)
{
	STACK_OF(SSL_CIPHER) *ciphers = NULL;
	struct ssl_cipher_preference_list_st *pref_list = NULL;
	uint8_t *in_group_flags = NULL;
	int i;

	ciphers = sk_SSL_CIPHER_new_null ();
	if (!ciphers)
		goto err;

	for (i = 0; i < count; i++) {
		const SSL_CIPHER *cipher = SSL_get_cipher_by_value (data [i]);
		if (!cipher) {
			debug_printf (ctx, "mono_btls_ssl_ctx_set_ciphers(): unknown cipher %02x", data [i]);
			if (!allow_unsupported)
				goto err;
			continue;
		}
		if (!sk_SSL_CIPHER_push (ciphers, cipher))
			 goto err;
	}

	pref_list = OPENSSL_malloc (sizeof (struct ssl_cipher_preference_list_st));
	if (!pref_list)
		goto err;

	memset (pref_list, 0, sizeof (struct ssl_cipher_preference_list_st));
	pref_list->ciphers = sk_SSL_CIPHER_dup (ciphers);
	if (!pref_list->ciphers)
		goto err;
	pref_list->in_group_flags = OPENSSL_malloc (sk_SSL_CIPHER_num (ciphers));
	if (!pref_list->in_group_flags)
		goto err;

	if (ctx->ctx->cipher_list)
		ssl_cipher_preference_list_free (ctx->ctx->cipher_list);
	if (ctx->ctx->cipher_list_by_id)
		sk_SSL_CIPHER_free (ctx->ctx->cipher_list_by_id);
	if (ctx->ctx->cipher_list_tls10) {
		ssl_cipher_preference_list_free (ctx->ctx->cipher_list_tls10);
		ctx->ctx->cipher_list_tls10 = NULL;
	}
	if (ctx->ctx->cipher_list_tls11) {
		ssl_cipher_preference_list_free (ctx->ctx->cipher_list_tls11);
		ctx->ctx->cipher_list_tls11 = NULL;
	}

	ctx->ctx->cipher_list = pref_list;
	ctx->ctx->cipher_list_by_id = ciphers;

	return (int)sk_SSL_CIPHER_num (ciphers);

err:
	sk_SSL_CIPHER_free (ciphers);
	OPENSSL_free (pref_list);
	OPENSSL_free (in_group_flags);
	return 0;
}

MONO_API int
mono_btls_ssl_ctx_set_verify_param (MonoBtlsSslCtx *ctx, const MonoBtlsX509VerifyParam *param)
{
	return SSL_CTX_set1_param (ctx->ctx, mono_btls_x509_verify_param_peek_param (param));
}

