//
//  btls-x509-store.h
//  MonoBtls
//
//  Created by Martin Baulig on 3/3/16.
//  Copyright © 2016 Xamarin. All rights reserved.
//

#ifndef __btls__btls_x509_store__
#define __btls__btls_x509_store__

#include <stdio.h>
#include "btls-ssl.h"

MONO_API MonoBtlsX509Store *
mono_btls_x509_store_new (void);

MONO_API MonoBtlsX509Store *
mono_btls_x509_store_from_store (X509_STORE *ctx);

MONO_API MonoBtlsX509Store *
mono_btls_x509_store_from_ctx (X509_STORE_CTX *ctx);

MONO_API MonoBtlsX509Store *
mono_btls_x509_store_from_ssl_ctx (MonoBtlsSslCtx *ctx);

MONO_API MonoBtlsX509Store *
mono_btls_x509_store_up_ref (MonoBtlsX509Store *store);

MONO_API int
mono_btls_x509_store_free (MonoBtlsX509Store *store);

MONO_API X509_STORE *
mono_btls_x509_store_peek_store (MonoBtlsX509Store *store);

MONO_API int
mono_btls_x509_store_add_cert (MonoBtlsX509Store *store, X509 *cert);

MONO_API int
mono_btls_x509_store_load_locations (MonoBtlsX509Store *store, const char *file, const char *path);

MONO_API int
mono_btls_x509_store_set_default_paths (MonoBtlsX509Store *store);

MONO_API int
mono_btls_x509_store_get_count (MonoBtlsX509Store *store);

#endif /* defined(__btls__btls_x509_store__) */

