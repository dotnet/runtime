/**
 * \file
 * Copyright 2018 Microsoft
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */
#include "config.h"
#ifdef HAVE_ALLOCA_H
#include <alloca.h>
#endif

#include "metadata/method-builder-ilgen.h"
#include "metadata/method-builder-ilgen-internals.h"
#include <mono/metadata/object.h>
#include <mono/metadata/loader.h>
#include "cil-coff.h"
#include "metadata/marshal.h"
#include "metadata/marshal-internals.h"
#include "metadata/marshal-ilgen.h"
#include "metadata/tabledefs.h"
#include <mono/metadata/exception.h>
#include <mono/metadata/appdomain.h>
#include "mono/metadata/abi-details.h"
#include "mono/metadata/class-abi-details.h"
#include "mono/metadata/class-init.h"
#include "mono/metadata/debug-helpers.h"
#include "mono/metadata/threads.h"
#include "mono/metadata/monitor.h"
#include "mono/metadata/class-internals.h"
#include "mono/metadata/metadata-internals.h"
#include "mono/metadata/domain-internals.h"
#include "mono/metadata/gc-internals.h"
#include "mono/metadata/threads-types.h"
#include "mono/metadata/string-icalls.h"
#include "mono/metadata/attrdefs.h"
#include "mono/metadata/cominterop.h"
#include "mono/metadata/reflection-internals.h"
#include "mono/metadata/handle.h"
#include "mono/metadata/custom-attrs-internals.h"
#include "mono/metadata/icall-internals.h"
#include "mono/utils/mono-tls.h"
#include "mono/utils/mono-memory-model.h"
#include "mono/utils/atomic.h"
#include <mono/utils/mono-threads.h>
#include <mono/utils/mono-threads-coop.h>
#include <mono/utils/mono-error-internals.h>
#include <string.h>
#include <errno.h>
#include "icall-decl.h"

#define OPDEF(a,b,c,d,e,f,g,h,i,j) \
	a = i,

enum {
#include "mono/cil/opcode.def"
	LAST = 0xff
};
#undef OPDEF

static gboolean
is_in (const MonoType *t)
{
	const guint32 attrs = t->attrs;
	return (attrs & PARAM_ATTRIBUTE_IN) || !(attrs & PARAM_ATTRIBUTE_OUT);
}

static gboolean
is_out (const MonoType *t)
{
	const guint32 attrs = t->attrs;
	return (attrs & PARAM_ATTRIBUTE_OUT) || !(attrs & PARAM_ATTRIBUTE_IN);
}

static GENERATE_GET_CLASS_WITH_CACHE (fixed_buffer_attribute, "System.Runtime.CompilerServices", "FixedBufferAttribute");
static GENERATE_GET_CLASS_WITH_CACHE (date_time, "System", "DateTime");
static GENERATE_TRY_GET_CLASS_WITH_CACHE (icustom_marshaler, "System.Runtime.InteropServices", "ICustomMarshaler");
static GENERATE_TRY_GET_CLASS_WITH_CACHE (marshal, "System.Runtime.InteropServices", "Marshal");

/* MonoMethod pointers to SafeHandle::DangerousAddRef and ::DangerousRelease */
static MonoMethod *sh_dangerous_add_ref;
static MonoMethod *sh_dangerous_release;

// FIXME Consolidate the multiple functions named get_method_nofail.
static MonoMethod*
get_method_nofail (MonoClass *klass, const char *method_name, int num_params, int flags)
{
	MonoMethod *method;
	ERROR_DECL (error);
	method = mono_class_get_method_from_name_checked (klass, method_name, num_params, flags, error);
	mono_error_assert_ok (error);
	g_assertf (method, "Could not lookup method %s in %s", method_name, m_class_get_name (klass));
	return method;
}

static void
init_safe_handle (void)
{
	mono_atomic_store_seq (&sh_dangerous_add_ref, get_method_nofail (mono_class_try_get_safehandle_class (), "DangerousAddRef", 1, 0));
	mono_atomic_store_seq (&sh_dangerous_release, get_method_nofail (mono_class_try_get_safehandle_class (), "DangerousRelease", 0, 0));
}

static MonoImage*
get_method_image (MonoMethod *method)
{
	return m_class_get_image (method->klass);
}

static void
emit_struct_conv (MonoMethodBuilder *mb, MonoClass *klass, gboolean to_object);

static void
emit_struct_conv_full (MonoMethodBuilder *mb, MonoClass *klass, gboolean to_object, int offset_of_first_child_field, MonoMarshalNative string_encoding);

static MonoJitICallId
conv_to_icall (MonoMarshalConv conv, int *ind_store_type);

static MonoMarshalConv
conv_str_inverse (MonoMarshalConv conv);

/**
 * mono_mb_strdup:
 * \param mb the MethodBuilder
 * \param s a string
 *
 * Creates a copy of the string \p s that can be referenced from the IL of \c mb.
 *
 * \returns a pointer to the new string which is owned by the method builder
 */
char*
mono_mb_strdup (MonoMethodBuilder *mb, const char *s)
{
	char *res;
	if (!mb->dynamic)
		res = mono_image_strdup (get_method_image (mb->method), s);
	else
		res = g_strdup (s);
	return res;
}



/*
 * mono_mb_emit_exception_marshal_directive:
 *
 *   This function assumes ownership of MSG, which should be malloc-ed.
 */
static void
mono_mb_emit_exception_marshal_directive (MonoMethodBuilder *mb, char *msg)
{
	char *s = mono_mb_strdup (mb, msg);
	g_free (msg);
	mono_mb_emit_exception_full (mb, "System.Runtime.InteropServices", "MarshalDirectiveException", s);
}

static int
offset_of_first_nonstatic_field (MonoClass *klass)
{
	mono_class_setup_fields (klass);
	gpointer iter = NULL;
	MonoClassField *field;
	while ((field = mono_class_get_fields_internal (klass, &iter))) {
		if (!(field->type->attrs & FIELD_ATTRIBUTE_STATIC) && !mono_field_is_deleted (field)) {
			/*
			 * metadata-update: adding fields to existing structs isn't supported.  In
			 * newly-added structs, the "from update" field won't be set.
			 */
			g_assert (!m_field_is_from_update (field));
			return m_field_get_offset (field) - MONO_ABI_SIZEOF (MonoObject);
		}
	}

	return 0;
}

static gboolean
get_fixed_buffer_attr (MonoClassField *field, MonoType **out_etype, int *out_len)
{
	ERROR_DECL (error);
	MonoCustomAttrInfo *cinfo;
	MonoCustomAttrEntry *attr;
	int aindex;

	cinfo = mono_custom_attrs_from_field_checked (m_field_get_parent (field), field, error);
	if (!is_ok (error))
		return FALSE;
	attr = NULL;
	if (cinfo) {
		for (aindex = 0; aindex < cinfo->num_attrs; ++aindex) {
			MonoClass *ctor_class = cinfo->attrs [aindex].ctor->klass;
			if (mono_class_has_parent (ctor_class, mono_class_get_fixed_buffer_attribute_class ())) {
				attr = &cinfo->attrs [aindex];
				break;
			}
		}
	}
	if (attr) {
		gpointer *typed_args, *named_args;
		CattrNamedArg *arginfo;
		int num_named_args;

		mono_reflection_create_custom_attr_data_args_noalloc (mono_defaults.corlib, attr->ctor, attr->data, attr->data_size,
															  &typed_args, &named_args, &num_named_args, &arginfo, error);
		if (!is_ok (error))
			return FALSE;
		*out_etype = (MonoType*)typed_args [0];
		*out_len = *(gint32*)typed_args [1];
		g_free (typed_args [1]);
		g_free (typed_args);
		g_free (named_args);
		g_free (arginfo);
	}
	if (cinfo && !cinfo->cached)
		mono_custom_attrs_free (cinfo);
	return attr != NULL;
}

static void
emit_fixed_buf_conv (MonoMethodBuilder *mb, MonoType *type, MonoType *etype, int len, gboolean to_object, int *out_usize)
{
	MonoClass *klass = mono_class_from_mono_type_internal (type);
	MonoClass *eklass = mono_class_from_mono_type_internal (etype);
	int esize;

	esize = mono_class_native_size (eklass, NULL);

	MonoMarshalNative string_encoding = m_class_is_unicode (klass) ? MONO_NATIVE_LPWSTR : MONO_NATIVE_LPSTR;
	int usize = mono_class_value_size (eklass, NULL);
	int msize = mono_class_value_size (eklass, NULL);

	//printf ("FIXED: %s %d %d\n", mono_type_full_name (type), em_class_is_blittable (klass), string_encoding);

	if (m_class_is_blittable (eklass)) {
		/* copy the elements */
		mono_mb_emit_ldloc (mb, 1);
		mono_mb_emit_ldloc (mb, 0);
		mono_mb_emit_icon (mb, len * esize);
		mono_mb_emit_byte (mb, CEE_PREFIX1);
		mono_mb_emit_byte (mb, CEE_CPBLK);
	} else {
		int index_var;
		guint32 label2, label3;

		/* Emit marshalling loop */
		MonoType *int_type = mono_get_int_type ();
		index_var = mono_mb_add_local (mb, int_type);
		mono_mb_emit_byte (mb, CEE_LDC_I4_0);
		mono_mb_emit_stloc (mb, index_var);

		/* Loop header */
		label2 = mono_mb_get_label (mb);
		mono_mb_emit_ldloc (mb, index_var);
		mono_mb_emit_icon (mb, len);
		label3 = mono_mb_emit_branch (mb, CEE_BGE);

		/* src/dst is already set */

		/* Do the conversion */
		MonoTypeEnum t = etype->type;
		switch (t) {
		case MONO_TYPE_I4:
		case MONO_TYPE_U4:
		case MONO_TYPE_I1:
		case MONO_TYPE_U1:
		case MONO_TYPE_BOOLEAN:
		case MONO_TYPE_I2:
		case MONO_TYPE_U2:
		case MONO_TYPE_CHAR:
		case MONO_TYPE_I8:
		case MONO_TYPE_U8:
		case MONO_TYPE_PTR:
		case MONO_TYPE_R4:
		case MONO_TYPE_R8:
			mono_mb_emit_ldloc (mb, 1);
			mono_mb_emit_ldloc (mb, 0);
			if (t == MONO_TYPE_CHAR && string_encoding != MONO_NATIVE_LPWSTR) {
				if (to_object) {
					mono_mb_emit_byte (mb, CEE_LDIND_U1);
					mono_mb_emit_byte (mb, CEE_STIND_I2);
				} else {
					mono_mb_emit_byte (mb, CEE_LDIND_U2);
					mono_mb_emit_byte (mb, CEE_STIND_I1);
				}
				usize = 1;
			} else {
				mono_mb_emit_byte (mb, mono_type_to_ldind (etype));
				mono_mb_emit_byte (mb, mono_type_to_stind (etype));
			}
			break;
		default:
			g_assert_not_reached ();
			break;
		}

		if (to_object) {
			mono_mb_emit_add_to_local (mb, 0, usize);
			mono_mb_emit_add_to_local (mb, 1, msize);
		} else {
			mono_mb_emit_add_to_local (mb, 0, msize);
			mono_mb_emit_add_to_local (mb, 1, usize);
		}

		/* Loop footer */
		mono_mb_emit_add_to_local (mb, index_var, 1);

		mono_mb_emit_branch_label (mb, CEE_BR, label2);

		mono_mb_patch_branch (mb, label3);
	}

	*out_usize = usize * len;
}

static void
emit_ptr_to_object_conv (MonoMethodBuilder *mb, MonoType *type, MonoMarshalConv conv, MonoMarshalSpec *mspec)
{
	switch (conv) {
	case MONO_MARSHAL_CONV_BOOL_I4:
		mono_mb_emit_ldloc (mb, 1);
		mono_mb_emit_ldloc (mb, 0);
		mono_mb_emit_byte (mb, CEE_LDIND_I4);
		mono_mb_emit_byte (mb, CEE_BRFALSE_S);
		mono_mb_emit_byte (mb, 3);
		mono_mb_emit_byte (mb, CEE_LDC_I4_1);
		mono_mb_emit_byte (mb, CEE_BR_S);
		mono_mb_emit_byte (mb, 1);
		mono_mb_emit_byte (mb, CEE_LDC_I4_0);
		mono_mb_emit_byte (mb, CEE_STIND_I1);
		break;
	case MONO_MARSHAL_CONV_BOOL_VARIANTBOOL:
		mono_mb_emit_ldloc (mb, 1);
		mono_mb_emit_ldloc (mb, 0);
		mono_mb_emit_byte (mb, CEE_LDIND_I2);
		mono_mb_emit_byte (mb, CEE_BRFALSE_S);
		mono_mb_emit_byte (mb, 3);
		mono_mb_emit_byte (mb, CEE_LDC_I4_1);
		mono_mb_emit_byte (mb, CEE_BR_S);
		mono_mb_emit_byte (mb, 1);
		mono_mb_emit_byte (mb, CEE_LDC_I4_0);
		mono_mb_emit_byte (mb, CEE_STIND_I1);
		break;
	case MONO_MARSHAL_CONV_ARRAY_BYVALARRAY: {
		MonoClass *eklass = NULL;
		int esize;

		if (type->type == MONO_TYPE_SZARRAY) {
			eklass = type->data.klass;
		} else {
			g_assert_not_reached ();
		}

		esize = mono_class_native_size (eklass, NULL);

		/* create a new array */
		mono_mb_emit_ldloc (mb, 1);
		mono_mb_emit_icon (mb, mspec->data.array_data.num_elem);
		mono_mb_emit_op (mb, CEE_NEWARR, eklass);
		mono_mb_emit_byte (mb, CEE_STIND_REF);

		if (m_class_is_blittable (eklass)) {
			/* copy the elements */
			mono_mb_emit_ldloc (mb, 1);
			mono_mb_emit_byte (mb, CEE_LDIND_I);
			mono_mb_emit_icon (mb, MONO_STRUCT_OFFSET (MonoArray, vector));
			mono_mb_emit_byte (mb, CEE_ADD);
			mono_mb_emit_ldloc (mb, 0);
			mono_mb_emit_icon (mb, mspec->data.array_data.num_elem * esize);
			mono_mb_emit_byte (mb, CEE_PREFIX1);
			mono_mb_emit_byte (mb, CEE_CPBLK);
		}
		else {
			int array_var, src_var, dst_var, index_var;
			guint32 label2, label3;

			MonoType *int_type = mono_get_int_type ();
			array_var = mono_mb_add_local (mb, mono_get_object_type ());
			src_var = mono_mb_add_local (mb, int_type);
			dst_var = mono_mb_add_local (mb, int_type);

			/* set array_var */
			mono_mb_emit_ldloc (mb, 1);
			mono_mb_emit_byte (mb, CEE_LDIND_REF);
			mono_mb_emit_stloc (mb, array_var);

			/* save the old src pointer */
			mono_mb_emit_ldloc (mb, 0);
			mono_mb_emit_stloc (mb, src_var);
			/* save the old dst pointer */
			mono_mb_emit_ldloc (mb, 1);
			mono_mb_emit_stloc (mb, dst_var);

			/* Emit marshalling loop */
			index_var = mono_mb_add_local (mb, int_type);
			mono_mb_emit_byte (mb, CEE_LDC_I4_0);
			mono_mb_emit_stloc (mb, index_var);

			/* Loop header */
			label2 = mono_mb_get_label (mb);
			mono_mb_emit_ldloc (mb, index_var);
			mono_mb_emit_ldloc (mb, array_var);
			mono_mb_emit_byte (mb, CEE_LDLEN);
			label3 = mono_mb_emit_branch (mb, CEE_BGE);

			/* src is already set */

			/* Set dst */
			mono_mb_emit_ldloc (mb, array_var);
			mono_mb_emit_ldloc (mb, index_var);
			mono_mb_emit_op (mb, CEE_LDELEMA, eklass);
			mono_mb_emit_stloc (mb, 1);

			/* Do the conversion */
			emit_struct_conv (mb, eklass, TRUE);

			/* Loop footer */
			mono_mb_emit_add_to_local (mb, index_var, 1);

			mono_mb_emit_branch_label (mb, CEE_BR, label2);

			mono_mb_patch_branch (mb, label3);

			/* restore the old src pointer */
			mono_mb_emit_ldloc (mb, src_var);
			mono_mb_emit_stloc (mb, 0);
			/* restore the old dst pointer */
			mono_mb_emit_ldloc (mb, dst_var);
			mono_mb_emit_stloc (mb, 1);
		}
		break;
	}
	case MONO_MARSHAL_CONV_ARRAY_BYVALCHARARRAY: {
		MonoClass *eclass = mono_defaults.char_class;

		/* create a new array */
		mono_mb_emit_ldloc (mb, 1);
		mono_mb_emit_icon (mb, mspec->data.array_data.num_elem);
		mono_mb_emit_op (mb, CEE_NEWARR, eclass);
		mono_mb_emit_byte (mb, CEE_STIND_REF);

		mono_mb_emit_ldloc (mb, 1);
		mono_mb_emit_byte (mb, CEE_LDIND_REF);
		mono_mb_emit_ldloc (mb, 0);
		mono_mb_emit_icon (mb, mspec->data.array_data.num_elem);
		mono_mb_emit_icall (mb, mono_byvalarray_to_byte_array);
		break;
	}
	case MONO_MARSHAL_CONV_STR_BYVALSTR:
		if (mspec && mspec->native == MONO_NATIVE_BYVALTSTR && mspec->data.array_data.num_elem) {
			mono_mb_emit_ldloc (mb, 1);
			mono_mb_emit_ldloc (mb, 0);
			mono_mb_emit_icon (mb, mspec->data.array_data.num_elem);
			mono_mb_emit_icall (mb, mono_string_from_byvalstr);
		} else {
			mono_mb_emit_ldloc (mb, 1);
			mono_mb_emit_ldloc (mb, 0);
			mono_mb_emit_icall (mb, ves_icall_string_new_wrapper);
		}
		mono_mb_emit_byte (mb, CEE_STIND_REF);
		break;
	case MONO_MARSHAL_CONV_STR_BYVALWSTR:
		if (mspec && mspec->native == MONO_NATIVE_BYVALTSTR && mspec->data.array_data.num_elem) {
			mono_mb_emit_ldloc (mb, 1);
			mono_mb_emit_ldloc (mb, 0);
			mono_mb_emit_icon (mb, mspec->data.array_data.num_elem);
			mono_mb_emit_icall (mb, mono_string_from_byvalwstr);
		} else {
			mono_mb_emit_ldloc (mb, 1);
			mono_mb_emit_ldloc (mb, 0);
			mono_mb_emit_icall (mb, ves_icall_mono_string_from_utf16);
		}
		mono_mb_emit_byte (mb, CEE_STIND_REF);
		break;

	case MONO_MARSHAL_CONV_STR_ANSIBSTR:
	case MONO_MARSHAL_CONV_STR_TBSTR:
	case MONO_MARSHAL_CONV_STR_UTF8STR:
	case MONO_MARSHAL_CONV_STR_LPWSTR:
	case MONO_MARSHAL_CONV_STR_LPSTR:
	case MONO_MARSHAL_CONV_STR_LPTSTR:
	case MONO_MARSHAL_CONV_STR_BSTR: {
		mono_mb_emit_ldloc (mb, 1);
		mono_mb_emit_ldloc (mb, 0);
		mono_mb_emit_byte (mb, CEE_LDIND_I);
		mono_mb_emit_icall_id (mb, conv_to_icall (conv_str_inverse (conv), NULL));
		mono_mb_emit_byte (mb, CEE_STIND_REF);
		break;
	}

	case MONO_MARSHAL_CONV_OBJECT_STRUCT: {
		MonoClass *klass = mono_class_from_mono_type_internal (type);
		int src_var, dst_var;

		MonoType *int_type = mono_get_int_type ();
		src_var = mono_mb_add_local (mb, int_type);
		dst_var = mono_mb_add_local (mb, int_type);

		/* *dst = new object */
		mono_mb_emit_ldloc (mb, 1);
		mono_mb_emit_byte (mb, MONO_CUSTOM_PREFIX);
		mono_mb_emit_op (mb, CEE_MONO_NEWOBJ, klass);
		mono_mb_emit_byte (mb, CEE_STIND_REF);

		/* save the old src pointer */
		mono_mb_emit_ldloc (mb, 0);
		mono_mb_emit_stloc (mb, src_var);
		/* save the old dst pointer */
		mono_mb_emit_ldloc (mb, 1);
		mono_mb_emit_stloc (mb, dst_var);

		/* dst = pointer to newly created object data */
		mono_mb_emit_ldloc (mb, 1);
		mono_mb_emit_byte (mb, CEE_LDIND_I);
		mono_mb_emit_icon (mb, MONO_ABI_SIZEOF (MonoObject));
		mono_mb_emit_byte (mb, CEE_ADD);
		mono_mb_emit_stloc (mb, 1);

		emit_struct_conv (mb, klass, TRUE);

		/* restore the old src pointer */
		mono_mb_emit_ldloc (mb, src_var);
		mono_mb_emit_stloc (mb, 0);
		/* restore the old dst pointer */
		mono_mb_emit_ldloc (mb, dst_var);
		mono_mb_emit_stloc (mb, 1);
		break;
	}
	case MONO_MARSHAL_CONV_DEL_FTN: {
		MonoClass *klass = mono_class_from_mono_type_internal (type);

		mono_mb_emit_ldloc (mb, 1);
		mono_mb_emit_byte (mb, MONO_CUSTOM_PREFIX);
		mono_mb_emit_op (mb, CEE_MONO_CLASSCONST, klass);
		mono_mb_emit_ldloc (mb, 0);
		mono_mb_emit_byte (mb, CEE_LDIND_I);
		mono_mb_emit_icall (mb, mono_ftnptr_to_delegate);
		mono_mb_emit_byte (mb, CEE_STIND_REF);
		break;
	}
	case MONO_MARSHAL_CONV_ARRAY_LPARRAY: {
		char *msg = g_strdup_printf ("Structure field of type %s can't be marshalled as LPArray", m_class_get_name (mono_class_from_mono_type_internal (type)));
		mono_mb_emit_exception_marshal_directive (mb, msg);
		break;
	}

#ifndef DISABLE_COM
	case MONO_MARSHAL_CONV_OBJECT_INTERFACE:
	case MONO_MARSHAL_CONV_OBJECT_IUNKNOWN:
	case MONO_MARSHAL_CONV_OBJECT_IDISPATCH:
		mono_cominterop_emit_ptr_to_object_conv (mb, type, conv, mspec);
		break;
#endif /* DISABLE_COM */

	case MONO_MARSHAL_CONV_SAFEHANDLE: {
		/*
		 * Passing SafeHandles as ref does not allow the unmanaged code
		 * to change the SafeHandle value.   If the value is changed,
		 * we should issue a diagnostic exception (NotSupportedException)
		 * that informs the user that changes to handles in unmanaged code
		 * is not supported.
		 *
		 * Since we currently have no access to the original
		 * SafeHandle that was used during the marshalling,
		 * for now we just ignore this, and ignore/discard any
		 * changes that might have happened to the handle.
		 */
		break;
	}

	case MONO_MARSHAL_CONV_HANDLEREF: {
		/*
		 * Passing HandleRefs in a struct that is ref()ed does not
		 * copy the values back to the HandleRef
		 */
		break;
	}

	case MONO_MARSHAL_CONV_ARRAY_SAVEARRAY:
	default: {
		char *msg = g_strdup_printf ("marshaling conversion %d not implemented", conv);

		mono_mb_emit_exception_marshal_directive (mb, msg);
		break;
	}
	}
}

// On legacy Mono, LPTSTR was either UTF16 or UTF8 depending on platform
static inline MonoJitICallId
mono_string_to_platform_unicode (void)
{
#ifdef TARGET_WIN32
	return MONO_JIT_ICALL_mono_marshal_string_to_utf16;
#else
	return MONO_JIT_ICALL_mono_string_to_utf8str;
#endif
}

static inline MonoJitICallId
mono_string_from_platform_unicode (void)
{
#ifdef TARGET_WIN32
	return MONO_JIT_ICALL_ves_icall_mono_string_from_utf16;
#else
	return MONO_JIT_ICALL_ves_icall_string_new_wrapper;
#endif
}

static inline MonoJitICallId
mono_string_builder_to_platform_unicode (void)
{
#ifdef TARGET_WIN32
	return MONO_JIT_ICALL_mono_string_builder_to_utf16;
#else
	return MONO_JIT_ICALL_mono_string_builder_to_utf8;
#endif
}

static inline MonoJitICallId
mono_string_builder_from_platform_unicode (void)
{
#ifdef TARGET_WIN32
	return MONO_JIT_ICALL_mono_string_utf16_to_builder;
#else
	return MONO_JIT_ICALL_mono_string_utf8_to_builder;
#endif
}

static MonoMarshalConv
conv_str_inverse (MonoMarshalConv conv)
{
	switch (conv) {
	// AnsiBStr
	case MONO_MARSHAL_CONV_STR_ANSIBSTR:
		return MONO_MARSHAL_CONV_ANSIBSTR_STR;
	case MONO_MARSHAL_CONV_ANSIBSTR_STR:
		return MONO_MARSHAL_CONV_STR_ANSIBSTR;

	// BStr
	case MONO_MARSHAL_CONV_STR_BSTR:
		return MONO_MARSHAL_CONV_BSTR_STR;
	case MONO_MARSHAL_CONV_BSTR_STR:
		return MONO_MARSHAL_CONV_STR_BSTR;

	// LPStr
	case MONO_MARSHAL_CONV_STR_LPSTR:
		return MONO_MARSHAL_CONV_LPSTR_STR;
	case MONO_MARSHAL_CONV_LPSTR_STR:
		return MONO_MARSHAL_CONV_STR_LPSTR;

	// LPTStr
	case MONO_MARSHAL_CONV_STR_LPTSTR:
		return MONO_MARSHAL_CONV_LPTSTR_STR;
	case MONO_MARSHAL_CONV_LPTSTR_STR:
		return MONO_MARSHAL_CONV_STR_LPTSTR;

	// LPUTF8Str
	case MONO_MARSHAL_CONV_STR_UTF8STR:
		return MONO_MARSHAL_CONV_UTF8STR_STR;
	case MONO_MARSHAL_CONV_UTF8STR_STR:
		return MONO_MARSHAL_CONV_STR_UTF8STR;

	// LPWStr
	case MONO_MARSHAL_CONV_STR_LPWSTR:
		return MONO_MARSHAL_CONV_LPWSTR_STR;
	case MONO_MARSHAL_CONV_LPWSTR_STR:
		return MONO_MARSHAL_CONV_STR_LPWSTR;

	// TBStr
	case MONO_MARSHAL_CONV_STR_TBSTR:
		return MONO_MARSHAL_CONV_TBSTR_STR;
	case MONO_MARSHAL_CONV_TBSTR_STR:
		return MONO_MARSHAL_CONV_STR_TBSTR;

	default:
		g_assert_not_reached ();
	}
}

static MonoJitICallId
conv_to_icall (MonoMarshalConv conv, int *ind_store_type)
{
	// FIXME This or its caller might be a good place to inline some
	// of the wrapper logic. In particular, to produce
	// volatile stack-based handles. Being data-driven,
	// from icall-def.h.

	int dummy;
	if (!ind_store_type)
		ind_store_type = &dummy;
	*ind_store_type = CEE_STIND_I;
	switch (conv) {
	// AnsiBStr
	case MONO_MARSHAL_CONV_STR_ANSIBSTR:
		return MONO_JIT_ICALL_mono_string_to_ansibstr;
	case MONO_MARSHAL_CONV_ANSIBSTR_STR:
		*ind_store_type = CEE_STIND_REF;
		return MONO_JIT_ICALL_mono_string_from_ansibstr;

	// BStr
	case MONO_MARSHAL_CONV_STR_BSTR:
		return MONO_JIT_ICALL_mono_string_to_bstr;
	case MONO_MARSHAL_CONV_BSTR_STR:
		*ind_store_type = CEE_STIND_REF;
		return MONO_JIT_ICALL_mono_string_from_bstr_icall;

	// LPStr
	// In Mono, LPSTR was historically treated as UTF8STR
	case MONO_MARSHAL_CONV_STR_LPSTR:
		return MONO_JIT_ICALL_mono_string_to_utf8str;
	case MONO_MARSHAL_CONV_LPSTR_STR:
		*ind_store_type = CEE_STIND_REF;
		return MONO_JIT_ICALL_ves_icall_string_new_wrapper;
	case MONO_MARSHAL_CONV_SB_LPSTR:
		return MONO_JIT_ICALL_mono_string_builder_to_utf8;
	case MONO_MARSHAL_CONV_LPSTR_SB:
		*ind_store_type = CEE_STIND_REF;
		return MONO_JIT_ICALL_mono_string_utf8_to_builder;

	// LPTStr
	// FIXME: This is how LPTStr was handled on legacy, but it's not correct and for netcore we should implement this more properly.
	// This type is supposed to detect ANSI or UTF16 (as LPTStr can be either depending on _UNICODE) and handle it accordingly.
	// The CoreCLR test for this type only tests as LPWSTR regardless of platform.
	case MONO_MARSHAL_CONV_STR_LPTSTR:
		return mono_string_to_platform_unicode ();
	case MONO_MARSHAL_CONV_LPTSTR_STR:
		*ind_store_type = CEE_STIND_REF;
		return mono_string_from_platform_unicode ();
	case MONO_MARSHAL_CONV_SB_LPTSTR:
		return mono_string_builder_to_platform_unicode ();
	case MONO_MARSHAL_CONV_LPTSTR_SB:
		*ind_store_type = CEE_STIND_REF;
		return mono_string_builder_from_platform_unicode ();

	// LPUTF8Str
	case MONO_MARSHAL_CONV_STR_UTF8STR:
		return MONO_JIT_ICALL_mono_string_to_utf8str;
	case MONO_MARSHAL_CONV_UTF8STR_STR:
		*ind_store_type = CEE_STIND_REF;
		return MONO_JIT_ICALL_ves_icall_string_new_wrapper;
	case MONO_MARSHAL_CONV_SB_UTF8STR:
		return MONO_JIT_ICALL_mono_string_builder_to_utf8;
	case MONO_MARSHAL_CONV_UTF8STR_SB:
		*ind_store_type = CEE_STIND_REF;
		return MONO_JIT_ICALL_mono_string_utf8_to_builder;

	// LPWStr
	case MONO_MARSHAL_CONV_STR_LPWSTR:
		return MONO_JIT_ICALL_mono_marshal_string_to_utf16;
	case MONO_MARSHAL_CONV_LPWSTR_STR:
		*ind_store_type = CEE_STIND_REF;
		return MONO_JIT_ICALL_ves_icall_mono_string_from_utf16;
	case MONO_MARSHAL_CONV_SB_LPWSTR:
		return MONO_JIT_ICALL_mono_string_builder_to_utf16;
	case MONO_MARSHAL_CONV_LPWSTR_SB:
		*ind_store_type = CEE_STIND_REF;
		return MONO_JIT_ICALL_mono_string_utf16_to_builder;

	// TBStr
	case MONO_MARSHAL_CONV_STR_TBSTR:
		return MONO_JIT_ICALL_mono_string_to_tbstr;
	case MONO_MARSHAL_CONV_TBSTR_STR:
		*ind_store_type = CEE_STIND_REF;
		return MONO_JIT_ICALL_mono_string_from_tbstr;

	case MONO_MARSHAL_CONV_STR_BYVALSTR:
		return MONO_JIT_ICALL_mono_string_to_byvalstr;
	case MONO_MARSHAL_CONV_STR_BYVALWSTR:
		return MONO_JIT_ICALL_mono_string_to_byvalwstr;

	case MONO_MARSHAL_CONV_DEL_FTN:
		return MONO_JIT_ICALL_mono_delegate_to_ftnptr;
	case MONO_MARSHAL_CONV_FTN_DEL:
		*ind_store_type = CEE_STIND_REF;
		return MONO_JIT_ICALL_mono_ftnptr_to_delegate;

	case MONO_MARSHAL_CONV_ARRAY_SAVEARRAY:
		return MONO_JIT_ICALL_mono_array_to_savearray;
	case MONO_MARSHAL_FREE_ARRAY:
		return MONO_JIT_ICALL_mono_marshal_free_array;

	case MONO_MARSHAL_CONV_ARRAY_LPARRAY:
		return MONO_JIT_ICALL_mono_array_to_lparray;
	case MONO_MARSHAL_FREE_LPARRAY:
		return MONO_JIT_ICALL_mono_free_lparray;

	default:
		g_assert_not_reached ();
	}

	return MONO_JIT_ICALL_ZeroIsReserved;
}

static void
emit_object_to_ptr_conv (MonoMethodBuilder *mb, MonoType *type, MonoMarshalConv conv, MonoMarshalSpec *mspec)
{
	int pos;
	int stind_op;

	switch (conv) {
	case MONO_MARSHAL_CONV_BOOL_I4:
		mono_mb_emit_ldloc (mb, 1);
		mono_mb_emit_ldloc (mb, 0);
		mono_mb_emit_byte (mb, CEE_LDIND_U1);
		mono_mb_emit_byte (mb, CEE_STIND_I4);
		break;
	case MONO_MARSHAL_CONV_BOOL_VARIANTBOOL:
		mono_mb_emit_ldloc (mb, 1);
		mono_mb_emit_ldloc (mb, 0);
		mono_mb_emit_byte (mb, CEE_LDIND_U1);
		mono_mb_emit_byte (mb, CEE_NEG);
		mono_mb_emit_byte (mb, CEE_STIND_I2);
		break;
	case MONO_MARSHAL_CONV_STR_UTF8STR:
	case MONO_MARSHAL_CONV_STR_LPWSTR:
	case MONO_MARSHAL_CONV_STR_LPSTR:
	case MONO_MARSHAL_CONV_STR_LPTSTR:
	case MONO_MARSHAL_CONV_STR_BSTR:
	case MONO_MARSHAL_CONV_STR_ANSIBSTR:
	case MONO_MARSHAL_CONV_STR_TBSTR: {
		int pos;

		/* free space if free == true */
		mono_mb_emit_ldloc (mb, 2);
		pos = mono_mb_emit_short_branch (mb, CEE_BRFALSE_S);
		mono_mb_emit_ldloc (mb, 1);
		mono_mb_emit_byte (mb, CEE_LDIND_I);
		mono_mb_emit_icall (mb, g_free); // aka monoeg_g_free
		mono_mb_patch_short_branch (mb, pos);

		mono_mb_emit_ldloc (mb, 1);
		mono_mb_emit_ldloc (mb, 0);
		mono_mb_emit_byte (mb, CEE_LDIND_REF);
		mono_mb_emit_icall_id (mb, conv_to_icall (conv, &stind_op));
		mono_mb_emit_byte (mb, stind_op);
		break;
	}
	case MONO_MARSHAL_CONV_ARRAY_SAVEARRAY:
	case MONO_MARSHAL_CONV_ARRAY_LPARRAY:
	case MONO_MARSHAL_CONV_DEL_FTN:
		mono_mb_emit_ldloc (mb, 1);
		mono_mb_emit_ldloc (mb, 0);
		mono_mb_emit_byte (mb, CEE_LDIND_REF);
		mono_mb_emit_icall_id (mb, conv_to_icall (conv, &stind_op));
		mono_mb_emit_byte (mb, stind_op);
		break;
	case MONO_MARSHAL_CONV_STR_BYVALSTR:
	case MONO_MARSHAL_CONV_STR_BYVALWSTR: {
		g_assert (mspec);

		mono_mb_emit_ldloc (mb, 1); /* dst */
		mono_mb_emit_ldloc (mb, 0);
		mono_mb_emit_byte (mb, CEE_LDIND_REF); /* src String */
		mono_mb_emit_icon (mb, mspec->data.array_data.num_elem);
		mono_mb_emit_icall_id (mb, conv_to_icall (conv, NULL));
		break;
	}
	case MONO_MARSHAL_CONV_ARRAY_BYVALARRAY: {
		MonoClass *eklass = NULL;
		int esize;

		if (type->type == MONO_TYPE_SZARRAY) {
			eklass = type->data.klass;
		} else if (type->type == MONO_TYPE_ARRAY) {
			eklass = type->data.array->eklass;
			g_assert(m_class_is_blittable (eklass));
		} else {
			g_assert_not_reached ();
		}

		if (m_class_is_valuetype (eklass))
			esize = mono_class_native_size (eklass, NULL);
		else
			esize = TARGET_SIZEOF_VOID_P;

		mono_mb_emit_ldloc (mb, 0);
		mono_mb_emit_byte (mb, CEE_LDIND_REF);
		pos = mono_mb_emit_branch (mb, CEE_BRFALSE);

		if (m_class_is_blittable (eklass)) {
			mono_mb_emit_ldloc (mb, 1);
			mono_mb_emit_ldloc (mb, 0);
			mono_mb_emit_byte (mb, CEE_LDIND_REF);
			mono_mb_emit_ldflda (mb, MONO_STRUCT_OFFSET (MonoArray, vector));
			mono_mb_emit_icon (mb, mspec->data.array_data.num_elem * esize);
			mono_mb_emit_byte (mb, CEE_PREFIX1);
			mono_mb_emit_byte (mb, CEE_CPBLK);
		} else {
			int array_var, src_var, dst_var, index_var;
			guint32 label2, label3;

			MonoType *int_type = mono_get_int_type ();
			MonoType *object_type = mono_get_object_type ();
			array_var = mono_mb_add_local (mb, object_type);
			src_var = mono_mb_add_local (mb, int_type);
			dst_var = mono_mb_add_local (mb, int_type);

			/* set array_var */
			mono_mb_emit_ldloc (mb, 0);
			mono_mb_emit_byte (mb, CEE_LDIND_REF);
			mono_mb_emit_stloc (mb, array_var);

			/* save the old src pointer */
			mono_mb_emit_ldloc (mb, 0);
			mono_mb_emit_stloc (mb, src_var);
			/* save the old dst pointer */
			mono_mb_emit_ldloc (mb, 1);
			mono_mb_emit_stloc (mb, dst_var);

			/* Emit marshalling loop */
			index_var = mono_mb_add_local (mb, int_type);
			mono_mb_emit_byte (mb, CEE_LDC_I4_0);
			mono_mb_emit_stloc (mb, index_var);

			/* Loop header */
			label2 = mono_mb_get_label (mb);
			mono_mb_emit_ldloc (mb, index_var);
			mono_mb_emit_ldloc (mb, array_var);
			mono_mb_emit_byte (mb, CEE_LDLEN);
			label3 = mono_mb_emit_branch (mb, CEE_BGE);

			/* Set src */
			mono_mb_emit_ldloc (mb, array_var);
			mono_mb_emit_ldloc (mb, index_var);
			mono_mb_emit_op (mb, CEE_LDELEMA, eklass);
			mono_mb_emit_stloc (mb, 0);

			/* dst is already set */

			/* Do the conversion */
			emit_struct_conv (mb, eklass, FALSE);

			/* Loop footer */
			mono_mb_emit_add_to_local (mb, index_var, 1);

			mono_mb_emit_branch_label (mb, CEE_BR, label2);

			mono_mb_patch_branch (mb, label3);

			/* restore the old src pointer */
			mono_mb_emit_ldloc (mb, src_var);
			mono_mb_emit_stloc (mb, 0);
			/* restore the old dst pointer */
			mono_mb_emit_ldloc (mb, dst_var);
			mono_mb_emit_stloc (mb, 1);
		}

		mono_mb_patch_branch (mb, pos);
		break;
	}
	case MONO_MARSHAL_CONV_ARRAY_BYVALCHARARRAY: {
		mono_mb_emit_ldloc (mb, 0);
		mono_mb_emit_byte (mb, CEE_LDIND_REF);
		pos = mono_mb_emit_short_branch (mb, CEE_BRFALSE_S);

		mono_mb_emit_ldloc (mb, 1);
		mono_mb_emit_ldloc (mb, 0);
		mono_mb_emit_byte (mb, CEE_LDIND_REF);
		mono_mb_emit_icon (mb, mspec->data.array_data.num_elem);
		mono_mb_emit_icall (mb, mono_array_to_byte_byvalarray);
		mono_mb_patch_short_branch (mb, pos);
		break;
	}
	case MONO_MARSHAL_CONV_OBJECT_STRUCT: {
		int src_var, dst_var;

		MonoType *int_type = mono_get_int_type ();
		src_var = mono_mb_add_local (mb, int_type);
		dst_var = mono_mb_add_local (mb, int_type);

		mono_mb_emit_ldloc (mb, 0);
		mono_mb_emit_byte (mb, CEE_LDIND_I);
		pos = mono_mb_emit_branch (mb, CEE_BRFALSE);

		/* save the old src pointer */
		mono_mb_emit_ldloc (mb, 0);
		mono_mb_emit_stloc (mb, src_var);
		/* save the old dst pointer */
		mono_mb_emit_ldloc (mb, 1);
		mono_mb_emit_stloc (mb, dst_var);

		/* src = pointer to object data */
		mono_mb_emit_ldloc (mb, 0);
		mono_mb_emit_byte (mb, CEE_LDIND_I);
		mono_mb_emit_icon (mb, MONO_ABI_SIZEOF (MonoObject));
		mono_mb_emit_byte (mb, CEE_ADD);
		mono_mb_emit_stloc (mb, 0);

		emit_struct_conv (mb, mono_class_from_mono_type_internal (type), FALSE);

		/* restore the old src pointer */
		mono_mb_emit_ldloc (mb, src_var);
		mono_mb_emit_stloc (mb, 0);
		/* restore the old dst pointer */
		mono_mb_emit_ldloc (mb, dst_var);
		mono_mb_emit_stloc (mb, 1);

		mono_mb_patch_branch (mb, pos);
		break;
	}

#ifndef DISABLE_COM
	case MONO_MARSHAL_CONV_OBJECT_INTERFACE:
	case MONO_MARSHAL_CONV_OBJECT_IDISPATCH:
	case MONO_MARSHAL_CONV_OBJECT_IUNKNOWN:
		mono_cominterop_emit_object_to_ptr_conv (mb, type, conv, mspec);
		break;
#endif /* DISABLE_COM */

	case MONO_MARSHAL_CONV_SAFEHANDLE: {
		int pos;

		mono_mb_emit_ldloc (mb, 0);
		mono_mb_emit_byte (mb, CEE_LDIND_I);
		pos = mono_mb_emit_branch (mb, CEE_BRTRUE);
		mono_mb_emit_exception (mb, "ArgumentNullException", NULL);
		mono_mb_patch_branch (mb, pos);

		/* Pull the handle field from SafeHandle */
		mono_mb_emit_ldloc (mb, 1);
		mono_mb_emit_ldloc (mb, 0);
		mono_mb_emit_byte (mb, CEE_LDIND_I);
		mono_mb_emit_ldflda (mb, MONO_STRUCT_OFFSET (MonoSafeHandle, handle));
		mono_mb_emit_byte (mb, CEE_LDIND_I);
		mono_mb_emit_byte (mb, CEE_STIND_I);
		break;
	}

	case MONO_MARSHAL_CONV_HANDLEREF: {
		mono_mb_emit_ldloc (mb, 1);
		mono_mb_emit_ldloc (mb, 0);
		mono_mb_emit_icon (mb, MONO_STRUCT_OFFSET (MonoHandleRef, handle));
		mono_mb_emit_byte (mb, CEE_ADD);
		mono_mb_emit_byte (mb, CEE_LDIND_I);
		mono_mb_emit_byte (mb, CEE_STIND_I);
		break;
	}

	default: {
		g_error ("marshalling conversion %d not implemented", conv);
	}
	}
}

#ifndef DISABLE_COM

// FIXME There are multiple caches of "Clear".
G_GNUC_UNUSED
static MonoMethod*
mono_get_Variant_Clear (void)
{
	MONO_STATIC_POINTER_INIT (MonoMethod, variant_clear)
		variant_clear = get_method_nofail (mono_class_get_variant_class (), "Clear", 0, 0);
	MONO_STATIC_POINTER_INIT_END (MonoMethod, variant_clear)

	g_assert (variant_clear);
	return variant_clear;
}

#endif

// FIXME There are multiple caches of "GetObjectForNativeVariant".
G_GNUC_UNUSED
static MonoMethod*
mono_get_Marshal_GetObjectForNativeVariant (void)
{
	MONO_STATIC_POINTER_INIT (MonoMethod, get_object_for_native_variant)
		get_object_for_native_variant = get_method_nofail (mono_defaults.marshal_class, "GetObjectForNativeVariant", 1, 0);
	MONO_STATIC_POINTER_INIT_END (MonoMethod, get_object_for_native_variant)

	g_assert (get_object_for_native_variant);
	return get_object_for_native_variant;
}

// FIXME There are multiple caches of "GetNativeVariantForObject".
G_GNUC_UNUSED
static MonoMethod*
mono_get_Marshal_GetNativeVariantForObject (void)
{
	MONO_STATIC_POINTER_INIT (MonoMethod, get_native_variant_for_object)
		get_native_variant_for_object = get_method_nofail (mono_defaults.marshal_class, "GetNativeVariantForObject", 2, 0);
	MONO_STATIC_POINTER_INIT_END (MonoMethod, get_native_variant_for_object)

	g_assert (get_native_variant_for_object);
	return get_native_variant_for_object;
}

static void
emit_struct_conv_full (MonoMethodBuilder *mb, MonoClass *klass, gboolean to_object,
						int offset_of_first_child_field, MonoMarshalNative string_encoding)
{
	MonoMarshalType *info;
	int i;

	if (m_class_get_parent (klass))
		emit_struct_conv_full (mb, m_class_get_parent (klass), to_object, offset_of_first_nonstatic_field (klass), string_encoding);

	info = mono_marshal_load_type_info (klass);

	if (info->native_size == 0)
		return;

	if (m_class_is_blittable (klass)) {
		int usize = mono_class_value_size (klass, NULL);
		g_assert (usize == info->native_size);
		mono_mb_emit_ldloc (mb, 1);
		mono_mb_emit_ldloc (mb, 0);
		mono_mb_emit_icon (mb, usize);
		mono_mb_emit_byte (mb, CEE_PREFIX1);
		mono_mb_emit_byte (mb, CEE_CPBLK);

		if (to_object) {
			mono_mb_emit_add_to_local (mb, 0, usize);
			mono_mb_emit_add_to_local (mb, 1, offset_of_first_child_field);
		} else {
			mono_mb_emit_add_to_local (mb, 0, offset_of_first_child_field);
			mono_mb_emit_add_to_local (mb, 1, usize);
		}
		return;
	}

	if (klass != mono_class_try_get_safehandle_class ()) {
		if (mono_class_is_auto_layout (klass)) {
			char *msg = g_strdup_printf ("Type %s which is passed to unmanaged code must have a StructLayout attribute.",
										 mono_type_full_name (m_class_get_byval_arg (klass)));
			mono_mb_emit_exception_marshal_directive (mb, msg);
			return;
		}
	}

	for (i = 0; i < info->num_fields; i++) {
		MonoMarshalNative ntype;
		MonoMarshalConv conv;
		MonoType *ftype = info->fields [i].field->type;
		int msize = 0;
		int usize = 0;
		gboolean last_field = i < (info->num_fields -1) ? 0 : 1;

		if (ftype->attrs & FIELD_ATTRIBUTE_STATIC)
			continue;

		ntype = (MonoMarshalNative)mono_type_to_unmanaged (ftype, info->fields [i].mspec, TRUE, m_class_is_unicode (klass), &conv);

		if (last_field) {
			msize = m_class_get_instance_size (klass) - m_field_get_offset (info->fields [i].field);
			usize = info->native_size - info->fields [i].offset;
		} else {
			msize = m_field_get_offset (info->fields [i + 1].field) - m_field_get_offset (info->fields [i].field);
			usize = info->fields [i + 1].offset - info->fields [i].offset;
		}

		if (klass != mono_class_try_get_safehandle_class ()){
			/*
			 * FIXME: Should really check for usize==0 and msize>0, but we apply
			 * the layout to the managed structure as well.
			 */

			if (mono_class_is_explicit_layout (klass) && (usize == 0)) {
				if (MONO_TYPE_IS_REFERENCE (info->fields [i].field->type) ||
				    ((!last_field && MONO_TYPE_IS_REFERENCE (info->fields [i + 1].field->type))))
					g_error ("Type %s which has an [ExplicitLayout] attribute cannot have a "
						 "reference field at the same offset as another field.",
						 mono_type_full_name (m_class_get_byval_arg (klass)));
			}
		}

		switch (conv) {
		case MONO_MARSHAL_CONV_NONE: {
			int t;

			//XXX a byref field!?!? that's not allowed! and worse, it might miss a WB
			g_assert (!m_type_is_byref (ftype));
			if (ftype->type == MONO_TYPE_I || ftype->type == MONO_TYPE_U) {
				mono_mb_emit_ldloc (mb, 1);
				mono_mb_emit_ldloc (mb, 0);
				mono_mb_emit_byte (mb, CEE_LDIND_I);
				mono_mb_emit_byte (mb, CEE_STIND_I);
				break;
			}

		handle_enum:
			t = ftype->type;
			switch (t) {
			case MONO_TYPE_I4:
			case MONO_TYPE_U4:
			case MONO_TYPE_I1:
			case MONO_TYPE_U1:
			case MONO_TYPE_BOOLEAN:
			case MONO_TYPE_I2:
			case MONO_TYPE_U2:
			case MONO_TYPE_CHAR:
			case MONO_TYPE_I8:
			case MONO_TYPE_U8:
			case MONO_TYPE_PTR:
			case MONO_TYPE_R4:
			case MONO_TYPE_R8:
				mono_mb_emit_ldloc (mb, 1);
				mono_mb_emit_ldloc (mb, 0);
				if (t == MONO_TYPE_CHAR && ntype == MONO_NATIVE_U1 && string_encoding != MONO_NATIVE_LPWSTR) {
					if (to_object) {
						mono_mb_emit_byte (mb, CEE_LDIND_U1);
						mono_mb_emit_byte (mb, CEE_STIND_I2);
					} else {
						mono_mb_emit_byte (mb, CEE_LDIND_U2);
						mono_mb_emit_byte (mb, CEE_STIND_I1);
					}
				} else {
					mono_mb_emit_byte (mb, mono_type_to_ldind (ftype));
					mono_mb_emit_byte (mb, mono_type_to_stind (ftype));
				}
				break;
			case MONO_TYPE_GENERICINST:
				if (!mono_type_generic_inst_is_valuetype (ftype)) {
					char *msg = g_strdup_printf ("Generic type %s cannot be marshaled as field in a struct.",
						mono_type_full_name (ftype));
					mono_mb_emit_exception_marshal_directive (mb, msg);
					break;
				}
				/* fall through */
			case MONO_TYPE_VALUETYPE: {
				int src_var, dst_var;
				MonoType *etype;
				int len;

				if (t == MONO_TYPE_VALUETYPE && m_class_is_enumtype (ftype->data.klass)) {
					ftype = mono_class_enum_basetype_internal (ftype->data.klass);
					goto handle_enum;
				}

				MonoType *int_type = mono_get_int_type ();
				src_var = mono_mb_add_local (mb, int_type);
				dst_var = mono_mb_add_local (mb, int_type);

				/* save the old src pointer */
				mono_mb_emit_ldloc (mb, 0);
				mono_mb_emit_stloc (mb, src_var);
				/* save the old dst pointer */
				mono_mb_emit_ldloc (mb, 1);
				mono_mb_emit_stloc (mb, dst_var);

				if (get_fixed_buffer_attr (info->fields [i].field, &etype, &len)) {
					emit_fixed_buf_conv (mb, ftype, etype, len, to_object, &usize);
				} else {
					emit_struct_conv (mb, mono_class_from_mono_type_internal (ftype), to_object);
				}

				/* restore the old src pointer */
				mono_mb_emit_ldloc (mb, src_var);
				mono_mb_emit_stloc (mb, 0);
				/* restore the old dst pointer */
				mono_mb_emit_ldloc (mb, dst_var);
				mono_mb_emit_stloc (mb, 1);
				break;
			}
			case MONO_TYPE_OBJECT: {
#ifndef DISABLE_COM
				if (to_object) {
					mono_mb_emit_ldloc (mb, 1);
					mono_mb_emit_ldloc (mb, 0);
					mono_mb_emit_managed_call (mb, mono_get_Marshal_GetObjectForNativeVariant (), NULL);
					mono_mb_emit_byte (mb, CEE_STIND_REF);

					mono_mb_emit_ldloc (mb, 0);
					mono_mb_emit_managed_call (mb, mono_get_Variant_Clear (), NULL);
				}
				else {
					mono_mb_emit_ldloc (mb, 0);
					mono_mb_emit_byte(mb, CEE_LDIND_REF);
					mono_mb_emit_ldloc (mb, 1);
					mono_mb_emit_managed_call (mb, mono_get_Marshal_GetNativeVariantForObject (), NULL);
				}
#else
				char *msg = g_strdup_printf ("COM support was disabled at compilation time.");
				mono_mb_emit_exception_marshal_directive (mb, msg);
#endif
				break;
			}

			default:
				g_warning ("marshaling type %02x not implemented", ftype->type);
				g_assert_not_reached ();
			}
			break;
		}
		default: {
			int src_var, dst_var;

			MonoType *int_type = mono_get_int_type ();
			src_var = mono_mb_add_local (mb, int_type);
			dst_var = mono_mb_add_local (mb, int_type);

			/* save the old src pointer */
			mono_mb_emit_ldloc (mb, 0);
			mono_mb_emit_stloc (mb, src_var);
			/* save the old dst pointer */
			mono_mb_emit_ldloc (mb, 1);
			mono_mb_emit_stloc (mb, dst_var);

			if (to_object)
				emit_ptr_to_object_conv (mb, ftype, conv, info->fields [i].mspec);
			else
				emit_object_to_ptr_conv (mb, ftype, conv, info->fields [i].mspec);

			/* restore the old src pointer */
			mono_mb_emit_ldloc (mb, src_var);
			mono_mb_emit_stloc (mb, 0);
			/* restore the old dst pointer */
			mono_mb_emit_ldloc (mb, dst_var);
			mono_mb_emit_stloc (mb, 1);
		}
		}

		if (to_object) {
			mono_mb_emit_add_to_local (mb, 0, usize);
			mono_mb_emit_add_to_local (mb, 1, msize);
		} else {
			mono_mb_emit_add_to_local (mb, 0, msize);
			mono_mb_emit_add_to_local (mb, 1, usize);
		}
	}
}

static void
emit_struct_conv (MonoMethodBuilder *mb, MonoClass *klass, gboolean to_object)
{
	emit_struct_conv_full (mb, klass, to_object, 0, (MonoMarshalNative)-1);
}

static void
emit_struct_free (MonoMethodBuilder *mb, MonoClass *klass, int struct_var)
{
	/* Call DestroyStructure */
	/* FIXME: Only do this if needed */
	mono_mb_emit_byte (mb, MONO_CUSTOM_PREFIX);
	mono_mb_emit_op (mb, CEE_MONO_CLASSCONST, klass);
	mono_mb_emit_ldloc (mb, struct_var);
	mono_mb_emit_icall (mb, mono_struct_delete_old);
}

static void
emit_thread_interrupt_checkpoint_call (MonoMethodBuilder *mb, MonoJitICallId checkpoint_icall_id)
{
	int pos_noabort, pos_noex;

	mono_mb_emit_byte (mb, MONO_CUSTOM_PREFIX);
	mono_mb_emit_byte (mb, CEE_MONO_LDPTR_INT_REQ_FLAG);
	mono_mb_emit_no_nullcheck (mb);
	mono_mb_emit_byte (mb, CEE_LDIND_U4);
	pos_noabort = mono_mb_emit_branch (mb, CEE_BRFALSE);

	mono_mb_emit_byte (mb, MONO_CUSTOM_PREFIX);
	mono_mb_emit_byte (mb, CEE_MONO_NOT_TAKEN);

	mono_mb_emit_icall_id (mb, checkpoint_icall_id);
	/* Throw the exception returned by the checkpoint function, if any */
	mono_mb_emit_byte (mb, CEE_DUP);
	pos_noex = mono_mb_emit_branch (mb, CEE_BRFALSE);

	mono_mb_emit_byte (mb, CEE_DUP);
	mono_mb_emit_ldflda (mb, MONO_STRUCT_OFFSET (MonoException, caught_in_unmanaged));
	mono_mb_emit_byte (mb, CEE_LDC_I4_1);
	mono_mb_emit_no_nullcheck (mb);
	mono_mb_emit_byte (mb, CEE_STIND_I4);

	mono_mb_emit_byte (mb, MONO_CUSTOM_PREFIX);
	mono_mb_emit_byte (mb, CEE_MONO_RETHROW);

	mono_mb_patch_branch (mb, pos_noex);
	mono_mb_emit_byte (mb, CEE_POP);

	mono_mb_patch_branch (mb, pos_noabort);
}

static void
emit_thread_interrupt_checkpoint (MonoMethodBuilder *mb)
{
	// FIXME Put a boolean in MonoMethodBuilder instead.
	if (strstr (mb->name, "mono_thread_interruption_checkpoint"))
		return;

	emit_thread_interrupt_checkpoint_call (mb, MONO_JIT_ICALL_mono_thread_interruption_checkpoint);
}

static void
emit_thread_force_interrupt_checkpoint (MonoMethodBuilder *mb)
{
	emit_thread_interrupt_checkpoint_call (mb, MONO_JIT_ICALL_mono_thread_force_interruption_checkpoint_noraise);
}

void
mono_marshal_emit_thread_interrupt_checkpoint (MonoMethodBuilder *mb)
{
	emit_thread_interrupt_checkpoint (mb);
}

void
mono_marshal_emit_thread_force_interrupt_checkpoint (MonoMethodBuilder *mb)
{
	emit_thread_force_interrupt_checkpoint (mb);
}

int
mono_mb_emit_save_args (MonoMethodBuilder *mb, MonoMethodSignature *sig, gboolean save_this)
{
	int i, params_var, tmp_var;

	MonoType *int_type = mono_get_int_type ();
	/* allocate local (pointer) *params[] */
	params_var = mono_mb_add_local (mb, int_type);
	/* allocate local (pointer) tmp */
	tmp_var = mono_mb_add_local (mb, int_type);

	/* alloate space on stack to store an array of pointers to the arguments */
	mono_mb_emit_icon (mb, TARGET_SIZEOF_VOID_P * (sig->param_count + 1));
	mono_mb_emit_byte (mb, CEE_PREFIX1);
	mono_mb_emit_byte (mb, CEE_LOCALLOC);
	mono_mb_emit_stloc (mb, params_var);

	/* tmp = params */
	mono_mb_emit_ldloc (mb, params_var);
	mono_mb_emit_stloc (mb, tmp_var);

	if (save_this && sig->hasthis) {
		mono_mb_emit_ldloc (mb, tmp_var);
		mono_mb_emit_ldarg_addr (mb, 0);
		mono_mb_emit_byte (mb, CEE_STIND_I);
		/* tmp = tmp + sizeof (gpointer) */
		if (sig->param_count)
			mono_mb_emit_add_to_local (mb, tmp_var, TARGET_SIZEOF_VOID_P);

	}

	for (i = 0; i < sig->param_count; i++) {
		mono_mb_emit_ldloc (mb, tmp_var);
		mono_mb_emit_ldarg_addr (mb, i + sig->hasthis);
		mono_mb_emit_byte (mb, CEE_STIND_I);
		/* tmp = tmp + sizeof (gpointer) */
		if (i < (sig->param_count - 1))
			mono_mb_emit_add_to_local (mb, tmp_var, TARGET_SIZEOF_VOID_P);
	}

	return params_var;
}


void
mono_mb_emit_restore_result (MonoMethodBuilder *mb, MonoType *return_type)
{
	MonoType *t = mono_type_get_underlying_type (return_type);
	MonoType *int_type = mono_get_int_type ();

	if (m_type_is_byref (return_type))
		return_type = int_type;

	switch (t->type) {
	case MONO_TYPE_VOID:
		g_assert_not_reached ();
		break;
	case MONO_TYPE_PTR:
	case MONO_TYPE_FNPTR:
	case MONO_TYPE_STRING:
	case MONO_TYPE_CLASS:
	case MONO_TYPE_OBJECT:
	case MONO_TYPE_ARRAY:
	case MONO_TYPE_SZARRAY:
		/* nothing to do */
		break;
	case MONO_TYPE_U1:
	case MONO_TYPE_BOOLEAN:
	case MONO_TYPE_I1:
	case MONO_TYPE_U2:
	case MONO_TYPE_CHAR:
	case MONO_TYPE_I2:
	case MONO_TYPE_I:
	case MONO_TYPE_U:
	case MONO_TYPE_I4:
	case MONO_TYPE_U4:
	case MONO_TYPE_U8:
	case MONO_TYPE_I8:
	case MONO_TYPE_R4:
	case MONO_TYPE_R8:
		mono_mb_emit_op (mb, CEE_UNBOX, mono_class_from_mono_type_internal (return_type));
		mono_mb_emit_byte (mb, mono_type_to_ldind (return_type));
		break;
	case MONO_TYPE_GENERICINST:
		if (!mono_type_generic_inst_is_valuetype (t))
			break;
		/* fall through */
	case MONO_TYPE_VALUETYPE: {
		MonoClass *klass = mono_class_from_mono_type_internal (return_type);
		mono_mb_emit_op (mb, CEE_UNBOX, klass);
		mono_mb_emit_op (mb, CEE_LDOBJ, klass);
		break;
	}
	case MONO_TYPE_VAR:
	case MONO_TYPE_MVAR: {
		MonoClass *klass = mono_class_from_mono_type_internal (return_type);
		mono_mb_emit_op (mb, CEE_UNBOX_ANY, klass);
		break;
	}
	default:
		g_warning ("type 0x%x not handled", return_type->type);
		g_assert_not_reached ();
	}

	mono_mb_emit_byte (mb, CEE_RET);
}

/*
 * emit_invoke_call:
 *
 *   Emit the call to the wrapper method from a runtime invoke wrapper.
 */
static void
emit_invoke_call (MonoMethodBuilder *mb, MonoMethod *method,
				  MonoMethodSignature *sig, MonoMethodSignature *callsig,
				  int loc_res,
				  gboolean virtual_, gboolean need_direct_wrapper)
{
	int i;
	int *tmp_nullable_locals;
	gboolean void_ret = FALSE;
	gboolean string_ctor = method && method->string_ctor;

	if (virtual_) {
		g_assert (sig->hasthis);
		g_assert (method->flags & METHOD_ATTRIBUTE_VIRTUAL);
	}

	if (sig->hasthis) {
		if (string_ctor) {
			/* This will call the code emitted by mono_marshal_get_native_wrapper () which ignores it */
			mono_mb_emit_icon (mb, 0);
			mono_mb_emit_byte (mb, CEE_CONV_I);
		} else {
			mono_mb_emit_ldarg (mb, 0);
		}
	}

	tmp_nullable_locals = g_new0 (int, sig->param_count);

	for (i = 0; i < sig->param_count; i++) {
		MonoType *t = sig->params [i];
		int type;

		mono_mb_emit_ldarg (mb, 1);
		if (i) {
			mono_mb_emit_icon (mb, TARGET_SIZEOF_VOID_P * i);
			mono_mb_emit_byte (mb, CEE_ADD);
		}

		if (m_type_is_byref (t)) {
			mono_mb_emit_byte (mb, CEE_LDIND_I);
			/* A Nullable<T> type don't have a boxed form, it's either null or a boxed T.
			 * So to make this work we unbox it to a local variablee and push a reference to that.
			 */
			if (t->type == MONO_TYPE_GENERICINST && mono_class_is_nullable (mono_class_from_mono_type_internal (t))) {
				tmp_nullable_locals [i] = mono_mb_add_local (mb, m_class_get_byval_arg (mono_class_from_mono_type_internal (t)));

				mono_mb_emit_op (mb, CEE_UNBOX_ANY, mono_class_from_mono_type_internal (t));
				mono_mb_emit_stloc (mb, tmp_nullable_locals [i]);
				mono_mb_emit_ldloc_addr (mb, tmp_nullable_locals [i]);
			}
			continue;
		}

		type = sig->params [i]->type;
handle_enum:
		switch (type) {
		case MONO_TYPE_I1:
		case MONO_TYPE_BOOLEAN:
		case MONO_TYPE_U1:
		case MONO_TYPE_I2:
		case MONO_TYPE_U2:
		case MONO_TYPE_CHAR:
		case MONO_TYPE_I:
		case MONO_TYPE_U:
		case MONO_TYPE_I4:
		case MONO_TYPE_U4:
		case MONO_TYPE_R4:
		case MONO_TYPE_R8:
		case MONO_TYPE_I8:
		case MONO_TYPE_U8:
			mono_mb_emit_no_nullcheck (mb);
			mono_mb_emit_byte (mb, CEE_LDIND_I);
			mono_mb_emit_no_nullcheck (mb);
			mono_mb_emit_byte (mb, mono_type_to_ldind (sig->params [i]));
			break;
		case MONO_TYPE_STRING:
		case MONO_TYPE_CLASS:
		case MONO_TYPE_ARRAY:
		case MONO_TYPE_PTR:
		case MONO_TYPE_FNPTR:
		case MONO_TYPE_SZARRAY:
		case MONO_TYPE_OBJECT:
			mono_mb_emit_no_nullcheck (mb);
			mono_mb_emit_byte (mb, mono_type_to_ldind (sig->params [i]));
			break;
		case MONO_TYPE_GENERICINST:
			if (!mono_type_generic_inst_is_valuetype (sig->params [i])) {
				mono_mb_emit_no_nullcheck (mb);
				mono_mb_emit_byte (mb, mono_type_to_ldind (sig->params [i]));
				break;
			}

			t = m_class_get_byval_arg (t->data.generic_class->container_class);
			type = t->type;
			goto handle_enum;
		case MONO_TYPE_VALUETYPE:
			if (type == MONO_TYPE_VALUETYPE && m_class_is_enumtype (t->data.klass)) {
				type = mono_class_enum_basetype_internal (t->data.klass)->type;
				goto handle_enum;
			}
			mono_mb_emit_no_nullcheck (mb);
			mono_mb_emit_byte (mb, CEE_LDIND_I);
			if (mono_class_is_nullable (mono_class_from_mono_type_internal (sig->params [i]))) {
				/* Need to convert a boxed vtype to an mp to a Nullable struct */
				mono_mb_emit_op (mb, CEE_UNBOX, mono_class_from_mono_type_internal (sig->params [i]));
				mono_mb_emit_op (mb, CEE_LDOBJ, mono_class_from_mono_type_internal (sig->params [i]));
			} else {
				mono_mb_emit_op (mb, CEE_LDOBJ, mono_class_from_mono_type_internal (sig->params [i]));
			}
			break;
		default:
			g_assert_not_reached ();
		}
	}

	if (virtual_) {
		mono_mb_emit_op (mb, CEE_CALLVIRT, method);
	} else if (need_direct_wrapper) {
		mono_mb_emit_op (mb, CEE_CALL, method);
	} else {
		mono_mb_emit_ldarg (mb, 3);
		mono_mb_emit_calli (mb, callsig);
	}

	if (m_type_is_byref (sig->ret)) {
		/* perform indirect load and return by value */
		int pos;
		mono_mb_emit_byte (mb, CEE_DUP);
		pos = mono_mb_emit_branch (mb, CEE_BRTRUE);
		mono_mb_emit_exception_full (mb, "Mono", "NullByRefReturnException", NULL);
		mono_mb_patch_branch (mb, pos);

		int ldind_op;
		MonoType* ret_byval = m_class_get_byval_arg (mono_class_from_mono_type_internal (sig->ret));
		g_assert (!m_type_is_byref (ret_byval));
		// TODO: Handle null references
		ldind_op = mono_type_to_ldind (ret_byval);
		/* taken from similar code in mini-generic-sharing.c
		 * we need to use mono_mb_emit_op to add method data when loading
		 * a structure since method-to-ir needs this data for wrapper methods */
		if (ldind_op == CEE_LDOBJ)
			mono_mb_emit_op (mb, CEE_LDOBJ, mono_class_from_mono_type_internal (ret_byval));
		else
			mono_mb_emit_byte (mb, ldind_op);
	}

	switch (sig->ret->type) {
	case MONO_TYPE_VOID:
		if (!string_ctor)
			void_ret = TRUE;
		break;
	case MONO_TYPE_BOOLEAN:
	case MONO_TYPE_CHAR:
	case MONO_TYPE_I1:
	case MONO_TYPE_U1:
	case MONO_TYPE_I2:
	case MONO_TYPE_U2:
	case MONO_TYPE_I4:
	case MONO_TYPE_U4:
	case MONO_TYPE_I:
	case MONO_TYPE_U:
	case MONO_TYPE_R4:
	case MONO_TYPE_R8:
	case MONO_TYPE_I8:
	case MONO_TYPE_U8:
	case MONO_TYPE_VALUETYPE:
	case MONO_TYPE_TYPEDBYREF:
	case MONO_TYPE_GENERICINST:
		/* box value types */
		mono_mb_emit_op (mb, CEE_BOX, mono_class_from_mono_type_internal (sig->ret));
		break;
	case MONO_TYPE_STRING:
	case MONO_TYPE_CLASS:
	case MONO_TYPE_ARRAY:
	case MONO_TYPE_SZARRAY:
	case MONO_TYPE_OBJECT:
		/* nothing to do */
		break;
	case MONO_TYPE_PTR:
		/* The result is an IntPtr */
		mono_mb_emit_op (mb, CEE_BOX, mono_defaults.int_class);
		break;
	default:
		g_assert_not_reached ();
	}

	if (!void_ret)
		mono_mb_emit_stloc (mb, loc_res);

	/* Convert back nullable-byref arguments */
	for (i = 0; i < sig->param_count; i++) {
		MonoType *t = sig->params [i];

		/*
		 * Box the result and put it back into the array, the caller will have
		 * to obtain it from there.
		 */
		if (m_type_is_byref (t) && t->type == MONO_TYPE_GENERICINST && mono_class_is_nullable (mono_class_from_mono_type_internal (t))) {
			mono_mb_emit_ldarg (mb, 1);
			mono_mb_emit_icon (mb, TARGET_SIZEOF_VOID_P * i);
			mono_mb_emit_byte (mb, CEE_ADD);

			mono_mb_emit_ldloc (mb, tmp_nullable_locals [i]);
			mono_mb_emit_op (mb, CEE_BOX, mono_class_from_mono_type_internal (t));

			mono_mb_emit_byte (mb, CEE_STIND_REF);
		}
	}

	g_free (tmp_nullable_locals);
}

static void
emit_runtime_invoke_body_ilgen (MonoMethodBuilder *mb, const char **param_names, MonoImage *image, MonoMethod *method,
						  MonoMethodSignature *sig, MonoMethodSignature *callsig,
						  gboolean virtual_, gboolean need_direct_wrapper)
{
	gint32 labels [16];
	MonoExceptionClause *clause;
	int loc_res, loc_exc;

	mono_mb_set_param_names (mb, param_names);

	/* The wrapper looks like this:
	 *
	 * <interrupt check>
	 * if (exc) {
	 *	 try {
	 *	   return <call>
	 *	 } catch (Exception e) {
	 *     *exc = e;
	 *   }
	 * } else {
	 *     return <call>
	 * }
	 */

	MonoType *object_type = mono_get_object_type ();
	/* allocate local 0 (object) tmp */
	loc_res = mono_mb_add_local (mb, object_type);
	/* allocate local 1 (object) exc */
	loc_exc = mono_mb_add_local (mb, object_type);

	/* *exc is assumed to be initialized to NULL by the caller */

	mono_mb_emit_byte (mb, CEE_LDARG_2);
	labels [0] = mono_mb_emit_branch (mb, CEE_BRFALSE);

	/*
	 * if (exc) case
	 */
	labels [1] = mono_mb_get_label (mb);
	emit_thread_force_interrupt_checkpoint (mb);
	emit_invoke_call (mb, method, sig, callsig, loc_res, virtual_, need_direct_wrapper);

	labels [2] = mono_mb_emit_branch (mb, CEE_LEAVE);

	/* Add a try clause around the call */
	clause = (MonoExceptionClause *)mono_image_alloc0 (image, sizeof (MonoExceptionClause));
	clause->flags = MONO_EXCEPTION_CLAUSE_NONE;
	clause->data.catch_class = mono_defaults.exception_class;
	clause->try_offset = labels [1];
	clause->try_len = mono_mb_get_label (mb) - labels [1];

	clause->handler_offset = mono_mb_get_label (mb);

	/* handler code */
	mono_mb_emit_stloc (mb, loc_exc);
	mono_mb_emit_byte (mb, CEE_LDARG_2);
	mono_mb_emit_ldloc (mb, loc_exc);
	mono_mb_emit_byte (mb, CEE_STIND_REF);

	mono_mb_emit_branch (mb, CEE_LEAVE);

	clause->handler_len = mono_mb_get_pos (mb) - clause->handler_offset;

	mono_mb_set_clauses (mb, 1, clause);

	mono_mb_patch_branch (mb, labels [2]);
	mono_mb_emit_ldloc (mb, loc_res);
	mono_mb_emit_byte (mb, CEE_RET);

	/*
	 * if (!exc) case
	 */
	mono_mb_patch_branch (mb, labels [0]);
	emit_thread_force_interrupt_checkpoint (mb);
	emit_invoke_call (mb, method, sig, callsig, loc_res, virtual_, need_direct_wrapper);

	mono_mb_emit_ldloc (mb, 0);
	mono_mb_emit_byte (mb, CEE_RET);
}

static void
emit_runtime_invoke_dynamic_ilgen (MonoMethodBuilder *mb)
{
	int pos;
	MonoExceptionClause *clause;

	MonoType *object_type = mono_get_object_type ();
	/* allocate local 0 (object) tmp */
	mono_mb_add_local (mb, object_type);
	/* allocate local 1 (object) exc */
	mono_mb_add_local (mb, object_type);

	/* cond set *exc to null */
	mono_mb_emit_byte (mb, CEE_LDARG_1);
	mono_mb_emit_byte (mb, CEE_BRFALSE_S);
	mono_mb_emit_byte (mb, 3);
	mono_mb_emit_byte (mb, CEE_LDARG_1);
	mono_mb_emit_byte (mb, CEE_LDNULL);
	mono_mb_emit_byte (mb, CEE_STIND_REF);

	emit_thread_force_interrupt_checkpoint (mb);

	mono_mb_emit_byte (mb, CEE_LDARG_0);
	mono_mb_emit_byte (mb, CEE_LDARG_2);
	mono_mb_emit_byte (mb, MONO_CUSTOM_PREFIX);
	mono_mb_emit_byte (mb, CEE_MONO_DYN_CALL);

	pos = mono_mb_emit_branch (mb, CEE_LEAVE);

	clause = (MonoExceptionClause *)mono_image_alloc0 (mono_defaults.corlib, sizeof (MonoExceptionClause));
	clause->flags = MONO_EXCEPTION_CLAUSE_FILTER;
	clause->try_len = mono_mb_get_label (mb);

	/* filter code */
	clause->data.filter_offset = mono_mb_get_label (mb);

	mono_mb_emit_byte (mb, CEE_POP);
	mono_mb_emit_byte (mb, CEE_LDARG_1);
	mono_mb_emit_byte (mb, CEE_LDC_I4_0);
	mono_mb_emit_byte (mb, CEE_PREFIX1);
	mono_mb_emit_byte (mb, CEE_CGT_UN);
	mono_mb_emit_byte (mb, CEE_PREFIX1);
	mono_mb_emit_byte (mb, CEE_ENDFILTER);

	clause->handler_offset = mono_mb_get_label (mb);

	/* handler code */
	/* store exception */
	mono_mb_emit_stloc (mb, 1);

	mono_mb_emit_byte (mb, CEE_LDARG_1);
	mono_mb_emit_ldloc (mb, 1);
	mono_mb_emit_byte (mb, CEE_STIND_REF);

	mono_mb_emit_byte (mb, CEE_LDNULL);
	mono_mb_emit_stloc (mb, 0);

	mono_mb_emit_branch (mb, CEE_LEAVE);

	clause->handler_len = mono_mb_get_pos (mb) - clause->handler_offset;

	mono_mb_set_clauses (mb, 1, clause);

	/* return result */
	mono_mb_patch_branch (mb, pos);
	//mono_mb_emit_ldloc (mb, 0);
	mono_mb_emit_byte (mb, CEE_RET);
}

static void
mono_mb_emit_auto_layout_exception (MonoMethodBuilder *mb, MonoClass *klass)
{
	char *msg = g_strdup_printf ("The type `%s.%s' layout needs to be Sequential or Explicit", m_class_get_name_space (klass), m_class_get_name (klass));
	mono_mb_emit_exception_marshal_directive (mb, msg);
}

typedef struct EmitGCSafeTransitionBuilder {
	MonoMethodBuilder *mb;
	gboolean func_param;
	int coop_gc_var;
#ifndef DISABLE_COM
	int coop_cominterop_fnptr;
#endif
} GCSafeTransitionBuilder;

static gboolean
gc_safe_transition_builder_init (GCSafeTransitionBuilder *builder, MonoMethodBuilder *mb, gboolean func_param)
{
	builder->mb = mb;
	builder->func_param = func_param;
	builder->coop_gc_var = -1;
#ifndef DISABLE_COM
	builder->coop_cominterop_fnptr = -1;
#endif
#if defined (TARGET_WASM)
	return FALSE;
#else
	return TRUE;
#endif
}

/**
 * adds locals for the gc safe transition to the method builder.
 */
static void
gc_safe_transition_builder_add_locals (GCSafeTransitionBuilder *builder)
{
	MonoType *int_type = mono_get_int_type();
	/* local 4, the local to be used when calling the suspend funcs */
	builder->coop_gc_var = mono_mb_add_local (builder->mb, int_type);
#ifndef DISABLE_COM
	if (!builder->func_param && MONO_CLASS_IS_IMPORT (builder->mb->method->klass)) {
		builder->coop_cominterop_fnptr = mono_mb_add_local (builder->mb, int_type);
	}
#endif
}

/**
 * emits
 *     cookie = mono_threads_enter_gc_safe_region_unbalanced (ref dummy);
 *
 */
static void
gc_safe_transition_builder_emit_enter (GCSafeTransitionBuilder *builder, MonoMethod *method, gboolean aot)
{

	// Perform an extra, early lookup of the function address, so any exceptions
	// potentially resulting from the lookup occur before entering blocking mode.
	if (!builder->func_param && !MONO_CLASS_IS_IMPORT (builder->mb->method->klass) && aot) {
		mono_mb_emit_byte (builder->mb, MONO_CUSTOM_PREFIX);
		mono_mb_emit_op (builder->mb, CEE_MONO_ICALL_ADDR, method);
		mono_mb_emit_byte (builder->mb, CEE_POP); // Result not needed yet
	}

#ifndef DISABLE_COM
	if (!builder->func_param && MONO_CLASS_IS_IMPORT (builder->mb->method->klass)) {
		mono_mb_emit_cominterop_get_function_pointer (builder->mb, method);
		mono_mb_emit_stloc (builder->mb, builder->coop_cominterop_fnptr);
	}
#endif

	mono_mb_emit_byte (builder->mb, MONO_CUSTOM_PREFIX);
	mono_mb_emit_byte (builder->mb, CEE_MONO_GET_SP);
	mono_mb_emit_icall (builder->mb, mono_threads_enter_gc_safe_region_unbalanced);
	mono_mb_emit_stloc (builder->mb, builder->coop_gc_var);
}

/**
 * emits
 *     mono_threads_exit_gc_safe_region_unbalanced (cookie, ref dummy);
 *
 */
static void
gc_safe_transition_builder_emit_exit (GCSafeTransitionBuilder *builder)
{
	mono_mb_emit_ldloc (builder->mb, builder->coop_gc_var);
	mono_mb_emit_byte (builder->mb, MONO_CUSTOM_PREFIX);
	mono_mb_emit_byte (builder->mb, CEE_MONO_GET_SP);
	mono_mb_emit_icall (builder->mb, mono_threads_exit_gc_safe_region_unbalanced);
}

static void
gc_safe_transition_builder_cleanup (GCSafeTransitionBuilder *builder)
{
	builder->mb = NULL;
	builder->coop_gc_var = -1;
#ifndef DISABLE_COM
	builder->coop_cominterop_fnptr = -1;
#endif
}

static gboolean
emit_native_wrapper_validate_signature (MonoMethodBuilder *mb, MonoMethodSignature* sig, MonoMarshalSpec** mspecs)
{
	if (mspecs) {
		for (int i = 0; i < sig->param_count; i ++) {
			if (mspecs [i + 1] && mspecs [i + 1]->native == MONO_NATIVE_CUSTOM) {
				if (!mspecs [i + 1]->data.custom_data.custom_name || strlen (mspecs [i + 1]->data.custom_data.custom_name) == 0) {
					mono_mb_emit_exception_full (mb, "System", "TypeLoadException", g_strdup ("Missing ICustomMarshaler type"));
					return FALSE;
				}

				switch (sig->params[i]->type) {
				case MONO_TYPE_CLASS:
				case MONO_TYPE_OBJECT:
				case MONO_TYPE_STRING:
				case MONO_TYPE_ARRAY:
				case MONO_TYPE_SZARRAY:
				case MONO_TYPE_VALUETYPE:
					break;

				default:
					mono_mb_emit_exception_full (mb, "System.Runtime.InteropServices", "MarshalDirectiveException", g_strdup_printf ("custom marshalling of type %x is currently not supported", sig->params[i]->type));
					return FALSE;
				}
			}
			else if (sig->params[i]->type == MONO_TYPE_VALUETYPE) {
				MonoMarshalType *marshal_type = mono_marshal_load_type_info (mono_class_from_mono_type_internal (sig->params [i]));
				for (int field_idx = 0; field_idx < marshal_type->num_fields; ++field_idx) {
					if (marshal_type->fields [field_idx].mspec && marshal_type->fields [field_idx].mspec->native == MONO_NATIVE_CUSTOM) {
						mono_mb_emit_exception_full (mb, "System", "TypeLoadException", g_strdup ("Value type includes custom marshaled fields"));
						return FALSE;
					}
				}
			}
		}
	}

	return TRUE;
}

/**
 * emit_native_wrapper_ilgen:
 * \param image the image to use for looking up custom marshallers
 * \param sig The signature of the native function
 * \param piinfo Marshalling information
 * \param mspecs Marshalling information
 * \param aot whenever the created method will be compiled by the AOT compiler
 * \param method if non-NULL, the pinvoke method to call
 * \param check_exceptions Whenever to check for pending exceptions after the native call
 * \param func_param the function to call is passed as a boxed IntPtr as the first parameter
 * \param func_param_unboxed combined with \p func_param, expect the function to call as an unboxed IntPtr as the first parameter
 * \param skip_gc_trans Whenever to skip GC transitions
 *
 * generates IL code for the pinvoke wrapper, the generated code calls \p func .
 */
static void
emit_native_wrapper_ilgen (MonoImage *image, MonoMethodBuilder *mb, MonoMethodSignature *sig, MonoMethodPInvoke *piinfo, MonoMarshalSpec **mspecs, gpointer func, MonoNativeWrapperFlags flags)
{
	gboolean aot = (flags & EMIT_NATIVE_WRAPPER_AOT) != 0;
	gboolean check_exceptions = (flags & EMIT_NATIVE_WRAPPER_CHECK_EXCEPTIONS) != 0;
	gboolean func_param = (flags & EMIT_NATIVE_WRAPPER_FUNC_PARAM) != 0;
	gboolean func_param_unboxed = (flags & EMIT_NATIVE_WRAPPER_FUNC_PARAM_UNBOXED) != 0;
	gboolean skip_gc_trans = (flags & EMIT_NATIVE_WRAPPER_SKIP_GC_TRANS) != 0;
	gboolean runtime_marshalling_enabled = (flags & EMIT_NATIVE_WRAPPER_RUNTIME_MARSHALLING_ENABLED) != 0;
	EmitMarshalContext m;
	MonoMethodSignature *csig;
	MonoClass *klass;
	int i, argnum, *tmp_locals;
	int type, param_shift = 0;
	int func_addr_local = -1;
	gboolean need_gc_safe = FALSE;
	GCSafeTransitionBuilder gc_safe_transition_builder;

	memset (&m, 0, sizeof (m));
	m.runtime_marshalling_enabled = runtime_marshalling_enabled;
	m.mb = mb;
	m.sig = sig;
	m.piinfo = piinfo;

	if (!emit_native_wrapper_validate_signature (mb, sig, mspecs))
		return;

	if (!skip_gc_trans)
		need_gc_safe = gc_safe_transition_builder_init (&gc_safe_transition_builder, mb, func_param);

	/* we copy the signature, so that we can set pinvoke to 0 */
	if (func_param) {
		/* The function address is passed as the first argument */
		g_assert (!sig->hasthis);
		param_shift += 1;
	}
	csig = mono_metadata_signature_dup_full (get_method_image (mb->method), sig);
	csig->pinvoke = 1;
	if (!runtime_marshalling_enabled)
		csig->marshalling_disabled = 1;
	m.csig = csig;
	m.image = image;

	if (sig->hasthis)
		param_shift += 1;

	MonoType *int_type = mono_get_int_type ();
	MonoType *boolean_type = m_class_get_byval_arg (mono_defaults.boolean_class);
	/* we allocate local for use with emit_struct_conv() */
	/* allocate local 0 (pointer) src_ptr */
	mono_mb_add_local (mb, int_type);
	/* allocate local 1 (pointer) dst_ptr */
	mono_mb_add_local (mb, int_type);
	/* allocate local 2 (boolean) delete_old */
	mono_mb_add_local (mb, boolean_type);

	/* delete_old = FALSE */
	mono_mb_emit_icon (mb, 0);
	mono_mb_emit_stloc (mb, 2);

	if (!MONO_TYPE_IS_VOID (sig->ret)) {
		/* allocate local 3 to store the return value */
		mono_mb_add_local (mb, sig->ret);
	}

	if (need_gc_safe)
		gc_safe_transition_builder_add_locals (&gc_safe_transition_builder);

	if (!func && !aot && !func_param && !MONO_CLASS_IS_IMPORT (mb->method->klass)) {
		/*
		 * On netcore, its possible to register pinvoke resolvers at runtime, so
		 * a pinvoke lookup can fail, and then succeed later. So if the
		 * original lookup failed, do a lookup every time until it
		 * succeeds.
		 * This adds some overhead, but only when the pinvoke lookup
		 * was not initially successful.
		 * FIXME: AOT case
		 */
		func_addr_local = mono_mb_add_local (mb, int_type);

		int cache_local = mono_mb_add_local (mb, int_type);
		mono_mb_emit_byte (mb, MONO_CUSTOM_PREFIX);
		mono_mb_emit_op (mb, CEE_MONO_PINVOKE_ADDR_CACHE, &piinfo->method);
		mono_mb_emit_stloc (mb, cache_local);

		mono_mb_emit_ldloc (mb, cache_local);
		mono_mb_emit_byte (mb, CEE_LDIND_I);
		int pos = mono_mb_emit_branch (mb, CEE_BRTRUE);

		mono_mb_emit_ldloc (mb, cache_local);
		mono_mb_emit_byte (mb, MONO_CUSTOM_PREFIX);
		mono_mb_emit_op (mb, CEE_MONO_METHODCONST, &piinfo->method);
		mono_mb_emit_icall (mb, mono_marshal_lookup_pinvoke);
		mono_mb_emit_byte (mb, CEE_STIND_I);

		mono_mb_patch_branch (mb, pos);
		mono_mb_emit_ldloc (mb, cache_local);
		mono_mb_emit_byte (mb, CEE_LDIND_I);
		mono_mb_emit_stloc (mb, func_addr_local);
	}

	/*
	 * cookie = mono_threads_enter_gc_safe_region_unbalanced (ref dummy);
	 *
	 * ret = method (...);
	 *
	 * mono_threads_exit_gc_safe_region_unbalanced (cookie, ref dummy);
	 *
	 * <interrupt check>
	 *
	 * return ret;
	 */

	if (MONO_TYPE_ISSTRUCT (sig->ret))
		m.vtaddr_var = mono_mb_add_local (mb, int_type);

	if (mspecs [0] && mspecs [0]->native == MONO_NATIVE_CUSTOM) {
		/* Return type custom marshaling */
		/*
		 * Since we can't determine the return type of the unmanaged function,
		 * we assume it returns a pointer, and pass that pointer to
		 * MarshalNativeToManaged.
		 */
		csig->ret = int_type;
	}

	// Check if SetLastError usage is valid early so we don't try to throw an exception after transitioning GC modes.
	if (piinfo && (piinfo->piflags & PINVOKE_ATTRIBUTE_SUPPORTS_LAST_ERROR) && !m.runtime_marshalling_enabled)
		mono_mb_emit_exception_marshal_directive(mb, g_strdup("Setting SetLastError=true is not supported when runtime marshalling is disabled."));

	/* we first do all conversions */
	tmp_locals = g_newa (int, sig->param_count);
	m.orig_conv_args = g_newa (int, sig->param_count + 1);

	for (i = 0; i < sig->param_count; i ++) {
		tmp_locals [i] = mono_emit_marshal (&m, i + param_shift, sig->params [i], mspecs [i + 1], 0, &csig->params [i], MARSHAL_ACTION_CONV_IN);
	}

	// In coop mode need to register blocking state during native call
	if (need_gc_safe)
		gc_safe_transition_builder_emit_enter (&gc_safe_transition_builder, &piinfo->method, aot);

	/* push all arguments */

	if (sig->hasthis)
		mono_mb_emit_byte (mb, CEE_LDARG_0);

	for (i = 0; i < sig->param_count; i++) {
		mono_emit_marshal (&m, i + param_shift, sig->params [i], mspecs [i + 1], tmp_locals [i], NULL, MARSHAL_ACTION_PUSH);
	}

	/* call the native method */
	if (func_param) {
		mono_mb_emit_byte (mb, CEE_LDARG_0);
		if (!func_param_unboxed) {
			mono_mb_emit_op (mb, CEE_UNBOX, mono_defaults.int_class);
			mono_mb_emit_byte (mb, CEE_LDIND_I);
		}
		if (piinfo && (piinfo->piflags & PINVOKE_ATTRIBUTE_SUPPORTS_LAST_ERROR) != 0) {
			mono_mb_emit_byte (mb, MONO_CUSTOM_PREFIX);
			mono_mb_emit_byte (mb, CEE_MONO_SAVE_LAST_ERROR);
		}
		mono_mb_emit_calli (mb, csig);
	} else if (MONO_CLASS_IS_IMPORT (mb->method->klass)) {
#ifndef DISABLE_COM
		mono_mb_emit_ldloc (mb, gc_safe_transition_builder.coop_cominterop_fnptr);
		if (piinfo->piflags & PINVOKE_ATTRIBUTE_SUPPORTS_LAST_ERROR) {
			mono_mb_emit_byte (mb, MONO_CUSTOM_PREFIX);
			mono_mb_emit_byte (mb, CEE_MONO_SAVE_LAST_ERROR);
		}
		mono_mb_emit_cominterop_call_function_pointer (mb, csig);
#else
		g_assert_not_reached ();
#endif
	} else {
		if (func_addr_local != -1) {
			mono_mb_emit_ldloc (mb, func_addr_local);
		} else {
			if (aot) {
				/* Reuse the ICALL_ADDR opcode for pinvokes too */
				mono_mb_emit_byte (mb, MONO_CUSTOM_PREFIX);
				mono_mb_emit_op (mb, CEE_MONO_ICALL_ADDR, &piinfo->method);
			}
		}
		if (piinfo->piflags & PINVOKE_ATTRIBUTE_SUPPORTS_LAST_ERROR) {
			mono_mb_emit_byte (mb, MONO_CUSTOM_PREFIX);
			mono_mb_emit_byte (mb, CEE_MONO_SAVE_LAST_ERROR);
		}
		if (func_addr_local != -1 || aot)
			mono_mb_emit_calli (mb, csig);
		else
			mono_mb_emit_native_call (mb, csig, func);
	}

	if (MONO_TYPE_ISSTRUCT (sig->ret)) {
		MonoClass *klass = mono_class_from_mono_type_internal (sig->ret);
		mono_class_init_internal (klass);
		if (!(mono_class_is_explicit_layout (klass) || m_class_is_blittable (klass))) {
			/* This is used by emit_marshal_vtype (), but it needs to go right before the call */
			mono_mb_emit_byte (mb, MONO_CUSTOM_PREFIX);
			mono_mb_emit_byte (mb, CEE_MONO_VTADDR);
			mono_mb_emit_stloc (mb, m.vtaddr_var);
		}
	}

	/* Unblock before converting the result, since that can involve calls into the runtime */
	if (need_gc_safe)
		gc_safe_transition_builder_emit_exit (&gc_safe_transition_builder);

	gc_safe_transition_builder_cleanup (&gc_safe_transition_builder);

	/* convert the result */
	if (!m_type_is_byref (sig->ret)) {
		MonoMarshalSpec *spec = mspecs [0];
		type = sig->ret->type;

		if (spec && spec->native == MONO_NATIVE_CUSTOM) {
			mono_emit_marshal (&m, 0, sig->ret, spec, 0, NULL, MARSHAL_ACTION_CONV_RESULT);
		} else {
		handle_enum:
			switch (type) {
			case MONO_TYPE_VOID:
				break;
			case MONO_TYPE_VALUETYPE:
				klass = sig->ret->data.klass;
				if (m_class_is_enumtype (klass)) {
					type = mono_class_enum_basetype_internal (sig->ret->data.klass)->type;
					goto handle_enum;
				}
				mono_emit_marshal (&m, 0, sig->ret, spec, 0, NULL, MARSHAL_ACTION_CONV_RESULT);
				break;
			case MONO_TYPE_I1:
			case MONO_TYPE_U1:
			case MONO_TYPE_I2:
			case MONO_TYPE_U2:
			case MONO_TYPE_I4:
			case MONO_TYPE_U4:
			case MONO_TYPE_I:
			case MONO_TYPE_U:
			case MONO_TYPE_R4:
			case MONO_TYPE_R8:
			case MONO_TYPE_I8:
			case MONO_TYPE_U8:
			case MONO_TYPE_FNPTR:
			case MONO_TYPE_STRING:
			case MONO_TYPE_CLASS:
			case MONO_TYPE_OBJECT:
			case MONO_TYPE_BOOLEAN:
			case MONO_TYPE_ARRAY:
			case MONO_TYPE_SZARRAY:
			case MONO_TYPE_CHAR:
			case MONO_TYPE_PTR:
			case MONO_TYPE_GENERICINST:
				mono_emit_marshal (&m, 0, sig->ret, spec, 0, NULL, MARSHAL_ACTION_CONV_RESULT);
				break;
			case MONO_TYPE_TYPEDBYREF:
			default:
				g_warning ("return type 0x%02x unknown", sig->ret->type);
				g_assert_not_reached ();
			}
		}
	} else {
		mono_mb_emit_stloc (mb, 3);
	}

	/*
	 * Need to call this after converting the result since MONO_VTADDR needs
	 * to be adjacent to the call instruction.
	 */
	if (check_exceptions)
		emit_thread_interrupt_checkpoint (mb);

	/* we need to convert byref arguments back and free string arrays */
	for (i = 0; i < sig->param_count; i++) {
		MonoType *t = sig->params [i];
		MonoMarshalSpec *spec = mspecs [i + 1];

		argnum = i + param_shift;

		if (spec && ((spec->native == MONO_NATIVE_CUSTOM) || (spec->native == MONO_NATIVE_ASANY))) {
			mono_emit_marshal (&m, argnum, t, spec, tmp_locals [i], NULL, MARSHAL_ACTION_CONV_OUT);
			continue;
		}

		switch (t->type) {
		case MONO_TYPE_STRING:
		case MONO_TYPE_VALUETYPE:
		case MONO_TYPE_CLASS:
		case MONO_TYPE_OBJECT:
		case MONO_TYPE_SZARRAY:
		case MONO_TYPE_BOOLEAN:
			mono_emit_marshal (&m, argnum, t, spec, tmp_locals [i], NULL, MARSHAL_ACTION_CONV_OUT);
			break;
		default:
			break;
		}
	}

	if (!MONO_TYPE_IS_VOID(sig->ret))
		mono_mb_emit_ldloc (mb, 3);

	mono_mb_emit_byte (mb, CEE_RET);
}

/*
 * The code directly following this is the cache hit, value positive branch
 *
 * This function takes a new method builder with 0 locals and adds two locals
 * to create multiple out-branches and the fall through state of having the object
 * on the stack after a cache miss
 */
static void
generate_check_cache (int obj_arg_position, int class_arg_position, int cache_arg_position, // In-parameters
											int *null_obj, int *cache_hit_neg, int *cache_hit_pos, // Out-parameters
											MonoMethodBuilder *mb)
{
	int cache_miss_pos;

	MonoType *int_type = mono_get_int_type ();
	/* allocate local 0 (pointer) obj_vtable */
	mono_mb_add_local (mb, int_type);
	/* allocate local 1 (pointer) cached_vtable */
	mono_mb_add_local (mb, int_type);

	/*if (!obj)*/
	mono_mb_emit_ldarg (mb, obj_arg_position);
	*null_obj = mono_mb_emit_branch (mb, CEE_BRFALSE);

	/*obj_vtable = obj->vtable;*/
	mono_mb_emit_ldarg (mb, obj_arg_position);
	mono_mb_emit_ldflda (mb, MONO_STRUCT_OFFSET (MonoObject, vtable));
	mono_mb_emit_byte (mb, CEE_LDIND_I);
	mono_mb_emit_stloc (mb, 0);

	/* cached_vtable = *cache*/
	mono_mb_emit_ldarg (mb, cache_arg_position);
	mono_mb_emit_byte (mb, CEE_LDIND_I);
	mono_mb_emit_stloc (mb, 1);

	mono_mb_emit_ldloc (mb, 1);
	mono_mb_emit_byte (mb, CEE_LDC_I4);
	mono_mb_emit_i4 (mb, ~0x1);
	mono_mb_emit_byte (mb, CEE_CONV_I);
	mono_mb_emit_byte (mb, CEE_AND);
	mono_mb_emit_ldloc (mb, 0);
	/*if ((cached_vtable & ~0x1)== obj_vtable)*/
	cache_miss_pos = mono_mb_emit_branch (mb, CEE_BNE_UN);

	/*return (cached_vtable & 0x1) ? NULL : obj;*/
	mono_mb_emit_ldloc (mb, 1);
	mono_mb_emit_byte(mb, CEE_LDC_I4_1);
	mono_mb_emit_byte (mb, CEE_CONV_U);
	mono_mb_emit_byte (mb, CEE_AND);
	*cache_hit_neg = mono_mb_emit_branch (mb, CEE_BRTRUE);
	*cache_hit_pos = mono_mb_emit_branch (mb, CEE_BR);

	// slow path
	mono_mb_patch_branch (mb, cache_miss_pos);

	// if isinst
	mono_mb_emit_ldarg (mb, obj_arg_position);
	mono_mb_emit_ldarg (mb, class_arg_position);
	mono_mb_emit_ldarg (mb, cache_arg_position);
	mono_mb_emit_icall (mb, mono_marshal_isinst_with_cache);
}

static void
emit_castclass_ilgen (MonoMethodBuilder *mb)
{
	int return_null_pos, positive_cache_hit_pos, negative_cache_hit_pos, invalid_cast_pos;
	const int obj_arg_position = TYPECHECK_OBJECT_ARG_POS;
	const int class_arg_position = TYPECHECK_CLASS_ARG_POS;
	const int cache_arg_position = TYPECHECK_CACHE_ARG_POS;

	generate_check_cache (obj_arg_position, class_arg_position, cache_arg_position,
												&return_null_pos, &negative_cache_hit_pos, &positive_cache_hit_pos, mb);
	invalid_cast_pos = mono_mb_emit_branch (mb, CEE_BRFALSE);

	/*return obj;*/
	mono_mb_patch_branch (mb, positive_cache_hit_pos);
	mono_mb_emit_ldarg (mb, obj_arg_position);
	mono_mb_emit_byte (mb, CEE_RET);

	/*fails*/
	mono_mb_patch_branch (mb, negative_cache_hit_pos);
	mono_mb_patch_branch (mb, invalid_cast_pos);
	mono_mb_emit_exception (mb, "InvalidCastException", NULL);

	/*return null*/
	mono_mb_patch_branch (mb, return_null_pos);
	mono_mb_emit_byte (mb, CEE_LDNULL);
	mono_mb_emit_byte (mb, CEE_RET);
}

static void
emit_isinst_ilgen (MonoMethodBuilder *mb)
{
	int return_null_pos, positive_cache_hit_pos, negative_cache_hit_pos;
	const int obj_arg_position = TYPECHECK_OBJECT_ARG_POS;
	const int class_arg_position = TYPECHECK_CLASS_ARG_POS;
	const int cache_arg_position = TYPECHECK_CACHE_ARG_POS;

	generate_check_cache (obj_arg_position, class_arg_position, cache_arg_position,
		&return_null_pos, &negative_cache_hit_pos, &positive_cache_hit_pos, mb);
	// Return the object gotten via the slow path.
	mono_mb_emit_byte (mb, CEE_RET);

	// return NULL;
	mono_mb_patch_branch (mb, negative_cache_hit_pos);
	mono_mb_patch_branch (mb, return_null_pos);
	mono_mb_emit_byte (mb, CEE_LDNULL);
	mono_mb_emit_byte (mb, CEE_RET);

	// return obj
	mono_mb_patch_branch (mb, positive_cache_hit_pos);
	mono_mb_emit_ldarg (mb, obj_arg_position);
	mono_mb_emit_byte (mb, CEE_RET);
}

static void
load_array_element_address (MonoMethodBuilder *mb)
{
	mono_mb_emit_ldarg (mb, 0);
	mono_mb_emit_ldarg (mb, 1);
	mono_mb_emit_op (mb, CEE_LDELEMA, mono_defaults.object_class);
}

static void
load_array_class (MonoMethodBuilder *mb, int aklass)
{
	mono_mb_emit_ldarg (mb, 0);
	mono_mb_emit_ldflda (mb, MONO_STRUCT_OFFSET (MonoObject, vtable));
	mono_mb_emit_byte (mb, CEE_LDIND_I);
	mono_mb_emit_ldflda (mb, MONO_STRUCT_OFFSET (MonoVTable, klass));
	mono_mb_emit_byte (mb, CEE_LDIND_I);
	mono_mb_emit_ldflda (mb, m_class_offsetof_element_class ());
	mono_mb_emit_byte (mb, CEE_LDIND_I);
	mono_mb_emit_stloc (mb, aklass);
}

static void
load_value_class (MonoMethodBuilder *mb, int vklass)
{
	mono_mb_emit_ldarg (mb, 2);
	mono_mb_emit_ldflda (mb, MONO_STRUCT_OFFSET (MonoObject, vtable));
	mono_mb_emit_byte (mb, CEE_LDIND_I);
	mono_mb_emit_ldflda (mb, MONO_STRUCT_OFFSET (MonoVTable, klass));
	mono_mb_emit_byte (mb, CEE_LDIND_I);
	mono_mb_emit_stloc (mb, vklass);
}

static int
emit_marshal_array_ilgen (EmitMarshalContext *m, int argnum, MonoType *t,
					MonoMarshalSpec *spec,
					int conv_arg, MonoType **conv_arg_type,
					MarshalAction action)
{
	MonoMethodBuilder *mb = m->mb;
	MonoClass *klass = mono_class_from_mono_type_internal (t);
	MonoMarshalNative encoding;

	encoding = mono_marshal_get_string_encoding (m->piinfo, spec);
	MonoType *int_type = mono_get_int_type ();
	MonoType *object_type = mono_get_object_type ();

	MonoClass *eklass = m_class_get_element_class (klass);

	switch (action) {
	case MARSHAL_ACTION_CONV_IN:
		*conv_arg_type = object_type;
		conv_arg = mono_mb_add_local (mb, object_type);

		if (m_class_is_blittable (eklass)) {
			mono_mb_emit_ldarg (mb, argnum);
			if (m_type_is_byref (t))
				mono_mb_emit_byte (mb, CEE_LDIND_I);
			mono_mb_emit_icall_id (mb, conv_to_icall (MONO_MARSHAL_CONV_ARRAY_LPARRAY, NULL));
			mono_mb_emit_stloc (mb, conv_arg);
		} else {
#ifdef DISABLE_NONBLITTABLE
			char *msg = g_strdup ("Non-blittable marshalling conversion is disabled");
			mono_mb_emit_exception_marshal_directive (mb, msg);
#else
			guint32 label1, label2, label3;
			int index_var, src_var, dest_ptr, esize;
			MonoMarshalConv conv;
			gboolean is_string = FALSE;

			dest_ptr = mono_mb_add_local (mb, int_type);

			if (eklass == mono_defaults.string_class) {
				is_string = TRUE;
				conv = mono_marshal_get_string_to_ptr_conv (m->piinfo, spec);
			}
			else if (eklass == mono_class_try_get_stringbuilder_class ()) {
				is_string = TRUE;
				conv = mono_marshal_get_stringbuilder_to_ptr_conv (m->piinfo, spec);
			}
			else
				conv = MONO_MARSHAL_CONV_INVALID;

			if (is_string && conv == MONO_MARSHAL_CONV_INVALID) {
				char *msg = g_strdup_printf ("string/stringbuilder marshalling conversion %d not implemented", encoding);
				mono_mb_emit_exception_marshal_directive (mb, msg);
				break;
			}

			src_var = mono_mb_add_local (mb, object_type);
			mono_mb_emit_ldarg (mb, argnum);
			if (m_type_is_byref (t))
				mono_mb_emit_byte (mb, CEE_LDIND_I);
			mono_mb_emit_stloc (mb, src_var);

			/* Check null */
			mono_mb_emit_ldloc (mb, src_var);
			mono_mb_emit_stloc (mb, conv_arg);
			mono_mb_emit_ldloc (mb, src_var);
			label1 = mono_mb_emit_branch (mb, CEE_BRFALSE);

			if (is_string)
				esize = TARGET_SIZEOF_VOID_P;
			else if (eklass == mono_defaults.char_class) /*can't call mono_marshal_type_size since it causes all sorts of asserts*/
				esize = mono_pinvoke_is_unicode (m->piinfo) ? 2 : 1;
			else
				esize = mono_class_native_size (eklass, NULL);

			/* allocate space for the native struct and store the address */
			mono_mb_emit_icon (mb, esize);
			mono_mb_emit_ldloc (mb, src_var);
			mono_mb_emit_byte (mb, CEE_LDLEN);

			if (eklass == mono_defaults.string_class) {
				/* Make the array bigger for the terminating null */
				mono_mb_emit_byte (mb, CEE_LDC_I4_1);
				mono_mb_emit_byte (mb, CEE_ADD);
			}
			mono_mb_emit_byte (mb, CEE_MUL);
			mono_mb_emit_byte (mb, CEE_PREFIX1);
			mono_mb_emit_byte (mb, CEE_LOCALLOC);
			mono_mb_emit_stloc (mb, conv_arg);

			mono_mb_emit_ldloc (mb, conv_arg);
			mono_mb_emit_stloc (mb, dest_ptr);

			/* Emit marshalling loop */
			index_var = mono_mb_add_local (mb, int_type);
			mono_mb_emit_byte (mb, CEE_LDC_I4_0);
			mono_mb_emit_stloc (mb, index_var);
			label2 = mono_mb_get_label (mb);
			mono_mb_emit_ldloc (mb, index_var);
			mono_mb_emit_ldloc (mb, src_var);
			mono_mb_emit_byte (mb, CEE_LDLEN);
			label3 = mono_mb_emit_branch (mb, CEE_BGE);

			/* Emit marshalling code */

			if (is_string) {
				int stind_op;
				mono_mb_emit_ldloc (mb, dest_ptr);
				mono_mb_emit_ldloc (mb, src_var);
				mono_mb_emit_ldloc (mb, index_var);
				mono_mb_emit_byte (mb, CEE_LDELEM_REF);
				mono_mb_emit_icall_id (mb, conv_to_icall (conv, &stind_op));
				mono_mb_emit_byte (mb, stind_op);
			} else {
				/* set the src_ptr */
				mono_mb_emit_ldloc (mb, src_var);
				mono_mb_emit_ldloc (mb, index_var);
				mono_mb_emit_op (mb, CEE_LDELEMA, eklass);
				mono_mb_emit_stloc (mb, 0);

				/* set dst_ptr */
				mono_mb_emit_ldloc (mb, dest_ptr);
				mono_mb_emit_stloc (mb, 1);

				/* emit valuetype conversion code */
				emit_struct_conv_full (mb, eklass, FALSE, 0, eklass == mono_defaults.char_class ? encoding : (MonoMarshalNative)-1);
			}

			mono_mb_emit_add_to_local (mb, index_var, 1);
			mono_mb_emit_add_to_local (mb, dest_ptr, esize);

			mono_mb_emit_branch_label (mb, CEE_BR, label2);

			mono_mb_patch_branch (mb, label3);

			if (eklass == mono_defaults.string_class) {
				/* Null terminate */
				mono_mb_emit_ldloc (mb, dest_ptr);
				mono_mb_emit_byte (mb, CEE_LDC_I4_0);
				mono_mb_emit_byte (mb, CEE_STIND_I);
			}

			mono_mb_patch_branch (mb, label1);
#endif
		}

		break;

	case MARSHAL_ACTION_CONV_OUT: {
#ifndef DISABLE_NONBLITTABLE
		gboolean need_convert, need_free;
		/* Unicode character arrays are implicitly marshalled as [Out] under MS.NET */
		need_convert = ((eklass == mono_defaults.char_class) && (encoding == MONO_NATIVE_LPWSTR)) || (eklass == mono_class_try_get_stringbuilder_class ()) || (t->attrs & PARAM_ATTRIBUTE_OUT);
		need_free = mono_marshal_need_free (m_class_get_byval_arg (eklass), m->piinfo, spec);

		if ((t->attrs & PARAM_ATTRIBUTE_OUT) && spec && spec->native == MONO_NATIVE_LPARRAY && spec->data.array_data.param_num != -1) {
			int param_num = spec->data.array_data.param_num;
			MonoType *param_type;

			param_type = m->sig->params [param_num];

			if (m_type_is_byref (param_type) && param_type->type != MONO_TYPE_I4) {
				char *msg = g_strdup ("Not implemented.");
				mono_mb_emit_exception_marshal_directive (mb, msg);
				break;
			}

			if (m_type_is_byref (t) ) {
				mono_mb_emit_ldarg (mb, argnum);

				/* Create the managed array */
				mono_mb_emit_ldarg (mb, param_num);
				if (m_type_is_byref (m->sig->params [param_num]))
					// FIXME: Support other types
					mono_mb_emit_byte (mb, CEE_LDIND_I4);
				mono_mb_emit_byte (mb, CEE_CONV_OVF_I);
				mono_mb_emit_op (mb, CEE_NEWARR, eklass);
				/* Store into argument */
				mono_mb_emit_byte (mb, CEE_STIND_REF);
			}
		}

		if (need_convert || need_free) {
			/* FIXME: Optimize blittable case */
			guint32 label1, label2, label3;
			int index_var, src_ptr, loc, esize;

			if ((eklass == mono_class_try_get_stringbuilder_class ()) || (eklass == mono_defaults.string_class))
				esize = TARGET_SIZEOF_VOID_P;
			else if (eklass == mono_defaults.char_class)
				esize = mono_pinvoke_is_unicode (m->piinfo) ? 2 : 1;
			else
				esize = mono_class_native_size (eklass, NULL);
			src_ptr = mono_mb_add_local (mb, int_type);
			loc = mono_mb_add_local (mb, int_type);

			/* Check null */
			mono_mb_emit_ldarg (mb, argnum);
			if (m_type_is_byref (t))
				mono_mb_emit_byte (mb, CEE_LDIND_I);
			label1 = mono_mb_emit_branch (mb, CEE_BRFALSE);

			mono_mb_emit_ldloc (mb, conv_arg);
			mono_mb_emit_stloc (mb, src_ptr);

			/* Emit marshalling loop */
			index_var = mono_mb_add_local (mb, int_type);
			mono_mb_emit_byte (mb, CEE_LDC_I4_0);
			mono_mb_emit_stloc (mb, index_var);
			label2 = mono_mb_get_label (mb);
			mono_mb_emit_ldloc (mb, index_var);
			mono_mb_emit_ldarg (mb, argnum);
			if (m_type_is_byref (t))
				mono_mb_emit_byte (mb, CEE_LDIND_REF);
			mono_mb_emit_byte (mb, CEE_LDLEN);
			label3 = mono_mb_emit_branch (mb, CEE_BGE);

			/* Emit marshalling code */

			if (eklass == mono_class_try_get_stringbuilder_class ()) {
				gboolean need_free2;
				MonoMarshalConv conv = mono_marshal_get_ptr_to_stringbuilder_conv (m->piinfo, spec, &need_free2);

				g_assert (conv != MONO_MARSHAL_CONV_INVALID);

				/* dest */
				mono_mb_emit_ldarg (mb, argnum);
				if (m_type_is_byref (t))
					mono_mb_emit_byte (mb, CEE_LDIND_I);
				mono_mb_emit_ldloc (mb, index_var);
				mono_mb_emit_byte (mb, CEE_LDELEM_REF);

				/* src */
				mono_mb_emit_ldloc (mb, src_ptr);
				mono_mb_emit_byte (mb, CEE_LDIND_I);

				mono_mb_emit_icall_id (mb, conv_to_icall (conv, NULL));

				if (need_free) {
					/* src */
					mono_mb_emit_ldloc (mb, src_ptr);
					mono_mb_emit_byte (mb, CEE_LDIND_I);

					mono_mb_emit_icall (mb, mono_marshal_free);
				}
			}
			else if (eklass == mono_defaults.string_class) {
				if (need_free) {
					/* src */
					mono_mb_emit_ldloc (mb, src_ptr);
					mono_mb_emit_byte (mb, CEE_LDIND_I);

					mono_mb_emit_icall (mb, mono_marshal_free);
				}
			}
			else {
				if (need_convert) {
					/* set the src_ptr */
					mono_mb_emit_ldloc (mb, src_ptr);
					mono_mb_emit_stloc (mb, 0);

					/* set dst_ptr */
					mono_mb_emit_ldarg (mb, argnum);
					if (m_type_is_byref (t))
						mono_mb_emit_byte (mb, CEE_LDIND_REF);
					mono_mb_emit_ldloc (mb, index_var);
					mono_mb_emit_op (mb, CEE_LDELEMA, eklass);
					mono_mb_emit_stloc (mb, 1);

					/* emit valuetype conversion code */
					emit_struct_conv_full (mb, eklass, TRUE, 0, eklass == mono_defaults.char_class ? encoding : (MonoMarshalNative)-1);
				}

				if (need_free) {
					mono_mb_emit_ldloc (mb, src_ptr);
					mono_mb_emit_stloc (mb, loc);

					emit_struct_free (mb, eklass, loc);
				}
			}

			mono_mb_emit_add_to_local (mb, index_var, 1);
			mono_mb_emit_add_to_local (mb, src_ptr, esize);

			mono_mb_emit_branch_label (mb, CEE_BR, label2);

			mono_mb_patch_branch (mb, label1);
			mono_mb_patch_branch (mb, label3);
		}
#endif

		if (m_class_is_blittable (eklass)) {
			/* free memory allocated (if any) by MONO_MARSHAL_CONV_ARRAY_LPARRAY */

			mono_mb_emit_ldarg (mb, argnum);
			if (m_type_is_byref (t))
				mono_mb_emit_byte (mb, CEE_LDIND_REF);
			mono_mb_emit_ldloc (mb, conv_arg);
			mono_mb_emit_icall_id (mb, conv_to_icall (MONO_MARSHAL_FREE_LPARRAY, NULL));
		}

		break;
	}

	case MARSHAL_ACTION_PUSH:
		if (m_type_is_byref (t))
			mono_mb_emit_ldloc_addr (mb, conv_arg);
		else
			mono_mb_emit_ldloc (mb, conv_arg);
		break;

	case MARSHAL_ACTION_CONV_RESULT: {
		mono_mb_emit_byte (mb, CEE_POP);
		char *msg = g_strdup_printf ("Cannot marshal 'return value': Invalid managed/unmanaged type combination.");
		mono_mb_emit_exception_marshal_directive (mb, msg);
		break;
	}

	case MARSHAL_ACTION_MANAGED_CONV_IN: {
		guint32 label1, label2, label3;
		int index_var, src_ptr, esize, param_num, num_elem;
		MonoMarshalConv conv;
		gboolean is_string = FALSE;

		conv_arg = mono_mb_add_local (mb, object_type);
		*conv_arg_type = int_type;

		if (m_type_is_byref (t)) {
			char *msg = g_strdup ("Byref array marshalling to managed code is not implemented.");
			mono_mb_emit_exception_marshal_directive (mb, msg);
			return conv_arg;
		}
		if (!spec) {
			char *msg = g_strdup ("[MarshalAs] attribute required to marshal arrays to managed code.");
			mono_mb_emit_exception_marshal_directive (mb, msg);
			return conv_arg;
		}

		switch (spec->native) {
		case MONO_NATIVE_LPARRAY:
			break;
		case MONO_NATIVE_SAFEARRAY:
#ifndef DISABLE_COM
			if (spec->data.safearray_data.elem_type != MONO_VARIANT_VARIANT) {
				char *msg = g_strdup ("Only SAFEARRAY(VARIANT) marshalling to managed code is implemented.");
				mono_mb_emit_exception_marshal_directive (mb, msg);
				return conv_arg;
			}
			return mono_cominterop_emit_marshal_safearray (m, argnum, t, spec, conv_arg, conv_arg_type, action);
#endif
		default: {
			char *msg = g_strdup ("Unsupported array type marshalling to managed code.");
			mono_mb_emit_exception_marshal_directive (mb, msg);
			return conv_arg;
		}
		}

		/* FIXME: t is from the method which is wrapped, not the delegate type */
		/* g_assert (t->attrs & PARAM_ATTRIBUTE_IN); */

		param_num = spec->data.array_data.param_num;
		num_elem = spec->data.array_data.num_elem;
		if (spec->data.array_data.elem_mult == 0)
			/* param_num is not specified */
			param_num = -1;

		if (param_num == -1) {
			if (num_elem <= 0) {
				char *msg = g_strdup ("Either SizeConst or SizeParamIndex should be specified when marshalling arrays to managed code.");
				mono_mb_emit_exception_marshal_directive (mb, msg);
				return conv_arg;
			}
		}

		/* FIXME: Optimize blittable case */

#ifndef DISABLE_NONBLITTABLE
		if (eklass == mono_defaults.string_class) {
			is_string = TRUE;
			gboolean need_free;
			conv = mono_marshal_get_ptr_to_string_conv (m->piinfo, spec, &need_free);
		}
		else if (eklass == mono_class_try_get_stringbuilder_class ()) {
			is_string = TRUE;
			gboolean need_free;
			conv = mono_marshal_get_ptr_to_stringbuilder_conv (m->piinfo, spec, &need_free);
		}
		else
			conv = MONO_MARSHAL_CONV_INVALID;
#endif

		mono_marshal_load_type_info (eklass);

		if (is_string)
			esize = TARGET_SIZEOF_VOID_P;
		else
			esize = mono_class_native_size (eklass, NULL);
		src_ptr = mono_mb_add_local (mb, int_type);

		mono_mb_emit_byte (mb, CEE_LDNULL);
		mono_mb_emit_stloc (mb, conv_arg);

		/* Check param index */
		if (param_num != -1) {
			if (param_num >= m->sig->param_count) {
				char *msg = g_strdup ("Array size control parameter index is out of range.");
				mono_mb_emit_exception_marshal_directive (mb, msg);
				return conv_arg;
			}
			switch (m->sig->params [param_num]->type) {
			case MONO_TYPE_I1:
			case MONO_TYPE_U1:
			case MONO_TYPE_I2:
			case MONO_TYPE_U2:
			case MONO_TYPE_I4:
			case MONO_TYPE_U4:
			case MONO_TYPE_I:
			case MONO_TYPE_U:
			case MONO_TYPE_I8:
			case MONO_TYPE_U8:
				break;
			default: {
				char *msg = g_strdup ("Array size control parameter must be an integral type.");
				mono_mb_emit_exception_marshal_directive (mb, msg);
				return conv_arg;
			}
			}
		}

		/* Check null */
		mono_mb_emit_ldarg (mb, argnum);
		label1 = mono_mb_emit_branch (mb, CEE_BRFALSE);

		mono_mb_emit_ldarg (mb, argnum);
		mono_mb_emit_stloc (mb, src_ptr);

		/* Create managed array */
		/*
		 * The LPArray marshalling spec says that sometimes param_num starts
		 * from 1, sometimes it starts from 0. But MS seems to allways start
		 * from 0.
		 */

		if (param_num == -1) {
			mono_mb_emit_icon (mb, num_elem);
		} else {
			mono_mb_emit_ldarg (mb, param_num);
			if (num_elem > 0) {
				mono_mb_emit_icon (mb, num_elem);
				mono_mb_emit_byte (mb, CEE_ADD);
			}
			mono_mb_emit_byte (mb, CEE_CONV_OVF_I);
		}

		mono_mb_emit_op (mb, CEE_NEWARR, eklass);
		mono_mb_emit_stloc (mb, conv_arg);

		if (m_class_is_blittable (eklass)) {
			mono_mb_emit_ldloc (mb, conv_arg);
			mono_mb_emit_byte (mb, CEE_CONV_I);
			mono_mb_emit_icon (mb, MONO_STRUCT_OFFSET (MonoArray, vector));
			mono_mb_emit_byte (mb, CEE_ADD);
			mono_mb_emit_ldarg (mb, argnum);
			mono_mb_emit_ldloc (mb, conv_arg);
			mono_mb_emit_byte (mb, CEE_LDLEN);
			mono_mb_emit_icon (mb, esize);
			mono_mb_emit_byte (mb, CEE_MUL);
			mono_mb_emit_byte (mb, CEE_PREFIX1);
			mono_mb_emit_byte (mb, CEE_CPBLK);
			mono_mb_patch_branch (mb, label1);
			break;
		}
#ifdef DISABLE_NONBLITTABLE
		else {
			char *msg = g_strdup ("Non-blittable marshalling conversion is disabled");
			mono_mb_emit_exception_marshal_directive (mb, msg);
		}
#else
		/* Emit marshalling loop */
		index_var = mono_mb_add_local (mb, int_type);
		mono_mb_emit_byte (mb, CEE_LDC_I4_0);
		mono_mb_emit_stloc (mb, index_var);
		label2 = mono_mb_get_label (mb);
		mono_mb_emit_ldloc (mb, index_var);
		mono_mb_emit_ldloc (mb, conv_arg);
		mono_mb_emit_byte (mb, CEE_LDLEN);
		label3 = mono_mb_emit_branch (mb, CEE_BGE);

		/* Emit marshalling code */
		if (is_string) {
			g_assert (conv != MONO_MARSHAL_CONV_INVALID);

			mono_mb_emit_ldloc (mb, conv_arg);
			mono_mb_emit_ldloc (mb, index_var);

			mono_mb_emit_ldloc (mb, src_ptr);
			mono_mb_emit_byte (mb, CEE_LDIND_I);

			mono_mb_emit_icall_id (mb, conv_to_icall (conv, NULL));
			mono_mb_emit_byte (mb, CEE_STELEM_REF);
		}
		else {
			char *msg = g_strdup ("Marshalling of non-string and non-blittable arrays to managed code is not implemented.");
			mono_mb_emit_exception_marshal_directive (mb, msg);
			return conv_arg;
		}

		mono_mb_emit_add_to_local (mb, index_var, 1);
		mono_mb_emit_add_to_local (mb, src_ptr, esize);

		mono_mb_emit_branch_label (mb, CEE_BR, label2);

		mono_mb_patch_branch (mb, label1);
		mono_mb_patch_branch (mb, label3);
#endif

		break;
	}
	case MARSHAL_ACTION_MANAGED_CONV_OUT: {
		guint32 label1, label2, label3;
		int index_var, dest_ptr, esize, param_num, num_elem;
		MonoMarshalConv conv;
		gboolean is_string = FALSE;

		if (!spec)
			/* Already handled in CONV_IN */
			break;

		/* These are already checked in CONV_IN */
		g_assert (!m_type_is_byref (t));
		g_assert (spec->native == MONO_NATIVE_LPARRAY);
		g_assert (t->attrs & PARAM_ATTRIBUTE_OUT);

		param_num = spec->data.array_data.param_num;
		num_elem = spec->data.array_data.num_elem;

		if (spec->data.array_data.elem_mult == 0)
			/* param_num is not specified */
			param_num = -1;

		if (param_num == -1) {
			if (num_elem <= 0) {
				g_assert_not_reached ();
			}
		}

		/* FIXME: Optimize blittable case */

#ifndef DISABLE_NONBLITTABLE
		if (eklass == mono_defaults.string_class) {
			is_string = TRUE;
			conv = mono_marshal_get_string_to_ptr_conv (m->piinfo, spec);
		}
		else if (eklass == mono_class_try_get_stringbuilder_class ()) {
			is_string = TRUE;
			conv = mono_marshal_get_stringbuilder_to_ptr_conv (m->piinfo, spec);
		}
		else
			conv = MONO_MARSHAL_CONV_INVALID;
#endif

		mono_marshal_load_type_info (eklass);

		if (is_string)
			esize = TARGET_SIZEOF_VOID_P;
		else
			esize = mono_class_native_size (eklass, NULL);

		dest_ptr = mono_mb_add_local (mb, int_type);

		/* Check null */
		mono_mb_emit_ldloc (mb, conv_arg);
		label1 = mono_mb_emit_branch (mb, CEE_BRFALSE);

		mono_mb_emit_ldarg (mb, argnum);
		mono_mb_emit_stloc (mb, dest_ptr);

		if (m_class_is_blittable (eklass)) {
			/* dest */
			mono_mb_emit_ldarg (mb, argnum);
			/* src */
			mono_mb_emit_ldloc (mb, conv_arg);
			mono_mb_emit_byte (mb, CEE_CONV_I);
			mono_mb_emit_icon (mb, MONO_STRUCT_OFFSET (MonoArray, vector));
			mono_mb_emit_byte (mb, CEE_ADD);
			/* length */
			mono_mb_emit_ldloc (mb, conv_arg);
			mono_mb_emit_byte (mb, CEE_LDLEN);
			mono_mb_emit_icon (mb, esize);
			mono_mb_emit_byte (mb, CEE_MUL);
			mono_mb_emit_byte (mb, CEE_PREFIX1);
			mono_mb_emit_byte (mb, CEE_CPBLK);
			mono_mb_patch_branch (mb, label1);
			break;
		}

#ifndef DISABLE_NONBLITTABLE
		/* Emit marshalling loop */
		index_var = mono_mb_add_local (mb, int_type);
		mono_mb_emit_byte (mb, CEE_LDC_I4_0);
		mono_mb_emit_stloc (mb, index_var);
		label2 = mono_mb_get_label (mb);
		mono_mb_emit_ldloc (mb, index_var);
		mono_mb_emit_ldloc (mb, conv_arg);
		mono_mb_emit_byte (mb, CEE_LDLEN);
		label3 = mono_mb_emit_branch (mb, CEE_BGE);

		/* Emit marshalling code */
		if (is_string) {
			int stind_op;
			g_assert (conv != MONO_MARSHAL_CONV_INVALID);

			/* dest */
			mono_mb_emit_ldloc (mb, dest_ptr);

			/* src */
			mono_mb_emit_ldloc (mb, conv_arg);
			mono_mb_emit_ldloc (mb, index_var);

			mono_mb_emit_byte (mb, CEE_LDELEM_REF);

			mono_mb_emit_icall_id (mb, conv_to_icall (conv, &stind_op));
			mono_mb_emit_byte (mb, stind_op);
		}
		else {
			char *msg = g_strdup ("Marshalling of non-string and non-blittable arrays to managed code is not implemented.");
			mono_mb_emit_exception_marshal_directive (mb, msg);
			return conv_arg;
		}

		mono_mb_emit_add_to_local (mb, index_var, 1);
		mono_mb_emit_add_to_local (mb, dest_ptr, esize);

		mono_mb_emit_branch_label (mb, CEE_BR, label2);

		mono_mb_patch_branch (mb, label1);
		mono_mb_patch_branch (mb, label3);
#endif

		break;
	}
	case MARSHAL_ACTION_MANAGED_CONV_RESULT: {
#ifndef DISABLE_NONBLITTABLE
		guint32 label1, label2, label3;
		int index_var, src, dest, esize;
		MonoMarshalConv conv = MONO_MARSHAL_CONV_INVALID;
		gboolean is_string = FALSE;

		g_assert (!m_type_is_byref (t));

		mono_marshal_load_type_info (eklass);

		if (eklass == mono_defaults.string_class) {
			is_string = TRUE;
			conv = mono_marshal_get_string_to_ptr_conv (m->piinfo, spec);
		}
		else {
			g_assert_not_reached ();
		}

		if (is_string)
			esize = TARGET_SIZEOF_VOID_P;
		else if (eklass == mono_defaults.char_class)
			esize = mono_pinvoke_is_unicode (m->piinfo) ? 2 : 1;
		else
			esize = mono_class_native_size (eklass, NULL);

		src = mono_mb_add_local (mb, object_type);
		dest = mono_mb_add_local (mb, int_type);

		mono_mb_emit_stloc (mb, src);
		mono_mb_emit_ldloc (mb, src);
		mono_mb_emit_stloc (mb, 3);

		/* Check for null */
		mono_mb_emit_ldloc (mb, src);
		label1 = mono_mb_emit_branch (mb, CEE_BRFALSE);

		/* Allocate native array */
		mono_mb_emit_icon (mb, esize);
		mono_mb_emit_ldloc (mb, src);
		mono_mb_emit_byte (mb, CEE_LDLEN);

		if (eklass == mono_defaults.string_class) {
			/* Make the array bigger for the terminating null */
			mono_mb_emit_byte (mb, CEE_LDC_I4_1);
			mono_mb_emit_byte (mb, CEE_ADD);
		}
		mono_mb_emit_byte (mb, CEE_MUL);
		mono_mb_emit_icall (mb, ves_icall_marshal_alloc);
		mono_mb_emit_stloc (mb, dest);
		mono_mb_emit_ldloc (mb, dest);
		mono_mb_emit_stloc (mb, 3);

		/* Emit marshalling loop */
		index_var = mono_mb_add_local (mb, int_type);
		mono_mb_emit_byte (mb, CEE_LDC_I4_0);
		mono_mb_emit_stloc (mb, index_var);
		label2 = mono_mb_get_label (mb);
		mono_mb_emit_ldloc (mb, index_var);
		mono_mb_emit_ldloc (mb, src);
		mono_mb_emit_byte (mb, CEE_LDLEN);
		label3 = mono_mb_emit_branch (mb, CEE_BGE);

		/* Emit marshalling code */
		if (is_string) {
			int stind_op;
			g_assert (conv != MONO_MARSHAL_CONV_INVALID);

			/* dest */
			mono_mb_emit_ldloc (mb, dest);

			/* src */
			mono_mb_emit_ldloc (mb, src);
			mono_mb_emit_ldloc (mb, index_var);

			mono_mb_emit_byte (mb, CEE_LDELEM_REF);

			mono_mb_emit_icall_id (mb, conv_to_icall (conv, &stind_op));
			mono_mb_emit_byte (mb, stind_op);
		}
		else {
			char *msg = g_strdup ("Marshalling of non-string arrays to managed code is not implemented.");
			mono_mb_emit_exception_marshal_directive (mb, msg);
			return conv_arg;
		}

		mono_mb_emit_add_to_local (mb, index_var, 1);
		mono_mb_emit_add_to_local (mb, dest, esize);

		mono_mb_emit_branch_label (mb, CEE_BR, label2);

		mono_mb_patch_branch (mb, label3);
		mono_mb_patch_branch (mb, label1);
#endif
		break;
	}
	default:
		g_assert_not_reached ();
	}
	return conv_arg;
}

static int
emit_marshal_ptr_ilgen (EmitMarshalContext *m, int argnum, MonoType *t,
		  MonoMarshalSpec *spec, int conv_arg,
		  MonoType **conv_arg_type, MarshalAction action)
{
	MonoMethodBuilder *mb = m->mb;

	switch (action) {
	case MARSHAL_ACTION_CONV_IN:
		/* MS seems to allow this in some cases, ie. bxc #158 */
		/*
		if (MONO_TYPE_ISSTRUCT (t->data.type) && !mono_class_from_mono_type_internal (t->data.type)->blittable) {
			char *msg = g_strdup_printf ("Can not marshal 'parameter #%d': Pointers can not reference marshaled structures. Use byref instead.", argnum + 1);
			mono_mb_emit_exception_marshal_directive (m->mb, msg);
		}
		*/
		break;

	case MARSHAL_ACTION_PUSH:
		mono_mb_emit_ldarg (mb, argnum);
		break;

	case MARSHAL_ACTION_CONV_RESULT:
		/* no conversions necessary */
		mono_mb_emit_stloc (mb, 3);
		break;

	default:
		break;
	}
	return conv_arg;
}

static int
emit_marshal_scalar_ilgen (EmitMarshalContext *m, int argnum, MonoType *t,
		     MonoMarshalSpec *spec, int conv_arg,
		     MonoType **conv_arg_type, MarshalAction action)
{
	MonoMethodBuilder *mb = m->mb;

	switch (action) {
	case MARSHAL_ACTION_PUSH:
		mono_mb_emit_ldarg (mb, argnum);
		break;

	case MARSHAL_ACTION_CONV_RESULT:
		/* no conversions necessary */
		mono_mb_emit_stloc (mb, 3);
		break;

	case MARSHAL_ACTION_MANAGED_CONV_RESULT:
		mono_mb_emit_stloc (mb, 3);
		break;

	default:
		break;
	}
	return conv_arg;
}

static int
emit_marshal_boolean_ilgen (EmitMarshalContext *m, int argnum, MonoType *t,
		      MonoMarshalSpec *spec,
		      int conv_arg, MonoType **conv_arg_type,
		      MarshalAction action)
{
	MonoMethodBuilder *mb = m->mb;
	MonoType *int_type = mono_get_int_type ();
	MonoType *boolean_type = m_class_get_byval_arg (mono_defaults.boolean_class);

	switch (action) {
	case MARSHAL_ACTION_CONV_IN: {
		MonoType *local_type;
		int label_false;
		guint8 ldc_op = CEE_LDC_I4_1;

		local_type = mono_marshal_boolean_conv_in_get_local_type (spec, &ldc_op);
		if (m_type_is_byref (t))
			*conv_arg_type = int_type;
		else
			*conv_arg_type = local_type;
		conv_arg = mono_mb_add_local (mb, local_type);

		mono_mb_emit_ldarg (mb, argnum);
		if (m_type_is_byref (t))
			mono_mb_emit_byte (mb, CEE_LDIND_I1);
		label_false = mono_mb_emit_branch (mb, CEE_BRFALSE);
		mono_mb_emit_byte (mb, ldc_op);
		mono_mb_emit_stloc (mb, conv_arg);
		mono_mb_patch_branch (mb, label_false);

		break;
	}

	case MARSHAL_ACTION_CONV_OUT:
	{
		int label_false, label_end;
		if (!m_type_is_byref (t))
			break;

		mono_mb_emit_ldarg (mb, argnum);
		mono_mb_emit_ldloc (mb, conv_arg);

		label_false = mono_mb_emit_branch (mb, CEE_BRFALSE);
		mono_mb_emit_byte (mb, CEE_LDC_I4_1);

		label_end = mono_mb_emit_branch (mb, CEE_BR);
		mono_mb_patch_branch (mb, label_false);
		mono_mb_emit_byte (mb, CEE_LDC_I4_0);
		mono_mb_patch_branch (mb, label_end);

		mono_mb_emit_byte (mb, CEE_STIND_I1);
		break;
	}

	case MARSHAL_ACTION_PUSH:
		if (m_type_is_byref (t))
			mono_mb_emit_ldloc_addr (mb, conv_arg);
		else if (conv_arg)
			mono_mb_emit_ldloc (mb, conv_arg);
		else
			mono_mb_emit_ldarg (mb, argnum);
		break;

	case MARSHAL_ACTION_CONV_RESULT:
		/* maybe we need to make sure that it fits within 8 bits */
		mono_mb_emit_stloc (mb, 3);
		break;

	case MARSHAL_ACTION_MANAGED_CONV_IN: {
		MonoClass* conv_arg_class = mono_defaults.int32_class;
		guint8 ldop = CEE_LDIND_I4;
		int label_null, label_false;

		conv_arg_class = mono_marshal_boolean_managed_conv_in_get_conv_arg_class (spec, &ldop);
		conv_arg = mono_mb_add_local (mb, boolean_type);

		if (m_type_is_byref (t))
			*conv_arg_type = m_class_get_this_arg (conv_arg_class);
		else
			*conv_arg_type = m_class_get_byval_arg (conv_arg_class);


		mono_mb_emit_ldarg (mb, argnum);

		/* Check null */
		if (m_type_is_byref (t)) {
			label_null = mono_mb_emit_branch (mb, CEE_BRFALSE);
			mono_mb_emit_ldarg (mb, argnum);
			mono_mb_emit_byte (mb, ldop);
		} else
			label_null = 0;

		label_false = mono_mb_emit_branch (mb, CEE_BRFALSE);
		mono_mb_emit_byte (mb, CEE_LDC_I4_1);
		mono_mb_emit_stloc (mb, conv_arg);
		mono_mb_patch_branch (mb, label_false);

		if (m_type_is_byref (t))
			mono_mb_patch_branch (mb, label_null);
		break;
	}

	case MARSHAL_ACTION_MANAGED_CONV_OUT: {
		guint8 stop = CEE_STIND_I4;
		guint8 ldc_op = CEE_LDC_I4_1;
		int label_null,label_false, label_end;

		if (!m_type_is_byref (t))
			break;
		if (spec) {
			switch (spec->native) {
			case MONO_NATIVE_I1:
			case MONO_NATIVE_U1:
				stop = CEE_STIND_I1;
				break;
			case MONO_NATIVE_VARIANTBOOL:
				stop = CEE_STIND_I2;
				ldc_op = CEE_LDC_I4_M1;
				break;
			default:
				break;
			}
		}

		/* Check null */
		mono_mb_emit_ldarg (mb, argnum);
		label_null = mono_mb_emit_branch (mb, CEE_BRFALSE);

		mono_mb_emit_ldarg (mb, argnum);
		mono_mb_emit_ldloc (mb, conv_arg);

		label_false = mono_mb_emit_branch (mb, CEE_BRFALSE);
		mono_mb_emit_byte (mb, ldc_op);
		label_end = mono_mb_emit_branch (mb, CEE_BR);

		mono_mb_patch_branch (mb, label_false);
		mono_mb_emit_byte (mb, CEE_LDC_I4_0);
		mono_mb_patch_branch (mb, label_end);

		mono_mb_emit_byte (mb, stop);
		mono_mb_patch_branch (mb, label_null);
		break;
	}

	default:
		g_assert_not_reached ();
	}
	return conv_arg;
}

static int
emit_marshal_char_ilgen (EmitMarshalContext *m, int argnum, MonoType *t,
		   MonoMarshalSpec *spec, int conv_arg,
		   MonoType **conv_arg_type, MarshalAction action)
{
	MonoMethodBuilder *mb = m->mb;

	switch (action) {
	case MARSHAL_ACTION_PUSH:
		/* fixme: dont know how to marshal that. We cant simply
		 * convert it to a one byte UTF8 character, because an
		 * unicode character may need more that one byte in UTF8 */
		mono_mb_emit_ldarg (mb, argnum);
		break;

	case MARSHAL_ACTION_CONV_RESULT:
		/* fixme: we need conversions here */
		mono_mb_emit_stloc (mb, 3);
		break;

	default:
		break;
	}
	return conv_arg;
}

static void
emit_virtual_stelemref_ilgen (MonoMethodBuilder *mb, const char **param_names, MonoStelemrefKind kind)
{
	guint32 b1, b2, b3, b4;
	int aklass, vklass, vtable, uiid;
	int array_slot_addr;

	mono_mb_set_param_names (mb, param_names);
	MonoType *int_type = mono_get_int_type ();
	MonoType *int32_type = m_class_get_byval_arg (mono_defaults.int32_class);
	MonoType *object_type_byref = mono_class_get_byref_type (mono_defaults.object_class);

	/*For now simply call plain old stelemref*/
	switch (kind) {
	case STELEMREF_OBJECT:
		/* ldelema (implicit bound check) */
		load_array_element_address (mb);
		/* do_store */
		mono_mb_emit_ldarg (mb, 2);
		mono_mb_emit_byte (mb, CEE_STIND_REF);
		mono_mb_emit_byte (mb, CEE_RET);
		break;

	case STELEMREF_COMPLEX: {
		int b_fast;
		/*
		<ldelema (bound check)>
		if (!value)
			goto store;
		if (!mono_object_isinst (value, aklass))
			goto do_exception;

		 do_store:
			 *array_slot_addr = value;

		do_exception:
			throw new ArrayTypeMismatchException ();
		*/

		aklass = mono_mb_add_local (mb, int_type);
		vklass = mono_mb_add_local (mb, int_type);
		array_slot_addr = mono_mb_add_local (mb, object_type_byref);

#if 0
		{
			/*Use this to debug/record stores that are going thru the slow path*/
			MonoMethodSignature *csig;
			csig = mono_metadata_signature_alloc (mono_defaults.corlib, 3);
			csig->ret = mono_get_void_type ();
			csig->params [0] = object_type;
			csig->params [1] = int_type; /* this is a natural sized int */
			csig->params [2] = object_type;
			mono_mb_emit_ldarg (mb, 0);
			mono_mb_emit_ldarg (mb, 1);
			mono_mb_emit_ldarg (mb, 2);
			mono_mb_emit_native_call (mb, csig, record_slot_vstore);
		}
#endif

		/* ldelema (implicit bound check) */
		load_array_element_address (mb);
		mono_mb_emit_stloc (mb, array_slot_addr);

		/* if (!value) goto do_store */
		mono_mb_emit_ldarg (mb, 2);
		b1 = mono_mb_emit_branch (mb, CEE_BRFALSE);

		/* aklass = array->vtable->klass->element_class */
		load_array_class (mb, aklass);
		/* vklass = value->vtable->klass */
		load_value_class (mb, vklass);

		/* fastpath */
		mono_mb_emit_ldloc (mb, vklass);
		mono_mb_emit_ldloc (mb, aklass);
		b_fast = mono_mb_emit_branch (mb, CEE_BEQ);

		/*if (mono_object_isinst (value, aklass)) */
		mono_mb_emit_ldarg (mb, 2);
		mono_mb_emit_ldloc (mb, aklass);
		mono_mb_emit_icall (mb, mono_object_isinst_icall);
		b2 = mono_mb_emit_branch (mb, CEE_BRFALSE);

		/* do_store: */
		mono_mb_patch_branch (mb, b1);
		mono_mb_patch_branch (mb, b_fast);
		mono_mb_emit_ldloc (mb, array_slot_addr);
		mono_mb_emit_ldarg (mb, 2);
		mono_mb_emit_byte (mb, CEE_STIND_REF);
		mono_mb_emit_byte (mb, CEE_RET);

		/* do_exception: */
		mono_mb_patch_branch (mb, b2);

		mono_mb_emit_exception (mb, "ArrayTypeMismatchException", NULL);
		break;
	}
	case STELEMREF_SEALED_CLASS:
		/*
		<ldelema (bound check)>
		if (!value)
			goto store;

		aklass = array->vtable->m_class_get_element_class (klass);
		vklass = value->vtable->klass;

		if (vklass != aklass)
			goto do_exception;

		do_store:
			 *array_slot_addr = value;

		do_exception:
			throw new ArrayTypeMismatchException ();
		*/
		aklass = mono_mb_add_local (mb, int_type);
		vklass = mono_mb_add_local (mb, int_type);
		array_slot_addr = mono_mb_add_local (mb, object_type_byref);

		/* ldelema (implicit bound check) */
		load_array_element_address (mb);
		mono_mb_emit_stloc (mb, array_slot_addr);

		/* if (!value) goto do_store */
		mono_mb_emit_ldarg (mb, 2);
		b1 = mono_mb_emit_branch (mb, CEE_BRFALSE);

		/* aklass = array->vtable->klass->element_class */
		load_array_class (mb, aklass);

		/* vklass = value->vtable->klass */
		load_value_class (mb, vklass);

		/*if (vklass != aklass) goto do_exception; */
		mono_mb_emit_ldloc (mb, aklass);
		mono_mb_emit_ldloc (mb, vklass);
		b2 = mono_mb_emit_branch (mb, CEE_BNE_UN);

		/* do_store: */
		mono_mb_patch_branch (mb, b1);
		mono_mb_emit_ldloc (mb, array_slot_addr);
		mono_mb_emit_ldarg (mb, 2);
		mono_mb_emit_byte (mb, CEE_STIND_REF);
		mono_mb_emit_byte (mb, CEE_RET);

		/* do_exception: */
		mono_mb_patch_branch (mb, b2);
		mono_mb_emit_exception (mb, "ArrayTypeMismatchException", NULL);
		break;

	case STELEMREF_CLASS: {
		/*
		the method:
		<ldelema (bound check)>
		if (!value)
			goto do_store;

		aklass = array->vtable->m_class_get_element_class (klass);
		vklass = value->vtable->klass;

		if (vklass->idepth < aklass->idepth)
			goto do_exception;

		if (vklass->supertypes [aklass->idepth - 1] != aklass)
			goto do_exception;

		do_store:
			*array_slot_addr = value;
			return;

		long:
			throw new ArrayTypeMismatchException ();
		*/
		aklass = mono_mb_add_local (mb, int_type);
		vklass = mono_mb_add_local (mb, int_type);
		array_slot_addr = mono_mb_add_local (mb, object_type_byref);

		/* ldelema (implicit bound check) */
		load_array_element_address (mb);
		mono_mb_emit_stloc (mb, array_slot_addr);

		/* if (!value) goto do_store */
		mono_mb_emit_ldarg (mb, 2);
		b1 = mono_mb_emit_branch (mb, CEE_BRFALSE);

		/* aklass = array->vtable->klass->element_class */
		load_array_class (mb, aklass);

		/* vklass = value->vtable->klass */
		load_value_class (mb, vklass);

		/* if (vklass->idepth < aklass->idepth) goto failue */
		mono_mb_emit_ldloc (mb, vklass);
		mono_mb_emit_ldflda (mb, m_class_offsetof_idepth ());
		mono_mb_emit_byte (mb, CEE_LDIND_U2);

		mono_mb_emit_ldloc (mb, aklass);
		mono_mb_emit_ldflda (mb, m_class_offsetof_idepth ());
		mono_mb_emit_byte (mb, CEE_LDIND_U2);

		b3 = mono_mb_emit_branch (mb, CEE_BLT_UN);

		/* if (vklass->supertypes [aklass->idepth - 1] != aklass) goto failure */
		mono_mb_emit_ldloc (mb, vklass);
		mono_mb_emit_ldflda (mb, m_class_offsetof_supertypes ());
		mono_mb_emit_byte (mb, CEE_LDIND_I);

		mono_mb_emit_ldloc (mb, aklass);
		mono_mb_emit_ldflda (mb, m_class_offsetof_idepth ());
		mono_mb_emit_byte (mb, CEE_LDIND_U2);
		mono_mb_emit_icon (mb, 1);
		mono_mb_emit_byte (mb, CEE_SUB);
		mono_mb_emit_icon (mb, TARGET_SIZEOF_VOID_P);
		mono_mb_emit_byte (mb, CEE_MUL);
		mono_mb_emit_byte (mb, CEE_ADD);
		mono_mb_emit_byte (mb, CEE_LDIND_I);

		mono_mb_emit_ldloc (mb, aklass);
		b4 = mono_mb_emit_branch (mb, CEE_BNE_UN);

		/* do_store: */
		mono_mb_patch_branch (mb, b1);
		mono_mb_emit_ldloc (mb, array_slot_addr);
		mono_mb_emit_ldarg (mb, 2);
		mono_mb_emit_byte (mb, CEE_STIND_REF);
		mono_mb_emit_byte (mb, CEE_RET);

		/* do_exception: */
		mono_mb_patch_branch (mb, b3);
		mono_mb_patch_branch (mb, b4);

		mono_mb_emit_exception (mb, "ArrayTypeMismatchException", NULL);
		break;
	}

	case STELEMREF_CLASS_SMALL_IDEPTH:
		/*
		the method:
		<ldelema (bound check)>
		if (!value)
			goto do_store;

		aklass = array->vtable->m_class_get_element_class (klass);
		vklass = value->vtable->klass;

		if (vklass->supertypes [aklass->idepth - 1] != aklass)
			goto do_exception;

		do_store:
			*array_slot_addr = value;
			return;

		long:
			throw new ArrayTypeMismatchException ();
		*/
		aklass = mono_mb_add_local (mb, int_type);
		vklass = mono_mb_add_local (mb, int_type);
		array_slot_addr = mono_mb_add_local (mb, object_type_byref);

		/* ldelema (implicit bound check) */
		load_array_element_address (mb);
		mono_mb_emit_stloc (mb, array_slot_addr);

		/* if (!value) goto do_store */
		mono_mb_emit_ldarg (mb, 2);
		b1 = mono_mb_emit_branch (mb, CEE_BRFALSE);

		/* aklass = array->vtable->klass->element_class */
		load_array_class (mb, aklass);

		/* vklass = value->vtable->klass */
		load_value_class (mb, vklass);

		/* if (vklass->supertypes [aklass->idepth - 1] != aklass) goto failure */
		mono_mb_emit_ldloc (mb, vklass);
		mono_mb_emit_ldflda (mb, m_class_offsetof_supertypes ());
		mono_mb_emit_byte (mb, CEE_LDIND_I);

		mono_mb_emit_ldloc (mb, aklass);
		mono_mb_emit_ldflda (mb, m_class_offsetof_idepth ());
		mono_mb_emit_byte (mb, CEE_LDIND_U2);
		mono_mb_emit_icon (mb, 1);
		mono_mb_emit_byte (mb, CEE_SUB);
		mono_mb_emit_icon (mb, TARGET_SIZEOF_VOID_P);
		mono_mb_emit_byte (mb, CEE_MUL);
		mono_mb_emit_byte (mb, CEE_ADD);
		mono_mb_emit_byte (mb, CEE_LDIND_I);

		mono_mb_emit_ldloc (mb, aklass);
		b4 = mono_mb_emit_branch (mb, CEE_BNE_UN);

		/* do_store: */
		mono_mb_patch_branch (mb, b1);
		mono_mb_emit_ldloc (mb, array_slot_addr);
		mono_mb_emit_ldarg (mb, 2);
		mono_mb_emit_byte (mb, CEE_STIND_REF);
		mono_mb_emit_byte (mb, CEE_RET);

		/* do_exception: */
		mono_mb_patch_branch (mb, b4);

		mono_mb_emit_exception (mb, "ArrayTypeMismatchException", NULL);
		break;

	case STELEMREF_INTERFACE:
		/*Mono *klass;
		MonoVTable *vt;
		unsigned uiid;
		if (value == NULL)
			goto store;

		klass = array->obj.vtable->klass->element_class;
		vt = value->vtable;
		uiid = klass->interface_id;
		if (uiid > vt->max_interface_id)
			goto exception;
		if (!(vt->interface_bitmap [(uiid) >> 3] & (1 << ((uiid)&7))))
			goto exception;
		store:
			mono_array_setref_internal (array, index, value);
			return;
		exception:
			mono_raise_exception (mono_get_exception_array_type_mismatch ());*/

		array_slot_addr = mono_mb_add_local (mb, object_type_byref);
		aklass = mono_mb_add_local (mb, int_type);
		vtable = mono_mb_add_local (mb, int_type);
		uiid = mono_mb_add_local (mb, int32_type);

		/* ldelema (implicit bound check) */
		load_array_element_address (mb);
		mono_mb_emit_stloc (mb, array_slot_addr);

		/* if (!value) goto do_store */
		mono_mb_emit_ldarg (mb, 2);
		b1 = mono_mb_emit_branch (mb, CEE_BRFALSE);

		/* klass = array->vtable->m_class_get_element_class (klass) */
		load_array_class (mb, aklass);

		/* vt = value->vtable */
		mono_mb_emit_ldarg (mb, 2);
		mono_mb_emit_ldflda (mb, MONO_STRUCT_OFFSET (MonoObject, vtable));
		mono_mb_emit_byte (mb, CEE_LDIND_I);
		mono_mb_emit_stloc (mb, vtable);

		/* uiid = klass->interface_id; */
		mono_mb_emit_ldloc (mb, aklass);
		mono_mb_emit_ldflda (mb, m_class_offsetof_interface_id ());
		mono_mb_emit_byte (mb, CEE_LDIND_U4);
		mono_mb_emit_stloc (mb, uiid);

		/*if (uiid > vt->max_interface_id)*/
		mono_mb_emit_ldloc (mb, uiid);
		mono_mb_emit_ldloc (mb, vtable);
		mono_mb_emit_ldflda (mb, MONO_STRUCT_OFFSET (MonoVTable, max_interface_id));
		mono_mb_emit_byte (mb, CEE_LDIND_U4);
		b2 = mono_mb_emit_branch (mb, CEE_BGT_UN);

		/* if (!(vt->interface_bitmap [(uiid) >> 3] & (1 << ((uiid)&7)))) */

		/*vt->interface_bitmap*/
		mono_mb_emit_ldloc (mb, vtable);
		mono_mb_emit_ldflda (mb, MONO_STRUCT_OFFSET (MonoVTable, interface_bitmap));
		mono_mb_emit_byte (mb, CEE_LDIND_I);

		/*uiid >> 3*/
		mono_mb_emit_ldloc (mb, uiid);
		mono_mb_emit_icon (mb, 3);
		mono_mb_emit_byte (mb, CEE_SHR_UN);

		/*vt->interface_bitmap [(uiid) >> 3]*/
		mono_mb_emit_byte (mb, CEE_ADD); /*interface_bitmap is a guint8 array*/
		mono_mb_emit_byte (mb, CEE_LDIND_U1);

		/*(1 << ((uiid)&7)))*/
		mono_mb_emit_icon (mb, 1);
		mono_mb_emit_ldloc (mb, uiid);
		mono_mb_emit_icon (mb, 7);
		mono_mb_emit_byte (mb, CEE_AND);
		mono_mb_emit_byte (mb, CEE_SHL);

		/*bitwise and the whole thing*/
		mono_mb_emit_byte (mb, CEE_AND);
		b3 = mono_mb_emit_branch (mb, CEE_BRFALSE);

		/* do_store: */
		mono_mb_patch_branch (mb, b1);
		mono_mb_emit_ldloc (mb, array_slot_addr);
		mono_mb_emit_ldarg (mb, 2);
		mono_mb_emit_byte (mb, CEE_STIND_REF);
		mono_mb_emit_byte (mb, CEE_RET);

		/* do_exception: */
		mono_mb_patch_branch (mb, b2);
		mono_mb_patch_branch (mb, b3);
		mono_mb_emit_exception (mb, "ArrayTypeMismatchException", NULL);
		break;

	default:
		mono_mb_emit_ldarg (mb, 0);
		mono_mb_emit_ldarg (mb, 1);
		mono_mb_emit_ldarg (mb, 2);
		mono_mb_emit_managed_call (mb, mono_marshal_get_stelemref (), NULL);
		mono_mb_emit_byte (mb, CEE_RET);
		g_assert (0);
	}
}

static void
emit_stelemref_ilgen (MonoMethodBuilder *mb)
{
	guint32 b1, b2, b3, b4;
	guint32 copy_pos;
	int aklass, vklass;
	int array_slot_addr;

	MonoType *int_type = mono_get_int_type ();
	MonoType *object_type_byref = mono_class_get_byref_type (mono_defaults.object_class);

	aklass = mono_mb_add_local (mb, int_type);
	vklass = mono_mb_add_local (mb, int_type);
	array_slot_addr = mono_mb_add_local (mb, object_type_byref);

	/*
	the method:
	<ldelema (bound check)>
	if (!value)
		goto store;

	aklass = array->vtable->m_class_get_element_class (klass);
	vklass = value->vtable->klass;

	if (vklass->idepth < aklass->idepth)
		goto long;

	if (vklass->supertypes [aklass->idepth - 1] != aklass)
		goto long;

	store:
		*array_slot_addr = value;
		return;

	long:
		if (mono_object_isinst (value, aklass))
			goto store;

		throw new ArrayTypeMismatchException ();
	*/

	/* ldelema (implicit bound check) */
	mono_mb_emit_ldarg (mb, 0);
	mono_mb_emit_ldarg (mb, 1);
	mono_mb_emit_op (mb, CEE_LDELEMA, mono_defaults.object_class);
	mono_mb_emit_stloc (mb, array_slot_addr);

	/* if (!value) goto do_store */
	mono_mb_emit_ldarg (mb, 2);
	b1 = mono_mb_emit_branch (mb, CEE_BRFALSE);

	/* aklass = array->vtable->klass->element_class */
	mono_mb_emit_ldarg (mb, 0);
	mono_mb_emit_ldflda (mb, MONO_STRUCT_OFFSET (MonoObject, vtable));
	mono_mb_emit_byte (mb, CEE_LDIND_I);
	mono_mb_emit_ldflda (mb, MONO_STRUCT_OFFSET (MonoVTable, klass));
	mono_mb_emit_byte (mb, CEE_LDIND_I);
	mono_mb_emit_ldflda (mb, m_class_offsetof_element_class ());
	mono_mb_emit_byte (mb, CEE_LDIND_I);
	mono_mb_emit_stloc (mb, aklass);

	/* vklass = value->vtable->klass */
	mono_mb_emit_ldarg (mb, 2);
	mono_mb_emit_ldflda (mb, MONO_STRUCT_OFFSET (MonoObject, vtable));
	mono_mb_emit_byte (mb, CEE_LDIND_I);
	mono_mb_emit_ldflda (mb, MONO_STRUCT_OFFSET (MonoVTable, klass));
	mono_mb_emit_byte (mb, CEE_LDIND_I);
	mono_mb_emit_stloc (mb, vklass);

	/* if (vklass->idepth < aklass->idepth) goto failue */
	mono_mb_emit_ldloc (mb, vklass);
	mono_mb_emit_ldflda (mb, m_class_offsetof_idepth ());
	mono_mb_emit_byte (mb, CEE_LDIND_U2);

	mono_mb_emit_ldloc (mb, aklass);
	mono_mb_emit_ldflda (mb, m_class_offsetof_idepth ());
	mono_mb_emit_byte (mb, CEE_LDIND_U2);

	b2 = mono_mb_emit_branch (mb, CEE_BLT_UN);

	/* if (vklass->supertypes [aklass->idepth - 1] != aklass) goto failure */
	mono_mb_emit_ldloc (mb, vklass);
	mono_mb_emit_ldflda (mb, m_class_offsetof_supertypes ());
	mono_mb_emit_byte (mb, CEE_LDIND_I);

	mono_mb_emit_ldloc (mb, aklass);
	mono_mb_emit_ldflda (mb, m_class_offsetof_idepth ());
	mono_mb_emit_byte (mb, CEE_LDIND_U2);
	mono_mb_emit_icon (mb, 1);
	mono_mb_emit_byte (mb, CEE_SUB);
	mono_mb_emit_icon (mb, TARGET_SIZEOF_VOID_P);
	mono_mb_emit_byte (mb, CEE_MUL);
	mono_mb_emit_byte (mb, CEE_ADD);
	mono_mb_emit_byte (mb, CEE_LDIND_I);

	mono_mb_emit_ldloc (mb, aklass);

	b3 = mono_mb_emit_branch (mb, CEE_BNE_UN);

	copy_pos = mono_mb_get_label (mb);
	/* do_store */
	mono_mb_patch_branch (mb, b1);
	mono_mb_emit_ldloc (mb, array_slot_addr);
	mono_mb_emit_ldarg (mb, 2);
	mono_mb_emit_byte (mb, CEE_STIND_REF);

	mono_mb_emit_byte (mb, CEE_RET);

	/* the hard way */
	mono_mb_patch_branch (mb, b2);
	mono_mb_patch_branch (mb, b3);

	mono_mb_emit_ldarg (mb, 2);
	mono_mb_emit_ldloc (mb, aklass);
	mono_mb_emit_icall (mb, mono_object_isinst_icall);

	b4 = mono_mb_emit_branch (mb, CEE_BRTRUE);
	mono_mb_patch_addr (mb, b4, copy_pos - (b4 + 4));
	mono_mb_emit_exception (mb, "ArrayTypeMismatchException", NULL);

	mono_mb_emit_byte (mb, CEE_RET);
}

static void
mb_emit_byte_ilgen (MonoMethodBuilder *mb, guint8 op)
{
	mono_mb_emit_byte (mb, op);
}

static void
emit_array_address_ilgen (MonoMethodBuilder *mb, int rank, int elem_size)
{
	int i, bounds, ind, realidx;
	int branch_pos, *branch_positions;

	MonoType *int_type = mono_get_int_type ();
	MonoType *int32_type = mono_get_int32_type ();

	branch_positions = g_new0 (int, rank);

	bounds = mono_mb_add_local (mb, int_type);
	ind = mono_mb_add_local (mb, int32_type);
	realidx = mono_mb_add_local (mb, int32_type);

	/* bounds = array->bounds; */
	mono_mb_emit_ldarg (mb, 0);
	mono_mb_emit_ldflda (mb, MONO_STRUCT_OFFSET (MonoArray, bounds));
	mono_mb_emit_byte (mb, CEE_LDIND_I);
	mono_mb_emit_stloc (mb, bounds);

	/* ind is the overall element index, realidx is the partial index in a single dimension */
	/* ind = idx0 - bounds [0].lower_bound */
	mono_mb_emit_ldarg (mb, 1);
	mono_mb_emit_ldloc (mb, bounds);
	mono_mb_emit_icon (mb, MONO_STRUCT_OFFSET (MonoArrayBounds, lower_bound));
	mono_mb_emit_byte (mb, CEE_ADD);
	mono_mb_emit_byte (mb, CEE_LDIND_I4);
	mono_mb_emit_byte (mb, CEE_SUB);
	mono_mb_emit_stloc (mb, ind);
	/* if (ind >= bounds [0].length) goto exeception; */
	mono_mb_emit_ldloc (mb, ind);
	mono_mb_emit_ldloc (mb, bounds);
	mono_mb_emit_icon (mb, MONO_STRUCT_OFFSET (MonoArrayBounds, length));
	mono_mb_emit_byte (mb, CEE_ADD);
	mono_mb_emit_byte (mb, CEE_LDIND_I4);
	/* note that we use unsigned comparison */
	branch_pos = mono_mb_emit_branch (mb, CEE_BGE_UN);

 	/* For large ranks (> 4?) use a loop n IL later to reduce code size.
	 * We could also decide to ignore the passed elem_size and get it
	 * from the array object, to reduce the number of methods we generate:
	 * the additional cost is 3 memory loads and a non-immediate mul.
	 */
	for (i = 1; i < rank; ++i) {
		/* realidx = idxi - bounds [i].lower_bound */
		mono_mb_emit_ldarg (mb, 1 + i);
		mono_mb_emit_ldloc (mb, bounds);
		mono_mb_emit_icon (mb, (i * sizeof (MonoArrayBounds)) + MONO_STRUCT_OFFSET (MonoArrayBounds, lower_bound));
		mono_mb_emit_byte (mb, CEE_ADD);
		mono_mb_emit_byte (mb, CEE_LDIND_I4);
		mono_mb_emit_byte (mb, CEE_SUB);
		mono_mb_emit_stloc (mb, realidx);
		/* if (realidx >= bounds [i].length) goto exeception; */
		mono_mb_emit_ldloc (mb, realidx);
		mono_mb_emit_ldloc (mb, bounds);
		mono_mb_emit_icon (mb, (i * sizeof (MonoArrayBounds)) + MONO_STRUCT_OFFSET (MonoArrayBounds, length));
		mono_mb_emit_byte (mb, CEE_ADD);
		mono_mb_emit_byte (mb, CEE_LDIND_I4);
		branch_positions [i] = mono_mb_emit_branch (mb, CEE_BGE_UN);
		/* ind = ind * bounds [i].length + realidx */
		mono_mb_emit_ldloc (mb, ind);
		mono_mb_emit_ldloc (mb, bounds);
		mono_mb_emit_icon (mb, (i * sizeof (MonoArrayBounds)) + MONO_STRUCT_OFFSET (MonoArrayBounds, length));
		mono_mb_emit_byte (mb, CEE_ADD);
		mono_mb_emit_byte (mb, CEE_LDIND_I4);
		mono_mb_emit_byte (mb, CEE_MUL);
		mono_mb_emit_ldloc (mb, realidx);
		mono_mb_emit_byte (mb, CEE_ADD);
		mono_mb_emit_stloc (mb, ind);
	}

	/* return array->vector + ind * element_size */
	mono_mb_emit_ldarg (mb, 0);
	mono_mb_emit_ldflda (mb, MONO_STRUCT_OFFSET (MonoArray, vector));
	mono_mb_emit_ldloc (mb, ind);
	if (elem_size) {
		mono_mb_emit_icon (mb, elem_size);
	} else {
		/* Load arr->vtable->klass->sizes.element_class */
		mono_mb_emit_ldarg (mb, 0);
		mono_mb_emit_byte (mb, CEE_CONV_I);
		mono_mb_emit_icon (mb, MONO_STRUCT_OFFSET (MonoObject, vtable));
		mono_mb_emit_byte (mb, CEE_ADD);
		mono_mb_emit_byte (mb, CEE_LDIND_I);
		mono_mb_emit_icon (mb, MONO_STRUCT_OFFSET (MonoVTable, klass));
		mono_mb_emit_byte (mb, CEE_ADD);
		mono_mb_emit_byte (mb, CEE_LDIND_I);
		/* sizes is an union, so this reads sizes.element_size */
		mono_mb_emit_icon (mb, m_class_offsetof_sizes ());
		mono_mb_emit_byte (mb, CEE_ADD);
		mono_mb_emit_byte (mb, CEE_LDIND_I4);
	}
		mono_mb_emit_byte (mb, CEE_MUL);
	mono_mb_emit_byte (mb, CEE_ADD);
	mono_mb_emit_byte (mb, CEE_RET);

	/* patch the branches to get here and throw */
	for (i = 1; i < rank; ++i) {
		mono_mb_patch_branch (mb, branch_positions [i]);
	}
	mono_mb_patch_branch (mb, branch_pos);
	/* throw exception */
	mono_mb_emit_exception (mb, "IndexOutOfRangeException", NULL);

	g_free (branch_positions);
}

static void
emit_delegate_begin_invoke_ilgen (MonoMethodBuilder *mb, MonoMethodSignature *sig)
{
	int params_var;
	params_var = mono_mb_emit_save_args (mb, sig, FALSE);

	mono_mb_emit_ldarg (mb, 0);
	mono_mb_emit_ldloc (mb, params_var);
	mono_mb_emit_icall (mb, mono_delegate_begin_invoke);
	mono_mb_emit_byte (mb, CEE_RET);
}

static void
emit_delegate_end_invoke_ilgen (MonoMethodBuilder *mb, MonoMethodSignature *sig)
{
	int params_var;
	params_var = mono_mb_emit_save_args (mb, sig, FALSE);

	mono_mb_emit_ldarg (mb, 0);
	mono_mb_emit_ldloc (mb, params_var);
	mono_mb_emit_icall (mb, mono_delegate_end_invoke);

	if (sig->ret->type == MONO_TYPE_VOID) {
		mono_mb_emit_byte (mb, CEE_POP);
		mono_mb_emit_byte (mb, CEE_RET);
	} else
		mono_mb_emit_restore_result (mb, sig->ret);
}

static void
emit_delegate_invoke_internal_ilgen (MonoMethodBuilder *mb, MonoMethodSignature *sig, MonoMethodSignature *invoke_sig, gboolean static_method_with_first_arg_bound, gboolean callvirt, gboolean closed_over_null, MonoMethod *method, MonoMethod *target_method, MonoClass *target_class, MonoGenericContext *ctx, MonoGenericContainer *container)
{
	int local_i, local_len, local_delegates, local_d, local_target, local_res;
	int pos0, pos1, pos2;
	int i;
	gboolean void_ret;

	MonoType *int32_type = mono_get_int32_type ();
	MonoType *object_type = mono_get_object_type ();

	void_ret = sig->ret->type == MONO_TYPE_VOID && !method->string_ctor;

	/* allocate local 0 (object) */
	local_i = mono_mb_add_local (mb, int32_type);
	local_len = mono_mb_add_local (mb, int32_type);
	local_delegates = mono_mb_add_local (mb, m_class_get_byval_arg (mono_defaults.array_class));
	local_d = mono_mb_add_local (mb, m_class_get_byval_arg (mono_defaults.multicastdelegate_class));
	local_target = mono_mb_add_local (mb, object_type);

	if (!void_ret)
		local_res = mono_mb_add_local (mb, m_class_get_byval_arg (mono_class_from_mono_type_internal (sig->ret)));

	g_assert (sig->hasthis);

	/*
	 * {type: sig->ret} res;
	 * if (delegates == null) {
	 *     return this.<target> ( args .. );
	 * } else {
	 *     int i = 0, len = this.delegates.Length;
	 *     do {
	 *         res = this.delegates [i].Invoke ( args .. );
	 *     } while (++i < len);
	 *     return res;
	 * }
	 */

	/* this wrapper can be used in unmanaged-managed transitions */
	emit_thread_interrupt_checkpoint (mb);

	/* delegates = this.delegates */
	mono_mb_emit_ldarg (mb, 0);
	mono_mb_emit_ldflda (mb, MONO_STRUCT_OFFSET (MonoMulticastDelegate, delegates));
	mono_mb_emit_byte (mb, CEE_LDIND_REF);
	mono_mb_emit_stloc (mb, local_delegates);

	/* if (delegates == null) */
	mono_mb_emit_ldloc (mb, local_delegates);
	pos2 = mono_mb_emit_branch (mb, CEE_BRTRUE);

	/* return target.<target_method|method_ptr> ( args .. ); */

	/* target = d.target; */
	mono_mb_emit_ldarg (mb, 0);
	mono_mb_emit_ldflda (mb, MONO_STRUCT_OFFSET (MonoDelegate, target));
	mono_mb_emit_byte (mb, CEE_LDIND_REF);
	mono_mb_emit_stloc (mb, local_target);

	/*static methods with bound first arg can have null target and still be bound*/
	if (!static_method_with_first_arg_bound) {
		/* if target != null */
		mono_mb_emit_ldloc (mb, local_target);
		pos0 = mono_mb_emit_branch (mb, CEE_BRFALSE);

		/* then call this->method_ptr nonstatic */
		if (callvirt) {
			// FIXME:
			mono_mb_emit_exception_full (mb, "System", "NotImplementedException", "");
		} else {
			mono_mb_emit_ldloc (mb, local_target);
			for (i = 0; i < sig->param_count; ++i)
				mono_mb_emit_ldarg (mb, i + 1);
			mono_mb_emit_ldarg (mb, 0);
			mono_mb_emit_ldflda (mb, MONO_STRUCT_OFFSET (MonoDelegate, extra_arg));
			mono_mb_emit_byte (mb, CEE_LDIND_I);
			mono_mb_emit_ldarg (mb, 0);
			mono_mb_emit_byte (mb, MONO_CUSTOM_PREFIX);
			mono_mb_emit_byte (mb, CEE_MONO_LD_DELEGATE_METHOD_PTR);
			mono_mb_emit_byte (mb, MONO_CUSTOM_PREFIX);
			mono_mb_emit_op (mb, CEE_MONO_CALLI_EXTRA_ARG, sig);
			mono_mb_emit_byte (mb, CEE_RET);
		}

		/* else [target == null] call this->method_ptr static */
		mono_mb_patch_branch (mb, pos0);
	}

	if (callvirt) {
		if (!closed_over_null) {
			/* if target_method is not really virtual, turn it into a direct call */
			if (!(target_method->flags & METHOD_ATTRIBUTE_VIRTUAL) || m_class_is_valuetype (target_class)) {
				mono_mb_emit_ldarg (mb, 1);
				for (i = 1; i < sig->param_count; ++i)
					mono_mb_emit_ldarg (mb, i + 1);
				mono_mb_emit_op (mb, CEE_CALL, target_method);
			} else {
				mono_mb_emit_ldarg (mb, 1);
				mono_mb_emit_op (mb, CEE_CASTCLASS, target_class);
				for (i = 1; i < sig->param_count; ++i)
					mono_mb_emit_ldarg (mb, i + 1);
				mono_mb_emit_op (mb, CEE_CALLVIRT, target_method);
			}
		} else {
			mono_mb_emit_byte (mb, CEE_LDNULL);
			for (i = 0; i < sig->param_count; ++i)
				mono_mb_emit_ldarg (mb, i + 1);
			mono_mb_emit_op (mb, CEE_CALL, target_method);
		}
	} else {
		if (static_method_with_first_arg_bound) {
			mono_mb_emit_ldloc (mb, local_target);
			if (!MONO_TYPE_IS_REFERENCE (invoke_sig->params[0]))
				mono_mb_emit_op (mb, CEE_UNBOX_ANY, mono_class_from_mono_type_internal (invoke_sig->params[0]));
		}
		for (i = 0; i < sig->param_count; ++i)
			mono_mb_emit_ldarg (mb, i + 1);
		mono_mb_emit_ldarg (mb, 0);
		mono_mb_emit_ldflda (mb, MONO_STRUCT_OFFSET (MonoDelegate, extra_arg));
		mono_mb_emit_byte (mb, CEE_LDIND_I);
		mono_mb_emit_ldarg (mb, 0);
		mono_mb_emit_byte (mb, MONO_CUSTOM_PREFIX);
		mono_mb_emit_byte (mb, CEE_MONO_LD_DELEGATE_METHOD_PTR);
		mono_mb_emit_byte (mb, MONO_CUSTOM_PREFIX);
		mono_mb_emit_op (mb, CEE_MONO_CALLI_EXTRA_ARG, invoke_sig);
	}

	mono_mb_emit_byte (mb, CEE_RET);

	/* else [delegates != null] */
	mono_mb_patch_branch (mb, pos2);

	/* len = delegates.Length; */
	mono_mb_emit_ldloc (mb, local_delegates);
	mono_mb_emit_byte (mb, CEE_LDLEN);
	mono_mb_emit_byte (mb, CEE_CONV_I4);
	mono_mb_emit_stloc (mb, local_len);

	/* i = 0; */
	mono_mb_emit_icon (mb, 0);
	mono_mb_emit_stloc (mb, local_i);

	pos1 = mono_mb_get_label (mb);

	/* d = delegates [i]; */
	mono_mb_emit_ldloc (mb, local_delegates);
	mono_mb_emit_ldloc (mb, local_i);
	mono_mb_emit_byte (mb, CEE_LDELEM_REF);
	mono_mb_emit_stloc (mb, local_d);

	/* res = d.Invoke ( args .. ); */
	mono_mb_emit_ldloc (mb, local_d);
	for (i = 0; i < sig->param_count; i++)
		mono_mb_emit_ldarg (mb, i + 1);
	if (!ctx) {
		mono_mb_emit_op (mb, CEE_CALLVIRT, method);
	} else {
		ERROR_DECL (error);
		mono_mb_emit_op (mb, CEE_CALLVIRT, mono_class_inflate_generic_method_checked (method, &container->context, error));
		g_assert (is_ok (error)); /* FIXME don't swallow the error */
	}
	if (!void_ret)
		mono_mb_emit_stloc (mb, local_res);

	/* i += 1 */
	mono_mb_emit_add_to_local (mb, local_i, 1);

	/* i < l */
	mono_mb_emit_ldloc (mb, local_i);
	mono_mb_emit_ldloc (mb, local_len);
	mono_mb_emit_branch_label (mb, CEE_BLT, pos1);

	/* return res */
	if (!void_ret)
		mono_mb_emit_ldloc (mb, local_res);
	mono_mb_emit_byte (mb, CEE_RET);
}

static void
mb_skip_visibility_ilgen (MonoMethodBuilder *mb)
{
	mb->skip_visibility = 1;
}

static void
mb_set_dynamic_ilgen (MonoMethodBuilder *mb)
{
	mb->dynamic = 1;
}

static void
emit_synchronized_wrapper_ilgen (MonoMethodBuilder *mb, MonoMethod *method, MonoGenericContext *ctx, MonoGenericContainer *container, MonoMethod *enter_method, MonoMethod *exit_method, MonoMethod *gettypefromhandle_method)
{
	int i, pos, pos2, this_local, taken_local, ret_local = 0;
	MonoMethodSignature *sig = mono_method_signature_internal (method);
	MonoExceptionClause *clause;

	/* result */
	if (!MONO_TYPE_IS_VOID (sig->ret))
		ret_local = mono_mb_add_local (mb, sig->ret);

	if (m_class_is_valuetype (method->klass) && !(method->flags & MONO_METHOD_ATTR_STATIC)) {
		/* FIXME Is this really the best way to signal an error here?  Isn't this called much later after class setup? -AK */
		mono_class_set_type_load_failure (method->klass, "");
		/* This will throw the type load exception when the wrapper is compiled */
		mono_mb_emit_byte (mb, CEE_LDNULL);
		mono_mb_emit_op (mb, CEE_ISINST, method->klass);
		mono_mb_emit_byte (mb, CEE_POP);

		if (!MONO_TYPE_IS_VOID (sig->ret))
			mono_mb_emit_ldloc (mb, ret_local);
		mono_mb_emit_byte (mb, CEE_RET);

		return;
	}

	MonoType *object_type = mono_get_object_type ();
	MonoType *boolean_type = m_class_get_byval_arg (mono_defaults.boolean_class);
	/* this */
	this_local = mono_mb_add_local (mb, object_type);
	taken_local = mono_mb_add_local (mb, boolean_type);

	clause = (MonoExceptionClause *)mono_image_alloc0 (get_method_image (method), sizeof (MonoExceptionClause));
	clause->flags = MONO_EXCEPTION_CLAUSE_FINALLY;

	/* Push this or the type object */
	if (method->flags & METHOD_ATTRIBUTE_STATIC) {
		/* We have special handling for this in the JIT */
		int index = mono_mb_add_data (mb, method->klass);
		mono_mb_add_data (mb, mono_defaults.typehandle_class);
		mono_mb_emit_byte (mb, CEE_LDTOKEN);
		mono_mb_emit_i4 (mb, index);

		mono_mb_emit_managed_call (mb, gettypefromhandle_method, NULL);
	}
	else
		mono_mb_emit_ldarg (mb, 0);
	mono_mb_emit_stloc (mb, this_local);

	clause->try_offset = mono_mb_get_label (mb);
	/* Call Monitor::Enter() */
	mono_mb_emit_ldloc (mb, this_local);
	mono_mb_emit_ldloc_addr (mb, taken_local);
	mono_mb_emit_managed_call (mb, enter_method, NULL);

	/* Call the method */
	if (sig->hasthis)
		mono_mb_emit_ldarg (mb, 0);
	for (i = 0; i < sig->param_count; i++)
		mono_mb_emit_ldarg (mb, i + (sig->hasthis == TRUE));

	if (ctx) {
		ERROR_DECL (error);
		mono_mb_emit_managed_call (mb, mono_class_inflate_generic_method_checked (method, &container->context, error), NULL);
		g_assert (is_ok (error)); /* FIXME don't swallow the error */
	} else {
		mono_mb_emit_managed_call (mb, method, NULL);
	}

	if (!MONO_TYPE_IS_VOID (sig->ret))
		mono_mb_emit_stloc (mb, ret_local);

	pos = mono_mb_emit_branch (mb, CEE_LEAVE);

	clause->try_len = mono_mb_get_pos (mb) - clause->try_offset;
	clause->handler_offset = mono_mb_get_label (mb);

	/* Call Monitor::Exit() if needed */
	mono_mb_emit_ldloc (mb, taken_local);
	pos2 = mono_mb_emit_branch (mb, CEE_BRFALSE);
	mono_mb_emit_ldloc (mb, this_local);
	mono_mb_emit_managed_call (mb, exit_method, NULL);
	mono_mb_patch_branch (mb, pos2);
	mono_mb_emit_byte (mb, CEE_ENDFINALLY);

	clause->handler_len = mono_mb_get_pos (mb) - clause->handler_offset;

	mono_mb_patch_branch (mb, pos);
	if (!MONO_TYPE_IS_VOID (sig->ret))
		mono_mb_emit_ldloc (mb, ret_local);
	mono_mb_emit_byte (mb, CEE_RET);

	mono_mb_set_clauses (mb, 1, clause);
}

static void
emit_unbox_wrapper_ilgen (MonoMethodBuilder *mb, MonoMethod *method)
{
	MonoMethodSignature *sig = mono_method_signature_internal (method);

	mono_mb_emit_ldarg (mb, 0);
	mono_mb_emit_icon (mb, MONO_ABI_SIZEOF (MonoObject));
	mono_mb_emit_byte (mb, CEE_ADD);
	for (int i = 0; i < sig->param_count; ++i)
		mono_mb_emit_ldarg (mb, i + 1);
	mono_mb_emit_managed_call (mb, method, NULL);
	mono_mb_emit_byte (mb, CEE_RET);
}

static void
emit_array_accessor_wrapper_ilgen (MonoMethodBuilder *mb, MonoMethod *method, MonoMethodSignature *sig, MonoGenericContext *ctx)
{
	MonoGenericContainer *container = NULL;
	/* Call the method */
	if (sig->hasthis)
		mono_mb_emit_ldarg (mb, 0);
	for (int i = 0; i < sig->param_count; i++)
		mono_mb_emit_ldarg (mb, i + (sig->hasthis == TRUE));

	if (ctx) {
		ERROR_DECL (error);
		mono_mb_emit_managed_call (mb, mono_class_inflate_generic_method_checked (method, &container->context, error), NULL);
		g_assert (is_ok (error)); /* FIXME don't swallow the error */
	} else {
		mono_mb_emit_managed_call (mb, method, NULL);
	}
	mono_mb_emit_byte (mb, CEE_RET);
}

static void
emit_generic_array_helper_ilgen (MonoMethodBuilder *mb, MonoMethod *method, MonoMethodSignature *csig)
{
	mono_mb_emit_ldarg (mb, 0);
	for (int i = 0; i < csig->param_count; i++)
		mono_mb_emit_ldarg (mb, i + 1);
	mono_mb_emit_managed_call (mb, method, NULL);
	mono_mb_emit_byte (mb, CEE_RET);
}

static void
emit_thunk_invoke_wrapper_ilgen (MonoMethodBuilder *mb, MonoMethod *method, MonoMethodSignature *csig)
{
	MonoImage *image = get_method_image (method);
	MonoMethodSignature *sig = mono_method_signature_internal (method);
	int param_count = sig->param_count + sig->hasthis + 1;
	int pos_leave, coop_gc_var = 0;
	MonoExceptionClause *clause;
	MonoType *object_type = mono_get_object_type ();
#if defined (TARGET_WASM)
	const gboolean do_blocking_transition = FALSE;
#else
	const gboolean do_blocking_transition = TRUE;
#endif

	/* local 0 (temp for exception object) */
	mono_mb_add_local (mb, object_type);

	/* local 1 (temp for result) */
	if (!MONO_TYPE_IS_VOID (sig->ret))
		mono_mb_add_local (mb, sig->ret);

	if (do_blocking_transition) {
		/* local 4, the local to be used when calling the suspend funcs */
		coop_gc_var = mono_mb_add_local (mb, mono_get_int_type ());
	}

	/* clear exception arg */
	mono_mb_emit_ldarg (mb, param_count - 1);
	mono_mb_emit_byte (mb, CEE_LDNULL);
	mono_mb_emit_byte (mb, CEE_STIND_REF);

	if (do_blocking_transition) {
		mono_mb_emit_byte (mb, MONO_CUSTOM_PREFIX);
		mono_mb_emit_byte (mb, CEE_MONO_GET_SP);
		mono_mb_emit_icall (mb, mono_threads_enter_gc_unsafe_region_unbalanced);
		mono_mb_emit_stloc (mb, coop_gc_var);
	}

	/* try */
	clause = (MonoExceptionClause *)mono_image_alloc0 (image, sizeof (MonoExceptionClause));
	clause->try_offset = mono_mb_get_label (mb);

	/* push method's args */
	for (int i = 0; i < param_count - 1; i++) {
		MonoType *type;
		MonoClass *klass;

		mono_mb_emit_ldarg (mb, i);

		/* get the byval type of the param */
		klass = mono_class_from_mono_type_internal (csig->params [i]);
		type = m_class_get_byval_arg (klass);

		/* unbox struct args */
		if (MONO_TYPE_ISSTRUCT (type)) {
			mono_mb_emit_op (mb, CEE_UNBOX, klass);

			/* byref args & and the "this" arg must remain a ptr.
			   Otherwise make a copy of the value type */
			if (!(m_type_is_byref (csig->params [i]) || (i == 0 && sig->hasthis)))
				mono_mb_emit_op (mb, CEE_LDOBJ, klass);

			csig->params [i] = object_type;
		}
	}

	/* call */
	if (method->flags & METHOD_ATTRIBUTE_VIRTUAL)
		mono_mb_emit_op (mb, CEE_CALLVIRT, method);
	else
		mono_mb_emit_op (mb, CEE_CALL, method);

	/* save result at local 1 */
	if (!MONO_TYPE_IS_VOID (sig->ret))
		mono_mb_emit_stloc (mb, 1);

	pos_leave = mono_mb_emit_branch (mb, CEE_LEAVE);

	/* catch */
	clause->flags = MONO_EXCEPTION_CLAUSE_NONE;
	clause->try_len = mono_mb_get_pos (mb) - clause->try_offset;
	clause->data.catch_class = mono_defaults.object_class;

	clause->handler_offset = mono_mb_get_label (mb);

	/* store exception at local 0 */
	mono_mb_emit_stloc (mb, 0);
	mono_mb_emit_ldarg (mb, param_count - 1);
	mono_mb_emit_ldloc (mb, 0);
	mono_mb_emit_byte (mb, CEE_STIND_REF);
	mono_mb_emit_branch (mb, CEE_LEAVE);

	clause->handler_len = mono_mb_get_pos (mb) - clause->handler_offset;

	mono_mb_set_clauses (mb, 1, clause);

	mono_mb_patch_branch (mb, pos_leave);
	/* end-try */

	if (!MONO_TYPE_IS_VOID (sig->ret)) {
		mono_mb_emit_ldloc (mb, 1);

		/* box the return value */
		if (MONO_TYPE_ISSTRUCT (sig->ret))
			mono_mb_emit_op (mb, CEE_BOX, mono_class_from_mono_type_internal (sig->ret));
	}

	if (do_blocking_transition) {
		mono_mb_emit_ldloc (mb, coop_gc_var);
		mono_mb_emit_byte (mb, MONO_CUSTOM_PREFIX);
		mono_mb_emit_byte (mb, CEE_MONO_GET_SP);
		mono_mb_emit_icall (mb, mono_threads_exit_gc_unsafe_region_unbalanced);
	}

	mono_mb_emit_byte (mb, CEE_RET);
}

static void
emit_marshal_custom_get_instance (MonoMethodBuilder *mb, MonoClass *klass, MonoMarshalSpec *spec)
{
	MONO_STATIC_POINTER_INIT (MonoMethod, get_instance)

		MonoClass *Marshal = mono_class_try_get_marshal_class ();
		g_assert (Marshal);
		get_instance = get_method_nofail (Marshal, "GetCustomMarshalerInstance", 2, 0);
		g_assert (get_instance);

	MONO_STATIC_POINTER_INIT_END (MonoClass, get_instance)

	// HACK: We cannot use ldtoken in this type of wrapper.
	mono_mb_emit_byte (mb, MONO_CUSTOM_PREFIX);
	mono_mb_emit_op (mb, CEE_MONO_CLASSCONST, klass);
	mono_mb_emit_icall (mb, mono_marshal_get_type_object);
	mono_mb_emit_ldstr (mb, g_strdup (spec->data.custom_data.cookie));

	mono_mb_emit_op (mb, CEE_CALL, get_instance);
}

static int
emit_marshal_custom_ilgen_throw_exception (MonoMethodBuilder *mb, const char *exc_nspace, const char *exc_name, const char *msg, MarshalAction action)
{
	/* Throw exception and emit compensation code, if neccesary */
	switch (action) {
	case MARSHAL_ACTION_CONV_IN:
	case MARSHAL_ACTION_MANAGED_CONV_IN:
	case MARSHAL_ACTION_CONV_RESULT:
	case MARSHAL_ACTION_MANAGED_CONV_RESULT:
		if ((action == MARSHAL_ACTION_CONV_RESULT) || (action == MARSHAL_ACTION_MANAGED_CONV_RESULT))
			mono_mb_emit_byte (mb, CEE_POP);

		mono_mb_emit_exception_full (mb, exc_nspace, exc_name, msg);

		break;
	case MARSHAL_ACTION_PUSH:
		mono_mb_emit_byte (mb, CEE_LDNULL);
		break;
	default:
		break;
	}

	return 0;
}

static int
emit_marshal_custom_ilgen (EmitMarshalContext *m, int argnum, MonoType *t,
					 MonoMarshalSpec *spec,
					 int conv_arg, MonoType **conv_arg_type,
					 MarshalAction action)
{
	ERROR_DECL (error);
	MonoType *mtype;
	MonoClass *mklass;
	static MonoClass *ICustomMarshaler = NULL;
	static MonoMethod *cleanup_native, *cleanup_managed;
	static MonoMethod *marshal_managed_to_native, *marshal_native_to_managed;
	MonoMethodBuilder *mb = m->mb;
	MonoAssemblyLoadContext *alc = mono_alc_get_ambient ();
	guint32 loc1;
	int pos2;

	MonoType *int_type = mono_get_int_type ();
	MonoType *object_type = mono_get_object_type ();

	if (!ICustomMarshaler) {
		MonoClass *klass = mono_class_try_get_icustom_marshaler_class ();
		if (!klass)
			return emit_marshal_custom_ilgen_throw_exception (mb, "System", "ApplicationException", g_strdup ("Current profile doesn't support ICustomMarshaler"), action);

		cleanup_native = get_method_nofail (klass, "CleanUpNativeData", 1, 0);
		g_assert (cleanup_native);

		cleanup_managed = get_method_nofail (klass, "CleanUpManagedData", 1, 0);
		g_assert (cleanup_managed);

		marshal_managed_to_native = get_method_nofail (klass, "MarshalManagedToNative", 1, 0);
		g_assert (marshal_managed_to_native);

		marshal_native_to_managed = get_method_nofail (klass, "MarshalNativeToManaged", 1, 0);
		g_assert (marshal_native_to_managed);

		mono_memory_barrier ();
		ICustomMarshaler = klass;
	}

	if (spec->data.custom_data.image)
		mtype = mono_reflection_type_from_name_checked (spec->data.custom_data.custom_name, alc, spec->data.custom_data.image, error);
	else
		mtype = mono_reflection_type_from_name_checked (spec->data.custom_data.custom_name, alc, m->image, error);

	if (!mtype)
		return emit_marshal_custom_ilgen_throw_exception (mb, "System", "TypeLoadException", g_strdup ("Failed to load ICustomMarshaler type"), action);

	mklass = mono_class_from_mono_type_internal (mtype);
	g_assert (mklass != NULL);

	switch (action) {
	case MARSHAL_ACTION_CONV_IN:
		switch (t->type) {
		case MONO_TYPE_CLASS:
		case MONO_TYPE_OBJECT:
		case MONO_TYPE_STRING:
		case MONO_TYPE_ARRAY:
		case MONO_TYPE_SZARRAY:
		case MONO_TYPE_VALUETYPE:
			break;

		default:
			g_warning ("custom marshalling of type %x is currently not supported", t->type);
			g_assert_not_reached ();
			break;
		}

		conv_arg = mono_mb_add_local (mb, int_type);

		mono_mb_emit_byte (mb, CEE_LDNULL);
		mono_mb_emit_stloc (mb, conv_arg);

		if (m_type_is_byref (t) && (t->attrs & PARAM_ATTRIBUTE_OUT))
			break;

		/* Minic MS.NET behavior */
		if (!m_type_is_byref (t) && (t->attrs & PARAM_ATTRIBUTE_OUT) && !(t->attrs & PARAM_ATTRIBUTE_IN))
			break;

		/* Check for null */
		mono_mb_emit_ldarg (mb, argnum);
		if (m_type_is_byref (t))
			mono_mb_emit_byte (mb, CEE_LDIND_I);
		pos2 = mono_mb_emit_branch (mb, CEE_BRFALSE);

		emit_marshal_custom_get_instance (mb, mklass, spec);

		mono_mb_emit_ldarg (mb, argnum);
		if (m_type_is_byref (t))
			mono_mb_emit_byte (mb, CEE_LDIND_REF);

		if (t->type == MONO_TYPE_VALUETYPE) {
			/*
			 * Since we can't determine the type of the argument, we
			 * will assume the unmanaged function takes a pointer.
			 */
			*conv_arg_type = int_type;

			mono_mb_emit_op (mb, CEE_BOX, mono_class_from_mono_type_internal (t));
		}

		mono_mb_emit_op (mb, CEE_CALLVIRT, marshal_managed_to_native);
		mono_mb_emit_stloc (mb, conv_arg);

		mono_mb_patch_branch (mb, pos2);
		break;

	case MARSHAL_ACTION_CONV_OUT:
		/* Check for null */
		mono_mb_emit_ldloc (mb, conv_arg);
		pos2 = mono_mb_emit_branch (mb, CEE_BRFALSE);

		if (m_type_is_byref (t) && !(t->attrs & PARAM_ATTRIBUTE_OUT)) {
			mono_mb_emit_ldarg (mb, argnum);

			emit_marshal_custom_get_instance (mb, mklass, spec);
			mono_mb_emit_byte (mb, CEE_DUP);

			mono_mb_emit_ldarg (mb, argnum);
			mono_mb_emit_byte (mb, CEE_LDIND_REF);
			mono_mb_emit_op (mb, CEE_CALLVIRT, cleanup_managed);

			mono_mb_emit_ldloc (mb, conv_arg);
			mono_mb_emit_op (mb, CEE_CALLVIRT, marshal_native_to_managed);
			mono_mb_emit_byte (mb, CEE_STIND_REF);
		} else if (m_type_is_byref (t) && (t->attrs & PARAM_ATTRIBUTE_OUT)) {
			mono_mb_emit_ldarg (mb, argnum);

			emit_marshal_custom_get_instance (mb, mklass, spec);

			mono_mb_emit_ldloc (mb, conv_arg);
			mono_mb_emit_op (mb, CEE_CALLVIRT, marshal_native_to_managed);
			mono_mb_emit_byte (mb, CEE_STIND_REF);
		} else if (t->attrs & PARAM_ATTRIBUTE_OUT) {
			emit_marshal_custom_get_instance (mb, mklass, spec);

			mono_mb_emit_ldloc (mb, conv_arg);
			mono_mb_emit_op (mb, CEE_CALLVIRT, marshal_native_to_managed);
			/* We have nowhere to store the result */
			mono_mb_emit_byte (mb, CEE_POP);
		}

		// Only call cleanup_native if MARSHAL_ACTION_CONV_IN called marshal_managed_to_native.
		if (!(m_type_is_byref (t) && (t->attrs & PARAM_ATTRIBUTE_OUT)) &&
			!(!m_type_is_byref (t) && (t->attrs & PARAM_ATTRIBUTE_OUT) && !(t->attrs & PARAM_ATTRIBUTE_IN))) {
			emit_marshal_custom_get_instance (mb, mklass, spec);

			mono_mb_emit_ldloc (mb, conv_arg);

			mono_mb_emit_op (mb, CEE_CALLVIRT, cleanup_native);
		}

		mono_mb_patch_branch (mb, pos2);
		break;

	case MARSHAL_ACTION_PUSH:
		if (m_type_is_byref (t))
			mono_mb_emit_ldloc_addr (mb, conv_arg);
		else
			mono_mb_emit_ldloc (mb, conv_arg);
		break;

	case MARSHAL_ACTION_CONV_RESULT:
		mono_mb_emit_stloc (mb, 3);

		/* Check for null */
		mono_mb_emit_ldloc (mb, 3);
		pos2 = mono_mb_emit_branch (mb, CEE_BRFALSE);

		emit_marshal_custom_get_instance (mb, mklass, spec);

		mono_mb_emit_ldloc (mb, 3);
		mono_mb_emit_op (mb, CEE_CALLVIRT, marshal_native_to_managed);
		mono_mb_emit_stloc (mb, 3);

		mono_mb_patch_branch (mb, pos2);
		break;

	case MARSHAL_ACTION_MANAGED_CONV_IN:
		switch (t->type) {
		case MONO_TYPE_CLASS:
		case MONO_TYPE_OBJECT:
		case MONO_TYPE_STRING:
		case MONO_TYPE_ARRAY:
		case MONO_TYPE_SZARRAY:
		case MONO_TYPE_VALUETYPE:
		case MONO_TYPE_BOOLEAN:
			break;

		default:
			g_warning ("custom marshalling of type %x is currently not supported", t->type);
			g_assert_not_reached ();
			break;
		}

		conv_arg = mono_mb_add_local (mb, object_type);

		mono_mb_emit_byte (mb, CEE_LDNULL);
		mono_mb_emit_stloc (mb, conv_arg);

		if (m_type_is_byref (t) && t->attrs & PARAM_ATTRIBUTE_OUT)
			break;

		/* Check for null */
		mono_mb_emit_ldarg (mb, argnum);
		if (m_type_is_byref (t))
			mono_mb_emit_byte (mb, CEE_LDIND_I);
		pos2 = mono_mb_emit_branch (mb, CEE_BRFALSE);

		emit_marshal_custom_get_instance (mb, mklass, spec);

		mono_mb_emit_ldarg (mb, argnum);
		if (m_type_is_byref (t))
			mono_mb_emit_byte (mb, CEE_LDIND_I);

		mono_mb_emit_op (mb, CEE_CALLVIRT, marshal_native_to_managed);
		mono_mb_emit_stloc (mb, conv_arg);

		mono_mb_patch_branch (mb, pos2);
		break;

	case MARSHAL_ACTION_MANAGED_CONV_RESULT:
		g_assert (!m_type_is_byref (t));

		loc1 = mono_mb_add_local (mb, object_type);

		mono_mb_emit_stloc (mb, 3);

		mono_mb_emit_ldloc (mb, 3);
		mono_mb_emit_stloc (mb, loc1);

		/* Check for null */
		mono_mb_emit_ldloc (mb, 3);
		pos2 = mono_mb_emit_branch (mb, CEE_BRFALSE);

		emit_marshal_custom_get_instance (mb, mklass, spec);
		mono_mb_emit_byte (mb, CEE_DUP);

		mono_mb_emit_ldloc (mb, 3);
		mono_mb_emit_op (mb, CEE_CALLVIRT, marshal_managed_to_native);
		mono_mb_emit_stloc (mb, 3);

		mono_mb_emit_ldloc (mb, loc1);
		mono_mb_emit_op (mb, CEE_CALLVIRT, cleanup_managed);

		mono_mb_patch_branch (mb, pos2);
		break;

	case MARSHAL_ACTION_MANAGED_CONV_OUT:

		/* Check for null */
		mono_mb_emit_ldloc (mb, conv_arg);
		pos2 = mono_mb_emit_branch (mb, CEE_BRFALSE);

		if (m_type_is_byref (t)) {
			mono_mb_emit_ldarg (mb, argnum);

			emit_marshal_custom_get_instance (mb, mklass, spec);

			mono_mb_emit_ldloc (mb, conv_arg);
			mono_mb_emit_op (mb, CEE_CALLVIRT, marshal_managed_to_native);
			mono_mb_emit_byte (mb, CEE_STIND_I);
		}

		// Only call cleanup_managed if MARSHAL_ACTION_MANAGED_CONV_IN called marshal_native_to_managed.
		if (!(m_type_is_byref (t) && (t->attrs & PARAM_ATTRIBUTE_OUT))) {
			emit_marshal_custom_get_instance (mb, mklass, spec);
			mono_mb_emit_ldloc (mb, conv_arg);
			mono_mb_emit_op (mb, CEE_CALLVIRT, cleanup_managed);
		}

		mono_mb_patch_branch (mb, pos2);
		break;

	default:
		g_assert_not_reached ();
	}
	return conv_arg;
}

static int
emit_marshal_asany_ilgen (EmitMarshalContext *m, int argnum, MonoType *t,
					MonoMarshalSpec *spec,
					int conv_arg, MonoType **conv_arg_type,
					MarshalAction action)
{
	MonoMethodBuilder *mb = m->mb;

	MonoType *int_type = mono_get_int_type ();
	switch (action) {
	case MARSHAL_ACTION_CONV_IN: {
		MonoMarshalNative encoding = mono_marshal_get_string_encoding (m->piinfo, NULL);

		g_assert (t->type == MONO_TYPE_OBJECT);
		g_assert (!m_type_is_byref (t));

		conv_arg = mono_mb_add_local (mb, int_type);
		mono_mb_emit_ldarg (mb, argnum);
		mono_mb_emit_icon (mb, encoding);
		mono_mb_emit_icon (mb, t->attrs);
		mono_mb_emit_icall (mb, mono_marshal_asany);
		mono_mb_emit_stloc (mb, conv_arg);
		break;
	}

	case MARSHAL_ACTION_PUSH:
		mono_mb_emit_ldloc (mb, conv_arg);
		break;

	case MARSHAL_ACTION_CONV_OUT: {
		MonoMarshalNative encoding = mono_marshal_get_string_encoding (m->piinfo, NULL);

		mono_mb_emit_ldarg (mb, argnum);
		mono_mb_emit_ldloc (mb, conv_arg);
		mono_mb_emit_icon (mb, encoding);
		mono_mb_emit_icon (mb, t->attrs);
		mono_mb_emit_icall (mb, mono_marshal_free_asany);
		break;
	}

	default:
		g_assert_not_reached ();
	}
	return conv_arg;
}

static int
emit_marshal_vtype_ilgen (EmitMarshalContext *m, int argnum, MonoType *t,
					MonoMarshalSpec *spec,
					int conv_arg, MonoType **conv_arg_type,
					MarshalAction action)
{
	MonoMethodBuilder *mb = m->mb;
	MonoClass *klass, *date_time_class;
	int pos = 0, pos2;

	klass = mono_class_from_mono_type_internal (t);

	date_time_class = mono_class_get_date_time_class ();

	MonoType *int_type = mono_get_int_type ();
	MonoType *double_type = m_class_get_byval_arg (mono_defaults.double_class);

	switch (action) {
	case MARSHAL_ACTION_CONV_IN:
		if (klass == date_time_class) {
			/* Convert it to an OLE DATE type */

			conv_arg = mono_mb_add_local (mb, double_type);

			if (m_type_is_byref (t)) {
				mono_mb_emit_ldarg (mb, argnum);
				pos = mono_mb_emit_branch (mb, CEE_BRFALSE);
			}

			if (!(m_type_is_byref (t) && !(t->attrs & PARAM_ATTRIBUTE_IN) && (t->attrs & PARAM_ATTRIBUTE_OUT))) {
				if (!m_type_is_byref (t))
					m->csig->params [argnum - m->csig->hasthis] = double_type;

				MONO_STATIC_POINTER_INIT (MonoMethod, to_oadate)
					to_oadate = get_method_nofail (date_time_class, "ToOADate", 0, 0);
					g_assert (to_oadate);
				MONO_STATIC_POINTER_INIT_END (MonoMethod, to_oadate)

				mono_mb_emit_ldarg_addr (mb, argnum);
				mono_mb_emit_managed_call (mb, to_oadate, NULL);
				mono_mb_emit_stloc (mb, conv_arg);
			}

			if (m_type_is_byref (t))
				mono_mb_patch_branch (mb, pos);
			break;
		}

		if (mono_class_is_explicit_layout (klass) || m_class_is_blittable (klass) || m_class_is_enumtype (klass))
			break;

		conv_arg = mono_mb_add_local (mb, int_type);

		/* store the address of the source into local variable 0 */
		if (m_type_is_byref (t))
			mono_mb_emit_ldarg (mb, argnum);
		else
			mono_mb_emit_ldarg_addr (mb, argnum);

		mono_mb_emit_stloc (mb, 0);

		/* allocate space for the native struct and
		 * store the address into local variable 1 (dest) */
		mono_mb_emit_icon (mb, mono_class_native_size (klass, NULL));
		mono_mb_emit_byte (mb, CEE_PREFIX1);
		mono_mb_emit_byte (mb, CEE_LOCALLOC);
		mono_mb_emit_stloc (mb, conv_arg);

		if (m_type_is_byref (t)) {
			mono_mb_emit_ldloc (mb, 0);
			pos = mono_mb_emit_branch (mb, CEE_BRFALSE);
		}

		if (!(m_type_is_byref (t) && !(t->attrs & PARAM_ATTRIBUTE_IN) && (t->attrs & PARAM_ATTRIBUTE_OUT))) {
			/* set dst_ptr */
			mono_mb_emit_ldloc (mb, conv_arg);
			mono_mb_emit_stloc (mb, 1);

			/* emit valuetype conversion code */
			emit_struct_conv (mb, klass, FALSE);
		}

		if (m_type_is_byref (t))
			mono_mb_patch_branch (mb, pos);
		break;

	case MARSHAL_ACTION_PUSH:
		if (spec && spec->native == MONO_NATIVE_LPSTRUCT) {
			/* FIXME: */
			g_assert (!m_type_is_byref (t));

			/* Have to change the signature since the vtype is passed byref */
			m->csig->params [argnum - m->csig->hasthis] = int_type;

			if (mono_class_is_explicit_layout (klass) || m_class_is_blittable (klass) || m_class_is_enumtype (klass))
				mono_mb_emit_ldarg_addr (mb, argnum);
			else
				mono_mb_emit_ldloc (mb, conv_arg);
			break;
		}

		if (klass == date_time_class) {
			if (m_type_is_byref (t))
				mono_mb_emit_ldloc_addr (mb, conv_arg);
			else
				mono_mb_emit_ldloc (mb, conv_arg);
			break;
		}

		if (mono_class_is_explicit_layout (klass) || m_class_is_blittable (klass) || m_class_is_enumtype (klass)) {
			mono_mb_emit_ldarg (mb, argnum);
			break;
		}
		mono_mb_emit_ldloc (mb, conv_arg);
		if (!m_type_is_byref (t)) {
			mono_mb_emit_byte (mb, MONO_CUSTOM_PREFIX);
			mono_mb_emit_op (mb, CEE_MONO_LDNATIVEOBJ, klass);
		}
		break;

	case MARSHAL_ACTION_CONV_OUT:
		if (klass == date_time_class) {
			/* Convert from an OLE DATE type */

			if (!m_type_is_byref (t))
				break;

			if (!((t->attrs & PARAM_ATTRIBUTE_IN) && !(t->attrs & PARAM_ATTRIBUTE_OUT))) {

				MONO_STATIC_POINTER_INIT (MonoMethod, from_oadate)
					from_oadate = get_method_nofail (date_time_class, "FromOADate", 1, 0);
				MONO_STATIC_POINTER_INIT_END (MonoMethod, from_oadate)

				g_assert (from_oadate);

				mono_mb_emit_ldarg (mb, argnum);
				mono_mb_emit_ldloc (mb, conv_arg);
				mono_mb_emit_managed_call (mb, from_oadate, NULL);
				mono_mb_emit_op (mb, CEE_STOBJ, date_time_class);
			}
			break;
		}

		if (mono_class_is_explicit_layout (klass) || m_class_is_blittable (klass) || m_class_is_enumtype (klass))
			break;

		if (m_type_is_byref (t)) {
			/* dst = argument */
			mono_mb_emit_ldarg (mb, argnum);
			mono_mb_emit_stloc (mb, 1);

			mono_mb_emit_ldloc (mb, 1);
			pos = mono_mb_emit_branch (mb, CEE_BRFALSE);

			if (!((t->attrs & PARAM_ATTRIBUTE_IN) && !(t->attrs & PARAM_ATTRIBUTE_OUT))) {
				/* src = tmp_locals [i] */
				mono_mb_emit_ldloc (mb, conv_arg);
				mono_mb_emit_stloc (mb, 0);

				/* emit valuetype conversion code */
				emit_struct_conv (mb, klass, TRUE);
			}
		}

		emit_struct_free (mb, klass, conv_arg);

		if (m_type_is_byref (t))
			mono_mb_patch_branch (mb, pos);
		break;

	case MARSHAL_ACTION_CONV_RESULT:
		if (mono_class_is_explicit_layout (klass) || m_class_is_blittable (klass)) {
			mono_mb_emit_stloc (mb, 3);
			break;
		}

		/* load pointer to returned value type */
		g_assert (m->vtaddr_var);
		mono_mb_emit_ldloc (mb, m->vtaddr_var);
		/* store the address of the source into local variable 0 */
		mono_mb_emit_stloc (mb, 0);
		/* set dst_ptr */
		mono_mb_emit_ldloc_addr (mb, 3);
		mono_mb_emit_stloc (mb, 1);

		/* emit valuetype conversion code */
		emit_struct_conv (mb, klass, TRUE);
		break;

	case MARSHAL_ACTION_MANAGED_CONV_IN:
		if (mono_class_is_explicit_layout (klass) || m_class_is_blittable (klass) || m_class_is_enumtype (klass)) {
			conv_arg = 0;
			break;
		}

		conv_arg = mono_mb_add_local (mb, m_class_get_byval_arg (klass));

		if (t->attrs & PARAM_ATTRIBUTE_OUT)
			break;

		if (m_type_is_byref (t))
			mono_mb_emit_ldarg (mb, argnum);
		else
			mono_mb_emit_ldarg_addr (mb, argnum);
		mono_mb_emit_stloc (mb, 0);

		if (m_type_is_byref (t)) {
			mono_mb_emit_ldloc (mb, 0);
			pos = mono_mb_emit_branch (mb, CEE_BRFALSE);
		}

		mono_mb_emit_ldloc_addr (mb, conv_arg);
		mono_mb_emit_stloc (mb, 1);

		/* emit valuetype conversion code */
		emit_struct_conv (mb, klass, TRUE);

		if (m_type_is_byref (t))
			mono_mb_patch_branch (mb, pos);
		break;

	case MARSHAL_ACTION_MANAGED_CONV_OUT:
		if (mono_class_is_explicit_layout (klass) || m_class_is_blittable (klass) || m_class_is_enumtype (klass))
			break;
		if (m_type_is_byref (t) && (t->attrs & PARAM_ATTRIBUTE_IN) && !(t->attrs & PARAM_ATTRIBUTE_OUT))
			break;

		/* Check for null */
		mono_mb_emit_ldarg (mb, argnum);
		pos2 = mono_mb_emit_branch (mb, CEE_BRFALSE);

		/* Set src */
		mono_mb_emit_ldloc_addr (mb, conv_arg);
		mono_mb_emit_stloc (mb, 0);

		/* Set dest */
		mono_mb_emit_ldarg (mb, argnum);
		mono_mb_emit_stloc (mb, 1);

		/* emit valuetype conversion code */
		emit_struct_conv (mb, klass, FALSE);

		mono_mb_patch_branch (mb, pos2);
		break;

	case MARSHAL_ACTION_MANAGED_CONV_RESULT:
		if (mono_class_is_explicit_layout (klass) || m_class_is_blittable (klass) || m_class_is_enumtype (klass)) {
			mono_mb_emit_stloc (mb, 3);
			m->retobj_var = 0;
			break;
		}

		/* load pointer to returned value type */
		g_assert (m->vtaddr_var);
		mono_mb_emit_ldloc (mb, m->vtaddr_var);

		/* store the address of the source into local variable 0 */
		mono_mb_emit_stloc (mb, 0);
		/* allocate space for the native struct and
		 * store the address into dst_ptr */
		m->retobj_var = mono_mb_add_local (mb, int_type);
		m->retobj_class = klass;
		g_assert (m->retobj_var);
		mono_mb_emit_icon (mb, mono_class_native_size (klass, NULL));
		mono_mb_emit_byte (mb, CEE_CONV_I);
		mono_mb_emit_icall (mb, ves_icall_marshal_alloc);
		mono_mb_emit_stloc (mb, 1);
		mono_mb_emit_ldloc (mb, 1);
		mono_mb_emit_stloc (mb, m->retobj_var);

		/* emit valuetype conversion code */
		emit_struct_conv (mb, klass, FALSE);
		break;

	default:
		g_assert_not_reached ();
	}
	return conv_arg;
}

static inline void
emit_string_free_icall (MonoMethodBuilder *mb, MonoMarshalConv conv)
{
	if (conv == MONO_MARSHAL_CONV_BSTR_STR || conv == MONO_MARSHAL_CONV_ANSIBSTR_STR || conv == MONO_MARSHAL_CONV_TBSTR_STR)
		mono_mb_emit_icall (mb, mono_free_bstr);
	else
		mono_mb_emit_icall (mb, mono_marshal_free);
}

static int
emit_marshal_string_ilgen (EmitMarshalContext *m, int argnum, MonoType *t,
					 MonoMarshalSpec *spec,
					 int conv_arg, MonoType **conv_arg_type,
					 MarshalAction action)
{
	MonoMethodBuilder *mb = m->mb;
	MonoMarshalNative encoding = mono_marshal_get_string_encoding (m->piinfo, spec);
	MonoMarshalConv conv = mono_marshal_get_string_to_ptr_conv (m->piinfo, spec);
	gboolean need_free;

	MonoType *int_type = mono_get_int_type ();
	MonoType *object_type = mono_get_object_type ();
	switch (action) {
	case MARSHAL_ACTION_CONV_IN:
		*conv_arg_type = int_type;
		conv_arg = mono_mb_add_local (mb, int_type);

		if (m_type_is_byref (t)) {
			if (t->attrs & PARAM_ATTRIBUTE_OUT)
				break;

			mono_mb_emit_ldarg (mb, argnum);
			mono_mb_emit_byte (mb, CEE_LDIND_I);
		} else {
			mono_mb_emit_ldarg (mb, argnum);
		}

		if (conv == MONO_MARSHAL_CONV_INVALID) {
			char *msg = g_strdup_printf ("string marshalling conversion %d not implemented", encoding);
			mono_mb_emit_exception_marshal_directive (mb, msg);
		} else {
			mono_mb_emit_icall_id (mb, conv_to_icall (conv, NULL));

			mono_mb_emit_stloc (mb, conv_arg);
		}
		break;

	case MARSHAL_ACTION_CONV_OUT:
		conv = mono_marshal_get_ptr_to_string_conv (m->piinfo, spec, &need_free);
		if (conv == MONO_MARSHAL_CONV_INVALID) {
			char *msg = g_strdup_printf ("string marshalling conversion %d not implemented", encoding);
			mono_mb_emit_exception_marshal_directive (mb, msg);
			break;
		}

		if (encoding == MONO_NATIVE_VBBYREFSTR) {

			if (!m_type_is_byref (t)) {
				char *msg = g_strdup ("VBByRefStr marshalling requires a ref parameter.");
				mono_mb_emit_exception_marshal_directive (mb, msg);
				break;
			}

			MONO_STATIC_POINTER_INIT (MonoMethod, m)

				m = get_method_nofail (mono_defaults.string_class, "get_Length", -1, 0);

			MONO_STATIC_POINTER_INIT_END (MonoMethod, m)

			/*
			 * Have to allocate a new string with the same length as the original, and
			 * copy the contents of the buffer pointed to by CONV_ARG into it.
			 */
			g_assert (m_type_is_byref (t));
			mono_mb_emit_ldarg (mb, argnum);
			mono_mb_emit_ldloc (mb, conv_arg);
			mono_mb_emit_ldarg (mb, argnum);
			mono_mb_emit_byte (mb, CEE_LDIND_I);
			mono_mb_emit_managed_call (mb, m, NULL);
			mono_mb_emit_icall (mb, mono_string_new_len_wrapper);
			mono_mb_emit_byte (mb, CEE_STIND_REF);
		} else if (m_type_is_byref (t) && (t->attrs & PARAM_ATTRIBUTE_OUT || !(t->attrs & PARAM_ATTRIBUTE_IN))) {
			int stind_op;
			mono_mb_emit_ldarg (mb, argnum);
			mono_mb_emit_ldloc (mb, conv_arg);
			mono_mb_emit_icall_id (mb, conv_to_icall (conv, &stind_op));
			mono_mb_emit_byte (mb, stind_op);
			need_free = TRUE;
		}

		if (need_free) {
			mono_mb_emit_ldloc (mb, conv_arg);
			emit_string_free_icall (mb, conv);
		}
		break;

	case MARSHAL_ACTION_PUSH:
		if (m_type_is_byref (t) && encoding != MONO_NATIVE_VBBYREFSTR)
			mono_mb_emit_ldloc_addr (mb, conv_arg);
		else
			mono_mb_emit_ldloc (mb, conv_arg);
		break;

	case MARSHAL_ACTION_CONV_RESULT:
		mono_mb_emit_stloc (mb, 0);

		conv = mono_marshal_get_ptr_to_string_conv (m->piinfo, spec, &need_free);
		if (conv == MONO_MARSHAL_CONV_INVALID) {
			char *msg = g_strdup_printf ("string marshalling conversion %d not implemented", encoding);
			mono_mb_emit_exception_marshal_directive (mb, msg);
			break;
		}

		mono_mb_emit_ldloc (mb, 0);
		mono_mb_emit_icall_id (mb, conv_to_icall (conv, NULL));
		mono_mb_emit_stloc (mb, 3);

		/* free the string */
		mono_mb_emit_ldloc (mb, 0);
		emit_string_free_icall (mb, conv);
		break;

	case MARSHAL_ACTION_MANAGED_CONV_IN:
		conv_arg = mono_mb_add_local (mb, object_type);

		*conv_arg_type = int_type;

		if (m_type_is_byref (t)) {
			if (t->attrs & PARAM_ATTRIBUTE_OUT)
				break;
		}

		conv = mono_marshal_get_ptr_to_string_conv (m->piinfo, spec, &need_free);
		if (conv == MONO_MARSHAL_CONV_INVALID) {
			char *msg = g_strdup_printf ("string marshalling conversion %d not implemented", encoding);
			mono_mb_emit_exception_marshal_directive (mb, msg);
			break;
		}

		mono_mb_emit_ldarg (mb, argnum);
		if (m_type_is_byref (t))
			mono_mb_emit_byte (mb, CEE_LDIND_I);
		mono_mb_emit_icall_id (mb, conv_to_icall (conv, NULL));
		mono_mb_emit_stloc (mb, conv_arg);
		break;

	case MARSHAL_ACTION_MANAGED_CONV_OUT:
		if (m_type_is_byref (t)) {
			if (conv_arg) {
				int stind_op;
				mono_mb_emit_ldarg (mb, argnum);
				mono_mb_emit_ldloc (mb, conv_arg);
				mono_mb_emit_icall_id (mb, conv_to_icall (conv, &stind_op));
				mono_mb_emit_byte (mb, stind_op);
			}
		}
		break;

	case MARSHAL_ACTION_MANAGED_CONV_RESULT:
		if (conv_to_icall (conv, NULL) == MONO_JIT_ICALL_mono_marshal_string_to_utf16)
			/* We need to make a copy so the caller is able to free it */
			mono_mb_emit_icall (mb, mono_marshal_string_to_utf16_copy);
		else
			mono_mb_emit_icall_id (mb, conv_to_icall (conv, NULL));
		mono_mb_emit_stloc (mb, 3);
		break;

	default:
		g_assert_not_reached ();
	}
	return conv_arg;
}


static int
emit_marshal_safehandle_ilgen (EmitMarshalContext *m, int argnum, MonoType *t,
			 MonoMarshalSpec *spec, int conv_arg,
			 MonoType **conv_arg_type, MarshalAction action)
{
	MonoMethodBuilder *mb = m->mb;
	MonoType *int_type = mono_get_int_type ();
	MonoType *boolean_type = m_class_get_byval_arg (mono_defaults.boolean_class);

	switch (action){
	case MARSHAL_ACTION_CONV_IN: {
		int dar_release_slot, pos;

		conv_arg = mono_mb_add_local (mb, int_type);
		*conv_arg_type = int_type;

		if (!sh_dangerous_add_ref)
			init_safe_handle ();

		mono_mb_emit_ldarg (mb, argnum);
		pos = mono_mb_emit_branch (mb, CEE_BRTRUE);
		mono_mb_emit_exception (mb, "ArgumentNullException", NULL);

		mono_mb_patch_branch (mb, pos);

		/* Create local to hold the ref parameter to DangerousAddRef */
		dar_release_slot = mono_mb_add_local (mb, boolean_type);

		/* set release = false; */
		mono_mb_emit_icon (mb, 0);
		mono_mb_emit_stloc (mb, dar_release_slot);

		if (m_type_is_byref (t)) {
			int old_handle_value_slot = mono_mb_add_local (mb, int_type);

			if (!is_in (t)) {
				mono_mb_emit_icon (mb, 0);
				mono_mb_emit_stloc (mb, conv_arg);
			} else {
				/* safehandle.DangerousAddRef (ref release) */
				mono_mb_emit_ldarg (mb, argnum);
				mono_mb_emit_byte (mb, CEE_LDIND_REF);
				mono_mb_emit_ldloc_addr (mb, dar_release_slot);
				mono_mb_emit_managed_call (mb, sh_dangerous_add_ref, NULL);

				/* Pull the handle field from SafeHandle */
				mono_mb_emit_ldarg (mb, argnum);
				mono_mb_emit_byte (mb, CEE_LDIND_REF);
				mono_mb_emit_ldflda (mb, MONO_STRUCT_OFFSET (MonoSafeHandle, handle));
				mono_mb_emit_byte (mb, CEE_LDIND_I);
				mono_mb_emit_byte (mb, CEE_DUP);
				mono_mb_emit_stloc (mb, conv_arg);
				mono_mb_emit_stloc (mb, old_handle_value_slot);
			}
		} else {
			/* safehandle.DangerousAddRef (ref release) */
			mono_mb_emit_ldarg (mb, argnum);
			mono_mb_emit_ldloc_addr (mb, dar_release_slot);
			mono_mb_emit_managed_call (mb, sh_dangerous_add_ref, NULL);

			/* Pull the handle field from SafeHandle */
			mono_mb_emit_ldarg (mb, argnum);
			mono_mb_emit_ldflda (mb, MONO_STRUCT_OFFSET (MonoSafeHandle, handle));
			mono_mb_emit_byte (mb, CEE_LDIND_I);
			mono_mb_emit_stloc (mb, conv_arg);
		}

		break;
	}

	case MARSHAL_ACTION_PUSH:
		if (m_type_is_byref (t))
			mono_mb_emit_ldloc_addr (mb, conv_arg);
		else
			mono_mb_emit_ldloc (mb, conv_arg);
		break;

	case MARSHAL_ACTION_CONV_OUT: {
		/* The slot for the boolean is the next temporary created after conv_arg, see the CONV_IN code */
		int dar_release_slot = conv_arg + 1;
		int label_next = 0;

		if (!sh_dangerous_release)
			init_safe_handle ();

		if (m_type_is_byref (t)) {
			/* If there was SafeHandle on input we have to release the reference to it */
			if (is_in (t)) {
				mono_mb_emit_ldloc (mb, dar_release_slot);
				label_next = mono_mb_emit_branch (mb, CEE_BRFALSE);
				mono_mb_emit_ldarg (mb, argnum);
				mono_mb_emit_byte (mb, CEE_LDIND_I);
				mono_mb_emit_managed_call (mb, sh_dangerous_release, NULL);
				mono_mb_patch_branch (mb, label_next);
			}

			if (is_out (t)) {
				ERROR_DECL (local_error);
				MonoMethod *ctor;

				/*
				 * If the SafeHandle was marshalled on input we can skip the marshalling on
				 * output if the handle value is identical.
				 */
				if (is_in (t)) {
					int old_handle_value_slot = dar_release_slot + 1;
					mono_mb_emit_ldloc (mb, old_handle_value_slot);
					mono_mb_emit_ldloc (mb, conv_arg);
					label_next = mono_mb_emit_branch (mb, CEE_BEQ);
				}

				/*
				 * Create an empty SafeHandle (of correct derived type).
				 *
				 * FIXME: If an out-of-memory situation or exception happens here we will
				 * leak the handle. We should move the allocation of the SafeHandle to the
				 * input marshalling code to prevent that.
				 */
				ctor = mono_class_get_method_from_name_checked (t->data.klass, ".ctor", 0, 0, local_error);
				if (ctor == NULL || !is_ok (local_error)){
					mono_mb_emit_exception (mb, "MissingMethodException", "parameterless constructor required");
					mono_error_cleanup (local_error);
					break;
				}

				/* refval = new SafeHandleDerived ()*/
				mono_mb_emit_ldarg (mb, argnum);
				mono_mb_emit_op (mb, CEE_NEWOBJ, ctor);
				mono_mb_emit_byte (mb, CEE_STIND_REF);

				/* refval.handle = returned_handle */
				mono_mb_emit_ldarg (mb, argnum);
				mono_mb_emit_byte (mb, CEE_LDIND_REF);
				mono_mb_emit_ldflda (mb, MONO_STRUCT_OFFSET (MonoSafeHandle, handle));
				mono_mb_emit_ldloc (mb, conv_arg);
				mono_mb_emit_byte (mb, CEE_STIND_I);

				if (is_in (t) && label_next) {
					mono_mb_patch_branch (mb, label_next);
				}
			}
		} else {
			mono_mb_emit_ldloc (mb, dar_release_slot);
			label_next = mono_mb_emit_branch (mb, CEE_BRFALSE);
			mono_mb_emit_ldarg (mb, argnum);
			mono_mb_emit_managed_call (mb, sh_dangerous_release, NULL);
			mono_mb_patch_branch (mb, label_next);
		}
		break;
	}

	case MARSHAL_ACTION_CONV_RESULT: {
		ERROR_DECL (error);
		MonoMethod *ctor = NULL;
		int intptr_handle_slot;

		if (mono_class_is_abstract (t->data.klass)) {
			mono_mb_emit_byte (mb, CEE_POP);
			mono_mb_emit_exception_marshal_directive (mb, g_strdup ("Returned SafeHandles should not be abstract"));
			break;
		}

		ctor = mono_class_get_method_from_name_checked (t->data.klass, ".ctor", 0, 0, error);
		if (ctor == NULL || !is_ok (error)){
			mono_error_cleanup (error);
			mono_mb_emit_byte (mb, CEE_POP);
			mono_mb_emit_exception (mb, "MissingMethodException", "parameterless constructor required");
			break;
		}
		/* Store the IntPtr results into a local */
		intptr_handle_slot = mono_mb_add_local (mb, int_type);
		mono_mb_emit_stloc (mb, intptr_handle_slot);

		/* Create return value */
		mono_mb_emit_op (mb, CEE_NEWOBJ, ctor);
		mono_mb_emit_stloc (mb, 3);

		/* Set the return.handle to the value, am using ldflda, not sure if thats a good idea */
		mono_mb_emit_ldloc (mb, 3);
		mono_mb_emit_ldflda (mb, MONO_STRUCT_OFFSET (MonoSafeHandle, handle));
		mono_mb_emit_ldloc (mb, intptr_handle_slot);
		mono_mb_emit_byte (mb, CEE_STIND_I);
		break;
	}

	case MARSHAL_ACTION_MANAGED_CONV_IN:
		fprintf (stderr, "mono/marshal: SafeHandles missing MANAGED_CONV_IN\n");
		break;

	case MARSHAL_ACTION_MANAGED_CONV_OUT:
		fprintf (stderr, "mono/marshal: SafeHandles missing MANAGED_CONV_OUT\n");
		break;

	case MARSHAL_ACTION_MANAGED_CONV_RESULT:
		fprintf (stderr, "mono/marshal: SafeHandles missing MANAGED_CONV_RESULT\n");
		break;
	default:
		printf ("Unhandled case for MarshalAction: %d\n", action);
	}
	return conv_arg;
}


static int
emit_marshal_handleref_ilgen (EmitMarshalContext *m, int argnum, MonoType *t,
			MonoMarshalSpec *spec, int conv_arg,
			MonoType **conv_arg_type, MarshalAction action)
{
	MonoMethodBuilder *mb = m->mb;

	MonoType *int_type = mono_get_int_type ();
	switch (action){
	case MARSHAL_ACTION_CONV_IN: {
		conv_arg = mono_mb_add_local (mb, int_type);
		*conv_arg_type = int_type;

		if (m_type_is_byref (t)) {
			char *msg = g_strdup ("HandleRefs can not be returned from unmanaged code (or passed by ref)");
			mono_mb_emit_exception_marshal_directive (mb, msg);
			break;
		}
		mono_mb_emit_ldarg_addr (mb, argnum);
		mono_mb_emit_icon (mb, MONO_STRUCT_OFFSET (MonoHandleRef, handle));
		mono_mb_emit_byte (mb, CEE_ADD);
		mono_mb_emit_byte (mb, CEE_LDIND_I);
		mono_mb_emit_stloc (mb, conv_arg);
		break;
	}

	case MARSHAL_ACTION_PUSH:
		mono_mb_emit_ldloc (mb, conv_arg);
		break;

	case MARSHAL_ACTION_CONV_OUT: {
		/* no resource release required */
		break;
	}

	case MARSHAL_ACTION_CONV_RESULT: {
		char *msg = g_strdup ("HandleRefs can not be returned from unmanaged code (or passed by ref)");
		mono_mb_emit_exception_marshal_directive (mb, msg);
		break;
	}

	case MARSHAL_ACTION_MANAGED_CONV_IN:
		fprintf (stderr, "mono/marshal: SafeHandles missing MANAGED_CONV_IN\n");
		break;

	case MARSHAL_ACTION_MANAGED_CONV_OUT:
		fprintf (stderr, "mono/marshal: SafeHandles missing MANAGED_CONV_OUT\n");
		break;

	case MARSHAL_ACTION_MANAGED_CONV_RESULT:
		fprintf (stderr, "mono/marshal: SafeHandles missing MANAGED_CONV_RESULT\n");
		break;
	default:
		fprintf (stderr, "Unhandled case for MarshalAction: %d\n", action);
	}
	return conv_arg;
}


static int
emit_marshal_object_ilgen (EmitMarshalContext *m, int argnum, MonoType *t,
		     MonoMarshalSpec *spec,
		     int conv_arg, MonoType **conv_arg_type,
		     MarshalAction action)
{
	MonoMethodBuilder *mb = m->mb;
	MonoClass *klass = mono_class_from_mono_type_internal (t);
	int pos, pos2, loc;

	MonoType *int_type = mono_get_int_type ();
	switch (action) {
	case MARSHAL_ACTION_CONV_IN:
		*conv_arg_type = int_type;
		conv_arg = mono_mb_add_local (mb, int_type);

		m->orig_conv_args [argnum] = 0;

		if (mono_class_from_mono_type_internal (t) == mono_defaults.object_class) {
			char *msg = g_strdup_printf ("Marshalling of type object is not implemented");
			mono_mb_emit_exception_marshal_directive (mb, msg);
			break;
		}

		if (m_class_is_delegate (klass)) {
			if (m_type_is_byref (t)) {
				if (!(t->attrs & PARAM_ATTRIBUTE_OUT)) {
					char *msg = g_strdup_printf ("Byref marshalling of delegates is not implemented.");
					mono_mb_emit_exception_marshal_directive (mb, msg);
				}
				mono_mb_emit_byte (mb, CEE_LDNULL);
				mono_mb_emit_stloc (mb, conv_arg);
			} else {
				mono_mb_emit_ldarg (mb, argnum);
				mono_mb_emit_icall_id (mb, conv_to_icall (MONO_MARSHAL_CONV_DEL_FTN, NULL));
				mono_mb_emit_stloc (mb, conv_arg);
			}
		} else if (klass == mono_class_try_get_stringbuilder_class ()) {
			MonoMarshalNative encoding = mono_marshal_get_string_encoding (m->piinfo, spec);
			MonoMarshalConv conv = mono_marshal_get_stringbuilder_to_ptr_conv (m->piinfo, spec);

#if 0
			if (m_type_is_byref (t)) {
				if (!(t->attrs & PARAM_ATTRIBUTE_OUT)) {
					char *msg = g_strdup_printf ("Byref marshalling of stringbuilders is not implemented.");
					mono_mb_emit_exception_marshal_directive (mb, msg);
				}
				break;
			}
#endif

			if (m_type_is_byref (t) && !(t->attrs & PARAM_ATTRIBUTE_IN) && (t->attrs & PARAM_ATTRIBUTE_OUT))
				break;

			if (conv == MONO_MARSHAL_CONV_INVALID) {
				char *msg = g_strdup_printf ("stringbuilder marshalling conversion %d not implemented", encoding);
				mono_mb_emit_exception_marshal_directive (mb, msg);
				break;
			}

			mono_mb_emit_ldarg (mb, argnum);
			if (m_type_is_byref (t))
				mono_mb_emit_byte (mb, CEE_LDIND_I);

			mono_mb_emit_icall_id (mb, conv_to_icall (conv, NULL));
			mono_mb_emit_stloc (mb, conv_arg);
		} else if (m_class_is_blittable (klass)) {
			mono_mb_emit_byte (mb, CEE_LDNULL);
			mono_mb_emit_stloc (mb, conv_arg);

			mono_mb_emit_ldarg (mb, argnum);
			pos = mono_mb_emit_branch (mb, CEE_BRFALSE);

			mono_mb_emit_ldarg (mb, argnum);
			mono_mb_emit_ldflda (mb, MONO_ABI_SIZEOF (MonoObject));
			mono_mb_emit_stloc (mb, conv_arg);

			mono_mb_patch_branch (mb, pos);
			break;
		} else {
			mono_mb_emit_byte (mb, CEE_LDNULL);
			mono_mb_emit_stloc (mb, conv_arg);

			if (m_type_is_byref (t)) {
				/* we dont need any conversions for out parameters */
				if (t->attrs & PARAM_ATTRIBUTE_OUT)
					break;

				mono_mb_emit_ldarg (mb, argnum);
				mono_mb_emit_byte (mb, CEE_LDIND_I);

			} else {
				mono_mb_emit_ldarg (mb, argnum);
				mono_mb_emit_byte (mb, MONO_CUSTOM_PREFIX);
				mono_mb_emit_byte (mb, CEE_MONO_OBJADDR);
			}

			/* store the address of the source into local variable 0 */
			mono_mb_emit_stloc (mb, 0);
			mono_mb_emit_ldloc (mb, 0);
			pos = mono_mb_emit_branch (mb, CEE_BRFALSE);

			/* allocate space for the native struct and store the address */
			mono_mb_emit_icon (mb, mono_class_native_size (klass, NULL));
			mono_mb_emit_byte (mb, CEE_PREFIX1);
			mono_mb_emit_byte (mb, CEE_LOCALLOC);
			mono_mb_emit_stloc (mb, conv_arg);

			if (m_type_is_byref (t)) {
				/* Need to store the original buffer so we can free it later */
				m->orig_conv_args [argnum] = mono_mb_add_local (mb, int_type);
				mono_mb_emit_ldloc (mb, conv_arg);
				mono_mb_emit_stloc (mb, m->orig_conv_args [argnum]);
			}

			/* set the src_ptr */
			mono_mb_emit_ldloc (mb, 0);
			mono_mb_emit_ldflda (mb, MONO_ABI_SIZEOF (MonoObject));
			mono_mb_emit_stloc (mb, 0);

			/* set dst_ptr */
			mono_mb_emit_ldloc (mb, conv_arg);
			mono_mb_emit_stloc (mb, 1);

			/* emit valuetype conversion code */
			emit_struct_conv (mb, klass, FALSE);

			mono_mb_patch_branch (mb, pos);
		}
		break;

	case MARSHAL_ACTION_CONV_OUT:
		if (klass == mono_class_try_get_stringbuilder_class ()) {
			gboolean need_free;
			MonoMarshalNative encoding;
			MonoMarshalConv conv;

			encoding = mono_marshal_get_string_encoding (m->piinfo, spec);
			conv = mono_marshal_get_ptr_to_stringbuilder_conv (m->piinfo, spec, &need_free);

			g_assert (encoding != -1);

			if (m_type_is_byref (t)) {
				//g_assert (!(t->attrs & PARAM_ATTRIBUTE_OUT));

				need_free = TRUE;

				mono_mb_emit_ldarg (mb, argnum);
				mono_mb_emit_ldloc (mb, conv_arg);

				switch (encoding) {
				case MONO_NATIVE_LPWSTR:
					mono_mb_emit_icall (mb, mono_string_utf16_to_builder2);
					break;
				case MONO_NATIVE_LPSTR:
					mono_mb_emit_icall (mb, mono_string_utf8_to_builder2);
					break;
				case MONO_NATIVE_UTF8STR:
					mono_mb_emit_icall (mb, mono_string_utf8_to_builder2);
					break;
				default:
					g_assert_not_reached ();
				}

				mono_mb_emit_byte (mb, CEE_STIND_REF);
			} else if (t->attrs & PARAM_ATTRIBUTE_OUT || !(t->attrs & PARAM_ATTRIBUTE_IN)) {
				mono_mb_emit_ldarg (mb, argnum);
				mono_mb_emit_ldloc (mb, conv_arg);

				mono_mb_emit_icall_id (mb, conv_to_icall (conv, NULL));
			}

			if (need_free) {
				mono_mb_emit_ldloc (mb, conv_arg);
				mono_mb_emit_icall (mb, mono_marshal_free);
			}
			break;
		}

		if (m_class_is_delegate (klass)) {
			if (m_type_is_byref (t)) {
				mono_mb_emit_ldarg (mb, argnum);
				mono_mb_emit_byte (mb, MONO_CUSTOM_PREFIX);
				mono_mb_emit_op (mb, CEE_MONO_CLASSCONST, klass);
				mono_mb_emit_ldloc (mb, conv_arg);
				mono_mb_emit_icall_id (mb, conv_to_icall (MONO_MARSHAL_CONV_FTN_DEL, NULL));
				mono_mb_emit_byte (mb, CEE_STIND_REF);
			}
			break;
		}

		if (m_type_is_byref (t) && (t->attrs & PARAM_ATTRIBUTE_OUT)) {
			/* allocate a new object */
			mono_mb_emit_ldarg (mb, argnum);
			mono_mb_emit_byte (mb, MONO_CUSTOM_PREFIX);
			mono_mb_emit_op (mb, CEE_MONO_NEWOBJ, klass);
			mono_mb_emit_byte (mb, CEE_STIND_REF);
		}

		/* dst = *argument */
		mono_mb_emit_ldarg (mb, argnum);

		if (m_type_is_byref (t))
			mono_mb_emit_byte (mb, CEE_LDIND_I);

		mono_mb_emit_stloc (mb, 1);

		mono_mb_emit_ldloc (mb, 1);
		pos = mono_mb_emit_branch (mb, CEE_BRFALSE);

		if (m_type_is_byref (t) || (t->attrs & PARAM_ATTRIBUTE_OUT)) {
			mono_mb_emit_ldloc (mb, 1);
			mono_mb_emit_icon (mb, MONO_ABI_SIZEOF (MonoObject));
			mono_mb_emit_byte (mb, CEE_ADD);
			mono_mb_emit_stloc (mb, 1);

			/* src = tmp_locals [i] */
			mono_mb_emit_ldloc (mb, conv_arg);
			mono_mb_emit_stloc (mb, 0);

			/* emit valuetype conversion code */
			emit_struct_conv (mb, klass, TRUE);

			/* Free the structure returned by the native code */
			emit_struct_free (mb, klass, conv_arg);

			if (m->orig_conv_args [argnum]) {
				/*
				 * If the native function changed the pointer, then free
				 * the original structure plus the new pointer.
				 */
				mono_mb_emit_ldloc (mb, m->orig_conv_args [argnum]);
				mono_mb_emit_ldloc (mb, conv_arg);
				pos2 = mono_mb_emit_branch (mb, CEE_BEQ);

				if (!(t->attrs & PARAM_ATTRIBUTE_OUT)) {
					g_assert (m->orig_conv_args [argnum]);

					emit_struct_free (mb, klass, m->orig_conv_args [argnum]);
				}

				mono_mb_emit_ldloc (mb, conv_arg);
				mono_mb_emit_icall (mb, mono_marshal_free);

				mono_mb_patch_branch (mb, pos2);
			}
		}
		else
			/* Free the original structure passed to native code */
			emit_struct_free (mb, klass, conv_arg);

		mono_mb_patch_branch (mb, pos);
		break;

	case MARSHAL_ACTION_PUSH:
		if (m_type_is_byref (t))
			mono_mb_emit_ldloc_addr (mb, conv_arg);
		else
			mono_mb_emit_ldloc (mb, conv_arg);
		break;

	case MARSHAL_ACTION_CONV_RESULT:
		if (m_class_is_delegate (klass)) {
			g_assert (!m_type_is_byref (t));
			mono_mb_emit_stloc (mb, 0);
			mono_mb_emit_byte (mb, MONO_CUSTOM_PREFIX);
			mono_mb_emit_op (mb, CEE_MONO_CLASSCONST, klass);
			mono_mb_emit_ldloc (mb, 0);
			mono_mb_emit_icall_id (mb, conv_to_icall (MONO_MARSHAL_CONV_FTN_DEL, NULL));
			mono_mb_emit_stloc (mb, 3);
		} else if (klass == mono_class_try_get_stringbuilder_class ()) {
			// FIXME:
			char *msg = g_strdup_printf ("Return marshalling of stringbuilders is not implemented.");
			mono_mb_emit_exception_marshal_directive (mb, msg);
		} else {
			/* set src */
			mono_mb_emit_stloc (mb, 0);

			/* Make a copy since emit_conv modifies local 0 */
			loc = mono_mb_add_local (mb, int_type);
			mono_mb_emit_ldloc (mb, 0);
			mono_mb_emit_stloc (mb, loc);

			mono_mb_emit_byte (mb, CEE_LDNULL);
			mono_mb_emit_stloc (mb, 3);

			mono_mb_emit_ldloc (mb, 0);
			pos = mono_mb_emit_branch (mb, CEE_BRFALSE);

			/* allocate result object */

			mono_mb_emit_byte (mb, MONO_CUSTOM_PREFIX);
			mono_mb_emit_op (mb, CEE_MONO_NEWOBJ, klass);
			mono_mb_emit_stloc (mb, 3);

			/* set dst  */

			mono_mb_emit_ldloc (mb, 3);
			mono_mb_emit_ldflda (mb, MONO_ABI_SIZEOF (MonoObject));
			mono_mb_emit_stloc (mb, 1);

			/* emit conversion code */
			emit_struct_conv (mb, klass, TRUE);

			emit_struct_free (mb, klass, loc);

			/* Free the pointer allocated by unmanaged code */
			mono_mb_emit_ldloc (mb, loc);
			mono_mb_emit_icall (mb, mono_marshal_free);
			mono_mb_patch_branch (mb, pos);
		}
		break;

	case MARSHAL_ACTION_MANAGED_CONV_IN:
		conv_arg = mono_mb_add_local (mb, m_class_get_byval_arg (klass));

		if (m_class_is_delegate (klass)) {
			mono_mb_emit_byte (mb, MONO_CUSTOM_PREFIX);
			mono_mb_emit_op (mb, CEE_MONO_CLASSCONST, klass);
			mono_mb_emit_ldarg (mb, argnum);
			if (m_type_is_byref (t))
				mono_mb_emit_byte (mb, CEE_LDIND_I);
			mono_mb_emit_icall_id (mb, conv_to_icall (MONO_MARSHAL_CONV_FTN_DEL, NULL));
			mono_mb_emit_stloc (mb, conv_arg);
			break;
		}

		if (klass == mono_class_try_get_stringbuilder_class ()) {
			MonoMarshalNative encoding;

			encoding = mono_marshal_get_string_encoding (m->piinfo, spec);

			// FIXME:
			g_assert (encoding == MONO_NATIVE_LPSTR || encoding == MONO_NATIVE_UTF8STR);

			g_assert (!m_type_is_byref (t));
			g_assert (encoding != -1);

			mono_mb_emit_ldarg (mb, argnum);
			mono_mb_emit_icall (mb, mono_string_utf8_to_builder2);
			mono_mb_emit_stloc (mb, conv_arg);
			break;
		}

		/* The class can not have an automatic layout */
		if (mono_class_is_auto_layout (klass)) {
			mono_mb_emit_auto_layout_exception (mb, klass);
			break;
		}

		if (t->attrs & PARAM_ATTRIBUTE_OUT) {
			mono_mb_emit_byte (mb, CEE_LDNULL);
			mono_mb_emit_stloc (mb, conv_arg);
			break;
		}

		/* Set src */
		mono_mb_emit_ldarg (mb, argnum);
		if (m_type_is_byref (t)) {
			int pos2;

			/* Check for NULL and raise an exception */
			pos2 = mono_mb_emit_branch (mb, CEE_BRTRUE);

			mono_mb_emit_exception (mb, "ArgumentNullException", NULL);

			mono_mb_patch_branch (mb, pos2);
			mono_mb_emit_ldarg (mb, argnum);
			mono_mb_emit_byte (mb, CEE_LDIND_I);
		}

		mono_mb_emit_stloc (mb, 0);

		mono_mb_emit_byte (mb, CEE_LDC_I4_0);
		mono_mb_emit_stloc (mb, conv_arg);

		mono_mb_emit_ldloc (mb, 0);
		pos = mono_mb_emit_branch (mb, CEE_BRFALSE);

		/* Create and set dst */
		mono_mb_emit_byte (mb, MONO_CUSTOM_PREFIX);
		mono_mb_emit_op (mb, CEE_MONO_NEWOBJ, klass);
		mono_mb_emit_stloc (mb, conv_arg);
		mono_mb_emit_ldloc (mb, conv_arg);
		mono_mb_emit_ldflda (mb, MONO_ABI_SIZEOF (MonoObject));
		mono_mb_emit_stloc (mb, 1);

		/* emit valuetype conversion code */
		emit_struct_conv (mb, klass, TRUE);

		mono_mb_patch_branch (mb, pos);
		break;

	case MARSHAL_ACTION_MANAGED_CONV_OUT:
		if (m_class_is_delegate (klass)) {
			if (m_type_is_byref (t)) {
				int stind_op;
				mono_mb_emit_ldarg (mb, argnum);
				mono_mb_emit_ldloc (mb, conv_arg);
				mono_mb_emit_icall_id (mb, conv_to_icall (MONO_MARSHAL_CONV_DEL_FTN, &stind_op));
				mono_mb_emit_byte (mb, stind_op);
				break;
			}
		}

		if (m_type_is_byref (t)) {
			/* Check for null */
			mono_mb_emit_ldloc (mb, conv_arg);
			pos = mono_mb_emit_branch (mb, CEE_BRTRUE);
			mono_mb_emit_ldarg (mb, argnum);
			mono_mb_emit_byte (mb, CEE_LDC_I4_0);
			mono_mb_emit_byte (mb, CEE_STIND_I);
			pos2 = mono_mb_emit_branch (mb, CEE_BR);

			mono_mb_patch_branch (mb, pos);

			/* Set src */
			mono_mb_emit_ldloc (mb, conv_arg);
			mono_mb_emit_ldflda (mb, MONO_ABI_SIZEOF (MonoObject));
			mono_mb_emit_stloc (mb, 0);

			/* Allocate and set dest */
			mono_mb_emit_icon (mb, mono_class_native_size (klass, NULL));
			mono_mb_emit_byte (mb, CEE_CONV_I);
			mono_mb_emit_icall (mb, ves_icall_marshal_alloc);
			mono_mb_emit_stloc (mb, 1);

			/* Update argument pointer */
			mono_mb_emit_ldarg (mb, argnum);
			mono_mb_emit_ldloc (mb, 1);
			mono_mb_emit_byte (mb, CEE_STIND_I);

			/* emit valuetype conversion code */
			emit_struct_conv (mb, klass, FALSE);

			mono_mb_patch_branch (mb, pos2);
		} else if (klass == mono_class_try_get_stringbuilder_class ()) {
			// FIXME: What to do here ?
		} else {
			/* byval [Out] marshalling */

			/* FIXME: Handle null */

			/* Set src */
			mono_mb_emit_ldloc (mb, conv_arg);
			mono_mb_emit_ldflda (mb, MONO_ABI_SIZEOF (MonoObject));
			mono_mb_emit_stloc (mb, 0);

			/* Set dest */
			mono_mb_emit_ldarg (mb, argnum);
			mono_mb_emit_stloc (mb, 1);

			/* emit valuetype conversion code */
			emit_struct_conv (mb, klass, FALSE);
		}
		break;

	case MARSHAL_ACTION_MANAGED_CONV_RESULT:
		if (m_class_is_delegate (klass)) {
			mono_mb_emit_icall_id (mb, conv_to_icall (MONO_MARSHAL_CONV_DEL_FTN, NULL));
			mono_mb_emit_stloc (mb, 3);
			break;
		}

		/* The class can not have an automatic layout */
		if (mono_class_is_auto_layout (klass)) {
			mono_mb_emit_auto_layout_exception (mb, klass);
			break;
		}

		mono_mb_emit_stloc (mb, 0);
		/* Check for null */
		mono_mb_emit_ldloc (mb, 0);
		pos = mono_mb_emit_branch (mb, CEE_BRTRUE);
		mono_mb_emit_byte (mb, CEE_LDNULL);
		mono_mb_emit_stloc (mb, 3);
		pos2 = mono_mb_emit_branch (mb, CEE_BR);

		mono_mb_patch_branch (mb, pos);

		/* Set src */
		mono_mb_emit_ldloc (mb, 0);
		mono_mb_emit_ldflda (mb, MONO_ABI_SIZEOF (MonoObject));
		mono_mb_emit_stloc (mb, 0);

		/* Allocate and set dest */
		mono_mb_emit_icon (mb, mono_class_native_size (klass, NULL));
		mono_mb_emit_byte (mb, CEE_CONV_I);
		mono_mb_emit_icall (mb, ves_icall_marshal_alloc);
		mono_mb_emit_byte (mb, CEE_DUP);
		mono_mb_emit_stloc (mb, 1);
		mono_mb_emit_stloc (mb, 3);

		emit_struct_conv (mb, klass, FALSE);

		mono_mb_patch_branch (mb, pos2);
		break;

	default:
		g_assert_not_reached ();
	}
	return conv_arg;
}

static int
emit_marshal_variant_ilgen (EmitMarshalContext *m, int argnum, MonoType *t,
		     MonoMarshalSpec *spec,
		     int conv_arg, MonoType **conv_arg_type,
		     MarshalAction action)
{
#ifndef DISABLE_COM
	MonoMethodBuilder *mb = m->mb;
	MonoType *variant_type = m_class_get_byval_arg (mono_class_get_variant_class ());
	MonoType *variant_type_byref = mono_class_get_byref_type (mono_class_get_variant_class ());
	MonoType *object_type = mono_get_object_type ();

	switch (action) {
	case MARSHAL_ACTION_CONV_IN: {
		conv_arg = mono_mb_add_local (mb, variant_type);

		if (m_type_is_byref (t))
			*conv_arg_type = variant_type_byref;
		else
			*conv_arg_type = variant_type;

		if (m_type_is_byref (t) && !(t->attrs & PARAM_ATTRIBUTE_IN) && t->attrs & PARAM_ATTRIBUTE_OUT)
			break;

		mono_mb_emit_ldarg (mb, argnum);
		if (m_type_is_byref (t))
			mono_mb_emit_byte(mb, CEE_LDIND_REF);
		mono_mb_emit_ldloc_addr (mb, conv_arg);
		mono_mb_emit_managed_call (mb, mono_get_Marshal_GetNativeVariantForObject (), NULL);
		break;
	}

	case MARSHAL_ACTION_CONV_OUT: {
		if (m_type_is_byref (t) && (t->attrs & PARAM_ATTRIBUTE_OUT || !(t->attrs & PARAM_ATTRIBUTE_IN))) {
			mono_mb_emit_ldarg (mb, argnum);
			mono_mb_emit_ldloc_addr (mb, conv_arg);
			mono_mb_emit_managed_call (mb, mono_get_Marshal_GetObjectForNativeVariant (), NULL);
			mono_mb_emit_byte (mb, CEE_STIND_REF);
		}

		mono_mb_emit_ldloc_addr (mb, conv_arg);
		mono_mb_emit_managed_call (mb, mono_get_Variant_Clear (), NULL);
		break;
	}

	case MARSHAL_ACTION_PUSH:
		if (m_type_is_byref (t))
			mono_mb_emit_ldloc_addr (mb, conv_arg);
		else
			mono_mb_emit_ldloc (mb, conv_arg);
		break;

	case MARSHAL_ACTION_CONV_RESULT: {
		char *msg = g_strdup ("Marshalling of VARIANT not supported as a return type.");
		mono_mb_emit_exception_marshal_directive (mb, msg);
		break;
	}

	case MARSHAL_ACTION_MANAGED_CONV_IN: {
		conv_arg = mono_mb_add_local (mb, object_type);

		if (m_type_is_byref (t))
			*conv_arg_type = variant_type_byref;
		else
			*conv_arg_type = variant_type;

		if (m_type_is_byref (t) && !(t->attrs & PARAM_ATTRIBUTE_IN) && t->attrs & PARAM_ATTRIBUTE_OUT)
			break;

		if (m_type_is_byref (t))
			mono_mb_emit_ldarg (mb, argnum);
		else
			mono_mb_emit_ldarg_addr (mb, argnum);
		mono_mb_emit_managed_call (mb, mono_get_Marshal_GetObjectForNativeVariant (), NULL);
		mono_mb_emit_stloc (mb, conv_arg);
		break;
	}

	case MARSHAL_ACTION_MANAGED_CONV_OUT: {
		if (m_type_is_byref (t) && (t->attrs & PARAM_ATTRIBUTE_OUT || !(t->attrs & PARAM_ATTRIBUTE_IN))) {
			mono_mb_emit_ldloc (mb, conv_arg);
			mono_mb_emit_ldarg (mb, argnum);
			mono_mb_emit_managed_call (mb, mono_get_Marshal_GetNativeVariantForObject (), NULL);
		}
		break;
	}

	case MARSHAL_ACTION_MANAGED_CONV_RESULT: {
		char *msg = g_strdup ("Marshalling of VARIANT not supported as a return type.");
		mono_mb_emit_exception_marshal_directive (mb, msg);
		break;
	}

	default:
		g_assert_not_reached ();
	}
#endif /* DISABLE_COM */

	return conv_arg;
}

static gboolean
emit_managed_wrapper_validate_signature (MonoMethodSignature* sig, MonoMarshalSpec** mspecs, MonoError* error)
{
	if (mspecs) {
		for (int i = 0; i < sig->param_count; i ++) {
			if (mspecs [i + 1] && mspecs [i + 1]->native == MONO_NATIVE_CUSTOM) {
				if (!mspecs [i + 1]->data.custom_data.custom_name || strlen (mspecs [i + 1]->data.custom_data.custom_name) == 0) {
					mono_error_set_generic_error (error, "System", "TypeLoadException", "Missing ICustomMarshaler type");
					return FALSE;
				}

				switch (sig->params[i]->type) {
				case MONO_TYPE_OBJECT:
				case MONO_TYPE_CLASS:
				case MONO_TYPE_VALUETYPE:
				case MONO_TYPE_ARRAY:
				case MONO_TYPE_SZARRAY:
				case MONO_TYPE_STRING:
				case MONO_TYPE_BOOLEAN:
					break;
				default:
					mono_error_set_generic_error (error, "System.Runtime.InteropServices", "MarshalDirectiveException", "custom marshalling of type %x is currently not supported", sig->params[i]->type);
					return FALSE;
				}
			} else if (sig->params[i]->type == MONO_TYPE_VALUETYPE) {
				MonoClass *klass = mono_class_from_mono_type_internal (sig->params [i]);
				MonoMarshalType *marshal_type = mono_marshal_load_type_info (klass);
				for (int field_idx = 0; field_idx < marshal_type->num_fields; ++field_idx) {
					if (marshal_type->fields [field_idx].mspec && marshal_type->fields [field_idx].mspec->native == MONO_NATIVE_CUSTOM) {
						mono_error_set_type_load_class (error, klass, "Value type includes custom marshaled fields");
						return FALSE;
					}
				}
			}
		}
	}

	return TRUE;
}

static void
emit_managed_wrapper_ilgen (MonoMethodBuilder *mb, MonoMethodSignature *invoke_sig, MonoMarshalSpec **mspecs, EmitMarshalContext* m, MonoMethod *method, MonoGCHandle target_handle, MonoError *error)
{
	MonoMethodSignature *sig, *csig;
	int i, *tmp_locals, orig_domain, attach_cookie;
	gboolean closed = FALSE;

	sig = m->sig;
	csig = m->csig;

	if (!emit_managed_wrapper_validate_signature (sig, mspecs, error))
		return;

	MonoType *int_type = mono_get_int_type ();
	MonoType *boolean_type = m_class_get_byval_arg (mono_defaults.boolean_class);
	/* allocate local 0 (pointer) src_ptr */
	mono_mb_add_local (mb, int_type);
	/* allocate local 1 (pointer) dst_ptr */
	mono_mb_add_local (mb, int_type);
	/* allocate local 2 (boolean) delete_old */
	mono_mb_add_local (mb, boolean_type);

	if (!sig->hasthis && sig->param_count != invoke_sig->param_count) {
		/* Closed delegate */
		g_assert (sig->param_count == invoke_sig->param_count + 1);
		closed = TRUE;
		/* Use a new signature without the first argument */
		sig = mono_metadata_signature_dup (sig);
		memmove (&sig->params [0], &sig->params [1], (sig->param_count - 1) * sizeof (MonoType*));
		sig->param_count --;
	}

	if (!MONO_TYPE_IS_VOID(sig->ret)) {
		/* allocate local 3 to store the return value */
		mono_mb_add_local (mb, sig->ret);
	}

	if (MONO_TYPE_ISSTRUCT (sig->ret))
		m->vtaddr_var = mono_mb_add_local (mb, int_type);

	orig_domain = mono_mb_add_local (mb, int_type);
	attach_cookie = mono_mb_add_local (mb, int_type);

	/*
	 * // does (STARTING|RUNNING|BLOCKING) -> RUNNING + set/switch domain
	 * intptr_t attach_cookie;
	 * intptr_t orig_domain = mono_threads_attach_coop (domain, &attach_cookie);
	 * <interrupt check>
	 *
	 * ret = method (...);
	 * // does RUNNING -> (RUNNING|BLOCKING) + unset/switch domain
	 * mono_threads_detach_coop (orig_domain, &attach_cookie);
	 *
	 * return ret;
	 */

	mono_mb_emit_icon (mb, 0);
	mono_mb_emit_stloc (mb, 2);

	/* orig_domain = mono_threads_attach_coop (domain, &attach_cookie); */
	mono_mb_emit_byte (mb, MONO_CUSTOM_PREFIX);
	mono_mb_emit_byte (mb, CEE_MONO_LDDOMAIN);
	mono_mb_emit_ldloc_addr (mb, attach_cookie);
	/*
	 * This icall is special cased in the JIT so it works in native-to-managed wrappers in unattached threads.
	 * Keep this in sync with the CEE_JIT_ICALL code in the JIT.
	 *
	 * Special cased in interpreter, keep in sync.
	 */
	mono_mb_emit_icall (mb, mono_threads_attach_coop);
	mono_mb_emit_stloc (mb, orig_domain);

	/* <interrupt check> */
	emit_thread_interrupt_checkpoint (mb);

	/* we first do all conversions */
	tmp_locals = g_newa (int, sig->param_count);
	for (i = 0; i < sig->param_count; i ++) {
		MonoType *t = sig->params [i];
		MonoMarshalSpec *spec = mspecs [i + 1];

		if (spec && spec->native == MONO_NATIVE_CUSTOM) {
			tmp_locals [i] = mono_emit_marshal (m, i, t, mspecs [i + 1], 0,  &csig->params [i], MARSHAL_ACTION_MANAGED_CONV_IN);
		} else {
			switch (t->type) {
			case MONO_TYPE_OBJECT:
			case MONO_TYPE_CLASS:
			case MONO_TYPE_VALUETYPE:
			case MONO_TYPE_ARRAY:
			case MONO_TYPE_SZARRAY:
			case MONO_TYPE_STRING:
			case MONO_TYPE_BOOLEAN:
				tmp_locals [i] = mono_emit_marshal (m, i, t, mspecs [i + 1], 0, &csig->params [i], MARSHAL_ACTION_MANAGED_CONV_IN);
				break;
			default:
				tmp_locals [i] = 0;
				break;
			}
		}
	}

	if (sig->hasthis) {
		if (target_handle) {
			mono_mb_emit_icon8 (mb, (gint64)target_handle);
			mono_mb_emit_byte (mb, CEE_CONV_I);
			mono_mb_emit_icall (mb, mono_gchandle_get_target_internal);
		} else {
			/* fixme: */
			g_assert_not_reached ();
		}
	} else if (closed) {
		mono_mb_emit_icon8 (mb, (gint64)target_handle);
		mono_mb_emit_byte (mb, CEE_CONV_I);
		mono_mb_emit_icall (mb, mono_gchandle_get_target_internal);
	}

	for (i = 0; i < sig->param_count; i++) {
		MonoType *t = sig->params [i];

		if (tmp_locals [i]) {
			if (m_type_is_byref (t))
				mono_mb_emit_ldloc_addr (mb, tmp_locals [i]);
			else
				mono_mb_emit_ldloc (mb, tmp_locals [i]);
		}
		else
			mono_mb_emit_ldarg (mb, i);
	}

	/* ret = method (...) */
	mono_mb_emit_managed_call (mb, method, NULL);

	if (MONO_TYPE_ISSTRUCT (sig->ret) && sig->ret->type != MONO_TYPE_GENERICINST) {
		MonoClass *klass = mono_class_from_mono_type_internal (sig->ret);
		mono_class_init_internal (klass);
		if (!(mono_class_is_explicit_layout (klass) || m_class_is_blittable (klass))) {
			/* This is used by get_marshal_cb ()->emit_marshal_vtype (), but it needs to go right before the call */
			mono_mb_emit_byte (mb, MONO_CUSTOM_PREFIX);
			mono_mb_emit_byte (mb, CEE_MONO_VTADDR);
			mono_mb_emit_stloc (mb, m->vtaddr_var);
		}
	}

	if (mspecs [0] && mspecs [0]->native == MONO_NATIVE_CUSTOM) {
		mono_emit_marshal (m, 0, sig->ret, mspecs [0], 0, NULL, MARSHAL_ACTION_MANAGED_CONV_RESULT);
	} else if (!m_type_is_byref (sig->ret)) {
		switch (sig->ret->type) {
		case MONO_TYPE_VOID:
			break;
		case MONO_TYPE_BOOLEAN:
		case MONO_TYPE_I1:
		case MONO_TYPE_U1:
		case MONO_TYPE_CHAR:
		case MONO_TYPE_I2:
		case MONO_TYPE_U2:
		case MONO_TYPE_I4:
		case MONO_TYPE_U4:
		case MONO_TYPE_I:
		case MONO_TYPE_U:
		case MONO_TYPE_PTR:
		case MONO_TYPE_R4:
		case MONO_TYPE_R8:
		case MONO_TYPE_I8:
		case MONO_TYPE_U8:
		case MONO_TYPE_OBJECT:
			mono_mb_emit_stloc (mb, 3);
			break;
		case MONO_TYPE_STRING:
			csig->ret = int_type;
			mono_emit_marshal (m, 0, sig->ret, mspecs [0], 0, NULL, MARSHAL_ACTION_MANAGED_CONV_RESULT);
			break;
		case MONO_TYPE_VALUETYPE:
		case MONO_TYPE_CLASS:
		case MONO_TYPE_SZARRAY:
			mono_emit_marshal (m, 0, sig->ret, mspecs [0], 0, NULL, MARSHAL_ACTION_MANAGED_CONV_RESULT);
			break;
		case MONO_TYPE_GENERICINST: {
			mono_mb_emit_byte (mb, CEE_POP);
			break;
		}
		default:
			g_warning ("return type 0x%02x unknown", sig->ret->type);
			g_assert_not_reached ();
		}
	} else {
		mono_mb_emit_stloc (mb, 3);
	}

	/* Convert byref arguments back */
	for (i = 0; i < sig->param_count; i ++) {
		MonoType *t = sig->params [i];
		MonoMarshalSpec *spec = mspecs [i + 1];

		if (spec && spec->native == MONO_NATIVE_CUSTOM) {
			mono_emit_marshal (m, i, t, mspecs [i + 1], tmp_locals [i], NULL, MARSHAL_ACTION_MANAGED_CONV_OUT);
		}
		else if (m_type_is_byref (t)) {
			switch (t->type) {
			case MONO_TYPE_CLASS:
			case MONO_TYPE_VALUETYPE:
			case MONO_TYPE_OBJECT:
			case MONO_TYPE_STRING:
			case MONO_TYPE_BOOLEAN:
				mono_emit_marshal (m, i, t, mspecs [i + 1], tmp_locals [i], NULL, MARSHAL_ACTION_MANAGED_CONV_OUT);
				break;
			default:
				break;
			}
		}
		else if (invoke_sig->params [i]->attrs & PARAM_ATTRIBUTE_OUT) {
			/* The [Out] information is encoded in the delegate signature */
			switch (t->type) {
			case MONO_TYPE_SZARRAY:
			case MONO_TYPE_CLASS:
			case MONO_TYPE_VALUETYPE:
				mono_emit_marshal (m, i, invoke_sig->params [i], mspecs [i + 1], tmp_locals [i], NULL, MARSHAL_ACTION_MANAGED_CONV_OUT);
				break;
			default:
				g_assert_not_reached ();
			}
		}
	}

	/* mono_threads_detach_coop (orig_domain, &attach_cookie); */
	mono_mb_emit_ldloc (mb, orig_domain);
	mono_mb_emit_ldloc_addr (mb, attach_cookie);
	/* Special cased in interpreter, keep in sync */
	mono_mb_emit_icall (mb, mono_threads_detach_coop);

	/* return ret; */
	if (m->retobj_var) {
		mono_mb_emit_ldloc (mb, m->retobj_var);
		mono_mb_emit_byte (mb, MONO_CUSTOM_PREFIX);
		mono_mb_emit_op (mb, CEE_MONO_RETOBJ, m->retobj_class);
	}
	else {
		if (!MONO_TYPE_IS_VOID (sig->ret))
			mono_mb_emit_ldloc (mb, 3);
		mono_mb_emit_byte (mb, CEE_RET);
	}

	if (closed)
		g_free (sig);
}

static void
emit_struct_to_ptr_ilgen (MonoMethodBuilder *mb, MonoClass *klass)
{
	MonoType *int_type = mono_get_int_type ();
	MonoType *boolean_type = m_class_get_byval_arg (mono_defaults.boolean_class);
	if (m_class_is_blittable (klass)) {
		mono_mb_emit_byte (mb, CEE_LDARG_1);
		mono_mb_emit_byte (mb, CEE_LDARG_0);
		mono_mb_emit_ldflda (mb, MONO_ABI_SIZEOF (MonoObject));
		mono_mb_emit_icon (mb, mono_class_value_size (klass, NULL));
		mono_mb_emit_byte (mb, CEE_PREFIX1);
		mono_mb_emit_byte (mb, CEE_CPBLK);
	} else {

		/* allocate local 0 (pointer) src_ptr */
		mono_mb_add_local (mb, int_type);
		/* allocate local 1 (pointer) dst_ptr */
		mono_mb_add_local (mb, int_type);
		/* allocate local 2 (boolean) delete_old */
		mono_mb_add_local (mb, boolean_type);
		mono_mb_emit_byte (mb, CEE_LDARG_2);
		mono_mb_emit_stloc (mb, 2);

		/* initialize src_ptr to point to the start of object data */
		mono_mb_emit_byte (mb, CEE_LDARG_0);
		mono_mb_emit_ldflda (mb, MONO_ABI_SIZEOF (MonoObject));
		mono_mb_emit_stloc (mb, 0);

		/* initialize dst_ptr */
		mono_mb_emit_byte (mb, CEE_LDARG_1);
		mono_mb_emit_stloc (mb, 1);

		emit_struct_conv (mb, klass, FALSE);
	}

	mono_mb_emit_byte (mb, CEE_RET);
}

static void
emit_ptr_to_struct_ilgen (MonoMethodBuilder *mb, MonoClass *klass)
{
	MonoType *int_type = mono_get_int_type ();
	if (m_class_is_blittable (klass)) {
		mono_mb_emit_byte (mb, CEE_LDARG_1);
		mono_mb_emit_ldflda (mb, MONO_ABI_SIZEOF (MonoObject));
		mono_mb_emit_byte (mb, CEE_LDARG_0);
		mono_mb_emit_icon (mb, mono_class_value_size (klass, NULL));
		mono_mb_emit_byte (mb, CEE_PREFIX1);
		mono_mb_emit_byte (mb, CEE_CPBLK);
	} else {

		/* allocate local 0 (pointer) src_ptr */
		mono_mb_add_local (mb, int_type);
		/* allocate local 1 (pointer) dst_ptr */
		mono_mb_add_local (mb, m_class_get_this_arg (klass));

		/* initialize src_ptr to point to the start of object data */
		mono_mb_emit_byte (mb, CEE_LDARG_0);
		mono_mb_emit_stloc (mb, 0);

		/* initialize dst_ptr */
		mono_mb_emit_byte (mb, CEE_LDARG_1);
		mono_mb_emit_ldflda (mb, MONO_ABI_SIZEOF (MonoObject));
		mono_mb_emit_stloc (mb, 1);

		emit_struct_conv (mb, klass, TRUE);
	}

	mono_mb_emit_byte (mb, CEE_RET);
}

static void
emit_create_string_hack_ilgen (MonoMethodBuilder *mb, MonoMethodSignature *csig, MonoMethod *res)
{
	int i;

	g_assert (!mono_method_signature_internal (res)->hasthis);
	for (i = 1; i <= csig->param_count; i++)
		mono_mb_emit_ldarg (mb, i);
	mono_mb_emit_managed_call (mb, res, NULL);
	mono_mb_emit_byte (mb, CEE_RET);
}

/* How the arguments of an icall should be wrapped */
typedef enum {
	/* Don't wrap at all, pass the argument as is */
	ICALL_HANDLES_WRAP_NONE,
	/* Wrap the argument in an object handle, pass the handle to the icall */
	ICALL_HANDLES_WRAP_OBJ,
	/* Wrap the argument in an object handle, pass the handle to the icall,
	   write the value out from the handle when the icall returns */
	ICALL_HANDLES_WRAP_OBJ_INOUT,
	/* Initialized an object handle to null, pass to the icalls,
	   write the value out from the handle when the icall returns */
	ICALL_HANDLES_WRAP_OBJ_OUT,
	/* Wrap the argument (a valuetype reference) in a handle to pin its
	   enclosing object, but pass the raw reference to the icall.  This is
	   also how we pass byref generic parameter arguments to generic method
	   icalls (e.g. System.Array:GetGenericValue_icall<T>(int idx, T out value)) */
	ICALL_HANDLES_WRAP_VALUETYPE_REF,
} IcallHandlesWrap;

typedef struct {
	IcallHandlesWrap wrap;
	// If wrap is OBJ_OUT or OBJ_INOUT this is >= 0 and holds the referenced managed object,
	// in case the actual parameter refers to a native frame.
	// Otherwise it is -1.
	int handle;
}  IcallHandlesLocal;

/*
 * Describes how to wrap the given parameter.
 *
 */
static IcallHandlesWrap
signature_param_uses_handles (MonoMethodSignature *sig, MonoMethodSignature *generic_sig, int param)
{
	/* If there is a generic parameter that isn't passed byref, we don't
	 * know how to pass it to an icall that expects some arguments to be
	 * wrapped in handles: if the actual argument type is a reference type
	 * we'd need to wrap it in a handle, otherwise we'd want to pass it as is.
	 */
	/* FIXME: We should eventually relax the assertion, below, to
	 * allow generic parameters that are constrained to be reference types.
	 */
	g_assert (!generic_sig || !mono_type_is_generic_parameter (generic_sig->params [param]));

	/* If the parameter in the generic version of the method signature is a
	 * byref type variable T&, pass the corresponding argument by pinning
	 * the memory and passing the raw pointer to the icall.  Note that we
	 * do this even if the actual instantiation is a byref reference type
	 * like string& since the C code for the icall has to work uniformly
	 * for both valuetypes and reference types.
	 */
	if (generic_sig && m_type_is_byref (generic_sig->params [param]) &&
	    (generic_sig->params [param]->type == MONO_TYPE_VAR || generic_sig->params [param]->type == MONO_TYPE_MVAR))
		return ICALL_HANDLES_WRAP_VALUETYPE_REF;

	if (MONO_TYPE_IS_REFERENCE (sig->params [param])) {
		if (mono_signature_param_is_out (sig, param))
			return ICALL_HANDLES_WRAP_OBJ_OUT;
		else if (m_type_is_byref (sig->params [param]))
			return ICALL_HANDLES_WRAP_OBJ_INOUT;
		else
			return ICALL_HANDLES_WRAP_OBJ;
	} else if (m_type_is_byref (sig->params [param]))
		return ICALL_HANDLES_WRAP_VALUETYPE_REF;
	else
		return ICALL_HANDLES_WRAP_NONE;
}

static void
emit_native_icall_wrapper_ilgen (MonoMethodBuilder *mb, MonoMethod *method, MonoMethodSignature *csig, gboolean check_exceptions, gboolean aot, MonoMethodPInvoke *piinfo)
{
	// FIXME:
	MonoMethodSignature *call_sig = csig;
	gboolean uses_handles = FALSE;
	gboolean foreign_icall = FALSE;
	IcallHandlesLocal *handles_locals = NULL;
	MonoMethodSignature *sig = mono_method_signature_internal (method);
	gboolean need_gc_safe = FALSE;
	GCSafeTransitionBuilder gc_safe_transition_builder;

	(void) mono_lookup_internal_call_full (method, FALSE, &uses_handles, &foreign_icall);

	if (G_UNLIKELY (foreign_icall)) {
		/* FIXME: we only want the transitions for hybrid suspend.  Q: What to do about AOT? */
		need_gc_safe = gc_safe_transition_builder_init (&gc_safe_transition_builder, mb, FALSE);

		if (need_gc_safe)
			gc_safe_transition_builder_add_locals (&gc_safe_transition_builder);
	}

	if (sig->hasthis) {
		/*
		 * Add a null check since public icalls can be called with 'call' which
		 * does no such check.
		 */
		mono_mb_emit_byte (mb, CEE_LDARG_0);
		const int pos = mono_mb_emit_branch (mb, CEE_BRTRUE);
		mono_mb_emit_exception (mb, "NullReferenceException", NULL);
		mono_mb_patch_branch (mb, pos);
	}

	if (uses_handles) {
		MonoMethodSignature *generic_sig = NULL;

		if (method->is_inflated) {
			ERROR_DECL (error);
			MonoMethod *generic_method = ((MonoMethodInflated*)method)->declaring;
			generic_sig = mono_method_signature_checked (generic_method, error);
			mono_error_assert_ok (error);
		}

		// FIXME: The stuff from mono_metadata_signature_dup_internal_with_padding ()
		call_sig = mono_metadata_signature_alloc (get_method_image (method), csig->param_count);
		call_sig->param_count = csig->param_count;
		call_sig->ret = csig->ret;
		call_sig->pinvoke = csig->pinvoke;

		/* TODO support adding wrappers to non-static struct methods */
		g_assert (!sig->hasthis || !m_class_is_valuetype (mono_method_get_class (method)));

		handles_locals = g_new0 (IcallHandlesLocal, csig->param_count);

		for (int i = 0; i < csig->param_count; ++i) {
			// Determine which args need to be wrapped in handles and adjust icall signature.
			// Here, a handle is a pointer to a volatile local in a managed frame -- which is sufficient and efficient.
			const IcallHandlesWrap w = signature_param_uses_handles (csig, generic_sig, i);
			handles_locals [i].wrap = w;
			int local = -1;

			switch (w) {
				case ICALL_HANDLES_WRAP_OBJ:
				case ICALL_HANDLES_WRAP_OBJ_INOUT:
				case ICALL_HANDLES_WRAP_OBJ_OUT:
					call_sig->params [i] = mono_class_get_byref_type (mono_class_from_mono_type_internal (csig->params[i]));
					break;
				case ICALL_HANDLES_WRAP_NONE:
				case ICALL_HANDLES_WRAP_VALUETYPE_REF:
					call_sig->params [i] = csig->params [i];
					break;
				default:
					g_assert_not_reached ();
			}

			// Add a local var to hold the references for each out arg.
			switch (w) {
				case ICALL_HANDLES_WRAP_OBJ_INOUT:
				case ICALL_HANDLES_WRAP_OBJ_OUT:
					// FIXME better type
					local = mono_mb_add_local (mb, mono_get_object_type ());

					if (!mb->volatile_locals) {
						gpointer mem = mono_image_alloc0 (get_method_image (method), mono_bitset_alloc_size (csig->param_count + 1, 0));
						mb->volatile_locals = mono_bitset_mem_new (mem, csig->param_count + 1, 0);
					}
					mono_bitset_set (mb->volatile_locals, local);
					break;
				case ICALL_HANDLES_WRAP_VALUETYPE_REF:
				case ICALL_HANDLES_WRAP_OBJ:
					if (!mb->volatile_args) {
						gpointer mem = mono_image_alloc0 (get_method_image (method), mono_bitset_alloc_size (csig->param_count + 1, 0));
						mb->volatile_args = mono_bitset_mem_new (mem, csig->param_count + 1, 0);
					}
					mono_bitset_set (mb->volatile_args, i);
					break;
				case ICALL_HANDLES_WRAP_NONE:
					break;
				default:
					g_assert_not_reached ();
			}
			handles_locals [i].handle = local;

			// Load each argument. References into the managed heap get wrapped in handles.
			// Handles here are just pointers to managed volatile locals.
			switch (w) {
				case ICALL_HANDLES_WRAP_NONE:
				case ICALL_HANDLES_WRAP_VALUETYPE_REF:
					// argI = argI
					mono_mb_emit_ldarg (mb, i);
					break;
				case ICALL_HANDLES_WRAP_OBJ:
					// argI = &argI_raw
					mono_mb_emit_ldarg_addr (mb, i);
					break;
				case ICALL_HANDLES_WRAP_OBJ_INOUT:
				case ICALL_HANDLES_WRAP_OBJ_OUT:
					// If parameter guaranteeably referred to a managed frame,
					// then could just be passthrough and volatile. Since
					// that cannot be guaranteed, use a managed volatile local intermediate.
					// ObjOut:
					//   localI = NULL
					// ObjInOut:
					//   localI = *argI_raw
					// &localI
					if (w == ICALL_HANDLES_WRAP_OBJ_OUT) {
						mono_mb_emit_byte (mb, CEE_LDNULL);
					} else {
						mono_mb_emit_ldarg (mb, i);
						mono_mb_emit_byte (mb, CEE_LDIND_REF);
					}
					mono_mb_emit_stloc (mb, local);
					mono_mb_emit_ldloc_addr (mb, local);
					break;
				default:
					g_assert_not_reached ();
			}
		}
	} else {
		for (int i = 0; i < csig->param_count; i++)
			mono_mb_emit_ldarg (mb, i);
	}

	if (need_gc_safe)
		gc_safe_transition_builder_emit_enter (&gc_safe_transition_builder, &piinfo->method, aot);

	if (aot) {
		mono_mb_emit_byte (mb, MONO_CUSTOM_PREFIX);
		mono_mb_emit_op (mb, CEE_MONO_ICALL_ADDR, &piinfo->method);
		mono_mb_emit_calli (mb, call_sig);
	} else {
		g_assert (piinfo->addr);
		mono_mb_emit_native_call (mb, call_sig, piinfo->addr);
	}

	if (need_gc_safe)
		gc_safe_transition_builder_emit_exit (&gc_safe_transition_builder);

	// Copy back ObjOut and ObjInOut from locals through parameters.
	if (mb->volatile_locals) {
		g_assert (handles_locals);
		for (int i = 0; i < csig->param_count; i++) {
			const int local = handles_locals [i].handle;
			if (local >= 0) {
				// *argI_raw = localI
				mono_mb_emit_ldarg (mb, i);
				mono_mb_emit_ldloc (mb, local);
				mono_mb_emit_byte (mb, CEE_STIND_REF);
			}
		}
	}
	g_free (handles_locals);

	if (need_gc_safe)
		gc_safe_transition_builder_cleanup (&gc_safe_transition_builder);

	if (check_exceptions)
		emit_thread_interrupt_checkpoint (mb);
	mono_mb_emit_byte (mb, CEE_RET);
}

static void
mb_emit_exception_ilgen (MonoMethodBuilder *mb, const char *exc_nspace, const char *exc_name, const char *msg)
{
	mono_mb_emit_exception_full (mb, exc_nspace, exc_name, msg);
}

static void
mb_emit_exception_for_error_ilgen (MonoMethodBuilder *mb, const MonoError *error)
{
	mono_mb_emit_exception_for_error (mb, (MonoError*)error);
}

static void
emit_marshal_directive_exception_ilgen (EmitMarshalContext *m, int argnum, const char* msg)
{
	char* fullmsg = NULL;
	if (argnum == 0)
		fullmsg = g_strdup_printf("Error marshalling return value: %s", msg);
	else
		fullmsg = g_strdup_printf("Error marshalling parameter #%d: %s", argnum, msg);

	mono_mb_emit_exception_marshal_directive (m->mb, fullmsg);
}

static void
emit_vtfixup_ftnptr_ilgen (MonoMethodBuilder *mb, MonoMethod *method, int param_count, guint16 type)
{
	for (int i = 0; i < param_count; i++)
		mono_mb_emit_ldarg (mb, i);

	if (type & VTFIXUP_TYPE_CALL_MOST_DERIVED)
		mono_mb_emit_op (mb, CEE_CALLVIRT, method);
	else
		mono_mb_emit_op (mb, CEE_CALL, method);
	mono_mb_emit_byte (mb, CEE_RET);
}

static void
emit_icall_wrapper_ilgen (MonoMethodBuilder *mb, MonoJitICallInfo *callinfo, MonoMethodSignature *csig2, gboolean check_exceptions)
{
	MonoMethodSignature *const sig = callinfo->sig;

	if (sig->hasthis)
		mono_mb_emit_byte (mb, CEE_LDARG_0);

	for (int i = 0; i < sig->param_count; i++)
		mono_mb_emit_ldarg (mb, i + sig->hasthis);

	mono_mb_emit_byte (mb, MONO_CUSTOM_PREFIX);
	mono_mb_emit_byte (mb, CEE_MONO_JIT_ICALL_ADDR);
	mono_mb_emit_i4 (mb, mono_jit_icall_info_index (callinfo));
	mono_mb_emit_calli (mb, csig2);
	if (check_exceptions)
		emit_thread_interrupt_checkpoint (mb);
	mono_mb_emit_byte (mb, CEE_RET);
}

static void
emit_return_ilgen (MonoMethodBuilder *mb)
{
	mono_mb_emit_byte (mb, CEE_RET);
}

void
mono_marshal_ilgen_init (void)
{
	MonoMarshalCallbacks cb;
	cb.version = MONO_MARSHAL_CALLBACKS_VERSION;
	cb.emit_marshal_array = emit_marshal_array_ilgen;
	cb.emit_marshal_ptr = emit_marshal_ptr_ilgen;
	cb.emit_marshal_scalar = emit_marshal_scalar_ilgen;
#ifndef DISABLE_NONBLITTABLE
	cb.emit_marshal_boolean = emit_marshal_boolean_ilgen;
	cb.emit_marshal_char = emit_marshal_char_ilgen;
	cb.emit_marshal_custom = emit_marshal_custom_ilgen;
	cb.emit_marshal_asany = emit_marshal_asany_ilgen;
	cb.emit_marshal_vtype = emit_marshal_vtype_ilgen;
	cb.emit_marshal_string = emit_marshal_string_ilgen;
	cb.emit_marshal_safehandle = emit_marshal_safehandle_ilgen;
	cb.emit_marshal_handleref = emit_marshal_handleref_ilgen;
	cb.emit_marshal_object = emit_marshal_object_ilgen;
	cb.emit_marshal_variant = emit_marshal_variant_ilgen;
#endif
	cb.emit_castclass = emit_castclass_ilgen;
	cb.emit_struct_to_ptr = emit_struct_to_ptr_ilgen;
	cb.emit_ptr_to_struct = emit_ptr_to_struct_ilgen;
	cb.emit_isinst = emit_isinst_ilgen;
	cb.emit_virtual_stelemref = emit_virtual_stelemref_ilgen;
	cb.emit_stelemref = emit_stelemref_ilgen;
	cb.emit_array_address = emit_array_address_ilgen;
	cb.emit_native_wrapper = emit_native_wrapper_ilgen;
	cb.emit_managed_wrapper = emit_managed_wrapper_ilgen;
	cb.emit_runtime_invoke_body = emit_runtime_invoke_body_ilgen;
	cb.emit_runtime_invoke_dynamic = emit_runtime_invoke_dynamic_ilgen;
	cb.emit_delegate_begin_invoke = emit_delegate_begin_invoke_ilgen;
	cb.emit_delegate_end_invoke = emit_delegate_end_invoke_ilgen;
	cb.emit_delegate_invoke_internal = emit_delegate_invoke_internal_ilgen;
	cb.emit_synchronized_wrapper = emit_synchronized_wrapper_ilgen;
	cb.emit_unbox_wrapper = emit_unbox_wrapper_ilgen;
	cb.emit_array_accessor_wrapper = emit_array_accessor_wrapper_ilgen;
	cb.emit_generic_array_helper = emit_generic_array_helper_ilgen;
	cb.emit_thunk_invoke_wrapper = emit_thunk_invoke_wrapper_ilgen;
	cb.emit_create_string_hack = emit_create_string_hack_ilgen;
	cb.emit_native_icall_wrapper = emit_native_icall_wrapper_ilgen;
	cb.emit_icall_wrapper = emit_icall_wrapper_ilgen;
	cb.emit_return = emit_return_ilgen;
	cb.emit_vtfixup_ftnptr = emit_vtfixup_ftnptr_ilgen;
	cb.mb_skip_visibility = mb_skip_visibility_ilgen;
	cb.mb_set_dynamic = mb_set_dynamic_ilgen;
	cb.mb_emit_exception = mb_emit_exception_ilgen;
	cb.mb_emit_exception_for_error = mb_emit_exception_for_error_ilgen;
	cb.mb_emit_byte = mb_emit_byte_ilgen;
	cb.emit_marshal_directive_exception = emit_marshal_directive_exception_ilgen;
#ifdef DISABLE_NONBLITTABLE
	mono_marshal_noilgen_init_blittable (&cb);
#endif
	mono_install_marshal_callbacks (&cb);
}
