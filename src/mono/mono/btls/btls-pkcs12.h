//
//  btls-pkcs12.h
//  MonoBtls
//
//  Created by Martin Baulig on 3/8/16.
//  Copyright © 2016 Xamarin. All rights reserved.
//

#ifndef __btls__btls_pkcs12__
#define __btls__btls_pkcs12__

#include <stdio.h>
#include <btls-ssl.h>
#include <btls-x509.h>

MonoBtlsPkcs12 *
mono_btls_pkcs12_new (void);

int
mono_btls_pkcs12_get_count (MonoBtlsPkcs12 *pkcs12);

X509 *
mono_btls_pkcs12_get_cert (MonoBtlsPkcs12 *pkcs12, int index);

STACK_OF(X509) *
mono_btls_pkcs12_get_certs (MonoBtlsPkcs12 *pkcs12);

int
mono_btls_pkcs12_free (MonoBtlsPkcs12 *pkcs12);

MonoBtlsPkcs12 *
mono_btls_pkcs12_up_ref (MonoBtlsPkcs12 *pkcs12);

void
mono_btls_pkcs12_add_cert (MonoBtlsPkcs12 *pkcs12, X509 *x509);

int
mono_btls_pkcs12_import (MonoBtlsPkcs12 *pkcs12, const void *data, int len, const void *password);

int
mono_btls_pkcs12_has_private_key (MonoBtlsPkcs12 *pkcs12);

EVP_PKEY *
mono_btls_pkcs12_get_private_key (MonoBtlsPkcs12 *pkcs12);

#endif /* __btls__btls_pkcs12__ */
