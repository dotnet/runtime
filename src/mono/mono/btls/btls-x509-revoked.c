//
//  btls-x509-revoked.c
//  MonoBtls
//
//  Created by Martin Baulig on 3/23/16.
//  Copyright © 2016 Xamarin. All rights reserved.
//

#include <btls-x509-revoked.h>

struct MonoBtlsX509Revoked {
	MonoBtlsX509Crl *owner;
	X509_REVOKED *revoked;
};

MONO_API MonoBtlsX509Revoked *
mono_btls_x509_revoked_new (MonoBtlsX509Crl *owner, X509_REVOKED *revoked)
{
	MonoBtlsX509Revoked *instance;

	instance = OPENSSL_malloc (sizeof (MonoBtlsX509Revoked));
	memset (instance, 0, sizeof (MonoBtlsX509Revoked));

	instance->owner = mono_btls_x509_crl_ref (owner);
	instance->revoked = revoked;
	return instance;
}

MONO_API void
mono_btls_x509_revoked_free (MonoBtlsX509Revoked *revoked)
{
	mono_btls_x509_crl_free (revoked->owner);
	OPENSSL_free (revoked);
}

MONO_API int
mono_btls_x509_revoked_get_serial_number (MonoBtlsX509Revoked *revoked, char *buffer, int size)
{
	ASN1_INTEGER *serial;

	serial = revoked->revoked->serialNumber;
	if (serial->length == 0 || serial->length+1 > size)
		return 0;

	memcpy (buffer, serial->data, serial->length);
	return serial->length;
}

MONO_API int64_t
mono_btls_x509_revoked_get_revocation_date (MonoBtlsX509Revoked *revoked)
{
	ASN1_TIME *date;

	date = revoked->revoked->revocationDate;
	if (!date)
		return 0;

	return mono_btls_util_asn1_time_to_ticks (date);
}

MONO_API int
mono_btls_x509_revoked_get_reason (MonoBtlsX509Revoked *revoked)
{
	return revoked->revoked->reason;
}

MONO_API int
mono_btls_x509_revoked_get_sequence (MonoBtlsX509Revoked *revoked)
{
	return revoked->revoked->sequence;
}

