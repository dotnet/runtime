//
//  btls-x509-crl.h
//  MonoBtls
//
//  Created by Martin Baulig on 3/23/16.
//  Copyright © 2016 Xamarin. All rights reserved.
//

#ifndef __btls__btls_x509_crl__
#define __btls__btls_x509_crl__

#include <stdio.h>
#include <btls-ssl.h>
#include <btls-x509.h>

MonoBtlsX509Crl *
mono_btls_x509_crl_from_data (const void *buf, int len, MonoBtlsX509Format format);

MonoBtlsX509Crl *
mono_btls_x509_crl_ref (MonoBtlsX509Crl *crl);

int
mono_btls_x509_crl_free (MonoBtlsX509Crl *crl);

MonoBtlsX509Revoked *
mono_btls_x509_crl_get_by_cert (MonoBtlsX509Crl *crl, X509 *x509);

MonoBtlsX509Revoked *
mono_btls_x509_crl_get_by_serial (MonoBtlsX509Crl *crl, void *serial, int len);

int
mono_btls_x509_crl_get_revoked_count (MonoBtlsX509Crl *crl);

MonoBtlsX509Revoked *
mono_btls_x509_crl_get_revoked (MonoBtlsX509Crl *crl, int index);

int64_t
mono_btls_x509_crl_get_last_update (MonoBtlsX509Crl *crl);

int64_t
mono_btls_x509_crl_get_next_update (MonoBtlsX509Crl *crl);

int64_t
mono_btls_x509_crl_get_version (MonoBtlsX509Crl *crl);

MonoBtlsX509Name *
mono_btls_x509_crl_get_issuer (MonoBtlsX509Crl *crl);

#endif /* __btls__btls_x509_crl__ */
