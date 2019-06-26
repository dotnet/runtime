//
//  btls-ssl-ctx.c
//  MonoBtls
//
//  Created by Martin Baulig on 4/11/16.
//  Copyright © 2016 Xamarin. All rights reserved.
//

#include "btls-ssl-ctx.h"
#include "btls-x509-verify-param.h"
#include <openssl/bytestring.h>
#include <string.h>

struct MonoBtlsSslCtx {
	CRYPTO_refcount_t references;
	SSL_CTX *ctx;
	BIO *bio;
	BIO *debug_bio;
	void *instance;
	MonoBtlsVerifyFunc verify_func;
	MonoBtlsSelectFunc select_func;
	MonoBtlsServerNameFunc server_name_func;
};

#define debug_print(ptr,message) \
do { if (mono_btls_ssl_ctx_is_debug_enabled(ptr)) \
mono_btls_ssl_ctx_debug_printf (ptr, "%s:%d:%s(): " message, __FILE__, __LINE__, \
	__func__); } while (0)

#define debug_printf(ptr,fmt, ...) \
do { if (mono_btls_ssl_ctx_is_debug_enabled(ptr)) \
mono_btls_ssl_ctx_debug_printf (ptr, "%s:%d:%s(): " fmt, __FILE__, __LINE__, \
	__func__, __VA_ARGS__); } while (0)

int
mono_btls_ssl_ctx_is_debug_enabled (MonoBtlsSslCtx *ctx)
{
	return ctx->debug_bio != NULL;
}

int
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

MonoBtlsSslCtx *
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

MonoBtlsSslCtx *
mono_btls_ssl_ctx_up_ref (MonoBtlsSslCtx *ctx)
{
	CRYPTO_refcount_inc (&ctx->references);
	return ctx;
}

int
mono_btls_ssl_ctx_free (MonoBtlsSslCtx *ctx)
{
	if (!CRYPTO_refcount_dec_and_test_zero (&ctx->references))
		return 0;
	SSL_CTX_free (ctx->ctx);
	ctx->instance = NULL;
	OPENSSL_free (ctx);
	return 1;
}

SSL_CTX *
mono_btls_ssl_ctx_get_ctx (MonoBtlsSslCtx *ctx)
{
	return ctx->ctx;
}

void
mono_btls_ssl_ctx_set_debug_bio (MonoBtlsSslCtx *ctx, BIO *debug_bio)
{
	if (debug_bio)
		ctx->debug_bio = BIO_up_ref(debug_bio);
	else
		ctx->debug_bio = NULL;
}

void
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

void
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
	STACK_OF(X509_NAME) *ca_list;
	int *sizes = NULL;
	void **cadata = NULL;
	int count = 0;
	int ret = 1;
	int i;

	debug_printf (ptr, "cert_select_callback(): %p\n", ptr->select_func);

	// SSL_get_client_CA_list() may only be called during this callback.
	ca_list = SSL_get_client_CA_list (ssl);
	if (ca_list) {
		count = (int)sk_X509_NAME_num (ca_list);
		cadata = OPENSSL_malloc (sizeof (void *) * (count + 1));
		sizes = OPENSSL_malloc (sizeof (int) * (count + 1));
		if (!cadata || !sizes) {
			ret = 0;
			goto out;
		}
		for (i = 0; i < count; i++) {
			X509_NAME *name = sk_X509_NAME_value (ca_list, i);
			cadata[i] = name->bytes->data;
			sizes[i] = (int)name->bytes->length;
		}
	}

	debug_printf (ptr, "cert_select_callback() #1: %p\n", ca_list);

	if (ptr->select_func)
		ret = ptr->select_func (ptr->instance, count, sizes, cadata);
	debug_printf (ptr, "cert_select_callback() #1: %d\n", ret);

out:
	if (cadata)
		OPENSSL_free (cadata);
	if (sizes)
		OPENSSL_free (sizes);

	return ret;
}

void
mono_btls_ssl_ctx_set_cert_select_callback (MonoBtlsSslCtx *ptr, MonoBtlsSelectFunc func)
{
	ptr->select_func = func;
	SSL_CTX_set_cert_cb (ptr->ctx, cert_select_callback, ptr);
}

X509_STORE *
mono_btls_ssl_ctx_peek_store (MonoBtlsSslCtx *ctx)
{
	return SSL_CTX_get_cert_store (ctx->ctx);
}

void
mono_btls_ssl_ctx_set_min_version (MonoBtlsSslCtx *ctx, int version)
{
	SSL_CTX_set_min_version (ctx->ctx, version);
}

void
mono_btls_ssl_ctx_set_max_version (MonoBtlsSslCtx *ctx, int version)
{
	SSL_CTX_set_max_version (ctx->ctx, version);
}

int
mono_btls_ssl_ctx_is_cipher_supported (MonoBtlsSslCtx *ctx, uint16_t value)
{
	const SSL_CIPHER *cipher;

	cipher = SSL_get_cipher_by_value (value);
	return cipher != NULL;
}

int
mono_btls_ssl_ctx_set_ciphers (MonoBtlsSslCtx *ctx, int count, const uint16_t *data,
				   int allow_unsupported)
{
	CBB cbb;
	int i, ret = 0;

	if (!CBB_init (&cbb, 64))
		goto err;

	/* Assemble a cipher string with the specified ciphers' names. */
	for (i = 0; i < count; i++) {
		const char *name;
		const SSL_CIPHER *cipher = SSL_get_cipher_by_value (data [i]);
		if (!cipher) {
			debug_printf (ctx, "mono_btls_ssl_ctx_set_ciphers(): unknown cipher %02x", data [i]);
			if (!allow_unsupported)
				goto err;
			continue;
		}
		name = SSL_CIPHER_get_name (cipher);
		if (i > 0 && !CBB_add_u8 (&cbb, ':'))
			goto err;
		if (!CBB_add_bytes (&cbb, (const uint8_t *)name, strlen(name)))
			goto err;
	}

	/* NUL-terminate the string. */
	if (!CBB_add_u8 (&cbb, 0))
		goto err;

	ret = SSL_CTX_set_cipher_list (ctx->ctx, (const char *)CBB_data (&cbb));

err:
	CBB_cleanup (&cbb);
	return ret;
}

int
mono_btls_ssl_ctx_set_verify_param (MonoBtlsSslCtx *ctx, const MonoBtlsX509VerifyParam *param)
{
	return SSL_CTX_set1_param (ctx->ctx, mono_btls_x509_verify_param_peek_param (param));
}

int
mono_btls_ssl_ctx_set_client_ca_list (MonoBtlsSslCtx *ctx, int count, int *sizes, const void **data)
{
	STACK_OF(X509_NAME) *name_list;
	int i;

	name_list = sk_X509_NAME_new_null ();
	if (!name_list)
		return 0;

	for (i = 0; i < count; i++) {
		X509_NAME *name;
		const unsigned char *ptr = (const unsigned char*)data[i];

		name = d2i_X509_NAME (NULL, &ptr, sizes[i]);
		if (!name) {
			sk_X509_NAME_pop_free (name_list, X509_NAME_free);
			return 0;
		}
		sk_X509_NAME_push (name_list, name);
	}

	// Takes ownership of the list.
	SSL_CTX_set_client_CA_list (ctx->ctx, name_list);
	return 1;
}

static int
server_name_callback (SSL *ssl, int *out_alert, void *arg)
{
	MonoBtlsSslCtx *ctx = (MonoBtlsSslCtx *)arg;

	if (ctx->server_name_func (ctx->instance) == 1)
		return SSL_TLSEXT_ERR_OK;

	*out_alert = SSL_AD_USER_CANCELLED;
	return SSL_TLSEXT_ERR_ALERT_FATAL;
}

void
mono_btls_ssl_ctx_set_server_name_callback (MonoBtlsSslCtx *ptr, MonoBtlsServerNameFunc func)
{
	ptr->server_name_func = func;

	SSL_CTX_set_tlsext_servername_callback (ptr->ctx, server_name_callback);
	SSL_CTX_set_tlsext_servername_arg (ptr->ctx, ptr);
}
