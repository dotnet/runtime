//
//  btls-x509-lookup-mono.c
//  MonoBtls
//
//  Created by Martin Baulig on 3/6/16.
//  Copyright © 2016 Xamarin. All rights reserved.
//

#include <btls-x509-lookup.h>
#include <btls-x509-lookup-mono.h>
#include <openssl/stack.h>

// random high number
#define MONO_BTLS_X509_L_MONO_ADD	36292

typedef struct MonoLookupNode MonoLookupNode;
struct MonoLookupNode {
	MonoBtlsX509LookupMono *mono;
	MonoLookupNode *next;
};

typedef struct {
	MonoLookupNode *nodes;
} MonoLookup;

struct MonoBtlsX509LookupMono {
	const void *instance;
	MonoBtlsX509LookupMono_BySubject by_subject_func;
	MonoLookup *lookup;
};

MONO_API MonoBtlsX509LookupMono *
mono_btls_x509_lookup_mono_new (void)
{
	MonoBtlsX509LookupMono *mono;

	mono = OPENSSL_malloc (sizeof (MonoBtlsX509LookupMono));
	if (!mono)
		return NULL;

	memset (mono, 0, sizeof (MonoBtlsX509LookupMono));
	return mono;
}

MONO_API void
mono_btls_x509_lookup_mono_init (MonoBtlsX509LookupMono *mono, const void *instance,
				 MonoBtlsX509LookupMono_BySubject by_subject_func)
{
	mono->instance = instance;
	mono->by_subject_func = by_subject_func;
}

static int
mono_lookup_install (MonoLookup *lookup, MonoBtlsX509LookupMono *mono)
{
	MonoLookupNode *node;

	node = OPENSSL_malloc (sizeof (MonoLookupNode));
	if (!node)
		return 0;

	memset (node, 0, sizeof (MonoLookupNode));
	mono->lookup = lookup;
	node->mono = mono;
	node->next = lookup->nodes;
	lookup->nodes = node;
	return 1;
}

static int
mono_lookup_uninstall (MonoBtlsX509LookupMono *mono)
{
	MonoLookupNode **ptr;

	if (!mono->lookup)
		return 0;

	for (ptr = &mono->lookup->nodes; *ptr; ptr = &(*ptr)->next) {
		if ((*ptr)->mono == mono) {
			*ptr = (*ptr)->next;
			return 1;
		}
	}

	return 0;
}

MONO_API int
mono_btls_x509_lookup_mono_free (MonoBtlsX509LookupMono *mono)
{
	mono->instance = NULL;
	mono->by_subject_func = NULL;

	if (mono->lookup) {
		if (!mono_lookup_uninstall (mono))
			return 0;
	}

	mono->lookup = NULL;

	OPENSSL_free (mono);
	return 1;
}

static int
mono_lookup_ctrl (X509_LOOKUP *ctx, int cmd, const char *argp, long argl, char **ret)
{
	MonoLookup *lookup = (MonoLookup*)ctx->method_data;
	MonoBtlsX509LookupMono *mono = (MonoBtlsX509LookupMono*)argp;

	if (!lookup || cmd != MONO_BTLS_X509_L_MONO_ADD)
		return 0;
	if (!mono || mono->lookup)
		return 0;

	return mono_lookup_install (lookup, mono);
}

static int
mono_lookup_new (X509_LOOKUP *ctx)
{
	MonoLookup *data;

	data = OPENSSL_malloc (sizeof (MonoLookup));
	if (!data)
		return 0;

	memset (data, 0, sizeof (MonoLookup));
	ctx->method_data = (void *)data;
	return 1;
}

static void
mono_lookup_free (X509_LOOKUP *ctx)
{
	MonoLookup *lookup;
	MonoLookupNode *ptr;

	lookup = (MonoLookup *)ctx->method_data;
	ctx->method_data = NULL;
	if (!lookup)
		return;

	ptr = lookup->nodes;
	lookup->nodes = NULL;

	while (ptr) {
		MonoLookupNode *node = ptr;
		ptr = ptr->next;

		if (node->mono)
			node->mono->lookup = NULL;
		node->mono = NULL;
		node->next = NULL;
		OPENSSL_free (node);
	}

	OPENSSL_free (lookup);
}

static int
mono_lookup_get_by_subject (X509_LOOKUP *ctx, int type, X509_NAME *name, X509_OBJECT *obj_ret)
{
	MonoLookup *lookup;
	MonoBtlsX509Name *name_obj;
	MonoLookupNode *node;
	X509 *x509 = NULL;
	int ret = 0;

	lookup = (MonoLookup *)ctx->method_data;

	if (!lookup || !lookup->nodes)
		return 0;
	if (type != X509_LU_X509)
		return 0;

	name_obj = mono_btls_x509_name_from_name (name);
	x509 = NULL;

	for (node = lookup->nodes; node; node = node->next) {
		if (!node->mono || !node->mono->by_subject_func)
			continue;
		ret = (* node->mono->by_subject_func) (node->mono->instance, name_obj, &x509);
		if (ret)
			break;
	}

	mono_btls_x509_name_free (name_obj);

	if (!ret) {
		if (x509)
			X509_free(x509);
		return 0;
	}

	obj_ret->type = X509_LU_X509;
	obj_ret->data.x509 = x509;
	return 1;
}

static X509_LOOKUP_METHOD mono_lookup_method = {
	"Mono lookup method",
	mono_lookup_new,		/* new */
	mono_lookup_free,		/* free */
	NULL,				/* init */
	NULL,				/* shutdown */
	mono_lookup_ctrl,		/* ctrl	*/
	mono_lookup_get_by_subject,	/* get_by_subject */
	NULL,				/* get_by_issuer_serial */
	NULL,				/* get_by_fingerprint */
	NULL,				/* get_by_alias */
};

MONO_API X509_LOOKUP_METHOD *
mono_btls_x509_lookup_mono_method (void)
{
	return &mono_lookup_method;
}

MONO_API int
mono_btls_x509_lookup_add_mono (MonoBtlsX509Lookup *lookup, MonoBtlsX509LookupMono *mono)
{
	if (mono_btls_x509_lookup_get_type (lookup) != MONO_BTLS_X509_LOOKUP_TYPE_MONO)
		return 0;
	return X509_LOOKUP_ctrl (mono_btls_x509_lookup_peek_lookup (lookup),
				 MONO_BTLS_X509_L_MONO_ADD,
				 (void*)mono, 0, NULL);
}
