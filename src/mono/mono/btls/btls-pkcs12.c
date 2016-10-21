//
//  btls-pkcs12.c
//  MonoBtls
//
//  Created by Martin Baulig on 3/8/16.
//  Copyright © 2016 Xamarin. All rights reserved.
//

#include <btls-pkcs12.h>
#include <openssl/pkcs12.h>

struct MonoBtlsPkcs12 {
	STACK_OF(X509) *certs;
	EVP_PKEY *private_key;
	CRYPTO_refcount_t references;
};

MONO_API MonoBtlsPkcs12 *
mono_btls_pkcs12_new (void)
{
	MonoBtlsPkcs12 *pkcs12 = (MonoBtlsPkcs12 *)OPENSSL_malloc (sizeof (MonoBtlsPkcs12));
	if (pkcs12 == NULL)
		return NULL;

	memset (pkcs12, 0, sizeof(MonoBtlsPkcs12));
	pkcs12->certs = sk_X509_new_null ();
	pkcs12->references = 1;
	return pkcs12;
}

MONO_API int
mono_btls_pkcs12_get_count (MonoBtlsPkcs12 *pkcs12)
{
	return (int)sk_X509_num (pkcs12->certs);
}

MONO_API X509 *
mono_btls_pkcs12_get_cert (MonoBtlsPkcs12 *pkcs12, int index)
{
	X509 *cert;

	if ((size_t)index >= sk_X509_num (pkcs12->certs))
		return NULL;
	cert = sk_X509_value (pkcs12->certs, index);
	if (cert)
		X509_up_ref (cert);
	return cert;
}

MONO_API STACK_OF(X509) *
mono_btls_pkcs12_get_certs (MonoBtlsPkcs12 *pkcs12)
{
	return pkcs12->certs;
}

MONO_API int
mono_btls_pkcs12_free (MonoBtlsPkcs12 *pkcs12)
{
	if (!CRYPTO_refcount_dec_and_test_zero (&pkcs12->references))
		return 0;

	sk_X509_pop_free (pkcs12->certs, X509_free);
	OPENSSL_free (pkcs12);
	return 1;
}

MONO_API MonoBtlsPkcs12 *
mono_btls_pkcs12_up_ref (MonoBtlsPkcs12 *pkcs12)
{
	CRYPTO_refcount_inc (&pkcs12->references);
	return pkcs12;
}

MONO_API void
mono_btls_pkcs12_add_cert (MonoBtlsPkcs12 *pkcs12, X509 *x509)
{
	X509_up_ref (x509);
	sk_X509_push (pkcs12->certs, x509);
}

MONO_API int
mono_btls_pkcs12_import (MonoBtlsPkcs12 *pkcs12, const void *data, int len, const void *password)
{
	CBS cbs;
	CBS_init (&cbs, data, len);
	return PKCS12_get_key_and_certs (&pkcs12->private_key, pkcs12->certs, &cbs, password);
}

MONO_API int
mono_btls_pkcs12_has_private_key (MonoBtlsPkcs12 *pkcs12)
{
	return pkcs12->private_key != NULL;
}

MONO_API EVP_PKEY *
mono_btls_pkcs12_get_private_key (MonoBtlsPkcs12 *pkcs12)
{
	if (!pkcs12->private_key)
		return NULL;
	return EVP_PKEY_up_ref (pkcs12->private_key);
}
