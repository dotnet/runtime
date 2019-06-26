//
//  btls-x509-lookup.h
//  MonoBtls
//
//  Created by Martin Baulig on 3/3/16.
//  Copyright © 2016 Xamarin. All rights reserved.
//

#ifndef __btls__btls_x509_lookup__
#define __btls__btls_x509_lookup__

#include <stdio.h>
#include "btls-ssl.h"
#include "btls-x509.h"
#include "btls-x509-store.h"

typedef enum {
	MONO_BTLS_X509_LOOKUP_TYPE_UNKNOWN = 0,
	MONO_BTLS_X509_LOOKUP_TYPE_FILE,
	MONO_BTLS_X509_LOOKUP_TYPE_HASH_DIR,
	MONO_BTLS_X509_LOOKUP_TYPE_MONO
} MonoBtlsX509LookupType;

MONO_API MonoBtlsX509Lookup *
mono_btls_x509_lookup_new (MonoBtlsX509Store *store, MonoBtlsX509LookupType type);

MONO_API int
mono_btls_x509_lookup_load_file (MonoBtlsX509Lookup *lookup, const char *file, MonoBtlsX509FileType type);

MONO_API int
mono_btls_x509_lookup_add_dir (MonoBtlsX509Lookup *lookup, const char *dir, MonoBtlsX509FileType type);

MONO_API MonoBtlsX509Lookup *
mono_btls_x509_lookup_up_ref (MonoBtlsX509Lookup *lookup);

MONO_API int
mono_btls_x509_lookup_free (MonoBtlsX509Lookup *lookup);

MONO_API int
mono_btls_x509_lookup_init (MonoBtlsX509Lookup *lookup);

MONO_API MonoBtlsX509LookupType
mono_btls_x509_lookup_get_type (MonoBtlsX509Lookup *lookup);

MONO_API X509_LOOKUP *
mono_btls_x509_lookup_peek_lookup (MonoBtlsX509Lookup *lookup);

MONO_API int
mono_btls_x509_lookup_shutdown (MonoBtlsX509Lookup *lookup);

MONO_API X509 *
mono_btls_x509_lookup_by_subject (MonoBtlsX509Lookup *lookup, MonoBtlsX509Name *name);

MONO_API X509 *
mono_btls_x509_lookup_by_fingerprint (MonoBtlsX509Lookup *lookup, unsigned char *bytes, int len);

#endif /* defined(__btls__btls_x509_lookup__) */

