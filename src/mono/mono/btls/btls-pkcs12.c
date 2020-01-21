//
//  btls-pkcs12.c
//  MonoBtls
//
//  Created by Martin Baulig on 3/8/16.
//  Copyright © 2016 Xamarin. All rights reserved.
//

#include "btls-pkcs12.h"
#include <openssl/pkcs12.h>

#ifdef WIN32
#include <windows.h>
#endif

struct MonoBtlsPkcs12 {
	STACK_OF(X509) *certs;
	EVP_PKEY *private_key;
	CRYPTO_refcount_t references;
};

// Passwords are marsahled directly in pinvokes from a SafePasswordHandle that uses different encoding
// depending on used PAL (SafePasswordHande.Unix.cs, SafePasswordHande.Windows.cs).
// On Unix:es, passwords are stored using Marshal.StringToHGlobalAnsi that can be passed directly to OpenSSL
// but on Windows passwords are stored using Marshal.StringToHGlobalUni, most likely because Windows Crypto API
// uses Unicode strings for passwords. Instead of changing this pattern (passing passwords as SafePasswordHandle
// throught pinvokes) at this particular call site convert things to UTF-8 before calling into OpenSSL.
#ifdef WIN32
static void
deallocate_btls_password (char *password)
{
	if (password) {
		memset (password, 0, strlen (password));
		free (password);
	}
}

static char *
allocate_btls_password (const void *password)
{
	char *buffer = NULL;
	int buffer_size = WideCharToMultiByte (CP_UTF8, 0, (PCWCH)password, -1, NULL, 0, NULL, NULL);
	if (buffer_size != 0) {
		buffer = malloc (buffer_size);
		if (buffer) {
			buffer [buffer_size - 1] = '\0';
			if (WideCharToMultiByte (CP_UTF8, 0, (PCWCH)password, -1, buffer, buffer_size, NULL, NULL) == 0) {
				// Failed to convert buffer.
				deallocate_btls_password (buffer);
				buffer = NULL;
			}
		}
	}

	return buffer;
}
#else
static void
deallocate_btls_password (char *password)
{
	return;
}

static char *
allocate_btls_password (const void *password)
{
	return (char *)password;
}
#endif

MonoBtlsPkcs12 *
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

int
mono_btls_pkcs12_get_count (MonoBtlsPkcs12 *pkcs12)
{
	return (int)sk_X509_num (pkcs12->certs);
}

X509 *
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

STACK_OF(X509) *
mono_btls_pkcs12_get_certs (MonoBtlsPkcs12 *pkcs12)
{
	return pkcs12->certs;
}

int
mono_btls_pkcs12_free (MonoBtlsPkcs12 *pkcs12)
{
	if (!CRYPTO_refcount_dec_and_test_zero (&pkcs12->references))
		return 0;

	sk_X509_pop_free (pkcs12->certs, X509_free);
	OPENSSL_free (pkcs12);
	return 1;
}

MonoBtlsPkcs12 *
mono_btls_pkcs12_up_ref (MonoBtlsPkcs12 *pkcs12)
{
	CRYPTO_refcount_inc (&pkcs12->references);
	return pkcs12;
}

void
mono_btls_pkcs12_add_cert (MonoBtlsPkcs12 *pkcs12, X509 *x509)
{
	X509_up_ref (x509);
	sk_X509_push (pkcs12->certs, x509);
}

static int
btls_pkcs12_import (MonoBtlsPkcs12 *pkcs12, const void *data, int len, const char *btls_password)
{
	CBS cbs;
	CBS_init (&cbs, data, len);
	int ret;

	ret = PKCS12_get_key_and_certs (&pkcs12->private_key, pkcs12->certs, &cbs, btls_password);
	if ((ret == 1) || (btls_password && strlen (btls_password) > 0))
		return ret;

	// When passed an empty password, we try both NULL and the empty string.
	CBS_init (&cbs, data, len);
	if (btls_password)
		return PKCS12_get_key_and_certs (&pkcs12->private_key, pkcs12->certs, &cbs, NULL);
	else
		return PKCS12_get_key_and_certs (&pkcs12->private_key, pkcs12->certs, &cbs, "");
}

int
mono_btls_pkcs12_import (MonoBtlsPkcs12 *pkcs12, const void *data, int len, const void *password)
{
	char *btls_password = allocate_btls_password (password);
	int ret = btls_pkcs12_import (pkcs12, data, len, btls_password);
	deallocate_btls_password (btls_password);
	return ret;
}

int
mono_btls_pkcs12_has_private_key (MonoBtlsPkcs12 *pkcs12)
{
	return pkcs12->private_key != NULL;
}

EVP_PKEY *
mono_btls_pkcs12_get_private_key (MonoBtlsPkcs12 *pkcs12)
{
	if (!pkcs12->private_key)
		return NULL;
	return EVP_PKEY_up_ref (pkcs12->private_key);
}
