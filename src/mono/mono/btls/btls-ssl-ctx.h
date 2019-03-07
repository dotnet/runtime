//
//  btls-ssl-ctx.h
//  MonoBtls
//
//  Created by Martin Baulig on 4/11/16.
//  Copyright © 2016 Xamarin. All rights reserved.
//

#ifndef __btls_ssl_ctx__btls_ssl_ctx__
#define __btls_ssl_ctx__btls_ssl_ctx__

#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <openssl/ssl.h>
#include "btls-util.h"

typedef struct MonoBtlsBio MonoBtlsBio;
typedef struct MonoBtlsX509Chain MonoBtlsX509Chain;
typedef struct MonoBtlsX509Crl MonoBtlsX509Crl;
typedef struct MonoBtlsX509Lookup MonoBtlsX509Lookup;
typedef struct MonoBtlsX509LookupMono MonoBtlsX509LookupMono;
typedef struct MonoBtlsX509Name MonoBtlsX509Name;
typedef struct MonoBtlsX509Store MonoBtlsX509Store;
typedef struct MonoBtlsX509StoreCtx MonoBtlsX509StoreCtx;
typedef struct MonoBtlsX509Revoked MonoBtlsX509Revoked;
typedef struct MonoBtlsX509VerifyParam MonoBtlsX509VerifyParam;
typedef struct MonoBtlsPkcs12 MonoBtlsPkcs12;
typedef struct MonoBtlsSsl MonoBtlsSsl;
typedef struct MonoBtlsSslCtx MonoBtlsSslCtx;

typedef int (* MonoBtlsVerifyFunc) (void *instance, int preverify_ok, X509_STORE_CTX *ctx);
typedef int (* MonoBtlsSelectFunc) (void *instance, int countIssuers, const int *sizes, void **issuerData);
typedef int (* MonoBtlsServerNameFunc) (void *instance);

MONO_API MonoBtlsSslCtx *
mono_btls_ssl_ctx_new (void);

MONO_API MonoBtlsSslCtx *
mono_btls_ssl_ctx_up_ref (MonoBtlsSslCtx *ctx);

MONO_API int
mono_btls_ssl_ctx_free (MonoBtlsSslCtx *ctx);

MONO_API void
mono_btls_ssl_ctx_initialize (MonoBtlsSslCtx *ctx, void *instance);

MONO_API SSL_CTX *
mono_btls_ssl_ctx_get_ctx (MonoBtlsSslCtx *ctx);

MONO_API int
mono_btls_ssl_ctx_debug_printf (MonoBtlsSslCtx *ctx, const char *format, ...);

MONO_API int
mono_btls_ssl_ctx_is_debug_enabled (MonoBtlsSslCtx *ctx);

MONO_API void
mono_btls_ssl_ctx_set_cert_verify_callback (MonoBtlsSslCtx *ptr, MonoBtlsVerifyFunc func, int cert_required);

MONO_API void
mono_btls_ssl_ctx_set_cert_select_callback (MonoBtlsSslCtx *ptr, MonoBtlsSelectFunc func);

MONO_API void
mono_btls_ssl_ctx_set_debug_bio (MonoBtlsSslCtx *ctx, BIO *debug_bio);

MONO_API X509_STORE *
mono_btls_ssl_ctx_peek_store (MonoBtlsSslCtx *ctx);

MONO_API void
mono_btls_ssl_ctx_set_min_version (MonoBtlsSslCtx *ctx, int version);

MONO_API void
mono_btls_ssl_ctx_set_max_version (MonoBtlsSslCtx *ctx, int version);

MONO_API int
mono_btls_ssl_ctx_is_cipher_supported (MonoBtlsSslCtx *ctx, uint16_t value);

MONO_API int
mono_btls_ssl_ctx_set_ciphers (MonoBtlsSslCtx *ctx, int count, const uint16_t *data,
				   int allow_unsupported);

MONO_API int
mono_btls_ssl_ctx_set_verify_param (MonoBtlsSslCtx *ctx, const MonoBtlsX509VerifyParam *param);

MONO_API int
mono_btls_ssl_ctx_set_client_ca_list (MonoBtlsSslCtx *ctx, int count, int *sizes, const void **data);

MONO_API void
mono_btls_ssl_ctx_set_server_name_callback (MonoBtlsSslCtx *ctx, MonoBtlsServerNameFunc func);

#endif /* __btls_ssl_ctx__btls_ssl_ctx__ */
