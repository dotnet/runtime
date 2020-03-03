//
//  btls-x509-name.c
//  MonoBtls
//
//  Created by Martin Baulig on 3/5/16.
//  Copyright © 2016 Xamarin. All rights reserved.
//

#include "btls-x509-name.h"

struct MonoBtlsX509Name {
	int owns;
	X509_NAME *name;
};

MonoBtlsX509Name *
mono_btls_x509_name_from_name (X509_NAME *xn)
{
	MonoBtlsX509Name *name;

	name = OPENSSL_malloc (sizeof (MonoBtlsX509Name));
	if (!name)
		return NULL;

	memset(name, 0, sizeof(MonoBtlsX509Name));
	name->name = xn;
	return name;
}

MonoBtlsX509Name *
mono_btls_x509_name_copy (X509_NAME *xn)
{
	MonoBtlsX509Name *name;

	name = OPENSSL_malloc (sizeof (MonoBtlsX509Name));
	if (!name)
		return NULL;

	memset(name, 0, sizeof(MonoBtlsX509Name));
	name->name = X509_NAME_dup(xn);
	name->owns = 1;
	return name;
}

void
mono_btls_x509_name_free (MonoBtlsX509Name *name)
{
	if (name->owns) {
		if (name->name) {
			X509_NAME_free(name->name);
			name->name = NULL;
		}
	}
	OPENSSL_free(name);
}

X509_NAME *
mono_btls_x509_name_peek_name (MonoBtlsX509Name *name)
{
	return name->name;
}

int
mono_btls_x509_name_print_bio (MonoBtlsX509Name *name, BIO *bio)
{
	return X509_NAME_print_ex (bio, name->name, 0, ASN1_STRFLGS_RFC2253 | XN_FLAG_FN_SN | XN_FLAG_SEP_CPLUS_SPC | XN_FLAG_DN_REV);
}

int
mono_btls_x509_name_get_raw_data (MonoBtlsX509Name *name, void **buffer, int use_canon_enc)
{
	int len;
	void *ptr;

	if (use_canon_enc) {
		// make sure canon_enc is initialized.
		i2d_X509_NAME (name->name, NULL);

		len = name->name->canon_enclen;
		ptr = name->name->canon_enc;
	} else {
		len = (int)name->name->bytes->length;
		ptr = name->name->bytes->data;
	}

	*buffer = OPENSSL_malloc (len);
	if (!*buffer)
		return 0;

	memcpy (*buffer, ptr, len);
	return len;
}

MonoBtlsX509Name *
mono_btls_x509_name_from_data (const void *data, int len, int use_canon_enc)
{
	MonoBtlsX509Name *name;
	uint8_t *buf;
	const unsigned char *ptr;
	X509_NAME *ret;

	name = OPENSSL_malloc (sizeof (MonoBtlsX509Name));
	if (!name)
		return NULL;

	memset (name, 0, sizeof(MonoBtlsX509Name));
	name->owns = 1;

	name->name = X509_NAME_new ();
	if (!name->name) {
		OPENSSL_free (name);
		return NULL;
	}

	if (use_canon_enc) {
		CBB cbb, contents;
		size_t buf_len;

		// re-add ASN1 SEQUENCE header.
		CBB_init(&cbb, 0);
		if (!CBB_add_asn1(&cbb, &contents, 0x30) ||
		    !CBB_add_bytes(&contents, data, len) ||
		    !CBB_finish(&cbb, &buf, &buf_len)) {
			CBB_cleanup (&cbb);
			mono_btls_x509_name_free (name);
			return NULL;
		}

		ptr = buf;
		len = (int)buf_len;
	} else {
		ptr = data;
		buf = NULL;
	}

	ret = d2i_X509_NAME (&name->name, &ptr, len);

	if (buf)
		OPENSSL_free (buf);

	if (ret != name->name) {
		mono_btls_x509_name_free (name);
		return NULL;
	}

	return name;
}

int
mono_btls_x509_name_print_string (MonoBtlsX509Name *name, char *buffer, int size)
{
	*buffer = 0;
	return X509_NAME_oneline (name->name, buffer, size) != NULL;
}

int64_t
mono_btls_x509_name_hash (MonoBtlsX509Name *name)
{
	return X509_NAME_hash (name->name);
}

int64_t
mono_btls_x509_name_hash_old (MonoBtlsX509Name *name)
{
	return X509_NAME_hash_old (name->name);
}

int
mono_btls_x509_name_get_entry_count (MonoBtlsX509Name *name)
{
	return X509_NAME_entry_count (name->name);
}

static MonoBtlsX509NameEntryType
nid2mono (int nid)
{
	switch (nid) {
	case NID_countryName:
		return MONO_BTLS_X509_NAME_ENTRY_TYPE_COUNTRY_NAME;
	case NID_organizationName:
		return MONO_BTLS_X509_NAME_ENTRY_TYPE_ORGANIZATION_NAME;
	case NID_organizationalUnitName:
		return MONO_BTLS_X509_NAME_ENTRY_TYPE_ORGANIZATIONAL_UNIT_NAME;
	case NID_commonName:
		return MONO_BTLS_X509_NAME_ENTRY_TYPE_COMMON_NAME;
	case NID_localityName:
		return MONO_BTLS_X509_NAME_ENTRY_TYPE_LOCALITY_NAME;
	case NID_stateOrProvinceName:
		return MONO_BTLS_X509_NAME_ENTRY_TYPE_STATE_OR_PROVINCE_NAME;
	case NID_streetAddress:
		return MONO_BTLS_X509_NAME_ENTRY_TYPE_STREET_ADDRESS;
	case NID_serialNumber:
		return MONO_BTLS_X509_NAME_ENTRY_TYPE_SERIAL_NUMBER;
	case NID_domainComponent:
		return MONO_BTLS_X509_NAME_ENTRY_TYPE_DOMAIN_COMPONENT;
	case NID_userId:
		return MONO_BTLS_X509_NAME_ENTRY_TYPE_USER_ID;
	case NID_dnQualifier:
		return MONO_BTLS_X509_NAME_ENTRY_TYPE_DN_QUALIFIER;
	case NID_title:
		return MONO_BTLS_X509_NAME_ENTRY_TYPE_TITLE;
	case NID_surname:
		return MONO_BTLS_X509_NAME_ENTRY_TYPE_SURNAME;
	case NID_givenName:
		return MONO_BTLS_X509_NAME_ENTRY_TYPE_GIVEN_NAME;
	case NID_initials:
		return MONO_BTLS_X509_NAME_ENTRY_TYPE_INITIAL;
	default:
		return MONO_BTLS_X509_NAME_ENTRY_TYPE_UNKNOWN;
	}
}

MonoBtlsX509NameEntryType
mono_btls_x509_name_get_entry_type (MonoBtlsX509Name *name, int index)
{
	X509_NAME_ENTRY *entry;
	ASN1_OBJECT *obj;

	if (index >= X509_NAME_entry_count (name->name))
		return -1;

	entry = X509_NAME_get_entry (name->name, index);
	if (!entry)
		return -1;

	obj = X509_NAME_ENTRY_get_object (entry);
	if (!obj)
		return -1;

	return nid2mono (OBJ_obj2nid (obj));
}

int
mono_btls_x509_name_get_entry_oid (MonoBtlsX509Name *name, int index, char *buffer, int size)
{
	X509_NAME_ENTRY *entry;
	ASN1_OBJECT *obj;

	if (index >= X509_NAME_entry_count (name->name))
		return 0;

	entry = X509_NAME_get_entry (name->name, index);
	if (!entry)
		return 0;

	obj = X509_NAME_ENTRY_get_object (entry);
	if (!obj)
		return 0;

	return OBJ_obj2txt (buffer, size, obj, 1);
}

int
mono_btls_x509_name_get_entry_oid_data (MonoBtlsX509Name *name, int index, const void **data)
{
	X509_NAME_ENTRY *entry;
	ASN1_OBJECT *obj;

	if (index >= X509_NAME_entry_count (name->name))
		return -1;

	entry = X509_NAME_get_entry (name->name, index);
	if (!entry)
		return -1;

	obj = X509_NAME_ENTRY_get_object (entry);
	if (!obj)
		return -1;

	*data = obj->data;
	return obj->length;
}

int
mono_btls_x509_name_get_entry_value (MonoBtlsX509Name *name, int index, int *tag, unsigned char **str)
{
	X509_NAME_ENTRY *entry;
	ASN1_STRING *data;

	*str = NULL;
	*tag = 0;

	if (index >= X509_NAME_entry_count (name->name))
		return 0;

	entry = X509_NAME_get_entry (name->name, index);
	if (!entry)
		return 0;

	data = X509_NAME_ENTRY_get_data (entry);
	if (!data)
		return 0;

	*tag = data->type;
	return ASN1_STRING_to_UTF8 (str, data);
}
