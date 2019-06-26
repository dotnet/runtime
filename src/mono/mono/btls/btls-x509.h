//
//  btls-x509.h
//  MonoBtls
//
//  Created by Martin Baulig on 14/11/15.
//  Copyright (c) 2015 Xamarin. All rights reserved.
//

#ifndef __btls__btls_x509__
#define __btls__btls_x509__

#include <stdio.h>
#include "btls-ssl.h"
#include "btls-x509-name.h"

typedef enum {
	MONO_BTLS_X509_FORMAT_DER = 1,
	MONO_BTLS_X509_FORMAT_PEM = 2
} MonoBtlsX509Format;

typedef enum {
	MONO_BTLS_x509_FILE_TYPE_PEM = 1,		// X509_FILETYPE_PEM
	MONO_BTLS_x509_FILE_TYPE_ASN1 = 2,		// X509_FILETYPE_ASN1
	MONO_BTLS_x509_FILE_TYPE_DEFAULT = 3,	// X509_FILETYPE_DEFAULT
} MonoBtlsX509FileType;

typedef enum {
	MONO_BTLS_X509_PURPOSE_SSL_CLIENT		= 1,
	MONO_BTLS_X509_PURPOSE_SSL_SERVER		= 2,
	MONO_BTLS_X509_PURPOSE_NS_SSL_SERVER	= 3,
	MONO_BTLS_X509_PURPOSE_SMIME_SIGN		= 4,
	MONO_BTLS_X509_PURPOSE_SMIME_ENCRYPT	= 5,
	MONO_BTLS_X509_PURPOSE_CRL_SIGN		= 6,
	MONO_BTLS_X509_PURPOSE_ANY			= 7,
	MONO_BTLS_X509_PURPOSE_OCSP_HELPER		= 8,
	MONO_BTLS_X509_PURPOSE_TIMESTAMP_SIGN	= 9,
} MonoBtlsX509Purpose;

typedef enum {
	MONO_BTLS_X509_TRUST_KIND_DEFAULT		= 0,
	MONO_BTLS_X509_TRUST_KIND_TRUST_CLIENT	= 1,
	MONO_BTLS_X509_TRUST_KIND_TRUST_SERVER	= 2,
	MONO_BTLS_X509_TRUST_KIND_TRUST_ALL		= 4,
	MONO_BTLS_X509_TRUST_KIND_REJECT_CLIENT	= 32,
	MONO_BTLS_X509_TRUST_KIND_REJECT_SERVER	= 64,
	MONO_BTLS_X509_TRUST_KIND_REJECT_ALL	= 128
} MonoBtlsX509TrustKind;

MONO_API X509 *
mono_btls_x509_from_data (const void *buf, int len, MonoBtlsX509Format format);

MONO_API X509 *
mono_btls_x509_up_ref (X509 *x509);

MONO_API void
mono_btls_x509_free (X509 *x509);

MONO_API X509 *
mono_btls_x509_dup (X509 *x509);

MONO_API MonoBtlsX509Name *
mono_btls_x509_get_subject_name (X509 *x509);

MONO_API MonoBtlsX509Name *
mono_btls_x509_get_issuer_name (X509 *x509);

MONO_API int
mono_btls_x509_get_subject_name_string (X509 *name, char *buffer, int size);

MONO_API int
mono_btls_x509_get_issuer_name_string (X509 *name, char *buffer, int size);

MONO_API int
mono_btls_x509_get_raw_data (X509 *x509, BIO *bio, MonoBtlsX509Format format);

MONO_API int
mono_btls_x509_cmp (const X509 *a, const X509 *b);

MONO_API int
mono_btls_x509_get_hash (X509 *x509, const void **data);

MONO_API int64_t
mono_btls_x509_get_not_before (X509 *x509);

MONO_API int64_t
mono_btls_x509_get_not_after (X509 *x509);

MONO_API int
mono_btls_x509_get_public_key (X509 *x509, BIO *bio);

MONO_API int
mono_btls_x509_get_public_key_parameters (X509 *x509, char *out_oid, int oid_len, uint8_t **buffer, int *size);

MONO_API int
mono_btls_x509_get_serial_number (X509 *x509, char *buffer, int size, int mono_style);

MONO_API int
mono_btls_x509_get_public_key_algorithm (X509 *x509, char *buffer, int size);

MONO_API int
mono_btls_x509_get_version (X509 *x509);

MONO_API int
mono_btls_x509_get_signature_algorithm (X509 *x509, char *buffer, int size);

MONO_API int
mono_btls_x509_get_public_key_asn1 (X509 *x509, char *out_oid, int oid_len, uint8_t **buffer, int *size);

MONO_API EVP_PKEY *
mono_btls_x509_get_pubkey (X509 *x509);

MONO_API int
mono_btls_x509_get_subject_key_identifier (X509 *x509, uint8_t **buffer, int *size);

MONO_API int
mono_btls_x509_print (X509 *x509, BIO *bio);

MONO_API int
mono_btls_x509_add_trust_object (X509 *x509, MonoBtlsX509Purpose purpose);

MONO_API int
mono_btls_x509_add_reject_object (X509 *x509, MonoBtlsX509Purpose purpose);

MONO_API int
mono_btls_x509_add_explicit_trust (X509 *x509, MonoBtlsX509TrustKind kind);

#endif /* defined(__btls__btls_x509__) */
