//
//  btls-x509-store.c
//  MonoBtls
//
//  Created by Martin Baulig on 3/3/16.
//  Copyright © 2016 Xamarin. All rights reserved.
//

#include "btls-x509-store.h"

struct MonoBtlsX509Store {
	X509_STORE *store;
	CRYPTO_refcount_t references;
};

MonoBtlsX509Store *
mono_btls_x509_store_from_store (X509_STORE *ctx)
{
	MonoBtlsX509Store *store;

	store = OPENSSL_malloc (sizeof(MonoBtlsX509Store));
	if (!store)
		return NULL;

	memset (store, 0, sizeof(MonoBtlsX509Store));
	store->store = ctx;
	CRYPTO_refcount_inc (&store->store->references);
	store->references = 1;
	return store;
}

MonoBtlsX509Store *
mono_btls_x509_store_from_ctx (X509_STORE_CTX *ctx)
{
	return mono_btls_x509_store_from_store (ctx->ctx);
}

MonoBtlsX509Store *
mono_btls_x509_store_new (void)
{
	MonoBtlsX509Store *store;

	store = OPENSSL_malloc (sizeof(MonoBtlsX509Store));
	if (!store)
		return NULL;

	memset (store, 0, sizeof(MonoBtlsX509Store));
	store->store = X509_STORE_new ();
	store->references = 1;
	return store;
}

X509_STORE *
mono_btls_x509_store_peek_store (MonoBtlsX509Store *store)
{
	return store->store;
}

MonoBtlsX509Store *
mono_btls_x509_store_from_ssl_ctx (MonoBtlsSslCtx *ctx)
{
	X509_STORE *store = mono_btls_ssl_ctx_peek_store (ctx);
	return mono_btls_x509_store_from_store (store);
}

int
mono_btls_x509_store_free (MonoBtlsX509Store *store)
{
	if (!CRYPTO_refcount_dec_and_test_zero(&store->references))
		return 0;

	if (store->store) {
		X509_STORE_free (store->store);
		store->store = NULL;
	}
	OPENSSL_free (store);
	return 1;
}

MonoBtlsX509Store *
mono_btls_x509_store_up_ref (MonoBtlsX509Store *store)
{
	CRYPTO_refcount_inc (&store->references);
	return store;
}

int
mono_btls_x509_store_add_cert (MonoBtlsX509Store *store, X509 *cert)
{
	return X509_STORE_add_cert (store->store, cert);
}

int
mono_btls_x509_store_load_locations (MonoBtlsX509Store *store, const char *file, const char *path)
{
	return X509_STORE_load_locations (store->store, file, path);
}

int
mono_btls_x509_store_set_default_paths (MonoBtlsX509Store *store)
{
	return X509_STORE_set_default_paths (store->store);
}

int
mono_btls_x509_store_get_count (MonoBtlsX509Store *store)
{
	return (int)sk_X509_OBJECT_num (store->store->objs);
}

