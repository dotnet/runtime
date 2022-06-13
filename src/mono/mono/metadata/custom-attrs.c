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
#include "mono/metadata/class-init.h"
#include "mono/metadata/class-internals.h"
#include "mono/metadata/gc-internals.h"
#include "mono/metadata/mono-endian.h"
#include "mono/metadata/object-internals.h"
#include "mono/metadata/custom-attrs-internals.h"
#include "mono/metadata/sre-internals.h"
#include "mono/metadata/reflection-internals.h"
#include "mono/metadata/tabledefs.h"
#include "mono/metadata/tokentype.h"
#include "mono/metadata/icall-decl.h"
#include "mono/metadata/metadata-update.h"
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

#define CATTR_TYPE_SYSTEM_TYPE 0x50
#define CATTR_BOXED_VALUETYPE_PREFIX 0x51
#define CATTR_TYPE_FIELD 0x53
#define CATTR_TYPE_PROPERTY 0x54

static gboolean type_is_reference (MonoType *type);

static GENERATE_GET_CLASS_WITH_CACHE (custom_attribute_typed_argument, "System.Reflection", "CustomAttributeTypedArgument");
static GENERATE_GET_CLASS_WITH_CACHE (custom_attribute_named_argument, "System.Reflection", "CustomAttributeNamedArgument");
static GENERATE_TRY_GET_CLASS_WITH_CACHE (customattribute_data, "System.Reflection", "RuntimeCustomAttributeData");

static MonoCustomAttrInfo*
mono_custom_attrs_from_builders_handle (MonoImage *alloc_img, MonoImage *image, MonoArrayHandle cattrs);

static gboolean
bcheck_blob (const char *ptr, int bump, const char *endp, MonoError *error);

static gboolean
decode_blob_value_checked (const char *ptr, const char *endp, guint32 *size_out, const char **retp, MonoError *error);

static guint32
custom_attrs_idx_from_class (MonoClass *klass);

static guint32
custom_attrs_idx_from_method (MonoMethod *method);

static void
metadata_foreach_custom_attr_from_index (MonoImage *image, guint32 idx, MonoAssemblyMetadataCustomAttrIterFunc func, gpointer user_data);


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
custom_attr_visible (MonoImage *image, MonoReflectionCustomAttrHandle cattr, MonoReflectionMethodHandle ctor_handle, MonoMethod **ctor_method)
// ctor_handle is local to this function, allocated by its caller for efficiency.
{
	MONO_REQ_GC_UNSAFE_MODE;

	MONO_HANDLE_GET (ctor_handle, cattr, ctor);
	*ctor_method = MONO_HANDLE_GETVAL (ctor_handle, method);

	/* FIXME: Need to do more checks */
	if (*ctor_method) {
		MonoClass *klass = (*ctor_method)->klass;
		if (m_class_get_image (klass) != image) {
			const int visibility = (mono_class_get_flags (klass) & TYPE_ATTRIBUTE_VISIBILITY_MASK);
			if ((visibility != TYPE_ATTRIBUTE_PUBLIC) && (visibility != TYPE_ATTRIBUTE_NESTED_PUBLIC))
				return FALSE;
		}
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
	if (G_UNLIKELY (m_field_is_from_update (field)))
		return mono_metadata_update_get_field_idx (field);
	int fcount = mono_class_get_field_count (klass);
	MonoClassField *klass_fields = m_class_get_fields (klass);
	int index = GPTRDIFF_TO_INT (field - klass_fields);
	if (index > fcount)
		return 0;

	g_assert (field == &klass_fields [index]);
	return mono_class_get_first_field_idx (klass) + 1 + index;
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
	ERROR_DECL (inner_error);
	MonoAssemblyLoadContext *alc = mono_alc_get_ambient ();
	MonoType *t = mono_reflection_type_from_name_checked (n, alc, image, inner_error);
	if (!t) {
		mono_error_set_type_load_name (error, g_strdup(n), NULL,
					       "Could not load %s %s while decoding custom attribute: %s",
					       is_enum ? "enum type": "type",
					       n,
					       mono_error_get_message (inner_error));
		mono_error_cleanup (inner_error);
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
	return mono_class_from_mono_type_internal (t);
}

static MonoType*
load_cattr_type (MonoImage *image, MonoType *t, gboolean header, const char *p, const char *boundp, const char **end, MonoError *error, guint32 *slen)
{
	MonoType *res;
	char *n;

	if (header) {
		if (!bcheck_blob (p, 0, boundp, error))
			return NULL;
MONO_DISABLE_WARNING(4310) // cast truncates constant value
		if (*p == (char)0xFF) {
			*end = p + 1;
			return NULL;
		}
MONO_RESTORE_WARNING
	}

	if (!decode_blob_value_checked (p, boundp, slen, &p, error))
		return NULL;
	if (*slen > 0 && !bcheck_blob (p, *slen - 1, boundp, error))
		return NULL;
	n = (char *)g_memdup (p, *slen + 1);
	n [*slen] = 0;
	res = cattr_type_from_name (n, image, FALSE, error);
	g_free (n);
	return_val_if_nok (error, NULL);

	*end = p + *slen;

	return res;
}

/*
 * If OUT_OBJ is non-NULL, created objects are stored into it and NULL is returned.
 * If OUT_OBJ is NULL, assert if objects were to be created.
 */
static void*
load_cattr_value (MonoImage *image, MonoType *t, MonoObject **out_obj, const char *p, const char *boundp, const char **end, MonoError *error)
{
	int type = t->type;
	guint32 slen;
	MonoClass *tklass = t->data.klass;

	if (out_obj)
		*out_obj = NULL;
	g_assert (boundp);
	error_init (error);

	if (type == MONO_TYPE_GENERICINST) {
		MonoGenericClass * mgc = t->data.generic_class;
		MonoClass * cc = mgc->container_class;
		if (m_class_is_enumtype (cc)) {
			tklass = m_class_get_element_class (cc);
			t = m_class_get_byval_arg (tklass);
			type = t->type;
		} else {
			g_error ("Unhandled type of generic instance in load_cattr_value: %s", m_class_get_name (cc));
		}
	}

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
		if (m_class_is_enumtype (t->data.klass)) {
			type = mono_class_enum_basetype_internal (t->data.klass)->type;
			goto handle_enum;
		} else {
			MonoClass *k =  t->data.klass;

			if (mono_is_corlib_image (m_class_get_image (k)) && strcmp (m_class_get_name_space (k), "System") == 0 && strcmp (m_class_get_name (k), "DateTime") == 0){
				guint64 *val = (guint64 *)g_malloc (sizeof (guint64));
				if (!bcheck_blob (p, 7, boundp, error))
					return NULL;
				*val = read64 (p);
				*end = p + 8;
				return val;
			}
		}
		g_error ("generic valutype %s not handled in custom attr value decoding", m_class_get_name (t->data.klass));
		break;

	case MONO_TYPE_STRING: {
		if (!bcheck_blob (p, 0, boundp, error))
			return NULL;
MONO_DISABLE_WARNING (4310) // cast truncates constant value
		if (*p == (char)0xFF) {
			*end = p + 1;
			return NULL;
		}
MONO_RESTORE_WARNING
		if (!decode_blob_value_checked (p, boundp, &slen, &p, error))
			return NULL;
		if (slen > 0 && !bcheck_blob (p, slen - 1, boundp, error))
			return NULL;
		*end = p + slen;
		// https://bugzilla.xamarin.com/show_bug.cgi?id=60848
		// Custom attribute strings are encoded as wtf-8 instead of utf-8.
		// If we decode using utf-8 like the spec says, we will silently fail
		//  to decode some attributes in assemblies that Windows .NET Framework
		//  and CoreCLR both manage to decode.
		// See https://simonsapin.github.io/wtf-8/ for a description of wtf-8.
		// Always use string.Empty for empty strings
		if (slen == 0)
			*out_obj = (MonoObject*)mono_string_empty_internal (mono_domain_get ());
		else
			*out_obj = (MonoObject*)mono_string_new_wtf8_len_checked (p, slen, error);
		return NULL;
	}
	case MONO_TYPE_CLASS: {
		MonoType *cattr_type  = load_cattr_type (image, t, TRUE, p, boundp, end, error, &slen);
		if (!cattr_type )
			return NULL;
		*out_obj = (MonoObject*)mono_type_get_object_checked (cattr_type , error);
		return NULL;
	}
	case MONO_TYPE_OBJECT: {
		if (!bcheck_blob (p, 0, boundp, error))
			return NULL;
		char subt = *p++;
		MonoObject *obj;
		MonoClass *subc = NULL;
		void *val;

		if (subt == CATTR_TYPE_SYSTEM_TYPE) {
			MonoType *cattr_type = load_cattr_type (image, t, FALSE, p, boundp, end, error, &slen);
			if (!cattr_type)
				return NULL;
			*out_obj = (MonoObject*)mono_type_get_object_checked (cattr_type, error);
			return NULL;
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
			if (etype == CATTR_TYPE_SYSTEM_TYPE) {
				tklass = mono_defaults.systemtype_class;
			} else if (etype == MONO_TYPE_ENUM) {
				tklass = load_cattr_enum_type (image, p, boundp, &p, error);
				if (!is_ok (error))
					return NULL;
			} else {
				if (etype == CATTR_BOXED_VALUETYPE_PREFIX)
					/* See Partition II, Appendix B3 */
					etype = MONO_TYPE_OBJECT;
				simple_type.type = (MonoTypeEnum)etype;
				tklass = mono_class_from_mono_type_internal (&simple_type);
			}
			goto handle_enum;
		} else if (subt == MONO_TYPE_ENUM) {
			char *n;
			MonoType *enum_type;
			if (!decode_blob_value_checked (p, boundp, &slen, &p, error))
				return NULL;
			if (slen > 0 && !bcheck_blob (p, slen - 1, boundp, error))
				return NULL;
			n = (char *)g_memdup (p, slen + 1);
			n [slen] = 0;
			enum_type = cattr_type_from_name (n, image, FALSE, error);
			g_free (n);
			return_val_if_nok (error, NULL);
			p += slen;
			subc = mono_class_from_mono_type_internal (enum_type);
		} else if (subt >= MONO_TYPE_BOOLEAN && subt <= MONO_TYPE_R8) {
			MonoType simple_type = {{0}};
			simple_type.type = (MonoTypeEnum)subt;
			subc = mono_class_from_mono_type_internal (&simple_type);
		} else {
			g_error ("Unknown type 0x%02x for object type encoding in custom attr", subt);
		}
		val = load_cattr_value (image, m_class_get_byval_arg (subc), NULL, p, boundp, end, error);
		if (is_ok (error)) {
			obj = mono_object_new_checked (subc, error);
			g_assert (!m_class_has_references (subc));
			if (is_ok (error))
				mono_gc_memmove_atomic (mono_object_get_data (obj), val, mono_class_value_size (subc, NULL));
			g_assert (out_obj);
			*out_obj = obj;
		}

		g_free (val);
		return NULL;
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

		arr = mono_array_new_checked (tklass, alen, error);
		return_val_if_nok (error, NULL);

		basetype = m_class_get_byval_arg (tklass)->type;
		if (basetype == MONO_TYPE_VALUETYPE && m_class_is_enumtype (tklass))
			basetype = mono_class_enum_basetype_internal (tklass)->type;

		if (basetype == MONO_TYPE_GENERICINST) {
			MonoGenericClass * mgc = m_class_get_byval_arg (tklass)->data.generic_class;
			MonoClass * cc = mgc->container_class;
			if (m_class_is_enumtype (cc)) {
				basetype = m_class_get_byval_arg (m_class_get_element_class (cc))->type;
			} else {
				g_error ("Unhandled type of generic instance in load_cattr_value: %s[]", m_class_get_name (cc));
			}
		}

		switch (basetype) {
		case MONO_TYPE_U1:
		case MONO_TYPE_I1:
		case MONO_TYPE_BOOLEAN:
			for (i = 0; i < alen; i++) {
				if (!bcheck_blob (p, 0, boundp, error))
					return NULL;
				MonoBoolean val = *p++;
				mono_array_set_internal (arr, MonoBoolean, i, val);
			}
			break;
		case MONO_TYPE_CHAR:
		case MONO_TYPE_U2:
		case MONO_TYPE_I2:
			for (i = 0; i < alen; i++) {
				if (!bcheck_blob (p, 1, boundp, error))
					return NULL;
				guint16 val = read16 (p);
				mono_array_set_internal (arr, guint16, i, val);
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
				mono_array_set_internal (arr, guint32, i, val);
				p += 4;
			}
			break;
		case MONO_TYPE_R8:
			for (i = 0; i < alen; i++) {
				if (!bcheck_blob (p, 7, boundp, error))
					return NULL;
				double val;
				readr8 (p, &val);
				mono_array_set_internal (arr, double, i, val);
				p += 8;
			}
			break;
		case MONO_TYPE_U8:
		case MONO_TYPE_I8:
			for (i = 0; i < alen; i++) {
				if (!bcheck_blob (p, 7, boundp, error))
					return NULL;
				guint64 val = read64 (p);
				mono_array_set_internal (arr, guint64, i, val);
				p += 8;
			}
			break;
		case MONO_TYPE_CLASS:
		case MONO_TYPE_OBJECT:
		case MONO_TYPE_STRING:
		case MONO_TYPE_SZARRAY: {
			HANDLE_FUNCTION_ENTER ();
			MONO_HANDLE_NEW (MonoArray, arr);

			for (i = 0; i < alen; i++) {
				MonoObject *item = NULL;
				load_cattr_value (image, m_class_get_byval_arg (tklass), &item, p, boundp, &p, error);
				if (!is_ok (error))
					return NULL;
				mono_array_setref_internal (arr, i, item);
			}
			HANDLE_FUNCTION_RETURN ();
			break;
		}
		default:
			g_error ("Type 0x%02x not handled in custom attr array decoding", basetype);
		}
		*end = p;
		g_assert (out_obj);
		*out_obj = (MonoObject*)arr;

		return NULL;
	}
	default:
		g_error ("Type 0x%02x not handled in custom attr value decoding", type);
	}
	return NULL;
}


/*
 * load_cattr_value_noalloc:
 *
 * Loads custom attribute values without mono allocation (invoked when AOT compiling).
 * Returns MonoCustomAttrValue:
 * 	- for primitive types:
 * 		- (bools, ints, and enums) a pointer to the value is returned.
 * 		- for types, the address of the loaded type is returned (DON'T FREE).
 *		- for strings, the address in the metadata blob is returned (DON'T FREE).
 * 	- for arrays:
 * 		- MonoCustomAttrValueArray* is returned.
 */
static MonoCustomAttrValue*
load_cattr_value_noalloc (MonoImage *image, MonoType *t, const char *p, const char *boundp, const char **end, MonoError *error)
{
	int type = t->type;
	guint32 slen;
	MonoClass *tklass = t->data.klass;
	MonoCustomAttrValue* result = (MonoCustomAttrValue *)g_malloc (sizeof (MonoCustomAttrValue));

	g_assert (boundp);
	error_init (error);

	if (type == MONO_TYPE_GENERICINST) {
		MonoGenericClass * mgc = t->data.generic_class;
		MonoClass * cc = mgc->container_class;
		if (m_class_is_enumtype (cc)) {
			tklass = m_class_get_element_class (cc);
			t = m_class_get_byval_arg (tklass);
			type = t->type;
		} else {
			g_error ("Unhandled type of generic instance in load_cattr_value_noalloc: %s", m_class_get_name (cc));
		}
	}
	result->type = type;

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
		result->value.primitive = bval;
		return result;
	}
	case MONO_TYPE_CHAR:
	case MONO_TYPE_U2:
	case MONO_TYPE_I2: {
		guint16 *val = (guint16 *)g_malloc (sizeof (guint16));
		if (!bcheck_blob (p, 1, boundp, error))
			return NULL;
		*val = read16 (p);
		*end = p + 2;
		result->value.primitive = val;
		return result;
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
		result->value.primitive = val;
		return result;
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
		result->value.primitive = val;
		return result;
	}
	case MONO_TYPE_R8: {
		double *val = (double *)g_malloc (sizeof (double));
		if (!bcheck_blob (p, 7, boundp, error))
			return NULL;
		readr8 (p, val);
		*end = p + 8;
		result->value.primitive = val;
		return result;
	}
	case MONO_TYPE_VALUETYPE:
		if (m_class_is_enumtype (t->data.klass)) {
			type = mono_class_enum_basetype_internal (t->data.klass)->type;
			goto handle_enum;
		} else {
			MonoClass *k =  t->data.klass;

			if (mono_is_corlib_image (m_class_get_image (k)) && strcmp (m_class_get_name_space (k), "System") == 0 && strcmp (m_class_get_name (k), "DateTime") == 0){
				guint64 *val = (guint64 *)g_malloc (sizeof (guint64));
				if (!bcheck_blob (p, 7, boundp, error))
					return NULL;
				*val = read64 (p);
				*end = p + 8;
				result->value.primitive = val;
				return result;
			}
		}
		g_error ("generic valutype %s not handled in custom attr value decoding", m_class_get_name (t->data.klass));
		break;

	case MONO_TYPE_STRING: {
		const char *start = p;

		if (!bcheck_blob (p, 0, boundp, error))
			return NULL;
MONO_DISABLE_WARNING (4310) // cast truncates constant value
		if (*p == (char)0xFF) {
			*end = p + 1;
			return NULL;
		}
MONO_RESTORE_WARNING
		if (!decode_blob_value_checked (p, boundp, &slen, &p, error))
			return NULL;
		if (slen > 0 && !bcheck_blob (p, slen - 1, boundp, error))
			return NULL;
		*end = p + slen;
		result->value.primitive = (gpointer)start;
		return result;
	}
	case MONO_TYPE_CLASS: {
		result->value.primitive = load_cattr_type (image, t, TRUE, p, boundp, end, error, &slen);;
		return result;
	}
	case MONO_TYPE_OBJECT: {
		if (!bcheck_blob (p, 0, boundp, error))
			return NULL;
		char subt = *p++;
		MonoClass *subc = NULL;

		if (subt == CATTR_TYPE_SYSTEM_TYPE) {
			result->value.primitive = load_cattr_type (image, t, FALSE, p, boundp, end, error, &slen);
			return result;
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
			if (etype == CATTR_TYPE_SYSTEM_TYPE) {
				tklass = mono_defaults.systemtype_class;
			} else if (etype == MONO_TYPE_ENUM) {
				tklass = load_cattr_enum_type (image, p, boundp, &p, error);
				if (!is_ok (error))
					return NULL;
			} else {
				if (etype == CATTR_BOXED_VALUETYPE_PREFIX)
					/* See Partition II, Appendix B3 */
					etype = MONO_TYPE_OBJECT;
				simple_type.type = (MonoTypeEnum)etype;
				tklass = mono_class_from_mono_type_internal (&simple_type);
			}
			goto handle_enum;
		} else if (subt == MONO_TYPE_ENUM) {
			char *n;
			MonoType *enum_type;
			if (!decode_blob_value_checked (p, boundp, &slen, &p, error))
				return NULL;
			if (slen > 0 && !bcheck_blob (p, slen - 1, boundp, error))
				return NULL;
			n = (char *)g_memdup (p, slen + 1);
			n [slen] = 0;
			enum_type = cattr_type_from_name (n, image, FALSE, error);
			g_free (n);
			return_val_if_nok (error, NULL);
			p += slen;
			subc = mono_class_from_mono_type_internal (enum_type);
		} else if (subt >= MONO_TYPE_BOOLEAN && subt <= MONO_TYPE_R8) {
			MonoType simple_type = {{0}};
			simple_type.type = (MonoTypeEnum)subt;
			subc = mono_class_from_mono_type_internal (&simple_type);
		} else {
			g_error ("Unknown type 0x%02x for object type encoding in custom attr", subt);
		}
		result->value.primitive = load_cattr_value_noalloc (image, m_class_get_byval_arg (subc), p, boundp, end, error);
		return result;
	}
	case MONO_TYPE_SZARRAY: {
		guint32 i, alen;

		if (!bcheck_blob (p, 3, boundp, error))
			return NULL;
		alen = read32 (p);
		p += 4;
		if (alen == 0xffffffff) {
			*end = p;
			return NULL;
		}

		result->value.array = g_malloc (sizeof (MonoCustomAttrValueArray) + alen * sizeof (MonoCustomAttrValue));
		result->value.array->len = alen;

		for (i = 0; i < alen; i++) {
			MonoCustomAttrValue* array_element = load_cattr_value_noalloc (image, m_class_get_byval_arg (tklass), p, boundp, &p, error);
			if (!is_ok (error))
				return NULL;
			result->value.array->values[i] = *array_element;
		}
		*end = p;
		return result;
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

	if (is_ref) {
		MonoObject *obj = NULL;
		gpointer val = load_cattr_value (image, t, &obj, p, boundp, end, error);
		if (!is_ok (error))
			return NULL;
		g_assert (!val);
		return obj;
	} else {
		void *val = load_cattr_value (image, t, NULL, p, boundp, end, error);
		if (!is_ok (error))
			return NULL;

		MonoObject *boxed = mono_value_box_checked (mono_class_from_mono_type_internal (t), val, error);
		g_free (val);
		return boxed;
	}
}

static MonoObject*
create_cattr_typed_arg (MonoType *t, MonoObject *val, MonoError *error)
{
	MonoObject *retval;
	void *params [2], *unboxed;

	error_init (error);

	HANDLE_FUNCTION_ENTER ();

	MONO_STATIC_POINTER_INIT (MonoMethod, ctor)

		ctor = mono_class_get_method_from_name_checked (mono_class_get_custom_attribute_typed_argument_class (), ".ctor", 2, 0, error);
		mono_error_assert_ok (error);

	MONO_STATIC_POINTER_INIT_END (MonoMethod, ctor)

	params [0] = mono_type_get_object_checked (t, error);
	return_val_if_nok (error, NULL);
	MONO_HANDLE_PIN ((MonoObject*)params [0]);

	params [1] = val;
	retval = mono_object_new_checked (mono_class_get_custom_attribute_typed_argument_class (), error);
	return_val_if_nok (error, NULL);
	MONO_HANDLE_PIN (retval);

	unboxed = mono_object_unbox_internal (retval);

	mono_runtime_invoke_checked (ctor, unboxed, params, error);
	return_val_if_nok (error, NULL);

	HANDLE_FUNCTION_RETURN ();

	return retval;
}

static MonoObject*
create_cattr_named_arg (void *minfo, MonoObject *typedarg, MonoError *error)
{
	MonoObject *retval;
	void *unboxed, *params [2];

	error_init (error);

	HANDLE_FUNCTION_ENTER ();

	MONO_STATIC_POINTER_INIT (MonoMethod, ctor)

		ctor = mono_class_get_method_from_name_checked (mono_class_get_custom_attribute_named_argument_class (), ".ctor", 2, 0, error);
		mono_error_assert_ok (error);

	MONO_STATIC_POINTER_INIT_END (MonoMethod, ctor)

	params [0] = minfo;
	params [1] = typedarg;
	retval = mono_object_new_checked (mono_class_get_custom_attribute_named_argument_class (), error);
	return_val_if_nok (error, NULL);
	MONO_HANDLE_PIN (retval);

	unboxed = mono_object_unbox_internal (retval);

	mono_runtime_invoke_checked (ctor, unboxed, params, error);
	return_val_if_nok (error, NULL);

	HANDLE_FUNCTION_RETURN ();

	return retval;
}

static MonoCustomAttrInfo*
mono_custom_attrs_from_builders_handle (MonoImage *alloc_img, MonoImage *image, MonoArrayHandle cattrs)
{
	MONO_REQ_GC_UNSAFE_MODE;

	if (!MONO_HANDLE_BOOL (cattrs))
		return NULL;

	HANDLE_FUNCTION_ENTER ();

	/* FIXME: check in assembly the Run flag is set */

	MonoReflectionCustomAttrHandle cattr = MONO_HANDLE_NEW (MonoReflectionCustomAttr, NULL);
	MonoArrayHandle cattr_data = MONO_HANDLE_NEW (MonoArray, NULL);
	MonoReflectionMethodHandle ctor_handle = MONO_HANDLE_NEW (MonoReflectionMethod, NULL);

	int const count = GUINTPTR_TO_INT (mono_array_handle_length (cattrs));
	MonoMethod *ctor_method =  NULL;

	/* Skip nonpublic attributes since MS.NET seems to do the same */
	/* FIXME: This needs to be done more globally */
	int count_visible = 0;
	for (int i = 0; i < count; ++i) {
		MONO_HANDLE_ARRAY_GETREF (cattr, cattrs, i);
		count_visible += custom_attr_visible (image, cattr, ctor_handle, &ctor_method);
	}

	MonoCustomAttrInfo *ainfo;
	ainfo = (MonoCustomAttrInfo *)mono_image_g_malloc0 (alloc_img, MONO_SIZEOF_CUSTOM_ATTR_INFO + sizeof (MonoCustomAttrEntry) * count_visible);

	ainfo->image = image;
	ainfo->num_attrs = count_visible;
	ainfo->cached = alloc_img != NULL;
	int index = 0;
	for (int i = 0; i < count; ++i) {
		MONO_HANDLE_ARRAY_GETREF (cattr, cattrs, i);
		if (!custom_attr_visible (image, cattr, ctor_handle, &ctor_method))
			continue;

		if (image_is_dynamic (image))
			mono_reflection_resolution_scope_from_image ((MonoDynamicImage *)image->assembly->image, m_class_get_image (ctor_method->klass));

		MONO_HANDLE_GET (cattr_data, cattr, data);
		unsigned char *saved = (unsigned char *)mono_image_alloc (image, GUINTPTR_TO_UINT (mono_array_handle_length (cattr_data)));
		MonoGCHandle gchandle = NULL;
		memcpy (saved, MONO_ARRAY_HANDLE_PIN (cattr_data, char, 0, &gchandle), GUINTPTR_TO_UINT32 (mono_array_handle_length (cattr_data)));
		mono_gchandle_free_internal (gchandle);
		ainfo->attrs [index].ctor = ctor_method;
		g_assert (ctor_method);
		ainfo->attrs [index].data = saved;
		ainfo->attrs [index].data_size = GUINTPTR_TO_UINT32 (mono_array_handle_length (cattr_data));
		index ++;
	}
	g_assert (index == count_visible);
	HANDLE_FUNCTION_RETURN_VAL (ainfo);
}

MonoCustomAttrInfo*
mono_custom_attrs_from_builders (MonoImage *alloc_img, MonoImage *image, MonoArray* cattrs)
{
	HANDLE_FUNCTION_ENTER ();
	MonoCustomAttrInfo* const result = mono_custom_attrs_from_builders_handle (alloc_img, image, MONO_HANDLE_NEW (MonoArray, cattrs));
	HANDLE_FUNCTION_RETURN_VAL (result);
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

static MonoObjectHandle
create_custom_attr (MonoImage *image, MonoMethod *method, const guchar *data, guint32 len, MonoError *error)
{
	HANDLE_FUNCTION_ENTER ();

	const char *p = (const char*)data;
	const char *data_end = (const char*)data + len;
	const char *named;
	guint32 i, j, num_named;
	MonoObjectHandle attr = NULL_HANDLE;
	void *params_buf [32];
	void **params = NULL;
	MonoMethodSignature *sig;
	MonoClassField *field = NULL;
	char *name = NULL;
	void *pparams [1] = { NULL };
	MonoType *prop_type = NULL;
	void *val = NULL; // FIXMEcoop is this a raw pointer or a value?

	error_init (error);

	mono_class_init_internal (method->klass);

	if (len == 0) {
		attr = mono_object_new_handle (method->klass, error);
		goto_if_nok (error, fail);

		mono_runtime_invoke_handle_void (method, attr, NULL, error);
		goto_if_nok (error, fail);

		goto exit;
	}

	if (len < 2 || read16 (p) != 0x0001) /* Prolog */
		goto fail;

	/*g_print ("got attr %s\n", method->klass->name);*/

	sig = mono_method_signature_internal (method);
	if (sig->param_count < 32) {
		params = params_buf;
		memset (params, 0, sizeof (void*) * sig->param_count);
	} else {
		/* Allocate using GC so it gets GC tracking */
		params = (void **)mono_gc_alloc_fixed (sig->param_count * sizeof (void*), MONO_GC_DESCRIPTOR_NULL, MONO_ROOT_SOURCE_REFLECTION, NULL, "Reflection Custom Attribute Parameters");
	}

	/* skip prolog */
	p += 2;
	for (i = 0; i < mono_method_signature_internal (method)->param_count; ++i) {
		MonoObject *param_obj;
		params [i] = load_cattr_value (image, mono_method_signature_internal (method)->params [i], &param_obj, p, data_end, &p, error);
		if (param_obj)
			params [i] = param_obj;
		goto_if_nok (error, fail);
	}

	named = p;
	attr = mono_object_new_handle (method->klass, error);
	goto_if_nok (error, fail);

	(void)mono_runtime_try_invoke_handle (method, attr, params, error);
	goto_if_nok (error, fail);

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
		char named_type, data_type;
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
		if (named_type == CATTR_TYPE_FIELD) {
			/* how this fail is a blackbox */
			field = mono_class_get_field_from_name_full (mono_handle_class (attr), name, NULL);
			if (!field) {
				mono_error_set_generic_error (error, "System.Reflection", "CustomAttributeFormatException", "Could not find a field with name %s", name);
				goto fail;
			}

			MonoObject *param_obj;
			val = load_cattr_value (image, field->type, &param_obj, named, data_end, &named, error);
			if (param_obj)
				val = param_obj;
			goto_if_nok (error, fail);

			mono_field_set_value_internal (MONO_HANDLE_RAW (attr), field, val); // FIXMEcoop
		} else if (named_type == CATTR_TYPE_PROPERTY) {
			MonoProperty *prop;
			prop = mono_class_get_property_from_name_internal (mono_handle_class (attr), name);
			if (!prop) {
				mono_error_set_generic_error (error, "System.Reflection", "CustomAttributeFormatException", "Could not find a property with name %s", name);
				goto fail;
			}

			if (!prop->set) {
				mono_error_set_generic_error (error, "System.Reflection", "CustomAttributeFormatException", "Could not find the setter for %s", name);
				goto fail;
			}

			/* can we have more that 1 arg in a custom attr named property? */
			prop_type = prop->get? mono_method_signature_internal (prop->get)->ret :
			     mono_method_signature_internal (prop->set)->params [mono_method_signature_internal (prop->set)->param_count - 1];

			MonoObject *param_obj;
			pparams [0] = load_cattr_value (image, prop_type, &param_obj, named, data_end, &named, error);
			if (param_obj)
				pparams [0] = param_obj;
			goto_if_nok (error, fail);

			mono_property_set_value_handle (prop, attr, pparams, error);
			goto_if_nok (error, fail);
		}

		g_free (name);
		name = NULL;
	}

	goto exit;
fail:
	g_free (name);
	name = NULL;
	attr = mono_new_null ();
exit:
	if (field && !type_is_reference (field->type))
		g_free (val);
	if (prop_type && !type_is_reference (prop_type))
		g_free (pparams [0]);
	if (params) {
		free_param_data (method->signature, params);
		if (params != params_buf)
			mono_gc_free_fixed (params);
	}

	HANDLE_FUNCTION_RETURN_REF (MonoObject, attr);
}

static void
create_custom_attr_into_array (MonoImage *image, MonoMethod *method, const guchar *data,
	guint32 len, MonoArrayHandle array, int index, MonoError *error)
{
	// This function serves to avoid creating handles in a loop.
	HANDLE_FUNCTION_ENTER ();
	MonoObjectHandle attr = create_custom_attr (image, method, data, len, error);
	MONO_HANDLE_ARRAY_SETREF (array, index, attr);
	HANDLE_FUNCTION_RETURN ();
}

/*
 * mono_reflection_create_custom_attr_data_args:
 *
 *   Create an array of typed and named arguments from the cattr blob given by DATA.
 * TYPED_ARGS and NAMED_ARGS will contain the objects representing the arguments,
 * NAMED_ARG_INFO will contain information about the named arguments.
 */
void
mono_reflection_create_custom_attr_data_args (MonoImage *image, MonoMethod *method, const guchar *data, guint32 len, MonoArrayHandleOut typed_args_h, MonoArrayHandleOut named_args_h, CattrNamedArg **named_arg_info, MonoError *error)
{
	MonoArray *typed_args, *named_args;
	MonoClass *attrklass;
	MonoDomain *domain;
	const char *p = (const char*)data;
	const char *data_end = p + len;
	const char *named;
	guint32 i, j, num_named;
	CattrNamedArg *arginfo = NULL;

	MONO_HANDLE_ASSIGN_RAW (typed_args_h, NULL);
	MONO_HANDLE_ASSIGN_RAW (named_args_h, NULL);
	*named_arg_info = NULL;

	typed_args = NULL;
	named_args = NULL;

	error_init (error);

	mono_class_init_internal (method->klass);

	domain = mono_domain_get ();

	if (len < 2 || read16 (p) != 0x0001) /* Prolog */
		return;
	/* skip prolog */
	p += 2;

	/* Parse each argument corresponding to the signature's parameters from
	 * the blob and store in typed_args.
	 */
	typed_args = mono_array_new_checked (mono_get_object_class (), mono_method_signature_internal (method)->param_count, error);
	return_if_nok (error);
	MONO_HANDLE_ASSIGN_RAW (typed_args_h, typed_args);

	for (i = 0; i < mono_method_signature_internal (method)->param_count; ++i) {
		MonoObject *obj;

		obj = load_cattr_value_boxed (domain, image, mono_method_signature_internal (method)->params [i], p, data_end, &p, error);
		return_if_nok (error);
		mono_array_setref_internal (typed_args, i, obj);
	}

	named = p;

	/* Parse mandatory count of named arguments (could be zero) */
	if (!bcheck_blob (named, 1, data_end, error))
		return;
	num_named = read16 (named);
	named_args = mono_array_new_checked (mono_get_object_class (), num_named, error);
	return_if_nok (error);
	MONO_HANDLE_ASSIGN_RAW (named_args_h, named_args);
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
		if (named_type == CATTR_TYPE_FIELD) {
			/* Named arg is a field. */
			MonoObject *obj;
			MonoClassField *field = mono_class_get_field_from_name_full (attrklass, name, NULL);

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
			mono_array_setref_internal (named_args, j, obj);

		} else if (named_type == CATTR_TYPE_PROPERTY) {
			/* Named arg is a property */
			MonoObject *obj;
			MonoType *prop_type;
			MonoProperty *prop = mono_class_get_property_from_name_internal (attrklass, name);

			if (!prop || !prop->set) {
				g_free (name);
				goto fail;
			}

			prop_type = prop->get? mono_method_signature_internal (prop->get)->ret :
			     mono_method_signature_internal (prop->set)->params [mono_method_signature_internal (prop->set)->param_count - 1];

			arginfo [j].type = prop_type;
			arginfo [j].prop = prop;

			obj = load_cattr_value_boxed (domain, image, prop_type, named, data_end, &named, error);
			if (!is_ok (error)) {
				g_free (name);
				return;
			}
			mono_array_setref_internal (named_args, j, obj);
		}
		g_free (name);
	}

	return;
fail:
	mono_error_set_generic_error (error, "System.Reflection", "CustomAttributeFormatException", "Binary format of the specified custom attribute was invalid.");
	g_free (arginfo);
	*named_arg_info = NULL;
}

/*
 * free_decoded_custom_attr:
 * 
 * Handles freeing of MonoCustomAttrValue type properly.
 * Strings and MonoType* are not freed since they come from the metadata.
 */
static void
free_decoded_custom_attr(MonoCustomAttrValue* cattr_val)
{
	if (!cattr_val)
		return;

	if (cattr_val->type == MONO_TYPE_SZARRAY) {
		// attribute parameter types only support single-dimensional arrays
		for (int i = 0; i < cattr_val->value.array->len; i++) {
			if (cattr_val->value.array->values[i].type != MONO_TYPE_STRING && cattr_val->value.array->values[i].type != MONO_TYPE_CLASS)
				g_free(cattr_val->value.array->values[i].value.primitive);
		}
		g_free (cattr_val->value.array);
	} else if (cattr_val->type != MONO_TYPE_STRING && cattr_val->type != MONO_TYPE_CLASS) {
		g_free (cattr_val->value.primitive);
	}
}

/*
 * mono_reflection_free_custom_attr_data_args_noalloc:
 * 
 * Frees up MonoDecodeCustomAttr type.
 * Must be called after mono_reflection_create_custom_attr_data_args_noalloc to properly free up allocated struct.
 */
void
mono_reflection_free_custom_attr_data_args_noalloc (MonoDecodeCustomAttr* decoded_args)
{
	if (!decoded_args)
		return;

	// free typed args
	for (int i = 0; i < decoded_args->typed_args_num; i++) {
		free_decoded_custom_attr(decoded_args->typed_args[i]);
		g_free(decoded_args->typed_args[i]);
	}
	g_free(decoded_args->typed_args);

	// free named args
	for (int i = 0; i < decoded_args->named_args_num; i++) {
		free_decoded_custom_attr(decoded_args->named_args[i]);
		g_free(decoded_args->named_args[i]);
	}
	g_free(decoded_args->named_args);

	// free named args info
	g_free(decoded_args->named_args_info);

	g_free(decoded_args);
}

/*
 * mono_reflection_create_custom_attr_data_args_noalloc:
 *
 * Same as mono_reflection_create_custom_attr_data_args but allocate no managed objects.
 * Returns MonoDecodeCustomAttr struct with information about typed and named arguments.
 * Typed and named arguments are represented as array of MonoCustomAttrValue.
 * Return value must be freed using mono_reflection_free_custom_attr_data_args_noalloc ().
 */
MonoDecodeCustomAttr*
mono_reflection_create_custom_attr_data_args_noalloc (MonoImage *image, MonoMethod *method, const guchar *data, guint32 len, MonoError *error)
{
	MonoClass *attrklass;
	const char *p = (const char*)data;
	const char *data_end = p + len;
	const char *named;
	guint32 i, j, num_named;
	MonoDecodeCustomAttr *decoded_args = g_malloc0 (sizeof (MonoDecodeCustomAttr));
	MonoMethodSignature *sig = mono_method_signature_internal (method);

	error_init (error);

	mono_class_init_internal (method->klass);

	if (len < 2 || read16 (p) != 0x0001) /* Prolog */
		goto fail;

	/* skip prolog */
	p += 2;

	decoded_args->typed_args_num = sig->param_count;
	decoded_args->typed_args = g_malloc0 (sig->param_count * sizeof (MonoCustomAttrValue*));
	for (i = 0; i < sig->param_count; ++i) {
		decoded_args->typed_args [i] = load_cattr_value_noalloc (image, sig->params [i], p, data_end, &p, error);
		return_val_if_nok (error, NULL);
	}

	named = p;

	/* Parse mandatory count of named arguments (could be zero) */
	if (!bcheck_blob (named, 1, data_end, error))
		goto fail;
	num_named = read16 (named);

	decoded_args->named_args_num = num_named;
	decoded_args->named_args = g_malloc0 (num_named * sizeof (MonoCustomAttrValue*));

	return_val_if_nok (error, NULL);
	named += 2;
	attrklass = method->klass;

	decoded_args->named_args_info = g_new0 (CattrNamedArg, num_named);

	/* Parse each named arg, and add to arginfo.  Each named argument could
	 * be a field name or a property name followed by a value. */
	for (j = 0; j < num_named; j++) {
		guint32 name_len;
		char *name, named_type, data_type;
		if (!bcheck_blob (named, 1, data_end, error))
			goto fail;
		named_type = *named++; /* field or property? */
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
			goto fail;
		if (ADDP_IS_GREATER_OR_OVF ((const guchar*)named, name_len, data + len))
			goto fail;
		name = (char *)g_malloc (name_len + 1);
		memcpy (name, named, name_len);
		name [name_len] = 0;
		named += name_len;
		if (named_type == CATTR_TYPE_FIELD) {
			/* Named arg is a field. */
			MonoClassField *field = mono_class_get_field_from_name_full (attrklass, name, NULL);

			if (!field) {
				g_free (name);
				goto fail;
			}

			decoded_args->named_args_info [j].type = field->type;
			decoded_args->named_args_info [j].field = field;

			decoded_args->named_args [j] = load_cattr_value_noalloc (image, field->type, named, data_end, &named, error);

			if (!is_ok (error)) {
				g_free (name);
				goto fail;
			}
		} else if (named_type == CATTR_TYPE_PROPERTY) {
			/* Named arg is a property */
			MonoType *prop_type;
			MonoProperty *prop = mono_class_get_property_from_name_internal (attrklass, name);

			if (!prop || !prop->set) {
				g_free (name);
				goto fail;
			}

			prop_type = prop->get? mono_method_signature_internal (prop->get)->ret :
			     mono_method_signature_internal (prop->set)->params [mono_method_signature_internal (prop->set)->param_count - 1];

			decoded_args->named_args_info [j].type = prop_type;
			decoded_args->named_args_info [j].prop = prop;

			decoded_args->named_args [j] = load_cattr_value_noalloc (image, prop_type, named, data_end, &named, error);
			if (!is_ok (error)) {
				g_free (name);
				goto fail;
			}
		}
		g_free (name);
	}

	return decoded_args;
fail:
	mono_error_set_generic_error (error, "System.Reflection", "CustomAttributeFormatException", "Binary format of the specified custom attribute was invalid.");
	mono_reflection_free_custom_attr_data_args_noalloc(decoded_args);
	return NULL;
}

void
ves_icall_System_Reflection_RuntimeCustomAttributeData_ResolveArgumentsInternal (MonoReflectionMethodHandle ref_method_h, MonoReflectionAssemblyHandle assembly_h,
																		  gpointer data, guint32 len,
																		  MonoArrayHandleOut ctor_args_h, MonoArrayHandleOut named_args_h,
																		  MonoError *error)
{
	MonoArray *typed_args, *named_args;
	MonoImage *image;
	MonoMethod *method;
	CattrNamedArg *arginfo = NULL;
	MonoReflectionMethod *ref_method = MONO_HANDLE_RAW (ref_method_h);
	MonoReflectionAssembly *assembly = MONO_HANDLE_RAW (assembly_h);
	MonoMethodSignature *sig;
	MonoObjectHandle obj_h, namedarg_h, minfo_h;
	int i;

	if (len == 0)
		return;

	obj_h = MONO_HANDLE_NEW (MonoObject, NULL);
	namedarg_h = MONO_HANDLE_NEW (MonoObject, NULL);
	minfo_h = MONO_HANDLE_NEW (MonoObject, NULL);

	image = assembly->assembly->image;
	method = ref_method->method;

	if (!mono_class_init_internal (method->klass)) {
		mono_error_set_for_class_failure (error, method->klass);
		goto leave;
	}

	// FIXME: Handles
	mono_reflection_create_custom_attr_data_args (image, method, (const guchar *)data, len, ctor_args_h, named_args_h, &arginfo, error);
	goto_if_nok (error, leave);
	typed_args = MONO_HANDLE_RAW (ctor_args_h);
	named_args = MONO_HANDLE_RAW (named_args_h);

	if (!typed_args || !named_args)
		goto leave;

	sig = mono_method_signature_internal (method);
	for (i = 0; i < sig->param_count; ++i) {
		MonoObject *obj;
		MonoObject *typedarg;
		MonoType *t;

		obj = mono_array_get_internal (typed_args, MonoObject*, i);
		MONO_HANDLE_ASSIGN_RAW (obj_h, obj);

		t = sig->params [i];
		if (t->type == MONO_TYPE_OBJECT && obj)
			t = m_class_get_byval_arg (obj->vtable->klass);
		typedarg = create_cattr_typed_arg (t, obj, error);
		goto_if_nok (error, leave);
		mono_array_setref_internal (typed_args, i, typedarg);
	}

	for (i = 0; i < mono_array_length_internal (named_args); ++i) {
		MonoObject *obj;
		MonoObject *namedarg, *minfo;

		obj = mono_array_get_internal (named_args, MonoObject*, i);
		MONO_HANDLE_ASSIGN_RAW (obj_h, obj);
		if (arginfo [i].prop) {
			minfo = (MonoObject*)mono_property_get_object_checked (arginfo [i].prop->parent, arginfo [i].prop, error);
			if (!minfo)
				goto leave;
		} else {
			minfo = (MonoObject*)mono_field_get_object_checked (NULL, arginfo [i].field, error);
			goto_if_nok (error, leave);
		}
		MONO_HANDLE_ASSIGN_RAW (minfo_h, minfo);

		namedarg = create_cattr_named_arg (minfo, obj, error);
		MONO_HANDLE_ASSIGN_RAW (namedarg_h, namedarg);

		goto_if_nok (error, leave);

		mono_array_setref_internal (named_args, i, namedarg);
	}

leave:
	g_free (arginfo);
}

static MonoClass*
try_get_cattr_data_class (MonoError* error)
{
	error_init (error);
	MonoClass *res = mono_class_try_get_customattribute_data_class ();
	if (!res)
		mono_error_set_execution_engine (error, "Class System.Reflection.RuntimeCustomAttributeData not found, probably removed by the linker");
	return res;
}

static MonoObjectHandle
create_custom_attr_data (MonoImage *image, MonoCustomAttrEntry *cattr, MonoError *error)
{
	HANDLE_FUNCTION_ENTER ();

	static MonoMethod *ctor;
	void *params [4];

	error_init (error);

	g_assert (image->assembly);
	MonoObjectHandle attr;

	MonoClass *cattr_data = try_get_cattr_data_class (error);
	goto_if_nok (error, result_null);

	if (!ctor) {
		MonoMethod *tmp = mono_class_get_method_from_name_checked (cattr_data, ".ctor", 4, 0, error);
		mono_error_assert_ok (error);
		g_assert (tmp);

		mono_memory_barrier (); //safe publish!
		ctor = tmp;
	}

	attr = mono_object_new_handle (cattr_data, error);
	goto_if_nok (error, fail);

	MonoReflectionMethodHandle ctor_obj;
	ctor_obj = mono_method_get_object_handle (cattr->ctor, NULL, error);
	goto_if_nok (error, fail);
	MonoReflectionAssemblyHandle assm;
	assm = mono_assembly_get_object_handle (image->assembly, error);
	goto_if_nok (error, fail);
	params [0] = MONO_HANDLE_RAW (ctor_obj);
	params [1] = MONO_HANDLE_RAW (assm);
	params [2] = &cattr->data;
	params [3] = &cattr->data_size;

	mono_runtime_invoke_handle_void (ctor, attr, params, error);
	goto fail;
result_null:
	attr = MONO_HANDLE_CAST (MonoObject, mono_new_null ());
fail:
	HANDLE_FUNCTION_RETURN_REF (MonoObject, attr);


}

static void
create_custom_attr_data_into_array (MonoImage *image, MonoCustomAttrEntry *cattr, MonoArrayHandle result, int index, MonoError *error)
{
	// This function serves to avoid creating handles in a loop.
	HANDLE_FUNCTION_ENTER ();
	MonoObjectHandle attr = create_custom_attr_data (image, cattr, error);
	goto_if_nok (error, exit);
	MONO_HANDLE_ARRAY_SETREF (result, index, attr);
exit:
	HANDLE_FUNCTION_RETURN ();
}

static MonoArrayHandle
mono_custom_attrs_construct_by_type (MonoCustomAttrInfo *cinfo, MonoClass *attr_klass, MonoError *error)
{
	HANDLE_FUNCTION_ENTER ();

	MonoArrayHandle result;
	int i, n;

	error_init (error);

	for (i = 0; i < cinfo->num_attrs; ++i) {
		MonoCustomAttrEntry *centry = &cinfo->attrs[i];
		if (!centry->ctor) {
			/* The cattr type is not finished yet */
			/* We should include the type name but cinfo doesn't contain it */
			mono_error_set_type_load_name (error, NULL, NULL, "Custom attribute constructor is null because the custom attribute type is not finished yet.");
			goto return_null;
		}
	}

	n = 0;
	if (attr_klass) {
		for (i = 0; i < cinfo->num_attrs; ++i) {
			MonoMethod *ctor = cinfo->attrs[i].ctor;
			g_assert (ctor);
			if (mono_class_is_assignable_from_internal (attr_klass, ctor->klass))
				n++;
		}
	} else {
		n = cinfo->num_attrs;
	}

	result = mono_array_new_cached_handle (mono_defaults.attribute_class, n, error);
	goto_if_nok (error, return_null);
	n = 0;
	for (i = 0; i < cinfo->num_attrs; ++i) {
		MonoCustomAttrEntry *centry = &cinfo->attrs [i];
		if (!attr_klass || mono_class_is_assignable_from_internal (attr_klass, centry->ctor->klass)) {
			create_custom_attr_into_array (cinfo->image, centry->ctor, centry->data,
				centry->data_size, result, n, error);
			goto_if_nok (error, exit);
			n ++;
		}
	}
	goto exit;
return_null:
	result = MONO_HANDLE_CAST (MonoArray, mono_new_null ());
exit:
	HANDLE_FUNCTION_RETURN_REF (MonoArray, result);
}

/**
 * mono_custom_attrs_construct:
 */
MonoArray*
mono_custom_attrs_construct (MonoCustomAttrInfo *cinfo)
{
	HANDLE_FUNCTION_ENTER ();
	ERROR_DECL (error);
	MonoArrayHandle result = mono_custom_attrs_construct_by_type (cinfo, NULL, error);
	mono_error_assert_ok (error); /*FIXME proper error handling*/
	HANDLE_FUNCTION_RETURN_OBJ (result);
}

static MonoArrayHandle
mono_custom_attrs_data_construct (MonoCustomAttrInfo *cinfo, MonoError *error)
{
	HANDLE_FUNCTION_ENTER ();

	MonoArrayHandle result;
	MonoClass *cattr_data = try_get_cattr_data_class (error);
	goto_if_nok (error, return_null);

	result = mono_array_new_handle (cattr_data, cinfo->num_attrs, error);
	goto_if_nok (error, return_null);
	for (int i = 0; i < cinfo->num_attrs; ++i) {
		create_custom_attr_data_into_array (cinfo->image, &cinfo->attrs [i], result, i, error);
		goto_if_nok (error, return_null);
	}
	goto exit;
return_null:
	result = MONO_HANDLE_CAST (MonoArray, mono_new_null ());
exit:
	HANDLE_FUNCTION_RETURN_REF (MonoArray, result);
}

/**
 * mono_custom_attrs_from_index:
 *
 * Returns: NULL if no attributes are found or if a loading error occurs.
 */
MonoCustomAttrInfo*
mono_custom_attrs_from_index (MonoImage *image, guint32 idx)
{
	ERROR_DECL (error);
	MonoCustomAttrInfo *result = mono_custom_attrs_from_index_checked (image, idx, FALSE, error);
	mono_error_cleanup (error);
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
	GArray *attr_array;
	const char *data;
	MonoCustomAttrEntry* attr;

	error_init (error);

	ca = &image->tables [MONO_TABLE_CUSTOMATTRIBUTE];

	i = mono_metadata_custom_attrs_from_index (image, idx);
	if (!i)
		return NULL;
	i --;
	// initial size chosen arbitrarily, but default is 16 which is rather small
	attr_array = g_array_sized_new (TRUE, TRUE, sizeof (guint32), 128);
	while (!mono_metadata_table_bounds_check (image, MONO_TABLE_CUSTOMATTRIBUTE, i + 1)) {
		if (mono_metadata_decode_row_col (ca, i, MONO_CUSTOM_ATTR_PARENT) != idx) {
			if (G_LIKELY (!image->has_updates)) {
				break;
			} else {
				// if there are updates, the new custom attributes are not sorted,
				// so we have to go until the end.
				++i;
				continue;
			}
		}
		attr_array = g_array_append_val (attr_array, i);
		++i;
	}
	len = attr_array->len;
	if (!len) {
		g_array_free (attr_array, TRUE);
		return NULL;
	}
	ainfo = (MonoCustomAttrInfo *)g_malloc0 (MONO_SIZEOF_CUSTOM_ATTR_INFO + sizeof (MonoCustomAttrEntry) * len);
	ainfo->num_attrs = len;
	ainfo->image = image;
	for (i = 0; i < len; ++i) {
		mono_metadata_decode_row (ca, g_array_index (attr_array, guint32, i), cols, MONO_CUSTOM_ATTR_SIZE);
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
		attr = &ainfo->attrs [i];
		attr->ctor = mono_get_method_checked (image, mtoken, NULL, NULL, error);
		if (!attr->ctor) {
			g_warning ("Can't find custom attr constructor image: %s mtoken: 0x%08x due to: %s", image->name, mtoken, mono_error_get_message (error));
			if (ignore_missing) {
				mono_error_cleanup (error);
				error_init (error);
			} else {
				g_array_free (attr_array, TRUE);
				g_free (ainfo);
				return NULL;
			}
		}

		data = mono_metadata_blob_heap (image, cols [MONO_CUSTOM_ATTR_VALUE]);
		attr->data_size = mono_metadata_decode_value (data, &data);
		attr->data = (guchar*)data;
	}
	g_array_free (attr_array, TRUE);

	return ainfo;
}

/**
 * mono_custom_attrs_from_method:
 */
MonoCustomAttrInfo*
mono_custom_attrs_from_method (MonoMethod *method)
{
	ERROR_DECL (error);
	MonoCustomAttrInfo* result = mono_custom_attrs_from_method_checked  (method, error);
	mono_error_cleanup (error); /* FIXME want a better API that doesn't swallow the error */
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

	if (method_is_dynamic (method) || image_is_dynamic (m_class_get_image (method->klass)))
		return lookup_custom_attr (m_class_get_image (method->klass), method);

	if (!method->token)
		/* Synthetic methods */
		return NULL;

	idx = custom_attrs_idx_from_method (method);
	return mono_custom_attrs_from_index_checked (m_class_get_image (method->klass), idx, FALSE, error);
}

/**
 * mono_custom_attrs_from_class:
 */
MonoCustomAttrInfo*
mono_custom_attrs_from_class (MonoClass *klass)
{
	ERROR_DECL (error);
	MonoCustomAttrInfo *result = mono_custom_attrs_from_class_checked (klass, error);
	mono_error_cleanup (error);
	return result;
}

guint32
custom_attrs_idx_from_class (MonoClass *klass)
{
	guint32 idx;
	g_assert (!image_is_dynamic (m_class_get_image (klass)));
	if (m_class_get_byval_arg (klass)->type == MONO_TYPE_VAR || m_class_get_byval_arg (klass)->type == MONO_TYPE_MVAR) {
		idx = mono_metadata_token_index (m_class_get_sizes (klass).generic_param_token);
		idx <<= MONO_CUSTOM_ATTR_BITS;
		idx |= MONO_CUSTOM_ATTR_GENERICPAR;
	} else {
		idx = mono_metadata_token_index (m_class_get_type_token (klass));
		idx <<= MONO_CUSTOM_ATTR_BITS;
		idx |= MONO_CUSTOM_ATTR_TYPEDEF;
	}
	return idx;
}

guint32
custom_attrs_idx_from_method (MonoMethod *method)
{
	guint32 idx;
	g_assert (!image_is_dynamic (m_class_get_image (method->klass)));
	idx = mono_method_get_index (method);
	idx <<= MONO_CUSTOM_ATTR_BITS;
	idx |= MONO_CUSTOM_ATTR_METHODDEF;
	return idx;
}

MonoCustomAttrInfo*
mono_custom_attrs_from_class_checked (MonoClass *klass, MonoError *error)
{
	guint32 idx;

	error_init (error);

	if (mono_class_is_ginst (klass))
		klass = mono_class_get_generic_class (klass)->container_class;

	if (image_is_dynamic (m_class_get_image (klass)))
		return lookup_custom_attr (m_class_get_image (klass), klass);

	idx = custom_attrs_idx_from_class (klass);

	return mono_custom_attrs_from_index_checked (m_class_get_image (klass), idx, FALSE, error);
}

/**
 * mono_custom_attrs_from_assembly:
 */
MonoCustomAttrInfo*
mono_custom_attrs_from_assembly (MonoAssembly *assembly)
{
	ERROR_DECL (error);
	MonoCustomAttrInfo *result = mono_custom_attrs_from_assembly_checked (assembly, FALSE, error);
	mono_error_cleanup (error);
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
	ERROR_DECL (error);
	MonoCustomAttrInfo * result = mono_custom_attrs_from_property_checked (klass, property, error);
	mono_error_cleanup (error);
	return result;
}

MonoCustomAttrInfo*
mono_custom_attrs_from_property_checked (MonoClass *klass, MonoProperty *property, MonoError *error)
{
	guint32 idx;

	error_init (error);

	if (image_is_dynamic (m_class_get_image (klass))) {
		property = mono_metadata_get_corresponding_property_from_generic_type_definition (property);
		return lookup_custom_attr (m_class_get_image (klass), property);
	}
	idx = find_property_index (klass, property);
	idx <<= MONO_CUSTOM_ATTR_BITS;
	idx |= MONO_CUSTOM_ATTR_PROPERTY;
	return mono_custom_attrs_from_index_checked (m_class_get_image (klass), idx, FALSE, error);
}

/**
 * mono_custom_attrs_from_event:
 */
MonoCustomAttrInfo*
mono_custom_attrs_from_event (MonoClass *klass, MonoEvent *event)
{
	ERROR_DECL (error);
	MonoCustomAttrInfo * result = mono_custom_attrs_from_event_checked (klass, event, error);
	mono_error_cleanup (error);
	return result;
}

MonoCustomAttrInfo*
mono_custom_attrs_from_event_checked (MonoClass *klass, MonoEvent *event, MonoError *error)
{
	guint32 idx;

	error_init (error);

	if (image_is_dynamic (m_class_get_image (klass))) {
		event = mono_metadata_get_corresponding_event_from_generic_type_definition (event);
		return lookup_custom_attr (m_class_get_image (klass), event);
	}
	idx = find_event_index (klass, event);
	idx <<= MONO_CUSTOM_ATTR_BITS;
	idx |= MONO_CUSTOM_ATTR_EVENT;
	return mono_custom_attrs_from_index_checked (m_class_get_image (klass), idx, FALSE, error);
}

/**
 * mono_custom_attrs_from_field:
 */
MonoCustomAttrInfo*
mono_custom_attrs_from_field (MonoClass *klass, MonoClassField *field)
{
	ERROR_DECL (error);
	MonoCustomAttrInfo * result = mono_custom_attrs_from_field_checked (klass, field, error);
	mono_error_cleanup (error);
	return result;
}

MonoCustomAttrInfo*
mono_custom_attrs_from_field_checked (MonoClass *klass, MonoClassField *field, MonoError *error)
{
	guint32 idx;
	error_init (error);

	if (image_is_dynamic (m_class_get_image (klass))) {
		field = mono_metadata_get_corresponding_field_from_generic_type_definition (field);
		return lookup_custom_attr (m_class_get_image (klass), field);
	}
	idx = find_field_index (klass, field);
	idx <<= MONO_CUSTOM_ATTR_BITS;
	idx |= MONO_CUSTOM_ATTR_FIELDDEF;
	return mono_custom_attrs_from_index_checked (m_class_get_image (klass), idx, FALSE, error);
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
	ERROR_DECL (error);
	MonoCustomAttrInfo *result = mono_custom_attrs_from_param_checked (method, param, error);
	mono_error_cleanup (error);
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

	if (image_is_dynamic (m_class_get_image (method->klass))) {
		MonoCustomAttrInfo *res, *ainfo;
		int size;

		aux = (MonoReflectionMethodAux *)g_hash_table_lookup (((MonoDynamicImage*)m_class_get_image (method->klass))->method_aux_hash, method);
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

	image = m_class_get_image (method->klass);
	method_index = mono_method_get_index (method);
	if (!method_index)
		return NULL;
	ca = &image->tables [MONO_TABLE_METHOD];

	/* FIXME: metadata-update */
	param_list = mono_metadata_decode_row_col (ca, method_index - 1, MONO_METHOD_PARAMLIST);
	if (method_index == table_info_get_rows (ca)) {
		param_last = table_info_get_rows (&image->tables [MONO_TABLE_PARAM]) + 1;
	} else {
		param_last = mono_metadata_decode_row_col (ca, method_index, MONO_METHOD_PARAMLIST);
	}
	ca = &image->tables [MONO_TABLE_PARAM];

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
		if (klass == attr_klass || mono_class_has_parent (klass, attr_klass) || (MONO_CLASS_IS_INTERFACE_INTERNAL (attr_klass) && mono_class_is_assignable_from_internal (attr_klass, klass)))
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
	ERROR_DECL (error);
	MonoObject *res = mono_custom_attrs_get_attr_checked (ainfo, attr_klass, error);
	mono_error_assert_ok (error); /*FIXME proper error handling*/
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
		if (attr_klass == klass || mono_class_is_assignable_from_internal (attr_klass, klass)) {
			HANDLE_FUNCTION_ENTER ();
			MonoObjectHandle result = create_custom_attr (ainfo->image, centry->ctor, centry->data, centry->data_size, error);
			HANDLE_FUNCTION_RETURN_OBJ (result);
		}
	}

	return NULL;
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
	ERROR_DECL (error);
	MONO_HANDLE_DCL (MonoObject, obj);
	MonoCustomAttrInfo * const result = mono_reflection_get_custom_attrs_info_checked (obj, error);
	mono_error_assert_ok (error);
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
	const char *klass_name = m_class_get_name (klass);
	if (klass == mono_defaults.runtimetype_class) {
		MonoType *type = mono_reflection_type_handle_mono_type (MONO_HANDLE_CAST(MonoReflectionType, obj), error);
		goto_if_nok (error, leave);
		klass = mono_class_from_mono_type_internal (type);
		/*We cannot mono_class_init_internal the class from which we'll load the custom attributes since this must work with broken types.*/
		cinfo = mono_custom_attrs_from_class_checked (klass, error);
		goto_if_nok (error, leave);
	} else if (strcmp ("Assembly", klass_name) == 0 || strcmp ("RuntimeAssembly", klass_name) == 0) {
		MonoReflectionAssemblyHandle rassembly = MONO_HANDLE_CAST (MonoReflectionAssembly, obj);
		cinfo = mono_custom_attrs_from_assembly_checked (MONO_HANDLE_GETVAL (rassembly, assembly), FALSE, error);
		goto_if_nok (error, leave);
	} else if (strcmp ("RuntimeModule", klass_name) == 0) {
		MonoReflectionModuleHandle module = MONO_HANDLE_CAST (MonoReflectionModule, obj);
		cinfo = mono_custom_attrs_from_module (MONO_HANDLE_GETVAL (module, image), error);
		goto_if_nok (error, leave);
	} else if (strcmp ("RuntimePropertyInfo", klass_name) == 0) {
		MonoReflectionPropertyHandle rprop = MONO_HANDLE_CAST (MonoReflectionProperty, obj);
		MonoProperty *property = MONO_HANDLE_GETVAL (rprop, property);
		cinfo = mono_custom_attrs_from_property_checked (property->parent, property, error);
		goto_if_nok (error, leave);
	} else if (strcmp ("RuntimeEventInfo", klass_name) == 0) {
		MonoReflectionMonoEventHandle revent = MONO_HANDLE_CAST (MonoReflectionMonoEvent, obj);
		MonoEvent *event = MONO_HANDLE_GETVAL (revent, event);
		cinfo = mono_custom_attrs_from_event_checked (event->parent, event, error);
		goto_if_nok (error, leave);
	} else if (strcmp ("RuntimeFieldInfo", klass_name) == 0) {
		MonoReflectionFieldHandle rfield = MONO_HANDLE_CAST (MonoReflectionField, obj);
		MonoClassField *field = MONO_HANDLE_GETVAL (rfield, field);
		cinfo = mono_custom_attrs_from_field_checked (m_field_get_parent (field), field, error);
		goto_if_nok (error, leave);
	} else if ((strcmp ("RuntimeMethodInfo", klass_name) == 0) || (strcmp ("RuntimeConstructorInfo", klass_name) == 0)) {
		MonoReflectionMethodHandle rmethod = MONO_HANDLE_CAST (MonoReflectionMethod, obj);
		cinfo = mono_custom_attrs_from_method_checked (MONO_HANDLE_GETVAL (rmethod, method), error);
		goto_if_nok (error, leave);
	} else if (strcmp ("ParameterInfo", klass_name) == 0 || strcmp ("RuntimeParameterInfo", klass_name) == 0) {
		MonoReflectionParameterHandle param = MONO_HANDLE_CAST (MonoReflectionParameter, obj);

		MonoObjectHandle member_impl = MONO_HANDLE_NEW (MonoObject, NULL);
		int position;
		mono_reflection_get_param_info_member_and_pos (param, member_impl, &position);

		MonoClass *member_class = mono_handle_class (member_impl);
		if (mono_class_is_reflection_method_or_constructor (member_class)) {
			MonoReflectionMethodHandle rmethod = MONO_HANDLE_CAST (MonoReflectionMethod, member_impl);
			cinfo = mono_custom_attrs_from_param_checked (MONO_HANDLE_GETVAL (rmethod, method), position + 1, error);
			goto_if_nok (error, leave);
		} else if (mono_is_sr_mono_property (member_class)) {
			MonoReflectionPropertyHandle prop = MONO_HANDLE_CAST (MonoReflectionProperty, member_impl);
			MonoProperty *property = MONO_HANDLE_GETVAL (prop, property);
			MonoMethod *method;
			if (!(method = property->get))
				method = property->set;
			g_assert (method);

			cinfo = mono_custom_attrs_from_param_checked (method, position + 1, error);
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
	} else if (strcmp ("AssemblyBuilder", klass_name) == 0) {
		MonoReflectionAssemblyBuilderHandle assemblyb = MONO_HANDLE_CAST (MonoReflectionAssemblyBuilder, obj);
		MonoReflectionAssemblyHandle assembly = MONO_HANDLE_CAST (MonoReflectionAssembly, assemblyb);
		MonoArrayHandle cattrs = MONO_HANDLE_NEW_GET (MonoArray, assemblyb, cattrs);
		MonoImage * image = MONO_HANDLE_GETVAL (assembly, assembly)->image;
		g_assert (image);
		cinfo = mono_custom_attrs_from_builders_handle (NULL, image, cattrs);
	} else if (strcmp ("TypeBuilder", klass_name) == 0) {
		MonoReflectionTypeBuilderHandle tb = MONO_HANDLE_CAST (MonoReflectionTypeBuilder, obj);
		MonoReflectionModuleBuilderHandle module = MONO_HANDLE_NEW_GET (MonoReflectionModuleBuilder, tb, module);
		MonoDynamicImage *dynamic_image = MONO_HANDLE_GETVAL (module, dynamic_image);
		MonoArrayHandle cattrs = MONO_HANDLE_NEW_GET (MonoArray, tb, cattrs);
		cinfo = mono_custom_attrs_from_builders_handle (NULL, &dynamic_image->image, cattrs);
	} else if (strcmp ("ModuleBuilder", klass_name) == 0) {
		MonoReflectionModuleBuilderHandle mb = MONO_HANDLE_CAST (MonoReflectionModuleBuilder, obj);
		MonoDynamicImage *dynamic_image = MONO_HANDLE_GETVAL (mb, dynamic_image);
		MonoArrayHandle cattrs = MONO_HANDLE_NEW_GET (MonoArray, mb, cattrs);
		cinfo = mono_custom_attrs_from_builders_handle (NULL, &dynamic_image->image, cattrs);
	} else if (strcmp ("ConstructorBuilder", klass_name) == 0) {
		mono_error_set_not_supported (error, "");
		goto leave;
	} else if (strcmp ("MethodBuilder", klass_name) == 0) {
		MonoReflectionMethodBuilderHandle mb = MONO_HANDLE_CAST (MonoReflectionMethodBuilder, obj);
		MonoMethod *mhandle = MONO_HANDLE_GETVAL (mb, mhandle);
		MonoArrayHandle cattrs = MONO_HANDLE_NEW_GET (MonoArray, mb, cattrs);
		cinfo = mono_custom_attrs_from_builders_handle (NULL, m_class_get_image (mhandle->klass), cattrs);
	} else if (strcmp ("FieldBuilder", klass_name) == 0) {
		MonoReflectionFieldBuilderHandle fb = MONO_HANDLE_CAST (MonoReflectionFieldBuilder, obj);
		MonoReflectionTypeBuilderHandle tb = MONO_HANDLE_CAST (MonoReflectionTypeBuilder, MONO_HANDLE_NEW_GET (MonoReflectionType, fb, typeb));
		MonoReflectionModuleBuilderHandle mb = MONO_HANDLE_NEW_GET (MonoReflectionModuleBuilder, tb, module);
		MonoDynamicImage *dynamic_image = MONO_HANDLE_GETVAL (mb, dynamic_image);
		MonoArrayHandle cattrs = MONO_HANDLE_NEW_GET (MonoArray, fb, cattrs);
		cinfo = mono_custom_attrs_from_builders_handle (NULL, &dynamic_image->image, cattrs);
	} else if (strcmp ("MonoGenericClass", klass_name) == 0) {
		MonoReflectionGenericClassHandle gclass = MONO_HANDLE_CAST (MonoReflectionGenericClass, obj);
		MonoReflectionTypeHandle generic_type = MONO_HANDLE_NEW_GET (MonoReflectionType, gclass, generic_type);
		cinfo = mono_reflection_get_custom_attrs_info_checked (MONO_HANDLE_CAST (MonoObject, generic_type), error);
		goto_if_nok (error, leave);
	} else { /* handle other types here... */
		g_error ("get custom attrs not yet supported for %s", m_class_get_name (klass));
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
		MONO_HANDLE_ASSIGN (result, mono_custom_attrs_construct_by_type (cinfo, attr_klass, error));
		if (!cinfo->cached)
			mono_custom_attrs_free (cinfo);
	} else {
		MONO_HANDLE_ASSIGN (result, mono_array_new_handle (mono_defaults.attribute_class, 0, error));
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
	ERROR_DECL (error);
	MONO_HANDLE_DCL (MonoObject, obj);
	MonoArrayHandle result = mono_reflection_get_custom_attrs_by_type_handle (obj, NULL, error);
	mono_error_cleanup (error);
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
	ERROR_DECL (error);
	MONO_HANDLE_DCL (MonoObject, obj);
	MonoArrayHandle result = mono_reflection_get_custom_attrs_data_checked (obj, error);
	mono_error_cleanup (error);
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

	cinfo = mono_reflection_get_custom_attrs_info_checked (obj, error);
	goto_if_nok (error, leave);
	if (cinfo) {
		MONO_HANDLE_ASSIGN (result, mono_custom_attrs_data_construct (cinfo, error));
		if (!cinfo->cached)
			mono_custom_attrs_free (cinfo);
		goto_if_nok (error, leave);
	} else  {
		MonoClass *cattr_data = try_get_cattr_data_class (error);
		goto_if_nok (error, return_null);

		MONO_HANDLE_ASSIGN (result, mono_array_new_handle (cattr_data, 0, error));
	}
	goto leave;
return_null:
	result = MONO_HANDLE_CAST (MonoArray, mono_new_null ());
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

		if (mono_metadata_token_table (type_token) != MONO_TABLE_TYPEDEF || mono_metadata_table_bounds_check (image, MONO_TABLE_TYPEDEF, tidx)) {
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
 * @assembly_token: out argument set to the assembly ref token of the custom attr
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
				guint32 typeref_cols [MONO_TYPEREF_SIZE];
				MonoTableInfo  *t = &image->tables [MONO_TABLE_TYPEREF];

				mono_metadata_decode_row (t, (type_token&0xffffff)-1, typeref_cols, MONO_TYPEREF_SIZE);

				if (class_name)
					*class_name = mono_metadata_string_heap (image, typeref_cols [MONO_TYPEREF_NAME]);
				if (nspace)
					*nspace = mono_metadata_string_heap (image, typeref_cols [MONO_TYPEREF_NAMESPACE]);
				if (assembly_token)
					*assembly_token = typeref_cols [MONO_TYPEREF_SCOPE];
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

	metadata_foreach_custom_attr_from_index (image, idx, func, user_data);
}

/**
 * iterate over the custom attributes that belong to the given index and call func, passing the
 *  assembly ref (if any) and the namespace and name of the custom attribute.
 *
 * Everything is done using low-level metadata APIs, so it is safe to use
 * during assembly loading and class initialization.
 */
void
metadata_foreach_custom_attr_from_index (MonoImage *image, guint32 idx, MonoAssemblyMetadataCustomAttrIterFunc func, gpointer user_data)
{
	guint32 mtoken, i;
	guint32 cols [MONO_CUSTOM_ATTR_SIZE];
	MonoTableInfo *ca;

	/* Inlined from mono_custom_attrs_from_index_checked () */
	ca = &image->tables [MONO_TABLE_CUSTOMATTRIBUTE];
	i = mono_metadata_custom_attrs_from_index (image, idx);
	if (!i)
		return;
	i --;
	gboolean stop_iterating = FALSE;
	int rows = table_info_get_rows (ca);
	while (!stop_iterating && i < rows) {
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

/**
 * mono_class_metadata_foreach_custom_attr:
 * \param klass - the class to iterate over
 * \param func the funciton to call for each custom attribute
 * \param user_data passed to \p func
 *
 * Calls \p func for each custom attribute type on the given class until \p func returns TRUE.
 *
 * Everything is done using low-level metadata APIs, so it is fafe to use
 * during assembly loading and class initialization.
 *
 * The MonoClass \p klass should have the following fields initialized:
 *
 * \c MonoClass:kind, \c MonoClass:image, \c MonoClassGenericInst:generic_class,
 * \c MonoClass:type_token, \c MonoClass:sizes.generic_param_token, MonoClass:byval_arg
 */
void
mono_class_metadata_foreach_custom_attr (MonoClass *klass, MonoAssemblyMetadataCustomAttrIterFunc func, gpointer user_data)
{
	MonoImage *image = m_class_get_image (klass);

	/* dynamic images don't store custom attributes in tables */
	g_assert (!image_is_dynamic (image));

	if (mono_class_is_ginst (klass))
		klass = mono_class_get_generic_class (klass)->container_class;

	guint32 idx = custom_attrs_idx_from_class (klass);

	metadata_foreach_custom_attr_from_index (image, idx, func, user_data);
}

/**
 * mono_method_metadata_foreach_custom_attr:
 * \param method - the method to iterate over
 * \param func the funciton to call for each custom attribute
 * \param user_data passed to \p func
 *
 * Calls \p func for each custom attribute type on the given class until \p func returns TRUE.
 *
 * Everything is done using low-level metadata APIs, so it is fafe to use
 * during assembly loading and class initialization.
 *
 */
void
mono_method_metadata_foreach_custom_attr (MonoMethod *method, MonoAssemblyMetadataCustomAttrIterFunc func, gpointer user_data)
{
	if (method->is_inflated)
		method = ((MonoMethodInflated *) method)->declaring;

	MonoImage *image = m_class_get_image (method->klass);

	g_assert (!image_is_dynamic (image));

	if (!method->token)
		return;

	guint32 idx = custom_attrs_idx_from_method (method);

	metadata_foreach_custom_attr_from_index (image, idx, func, user_data);
}

#ifdef ENABLE_WEAK_ATTR
static void
init_weak_fields_inner (MonoImage *image, GHashTable *indexes)
{
	MonoTableInfo *tdef;
	ERROR_DECL (error);
	MonoClass *klass = NULL;
	guint32 memberref_index = -1;
	int first_method_idx = -1;
	int method_count = -1;

	if (image == mono_get_corlib ()) {
		/* Typedef */
		klass = mono_class_from_name_checked (image, "System", "WeakAttribute", error);
		if (!is_ok (error)) {
			mono_error_cleanup (error);
			return;
		}
		if (!klass)
			return;
		first_method_idx = mono_class_get_first_method_idx (klass);
		method_count = mono_class_get_method_count (klass);

		tdef = &image->tables [MONO_TABLE_CUSTOMATTRIBUTE];
		guint32 parent, field_idx, col, mtoken, idx;
		int rows = table_info_get_rows (tdef);
		for (int i = 0; i < rows; ++i) {
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
		/* FIXME: metadata-update */

		/* Memberref pointing to a typeref */
		tdef = &image->tables [MONO_TABLE_MEMBERREF];

		/* Check whenever the assembly references the WeakAttribute type */
		gboolean found = FALSE;
		tdef = &image->tables [MONO_TABLE_TYPEREF];
		int rows = table_info_get_rows (tdef);
		for (int i = 0; i < rows; ++i) {
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
		rows = table_info_get_rows (tdef);
		for (int i = 0; i < rows; ++i) {
			guint32 memberref_cols [MONO_MEMBERREF_SIZE];
			const char *sig;

			mono_metadata_decode_row (tdef, i, memberref_cols, MONO_MEMBERREF_SIZE);
			sig = mono_metadata_blob_heap (image, memberref_cols [MONO_MEMBERREF_SIGNATURE]);
			mono_metadata_decode_blob_size (sig, &sig);

			guint32 nindex = memberref_cols [MONO_MEMBERREF_CLASS] >> MONO_MEMBERREF_PARENT_BITS;
			guint32 class_index = memberref_cols [MONO_MEMBERREF_CLASS] & MONO_MEMBERREF_PARENT_MASK;
			const char *fname = mono_metadata_string_heap (image, memberref_cols [MONO_MEMBERREF_NAME]);

			if (!strcmp (fname, ".ctor") && class_index == MONO_MEMBERREF_PARENT_TYPEREF) {
				MonoTableInfo *typeref_table = &image->tables [MONO_TABLE_TYPEREF];
				guint32 typeref_cols [MONO_TYPEREF_SIZE];

				mono_metadata_decode_row (typeref_table, nindex - 1, typeref_cols, MONO_TYPEREF_SIZE);

				const char *name = mono_metadata_string_heap (image, typeref_cols [MONO_TYPEREF_NAME]);
				const char *nspace = mono_metadata_string_heap (image, typeref_cols [MONO_TYPEREF_NAMESPACE]);

				if (!strcmp (nspace, "System") && !strcmp (name, "WeakAttribute")) {
					klass = mono_class_from_typeref_checked (image, MONO_TOKEN_TYPE_REF | nindex, error);
					if (!is_ok (error)) {
						mono_error_cleanup (error);
						return;
					}
					g_assert (!strcmp (m_class_get_name (klass), "WeakAttribute"));
					/* Allow a testing dll as well since some profiles don't have WeakAttribute */
					if (klass && (m_class_get_image (klass) == mono_get_corlib () || strstr (m_class_get_image (klass)->name, "Mono.Runtime.Testing"))) {
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
		rows = table_info_get_rows (tdef);
		for (int i = 0; i < rows; ++i) {
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
#endif

/*
 * mono_assembly_init_weak_fields:
 *
 *   Initialize the image->weak_field_indexes hash.
 */
void
mono_assembly_init_weak_fields (MonoImage *image)
{
#ifdef ENABLE_WEAK_ATTR
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
#endif
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
#ifdef ENABLE_WEAK_ATTR
	if (image->dynamic)
		return FALSE;

	mono_assembly_init_weak_fields (image);

	/* The hash is not mutated, no need to lock */
	return g_hash_table_lookup (image->weak_field_indexes, GINT_TO_POINTER (field_idx)) != NULL;
#else
	g_assert_not_reached ();
#endif
}
