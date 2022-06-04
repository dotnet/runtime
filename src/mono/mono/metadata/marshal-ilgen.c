#include "mono/metadata/debug-helpers.h"
#include "metadata/marshal.h"
#include "metadata/marshal-ilgen.h"
#include "metadata/marshal-lightweight.h"
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

static GENERATE_GET_CLASS_WITH_CACHE (date_time, "System", "DateTime");
static GENERATE_TRY_GET_CLASS_WITH_CACHE (icustom_marshaler, "System.Runtime.InteropServices", "ICustomMarshaler");

static void emit_string_free_icall (MonoMethodBuilder *mb, MonoMarshalConv conv);

// TODO: Does this need to loose the mono_ prefix?
static void mono_marshal_ilgen_legacy_init (void);

static gboolean ilgen_cb_inited = FALSE;
static MonoMarshalIlgenCallbacks ilgen_marshal_cb;

void
mono_install_marshal_callbacks_ilgen (MonoMarshalIlgenCallbacks *cb)
{
	g_assert (!ilgen_cb_inited);
	g_assert (cb->version == MONO_MARSHAL_CALLBACKS_VERSION);
	memcpy (&ilgen_marshal_cb, cb, sizeof (MonoMarshalIlgenCallbacks));
	ilgen_cb_inited = TRUE;
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
			mono_mb_emit_icall_id (mb, mono_marshal_shared_conv_to_icall (MONO_MARSHAL_CONV_ARRAY_LPARRAY, NULL));
			mono_mb_emit_stloc (mb, conv_arg);
		} else {
#ifdef DISABLE_NONBLITTABLE
			char *msg = g_strdup ("Non-blittable marshalling conversion is disabled");
			mono_marshal_shared_mb_emit_exception_marshal_directive (mb, msg);
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
				mono_marshal_shared_mb_emit_exception_marshal_directive (mb, msg);
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
				mono_mb_emit_icall_id (mb, mono_marshal_shared_conv_to_icall (conv, &stind_op));
				mono_mb_emit_byte (mb, GINT_TO_UINT8 (stind_op));
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
				mono_marshal_shared_emit_struct_conv_full (mb, eklass, FALSE, 0, eklass == mono_defaults.char_class ? encoding : (MonoMarshalNative)-1);
			}

			mono_mb_emit_add_to_local (mb, GINT_TO_UINT16 (index_var), 1);
			mono_mb_emit_add_to_local (mb, GINT_TO_UINT16 (dest_ptr), esize);

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
				mono_marshal_shared_mb_emit_exception_marshal_directive (mb, msg);
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

				mono_mb_emit_icall_id (mb, mono_marshal_shared_conv_to_icall (conv, NULL));

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
					mono_marshal_shared_emit_struct_conv_full (mb, eklass, TRUE, 0, eklass == mono_defaults.char_class ? encoding : (MonoMarshalNative)-1);
				}

				if (need_free) {
					mono_mb_emit_ldloc (mb, src_ptr);
					mono_mb_emit_stloc (mb, loc);

					emit_struct_free (mb, eklass, loc);
				}
			}

			mono_mb_emit_add_to_local (mb, GINT_TO_UINT16 (index_var), 1);
			mono_mb_emit_add_to_local (mb, GINT_TO_UINT16 (src_ptr), esize);

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
			mono_mb_emit_icall_id (mb, mono_marshal_shared_conv_to_icall (MONO_MARSHAL_FREE_LPARRAY, NULL));
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
		mono_marshal_shared_mb_emit_exception_marshal_directive (mb, msg);
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
			mono_marshal_shared_mb_emit_exception_marshal_directive (mb, msg);
			return conv_arg;
		}
		if (!spec) {
			char *msg = g_strdup ("[MarshalAs] attribute required to marshal arrays to managed code.");
			mono_marshal_shared_mb_emit_exception_marshal_directive (mb, msg);
			return conv_arg;
		}

		switch (spec->native) {
		case MONO_NATIVE_LPARRAY:
			break;
		case MONO_NATIVE_SAFEARRAY:
#ifndef DISABLE_COM
			if (spec->data.safearray_data.elem_type != MONO_VARIANT_VARIANT) {
				char *msg = g_strdup ("Only SAFEARRAY(VARIANT) marshalling to managed code is implemented.");
				mono_marshal_shared_mb_emit_exception_marshal_directive (mb, msg);
				return conv_arg;
			}
			return mono_cominterop_emit_marshal_safearray (m, argnum, t, spec, conv_arg, conv_arg_type, action);
#endif
		default: {
			char *msg = g_strdup ("Unsupported array type marshalling to managed code.");
			mono_marshal_shared_mb_emit_exception_marshal_directive (mb, msg);
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
				mono_marshal_shared_mb_emit_exception_marshal_directive (mb, msg);
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
				mono_marshal_shared_mb_emit_exception_marshal_directive (mb, msg);
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
				mono_marshal_shared_mb_emit_exception_marshal_directive (mb, msg);
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
			mono_marshal_shared_mb_emit_exception_marshal_directive (mb, msg);
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

			mono_mb_emit_icall_id (mb, mono_marshal_shared_conv_to_icall (conv, NULL));
			mono_mb_emit_byte (mb, CEE_STELEM_REF);
		}
		else {
			char *msg = g_strdup ("Marshalling of non-string and non-blittable arrays to managed code is not implemented.");
			mono_marshal_shared_mb_emit_exception_marshal_directive (mb, msg);
			return conv_arg;
		}

		mono_mb_emit_add_to_local (mb, GINT_TO_UINT16 (index_var), 1);
		mono_mb_emit_add_to_local (mb, GINT_TO_UINT16 (src_ptr), esize);

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

			mono_mb_emit_icall_id (mb, mono_marshal_shared_conv_to_icall (conv, &stind_op));
			mono_mb_emit_byte (mb, GINT_TO_UINT8 (stind_op));
		}
		else {
			char *msg = g_strdup ("Marshalling of non-string and non-blittable arrays to managed code is not implemented.");
			mono_marshal_shared_mb_emit_exception_marshal_directive (mb, msg);
			return conv_arg;
		}

		mono_mb_emit_add_to_local (mb, GINT_TO_UINT16 (index_var), 1);
		mono_mb_emit_add_to_local (mb, GINT_TO_UINT16 (dest_ptr), esize);

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

			mono_mb_emit_icall_id (mb, mono_marshal_shared_conv_to_icall (conv, &stind_op));
			mono_mb_emit_byte (mb, GINT_TO_UINT8 (stind_op));
		}
		else {
			char *msg = g_strdup ("Marshalling of non-string arrays to managed code is not implemented.");
			mono_marshal_shared_mb_emit_exception_marshal_directive (mb, msg);
			return conv_arg;
		}

		mono_mb_emit_add_to_local (mb, GINT_TO_UINT16 (index_var), 1);
		mono_mb_emit_add_to_local (mb, GINT_TO_UINT16 (dest), esize);

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

static gboolean
emit_native_wrapper_validate_signature (MonoMethodBuilder *mb, MonoMethodSignature* sig, MonoMarshalSpec** mspecs)
{
	if (mspecs) {
		for (int i = 0; i < sig->param_count; i ++) {
			if (mspecs [i + 1] && mspecs [i + 1]->native == MONO_NATIVE_CUSTOM) {
				if (!mspecs [i + 1]->data.custom_data.custom_name || *mspecs [i + 1]->data.custom_data.custom_name == '\0') {
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
			mono_marshal_shared_mb_emit_exception_marshal_directive (m->mb, msg);
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

		cleanup_native = mono_marshal_shared_get_method_nofail (klass, "CleanUpNativeData", 1, 0);
		g_assert (cleanup_native);

		cleanup_managed = mono_marshal_shared_get_method_nofail (klass, "CleanUpManagedData", 1, 0);
		g_assert (cleanup_managed);

		marshal_managed_to_native = mono_marshal_shared_get_method_nofail (klass, "MarshalManagedToNative", 1, 0);
		g_assert (marshal_managed_to_native);

		marshal_native_to_managed = mono_marshal_shared_get_method_nofail (klass, "MarshalNativeToManaged", 1, 0);
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

		mono_marshal_shared_emit_marshal_custom_get_instance (mb, mklass, spec);

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

			mono_marshal_shared_emit_marshal_custom_get_instance (mb, mklass, spec);
			mono_mb_emit_byte (mb, CEE_DUP);

			mono_mb_emit_ldarg (mb, argnum);
			mono_mb_emit_byte (mb, CEE_LDIND_REF);
			mono_mb_emit_op (mb, CEE_CALLVIRT, cleanup_managed);

			mono_mb_emit_ldloc (mb, conv_arg);
			mono_mb_emit_op (mb, CEE_CALLVIRT, marshal_native_to_managed);
			mono_mb_emit_byte (mb, CEE_STIND_REF);
		} else if (m_type_is_byref (t) && (t->attrs & PARAM_ATTRIBUTE_OUT)) {
			mono_mb_emit_ldarg (mb, argnum);

			mono_marshal_shared_emit_marshal_custom_get_instance (mb, mklass, spec);

			mono_mb_emit_ldloc (mb, conv_arg);
			mono_mb_emit_op (mb, CEE_CALLVIRT, marshal_native_to_managed);
			mono_mb_emit_byte (mb, CEE_STIND_REF);
		} else if (t->attrs & PARAM_ATTRIBUTE_OUT) {
			mono_marshal_shared_emit_marshal_custom_get_instance (mb, mklass, spec);

			mono_mb_emit_ldloc (mb, conv_arg);
			mono_mb_emit_op (mb, CEE_CALLVIRT, marshal_native_to_managed);
			/* We have nowhere to store the result */
			mono_mb_emit_byte (mb, CEE_POP);
		}

		// Only call cleanup_native if MARSHAL_ACTION_CONV_IN called marshal_managed_to_native.
		if (!(m_type_is_byref (t) && (t->attrs & PARAM_ATTRIBUTE_OUT)) &&
			!(!m_type_is_byref (t) && (t->attrs & PARAM_ATTRIBUTE_OUT) && !(t->attrs & PARAM_ATTRIBUTE_IN))) {
			mono_marshal_shared_emit_marshal_custom_get_instance (mb, mklass, spec);

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

		mono_marshal_shared_emit_marshal_custom_get_instance (mb, mklass, spec);

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

		mono_marshal_shared_emit_marshal_custom_get_instance (mb, mklass, spec);

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

		mono_marshal_shared_emit_marshal_custom_get_instance (mb, mklass, spec);
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

			mono_marshal_shared_emit_marshal_custom_get_instance (mb, mklass, spec);

			mono_mb_emit_ldloc (mb, conv_arg);
			mono_mb_emit_op (mb, CEE_CALLVIRT, marshal_managed_to_native);
			mono_mb_emit_byte (mb, CEE_STIND_I);
		}

		// Only call cleanup_managed if MARSHAL_ACTION_MANAGED_CONV_IN called marshal_native_to_managed.
		if (!(m_type_is_byref (t) && (t->attrs & PARAM_ATTRIBUTE_OUT))) {
			mono_marshal_shared_emit_marshal_custom_get_instance (mb, mklass, spec);
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
					to_oadate = mono_marshal_shared_get_method_nofail (date_time_class, "ToOADate", 0, 0);
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
			mono_marshal_shared_emit_struct_conv (mb, klass, FALSE);
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
					from_oadate = mono_marshal_shared_get_method_nofail (date_time_class, "FromOADate", 1, 0);
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
				mono_marshal_shared_emit_struct_conv (mb, klass, TRUE);
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
		mono_marshal_shared_emit_struct_conv (mb, klass, TRUE);
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
		mono_marshal_shared_emit_struct_conv (mb, klass, TRUE);

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
		mono_marshal_shared_emit_struct_conv (mb, klass, FALSE);

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
		mono_marshal_shared_emit_struct_conv (mb, klass, FALSE);
		break;

	default:
		g_assert_not_reached ();
	}
	return conv_arg;
}

static void
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
			mono_marshal_shared_mb_emit_exception_marshal_directive (mb, msg);
		} else {
			mono_mb_emit_icall_id (mb, mono_marshal_shared_conv_to_icall (conv, NULL));

			mono_mb_emit_stloc (mb, conv_arg);
		}
		break;

	case MARSHAL_ACTION_CONV_OUT:
		conv = mono_marshal_get_ptr_to_string_conv (m->piinfo, spec, &need_free);
		if (conv == MONO_MARSHAL_CONV_INVALID) {
			char *msg = g_strdup_printf ("string marshalling conversion %d not implemented", encoding);
			mono_marshal_shared_mb_emit_exception_marshal_directive (mb, msg);
			break;
		}

		if (encoding == MONO_NATIVE_VBBYREFSTR) {

			if (!m_type_is_byref (t)) {
				char *msg = g_strdup ("VBByRefStr marshalling requires a ref parameter.");
				mono_marshal_shared_mb_emit_exception_marshal_directive (mb, msg);
				break;
			}

			MONO_STATIC_POINTER_INIT (MonoMethod, method)

				method = mono_marshal_shared_get_method_nofail (mono_defaults.string_class, "get_Length", -1, 0);

			MONO_STATIC_POINTER_INIT_END (MonoMethod, method)

			/*
			 * Have to allocate a new string with the same length as the original, and
			 * copy the contents of the buffer pointed to by CONV_ARG into it.
			 */
			g_assert (m_type_is_byref (t));
			mono_mb_emit_ldarg (mb, argnum);
			mono_mb_emit_ldloc (mb, conv_arg);
			mono_mb_emit_ldarg (mb, argnum);
			mono_mb_emit_byte (mb, CEE_LDIND_I);
			mono_mb_emit_managed_call (mb, method, NULL);
			mono_mb_emit_icall (mb, mono_string_new_len_wrapper);
			mono_mb_emit_byte (mb, CEE_STIND_REF);
		} else if (m_type_is_byref (t) && (t->attrs & PARAM_ATTRIBUTE_OUT || !(t->attrs & PARAM_ATTRIBUTE_IN))) {
			int stind_op;
			mono_mb_emit_ldarg (mb, argnum);
			mono_mb_emit_ldloc (mb, conv_arg);
			mono_mb_emit_icall_id (mb, mono_marshal_shared_conv_to_icall (conv, &stind_op));
			mono_mb_emit_byte (mb, GINT_TO_UINT8 (stind_op));
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
			mono_marshal_shared_mb_emit_exception_marshal_directive (mb, msg);
			break;
		}

		mono_mb_emit_ldloc (mb, 0);
		mono_mb_emit_icall_id (mb, mono_marshal_shared_conv_to_icall (conv, NULL));
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
			mono_marshal_shared_mb_emit_exception_marshal_directive (mb, msg);
			break;
		}

		mono_mb_emit_ldarg (mb, argnum);
		if (m_type_is_byref (t))
			mono_mb_emit_byte (mb, CEE_LDIND_I);
		mono_mb_emit_icall_id (mb, mono_marshal_shared_conv_to_icall (conv, NULL));
		mono_mb_emit_stloc (mb, conv_arg);
		break;

	case MARSHAL_ACTION_MANAGED_CONV_OUT:
		if (m_type_is_byref (t)) {
			if (conv_arg) {
				int stind_op;
				mono_mb_emit_ldarg (mb, argnum);
				mono_mb_emit_ldloc (mb, conv_arg);
				mono_mb_emit_icall_id (mb, mono_marshal_shared_conv_to_icall (conv, &stind_op));
				mono_mb_emit_byte (mb, GINT_TO_UINT8 (stind_op));
			}
		}
		break;

	case MARSHAL_ACTION_MANAGED_CONV_RESULT:
		if (mono_marshal_shared_conv_to_icall (conv, NULL) == MONO_JIT_ICALL_mono_marshal_string_to_utf16)
			/* We need to make a copy so the caller is able to free it */
			mono_mb_emit_icall (mb, mono_marshal_string_to_utf16_copy);
		else
			mono_mb_emit_icall_id (mb, mono_marshal_shared_conv_to_icall (conv, NULL));
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

		if (!*mono_marshal_shared_get_sh_dangerous_add_ref())
			mono_marshal_shared_init_safe_handle ();

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

			if (!mono_marshal_shared_is_in (t)) {
				mono_mb_emit_icon (mb, 0);
				mono_mb_emit_stloc (mb, conv_arg);
			} else {
				/* safehandle.DangerousAddRef (ref release) */
				mono_mb_emit_ldarg (mb, argnum);
				mono_mb_emit_byte (mb, CEE_LDIND_REF);
				mono_mb_emit_ldloc_addr (mb, dar_release_slot);
				mono_mb_emit_managed_call (mb, *mono_marshal_shared_get_sh_dangerous_add_ref(), NULL);

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
			mono_mb_emit_managed_call (mb, *mono_marshal_shared_get_sh_dangerous_add_ref(), NULL);

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

		if (!*mono_marshal_shared_get_sh_dangerous_release())
			mono_marshal_shared_init_safe_handle ();

		if (m_type_is_byref (t)) {
			/* If there was SafeHandle on input we have to release the reference to it */
			if (mono_marshal_shared_is_in (t)) {
				mono_mb_emit_ldloc (mb, dar_release_slot);
				label_next = mono_mb_emit_branch (mb, CEE_BRFALSE);
				mono_mb_emit_ldarg (mb, argnum);
				mono_mb_emit_byte (mb, CEE_LDIND_I);
				mono_mb_emit_managed_call (mb, *mono_marshal_shared_get_sh_dangerous_release (), NULL);
				mono_mb_patch_branch (mb, label_next);
			}

			if (mono_marshal_shared_is_out (t)) {
				ERROR_DECL (local_error);
				MonoMethod *ctor;

				/*
				 * If the SafeHandle was marshalled on input we can skip the marshalling on
				 * output if the handle value is identical.
				 */
				if (mono_marshal_shared_is_in (t)) {
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

				if (mono_marshal_shared_is_in (t) && label_next) {
					mono_mb_patch_branch (mb, label_next);
				}
			}
		} else {
			mono_mb_emit_ldloc (mb, dar_release_slot);
			label_next = mono_mb_emit_branch (mb, CEE_BRFALSE);
			mono_mb_emit_ldarg (mb, argnum);
			mono_mb_emit_managed_call (mb, *mono_marshal_shared_get_sh_dangerous_release (), NULL);
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
			mono_marshal_shared_mb_emit_exception_marshal_directive (mb, g_strdup ("Returned SafeHandles should not be abstract"));
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
			mono_marshal_shared_mb_emit_exception_marshal_directive (mb, msg);
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
		mono_marshal_shared_mb_emit_exception_marshal_directive (mb, msg);
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
			mono_marshal_shared_mb_emit_exception_marshal_directive (mb, msg);
			break;
		}

		if (m_class_is_delegate (klass)) {
			if (m_type_is_byref (t)) {
				if (!(t->attrs & PARAM_ATTRIBUTE_OUT)) {
					char *msg = g_strdup_printf ("Byref marshalling of delegates is not implemented.");
					mono_marshal_shared_mb_emit_exception_marshal_directive (mb, msg);
				}
				mono_mb_emit_byte (mb, CEE_LDNULL);
				mono_mb_emit_stloc (mb, conv_arg);
			} else {
				mono_mb_emit_ldarg (mb, argnum);
				mono_mb_emit_icall_id (mb, mono_marshal_shared_conv_to_icall (MONO_MARSHAL_CONV_DEL_FTN, NULL));
				mono_mb_emit_stloc (mb, conv_arg);
			}
		} else if (klass == mono_class_try_get_stringbuilder_class ()) {
			MonoMarshalNative encoding = mono_marshal_get_string_encoding (m->piinfo, spec);
			MonoMarshalConv conv = mono_marshal_get_stringbuilder_to_ptr_conv (m->piinfo, spec);

#if 0
			if (m_type_is_byref (t)) {
				if (!(t->attrs & PARAM_ATTRIBUTE_OUT)) {
					char *msg = g_strdup_printf ("Byref marshalling of stringbuilders is not implemented.");
					mono_marshal_shared_mb_emit_exception_marshal_directive (mb, msg);
				}
				break;
			}
#endif

			if (m_type_is_byref (t) && !(t->attrs & PARAM_ATTRIBUTE_IN) && (t->attrs & PARAM_ATTRIBUTE_OUT))
				break;

			if (conv == MONO_MARSHAL_CONV_INVALID) {
				char *msg = g_strdup_printf ("stringbuilder marshalling conversion %d not implemented", encoding);
				mono_marshal_shared_mb_emit_exception_marshal_directive (mb, msg);
				break;
			}

			mono_mb_emit_ldarg (mb, argnum);
			if (m_type_is_byref (t))
				mono_mb_emit_byte (mb, CEE_LDIND_I);

			mono_mb_emit_icall_id (mb, mono_marshal_shared_conv_to_icall (conv, NULL));
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
			mono_marshal_shared_emit_struct_conv (mb, klass, FALSE);

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

				mono_mb_emit_icall_id (mb, mono_marshal_shared_conv_to_icall (conv, NULL));
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
				mono_mb_emit_icall_id (mb, mono_marshal_shared_conv_to_icall (MONO_MARSHAL_CONV_FTN_DEL, NULL));
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
			mono_marshal_shared_emit_struct_conv (mb, klass, TRUE);

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
			mono_mb_emit_icall_id (mb, mono_marshal_shared_conv_to_icall (MONO_MARSHAL_CONV_FTN_DEL, NULL));
			mono_mb_emit_stloc (mb, 3);
		} else if (klass == mono_class_try_get_stringbuilder_class ()) {
			// FIXME:
			char *msg = g_strdup_printf ("Return marshalling of stringbuilders is not implemented.");
			mono_marshal_shared_mb_emit_exception_marshal_directive (mb, msg);
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
			mono_marshal_shared_emit_struct_conv (mb, klass, TRUE);

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
			mono_mb_emit_icall_id (mb, mono_marshal_shared_conv_to_icall (MONO_MARSHAL_CONV_FTN_DEL, NULL));
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
		mono_marshal_shared_emit_struct_conv (mb, klass, TRUE);

		mono_mb_patch_branch (mb, pos);
		break;

	case MARSHAL_ACTION_MANAGED_CONV_OUT:
		if (m_class_is_delegate (klass)) {
			if (m_type_is_byref (t)) {
				int stind_op;
				mono_mb_emit_ldarg (mb, argnum);
				mono_mb_emit_ldloc (mb, conv_arg);
				mono_mb_emit_icall_id (mb, mono_marshal_shared_conv_to_icall (MONO_MARSHAL_CONV_DEL_FTN, &stind_op));
				mono_mb_emit_byte (mb, GINT_TO_UINT8 (stind_op));
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
			mono_marshal_shared_emit_struct_conv (mb, klass, FALSE);

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
			mono_marshal_shared_emit_struct_conv (mb, klass, FALSE);
		}
		break;

	case MARSHAL_ACTION_MANAGED_CONV_RESULT:
		if (m_class_is_delegate (klass)) {
			mono_mb_emit_icall_id (mb, mono_marshal_shared_conv_to_icall (MONO_MARSHAL_CONV_DEL_FTN, NULL));
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

		mono_marshal_shared_emit_struct_conv (mb, klass, FALSE);

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
		mono_marshal_shared_mb_emit_exception_marshal_directive (mb, msg);
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
		mono_marshal_shared_mb_emit_exception_marshal_directive (mb, msg);
		break;
	}

	default:
		g_assert_not_reached ();
	}
#endif /* DISABLE_COM */

	return conv_arg;
}

static MonoMarshalIlgenCallbacks *
get_marshal_cb (void)
{
	if (G_UNLIKELY (!ilgen_cb_inited)) {
#ifdef ENABLE_ILGEN
		mono_marshal_ilgen_init ();
#else
		mono_marshal_noilgen_init_heavyweight ();
#endif
	}
	return &ilgen_marshal_cb;
}

int
mono_emit_marshal_ilgen (EmitMarshalContext *m, int argnum, MonoType *t,
	      MonoMarshalSpec *spec, int conv_arg,
	      MonoType **conv_arg_type, MarshalAction action, MonoMarshalLightweightCallbacks* lightweigth_cb)
{
	if (spec && spec->native == MONO_NATIVE_CUSTOM)
		return get_marshal_cb ()->emit_marshal_custom (m, argnum, t, spec, conv_arg, conv_arg_type, action);

	if (spec && spec->native == MONO_NATIVE_ASANY)
		return get_marshal_cb ()->emit_marshal_asany (m, argnum, t, spec, conv_arg, conv_arg_type, action);

	switch (t->type) {
	case MONO_TYPE_VALUETYPE:
		if (t->data.klass == mono_class_try_get_handleref_class ())
			return get_marshal_cb ()->emit_marshal_handleref (m, argnum, t, spec, conv_arg, conv_arg_type, action);

		return get_marshal_cb ()->emit_marshal_vtype (m, argnum, t, spec, conv_arg, conv_arg_type, action);
	case MONO_TYPE_STRING:
		return get_marshal_cb ()->emit_marshal_string (m, argnum, t, spec, conv_arg, conv_arg_type, action);
	case MONO_TYPE_CLASS:
	case MONO_TYPE_OBJECT:
#if !defined(DISABLE_COM)
		if (spec && spec->native == MONO_NATIVE_STRUCT)
			return get_marshal_cb ()->emit_marshal_variant (m, argnum, t, spec, conv_arg, conv_arg_type, action);
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
			return get_marshal_cb ()->emit_marshal_safehandle (m, argnum, t, spec, conv_arg, conv_arg_type, action);

		return get_marshal_cb ()->emit_marshal_object (m, argnum, t, spec, conv_arg, conv_arg_type, action);
	case MONO_TYPE_ARRAY:
	case MONO_TYPE_SZARRAY:
		return get_marshal_cb ()->emit_marshal_array (m, argnum, t, spec, conv_arg, conv_arg_type, action);
	case MONO_TYPE_BOOLEAN:
		return get_marshal_cb ()->emit_marshal_boolean (m, argnum, t, spec, conv_arg, conv_arg_type, action);
	case MONO_TYPE_PTR:
		return get_marshal_cb ()->emit_marshal_ptr (m, argnum, t, spec, conv_arg, conv_arg_type, action);
	case MONO_TYPE_CHAR:
		return get_marshal_cb ()->emit_marshal_char (m, argnum, t, spec, conv_arg, conv_arg_type, action);
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
		return lightweigth_cb->emit_marshal_scalar (m, argnum, t, spec, conv_arg, conv_arg_type, action);
	case MONO_TYPE_GENERICINST:
		if (mono_type_generic_inst_is_valuetype (t))
			return get_marshal_cb ()->emit_marshal_vtype (m, argnum, t, spec, conv_arg, conv_arg_type, action);
		else
			return get_marshal_cb ()->emit_marshal_object (m, argnum, t, spec, conv_arg, conv_arg_type, action);
	default:
		return conv_arg;
	}
}

void
mono_marshal_ilgen_init (void)
{
	MonoMarshalIlgenCallbacks cb;
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
	mono_install_marshal_callbacks_ilgen (&cb);
}


