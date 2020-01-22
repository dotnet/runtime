//
//  btls-x509-chain.h
//  MonoBtls
//
//  Created by Martin Baulig on 3/3/16.
//  Copyright © 2016 Xamarin. All rights reserved.
//

#ifndef __btls__btls_x509_chain__
#define __btls__btls_x509_chain__

#include <stdio.h>
#include "btls-ssl.h"
#include "btls-x509.h"

MONO_API MonoBtlsX509Chain *
mono_btls_x509_chain_new (void);

MONO_API MonoBtlsX509Chain *
mono_btls_x509_chain_from_certs (STACK_OF(X509) *certs);

MONO_API STACK_OF(X509) *
mono_btls_x509_chain_peek_certs (MonoBtlsX509Chain *chain);

MONO_API int
mono_btls_x509_chain_get_count (MonoBtlsX509Chain *chain);

MONO_API X509 *
mono_btls_x509_chain_get_cert (MonoBtlsX509Chain *chain, int index);

MONO_API STACK_OF(X509) *
mono_btls_x509_chain_get_certs (MonoBtlsX509Chain *chain);

MONO_API MonoBtlsX509Chain *
mono_btls_x509_chain_up_ref (MonoBtlsX509Chain *chain);

MONO_API int
mono_btls_x509_chain_free (MonoBtlsX509Chain *chain);

MONO_API void
mono_btls_x509_chain_add_cert (MonoBtlsX509Chain *chain, X509 *x509);

#endif /* defined(__btls__btls_x509_chain__) */

