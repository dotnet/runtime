/**
 * \file
 * Custom attributes.
 * 
 * Author:
 *   Paolo Molaro (lupus@ximian.com)
 *
 * Copyright 2001-2003 Ximian, Inc (http://www.ximian.com)
 * Copyright 2004-2009 Novell, Inc (http://www.novell.com)
 * Copyright 2011 Rodrigo Kumpera
 * Copyright 2016 Microsoft
 *
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */
#include <config.h>
#include "mono/metadata/assembly.h"
#include "mono/metadata/gc-internals.h"
#include "mono/metadata/mono-endian.h"
#include "mono/metadata/object-internals.h"
#include "mono/metadata/custom-attrs-internals.h"
#include "mono/metadata/sre-internals.h"
#include "mono/metadata/reflection-internals.h"
#include "mono/metadata/tabledefs.h"
#include "mono/metadata/tokentype.h"
#include "mono/metadata/verify-internals.h"
#include "mono/utils/checked-build.h"


#define CHECK_ADD4_OVERFLOW_UN(a, b) ((guint32)(0xFFFFFFFFU) - (guint32)(b) < (guint32)(a))
#define CHECK_ADD8_OVERFLOW_UN(a, b) ((guint64)(0xFFFFFFFFFFFFFFFFUL) - (guint64)(b) < (guint64)(a))

#if SIZEOF_VOID_P == 4
#define CHECK_ADDP_OVERFLOW_UN(a,b) CHECK_ADD4_OVERFLOW_UN(a, b)
#else
#define CHECK_ADDP_OVERFLOW_UN(a,b) CHECK_ADD8_OVERFLOW_UN(a, b)
#endif

#define ADDP_IS_GREATER_OR_OVF(a, b, c) (((a) + (b) > (c)) || CHECK_ADDP_OVERFLOW_UN (a, b))
#define ADD_IS_GREATER_OR_OVF(a, b, c) (((a) + (b) > (c)) || CHECK_ADD4_OVERFLOW_UN (a, b))

static gboolean type_is_reference (MonoType *type);

static GENERATE_GET_CLASS_WITH_CACHE (custom_attribute_typed_argument, "System.Reflection", "CustomAttributeTypedArgument");
static GENERATE_GET_CLASS_WITH_CACHE (custom_attribute_named_argument, "System.Reflection", "CustomAttributeNamedArgument");

static MonoCustomAttrInfo*
mono_custom_attrs_from_builders_handle (MonoImage *alloc_img, MonoImage *image, MonoArrayHandle cattrs);

static gboolean
bcheck_blob (const char *ptr, int bump, const char *endp, MonoError *error);

static gboolean
decode_blob_value_checked (const char *ptr, const char *endp, guint32 *size_out, const char **retp, MonoError *error);

/*
 * LOCKING: Acquires the loader lock. 
 */
static MonoCustomAttrInfo*
lookup_custom_attr (MonoImage *image, gpointer member)
{
	MONO_REQ_GC_NEUTRAL_MODE;

	MonoCustomAttrInfo* res;

	res = (MonoCustomAttrInfo *)mono_image_property_lookup (image, member, MONO_PROP_DYNAMIC_CATTR);

	if (!res)
		return NULL;

	res = (MonoCustomAttrInfo *)g_memdup (res, MONO_SIZEOF_CUSTOM_ATTR_INFO + sizeof (MonoCustomAttrEntry) * res->num_attrs);
	res->cached = 0;
	return res;
}

static gboolean
custom_attr_visible (MonoImage *image, MonoReflectionCustomAttr *cattr)
{
	MONO_REQ_GC_UNSAFE_MODE;

	/* FIXME: Need to do more checks */
	if (cattr->ctor->method && (cattr->ctor->method->klass->image != image)) {
		int visibility = mono_class_get_flags (cattr->ctor->method->klass) & TYPE_ATTRIBUTE_VISIBILITY_MASK;

		if ((visibility != TYPE_ATTRIBUTE_PUBLIC) && (visibility != TYPE_ATTRIBUTE_NESTED_PUBLIC))
			return FALSE;
	}

	return TRUE;
}

static gboolean
type_is_reference (MonoType *type)
{
	switch (type->type) {
	case MONO_TYPE_BOOLEAN:
	case MONO_TYPE_CHAR:
	case MONO_TYPE_U:
	case MONO_TYPE_I:
	case MONO_TYPE_U1:
	case MONO_TYPE_I1:
	case MONO_TYPE_U2:
	case MONO_TYPE_I2:
	case MONO_TYPE_U4:
	case MONO_TYPE_I4:
	case MONO_TYPE_U8:
	case MONO_TYPE_I8:
	case MONO_TYPE_R8:
	case MONO_TYPE_R4:
	case MONO_TYPE_VALUETYPE:
		return FALSE;
	default:
		return TRUE;
	}
}

static void
free_param_data (MonoMethodSignature *sig, void **params) {
	int i;
	for (i = 0; i < sig->param_count; ++i) {
		if (!type_is_reference (sig->params [i]))
			g_free (params [i]);
	}
}

/*
 * Find the field index in the metadata FieldDef table.
 */
static guint32
find_field_index (MonoClass *klass, MonoClassField *field) {
	int i;

	int fcount = mono_class_get_field_count (klass);
	for (i = 0; i < fcount; ++i) {
		if (field == &klass->fields [i])
			return mono_class_get_first_field_idx (klass) + 1 + i;
	}
	return 0;
}

/*
 * Find the property index in the metadata Property table.
 */
static guint32
find_property_index (MonoClass *klass, MonoProperty *property)
{
	int i;
	MonoClassPropertyInfo *info = mono_class_get_property_info (klass);

	for (i = 0; i < info->count; ++i) {
		if (property == &info->properties [i])
			return info->first + 1 + i;
	}
	return 0;
}

/*
 * Find the event index in the metadata Event table.
 */
static guint32
find_event_index (MonoClass *klass, MonoEvent *event)
{
	int i;
	MonoClassEventInfo *info = mono_class_get_event_info (klass);

	for (i = 0; i < info->count; ++i) {
		if (event == &info->events [i])
			return info->first + 1 + i;
	}
	return 0;
}

/*
 * Load the type with name @n on behalf of image @image.  On failure sets @error and returns NULL.
 * The @is_enum flag only affects the error message that's displayed on failure.
 */
static MonoType*
cattr_type_from_name (char *n, MonoImage *image, gboolean is_enum, MonoError *error)
{
	MonoError inner_error;
	MonoType *t = mono_reflection_type_from_name_checked (n, image, &inner_error);
	if (!t) {
		mono_error_set_type_load_name (error, g_strdup(n), NULL,
					       "Could not load %s %s while decoding custom attribute: %s",
					       is_enum ? "enum type": "type",
					       n,
					       mono_error_get_message (&inner_error));
		mono_error_cleanup (&inner_error);
		return NULL;
	}
	return t;
}

static MonoClass*
load_cattr_enum_type (MonoImage *image, const char *p, const char *boundp, const char **end, MonoError *error)
{
	char *n;
	MonoType *t;
	guint32 slen;
	error_init (error);

	if (!decode_blob_value_checked (p, boundp, &slen, &p, error))
		return NULL;

	if (boundp && slen > 0 && !bcheck_blob (p, slen - 1, boundp, error))
		return NULL;
	n = (char *)g_memdup (p, slen + 1);
	n [slen] = 0;
	t = cattr_type_from_name (n, image, TRUE, error);
	g_free (n);
	return_val_if_nok (error, NULL);
	p += slen;
	*end = p;
	return mono_class_from_mono_type (t);
}

static void*
load_cattr_value (MonoImage *image, MonoType *t, const char *p, const char *boundp, const char **end, MonoError *error)
{
	int type = t->type;
	guint32 slen;
	MonoClass *tklass = t->data.klass;

	g_assert (boundp);
	error_init (error);

handle_enum:
	switch (type) {
	case MONO_TYPE_U1:
	case MONO_TYPE_I1:
	case MONO_TYPE_BOOLEAN: {
		MonoBoolean *bval = (MonoBoolean *)g_malloc (sizeof (MonoBoolean));
		if (!bcheck_blob (p, 0, boundp, error))
			return NULL;
		*bval = *p;
		*end = p + 1;
		return bval;
	}
	case MONO_TYPE_CHAR:
	case MONO_TYPE_U2:
	case MONO_TYPE_I2: {
		guint16 *val = (guint16 *)g_malloc (sizeof (guint16));
		if (!bcheck_blob (p, 1, boundp, error))
			return NULL;
		*val = read16 (p);
		*end = p + 2;
		return val;
	}
#if SIZEOF_VOID_P == 4
	case MONO_TYPE_U:
	case MONO_TYPE_I:
#endif
	case MONO_TYPE_R4:
	case MONO_TYPE_U4:
	case MONO_TYPE_I4: {
		guint32 *val = (guint32 *)g_malloc (sizeof (guint32));
		if (!bcheck_blob (p, 3, boundp, error))
			return NULL;
		*val = read32 (p);
		*end = p + 4;
		return val;
	}
#if SIZEOF_VOID_P == 8
	case MONO_TYPE_U: /* error out instead? this should probably not happen */
	case MONO_TYPE_I:
#endif
	case MONO_TYPE_U8:
	case MONO_TYPE_I8: {
		guint64 *val = (guint64 *)g_malloc (sizeof (guint64));
		if (!bcheck_blob (p, 7, boundp, error))
			return NULL;
		*val = read64 (p);
		*end = p + 8;
		return val;
	}
	case MONO_TYPE_R8: {
		double *val = (double *)g_malloc (sizeof (double));
		if (!bcheck_blob (p, 7, boundp, error))
			return NULL;
		readr8 (p, val);
		*end = p + 8;
		return val;
	}
	case MONO_TYPE_VALUETYPE:
		if (t->data.klass->enumtype) {
			type = mono_class_enum_basetype (t->data.klass)->type;
			goto handle_enum;
		} else {
			MonoClass *k =  t->data.klass;
			
			if (mono_is_corlib_image (k->image) && strcmp (k->name_space, "System") == 0 && strcmp (k->name, "DateTime") == 0){
				guint64 *val = (guint64 *)g_malloc (sizeof (guint64));
				if (!bcheck_blob (p, 7, boundp, error))
					return NULL;
				*val = read64 (p);
				*end = p + 8;
				return val;
			}
		}
		g_error ("generic valutype %s not handled in custom attr value decoding", t->data.klass->name);
		break;
		
	case MONO_TYPE_STRING:
		if (!bcheck_blob (p, 0, boundp, error))
			return NULL;
		if (*p == (char)0xFF) {
			*end = p + 1;
			return NULL;
		}
		if (!decode_blob_value_checked (p, boundp, &slen, &p, error))
			return NULL;
		if (slen > 0 && !bcheck_blob (p, slen - 1, boundp, error))
			return NULL;
		*end = p + slen;
		return mono_string_new_len_checked (mono_domain_get (), p, slen, error);
	case MONO_TYPE_CLASS: {
		MonoReflectionType *rt;
		char *n;
		MonoType *t;
		if (!bcheck_blob (p, 0, boundp, error))
			return NULL;
		if (*p == (char)0xFF) {
			*end = p + 1;
			return NULL;
		}
handle_type:
		if (!decode_blob_value_checked (p, boundp, &slen, &p, error))
			return NULL;
		if (slen > 0 && !bcheck_blob (p, slen - 1, boundp, error))
			return NULL;
		n = (char *)g_memdup (p, slen + 1);
		n [slen] = 0;
		t = cattr_type_from_name (n, image, FALSE, error);
		g_free (n);
		return_val_if_nok (error, NULL);
		*end = p + slen;

		rt = mono_type_get_object_checked (mono_domain_get (), t, error);
		if (!mono_error_ok (error))
			return NULL;

		return rt;
	}
	case MONO_TYPE_OBJECT: {
		if (!bcheck_blob (p, 0, boundp, error))
			return NULL;
		char subt = *p++;
		MonoObject *obj;
		MonoClass *subc = NULL;
		void *val;

		if (subt == 0x50) {
			goto handle_type;
		} else if (subt == 0x0E) {
			type = MONO_TYPE_STRING;
			goto handle_enum;
		} else if (subt == 0x1D) {
			MonoType simple_type = {{0}};
			if (!bcheck_blob (p, 0, boundp, error))
				return NULL;
			int etype = *p;
			p ++;

			type = MONO_TYPE_SZARRAY;
			if (etype == 0x50) {
				tklass = mono_defaults.systemtype_class;
			} else if (etype == 0x55) {
				tklass = load_cattr_enum_type (image, p, boundp, &p, error);
				if (!is_ok (error))
					return NULL;
			} else {
				if (etype == 0x51)
					/* See Partition II, Appendix B3 */
					etype = MONO_TYPE_OBJECT;
				simple_type.type = (MonoTypeEnum)etype;
				tklass = mono_class_from_mono_type (&simple_type);
			}
			goto handle_enum;
		} else if (subt == 0x55) {
			char *n;
			MonoType *t;
			if (!decode_blob_value_checked (p, boundp, &slen, &p, error))
				return NULL;
			if (slen > 0 && !bcheck_blob (p, slen - 1, boundp, error))
				return NULL;
			n = (char *)g_memdup (p, slen + 1);
			n [slen] = 0;
			t = cattr_type_from_name (n, image, FALSE, error);
			g_free (n);
			return_val_if_nok (error, NULL);
			p += slen;
			subc = mono_class_from_mono_type (t);
		} else if (subt >= MONO_TYPE_BOOLEAN && subt <= MONO_TYPE_R8) {
			MonoType simple_type = {{0}};
			simple_type.type = (MonoTypeEnum)subt;
			subc = mono_class_from_mono_type (&simple_type);
		} else {
			g_error ("Unknown type 0x%02x for object type encoding in custom attr", subt);
		}
		val = load_cattr_value (image, &subc->byval_arg, p, boundp, end, error);
		obj = NULL;
		if (is_ok (error)) {
			obj = mono_object_new_checked (mono_domain_get (), subc, error);
			g_assert (!subc->has_references);
			if (is_ok (error))
				mono_gc_memmove_atomic ((char*)obj + sizeof (MonoObject), val, mono_class_value_size (subc, NULL));
		}

		g_free (val);
		return obj;
	}
	case MONO_TYPE_SZARRAY: {
		MonoArray *arr;
		guint32 i, alen, basetype;
		if (!bcheck_blob (p, 3, boundp, error))
			return NULL;
		alen = read32 (p);
		p += 4;
		if (alen == 0xffffffff) {
			*end = p;
			return NULL;
		}
		arr = mono_array_new_checked (mono_domain_get(), tklass, alen, error);
		return_val_if_nok (error, NULL);
		basetype = tklass->byval_arg.type;
		if (basetype == MONO_TYPE_VALUETYPE && tklass->enumtype)
			basetype = mono_class_enum_basetype (tklass)->type;
		switch (basetype)
		{
			case MONO_TYPE_U1:
			case MONO_TYPE_I1:
			case MONO_TYPE_BOOLEAN:
				for (i = 0; i < alen; i++) {
					if (!bcheck_blob (p, 0, boundp, error))
						return NULL;
					MonoBoolean val = *p++;
					mono_array_set (arr, MonoBoolean, i, val);
				}
				break;
			case MONO_TYPE_CHAR:
			case MONO_TYPE_U2:
			case MONO_TYPE_I2:
				for (i = 0; i < alen; i++) {
					if (!bcheck_blob (p, 1, boundp, error))
						return NULL;
					guint16 val = read16 (p);
					mono_array_set (arr, guint16, i, val);
					p += 2;
				}
				break;
			case MONO_TYPE_R4:
			case MONO_TYPE_U4:
			case MONO_TYPE_I4:
				for (i = 0; i < alen; i++) {
					if (!bcheck_blob (p, 3, boundp, error))
						return NULL;
					guint32 val = read32 (p);
					mono_array_set (arr, guint32, i, val);
					p += 4;
				}
				break;
			case MONO_TYPE_R8:
				for (i = 0; i < alen; i++) {
					if (!bcheck_blob (p, 7, boundp, error))
						return NULL;
					double val;
					readr8 (p, &val);
					mono_array_set (arr, double, i, val);
					p += 8;
				}
				break;
			case MONO_TYPE_U8:
			case MONO_TYPE_I8:
				for (i = 0; i < alen; i++) {
					if (!bcheck_blob (p, 7, boundp, error))
						return NULL;
					guint64 val = read64 (p);
					mono_array_set (arr, guint64, i, val);
					p += 8;
				}
				break;
			case MONO_TYPE_CLASS:
			case MONO_TYPE_OBJECT:
			case MONO_TYPE_STRING:
			case MONO_TYPE_SZARRAY:
				for (i = 0; i < alen; i++) {
					MonoObject *item = (MonoObject *)load_cattr_value (image, &tklass->byval_arg, p, boundp, &p, error);
					if (!is_ok (error))
						return NULL;
					mono_array_setref (arr, i, item);
				}
				break;
			default:
				g_error ("Type 0x%02x not handled in custom attr array decoding", basetype);
		}
		*end=p;
		return arr;
	}
	default:
		g_error ("Type 0x%02x not handled in custom attr value decoding", type);
	}
	return NULL;
}

static MonoObject*
load_cattr_value_boxed (MonoDomain *domain, MonoImage *image, MonoType *t, const char* p, const char *boundp, const char** end, MonoError *error)
{
	error_init (error);

	gboolean is_ref = type_is_reference (t);

	void *val = load_cattr_value (image, t, p, boundp, end, error);
	if (!is_ok (error)) {
		if (is_ref)
			g_free (val);
		return NULL;
	}

	if (is_ref)
		return (MonoObject*)val;

	MonoObject *boxed = mono_value_box_checked (domain, mono_class_from_mono_type (t), val, error);
	g_free (val);
	return boxed;
}

static MonoObject*
create_cattr_typed_arg (MonoType *t, MonoObject *val, MonoError *error)
{
	static MonoMethod *ctor;
	MonoObject *retval;
	void *params [2], *unboxed;

	error_init (error);

	if (!ctor)
		ctor = mono_class_get_method_from_name (mono_class_get_custom_attribute_typed_argument_class (), ".ctor", 2);
	
	params [0] = mono_type_get_object_checked (mono_domain_get (), t, error);
	return_val_if_nok (error, NULL);

	params [1] = val;
	retval = mono_object_new_checked (mono_domain_get (), mono_class_get_custom_attribute_typed_argument_class (), error);
	return_val_if_nok (error, NULL);
	unboxed = mono_object_unbox (retval);

	mono_runtime_invoke_checked (ctor, unboxed, params, error);
	return_val_if_nok (error, NULL);

	return retval;
}

static MonoObject*
create_cattr_named_arg (void *minfo, MonoObject *typedarg, MonoError *error)
{
	static MonoMethod *ctor;
	MonoObject *retval;
	void *unboxed, *params [2];

	error_init (error);

	if (!ctor)
		ctor = mono_class_get_method_from_name (mono_class_get_custom_attribute_named_argument_class (), ".ctor", 2);

	params [0] = minfo;
	params [1] = typedarg;
	retval = mono_object_new_checked (mono_domain_get (), mono_class_get_custom_attribute_named_argument_class (), error);
	return_val_if_nok (error, NULL);

	unboxed = mono_object_unbox (retval);

	mono_runtime_invoke_checked (ctor, unboxed, params, error);
	return_val_if_nok (error, NULL);

	return retval;
}


static MonoCustomAttrInfo*
mono_custom_attrs_from_builders_handle (MonoImage *alloc_img, MonoImage *image, MonoArrayHandle cattrs)
{
	return mono_custom_attrs_from_builders (alloc_img, image, MONO_HANDLE_RAW (cattrs)); /* FIXME use coop handles for mono_custom_attrs_from_builders */
}

MonoCustomAttrInfo*
mono_custom_attrs_from_builders (MonoImage *alloc_img, MonoImage *image, MonoArray *cattrs)
{
	MONO_REQ_GC_UNSAFE_MODE;

	int i, index, count, not_visible;
	MonoCustomAttrInfo *ainfo;
	MonoReflectionCustomAttr *cattr;

	if (!cattrs)
		return NULL;
	/* FIXME: check in assembly the Run flag is set */

	count = mono_array_length (cattrs);

	/* Skip nonpublic attributes since MS.NET seems to do the same */
	/* FIXME: This needs to be done more globally */
	not_visible = 0;
	for (i = 0; i < count; ++i) {
		cattr = (MonoReflectionCustomAttr*)mono_array_get (cattrs, gpointer, i);
		if (!custom_attr_visible (image, cattr))
			not_visible ++;
	}

	int num_attrs = count - not_visible;
	ainfo = (MonoCustomAttrInfo *)mono_image_g_malloc0 (alloc_img, MONO_SIZEOF_CUSTOM_ATTR_INFO + sizeof (MonoCustomAttrEntry) * num_attrs);

	ainfo->image = image;
	ainfo->num_attrs = num_attrs;
	ainfo->cached = alloc_img != NULL;
	index = 0;
	for (i = 0; i < count; ++i) {
		cattr = (MonoReflectionCustomAttr*)mono_array_get (cattrs, gpointer, i);
		if (custom_attr_visible (image, cattr)) {
			unsigned char *saved = (unsigned char *)mono_image_alloc (image, mono_array_length (cattr->data));
			memcpy (saved, mono_array_addr (cattr->data, char, 0), mono_array_length (cattr->data));
			ainfo->attrs [index].ctor = cattr->ctor->method;
			g_assert (cattr->ctor->method);
			ainfo->attrs [index].data = saved;
			ainfo->attrs [index].data_size = mono_array_length (cattr->data);
			index ++;
		}
	}
	g_assert (index == num_attrs && count == num_attrs + not_visible);

	return ainfo;
}

static void
set_custom_attr_fmt_error (MonoError *error)
{
	error_init (error);
	mono_error_set_generic_error (error, "System.Reflection", "CustomAttributeFormatException", "Binary format of the specified custom attribute was invalid.");
}

/**
 * bcheck_blob:
 * \param ptr a pointer into a blob
 * \param bump how far we plan on reading past \p ptr.
 * \param endp upper bound for \p ptr - one past the last valid value for \p ptr.
 * \param error set on error
 *
 * Check that ptr+bump is below endp.  Returns TRUE on success, or FALSE on
 * failure and sets \p error.
 */
static gboolean
bcheck_blob (const char *ptr, int bump, const char *endp, MonoError *error)
{
	error_init (error);
	if (ADDP_IS_GREATER_OR_OVF (ptr, bump, endp - 1)) {
		set_custom_attr_fmt_error (error);
		return FALSE;
	} else
		return TRUE;
}

/**
 * decode_blob_size_checked:
 * \param ptr a pointer into a blob
 * \param endp upper bound for \p ptr - one pas the last valid value for \p ptr
 * \param size_out on success set to the decoded size
 * \param retp on success set to the next byte after the encoded size
 * \param error set on error
 *
 * Decode an encoded size value which takes 1, 2, or 4 bytes and set \p
 * size_out to the decoded size and \p retp to the next byte after the encoded
 * size.  Returns TRUE on success, or FALASE on failure and sets \p error.
 */
static gboolean
decode_blob_size_checked (const char *ptr, const char *endp, guint32 *size_out, const char **retp, MonoError *error)
{
	error_init (error);
	if (endp && !bcheck_blob (ptr, 0, endp, error))
		goto leave;
	if ((*ptr & 0x80) != 0) {
		if ((*ptr & 0x40) == 0 && !bcheck_blob (ptr, 1, endp, error))
			goto leave;
		else if (!bcheck_blob (ptr, 3, endp, error))
			goto leave;
	}
	*size_out = mono_metadata_decode_blob_size (ptr, retp);
leave:
	return is_ok (error);
}

/**
 * decode_blob_value_checked:
 * \param ptr a pointer into a blob
 * \param endp upper bound for \p ptr - one pas the last valid value for \p ptr
 * \param value_out on success set to the decoded value
 * \param retp on success set to the next byte after the encoded size
 * \param error set on error
 *
 * Decode an encoded uint32 value which takes 1, 2, or 4 bytes and set \p
 * value_out to the decoded value and \p retp to the next byte after the
 * encoded value.  Returns TRUE on success, or FALASE on failure and sets \p
 * error.
 */
static gboolean
decode_blob_value_checked (const char *ptr, const char *endp, guint32 *value_out, const char **retp, MonoError *error)
{
	/* This similar to decode_blob_size_checked, above but delegates to
	 * mono_metadata_decode_value which is semantically different. */
	error_init (error);
	if (!bcheck_blob (ptr, 0, endp, error))
		goto leave;
	if ((*ptr & 0x80) != 0) {
		if ((*ptr & 0x40) == 0 && !bcheck_blob (ptr, 1, endp, error))
			goto leave;
		else if (!bcheck_blob (ptr, 3, endp, error))
			goto leave;
	}
	*value_out = mono_metadata_decode_value (ptr, retp);
leave:
	return is_ok (error);
}

static MonoObject*
create_custom_attr (MonoImage *image, MonoMethod *method, const guchar *data, guint32 len, MonoError *error)
{
	const char *p = (const char*)data;
	const char *data_end = (const char*)data + len;
	const char *named;
	guint32 i, j, num_named;
	MonoObject *attr;
	void *params_buf [32];
	void **params = NULL;
	MonoMethodSignature *sig;

	error_init (error);

	mono_class_init (method->klass);

	if (!mono_verifier_verify_cattr_content (image, method, data, len, NULL)) {
		set_custom_attr_fmt_error (error);
		return NULL;
	}

	if (len == 0) {
		attr = mono_object_new_checked (mono_domain_get (), method->klass, error);
		if (!mono_error_ok (error)) return NULL;

		mono_runtime_invoke_checked (method, attr, NULL, error);
		if (!mono_error_ok (error))
			return NULL;

		return attr;
	}

	if (len < 2 || read16 (p) != 0x0001) /* Prolog */
		return NULL;

	/*g_print ("got attr %s\n", method->klass->name);*/

	sig = mono_method_signature (method);
	if (sig->param_count < 32) {
		params = params_buf;
		memset (params, 0, sizeof (void*) * sig->param_count);
	} else {
		/* Allocate using GC so it gets GC tracking */
		params = (void **)mono_gc_alloc_fixed (sig->param_count * sizeof (void*), MONO_GC_DESCRIPTOR_NULL, MONO_ROOT_SOURCE_REFLECTION, NULL, "Reflection Custom Attribute Parameters");
	}

	/* skip prolog */
	p += 2;
	for (i = 0; i < mono_method_signature (method)->param_count; ++i) {
		params [i] = load_cattr_value (image, mono_method_signature (method)->params [i], p, data_end, &p, error);
		goto_if_nok (error, fail);
	}

	named = p;
	attr = mono_object_new_checked (mono_domain_get (), method->klass, error);
	if (!mono_error_ok (error)) goto fail;

	MonoObject *exc = NULL;
	mono_runtime_try_invoke (method, attr, params, &exc, error);
	if (!mono_error_ok (error))
		goto fail;
	if (exc) {
		mono_error_set_exception_instance (error, (MonoException*)exc);
		goto fail;
	}

	if (named + 1 < data_end) {
		num_named = read16 (named);
		named += 2;
	} else {
		/* CoreCLR allows p == data + len */
		if (named == data_end)
			num_named = 0;
		else {
			set_custom_attr_fmt_error (error);
			goto fail;
		}
	}
	for (j = 0; j < num_named; j++) {
		guint32 name_len;
		char *name, named_type, data_type;
		if (!bcheck_blob (named, 1, data_end, error))
			goto fail;
		named_type = *named++;
		data_type = *named++; /* type of data */
		if (data_type == MONO_TYPE_SZARRAY) {
			if (!bcheck_blob (named, 0, data_end, error))
				goto fail;
			data_type = *named++;
		}
		if (data_type == MONO_TYPE_ENUM) {
			guint32 type_len;
			char *type_name;
			if (!decode_blob_size_checked (named, data_end, &type_len, &named, error))
				goto fail;
			if (type_len > 0 && !bcheck_blob (named, type_len - 1, data_end, error))
				goto fail;
			type_name = (char *)g_malloc (type_len + 1);
			memcpy (type_name, named, type_len);
			type_name [type_len] = 0;
			named += type_len;
			/* FIXME: lookup the type and check type consistency */
			g_free (type_name);
		}
		if (!decode_blob_size_checked (named, data_end, &name_len, &named, error))
			goto fail;
		if (name_len > 0 && !bcheck_blob (named, name_len - 1, data_end, error))
			goto fail;
		name = (char *)g_malloc (name_len + 1);
		memcpy (name, named, name_len);
		name [name_len] = 0;
		named += name_len;
		if (named_type == 0x53) {
			MonoClassField *field;
			void *val;

			/* how this fail is a blackbox */
			field = mono_class_get_field_from_name (mono_object_class (attr), name);
			if (!field) {
				mono_error_set_generic_error (error, "System.Reflection", "CustomAttributeFormatException", "Could not find a field with name %s", name);
				g_free (name);
				goto fail;
			}

			val = load_cattr_value (image, field->type, named, data_end, &named, error);
			if (!is_ok (error)) {
				g_free (name);
				if (!type_is_reference (field->type))
					g_free (val);
				goto fail;
			}

			mono_field_set_value (attr, field, val);
			if (!type_is_reference (field->type))
				g_free (val);
		} else if (named_type == 0x54) {
			MonoProperty *prop;
			void *pparams [1];
			MonoType *prop_type;

			prop = mono_class_get_property_from_name (mono_object_class (attr), name);

			if (!prop) {
				mono_error_set_generic_error (error, "System.Reflection", "CustomAttributeFormatException", "Could not find a property with name %s", name);
				g_free (name);
				goto fail;
			}

			if (!prop->set) {
				mono_error_set_generic_error (error, "System.Reflection", "CustomAttributeFormatException", "Could not find the setter for %s", name);
				g_free (name);
				goto fail;
			}

			/* can we have more that 1 arg in a custom attr named property? */
			prop_type = prop->get? mono_method_signature (prop->get)->ret :
			     mono_method_signature (prop->set)->params [mono_method_signature (prop->set)->param_count - 1];

			pparams [0] = load_cattr_value (image, prop_type, named, data_end, &named, error);
			if (!is_ok (error)) {
				g_free (name);
				if (!type_is_reference (prop_type))
					g_free (pparams [0]);
				goto fail;
			}


			mono_property_set_value_checked (prop, attr, pparams, error);
			if (!type_is_reference (prop_type))
				g_free (pparams [0]);
			if (!is_ok (error)) {
				g_free (name);
				goto fail;
			}
		}
		g_free (name);
	}

	free_param_data (method->signature, params);
	if (params != params_buf)
		mono_gc_free_fixed (params);

	return attr;

fail:
	free_param_data (method->signature, params);
	if (params != params_buf)
		mono_gc_free_fixed (params);
	return NULL;
}
	
/*
 * mono_reflection_create_custom_attr_data_args:
 *
 *   Create an array of typed and named arguments from the cattr blob given by DATA.
 * TYPED_ARGS and NAMED_ARGS will contain the objects representing the arguments,
 * NAMED_ARG_INFO will contain information about the named arguments.
 */
void
mono_reflection_create_custom_attr_data_args (MonoImage *image, MonoMethod *method, const guchar *data, guint32 len, MonoArray **typed_args, MonoArray **named_args, CattrNamedArg **named_arg_info, MonoError *error)
{
	MonoArray *typedargs, *namedargs;
	MonoClass *attrklass;
	MonoDomain *domain;
	const char *p = (const char*)data;
	const char *data_end = p + len;
	const char *named;
	guint32 i, j, num_named;
	CattrNamedArg *arginfo = NULL;

	*typed_args = NULL;
	*named_args = NULL;
	*named_arg_info = NULL;

	error_init (error);

	if (!mono_verifier_verify_cattr_content (image, method, data, len, NULL)) {
		mono_error_set_generic_error (error, "System.Reflection", "CustomAttributeFormatException", "Binary format of the specified custom attribute was invalid.");
		return;
	}

	mono_class_init (method->klass);
	
	domain = mono_domain_get ();

	if (len < 2 || read16 (p) != 0x0001) /* Prolog */
		return;
	/* skip prolog */
	p += 2;

	/* Parse each argument corresponding to the signature's parameters from
	 * the blob and store in typedargs.
	 */
	typedargs = mono_array_new_checked (domain, mono_get_object_class (), mono_method_signature (method)->param_count, error);
	return_if_nok (error);

	for (i = 0; i < mono_method_signature (method)->param_count; ++i) {
		MonoObject *obj;

		obj = load_cattr_value_boxed (domain, image, mono_method_signature (method)->params [i], p, data_end, &p, error);
		return_if_nok (error);
		mono_array_setref (typedargs, i, obj);
	}

	named = p;

	/* Parse mandatory count of named arguments (could be zero) */
	if (!bcheck_blob (named, 1, data_end, error))
		return;
	num_named = read16 (named);
	namedargs = mono_array_new_checked (domain, mono_get_object_class (), num_named, error);
	return_if_nok (error);
	named += 2;
	attrklass = method->klass;

	arginfo = g_new0 (CattrNamedArg, num_named);
	*named_arg_info = arginfo;

	/* Parse each named arg, and add to arginfo.  Each named argument could
	 * be a field name or a property name followed by a value. */
	for (j = 0; j < num_named; j++) {
		guint32 name_len;
		char *name, named_type, data_type;
		if (!bcheck_blob (named, 1, data_end, error))
			return;
		named_type = *named++; /* field or property? */
		data_type = *named++; /* type of data */
		if (data_type == MONO_TYPE_SZARRAY) {
			if (!bcheck_blob (named, 0, data_end, error))
				return;
			data_type = *named++;
		}
		if (data_type == MONO_TYPE_ENUM) {
			guint32 type_len;
			char *type_name;
			if (!decode_blob_size_checked (named, data_end, &type_len, &named, error))
				return;
			if (ADDP_IS_GREATER_OR_OVF ((const guchar*)named, type_len, data + len))
				goto fail;

			type_name = (char *)g_malloc (type_len + 1);
			memcpy (type_name, named, type_len);
			type_name [type_len] = 0;
			named += type_len;
			/* FIXME: lookup the type and check type consistency */
			g_free (type_name);
		}
		/* named argument name: length, then name */
		if (!decode_blob_size_checked(named, data_end, &name_len, &named, error))
			return;
		if (ADDP_IS_GREATER_OR_OVF ((const guchar*)named, name_len, data + len))
			goto fail;
		name = (char *)g_malloc (name_len + 1);
		memcpy (name, named, name_len);
		name [name_len] = 0;
		named += name_len;
		if (named_type == 0x53) {
			/* Named arg is a field. */
			MonoObject *obj;
			MonoClassField *field = mono_class_get_field_from_name (attrklass, name);

			if (!field) {
				g_free (name);
				goto fail;
			}

			arginfo [j].type = field->type;
			arginfo [j].field = field;

			obj = load_cattr_value_boxed (domain, image, field->type, named, data_end, &named, error);
			if (!is_ok (error)) {
				g_free (name);
				return;
			}
			mono_array_setref (namedargs, j, obj);

		} else if (named_type == 0x54) {
			/* Named arg is a property */
			MonoObject *obj;
			MonoType *prop_type;
			MonoProperty *prop = mono_class_get_property_from_name (attrklass, name);

			if (!prop || !prop->set) {
				g_free (name);
				goto fail;
			}

			prop_type = prop->get? mono_method_signature (prop->get)->ret :
			     mono_method_signature (prop->set)->params [mono_method_signature (prop->set)->param_count - 1];

			arginfo [j].type = prop_type;
			arginfo [j].prop = prop;

			obj = load_cattr_value_boxed (domain, image, prop_type, named, data_end, &named, error);
			if (!is_ok (error)) {
				g_free (name);
				return;
			}
			mono_array_setref (namedargs, j, obj);
		}
		g_free (name);
	}

	*typed_args = typedargs;
	*named_args = namedargs;
	return;
fail:
	mono_error_set_generic_error (error, "System.Reflection", "CustomAttributeFormatException", "Binary format of the specified custom attribute was invalid.");
	g_free (arginfo);
	*named_arg_info = NULL;
}

static gboolean
reflection_resolve_custom_attribute_data (MonoReflectionMethod *ref_method, MonoReflectionAssembly *assembly, gpointer data, guint32 len, MonoArray **ctor_args, MonoArray **named_args, MonoError *error)
{
	MonoDomain *domain;
	MonoArray *typedargs, *namedargs;
	MonoImage *image;
	MonoMethod *method;
	CattrNamedArg *arginfo = NULL;
	int i;

	error_init (error);

	*ctor_args = NULL;
	*named_args = NULL;

	if (len == 0)
		return TRUE;

	image = assembly->assembly->image;
	method = ref_method->method;
	domain = mono_object_domain (ref_method);

	if (!mono_class_init (method->klass)) {
		mono_error_set_for_class_failure (error, method->klass);
		goto leave;
	}

	mono_reflection_create_custom_attr_data_args (image, method, (const guchar *)data, len, &typedargs, &namedargs, &arginfo, error);
	goto_if_nok (error, leave);

	if (!typedargs || !namedargs)
		goto leave;

	for (i = 0; i < mono_method_signature (method)->param_count; ++i) {
		MonoObject *obj = mono_array_get (typedargs, MonoObject*, i);
		MonoObject *typedarg;

		typedarg = create_cattr_typed_arg (mono_method_signature (method)->params [i], obj, error);
		goto_if_nok (error, leave);
		mono_array_setref (typedargs, i, typedarg);
	}

	for (i = 0; i < mono_array_length (namedargs); ++i) {
		MonoObject *obj = mono_array_get (namedargs, MonoObject*, i);
		MonoObject *typedarg, *namedarg, *minfo;

		if (arginfo [i].prop) {
			minfo = (MonoObject*)mono_property_get_object_checked (domain, NULL, arginfo [i].prop, error);
			if (!minfo)
				goto leave;
		} else {
			minfo = (MonoObject*)mono_field_get_object_checked (domain, NULL, arginfo [i].field, error);
			goto_if_nok (error, leave);
		}

		typedarg = create_cattr_typed_arg (arginfo [i].type, obj, error);
		goto_if_nok (error, leave);
		namedarg = create_cattr_named_arg (minfo, typedarg, error);
		goto_if_nok (error, leave);

		mono_array_setref (namedargs, i, namedarg);
	}

	*ctor_args = typedargs;
	*named_args = namedargs;

leave:
	g_free (arginfo);
	return mono_error_ok (error);
}

void
ves_icall_System_Reflection_CustomAttributeData_ResolveArgumentsInternal (MonoReflectionMethod *ref_method, MonoReflectionAssembly *assembly, gpointer data, guint32 len, MonoArray **ctor_args, MonoArray **named_args)
{
	MonoError error;
	(void) reflection_resolve_custom_attribute_data (ref_method, assembly, data, len, ctor_args, named_args, &error);
	mono_error_set_pending_exception (&error);
}

static MonoObjectHandle
create_custom_attr_data_handle (MonoImage *image, MonoCustomAttrEntry *cattr, MonoError *error)
{
	static MonoMethod *ctor;

	MonoDomain *domain;
	void *params [4];

	error_init (error);

	g_assert (image->assembly);

	if (!ctor)
		ctor = mono_class_get_method_from_name (mono_defaults.customattribute_data_class, ".ctor", 4);

	domain = mono_domain_get ();

	MonoObjectHandle attr = MONO_HANDLE_NEW (MonoObject, mono_object_new_checked (domain, mono_defaults.customattribute_data_class, error));
	goto_if_nok (error, fail);

	MonoReflectionMethod *ctor_obj = mono_method_get_object_checked (domain, cattr->ctor, NULL, error);
	goto_if_nok (error, fail);
	MonoReflectionAssemblyHandle assm = mono_assembly_get_object_handle (domain, image->assembly, error);
	goto_if_nok (error, fail);
	params [0] = ctor_obj;
	params [1] = MONO_HANDLE_RAW (assm);
	params [2] = (gpointer)&cattr->data;
	params [3] = &cattr->data_size;

	mono_runtime_invoke_checked (ctor, MONO_HANDLE_RAW (attr), params, error);
	return attr;
fail:
	return MONO_HANDLE_NEW (MonoObject, NULL);
}

static MonoObject *
create_custom_attr_data (MonoImage *image, MonoCustomAttrEntry *cattr, MonoError *error)
{
	HANDLE_FUNCTION_ENTER ();
	MonoObjectHandle obj = create_custom_attr_data_handle (image, cattr, error);
	HANDLE_FUNCTION_RETURN_OBJ (obj);
}

static MonoArray*
mono_custom_attrs_construct_by_type (MonoCustomAttrInfo *cinfo, MonoClass *attr_klass, MonoError *error)
{
	MonoArray *result;
	MonoObject *attr;
	int i, n;

	error_init (error);

	for (i = 0; i < cinfo->num_attrs; ++i) {
		MonoCustomAttrEntry *centry = &cinfo->attrs[i];
		if (!centry->ctor) {
			/* The cattr type is not finished yet */
			/* We should include the type name but cinfo doesn't contain it */
			mono_error_set_type_load_name (error, NULL, NULL, "Custom attribute constructor is null because the custom attribute type is not finished yet.");
			return NULL;
		}
	}

	n = 0;
	if (attr_klass) {
		for (i = 0; i < cinfo->num_attrs; ++i) {
			MonoMethod *ctor = cinfo->attrs[i].ctor;
			g_assert (ctor);
			if (mono_class_is_assignable_from (attr_klass, ctor->klass))
				n++;
		}
	} else {
		n = cinfo->num_attrs;
	}

	result = mono_array_new_cached (mono_domain_get (), mono_defaults.attribute_class, n, error);
	return_val_if_nok (error, NULL);
	n = 0;
	for (i = 0; i < cinfo->num_attrs; ++i) {
		MonoCustomAttrEntry *centry = &cinfo->attrs [i];
		if (!attr_klass || mono_class_is_assignable_from (attr_klass, centry->ctor->klass)) {
			attr = create_custom_attr (cinfo->image, centry->ctor, centry->data, centry->data_size, error);
			if (!mono_error_ok (error))
				return result;
			mono_array_setref (result, n, attr);
			n ++;
		}
	}
	return result;
}

/**
 * mono_custom_attrs_construct:
 */
MonoArray*
mono_custom_attrs_construct (MonoCustomAttrInfo *cinfo)
{
	MonoError error;
	MonoArray *result = mono_custom_attrs_construct_by_type (cinfo, NULL, &error);
	mono_error_assert_ok (&error); /*FIXME proper error handling*/

	return result;
}

static MonoArray*
mono_custom_attrs_data_construct (MonoCustomAttrInfo *cinfo, MonoError *error)
{
	MonoArray *result;
	MonoObject *attr;
	int i;
	
	error_init (error);
	result = mono_array_new_checked (mono_domain_get (), mono_defaults.customattribute_data_class, cinfo->num_attrs, error);
	return_val_if_nok (error, NULL);
	for (i = 0; i < cinfo->num_attrs; ++i) {
		attr = create_custom_attr_data (cinfo->image, &cinfo->attrs [i], error);
		return_val_if_nok (error, NULL);
		mono_array_setref (result, i, attr);
	}
	return result;
}

/**
 * mono_custom_attrs_from_index:
 *
 * Returns: NULL if no attributes are found or if a loading error occurs.
 */
MonoCustomAttrInfo*
mono_custom_attrs_from_index (MonoImage *image, guint32 idx)
{
	MonoError error;
	MonoCustomAttrInfo *result = mono_custom_attrs_from_index_checked (image, idx, FALSE, &error);
	mono_error_cleanup (&error);
	return result;
}
/**
 * mono_custom_attrs_from_index_checked:
 * \returns NULL if no attributes are found.  On error returns NULL and sets \p error.
 */
MonoCustomAttrInfo*
mono_custom_attrs_from_index_checked (MonoImage *image, guint32 idx, gboolean ignore_missing, MonoError *error)
{
	guint32 mtoken, i, len;
	guint32 cols [MONO_CUSTOM_ATTR_SIZE];
	MonoTableInfo *ca;
	MonoCustomAttrInfo *ainfo;
	GList *tmp, *list = NULL;
	const char *data;
	MonoCustomAttrEntry* attr;

	error_init (error);

	ca = &image->tables [MONO_TABLE_CUSTOMATTRIBUTE];

	i = mono_metadata_custom_attrs_from_index (image, idx);
	if (!i)
		return NULL;
	i --;
	while (i < ca->rows) {
		if (mono_metadata_decode_row_col (ca, i, MONO_CUSTOM_ATTR_PARENT) != idx)
			break;
		list = g_list_prepend (list, GUINT_TO_POINTER (i));
		++i;
	}
	len = g_list_length (list);
	if (!len)
		return NULL;
	ainfo = (MonoCustomAttrInfo *)g_malloc0 (MONO_SIZEOF_CUSTOM_ATTR_INFO + sizeof (MonoCustomAttrEntry) * len);
	ainfo->num_attrs = len;
	ainfo->image = image;
	for (i = len, tmp = list; i != 0; --i, tmp = tmp->next) {
		mono_metadata_decode_row (ca, GPOINTER_TO_UINT (tmp->data), cols, MONO_CUSTOM_ATTR_SIZE);
		mtoken = cols [MONO_CUSTOM_ATTR_TYPE] >> MONO_CUSTOM_ATTR_TYPE_BITS;
		switch (cols [MONO_CUSTOM_ATTR_TYPE] & MONO_CUSTOM_ATTR_TYPE_MASK) {
		case MONO_CUSTOM_ATTR_TYPE_METHODDEF:
			mtoken |= MONO_TOKEN_METHOD_DEF;
			break;
		case MONO_CUSTOM_ATTR_TYPE_MEMBERREF:
			mtoken |= MONO_TOKEN_MEMBER_REF;
			break;
		default:
			g_error ("Unknown table for custom attr type %08x", cols [MONO_CUSTOM_ATTR_TYPE]);
			break;
		}
		attr = &ainfo->attrs [i - 1];
		attr->ctor = mono_get_method_checked (image, mtoken, NULL, NULL, error);
		if (!attr->ctor) {
			g_warning ("Can't find custom attr constructor image: %s mtoken: 0x%08x due to: %s", image->name, mtoken, mono_error_get_message (error));
			if (ignore_missing) {
				mono_error_cleanup (error);
				error_init (error);
			} else {
				g_list_free (list);
				g_free (ainfo);
				return NULL;
			}
		}

		if (!mono_verifier_verify_cattr_blob (image, cols [MONO_CUSTOM_ATTR_VALUE], NULL)) {
			/*FIXME raising an exception here doesn't make any sense*/
			g_warning ("Invalid custom attribute blob on image %s for index %x", image->name, idx);
			g_list_free (list);
			g_free (ainfo);
			return NULL;
		}
		data = mono_metadata_blob_heap (image, cols [MONO_CUSTOM_ATTR_VALUE]);
		attr->data_size = mono_metadata_decode_value (data, &data);
		attr->data = (guchar*)data;
	}
	g_list_free (list);

	return ainfo;
}

/**
 * mono_custom_attrs_from_method:
 */
MonoCustomAttrInfo*
mono_custom_attrs_from_method (MonoMethod *method)
{
	MonoError error;
	MonoCustomAttrInfo* result = mono_custom_attrs_from_method_checked  (method, &error);
	mono_error_cleanup (&error); /* FIXME want a better API that doesn't swallow the error */
	return result;
}

MonoCustomAttrInfo*
mono_custom_attrs_from_method_checked (MonoMethod *method, MonoError *error)
{
	guint32 idx;

	error_init (error);

	/*
	 * An instantiated method has the same cattrs as the generic method definition.
	 *
	 * LAMESPEC: The .NET SRE throws an exception for instantiations of generic method builders
	 *           Note that this stanza is not necessary for non-SRE types, but it's a micro-optimization
	 */
	if (method->is_inflated)
		method = ((MonoMethodInflated *) method)->declaring;
	
	if (method_is_dynamic (method) || image_is_dynamic (method->klass->image))
		return lookup_custom_attr (method->klass->image, method);

	if (!method->token)
		/* Synthetic methods */
		return NULL;

	idx = mono_method_get_index (method);
	idx <<= MONO_CUSTOM_ATTR_BITS;
	idx |= MONO_CUSTOM_ATTR_METHODDEF;
	return mono_custom_attrs_from_index_checked (method->klass->image, idx, FALSE, error);
}

/**
 * mono_custom_attrs_from_class:
 */
MonoCustomAttrInfo*
mono_custom_attrs_from_class (MonoClass *klass)
{
	MonoError error;
	MonoCustomAttrInfo *result = mono_custom_attrs_from_class_checked (klass, &error);
	mono_error_cleanup (&error);
	return result;
}

MonoCustomAttrInfo*
mono_custom_attrs_from_class_checked (MonoClass *klass, MonoError *error)
{
	guint32 idx;

	error_init (error);

	if (mono_class_is_ginst (klass))
		klass = mono_class_get_generic_class (klass)->container_class;

	if (image_is_dynamic (klass->image))
		return lookup_custom_attr (klass->image, klass);

	if (klass->byval_arg.type == MONO_TYPE_VAR || klass->byval_arg.type == MONO_TYPE_MVAR) {
		idx = mono_metadata_token_index (klass->sizes.generic_param_token);
		idx <<= MONO_CUSTOM_ATTR_BITS;
		idx |= MONO_CUSTOM_ATTR_GENERICPAR;
	} else {
		idx = mono_metadata_token_index (klass->type_token);
		idx <<= MONO_CUSTOM_ATTR_BITS;
		idx |= MONO_CUSTOM_ATTR_TYPEDEF;
	}
	return mono_custom_attrs_from_index_checked (klass->image, idx, FALSE, error);
}

/**
 * mono_custom_attrs_from_assembly:
 */
MonoCustomAttrInfo*
mono_custom_attrs_from_assembly (MonoAssembly *assembly)
{
	MonoError error;
	MonoCustomAttrInfo *result = mono_custom_attrs_from_assembly_checked (assembly, FALSE, &error);
	mono_error_cleanup (&error);
	return result;
}

MonoCustomAttrInfo*
mono_custom_attrs_from_assembly_checked (MonoAssembly *assembly, gboolean ignore_missing, MonoError *error)
{
	guint32 idx;
	
	error_init (error);

	if (image_is_dynamic (assembly->image))
		return lookup_custom_attr (assembly->image, assembly);
	idx = 1; /* there is only one assembly */
	idx <<= MONO_CUSTOM_ATTR_BITS;
	idx |= MONO_CUSTOM_ATTR_ASSEMBLY;
	return mono_custom_attrs_from_index_checked (assembly->image, idx, ignore_missing, error);
}

static MonoCustomAttrInfo*
mono_custom_attrs_from_module (MonoImage *image, MonoError *error)
{
	guint32 idx;

	error_init (error);
	
	if (image_is_dynamic (image))
		return lookup_custom_attr (image, image);
	idx = 1; /* there is only one module */
	idx <<= MONO_CUSTOM_ATTR_BITS;
	idx |= MONO_CUSTOM_ATTR_MODULE;
	return mono_custom_attrs_from_index_checked (image, idx, FALSE, error);
}

/**
 * mono_custom_attrs_from_property:
 */
MonoCustomAttrInfo*
mono_custom_attrs_from_property (MonoClass *klass, MonoProperty *property)
{
	MonoError error;
	MonoCustomAttrInfo * result = mono_custom_attrs_from_property_checked (klass, property, &error);
	mono_error_cleanup (&error);
	return result;
}

MonoCustomAttrInfo*
mono_custom_attrs_from_property_checked (MonoClass *klass, MonoProperty *property, MonoError *error)
{
	guint32 idx;

	error_init (error);
	
	if (image_is_dynamic (klass->image)) {
		property = mono_metadata_get_corresponding_property_from_generic_type_definition (property);
		return lookup_custom_attr (klass->image, property);
	}
	idx = find_property_index (klass, property);
	idx <<= MONO_CUSTOM_ATTR_BITS;
	idx |= MONO_CUSTOM_ATTR_PROPERTY;
	return mono_custom_attrs_from_index_checked (klass->image, idx, FALSE, error);
}

/**
 * mono_custom_attrs_from_event:
 */
MonoCustomAttrInfo*
mono_custom_attrs_from_event (MonoClass *klass, MonoEvent *event)
{
	MonoError error;
	MonoCustomAttrInfo * result = mono_custom_attrs_from_event_checked (klass, event, &error);
	mono_error_cleanup (&error);
	return result;
}

MonoCustomAttrInfo*
mono_custom_attrs_from_event_checked (MonoClass *klass, MonoEvent *event, MonoError *error)
{
	guint32 idx;

	error_init (error);
	
	if (image_is_dynamic (klass->image)) {
		event = mono_metadata_get_corresponding_event_from_generic_type_definition (event);
		return lookup_custom_attr (klass->image, event);
	}
	idx = find_event_index (klass, event);
	idx <<= MONO_CUSTOM_ATTR_BITS;
	idx |= MONO_CUSTOM_ATTR_EVENT;
	return mono_custom_attrs_from_index_checked (klass->image, idx, FALSE, error);
}

/**
 * mono_custom_attrs_from_field:
 */
MonoCustomAttrInfo*
mono_custom_attrs_from_field (MonoClass *klass, MonoClassField *field)
{
	MonoError error;
	MonoCustomAttrInfo * result = mono_custom_attrs_from_field_checked (klass, field, &error);
	mono_error_cleanup (&error);
	return result;
}

MonoCustomAttrInfo*
mono_custom_attrs_from_field_checked (MonoClass *klass, MonoClassField *field, MonoError *error)
{
	guint32 idx;
	error_init (error);

	if (image_is_dynamic (klass->image)) {
		field = mono_metadata_get_corresponding_field_from_generic_type_definition (field);
		return lookup_custom_attr (klass->image, field);
	}
	idx = find_field_index (klass, field);
	idx <<= MONO_CUSTOM_ATTR_BITS;
	idx |= MONO_CUSTOM_ATTR_FIELDDEF;
	return mono_custom_attrs_from_index_checked (klass->image, idx, FALSE, error);
}

/**
 * mono_custom_attrs_from_param:
 * \param method handle to the method that we want to retrieve custom parameter information from
 * \param param parameter number, where zero represent the return value, and one is the first parameter in the method
 *
 * The result must be released with mono_custom_attrs_free().
 *
 * \returns the custom attribute object for the specified parameter, or NULL if there are none.
 */
MonoCustomAttrInfo*
mono_custom_attrs_from_param (MonoMethod *method, guint32 param)
{
	MonoError error;
	MonoCustomAttrInfo *result = mono_custom_attrs_from_param_checked (method, param, &error);
	mono_error_cleanup (&error);
	return result;
}

/**
 * mono_custom_attrs_from_param_checked:
 * \param method handle to the method that we want to retrieve custom parameter information from
 * \param param parameter number, where zero represent the return value, and one is the first parameter in the method
 * \param error set on error
 *
 * The result must be released with mono_custom_attrs_free().
 *
 * \returns the custom attribute object for the specified parameter, or NULL if there are none.  On failure returns NULL and sets \p error.
 */
MonoCustomAttrInfo*
mono_custom_attrs_from_param_checked (MonoMethod *method, guint32 param, MonoError *error)
{
	MonoTableInfo *ca;
	guint32 i, idx, method_index;
	guint32 param_list, param_last, param_pos, found;
	MonoImage *image;
	MonoReflectionMethodAux *aux;

	error_init (error);

	/*
	 * An instantiated method has the same cattrs as the generic method definition.
	 *
	 * LAMESPEC: The .NET SRE throws an exception for instantiations of generic method builders
	 *           Note that this stanza is not necessary for non-SRE types, but it's a micro-optimization
	 */
	if (method->is_inflated)
		method = ((MonoMethodInflated *) method)->declaring;

	if (image_is_dynamic (method->klass->image)) {
		MonoCustomAttrInfo *res, *ainfo;
		int size;

		aux = (MonoReflectionMethodAux *)g_hash_table_lookup (((MonoDynamicImage*)method->klass->image)->method_aux_hash, method);
		if (!aux || !aux->param_cattr)
			return NULL;

		/* Need to copy since it will be freed later */
		ainfo = aux->param_cattr [param];
		if (!ainfo)
			return NULL;
		size = MONO_SIZEOF_CUSTOM_ATTR_INFO + sizeof (MonoCustomAttrEntry) * ainfo->num_attrs;
		res = (MonoCustomAttrInfo *)g_malloc0 (size);
		memcpy (res, ainfo, size);
		return res;
	}

	image = method->klass->image;
	method_index = mono_method_get_index (method);
	if (!method_index)
		return NULL;
	ca = &image->tables [MONO_TABLE_METHOD];

	param_list = mono_metadata_decode_row_col (ca, method_index - 1, MONO_METHOD_PARAMLIST);
	if (method_index == ca->rows) {
		ca = &image->tables [MONO_TABLE_PARAM];
		param_last = ca->rows + 1;
	} else {
		param_last = mono_metadata_decode_row_col (ca, method_index, MONO_METHOD_PARAMLIST);
		ca = &image->tables [MONO_TABLE_PARAM];
	}
	found = FALSE;
	for (i = param_list; i < param_last; ++i) {
		param_pos = mono_metadata_decode_row_col (ca, i - 1, MONO_PARAM_SEQUENCE);
		if (param_pos == param) {
			found = TRUE;
			break;
		}
	}
	if (!found)
		return NULL;
	idx = i;
	idx <<= MONO_CUSTOM_ATTR_BITS;
	idx |= MONO_CUSTOM_ATTR_PARAMDEF;
	return mono_custom_attrs_from_index_checked (image, idx, FALSE, error);
}

/**
 * mono_custom_attrs_has_attr:
 */
gboolean
mono_custom_attrs_has_attr (MonoCustomAttrInfo *ainfo, MonoClass *attr_klass)
{
	int i;
	for (i = 0; i < ainfo->num_attrs; ++i) {
		MonoCustomAttrEntry *centry = &ainfo->attrs[i];
		if (centry->ctor == NULL)
			continue;
		MonoClass *klass = centry->ctor->klass;
		if (klass == attr_klass || mono_class_has_parent (klass, attr_klass) || (MONO_CLASS_IS_INTERFACE (attr_klass) && mono_class_is_assignable_from (attr_klass, klass)))
			return TRUE;
	}
	return FALSE;
}

/**
 * mono_custom_attrs_get_attr:
 */
MonoObject*
mono_custom_attrs_get_attr (MonoCustomAttrInfo *ainfo, MonoClass *attr_klass)
{
	MonoError error;
	MonoObject *res = mono_custom_attrs_get_attr_checked (ainfo, attr_klass, &error);
	mono_error_assert_ok (&error); /*FIXME proper error handling*/
	return res;
}

MonoObject*
mono_custom_attrs_get_attr_checked (MonoCustomAttrInfo *ainfo, MonoClass *attr_klass, MonoError *error)
{
	int i;
	MonoCustomAttrEntry *centry = NULL;

	g_assert (attr_klass != NULL);

	error_init (error);

	for (i = 0; i < ainfo->num_attrs; ++i) {
		centry = &ainfo->attrs[i];
		if (centry->ctor == NULL)
			continue;
		MonoClass *klass = centry->ctor->klass;
		if (attr_klass == klass || mono_class_is_assignable_from (attr_klass, klass))
			break;
	}
	if (centry == NULL)
		return NULL;

	return create_custom_attr (ainfo->image, centry->ctor, centry->data, centry->data_size, error);
}

/**
 * mono_reflection_get_custom_attrs_info:
 * \param obj a reflection object handle
 *
 * \returns the custom attribute info for attributes defined for the
 * reflection handle \p obj. The objects.
 *
 * FIXME this function leaks like a sieve for SRE objects.
 */
MonoCustomAttrInfo*
mono_reflection_get_custom_attrs_info (MonoObject *obj_raw)
{
	HANDLE_FUNCTION_ENTER ();
	MonoError error;
	MONO_HANDLE_DCL (MonoObject, obj);
	MonoCustomAttrInfo *result = mono_reflection_get_custom_attrs_info_checked (obj, &error);
	mono_error_assert_ok (&error);
	HANDLE_FUNCTION_RETURN_VAL (result);
}

/**
 * mono_reflection_get_custom_attrs_info_checked:
 * \param obj a reflection object handle
 * \param error set on error
 *
 * \returns the custom attribute info for attributes defined for the
 * reflection handle \p obj. The objects. On failure returns NULL and sets \p error.
 *
 * FIXME this function leaks like a sieve for SRE objects.
 */
MonoCustomAttrInfo*
mono_reflection_get_custom_attrs_info_checked (MonoObjectHandle obj, MonoError *error)
{
	HANDLE_FUNCTION_ENTER ();
	MonoClass *klass;
	MonoCustomAttrInfo *cinfo = NULL;
	
	error_init (error);

	klass = mono_handle_class (obj);
	if (klass == mono_defaults.runtimetype_class) {
		MonoType *type = mono_reflection_type_handle_mono_type (MONO_HANDLE_CAST(MonoReflectionType, obj), error);
		goto_if_nok (error, leave);
		klass = mono_class_from_mono_type (type);
		/*We cannot mono_class_init the class from which we'll load the custom attributes since this must work with broken types.*/
		cinfo = mono_custom_attrs_from_class_checked (klass, error);
		goto_if_nok (error, leave);
	} else if (strcmp ("Assembly", klass->name) == 0 || strcmp ("MonoAssembly", klass->name) == 0) {
		MonoReflectionAssemblyHandle rassembly = MONO_HANDLE_CAST (MonoReflectionAssembly, obj);
		cinfo = mono_custom_attrs_from_assembly_checked (MONO_HANDLE_GETVAL (rassembly, assembly), FALSE, error);
		goto_if_nok (error, leave);
	} else if (strcmp ("Module", klass->name) == 0 || strcmp ("MonoModule", klass->name) == 0) {
		MonoReflectionModuleHandle module = MONO_HANDLE_CAST (MonoReflectionModule, obj);
		cinfo = mono_custom_attrs_from_module (MONO_HANDLE_GETVAL (module, image), error);
		goto_if_nok (error, leave);
	} else if (strcmp ("MonoProperty", klass->name) == 0) {
		MonoReflectionPropertyHandle rprop = MONO_HANDLE_CAST (MonoReflectionProperty, obj);
		MonoProperty *property = MONO_HANDLE_GETVAL (rprop, property);
		cinfo = mono_custom_attrs_from_property_checked (property->parent, property, error);
		goto_if_nok (error, leave);
	} else if (strcmp ("MonoEvent", klass->name) == 0) {
		MonoReflectionMonoEventHandle revent = MONO_HANDLE_CAST (MonoReflectionMonoEvent, obj);
		MonoEvent *event = MONO_HANDLE_GETVAL (revent, event);
		cinfo = mono_custom_attrs_from_event_checked (event->parent, event, error);
		goto_if_nok (error, leave);
	} else if (strcmp ("MonoField", klass->name) == 0) {
		MonoReflectionFieldHandle rfield = MONO_HANDLE_CAST (MonoReflectionField, obj);
		MonoClassField *field = MONO_HANDLE_GETVAL (rfield, field);
		cinfo = mono_custom_attrs_from_field_checked (field->parent, field, error);
		goto_if_nok (error, leave);
	} else if ((strcmp ("MonoMethod", klass->name) == 0) || (strcmp ("MonoCMethod", klass->name) == 0)) {
		MonoReflectionMethodHandle rmethod = MONO_HANDLE_CAST (MonoReflectionMethod, obj);
		cinfo = mono_custom_attrs_from_method_checked (MONO_HANDLE_GETVAL (rmethod, method), error);
		goto_if_nok (error, leave);
	} else if (strcmp ("ParameterInfo", klass->name) == 0 || strcmp ("MonoParameterInfo", klass->name) == 0) {
		MonoReflectionParameterHandle param = MONO_HANDLE_CAST (MonoReflectionParameter, obj);
		MonoObjectHandle member_impl = MONO_HANDLE_NEW_GET (MonoObject, param, MemberImpl);
		MonoClass *member_class = mono_handle_class (member_impl);
		if (mono_class_is_reflection_method_or_constructor (member_class)) {
			MonoReflectionMethodHandle rmethod = MONO_HANDLE_CAST (MonoReflectionMethod, member_impl);
			cinfo = mono_custom_attrs_from_param_checked (MONO_HANDLE_GETVAL (rmethod, method), MONO_HANDLE_GETVAL (param, PositionImpl) + 1, error);
			goto_if_nok (error, leave);
		} else if (mono_is_sr_mono_property (member_class)) {
			MonoReflectionPropertyHandle prop = MONO_HANDLE_CAST (MonoReflectionProperty, member_impl);
			MonoProperty *property = MONO_HANDLE_GETVAL (prop, property);
			MonoMethod *method;
			if (!(method = property->get))
				method = property->set;
			g_assert (method);

			cinfo = mono_custom_attrs_from_param_checked (method, MONO_HANDLE_GETVAL (param, PositionImpl) + 1, error);
			goto_if_nok (error, leave);
		} 
#ifndef DISABLE_REFLECTION_EMIT
		else if (mono_is_sre_method_on_tb_inst (member_class)) {/*XXX This is a workaround for Compiler Context*/
			// FIXME: Is this still needed ?
			g_assert_not_reached ();
		} else if (mono_is_sre_ctor_on_tb_inst (member_class)) { /*XX This is a workaround for Compiler Context*/
			// FIXME: Is this still needed ?
			g_assert_not_reached ();
		} 
#endif
		else {
			char *type_name = mono_type_get_full_name (member_class);
			mono_error_set_not_supported (error,
						      "Custom attributes on a ParamInfo with member %s are not supported",
						      type_name);
			g_free (type_name);
			goto leave;
		}
	} else if (strcmp ("AssemblyBuilder", klass->name) == 0) {
		MonoReflectionAssemblyBuilderHandle assemblyb = MONO_HANDLE_CAST (MonoReflectionAssemblyBuilder, obj);
		MonoReflectionAssemblyHandle assembly = MONO_HANDLE_CAST (MonoReflectionAssembly, assemblyb);
		MonoArrayHandle cattrs = MONO_HANDLE_NEW_GET (MonoArray, assemblyb, cattrs);
		MonoImage * image = MONO_HANDLE_GETVAL (assembly, assembly)->image;
		g_assert (image);
		cinfo = mono_custom_attrs_from_builders_handle (NULL, image, cattrs);
	} else if (strcmp ("TypeBuilder", klass->name) == 0) {
		MonoReflectionTypeBuilderHandle tb = MONO_HANDLE_CAST (MonoReflectionTypeBuilder, obj);
		MonoReflectionModuleBuilderHandle module = MONO_HANDLE_NEW_GET (MonoReflectionModuleBuilder, tb, module);
		MonoDynamicImage *dynamic_image = MONO_HANDLE_GETVAL (module, dynamic_image);
		MonoArrayHandle cattrs = MONO_HANDLE_NEW_GET (MonoArray, tb, cattrs);
		cinfo = mono_custom_attrs_from_builders_handle (NULL, &dynamic_image->image, cattrs);
	} else if (strcmp ("ModuleBuilder", klass->name) == 0) {
		MonoReflectionModuleBuilderHandle mb = MONO_HANDLE_CAST (MonoReflectionModuleBuilder, obj);
		MonoDynamicImage *dynamic_image = MONO_HANDLE_GETVAL (mb, dynamic_image);
		MonoArrayHandle cattrs = MONO_HANDLE_NEW_GET (MonoArray, mb, cattrs);
		cinfo = mono_custom_attrs_from_builders_handle (NULL, &dynamic_image->image, cattrs);
	} else if (strcmp ("ConstructorBuilder", klass->name) == 0) {
		MonoReflectionCtorBuilderHandle cb = MONO_HANDLE_CAST (MonoReflectionCtorBuilder, obj);
		MonoMethod *mhandle = MONO_HANDLE_GETVAL (cb, mhandle);
		MonoArrayHandle cattrs = MONO_HANDLE_NEW_GET (MonoArray, cb, cattrs);
		cinfo = mono_custom_attrs_from_builders_handle (NULL, mhandle->klass->image, cattrs);
	} else if (strcmp ("MethodBuilder", klass->name) == 0) {
		MonoReflectionMethodBuilderHandle mb = MONO_HANDLE_CAST (MonoReflectionMethodBuilder, obj);
		MonoMethod *mhandle = MONO_HANDLE_GETVAL (mb, mhandle);
		MonoArrayHandle cattrs = MONO_HANDLE_NEW_GET (MonoArray, mb, cattrs);
		cinfo = mono_custom_attrs_from_builders_handle (NULL, mhandle->klass->image, cattrs);
	} else if (strcmp ("FieldBuilder", klass->name) == 0) {
		MonoReflectionFieldBuilderHandle fb = MONO_HANDLE_CAST (MonoReflectionFieldBuilder, obj);
		MonoReflectionTypeBuilderHandle tb = MONO_HANDLE_NEW_GET (MonoReflectionTypeBuilder, fb, typeb);
		MonoReflectionModuleBuilderHandle mb = MONO_HANDLE_NEW_GET (MonoReflectionModuleBuilder, tb, module);
		MonoDynamicImage *dynamic_image = MONO_HANDLE_GETVAL (mb, dynamic_image);
		MonoArrayHandle cattrs = MONO_HANDLE_NEW_GET (MonoArray, fb, cattrs);
		cinfo = mono_custom_attrs_from_builders_handle (NULL, &dynamic_image->image, cattrs);
	} else if (strcmp ("MonoGenericClass", klass->name) == 0) {
		MonoReflectionGenericClassHandle gclass = MONO_HANDLE_CAST (MonoReflectionGenericClass, obj);
		MonoReflectionTypeHandle generic_type = MONO_HANDLE_NEW_GET (MonoReflectionType, gclass, generic_type);
		cinfo = mono_reflection_get_custom_attrs_info_checked (MONO_HANDLE_CAST (MonoObject, generic_type), error);
		goto_if_nok (error, leave);
	} else { /* handle other types here... */
		g_error ("get custom attrs not yet supported for %s", klass->name);
	}

leave:
	HANDLE_FUNCTION_RETURN_VAL (cinfo);
}

/**
 * mono_reflection_get_custom_attrs_by_type:
 * \param obj a reflection object handle
 * \returns an array with all the custom attributes defined of the
 * reflection handle \p obj. If \p attr_klass is non-NULL, only custom attributes 
 * of that type are returned. The objects are fully build. Return NULL if a loading error
 * occurs.
 */
MonoArray*
mono_reflection_get_custom_attrs_by_type (MonoObject *obj_raw, MonoClass *attr_klass, MonoError *error)
{
	HANDLE_FUNCTION_ENTER ();
	MONO_HANDLE_DCL (MonoObject, obj);
	MonoArrayHandle result = mono_reflection_get_custom_attrs_by_type_handle (obj, attr_klass, error);
	HANDLE_FUNCTION_RETURN_OBJ (result);
}

MonoArrayHandle
mono_reflection_get_custom_attrs_by_type_handle (MonoObjectHandle obj, MonoClass *attr_klass, MonoError *error)
{
	MonoArrayHandle result = MONO_HANDLE_NEW (MonoArray, NULL);
	MonoCustomAttrInfo *cinfo;

	error_init (error);

	cinfo = mono_reflection_get_custom_attrs_info_checked (obj, error);
	goto_if_nok (error, leave);
	if (cinfo) {
		MONO_HANDLE_ASSIGN (result, MONO_HANDLE_NEW (MonoArray, mono_custom_attrs_construct_by_type (cinfo, attr_klass, error))); /* FIXME use coop handles for mono_custom_attrs_construct_by_type */
		if (!cinfo->cached)
			mono_custom_attrs_free (cinfo);
		if (!result)
			goto leave;
	} else {
		MONO_HANDLE_ASSIGN (result, mono_array_new_handle (mono_domain_get (), mono_defaults.attribute_class, 0, error));
	}

leave:
	return result;
}

/**
 * mono_reflection_get_custom_attrs:
 * \param obj a reflection object handle
 * \return an array with all the custom attributes defined of the
 * reflection handle \p obj. The objects are fully build. Return NULL if a loading error
 * occurs.
 */
MonoArray*
mono_reflection_get_custom_attrs (MonoObject *obj_raw)
{
	HANDLE_FUNCTION_ENTER ();
	MonoError error;
	MONO_HANDLE_DCL (MonoObject, obj);
	MonoArrayHandle result = mono_reflection_get_custom_attrs_by_type_handle (obj, NULL, &error);
	mono_error_cleanup (&error);
	HANDLE_FUNCTION_RETURN_OBJ (result);
}

/**
 * mono_reflection_get_custom_attrs_data:
 * \param obj a reflection obj handle
 * \returns an array of \c System.Reflection.CustomAttributeData,
 * which include information about attributes reflected on
 * types loaded using the Reflection Only methods
 */
MonoArray*
mono_reflection_get_custom_attrs_data (MonoObject *obj_raw)
{
	HANDLE_FUNCTION_ENTER ();
	MonoError error;
	MONO_HANDLE_DCL (MonoObject, obj);
	MonoArrayHandle result = mono_reflection_get_custom_attrs_data_checked (obj, &error);
	mono_error_cleanup (&error);
	HANDLE_FUNCTION_RETURN_OBJ (result);
}

/*
 * mono_reflection_get_custom_attrs_data_checked:
 * @obj: a reflection obj handle
 * @error: set on error
 *
 * Returns an array of System.Reflection.CustomAttributeData,
 * which include information about attributes reflected on
 * types loaded using the Reflection Only methods
 */
MonoArrayHandle
mono_reflection_get_custom_attrs_data_checked (MonoObjectHandle obj, MonoError *error)
{
	MonoArrayHandle result = MONO_HANDLE_NEW (MonoArray, NULL);
	MonoCustomAttrInfo *cinfo;

	error_init (error);

	cinfo = mono_reflection_get_custom_attrs_info_checked (obj, error);
	goto_if_nok (error, leave);
	if (cinfo) {
		MONO_HANDLE_ASSIGN (result, MONO_HANDLE_NEW (MonoArray, mono_custom_attrs_data_construct (cinfo, error))); /* FIXME use coop handles in mono_custom_attrs_data_construct */
		if (!cinfo->cached)
			mono_custom_attrs_free (cinfo);
		goto_if_nok (error, leave);
	} else 
		MONO_HANDLE_ASSIGN (result, mono_array_new_handle (mono_domain_get (), mono_defaults.customattribute_data_class, 0, error));

leave:
	return result;
}

static gboolean
custom_attr_class_name_from_methoddef (MonoImage *image, guint32 method_token, const gchar **nspace, const gchar **class_name)
{
	/* mono_get_method_from_token () */
	g_assert (mono_metadata_token_table (method_token) == MONO_TABLE_METHOD);
	guint32 type_token = mono_metadata_typedef_from_method (image, method_token);
	if (!type_token) {
		/* Bad method token (could not find corresponding typedef) */
		return FALSE;
	}
	type_token |= MONO_TOKEN_TYPE_DEF;
	{
		/* mono_class_create_from_typedef () */
		MonoTableInfo *tt = &image->tables [MONO_TABLE_TYPEDEF];
		guint32 cols [MONO_TYPEDEF_SIZE];
		guint tidx = mono_metadata_token_index (type_token);

		if (mono_metadata_token_table (type_token) != MONO_TABLE_TYPEDEF || tidx > tt->rows) {
			/* "Invalid typedef token %x", type_token */
			return FALSE;
		}

		mono_metadata_decode_row (tt, tidx - 1, cols, MONO_TYPEDEF_SIZE);

		if (class_name)
			*class_name = mono_metadata_string_heap (image, cols [MONO_TYPEDEF_NAME]);
		if (nspace)
			*nspace = mono_metadata_string_heap (image, cols [MONO_TYPEDEF_NAMESPACE]);
		return TRUE;
	}
}


/**
 * custom_attr_class_name_from_method_token:
 * @image: The MonoImage
 * @method_token: a token for a custom attr constructor in @image
 * @assembly_token: out argment set to the assembly ref token of the custom attr
 * @nspace: out argument set to namespace (a string in the string heap of @image) of the custom attr
 * @class_name: out argument set to the class name of the custom attr.
 *
 * Given an @image and a @method_token (which is assumed to be a
 * constructor), fills in the out arguments with the assembly ref (if
 * a methodref) and the namespace and class name of the custom
 * attribute.
 *
 * Returns: TRUE on success, FALSE otherwise.
 *
 * LOCKING: does not take locks
 */
static gboolean
custom_attr_class_name_from_method_token (MonoImage *image, guint32 method_token, guint32 *assembly_token, const gchar **nspace, const gchar **class_name)
{
	/* This only works with method tokens constructed from a
	 * custom attr token, which can only be methoddef or
	 * memberref */
	g_assert (mono_metadata_token_table (method_token) == MONO_TABLE_METHOD
		  || mono_metadata_token_table  (method_token) == MONO_TABLE_MEMBERREF);

	if (mono_metadata_token_table (method_token) == MONO_TABLE_MEMBERREF) {
		/* method_from_memberref () */
		guint32 cols[6];
		guint32 nindex, class_index;

		int idx = mono_metadata_token_index (method_token);

		mono_metadata_decode_row (&image->tables [MONO_TABLE_MEMBERREF], idx-1, cols, 3);
		nindex = cols [MONO_MEMBERREF_CLASS] >> MONO_MEMBERREF_PARENT_BITS;
		class_index = cols [MONO_MEMBERREF_CLASS] & MONO_MEMBERREF_PARENT_MASK;
		if (class_index == MONO_MEMBERREF_PARENT_TYPEREF) {
			guint32 type_token = MONO_TOKEN_TYPE_REF | nindex;
			/* mono_class_from_typeref_checked () */
			{
				guint32 cols [MONO_TYPEREF_SIZE];
				MonoTableInfo  *t = &image->tables [MONO_TABLE_TYPEREF];

				mono_metadata_decode_row (t, (type_token&0xffffff)-1, cols, MONO_TYPEREF_SIZE);

				if (class_name)
					*class_name = mono_metadata_string_heap (image, cols [MONO_TYPEREF_NAME]);
				if (nspace)
					*nspace = mono_metadata_string_heap (image, cols [MONO_TYPEREF_NAMESPACE]);
				if (assembly_token)
					*assembly_token = cols [MONO_TYPEREF_SCOPE];
				return TRUE;
			}
		} else if (class_index == MONO_MEMBERREF_PARENT_METHODDEF) {
			guint32 methoddef_token = MONO_TOKEN_METHOD_DEF | nindex;
			if (assembly_token)
				*assembly_token = 0;
			return custom_attr_class_name_from_methoddef (image, methoddef_token, nspace, class_name);
		} else {
			/* Attributes can't be generic, so it won't be
			 * a typespec, and they're always
			 * constructors, so it won't be a moduleref */
			g_assert_not_reached ();
		}
	} else {
		/* must be MONO_TABLE_METHOD */
		if (assembly_token)
			*assembly_token = 0;
		return custom_attr_class_name_from_methoddef (image, method_token, nspace, class_name);
	}
}

/**
 * mono_assembly_metadata_foreach_custom_attr:
 * \param assembly the assembly to iterate over
 * \param func the function to call for each custom attribute
 * \param user_data passed to \p func
 * Calls \p func for each custom attribute type on the given assembly until \p func returns TRUE.
 * Everything is done using low-level metadata APIs, so it is safe to use during assembly loading.
 */
void
mono_assembly_metadata_foreach_custom_attr (MonoAssembly *assembly, MonoAssemblyMetadataCustomAttrIterFunc func, gpointer user_data)
{
	MonoImage *image;
	guint32 mtoken, i;
	guint32 cols [MONO_CUSTOM_ATTR_SIZE];
	MonoTableInfo *ca;
	guint32 idx;

	/*
	 * This might be called during assembly loading, so do everything using the low-level
	 * metadata APIs.
	 */

	image = assembly->image;
	/* Dynamic images would need to go through the AssemblyBuilder's
	 * CustomAttributeBuilder array.  Going through the tables below
	 * definitely won't work. */
	g_assert (!image_is_dynamic (image));
	idx = 1; /* there is only one assembly */
	idx <<= MONO_CUSTOM_ATTR_BITS;
	idx |= MONO_CUSTOM_ATTR_ASSEMBLY;

	/* Inlined from mono_custom_attrs_from_index_checked () */
	ca = &image->tables [MONO_TABLE_CUSTOMATTRIBUTE];
	i = mono_metadata_custom_attrs_from_index (image, idx);
	if (!i)
		return;
	i --;
	gboolean stop_iterating = FALSE;
	while (!stop_iterating && i < ca->rows) {
		if (mono_metadata_decode_row_col (ca, i, MONO_CUSTOM_ATTR_PARENT) != idx)
			break;
		mono_metadata_decode_row (ca, i, cols, MONO_CUSTOM_ATTR_SIZE);
		i ++;
		mtoken = cols [MONO_CUSTOM_ATTR_TYPE] >> MONO_CUSTOM_ATTR_TYPE_BITS;
		switch (cols [MONO_CUSTOM_ATTR_TYPE] & MONO_CUSTOM_ATTR_TYPE_MASK) {
		case MONO_CUSTOM_ATTR_TYPE_METHODDEF:
			mtoken |= MONO_TOKEN_METHOD_DEF;
			break;
		case MONO_CUSTOM_ATTR_TYPE_MEMBERREF:
			mtoken |= MONO_TOKEN_MEMBER_REF;
			break;
		default:
			g_warning ("Unknown table for custom attr type %08x", cols [MONO_CUSTOM_ATTR_TYPE]);
			continue;
		}

		const char *nspace = NULL;
		const char *name = NULL;
		guint32 assembly_token = 0;

		if (!custom_attr_class_name_from_method_token (image, mtoken, &assembly_token, &nspace, &name))
			continue;

		stop_iterating = func (image, assembly_token, nspace, name, mtoken, user_data);
	}
}

static void
init_weak_fields_inner (MonoImage *image, GHashTable *indexes)
{
	MonoTableInfo *tdef;
	MonoError error;
	MonoClass *klass = NULL;
	guint32 memberref_index = -1;
	int first_method_idx = -1;
	int method_count = -1;

	if (image == mono_get_corlib ()) {
		/* Typedef */
		klass = mono_class_from_name_checked (image, "System", "WeakAttribute", &error);
		if (!is_ok (&error)) {
			mono_error_cleanup (&error);
			return;
		}
		if (!klass)
			return;
		first_method_idx = mono_class_get_first_method_idx (klass);
		method_count = mono_class_get_method_count (klass);

		tdef = &image->tables [MONO_TABLE_CUSTOMATTRIBUTE];
		guint32 parent, field_idx, col, mtoken, idx;
		for (int i = 0; i < tdef->rows; ++i) {
			parent = mono_metadata_decode_row_col (tdef, i, MONO_CUSTOM_ATTR_PARENT);
			if ((parent & MONO_CUSTOM_ATTR_MASK) != MONO_CUSTOM_ATTR_FIELDDEF)
				continue;

			col = mono_metadata_decode_row_col (tdef, i, MONO_CUSTOM_ATTR_TYPE);
			mtoken = col >> MONO_CUSTOM_ATTR_TYPE_BITS;
			/* 1 based index */
			idx = mtoken - 1;
			if ((col & MONO_CUSTOM_ATTR_TYPE_MASK) == MONO_CUSTOM_ATTR_TYPE_METHODDEF) {
				field_idx = parent >> MONO_CUSTOM_ATTR_BITS;
				if (idx >= first_method_idx && idx < first_method_idx + method_count)
					g_hash_table_insert (indexes, GUINT_TO_POINTER (field_idx), GUINT_TO_POINTER (1));
			}
		}
	} else {
		/* Memberref pointing to a typeref */
		tdef = &image->tables [MONO_TABLE_MEMBERREF];

		/* Check whenever the assembly references the WeakAttribute type */
		gboolean found = FALSE;
		tdef = &image->tables [MONO_TABLE_TYPEREF];
		for (int i = 0; i < tdef->rows; ++i) {
			guint32 string_offset = mono_metadata_decode_row_col (tdef, i, MONO_TYPEREF_NAME);
			const char *name = mono_metadata_string_heap (image, string_offset);
			if (!strcmp (name, "WeakAttribute")) {
				found = TRUE;
				break;
			}
		}

		if (!found)
			return;

		/* Find the memberref pointing to a typeref */
		tdef = &image->tables [MONO_TABLE_MEMBERREF];
		for (int i = 0; i < tdef->rows; ++i) {
			guint32 cols [MONO_MEMBERREF_SIZE];
			const char *sig;

			mono_metadata_decode_row (tdef, i, cols, MONO_MEMBERREF_SIZE);
			sig = mono_metadata_blob_heap (image, cols [MONO_MEMBERREF_SIGNATURE]);
			mono_metadata_decode_blob_size (sig, &sig);

			guint32 nindex = cols [MONO_MEMBERREF_CLASS] >> MONO_MEMBERREF_PARENT_BITS;
			guint32 class_index = cols [MONO_MEMBERREF_CLASS] & MONO_MEMBERREF_PARENT_MASK;
			const char *fname = mono_metadata_string_heap (image, cols [MONO_MEMBERREF_NAME]);

			if (!strcmp (fname, ".ctor") && class_index == MONO_MEMBERREF_PARENT_TYPEREF) {
				MonoTableInfo *typeref_table = &image->tables [MONO_TABLE_TYPEREF];
				guint32 cols [MONO_TYPEREF_SIZE];

				mono_metadata_decode_row (typeref_table, nindex - 1, cols, MONO_TYPEREF_SIZE);

				const char *name = mono_metadata_string_heap (image, cols [MONO_TYPEREF_NAME]);
				const char *nspace = mono_metadata_string_heap (image, cols [MONO_TYPEREF_NAMESPACE]);

				if (!strcmp (nspace, "System") && !strcmp (name, "WeakAttribute")) {
					MonoClass *klass = mono_class_from_typeref (image, MONO_TOKEN_TYPE_REF | nindex);
					g_assert (!strcmp (klass->name, "WeakAttribute"));
					/* Allow a testing dll as well since some profiles don't have WeakAttribute */
					if (klass && (klass->image == mono_get_corlib () || strstr (klass->image->name, "Mono.Runtime.Testing"))) {
						/* Sanity check that it only has 1 ctor */
						gpointer iter = NULL;
						int count = 0;
						MonoMethod *method;
						while ((method = mono_class_get_methods (klass, &iter))) {
							if (!strcmp (method->name, ".ctor"))
								count ++;
						}
						count ++;
						memberref_index = i;
						break;
					}
				}
			}
		}
		if (memberref_index == -1)
			return;

		tdef = &image->tables [MONO_TABLE_CUSTOMATTRIBUTE];
		guint32 parent, field_idx, col, mtoken, idx;
		for (int i = 0; i < tdef->rows; ++i) {
			parent = mono_metadata_decode_row_col (tdef, i, MONO_CUSTOM_ATTR_PARENT);
			if ((parent & MONO_CUSTOM_ATTR_MASK) != MONO_CUSTOM_ATTR_FIELDDEF)
				continue;

			col = mono_metadata_decode_row_col (tdef, i, MONO_CUSTOM_ATTR_TYPE);
			mtoken = col >> MONO_CUSTOM_ATTR_TYPE_BITS;
			/* 1 based index */
			idx = mtoken - 1;
			field_idx = parent >> MONO_CUSTOM_ATTR_BITS;
			if ((col & MONO_CUSTOM_ATTR_TYPE_MASK) == MONO_CUSTOM_ATTR_TYPE_MEMBERREF) {
				if (idx == memberref_index)
					g_hash_table_insert (indexes, GUINT_TO_POINTER (field_idx), GUINT_TO_POINTER (1));
			}
		}
	}
}

/*
 * mono_assembly_init_weak_fields:
 *
 *   Initialize the image->weak_field_indexes hash.
 */
void
mono_assembly_init_weak_fields (MonoImage *image)
{
	if (image->weak_fields_inited)
		return;

	GHashTable *indexes = NULL;

	if (mono_get_runtime_callbacks ()->get_weak_field_indexes)
		indexes = mono_get_runtime_callbacks ()->get_weak_field_indexes (image);
	if (!indexes) {
		indexes = g_hash_table_new (NULL, NULL);

		/*
		 * To avoid lookups for every field, we scan the customattr table for entries whose
		 * parent is a field and whose type is WeakAttribute.
		 */
		init_weak_fields_inner (image, indexes);
	}

	mono_image_lock (image);
	if (!image->weak_fields_inited) {
		image->weak_field_indexes = indexes;
		mono_memory_barrier ();
		image->weak_fields_inited = TRUE;
	} else {
		g_hash_table_destroy (indexes);
	}
	mono_image_unlock (image);
}

/*
 * mono_assembly_is_weak_field:
 *
 *   Return whenever the FIELD table entry with the 1-based index FIELD_IDX has
 * a [Weak] attribute.
 */
gboolean
mono_assembly_is_weak_field (MonoImage *image, guint32 field_idx)
{
	if (image->dynamic)
		return FALSE;

	mono_assembly_init_weak_fields (image);

	/* The hash is not mutated, no need to lock */
	return g_hash_table_lookup (image->weak_field_indexes, GINT_TO_POINTER (field_idx)) != NULL;
}
