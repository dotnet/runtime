//
//  btls-x509-verify-param.c
//  MonoBtls
//
//  Created by Martin Baulig on 3/5/16.
//  Copyright © 2016 Xamarin. All rights reserved.
//

#include "btls-x509-verify-param.h"
#include "btls-x509-store-ctx.h"

struct MonoBtlsX509VerifyParam {
	int owns;
	MonoBtlsX509StoreCtx *owner;
	X509_VERIFY_PARAM *param;
};

MonoBtlsX509VerifyParam *
mono_btls_x509_verify_param_new (void)
{
	MonoBtlsX509VerifyParam *param;

	param = OPENSSL_malloc (sizeof(MonoBtlsX509VerifyParam));
	if (!param)
		return NULL;
	memset (param, 0, sizeof (MonoBtlsX509VerifyParam));
	param->param = X509_VERIFY_PARAM_new();
	param->owns = 1;
	return param;
}

MonoBtlsX509VerifyParam *
mono_btls_x509_verify_param_from_store_ctx (MonoBtlsX509StoreCtx *ctx, X509_VERIFY_PARAM *param)
{
	MonoBtlsX509VerifyParam *instance;

	instance = OPENSSL_malloc (sizeof(MonoBtlsX509VerifyParam));
	if (!instance)
		return NULL;
	memset (instance, 0, sizeof (MonoBtlsX509VerifyParam));
	instance->param = param;
	instance->owner = mono_btls_x509_store_ctx_up_ref (ctx);
	return instance;
}

MonoBtlsX509VerifyParam *
mono_btls_x509_verify_param_copy (const MonoBtlsX509VerifyParam *from)
{
	MonoBtlsX509VerifyParam *param;

	param = mono_btls_x509_verify_param_new ();
	if (!param)
		return NULL;

	X509_VERIFY_PARAM_set1 (param->param, from->param);
	return param;
}

const X509_VERIFY_PARAM *
mono_btls_x509_verify_param_peek_param (const MonoBtlsX509VerifyParam *param)
{
	return param->param;
}

int
mono_btls_x509_verify_param_can_modify (MonoBtlsX509VerifyParam *param)
{
	return param->owns;
}

MonoBtlsX509VerifyParam *
mono_btls_x509_verify_param_lookup (const char *name)
{
	MonoBtlsX509VerifyParam *param;
	const X509_VERIFY_PARAM *p;

	p = X509_VERIFY_PARAM_lookup(name);
	if (!p)
		return NULL;

	param = OPENSSL_malloc (sizeof(MonoBtlsX509VerifyParam));
	if (!param)
		return NULL;
	memset (param, 0, sizeof (MonoBtlsX509VerifyParam));
	param->param = (X509_VERIFY_PARAM *)p;
	return param;
}

void
mono_btls_x509_verify_param_free (MonoBtlsX509VerifyParam *param)
{
	if (param->owns) {
		if (param->param) {
			X509_VERIFY_PARAM_free (param->param);
			param->param = NULL;
		}
	}
	if (param->owner) {
		mono_btls_x509_store_ctx_free (param->owner);
		param->owner = NULL;
	}
	OPENSSL_free (param);
}

int
mono_btls_x509_verify_param_set_name (MonoBtlsX509VerifyParam *param, const char *name)
{
	if (!param->owns)
		return -1;
	return X509_VERIFY_PARAM_set1_name (param->param, name);
}

int
mono_btls_x509_verify_param_set_host (MonoBtlsX509VerifyParam *param, const char *host, int namelen)
{
	if (!param->owns)
		return -1;
	return X509_VERIFY_PARAM_set1_host (param->param, host, namelen);
}

int
mono_btls_x509_verify_param_add_host (MonoBtlsX509VerifyParam *param, const char *host, int namelen)
{
	if (!param->owns)
		return -1;
	return X509_VERIFY_PARAM_set1_host (param->param, host, namelen);
}

uint64_t
mono_btls_x509_verify_param_get_flags (MonoBtlsX509VerifyParam *param)
{
	return X509_VERIFY_PARAM_get_flags (param->param);
}

int
mono_btls_x509_verify_param_set_flags (MonoBtlsX509VerifyParam *param, uint64_t flags)
{
	if (!param->owns)
		return -1;
	return X509_VERIFY_PARAM_set_flags (param->param, (unsigned long)flags);
}

MonoBtlsX509VerifyFlags
mono_btls_x509_verify_param_get_mono_flags (MonoBtlsX509VerifyParam *param)
{
	MonoBtlsX509VerifyFlags current;
	uint64_t flags;

	current = 0;
	flags = X509_VERIFY_PARAM_get_flags (param->param);

	if (flags & X509_V_FLAG_CRL_CHECK)
		current |= MONO_BTLS_X509_VERIFY_FLAGS_CRL_CHECK;
	if (flags & X509_V_FLAG_CRL_CHECK_ALL)
		current |= MONO_BTLS_X509_VERIFY_FLAGS_CRL_CHECK_ALL;
	if (flags & X509_V_FLAG_X509_STRICT)
		current |= MONO_BTLS_X509_VERIFY_FLAGS_X509_STRICT;

	return current;
}

int
mono_btls_x509_verify_param_set_mono_flags (MonoBtlsX509VerifyParam *param, MonoBtlsX509VerifyFlags flags)
{
	unsigned long current;

	if (!param->owns)
		return -1;

	current = X509_VERIFY_PARAM_get_flags (param->param);
	if (flags & MONO_BTLS_X509_VERIFY_FLAGS_CRL_CHECK)
		current |= X509_V_FLAG_CRL_CHECK;
	if (flags & MONO_BTLS_X509_VERIFY_FLAGS_CRL_CHECK_ALL)
		current |= X509_V_FLAG_CRL_CHECK_ALL;
	if (flags & MONO_BTLS_X509_VERIFY_FLAGS_X509_STRICT)
		current |= X509_V_FLAG_X509_STRICT;

	return X509_VERIFY_PARAM_set_flags (param->param, current);
}

int
mono_btls_x509_verify_param_set_purpose (MonoBtlsX509VerifyParam *param, MonoBtlsX509Purpose purpose)
{
	if (!param->owns)
		return -1;
	return X509_VERIFY_PARAM_set_purpose (param->param, purpose);
}

int
mono_btls_x509_verify_param_get_depth (MonoBtlsX509VerifyParam *param)
{
	return X509_VERIFY_PARAM_get_depth (param->param);
}

int
mono_btls_x509_verify_param_set_depth (MonoBtlsX509VerifyParam *param, int depth)
{
	if (!param->owns)
		return -1;
	X509_VERIFY_PARAM_set_depth (param->param, depth);
	return 1;
}

int
mono_btls_x509_verify_param_set_time (MonoBtlsX509VerifyParam *param, int64_t time)
{
	if (!param->owns)
		return -1;
	X509_VERIFY_PARAM_set_time (param->param, time);
	return 1;
}

char *
mono_btls_x509_verify_param_get_peername (MonoBtlsX509VerifyParam *param)
{
	char *peer = X509_VERIFY_PARAM_get0_peername (param->param);
	return peer;
}
