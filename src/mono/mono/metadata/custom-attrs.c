/*
 * custom-attrs.c: Custom attributes.
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
#include "mono/metadata/gc-internals.h"
#include "mono/metadata/mono-endian.h"
#include "mono/metadata/object-internals.h"
#include "mono/metadata/reflection-cache.h"
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

static GENERATE_GET_CLASS_WITH_CACHE (custom_attribute_typed_argument, System.Reflection, CustomAttributeTypedArgument);
static GENERATE_GET_CLASS_WITH_CACHE (custom_attribute_named_argument, System.Reflection, CustomAttributeNamedArgument);

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
		int visibility = cattr->ctor->method->klass->flags & TYPE_ATTRIBUTE_VISIBILITY_MASK;

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

	for (i = 0; i < klass->field.count; ++i) {
		if (field == &klass->fields [i])
			return klass->field.first + 1 + i;
	}
	return 0;
}

/*
 * Find the property index in the metadata Property table.
 */
static guint32
find_property_index (MonoClass *klass, MonoProperty *property) {
	int i;

	for (i = 0; i < klass->ext->property.count; ++i) {
		if (property == &klass->ext->properties [i])
			return klass->ext->property.first + 1 + i;
	}
	return 0;
}

/*
 * Find the event index in the metadata Event table.
 */
static guint32
find_event_index (MonoClass *klass, MonoEvent *event) {
	int i;

	for (i = 0; i < klass->ext->event.count; ++i) {
		if (event == &klass->ext->events [i])
			return klass->ext->event.first + 1 + i;
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
load_cattr_enum_type (MonoImage *image, const char *p, const char **end, MonoError *error)
{
	char *n;
	MonoType *t;
	int slen = mono_metadata_decode_value (p, &p);

	mono_error_init (error);

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
load_cattr_value (MonoImage *image, MonoType *t, const char *p, const char **end, MonoError *error)
{
	int slen, type = t->type;
	MonoClass *tklass = t->data.klass;

	mono_error_init (error);

handle_enum:
	switch (type) {
	case MONO_TYPE_U1:
	case MONO_TYPE_I1:
	case MONO_TYPE_BOOLEAN: {
		MonoBoolean *bval = (MonoBoolean *)g_malloc (sizeof (MonoBoolean));
		*bval = *p;
		*end = p + 1;
		return bval;
	}
	case MONO_TYPE_CHAR:
	case MONO_TYPE_U2:
	case MONO_TYPE_I2: {
		guint16 *val = (guint16 *)g_malloc (sizeof (guint16));
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
		*val = read64 (p);
		*end = p + 8;
		return val;
	}
	case MONO_TYPE_R8: {
		double *val = (double *)g_malloc (sizeof (double));
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
				*val = read64 (p);
				*end = p + 8;
				return val;
			}
		}
		g_error ("generic valutype %s not handled in custom attr value decoding", t->data.klass->name);
		break;
		
	case MONO_TYPE_STRING:
		if (*p == (char)0xFF) {
			*end = p + 1;
			return NULL;
		}
		slen = mono_metadata_decode_value (p, &p);
		*end = p + slen;
		return mono_string_new_len_checked (mono_domain_get (), p, slen, error);
	case MONO_TYPE_CLASS: {
		MonoReflectionType *rt;
		char *n;
		MonoType *t;
		if (*p == (char)0xFF) {
			*end = p + 1;
			return NULL;
		}
handle_type:
		slen = mono_metadata_decode_value (p, &p);
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
			int etype = *p;
			p ++;

			type = MONO_TYPE_SZARRAY;
			if (etype == 0x50) {
				tklass = mono_defaults.systemtype_class;
			} else if (etype == 0x55) {
				tklass = load_cattr_enum_type (image, p, &p, error);
				if (!mono_error_ok (error))
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
			slen = mono_metadata_decode_value (p, &p);
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
		val = load_cattr_value (image, &subc->byval_arg, p, end, error);
		obj = NULL;
		if (mono_error_ok (error)) {
			obj = mono_object_new_checked (mono_domain_get (), subc, error);
			g_assert (!subc->has_references);
			if (mono_error_ok (error))
				mono_gc_memmove_atomic ((char*)obj + sizeof (MonoObject), val, mono_class_value_size (subc, NULL));
		}

		g_free (val);
		return obj;
	}
	case MONO_TYPE_SZARRAY: {
		MonoArray *arr;
		guint32 i, alen, basetype;
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
					MonoBoolean val = *p++;
					mono_array_set (arr, MonoBoolean, i, val);
				}
				break;
			case MONO_TYPE_CHAR:
			case MONO_TYPE_U2:
			case MONO_TYPE_I2:
				for (i = 0; i < alen; i++) {
					guint16 val = read16 (p);
					mono_array_set (arr, guint16, i, val);
					p += 2;
				}
				break;
			case MONO_TYPE_R4:
			case MONO_TYPE_U4:
			case MONO_TYPE_I4:
				for (i = 0; i < alen; i++) {
					guint32 val = read32 (p);
					mono_array_set (arr, guint32, i, val);
					p += 4;
				}
				break;
			case MONO_TYPE_R8:
				for (i = 0; i < alen; i++) {
					double val;
					readr8 (p, &val);
					mono_array_set (arr, double, i, val);
					p += 8;
				}
				break;
			case MONO_TYPE_U8:
			case MONO_TYPE_I8:
				for (i = 0; i < alen; i++) {
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
					MonoObject *item = (MonoObject *)load_cattr_value (image, &tklass->byval_arg, p, &p, error);
					if (!mono_error_ok (error))
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
load_cattr_value_boxed (MonoDomain *domain, MonoImage *image, MonoType *t, const char* p, const char** end, MonoError *error)
{
	mono_error_init (error);

	gboolean is_ref = type_is_reference (t);

	void *val = load_cattr_value (image, t, p, end, error);
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

	mono_error_init (error);

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

	mono_error_init (error);

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


static MonoObject*
create_custom_attr (MonoImage *image, MonoMethod *method, const guchar *data, guint32 len, MonoError *error)
{
	const char *p = (const char*)data;
	const char *named;
	guint32 i, j, num_named;
	MonoObject *attr;
	void *params_buf [32];
	void **params = NULL;
	MonoMethodSignature *sig;

	mono_error_init (error);

	mono_class_init (method->klass);

	if (!mono_verifier_verify_cattr_content (image, method, data, len, NULL)) {
		mono_error_set_generic_error (error, "System.Reflection", "CustomAttributeFormatException", "Binary format of the specified custom attribute was invalid.");
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
		params = (void **)mono_gc_alloc_fixed (sig->param_count * sizeof (void*), MONO_GC_DESCRIPTOR_NULL, MONO_ROOT_SOURCE_REFLECTION, "custom attribute parameters");
	}

	/* skip prolog */
	p += 2;
	for (i = 0; i < mono_method_signature (method)->param_count; ++i) {
		params [i] = load_cattr_value (image, mono_method_signature (method)->params [i], p, &p, error);
		if (!mono_error_ok (error))
			goto fail;
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

	num_named = read16 (named);
	named += 2;
	for (j = 0; j < num_named; j++) {
		gint name_len;
		char *name, named_type, data_type;
		named_type = *named++;
		data_type = *named++; /* type of data */
		if (data_type == MONO_TYPE_SZARRAY)
			data_type = *named++;
		if (data_type == MONO_TYPE_ENUM) {
			gint type_len;
			char *type_name;
			type_len = mono_metadata_decode_blob_size (named, &named);
			type_name = (char *)g_malloc (type_len + 1);
			memcpy (type_name, named, type_len);
			type_name [type_len] = 0;
			named += type_len;
			/* FIXME: lookup the type and check type consistency */
			g_free (type_name);
		}
		name_len = mono_metadata_decode_blob_size (named, &named);
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

			val = load_cattr_value (image, field->type, named, &named, error);
			if (!mono_error_ok (error)) {
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

			pparams [0] = load_cattr_value (image, prop_type, named, &named, error);
			if (!mono_error_ok (error)) {
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
	const char *named;
	guint32 i, j, num_named;
	CattrNamedArg *arginfo = NULL;

	*typed_args = NULL;
	*named_args = NULL;
	*named_arg_info = NULL;

	mono_error_init (error);

	if (!mono_verifier_verify_cattr_content (image, method, data, len, NULL)) {
		mono_error_set_generic_error (error, "System.Reflection", "CustomAttributeFormatException", "Binary format of the specified custom attribute was invalid.");
		return;
	}

	mono_class_init (method->klass);
	
	domain = mono_domain_get ();

	if (len < 2 || read16 (p) != 0x0001) /* Prolog */
		return;

	typedargs = mono_array_new_checked (domain, mono_get_object_class (), mono_method_signature (method)->param_count, error);
	return_if_nok (error);

	/* skip prolog */
	p += 2;
	for (i = 0; i < mono_method_signature (method)->param_count; ++i) {
		MonoObject *obj;

		obj = load_cattr_value_boxed (domain, image, mono_method_signature (method)->params [i], p, &p, error);
		return_if_nok (error);
		mono_array_setref (typedargs, i, obj);
	}

	named = p;
	num_named = read16 (named);
	namedargs = mono_array_new_checked (domain, mono_get_object_class (), num_named, error);
	return_if_nok (error);
	named += 2;
	attrklass = method->klass;

	arginfo = g_new0 (CattrNamedArg, num_named);
	*named_arg_info = arginfo;

	for (j = 0; j < num_named; j++) {
		gint name_len;
		char *name, named_type, data_type;
		named_type = *named++;
		data_type = *named++; /* type of data */
		if (data_type == MONO_TYPE_SZARRAY)
			data_type = *named++;
		if (data_type == MONO_TYPE_ENUM) {
			gint type_len;
			char *type_name;
			type_len = mono_metadata_decode_blob_size (named, &named);
			if (ADDP_IS_GREATER_OR_OVF ((const guchar*)named, type_len, data + len))
				goto fail;

			type_name = (char *)g_malloc (type_len + 1);
			memcpy (type_name, named, type_len);
			type_name [type_len] = 0;
			named += type_len;
			/* FIXME: lookup the type and check type consistency */
			g_free (type_name);
		}
		name_len = mono_metadata_decode_blob_size (named, &named);
		if (ADDP_IS_GREATER_OR_OVF ((const guchar*)named, name_len, data + len))
			goto fail;
		name = (char *)g_malloc (name_len + 1);
		memcpy (name, named, name_len);
		name [name_len] = 0;
		named += name_len;
		if (named_type == 0x53) {
			MonoObject *obj;
			MonoClassField *field = mono_class_get_field_from_name (attrklass, name);

			if (!field) {
				g_free (name);
				goto fail;
			}

			arginfo [j].type = field->type;
			arginfo [j].field = field;

			obj = load_cattr_value_boxed (domain, image, field->type, named, &named, error);
			if (!is_ok (error)) {
				g_free (name);
				return;
			}
			mono_array_setref (namedargs, j, obj);

		} else if (named_type == 0x54) {
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

			obj = load_cattr_value_boxed (domain, image, prop_type, named, &named, error);
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

	mono_error_init (error);

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
	if (!is_ok (error))
		goto leave;

	if (!typedargs || !namedargs)
		goto leave;

	for (i = 0; i < mono_method_signature (method)->param_count; ++i) {
		MonoObject *obj = mono_array_get (typedargs, MonoObject*, i);
		MonoObject *typedarg;

		typedarg = create_cattr_typed_arg (mono_method_signature (method)->params [i], obj, error);
		if (!is_ok (error))
			goto leave;
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
			if (!is_ok (error))
				goto leave;
		}

		typedarg = create_cattr_typed_arg (arginfo [i].type, obj, error);
		if (!is_ok (error))
			goto leave;
		namedarg = create_cattr_named_arg (minfo, typedarg, error);
		if (!is_ok (error))
			goto leave;

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

static MonoObject*
create_custom_attr_data (MonoImage *image, MonoCustomAttrEntry *cattr, MonoError *error)
{
	static MonoMethod *ctor;

	MonoDomain *domain;
	MonoObject *attr;
	void *params [4];

	mono_error_init (error);

	g_assert (image->assembly);

	if (!ctor)
		ctor = mono_class_get_method_from_name (mono_defaults.customattribute_data_class, ".ctor", 4);

	domain = mono_domain_get ();
	attr = mono_object_new_checked (domain, mono_defaults.customattribute_data_class, error);
	return_val_if_nok (error, NULL);
	params [0] = mono_method_get_object_checked (domain, cattr->ctor, NULL, error);
	return_val_if_nok (error, NULL);
	params [1] = mono_assembly_get_object_checked (domain, image->assembly, error);
	return_val_if_nok (error, NULL);
	params [2] = (gpointer)&cattr->data;
	params [3] = &cattr->data_size;

	mono_runtime_invoke_checked (ctor, attr, params, error);
	return_val_if_nok (error, NULL);
	return attr;
}

static MonoArray*
mono_custom_attrs_construct_by_type (MonoCustomAttrInfo *cinfo, MonoClass *attr_klass, MonoError *error)
{
	MonoArray *result;
	MonoObject *attr;
	int i, n;

	mono_error_init (error);

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
	
	mono_error_init (error);
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
	MonoCustomAttrInfo *result = mono_custom_attrs_from_index_checked (image, idx, &error);
	mono_error_cleanup (&error);
	return result;
}
/**
 * mono_custom_attrs_from_index_checked:
 *
 * Returns: NULL if no attributes are found.  On error returns NULL and sets @error.
 */
MonoCustomAttrInfo*
mono_custom_attrs_from_index_checked (MonoImage *image, guint32 idx, MonoError *error)
{
	guint32 mtoken, i, len;
	guint32 cols [MONO_CUSTOM_ATTR_SIZE];
	MonoTableInfo *ca;
	MonoCustomAttrInfo *ainfo;
	GList *tmp, *list = NULL;
	const char *data;
	MonoCustomAttrEntry* attr;

	mono_error_init (error);

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
			g_warning ("Can't find custom attr constructor image: %s mtoken: 0x%08x due to %s", image->name, mtoken, mono_error_get_message (error));
			g_list_free (list);
			g_free (ainfo);
			return NULL;
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

	mono_error_init (error);

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
	return mono_custom_attrs_from_index_checked (method->klass->image, idx, error);
}

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

	mono_error_init (error);

	if (klass->generic_class)
		klass = klass->generic_class->container_class;

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
	return mono_custom_attrs_from_index_checked (klass->image, idx, error);
}

MonoCustomAttrInfo*
mono_custom_attrs_from_assembly (MonoAssembly *assembly)
{
	MonoError error;
	MonoCustomAttrInfo *result = mono_custom_attrs_from_assembly_checked (assembly, &error);
	mono_error_cleanup (&error);
	return result;
}

MonoCustomAttrInfo*
mono_custom_attrs_from_assembly_checked (MonoAssembly *assembly, MonoError *error)
{
	guint32 idx;
	
	mono_error_init (error);

	if (image_is_dynamic (assembly->image))
		return lookup_custom_attr (assembly->image, assembly);
	idx = 1; /* there is only one assembly */
	idx <<= MONO_CUSTOM_ATTR_BITS;
	idx |= MONO_CUSTOM_ATTR_ASSEMBLY;
	return mono_custom_attrs_from_index_checked (assembly->image, idx, error);
}

static MonoCustomAttrInfo*
mono_custom_attrs_from_module (MonoImage *image, MonoError *error)
{
	guint32 idx;
	
	if (image_is_dynamic (image))
		return lookup_custom_attr (image, image);
	idx = 1; /* there is only one module */
	idx <<= MONO_CUSTOM_ATTR_BITS;
	idx |= MONO_CUSTOM_ATTR_MODULE;
	return mono_custom_attrs_from_index_checked (image, idx, error);
}

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
	
	if (image_is_dynamic (klass->image)) {
		property = mono_metadata_get_corresponding_property_from_generic_type_definition (property);
		return lookup_custom_attr (klass->image, property);
	}
	idx = find_property_index (klass, property);
	idx <<= MONO_CUSTOM_ATTR_BITS;
	idx |= MONO_CUSTOM_ATTR_PROPERTY;
	return mono_custom_attrs_from_index_checked (klass->image, idx, error);
}

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
	
	if (image_is_dynamic (klass->image)) {
		event = mono_metadata_get_corresponding_event_from_generic_type_definition (event);
		return lookup_custom_attr (klass->image, event);
	}
	idx = find_event_index (klass, event);
	idx <<= MONO_CUSTOM_ATTR_BITS;
	idx |= MONO_CUSTOM_ATTR_EVENT;
	return mono_custom_attrs_from_index_checked (klass->image, idx, error);
}

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
	mono_error_init (error);

	if (image_is_dynamic (klass->image)) {
		field = mono_metadata_get_corresponding_field_from_generic_type_definition (field);
		return lookup_custom_attr (klass->image, field);
	}
	idx = find_field_index (klass, field);
	idx <<= MONO_CUSTOM_ATTR_BITS;
	idx |= MONO_CUSTOM_ATTR_FIELDDEF;
	return mono_custom_attrs_from_index_checked (klass->image, idx, error);
}

/**
 * mono_custom_attrs_from_param:
 * @method: handle to the method that we want to retrieve custom parameter information from
 * @param: parameter number, where zero represent the return value, and one is the first parameter in the method
 *
 * The result must be released with mono_custom_attrs_free().
 *
 * Returns: the custom attribute object for the specified parameter, or NULL if there are none.
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
 * @method: handle to the method that we want to retrieve custom parameter information from
 * @param: parameter number, where zero represent the return value, and one is the first parameter in the method
 * @error: set on error
 *
 * The result must be released with mono_custom_attrs_free().
 *
 * Returns: the custom attribute object for the specified parameter, or NULL if there are none.  On failure returns NULL and sets @error.
 */
MonoCustomAttrInfo*
mono_custom_attrs_from_param_checked (MonoMethod *method, guint32 param, MonoError *error)
{
	MonoTableInfo *ca;
	guint32 i, idx, method_index;
	guint32 param_list, param_last, param_pos, found;
	MonoImage *image;
	MonoReflectionMethodAux *aux;

	mono_error_init (error);

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
	return mono_custom_attrs_from_index_checked (image, idx, error);
}

gboolean
mono_custom_attrs_has_attr (MonoCustomAttrInfo *ainfo, MonoClass *attr_klass)
{
	int i;
	for (i = 0; i < ainfo->num_attrs; ++i) {
		MonoClass *klass = ainfo->attrs [i].ctor->klass;
		if (mono_class_has_parent (klass, attr_klass) || (MONO_CLASS_IS_INTERFACE (attr_klass) && mono_class_is_assignable_from (attr_klass, klass)))
			return TRUE;
	}
	return FALSE;
}

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
	int i, attr_index;
	MonoArray *attrs;

	mono_error_init (error);

	attr_index = -1;
	for (i = 0; i < ainfo->num_attrs; ++i) {
		MonoClass *klass = ainfo->attrs [i].ctor->klass;
		if (mono_class_has_parent (klass, attr_klass)) {
			attr_index = i;
			break;
		}
	}
	if (attr_index == -1)
		return NULL;

	attrs = mono_custom_attrs_construct_by_type (ainfo, NULL, error);
	if (!mono_error_ok (error))
		return NULL;
	return mono_array_get (attrs, MonoObject*, attr_index);
}

/*
 * mono_reflection_get_custom_attrs_info:
 * @obj: a reflection object handle
 *
 * Return the custom attribute info for attributes defined for the
 * reflection handle @obj. The objects.
 *
 * FIXME this function leaks like a sieve for SRE objects.
 */
MonoCustomAttrInfo*
mono_reflection_get_custom_attrs_info (MonoObject *obj)
{
	MonoError error;
	MonoCustomAttrInfo *result = mono_reflection_get_custom_attrs_info_checked (obj, &error);
	mono_error_assert_ok (&error);
	return result;
}

/**
 * mono_reflection_get_custom_attrs_info_checked:
 * @obj: a reflection object handle
 * @error: set on error
 *
 * Return the custom attribute info for attributes defined for the
 * reflection handle @obj. The objects.
 *
 * On failure returns NULL and sets @error.
 *
 * FIXME this function leaks like a sieve for SRE objects.
 */
MonoCustomAttrInfo*
mono_reflection_get_custom_attrs_info_checked (MonoObject *obj, MonoError *error)
{
	MonoClass *klass;
	MonoCustomAttrInfo *cinfo = NULL;
	
	mono_error_init (error);

	klass = obj->vtable->klass;
	if (klass == mono_defaults.runtimetype_class) {
		MonoType *type = mono_reflection_type_get_handle ((MonoReflectionType *)obj, error);
		return_val_if_nok (error, NULL);
		klass = mono_class_from_mono_type (type);
		/*We cannot mono_class_init the class from which we'll load the custom attributes since this must work with broken types.*/
		cinfo = mono_custom_attrs_from_class_checked (klass, error);
		return_val_if_nok (error, NULL);
	} else if (strcmp ("Assembly", klass->name) == 0 || strcmp ("MonoAssembly", klass->name) == 0) {
		MonoReflectionAssembly *rassembly = (MonoReflectionAssembly*)obj;
		cinfo = mono_custom_attrs_from_assembly_checked (rassembly->assembly, error);
		return_val_if_nok (error, NULL);
	} else if (strcmp ("Module", klass->name) == 0 || strcmp ("MonoModule", klass->name) == 0) {
		MonoReflectionModule *module = (MonoReflectionModule*)obj;
		cinfo = mono_custom_attrs_from_module (module->image, error);
		return_val_if_nok (error, NULL);
	} else if (strcmp ("MonoProperty", klass->name) == 0) {
		MonoReflectionProperty *rprop = (MonoReflectionProperty*)obj;
		cinfo = mono_custom_attrs_from_property_checked (rprop->property->parent, rprop->property, error);
		return_val_if_nok (error, NULL);
	} else if (strcmp ("MonoEvent", klass->name) == 0) {
		MonoReflectionMonoEvent *revent = (MonoReflectionMonoEvent*)obj;
		cinfo = mono_custom_attrs_from_event_checked (revent->event->parent, revent->event, error);
		return_val_if_nok (error, NULL);
	} else if (strcmp ("MonoField", klass->name) == 0) {
		MonoReflectionField *rfield = (MonoReflectionField*)obj;
		cinfo = mono_custom_attrs_from_field_checked (rfield->field->parent, rfield->field, error);
		return_val_if_nok (error, NULL);
	} else if ((strcmp ("MonoMethod", klass->name) == 0) || (strcmp ("MonoCMethod", klass->name) == 0)) {
		MonoReflectionMethod *rmethod = (MonoReflectionMethod*)obj;
		cinfo = mono_custom_attrs_from_method_checked (rmethod->method, error);
		return_val_if_nok (error, NULL);
	} else if (strcmp ("ParameterInfo", klass->name) == 0 || strcmp ("MonoParameterInfo", klass->name) == 0) {
		MonoReflectionParameter *param = (MonoReflectionParameter*)obj;
		MonoClass *member_class = mono_object_class (param->MemberImpl);
		if (mono_class_is_reflection_method_or_constructor (member_class)) {
			MonoReflectionMethod *rmethod = (MonoReflectionMethod*)param->MemberImpl;
			cinfo = mono_custom_attrs_from_param_checked (rmethod->method, param->PositionImpl + 1, error);
			return_val_if_nok (error, NULL);
		} else if (mono_is_sr_mono_property (member_class)) {
			MonoReflectionProperty *prop = (MonoReflectionProperty *)param->MemberImpl;
			MonoMethod *method;
			if (!(method = prop->property->get))
				method = prop->property->set;
			g_assert (method);

			cinfo = mono_custom_attrs_from_param_checked (method, param->PositionImpl + 1, error);
			return_val_if_nok (error, NULL);
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
			return NULL;
		}
	} else if (strcmp ("AssemblyBuilder", klass->name) == 0) {
		MonoReflectionAssemblyBuilder *assemblyb = (MonoReflectionAssemblyBuilder*)obj;
		cinfo = mono_custom_attrs_from_builders (NULL, assemblyb->assembly.assembly->image, assemblyb->cattrs);
	} else if (strcmp ("TypeBuilder", klass->name) == 0) {
		MonoReflectionTypeBuilder *tb = (MonoReflectionTypeBuilder*)obj;
		cinfo = mono_custom_attrs_from_builders (NULL, &tb->module->dynamic_image->image, tb->cattrs);
	} else if (strcmp ("ModuleBuilder", klass->name) == 0) {
		MonoReflectionModuleBuilder *mb = (MonoReflectionModuleBuilder*)obj;
		cinfo = mono_custom_attrs_from_builders (NULL, &mb->dynamic_image->image, mb->cattrs);
	} else if (strcmp ("ConstructorBuilder", klass->name) == 0) {
		MonoReflectionCtorBuilder *cb = (MonoReflectionCtorBuilder*)obj;
		cinfo = mono_custom_attrs_from_builders (NULL, cb->mhandle->klass->image, cb->cattrs);
	} else if (strcmp ("MethodBuilder", klass->name) == 0) {
		MonoReflectionMethodBuilder *mb = (MonoReflectionMethodBuilder*)obj;
		cinfo = mono_custom_attrs_from_builders (NULL, mb->mhandle->klass->image, mb->cattrs);
	} else if (strcmp ("FieldBuilder", klass->name) == 0) {
		MonoReflectionFieldBuilder *fb = (MonoReflectionFieldBuilder*)obj;
		cinfo = mono_custom_attrs_from_builders (NULL, &((MonoReflectionTypeBuilder*)fb->typeb)->module->dynamic_image->image, fb->cattrs);
	} else if (strcmp ("MonoGenericClass", klass->name) == 0) {
		MonoReflectionGenericClass *gclass = (MonoReflectionGenericClass*)obj;
		cinfo = mono_reflection_get_custom_attrs_info_checked ((MonoObject*)gclass->generic_type, error);
		return_val_if_nok (error, NULL);
	} else { /* handle other types here... */
		g_error ("get custom attrs not yet supported for %s", klass->name);
	}

	return cinfo;
}

/*
 * mono_reflection_get_custom_attrs_by_type:
 * @obj: a reflection object handle
 *
 * Return an array with all the custom attributes defined of the
 * reflection handle @obj. If @attr_klass is non-NULL, only custom attributes 
 * of that type are returned. The objects are fully build. Return NULL if a loading error
 * occurs.
 */
MonoArray*
mono_reflection_get_custom_attrs_by_type (MonoObject *obj, MonoClass *attr_klass, MonoError *error)
{
	MonoArray *result;
	MonoCustomAttrInfo *cinfo;

	mono_error_init (error);

	cinfo = mono_reflection_get_custom_attrs_info_checked (obj, error);
	return_val_if_nok (error, NULL);
	if (cinfo) {
		result = mono_custom_attrs_construct_by_type (cinfo, attr_klass, error);
		if (!cinfo->cached)
			mono_custom_attrs_free (cinfo);
		if (!result)
			return NULL;
	} else {
		result = mono_array_new_cached (mono_domain_get (), mono_defaults.attribute_class, 0, error);
	}

	return result;
}

/*
 * mono_reflection_get_custom_attrs:
 * @obj: a reflection object handle
 *
 * Return an array with all the custom attributes defined of the
 * reflection handle @obj. The objects are fully build. Return NULL if a loading error
 * occurs.
 */
MonoArray*
mono_reflection_get_custom_attrs (MonoObject *obj)
{
	MonoError error;

	return mono_reflection_get_custom_attrs_by_type (obj, NULL, &error);
}

/*
 * mono_reflection_get_custom_attrs_data:
 * @obj: a reflection obj handle
 *
 * Returns an array of System.Reflection.CustomAttributeData,
 * which include information about attributes reflected on
 * types loaded using the Reflection Only methods
 */
MonoArray*
mono_reflection_get_custom_attrs_data (MonoObject *obj)
{
	MonoError error;
	MonoArray* result;
	result = mono_reflection_get_custom_attrs_data_checked (obj, &error);
	mono_error_cleanup (&error);
	return result;
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
MonoArray*
mono_reflection_get_custom_attrs_data_checked (MonoObject *obj, MonoError *error)
{
	MonoArray *result;
	MonoCustomAttrInfo *cinfo;

	mono_error_init (error);

	cinfo = mono_reflection_get_custom_attrs_info_checked (obj, error);
	return_val_if_nok (error, NULL);
	if (cinfo) {
		result = mono_custom_attrs_data_construct (cinfo, error);
		if (!cinfo->cached)
			mono_custom_attrs_free (cinfo);
		return_val_if_nok (error, NULL);
	} else 
		result = mono_array_new_checked (mono_domain_get (), mono_defaults.customattribute_data_class, 0, error);

	return result;
}
