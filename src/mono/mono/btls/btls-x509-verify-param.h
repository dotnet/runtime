//
//  btls-x509-verify-param.h
//  MonoBtls
//
//  Created by Martin Baulig on 3/3/16.
//  Copyright © 2016 Xamarin. All rights reserved.
//

#ifndef __btls__btls_x509_verify_param__
#define __btls__btls_x509_verify_param__

#include <stdio.h>
#include "btls-ssl.h"
#include "btls-x509.h"

typedef enum {
	MONO_BTLS_X509_VERIFY_FLAGS_DEFAULT		= 0,
	MONO_BTLS_X509_VERIFY_FLAGS_CRL_CHECK	= 1,
	MONO_BTLS_X509_VERIFY_FLAGS_CRL_CHECK_ALL	= 2,
	MONO_BTLS_X509_VERIFY_FLAGS_X509_STRICT	= 4
} MonoBtlsX509VerifyFlags;

MONO_API MonoBtlsX509VerifyParam *
mono_btls_x509_verify_param_new (void);

MONO_API MonoBtlsX509VerifyParam *
mono_btls_x509_verify_param_from_store_ctx (MonoBtlsX509StoreCtx *ctx, X509_VERIFY_PARAM *param);

MONO_API MonoBtlsX509VerifyParam *
mono_btls_x509_verify_param_copy (const MonoBtlsX509VerifyParam *from);

MONO_API void
mono_btls_x509_verify_param_free (MonoBtlsX509VerifyParam *param);

MONO_API const X509_VERIFY_PARAM *
mono_btls_x509_verify_param_peek_param (const MonoBtlsX509VerifyParam *param);

MONO_API int
mono_btls_x509_verify_param_can_modify (MonoBtlsX509VerifyParam *param);

MONO_API MonoBtlsX509VerifyParam *
mono_btls_x509_verify_param_lookup (const char *name);

MONO_API int
mono_btls_x509_verify_param_set_name (MonoBtlsX509VerifyParam *param, const char *name);

MONO_API int
mono_btls_x509_verify_param_set_host (MonoBtlsX509VerifyParam *param, const char *host, int namelen);

MONO_API int
mono_btls_x509_verify_param_add_host (MonoBtlsX509VerifyParam *param, const char *host, int namelen);

MONO_API uint64_t
mono_btls_x509_verify_param_get_flags (MonoBtlsX509VerifyParam *param);

MONO_API int
mono_btls_x509_verify_param_set_flags (MonoBtlsX509VerifyParam *param, uint64_t flags);

MONO_API MonoBtlsX509VerifyFlags
mono_btls_x509_verify_param_get_mono_flags (MonoBtlsX509VerifyParam *param);

MONO_API int
mono_btls_x509_verify_param_set_mono_flags (MonoBtlsX509VerifyParam *param, MonoBtlsX509VerifyFlags flags);

MONO_API int
mono_btls_x509_verify_param_set_purpose (MonoBtlsX509VerifyParam *param, MonoBtlsX509Purpose purpose);

MONO_API int
mono_btls_x509_verify_param_get_depth (MonoBtlsX509VerifyParam *param);

MONO_API int
mono_btls_x509_verify_param_set_depth (MonoBtlsX509VerifyParam *param, int depth);

MONO_API int
mono_btls_x509_verify_param_set_time (MonoBtlsX509VerifyParam *param, int64_t time);

MONO_API char *
mono_btls_x509_verify_param_get_peername (MonoBtlsX509VerifyParam *param);

#endif /* defined(__btls__btls_x509_verify_param__) */

