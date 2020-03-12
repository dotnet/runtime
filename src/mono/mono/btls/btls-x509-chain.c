//
//  btls-x509-chain.c
//  MonoBtls
//
//  Created by Martin Baulig on 3/3/16.
//  Copyright © 2016 Xamarin. All rights reserved.
//

#include "btls-x509-chain.h"

struct MonoBtlsX509Chain {
	STACK_OF(X509) *certs;
	CRYPTO_refcount_t references;
};

MonoBtlsX509Chain *
mono_btls_x509_chain_new (void)
{
	MonoBtlsX509Chain *chain = (MonoBtlsX509Chain *)OPENSSL_malloc (sizeof (MonoBtlsX509Chain));
	if (chain == NULL)
		return NULL;

	memset(chain, 0, sizeof(MonoBtlsX509Chain));
	chain->certs = sk_X509_new_null ();
	chain->references = 1;
	return chain;
}

MonoBtlsX509Chain *
mono_btls_x509_chain_from_certs (STACK_OF(X509) *certs)
{
	MonoBtlsX509Chain *chain = (MonoBtlsX509Chain *)OPENSSL_malloc (sizeof (MonoBtlsX509Chain));
	if (chain == NULL)
		return NULL;

	memset(chain, 0, sizeof(MonoBtlsX509Chain));
	chain->certs = X509_chain_up_ref(certs);
	chain->references = 1;
	return chain;
}

STACK_OF(X509) *
mono_btls_x509_chain_peek_certs (MonoBtlsX509Chain *chain)
{
	return chain->certs;
}

int
mono_btls_x509_chain_get_count (MonoBtlsX509Chain *chain)
{
	return (int)sk_X509_num(chain->certs);
}

X509 *
mono_btls_x509_chain_get_cert (MonoBtlsX509Chain *chain, int index)
{
	X509 *cert;

	if ((size_t)index >= sk_X509_num(chain->certs))
		return NULL;
	cert = sk_X509_value(chain->certs, index);
	if (cert)
		X509_up_ref(cert);
	return cert;
}

STACK_OF(X509) *
mono_btls_x509_chain_get_certs (MonoBtlsX509Chain *chain)
{
	return chain->certs;
}

int
mono_btls_x509_chain_free (MonoBtlsX509Chain *chain)
{
	if (!CRYPTO_refcount_dec_and_test_zero(&chain->references))
		return 0;

	sk_X509_pop_free(chain->certs, X509_free);
	OPENSSL_free (chain);
	return 1;
}

MonoBtlsX509Chain *
mono_btls_x509_chain_up_ref (MonoBtlsX509Chain *chain)
{
	CRYPTO_refcount_inc(&chain->references);
	return chain;
}

void
mono_btls_x509_chain_add_cert (MonoBtlsX509Chain *chain, X509 *x509)
{
	X509_up_ref(x509);
	sk_X509_push(chain->certs, x509);
}
