//
//  btls-x509-lookup-mono.h
//  MonoBtls
//
//  Created by Martin Baulig on 3/3/16.
//  Copyright © 2016 Xamarin. All rights reserved.
//

#ifndef __btls__btls_x509_lookup_mono__
#define __btls__btls_x509_lookup_mono__

#include <stdio.h>
#include <btls-ssl.h>
#include <btls-x509.h>
#include <btls-x509-store.h>

typedef int (* MonoBtlsX509LookupMono_BySubject) (const void *instance, MonoBtlsX509Name *name, X509 **ret);

MonoBtlsX509LookupMono *
mono_btls_x509_lookup_mono_new (void);

int
mono_btls_x509_lookup_mono_free (MonoBtlsX509LookupMono *mono);

void
mono_btls_x509_lookup_mono_init (MonoBtlsX509LookupMono *mono, const void *instance,
				 MonoBtlsX509LookupMono_BySubject by_subject_func);

int
mono_btls_x509_lookup_add_mono (MonoBtlsX509Lookup *lookup, MonoBtlsX509LookupMono *mono);

X509_LOOKUP_METHOD *
mono_btls_x509_lookup_mono_method (void);

#endif /* defined(__btls__btls_x509_lookup_mono__) */

