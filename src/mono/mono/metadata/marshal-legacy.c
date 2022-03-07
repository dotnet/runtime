/**
 * \file
 * Copyright 2022 Microsoft
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */
#include "mono/metadata/debug-helpers.h"
#include "metadata/marshal.h"
#include "metadata/marshal-legacy.h"
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

/* TODO: dedup */
static GENERATE_GET_CLASS_WITH_CACHE (fixed_buffer_attribute, "System.Runtime.CompilerServices", "FixedBufferAttribute");
static GENERATE_GET_CLASS_WITH_CACHE (date_time, "System", "DateTime");
static GENERATE_TRY_GET_CLASS_WITH_CACHE (icustom_marshaler, "System.Runtime.InteropServices", "ICustomMarshaler");
static GENERATE_TRY_GET_CLASS_WITH_CACHE (marshal, "System.Runtime.InteropServices", "Marshal");


static MonoMethod*
get_method_nofail (MonoClass *klass, const char *method_name, int num_params, int flags);

static void
mono_mb_emit_exception_marshal_directive (MonoMethodBuilder *mb, char *msg);

/*TODO: figure out how to init these */
/* MonoMethod pointers to SafeHandle::DangerousAddRef and ::DangerousRelease */
static MonoMethod *sh_dangerous_add_ref;
static MonoMethod *sh_dangerous_release;

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


static void
init_safe_handle (void)
{
	mono_atomic_store_seq (&sh_dangerous_add_ref, get_method_nofail (mono_class_try_get_safehandle_class (), "DangerousAddRef", 1, 0));
	mono_atomic_store_seq (&sh_dangerous_release, get_method_nofail (mono_class_try_get_safehandle_class (), "DangerousRelease", 0, 0));
}

static void
mono_mb_emit_auto_layout_exception (MonoMethodBuilder *mb, MonoClass *klass)
{
	char *msg = g_strdup_printf ("The type `%s.%s' layout needs to be Sequential or Explicit", m_class_get_name_space (klass), m_class_get_name (klass));
	mono_mb_emit_exception_marshal_directive (mb, msg);
}

static gboolean cb_inited = FALSE;


static MonoJitICallId
conv_to_icall (MonoMarshalConv conv, int *ind_store_type);

static inline void
emit_string_free_icall (MonoMethodBuilder *mb, MonoMarshalConv conv)
{
	if (conv == MONO_MARSHAL_CONV_BSTR_STR || conv == MONO_MARSHAL_CONV_ANSIBSTR_STR || conv == MONO_MARSHAL_CONV_TBSTR_STR)
		mono_mb_emit_icall (mb, mono_free_bstr);
	else
		mono_mb_emit_icall (mb, mono_marshal_free);
}

/*TODO: dedup */
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



/*TODO: dedup */
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



static void
emit_struct_conv (MonoMethodBuilder *mb, MonoClass *klass, gboolean to_object);

static inline MonoJitICallId
mono_string_to_platform_unicode (void);

/*TODO: dedup */
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

/*TODO: dedup */
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

/* TODO: Dedup */
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

/*TODO: Dedup */
static int
offset_of_first_nonstatic_field (MonoClass *klass)
{
	int i;
	int fcount = mono_class_get_field_count (klass);
	mono_class_setup_fields (klass);
	MonoClassField *klass_fields = m_class_get_fields (klass);
	for (i = 0; i < fcount; i++) {
		if (!(klass_fields[i].type->attrs & FIELD_ATTRIBUTE_STATIC) && !mono_field_is_deleted (&klass_fields[i]))
			return klass_fields[i].offset - MONO_ABI_SIZEOF (MonoObject);
	}

	return 0;
}


static MonoMarshalCallbacksLegacy legacy_marshal_cb;
get_marshal_legacy_cb (void)
{
	if (G_UNLIKELY (!cb_inited)) {
#ifdef ENABLE_ILGEN
		mono_marshal_ilgen_legacy_init ();
#else
		mono_marshal_noilgen_legacy_init ();
#endif
	}
	return &legacy_marshal_cb;
}

/* TODO: This is duplicated in marshal-ilgen.c Need to move it somewhere shared */
MonoJitICallId
mono_string_to_platform_unicode (void);

static inline MonoJitICallId
mono_string_builder_to_platform_unicode (void);

static inline MonoJitICallId
mono_string_from_platform_unicode (void);

/* todo: deduplicate */
static inline MonoJitICallId
mono_string_builder_from_platform_unicode (void)
{
#ifdef TARGET_WIN32
	return MONO_JIT_ICALL_mono_string_utf16_to_builder;
#else
	return MONO_JIT_ICALL_mono_string_utf8_to_builder;
#endif
}

/*TODO: Dedupe */
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


/* todo: deduplicate */
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

/*TODO: deduplicate */
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

/* TODO: deduplicate */
static inline MonoJitICallId
mono_string_builder_to_platform_unicode (void)
{
#ifdef TARGET_WIN32
	return MONO_JIT_ICALL_mono_string_builder_to_utf16;
#else
	return MONO_JIT_ICALL_mono_string_builder_to_utf8;
#endif
}


/* TODO: Deduplicate */
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
			msize = m_class_get_instance_size (klass) - info->fields [i].field->offset;
			usize = info->native_size - info->fields [i].offset;
		} else {
			msize = info->fields [i + 1].field->offset - info->fields [i].field->offset;
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

/* TODO: deduplicate */
static void
emit_struct_conv (MonoMethodBuilder *mb, MonoClass *klass, gboolean to_object)
{
	emit_struct_conv_full (mb, klass, to_object, 0, (MonoMarshalNative)-1);
}

/* TODO: deduplicate */
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

/* TODO: deduplicate */
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

/* TODO: deduplicate */
static inline MonoJitICallId
mono_string_from_platform_unicode (void)
{
#ifdef TARGET_WIN32
	return MONO_JIT_ICALL_ves_icall_mono_string_from_utf16;
#else
	return MONO_JIT_ICALL_ves_icall_string_new_wrapper;
#endif
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
		if (!klass) {
			char *exception_msg = g_strdup ("Current profile doesn't support ICustomMarshaler");
			/* Throw exception and emit compensation code if neccesary */
			switch (action) {
			case MARSHAL_ACTION_CONV_IN:
			case MARSHAL_ACTION_CONV_RESULT:
			case MARSHAL_ACTION_MANAGED_CONV_RESULT:
				if ((action == MARSHAL_ACTION_CONV_RESULT) || (action == MARSHAL_ACTION_MANAGED_CONV_RESULT))
					mono_mb_emit_byte (mb, CEE_POP);

				mono_mb_emit_exception_full (mb, "System", "ApplicationException", exception_msg);

				break;
			case MARSHAL_ACTION_PUSH:
				mono_mb_emit_byte (mb, CEE_LDNULL);
				break;
			default:
				break;
			}
			return 0;
		}

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

	g_assert (mtype != NULL);
	mono_error_assert_ok (error);
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

		if (m_type_is_byref (t)) {
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

		emit_marshal_custom_get_instance (mb, mklass, spec);

		mono_mb_emit_ldloc (mb, conv_arg);

		mono_mb_emit_op (mb, CEE_CALLVIRT, cleanup_native);

		mono_mb_patch_branch (mb, pos2);
		break;

	case MARSHAL_ACTION_PUSH:
		if (m_type_is_byref (t))
			mono_mb_emit_ldloc_addr (mb, conv_arg);
		else
			mono_mb_emit_ldloc (mb, conv_arg);
		break;

	case MARSHAL_ACTION_CONV_RESULT:
		loc1 = mono_mb_add_local (mb, int_type);

		mono_mb_emit_stloc (mb, 3);

		mono_mb_emit_ldloc (mb, 3);
		mono_mb_emit_stloc (mb, loc1);

		/* Check for null */
		mono_mb_emit_ldloc (mb, 3);
		pos2 = mono_mb_emit_branch (mb, CEE_BRFALSE);

		emit_marshal_custom_get_instance (mb, mklass, spec);
		mono_mb_emit_byte (mb, CEE_DUP);

		mono_mb_emit_ldloc (mb, 3);
		mono_mb_emit_op (mb, CEE_CALLVIRT, marshal_native_to_managed);
		mono_mb_emit_stloc (mb, 3);

		mono_mb_emit_ldloc (mb, loc1);
		mono_mb_emit_op (mb, CEE_CALLVIRT, cleanup_native);

		mono_mb_patch_branch (mb, pos2);
		break;

	case MARSHAL_ACTION_MANAGED_CONV_IN:
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

		/* Call CleanUpManagedData */
		emit_marshal_custom_get_instance (mb, mklass, spec);

		mono_mb_emit_ldloc (mb, conv_arg);
		mono_mb_emit_op (mb, CEE_CALLVIRT, cleanup_managed);

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

void
mono_marshal_ilgen_legacy_init (void)
{
	MonoMarshalCallbacksLegacy cb;
	cb.version = MONO_MARSHAL_CALLBACKS_VERSION;
	cb.emit_marshal_array = emit_marshal_array_ilgen;
	cb.emit_marshal_ptr = emit_marshal_ptr_ilgen;
	cb.emit_marshal_char = emit_marshal_char_ilgen;
	cb.emit_marshal_vtype = emit_marshal_vtype_ilgen;
	cb.emit_marshal_string = emit_marshal_string_ilgen;
	cb.emit_marshal_variant = emit_marshal_variant_ilgen;
	cb.emit_marshal_safehandle = emit_marshal_safehandle_ilgen;
	cb.emit_marshal_object = emit_marshal_object_ilgen;
	cb.emit_marshal_boolean = emit_marshal_boolean_ilgen;
	cb.emit_marshal_custom = emit_marshal_custom_ilgen;
	cb.emit_marshal_asany = emit_marshal_asany_ilgen;
	cb.emit_marshal_handleref = emit_marshal_handleref_ilgen;

#ifdef DISABLE_NONBLITTABLE
	mono_marshal_noilgen_init_blittable (&cb);
#endif
	mono_install_marshal_callbacks_legacy (&cb);
}


void
mono_install_marshal_callbacks_legacy (MonoMarshalCallbacksLegacy *cb)
{
	g_assert (!cb_inited);
	g_assert (cb->version == MONO_MARSHAL_CALLBACKS_VERSION);
	memcpy (&legacy_marshal_cb, cb, sizeof (MonoMarshalCallbacks));
	cb_inited = TRUE;
}


int
mono_emit_marshal_legacy (EmitMarshalContext *m, int argnum, MonoType *t,
	      MonoMarshalSpec *spec, int conv_arg,
	      MonoType **conv_arg_type, MarshalAction action, MonoMarshalCallbacks* cb)
{
	MonoMarshalCallbacksLegacy* legacy_marshal_cb;

	legacy_marshal_cb = get_marshal_legacy_cb();

	if (spec && spec->native == MONO_NATIVE_CUSTOM)
		return legacy_marshal_cb->emit_marshal_custom (m, argnum, t, spec, conv_arg, conv_arg_type, action);

	if (spec && spec->native == MONO_NATIVE_ASANY)
		return legacy_marshal_cb->emit_marshal_asany (m, argnum, t, spec, conv_arg, conv_arg_type, action);

	switch (t->type) {
	case MONO_TYPE_VALUETYPE:
		if (t->data.klass == mono_class_try_get_handleref_class ())
			return legacy_marshal_cb->emit_marshal_handleref (m, argnum, t, spec, conv_arg, conv_arg_type, action);

		return legacy_marshal_cb->emit_marshal_vtype (m, argnum, t, spec, conv_arg, conv_arg_type, action);
	case MONO_TYPE_STRING:
		return legacy_marshal_cb->emit_marshal_string (m, argnum, t, spec, conv_arg, conv_arg_type, action);
	case MONO_TYPE_CLASS:
	case MONO_TYPE_OBJECT:
#if !defined(DISABLE_COM)
		if (spec && spec->native == MONO_NATIVE_STRUCT)
			return legacy_marshal_cb->emit_marshal_variant (m, argnum, t, spec, conv_arg, conv_arg_type, action);
#endif

#if !defined(DISABLE_COM)
		if ((spec && (spec->native == MONO_NATIVE_IUNKNOWN ||
			spec->native == MONO_NATIVE_IDISPATCH ||
			spec->native == MONO_NATIVE_INTERFACE)) ||
			(t->type == MONO_TYPE_CLASS && mono_cominterop_is_interface(t->data.klass)))
			return mono_cominterop_emit_marshal_com_interface (m, argnum, t, spec, conv_arg, conv_arg_type, action);
		if (spec && (spec->native == MONO_NATIVE_SAFEARRAY) &&
			(spec->data.safearray_data.elem_type == MONO_VARIANT_VARIANT) &&
			((action == MARSHAL_ACTION_CONV_OUT) || (action == MARSHAL_ACTION_CONV_IN) || (action == MARSHAL_ACTION_PUSH)))
			return mono_cominterop_emit_marshal_safearray (m, argnum, t, spec, conv_arg, conv_arg_type, action);
#endif

		if (mono_class_try_get_safehandle_class () != NULL && t->data.klass &&
		    mono_class_is_subclass_of_internal (t->data.klass,  mono_class_try_get_safehandle_class (), FALSE))
			return legacy_marshal_cb->emit_marshal_safehandle (m, argnum, t, spec, conv_arg, conv_arg_type, action);

		return legacy_marshal_cb->emit_marshal_object (m, argnum, t, spec, conv_arg, conv_arg_type, action);
	case MONO_TYPE_ARRAY:
	case MONO_TYPE_SZARRAY:
		return legacy_marshal_cb->emit_marshal_array (m, argnum, t, spec, conv_arg, conv_arg_type, action);
	case MONO_TYPE_BOOLEAN:
		return legacy_marshal_cb->emit_marshal_boolean (m, argnum, t, spec, conv_arg, conv_arg_type, action);
	case MONO_TYPE_PTR:
		return legacy_marshal_cb->emit_marshal_ptr (m, argnum, t, spec, conv_arg, conv_arg_type, action);
	case MONO_TYPE_CHAR:
		return legacy_marshal_cb->emit_marshal_char (m, argnum, t, spec, conv_arg, conv_arg_type, action);
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
		return cb->emit_marshal_scalar (m, argnum, t, spec, conv_arg, conv_arg_type, action);
	case MONO_TYPE_GENERICINST:
		if (mono_type_generic_inst_is_valuetype (t))
			return legacy_marshal_cb->emit_marshal_vtype (m, argnum, t, spec, conv_arg, conv_arg_type, action);
		else
			return legacy_marshal_cb->emit_marshal_object (m, argnum, t, spec, conv_arg, conv_arg_type, action);
	default:
		return conv_arg;
	}

}