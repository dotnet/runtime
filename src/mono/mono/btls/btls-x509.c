//
//  btls-x509.c
//  MonoBtls
//
//  Created by Martin Baulig on 14/11/15.
//  Copyright (c) 2015 Xamarin. All rights reserved.
//

#include <btls-x509.h>
#include <openssl/x509v3.h>
#include <openssl/pkcs12.h>

MONO_API X509 *
mono_btls_x509_from_data (const void *buf, int len, MonoBtlsX509Format format)
{
	BIO *bio;
	X509 *cert = NULL;

	bio = BIO_new_mem_buf ((void *)buf, len);
	switch (format) {
		case MONO_BTLS_X509_FORMAT_DER:
			cert = d2i_X509_bio (bio, NULL);
			break;
		case MONO_BTLS_X509_FORMAT_PEM:
			cert = PEM_read_bio_X509 (bio, NULL, NULL, NULL);
			break;
	}
	BIO_free (bio);
	return cert;
}

MONO_API X509 *
mono_btls_x509_up_ref (X509 *x509)
{
	X509_up_ref (x509);
	return x509;
}

MONO_API void
mono_btls_x509_free (X509 *x509)
{
	X509_free (x509);
}

MONO_API X509 *
mono_btls_x509_dup (X509 *x509)
{
	return X509_dup (x509);
}

MONO_API MonoBtlsX509Name *
mono_btls_x509_get_subject_name (X509 *x509)
{
	return mono_btls_x509_name_copy (X509_get_subject_name (x509));
}

MONO_API MonoBtlsX509Name *
mono_btls_x509_get_issuer_name (X509 *x509)
{
	return mono_btls_x509_name_copy (X509_get_issuer_name (x509));
}

MONO_API int
mono_btls_x509_get_subject_name_string (X509 *name, char *buffer, int size)
{
	*buffer = 0;
	return X509_NAME_oneline (X509_get_subject_name (name), buffer, size) != NULL;
}

MONO_API int
mono_btls_x509_get_issuer_name_string (X509 *name, char *buffer, int size)
{
	*buffer = 0;
	return X509_NAME_oneline (X509_get_issuer_name (name), buffer, size) != NULL;
}

MONO_API int
mono_btls_x509_get_raw_data (X509 *x509, BIO *bio, MonoBtlsX509Format format)
{
	switch (format) {
		case MONO_BTLS_X509_FORMAT_DER:
			return i2d_X509_bio (bio, x509);
		case MONO_BTLS_X509_FORMAT_PEM:
			return PEM_write_bio_X509 (bio, x509);
		default:
			return 0;
	}
}

MONO_API int
mono_btls_x509_cmp (const X509 *a, const X509 *b)
{
	return X509_cmp (a, b);
}

MONO_API int
mono_btls_x509_get_hash (X509 *x509, const void **data)
{
	X509_check_purpose (x509, -1, 0);
	*data = x509->sha1_hash;
	return SHA_DIGEST_LENGTH;
}

MONO_API int64_t
mono_btls_x509_get_not_before (X509 *x509)
{
	return mono_btls_util_asn1_time_to_ticks (X509_get_notBefore (x509));
}

MONO_API int64_t
mono_btls_x509_get_not_after (X509 *x509)
{
	return mono_btls_util_asn1_time_to_ticks (X509_get_notAfter (x509));
}

MONO_API int
mono_btls_x509_get_public_key (X509 *x509, BIO *bio)
{
	EVP_PKEY *pkey;
	uint8_t *data = NULL;
	int ret;

	pkey = X509_get_pubkey (x509);
	if (!pkey)
		return -1;

	ret = i2d_PublicKey (pkey, &data);

	if (ret > 0 && data) {
		ret = BIO_write (bio, data, ret);
		OPENSSL_free (data);
	}

	EVP_PKEY_free (pkey);
	return ret;
}

MONO_API int
mono_btls_x509_get_serial_number (X509 *x509, char *buffer, int size, int mono_style)
{
	ASN1_INTEGER *serial;
	unsigned char *temp, *p;
	int len, idx;

	serial = X509_get_serialNumber (x509);
	if (serial->length == 0 || serial->length+1 > size)
		return 0;

	if (!mono_style) {
		memcpy (buffer, serial->data, serial->length);
		return serial->length;
	}

	temp = OPENSSL_malloc (serial->length + 1);
	if (!temp)
		return 0;

	p = temp;
	len = i2c_ASN1_INTEGER (serial, &p);

	if (!len) {
		OPENSSL_free (temp);
		return 0;
	}

	for (idx = 0; idx < len; idx++) {
		buffer [idx] = *(--p);
	}
	buffer [len] = 0;

	OPENSSL_free (temp);
	return len;
}

MONO_API int
mono_btls_x509_get_public_key_algorithm (X509 *x509, char *buffer, int size)
{
	X509_PUBKEY *pkey;
	ASN1_OBJECT *ppkalg;
	int ret;

	*buffer = 0;
	pkey = X509_get_X509_PUBKEY (x509);
	if (!pkey)
		return 0;

	ret = X509_PUBKEY_get0_param (&ppkalg, NULL, NULL, NULL, pkey);
	if (!ret || !ppkalg)
		return ret;

	return OBJ_obj2txt (buffer, size, ppkalg, 1);
}

MONO_API int
mono_btls_x509_get_version (X509 *x509)
{
	return (int)X509_get_version (x509) + 1;
}

MONO_API int
mono_btls_x509_get_signature_algorithm (X509 *x509, char *buffer, int size)
{
	const ASN1_OBJECT *obj;
	int nid;

	*buffer = 0;

	nid = X509_get_signature_nid (x509);

	obj = OBJ_nid2obj (nid);
	if (!obj)
		return 0;

	return OBJ_obj2txt (buffer, size, obj, 1);
}

MONO_API int
mono_btls_x509_get_public_key_asn1 (X509 *x509, char *out_oid, int oid_len, uint8_t **buffer, int *size)
{
	X509_PUBKEY *pkey;
	ASN1_OBJECT *ppkalg;
	const unsigned char *pk;
	int pk_len;
	int ret;

	if (out_oid)
		*out_oid = 0;

	pkey = X509_get_X509_PUBKEY (x509);
	if (!pkey || !pkey->public_key)
		return 0;

	ret = X509_PUBKEY_get0_param (&ppkalg, &pk, &pk_len, NULL, pkey);
	if (ret != 1 || !ppkalg || !pk)
		return 0;

	if (out_oid) {
		OBJ_obj2txt (out_oid, oid_len, ppkalg, 1);
	}

	if (buffer) {
		*size = pk_len;
		*buffer = OPENSSL_malloc (pk_len);
		if (!*buffer)
			return 0;

		memcpy (*buffer, pk, pk_len);
	}

	return 1;

}

MONO_API int
mono_btls_x509_get_public_key_parameters (X509 *x509, char *out_oid, int oid_len, uint8_t **buffer, int *size)
{
	X509_PUBKEY *pkey;
	X509_ALGOR *algor;
	ASN1_OBJECT *paobj;
	int ptype;
	void *pval;
	int ret;

	if (out_oid)
		*out_oid = 0;

	pkey = X509_get_X509_PUBKEY (x509);

	ret = X509_PUBKEY_get0_param (NULL, NULL, NULL, &algor, pkey);
	if (ret != 1 || !algor)
		return 0;

	X509_ALGOR_get0 (&paobj, &ptype, &pval, algor);

	if (ptype != V_ASN1_NULL && ptype != V_ASN1_SEQUENCE)
		return 0;

	if (ptype == V_ASN1_NULL) {
		uint8_t *ptr;

		*size = 2;
		*buffer = OPENSSL_malloc (2);
		if (!*buffer)
			return 0;

		ptr = *buffer;
		*ptr++ = 0x05;
		*ptr++ = 0x00;

		if (out_oid)
			OBJ_obj2txt (out_oid, oid_len, paobj, 1);

		return 1;
	} else if (ptype == V_ASN1_SEQUENCE) {
		ASN1_STRING *pstr = pval;

		*size = pstr->length;
		*buffer = OPENSSL_malloc (pstr->length);
		if (!*buffer)
			return 0;

		memcpy (*buffer, pstr->data, pstr->length);

		if (out_oid)
			OBJ_obj2txt (out_oid, oid_len, paobj, 1);

		return 1;
	} else {
		return 0;
	}
}

MONO_API EVP_PKEY *
mono_btls_x509_get_pubkey (X509 *x509)
{
	return X509_get_pubkey (x509);
}

MONO_API int
mono_btls_x509_get_subject_key_identifier (X509 *x509, uint8_t **buffer, int *size)
{
	ASN1_OCTET_STRING *skid;

	*size = 0;
	*buffer = NULL;

	if (X509_get_version (x509) != 2)
		return 0;

	skid = X509_get_ext_d2i (x509, NID_subject_key_identifier, NULL, NULL);
	if (!skid)
		return 0;

	*size = skid->length;
	*buffer = OPENSSL_malloc (*size);
	if (!*buffer)
		return 0;

	memcpy (*buffer, skid->data, *size);
	return 1;
}

MONO_API int
mono_btls_x509_print (X509 *x509, BIO *bio)
{
	return X509_print_ex (bio, x509, XN_FLAG_COMPAT, X509_FLAG_COMPAT);
}

static int
get_trust_nid (MonoBtlsX509Purpose purpose)
{
	switch (purpose) {
		case MONO_BTLS_X509_PURPOSE_SSL_CLIENT:
			return NID_client_auth;
		case MONO_BTLS_X509_PURPOSE_SSL_SERVER:
			return NID_server_auth;
		default:
			return 0;
	}
}

MONO_API int
mono_btls_x509_add_trust_object (X509 *x509, MonoBtlsX509Purpose purpose)
{
	ASN1_OBJECT *trust;
	int nid;

	nid = get_trust_nid (purpose);
	if (!nid)
		return 0;

	trust = ASN1_OBJECT_new ();
	if (!trust)
		return 0;

	trust->nid = nid;
	return X509_add1_trust_object (x509, trust);
}

MONO_API int
mono_btls_x509_add_reject_object (X509 *x509, MonoBtlsX509Purpose purpose)
{
	ASN1_OBJECT *reject;
	int nid;

	nid = get_trust_nid (purpose);
	if (!nid)
		return 0;

	reject = ASN1_OBJECT_new ();
	if (!reject)
		return 0;

	reject->nid = nid;
	return X509_add1_reject_object (x509, reject);
}

MONO_API int
mono_btls_x509_add_explicit_trust (X509 *x509, MonoBtlsX509TrustKind kind)
{
	int ret = 0;

	if ((kind & MONO_BTLS_X509_TRUST_KIND_REJECT_ALL) != 0)
		kind |= MONO_BTLS_X509_TRUST_KIND_REJECT_CLIENT | MONO_BTLS_X509_TRUST_KIND_REJECT_SERVER;

	if ((kind & MONO_BTLS_X509_TRUST_KIND_TRUST_ALL) != 0)
		kind |= MONO_BTLS_X509_TRUST_KIND_TRUST_CLIENT | MONO_BTLS_X509_TRUST_KIND_TRUST_SERVER;


	if ((kind & MONO_BTLS_X509_TRUST_KIND_REJECT_CLIENT) != 0) {
		ret = mono_btls_x509_add_reject_object (x509, MONO_BTLS_X509_PURPOSE_SSL_CLIENT);
		if (!ret)
			return ret;
	}

	if ((kind & MONO_BTLS_X509_TRUST_KIND_REJECT_SERVER) != 0) {
		ret = mono_btls_x509_add_reject_object (x509, MONO_BTLS_X509_PURPOSE_SSL_SERVER);
		if (!ret)
			return ret;
	}

	if (ret) {
		// Ignore any MONO_BTLS_X509_TRUST_KIND_TRUST_* settings if we added
		// any kind of MONO_BTLS_X509_TRUST_KIND_REJECT_* before.
		return ret;
	}

	if ((kind & MONO_BTLS_X509_TRUST_KIND_TRUST_CLIENT) != 0) {
		ret = mono_btls_x509_add_trust_object (x509, MONO_BTLS_X509_PURPOSE_SSL_CLIENT);
		if (!ret)
			return ret;
	}

	if ((kind & MONO_BTLS_X509_TRUST_KIND_TRUST_SERVER) != 0) {
		ret = mono_btls_x509_add_trust_object (x509, MONO_BTLS_X509_PURPOSE_SSL_SERVER);
		if (!ret)
			return ret;
	}

	return ret;
}
