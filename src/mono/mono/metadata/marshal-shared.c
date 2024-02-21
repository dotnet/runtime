// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

#include "metadata/marshal-shared.h"
#include "mono/metadata/debug-helpers.h"
#include "metadata/marshal.h"
#include "metadata/marshal-shared.h"
#include "metadata/method-builder-ilgen.h"
#include "metadata/custom-attrs-internals.h"
#include "metadata/class-init.h"
#include "mono/metadata/class-internals.h"
#include "metadata/reflection-internals.h"
#include "mono/metadata/handle.h"


#define OPDEF(a,b,c,d,e,f,g,h,i,j) \
	a = i,

enum {
#include "mono/cil/opcode.def"
	LAST = 0xff
};
#undef OPDEF

static GENERATE_GET_CLASS_WITH_CACHE (fixed_buffer_attribute, "System.Runtime.CompilerServices", "FixedBufferAttribute");
static GENERATE_TRY_GET_CLASS_WITH_CACHE (marshal, "System.Runtime.InteropServices", "Marshal");

/* MonoMethod pointers to SafeHandle::DangerousAddRef and ::DangerousRelease */
static MonoMethod *sh_dangerous_add_ref;
static MonoMethod *sh_dangerous_release;


MonoMethod**
 mono_marshal_shared_get_sh_dangerous_add_ref (void)
{
	return &sh_dangerous_add_ref;
}

MonoMethod** mono_marshal_shared_get_sh_dangerous_release (void)
{
	return &sh_dangerous_release;
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

void
mono_marshal_shared_emit_marshal_custom_get_instance (MonoMethodBuilder *mb, MonoClass *klass, MonoMarshalSpec *spec)
{
	MONO_STATIC_POINTER_INIT (MonoMethod, get_instance)

		MonoClass *Marshal = mono_class_try_get_marshal_class ();
		g_assert (Marshal);
		get_instance = mono_marshal_shared_get_method_nofail (Marshal, "GetCustomMarshalerInstance", 2, 0);
		g_assert (get_instance);

	MONO_STATIC_POINTER_INIT_END (MonoClass, get_instance)

	// HACK: We cannot use ldtoken in this type of wrapper.
	mono_mb_emit_byte (mb, MONO_CUSTOM_PREFIX);
	mono_mb_emit_op (mb, CEE_MONO_CLASSCONST, klass);
	mono_mb_emit_icall (mb, mono_marshal_get_type_object);
	mono_mb_emit_ldstr (mb, g_strdup (spec->data.custom_data.cookie));

	mono_mb_emit_op (mb, CEE_CALL, get_instance);
}

void
mono_marshal_shared_init_safe_handle (void)
{
	mono_atomic_store_seq (mono_marshal_shared_get_sh_dangerous_add_ref(), mono_marshal_shared_get_method_nofail (mono_class_try_get_safehandle_class (), "DangerousAddRef", 1, 0));
	mono_atomic_store_seq (mono_marshal_shared_get_sh_dangerous_release(), mono_marshal_shared_get_method_nofail (mono_class_try_get_safehandle_class (), "DangerousRelease", 0, 0));
}

void
mono_mb_emit_auto_layout_exception (MonoMethodBuilder *mb, MonoClass *klass)
{
	char *msg = g_strdup_printf ("The type `%s.%s' layout needs to be Sequential or Explicit", m_class_get_name_space (klass), m_class_get_name (klass));
	mono_marshal_shared_mb_emit_exception_marshal_directive (mb, msg);
}

gboolean
mono_marshal_shared_is_in (const MonoType *t)
{
	const guint32 attrs = t->attrs;
	return (attrs & PARAM_ATTRIBUTE_IN) || !(attrs & PARAM_ATTRIBUTE_OUT);
}

gboolean
mono_marshal_shared_is_out (const MonoType *t)
{
	const guint32 attrs = t->attrs;
	return (attrs & PARAM_ATTRIBUTE_OUT) || !(attrs & PARAM_ATTRIBUTE_IN);
}

MonoMarshalConv
mono_marshal_shared_conv_str_inverse (MonoMarshalConv conv)
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

/*
 * mono_marshal_shared_mb_emit_exception_marshal_directive:
 *
 *   This function assumes ownership of MSG, which should be malloc-ed.
 */
void
mono_marshal_shared_mb_emit_exception_marshal_directive (MonoMethodBuilder *mb, char *msg)
{
	char *s = mono_mb_strdup (mb, msg);
	g_free (msg);
	mono_mb_emit_exception_full (mb, "System.Runtime.InteropServices", "MarshalDirectiveException", s);
}

gboolean
mono_marshal_shared_get_fixed_buffer_attr (MonoClassField *field, MonoType **out_etype, int *out_len)
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
		MonoDecodeCustomAttr *decoded_args = mono_reflection_create_custom_attr_data_args_noalloc (mono_defaults.corlib, attr->ctor, attr->data, attr->data_size, error);
		if (!is_ok (error))
			return FALSE;

		*out_etype = (MonoType*)decoded_args->typed_args[0]->value.primitive;
		*out_len = *(gint32*)decoded_args->typed_args[1]->value.primitive;

		mono_reflection_free_custom_attr_data_args_noalloc (decoded_args);
	}
	if (cinfo && !cinfo->cached)
		mono_custom_attrs_free (cinfo);
	return attr != NULL;
}

void
mono_marshal_shared_emit_fixed_buf_conv (MonoMethodBuilder *mb, MonoType *type, MonoType *etype, int len, gboolean to_object, int *out_usize)
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
		mono_mb_emit_add_to_local (mb, GINT_TO_UINT16 (index_var), 1);

		mono_mb_emit_branch_label (mb, CEE_BR, label2);

		mono_mb_patch_branch (mb, label3);
	}

	*out_usize = usize * len;
}

int
mono_marshal_shared_offset_of_first_nonstatic_field (MonoClass *klass)
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

// FIXME Consolidate the multiple functions named get_method_nofail.
MonoMethod*
mono_marshal_shared_get_method_nofail (MonoClass *klass, const char *method_name, int num_params, int flags)
{
	MonoMethod *method;
	ERROR_DECL (error);
	method = mono_class_get_method_from_name_checked (klass, method_name, num_params, flags, error);
	mono_error_assert_ok (error);
	g_assertf (method, "Could not lookup method %s in %s", method_name, m_class_get_name (klass));
	return method;
}

MonoJitICallId
mono_marshal_shared_conv_to_icall (MonoMarshalConv conv, int *ind_store_type)
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

void
mono_marshal_shared_emit_ptr_to_object_conv (MonoMethodBuilder *mb, MonoType *type, MonoMarshalConv conv, MonoMarshalSpec *mspec)
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
			mono_marshal_shared_emit_struct_conv (mb, eklass, TRUE);

			/* Loop footer */
			mono_mb_emit_add_to_local (mb, GINT_TO_UINT16 (index_var), 1);

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
		mono_mb_emit_icall_id (mb, mono_marshal_shared_conv_to_icall (mono_marshal_shared_conv_str_inverse (conv), NULL));
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

		mono_marshal_shared_emit_struct_conv (mb, klass, TRUE);

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
		mono_marshal_shared_mb_emit_exception_marshal_directive (mb, msg);
		break;
	}

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

		mono_marshal_shared_mb_emit_exception_marshal_directive (mb, msg);
		break;
	}
	}
}

void
mono_marshal_shared_emit_struct_conv_full (MonoMethodBuilder *mb, MonoClass *klass, gboolean to_object,
						int offset_of_first_child_field, MonoMarshalNative string_encoding)
{
	MonoMarshalType *info;

	if (m_class_get_parent (klass))
		mono_marshal_shared_emit_struct_conv_full (mb, m_class_get_parent (klass), to_object, mono_marshal_shared_offset_of_first_nonstatic_field (klass), string_encoding);

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
			mono_marshal_shared_mb_emit_exception_marshal_directive (mb, msg);
			return;
		}
	}

	for (guint32 i = 0; i < info->num_fields; i++) {
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

			if (m_type_is_byref (ftype) || ftype->type == MONO_TYPE_I || ftype->type == MONO_TYPE_U) {
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
					mono_marshal_shared_mb_emit_exception_marshal_directive (mb, msg);
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

				if (mono_marshal_shared_get_fixed_buffer_attr (info->fields [i].field, &etype, &len)) {
					mono_marshal_shared_emit_fixed_buf_conv (mb, ftype, etype, len, to_object, &usize);
				} else {
					mono_marshal_shared_emit_struct_conv (mb, mono_class_from_mono_type_internal (ftype), to_object);
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
				char *msg = g_strdup_printf ("COM support was disabled at compilation time.");
				mono_marshal_shared_mb_emit_exception_marshal_directive (mb, msg);
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
				mono_marshal_shared_emit_ptr_to_object_conv (mb, ftype, conv, info->fields [i].mspec);
			else
				mono_marshal_shared_emit_object_to_ptr_conv (mb, ftype, conv, info->fields [i].mspec);

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

void
mono_marshal_shared_emit_struct_conv (MonoMethodBuilder *mb, MonoClass *klass, gboolean to_object)
{
	mono_marshal_shared_emit_struct_conv_full (mb, klass, to_object, 0, (MonoMarshalNative)-1);
}

void
mono_marshal_shared_emit_thread_interrupt_checkpoint_call (MonoMethodBuilder *mb, MonoJitICallId checkpoint_icall_id)
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

void
mono_marshal_shared_emit_object_to_ptr_conv (MonoMethodBuilder *mb, MonoType *type, MonoMarshalConv conv, MonoMarshalSpec *mspec)
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
		mono_mb_emit_icall_id (mb, mono_marshal_shared_conv_to_icall (conv, &stind_op));
		mono_mb_emit_byte (mb, GINT_TO_UINT8 (stind_op));
		break;
	}
	case MONO_MARSHAL_CONV_ARRAY_SAVEARRAY:
	case MONO_MARSHAL_CONV_ARRAY_LPARRAY:
	case MONO_MARSHAL_CONV_DEL_FTN:
		mono_mb_emit_ldloc (mb, 1);
		mono_mb_emit_ldloc (mb, 0);
		mono_mb_emit_byte (mb, CEE_LDIND_REF);
		mono_mb_emit_icall_id (mb, mono_marshal_shared_conv_to_icall (conv, &stind_op));
		mono_mb_emit_byte (mb, GINT_TO_UINT8 (stind_op));
		break;
	case MONO_MARSHAL_CONV_STR_BYVALSTR:
	case MONO_MARSHAL_CONV_STR_BYVALWSTR: {
		g_assert (mspec);

		mono_mb_emit_ldloc (mb, 1); /* dst */
		mono_mb_emit_ldloc (mb, 0);
		mono_mb_emit_byte (mb, CEE_LDIND_REF); /* src String */
		mono_mb_emit_icon (mb, mspec->data.array_data.num_elem);
		mono_mb_emit_icall_id (mb, mono_marshal_shared_conv_to_icall (conv, NULL));
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
			mono_marshal_shared_emit_struct_conv (mb, eklass, FALSE);

			/* Loop footer */
			mono_mb_emit_add_to_local (mb, GINT_TO_UINT16 (index_var), 1);

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

		mono_marshal_shared_emit_struct_conv (mb, mono_class_from_mono_type_internal (type), FALSE);

		/* restore the old src pointer */
		mono_mb_emit_ldloc (mb, src_var);
		mono_mb_emit_stloc (mb, 0);
		/* restore the old dst pointer */
		mono_mb_emit_ldloc (mb, dst_var);
		mono_mb_emit_stloc (mb, 1);

		mono_mb_patch_branch (mb, pos);
		break;
	}

	case MONO_MARSHAL_CONV_SAFEHANDLE: {
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
