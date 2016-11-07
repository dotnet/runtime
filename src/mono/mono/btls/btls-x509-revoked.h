//
//  btls-x509-revoked.h
//  MonoBtls
//
//  Created by Martin Baulig on 3/23/16.
//  Copyright © 2016 Xamarin. All rights reserved.
//

#ifndef __btls__btls_x509_revoked__
#define __btls__btls_x509_revoked__

#include <stdio.h>
#include <btls-ssl.h>
#include <btls-x509-crl.h>

MonoBtlsX509Revoked *
mono_btls_x509_revoked_new (MonoBtlsX509Crl *owner, X509_REVOKED *revoked);

void
mono_btls_x509_revoked_free (MonoBtlsX509Revoked *revoked);

int
mono_btls_x509_revoked_get_serial_number (MonoBtlsX509Revoked *revoked, char *buffer, int size);

int64_t
mono_btls_x509_revoked_get_revocation_date (MonoBtlsX509Revoked *revoked);

int
mono_btls_x509_revoked_get_reason (MonoBtlsX509Revoked *revoked);

int
mono_btls_x509_revoked_get_sequence (MonoBtlsX509Revoked *revoked);

#endif /* __btls__btls_x509_revoked__ */
