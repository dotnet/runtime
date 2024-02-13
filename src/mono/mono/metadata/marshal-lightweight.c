/**
 * \file
 * Copyright 2018 Microsoft
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */
#include "config.h"
#ifdef HAVE_ALLOCA_H
#include <alloca.h>
#endif

#include "mono/metadata/method-builder-ilgen.h"
#include "mono/metadata/method-builder-ilgen-internals.h"
#include <mono/metadata/object.h>
#include <mono/metadata/loader.h>
#include "cil-coff.h"
#include "metadata/marshal.h"
#include "metadata/marshal-internals.h"
#include "metadata/marshal-lightweight.h"
#include "metadata/marshal-shared.h"
#include "metadata/tabledefs.h"
#include <mono/metadata/exception.h>
#include <mono/metadata/appdomain.h>
#include "mono/metadata/abi-details.h"
#include "mono/metadata/class-abi-details.h"
#include "mono/metadata/class-init.h"
#include "mono/metadata/components.h"
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
#include "mono/metadata/reflection-internals.h"
#include "mono/metadata/handle.h"
#include "mono/metadata/custom-attrs-internals.h"
#include "mono/metadata/icall-internals.h"
#include "mono/metadata/unsafe-accessor.h"
#include "mono/utils/mono-tls.h"
#include "mono/utils/mono-memory-model.h"
#include "mono/utils/atomic.h"
#include <mono/utils/mono-threads.h>
#include <mono/utils/mono-threads-coop.h>
#include <mono/utils/mono-error-internals.h>
#include <mono/utils/options.h>
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

static GENERATE_GET_CLASS_WITH_CACHE (date_time, "System", "DateTime");
static GENERATE_TRY_GET_CLASS_WITH_CACHE (icustom_marshaler, "System.Runtime.InteropServices", "ICustomMarshaler");

static MonoImage*
get_method_image (MonoMethod *method)
{
	return m_class_get_image (method->klass);
}

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

// FIXME There are multiple caches of "GetObjectForNativeVariant".
G_GNUC_UNUSED
static MonoMethod*
mono_get_Marshal_GetObjectForNativeVariant (void)
{
	MONO_STATIC_POINTER_INIT (MonoMethod, get_object_for_native_variant)
		get_object_for_native_variant = mono_marshal_shared_get_method_nofail (mono_defaults.marshal_class, "GetObjectForNativeVariant", 1, 0);
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
		get_native_variant_for_object = mono_marshal_shared_get_method_nofail (mono_defaults.marshal_class, "GetNativeVariantForObject", 2, 0);
	MONO_STATIC_POINTER_INIT_END (MonoMethod, get_native_variant_for_object)

	g_assert (get_native_variant_for_object);
	return get_native_variant_for_object;
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
emit_thread_interrupt_checkpoint (MonoMethodBuilder *mb)
{
	// FIXME Put a boolean in MonoMethodBuilder instead.
	if (strstr (mb->name, "mono_thread_interruption_checkpoint"))
		return;

	mono_marshal_shared_emit_thread_interrupt_checkpoint_call (mb, MONO_JIT_ICALL_mono_thread_interruption_checkpoint);
}

static void
emit_thread_force_interrupt_checkpoint (MonoMethodBuilder *mb)
{
	mono_marshal_shared_emit_thread_interrupt_checkpoint_call (mb, MONO_JIT_ICALL_mono_thread_force_interruption_checkpoint_noraise);
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
			mono_mb_emit_add_to_local (mb, GINT_TO_UINT16 (tmp_var), TARGET_SIZEOF_VOID_P);

	}

	for (i = 0; i < sig->param_count; i++) {
		mono_mb_emit_ldloc (mb, tmp_var);
		mono_mb_emit_ldarg_addr (mb, i + sig->hasthis);
		mono_mb_emit_byte (mb, CEE_STIND_I);
		/* tmp = tmp + sizeof (gpointer) */
		if (i < (sig->param_count - 1))
			mono_mb_emit_add_to_local (mb, GINT_TO_UINT16 (tmp_var), TARGET_SIZEOF_VOID_P);
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
			mono_mb_emit_op (mb, CEE_LDOBJ, mono_class_from_mono_type_internal (sig->params [i]));
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
		guint8 ldind_op;
		MonoType* ret_byval = m_class_get_byval_arg (mono_class_from_mono_type_internal (sig->ret));
		g_assert (!m_type_is_byref (ret_byval));
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
	case MONO_TYPE_FNPTR:
		/* The result is an IntPtr */
		mono_mb_emit_op (mb, CEE_BOX, mono_defaults.int_class);
		break;
	default:
		g_assert_not_reached ();
	}

	if (!void_ret)
		mono_mb_emit_stloc (mb, loc_res);
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

	mono_mb_emit_ldloc (mb, loc_res);
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

typedef struct EmitGCSafeTransitionBuilder {
	MonoMethodBuilder *mb;
	gboolean func_param;
	int coop_gc_var;
} GCSafeTransitionBuilder;

static gboolean
gc_safe_transition_builder_init (GCSafeTransitionBuilder *builder, MonoMethodBuilder *mb, gboolean func_param)
{
	builder->mb = mb;
	builder->func_param = func_param;
	builder->coop_gc_var = -1;
#if defined (TARGET_WASM)
	#ifndef DISABLE_THREADS
		return TRUE;
	#else
		/* if we're in the AOT compiler, obey the --wasm-gc-safepoints option even if the AOT compiler doesn't have threads enabled */
		return mono_opt_wasm_gc_safepoints;
	#endif
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
	if (!builder->func_param && aot) {
		mono_mb_emit_byte (builder->mb, MONO_CUSTOM_PREFIX);
		mono_mb_emit_op (builder->mb, CEE_MONO_ICALL_ADDR, method);
		mono_mb_emit_byte (builder->mb, CEE_POP); // Result not needed yet
	}

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
}

typedef struct EmitGCUnsafeTransitionBuilder {
	MonoMethodBuilder *mb;
	int orig_domain_var;
	int attach_cookie_var;
} GCUnsafeTransitionBuilder;

static void
gc_unsafe_transition_builder_init (GCUnsafeTransitionBuilder *builder, MonoMethodBuilder *mb, gboolean use_attach)
{
	g_assert_checked (use_attach);
	// Right now we always set use_attach and use mono_threads_coop_attach to enter into gc
	// unsafe regions.  If !use_attach is needed (ie adding transitions, using
	// mono_threads_enter_gc_unsafe_region_unbalanced) that needs to be implemented.
	builder->mb = mb;
	builder->orig_domain_var = -1;
	builder->attach_cookie_var = -1;
}

static void
gc_unsafe_transition_builder_add_vars (GCUnsafeTransitionBuilder *builder)
{
	MonoType *int_type = mono_get_int_type ();
	builder->orig_domain_var = mono_mb_add_local (builder->mb, int_type);
	builder->attach_cookie_var = mono_mb_add_local (builder->mb, int_type);
}

static void
gc_unsafe_transition_builder_emit_enter (GCUnsafeTransitionBuilder *builder)
{
	MonoMethodBuilder *mb = builder->mb;
	int attach_cookie = builder->attach_cookie_var;
	int orig_domain = builder->orig_domain_var;
	/*
	 * // does (STARTING|RUNNING|BLOCKING) -> RUNNING + set/switch domain
	 * intptr_t attach_cookie;
	 * intptr_t orig_domain = mono_threads_attach_coop (domain, &attach_cookie);
	 * <interrupt check>
	 */
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
}

static void
gc_unsafe_transition_builder_emit_exit (GCUnsafeTransitionBuilder *builder)
{
	MonoMethodBuilder *mb = builder->mb;
	int orig_domain = builder->orig_domain_var;
	int attach_cookie = builder->attach_cookie_var;
	/*
	 * // does RUNNING -> (RUNNING|BLOCKING) + unset/switch domain
	 * mono_threads_detach_coop (orig_domain, &attach_cookie);
	 */

	/* mono_threads_detach_coop (orig_domain, &attach_cookie); */
	mono_mb_emit_ldloc (mb, orig_domain);
	mono_mb_emit_ldloc_addr (mb, attach_cookie);
	/* Special cased in interpreter, keep in sync */
	mono_mb_emit_icall (mb, mono_threads_detach_coop);
}

static void
gc_unsafe_transition_builder_cleanup (GCUnsafeTransitionBuilder *builder)
{
	builder->mb = NULL;
	builder->orig_domain_var = -1;
	builder->attach_cookie_var = -1;
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
				for (guint32 field_idx = 0; field_idx < marshal_type->num_fields; ++field_idx) {
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
	g_assert (!MONO_CLASS_IS_IMPORT (mb->method->klass));
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
	/* we allocate local for use with mono_marshal_shared_emit_struct_conv() */
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

	if (!func && !aot && !func_param) {
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
		mono_marshal_shared_mb_emit_exception_marshal_directive(mb, g_strdup("Setting SetLastError=true is not supported when runtime marshalling is disabled."));

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
		klass = mono_class_from_mono_type_internal (sig->ret);
		mono_class_init_internal (klass);
		if (!(mono_class_is_explicit_layout (klass) || m_class_is_blittable (klass))) {
			/* TODO: marshal-lightweight: can this move to marshal-ilgen? */
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
	mono_mb_emit_ldflda (mb, GINTPTR_TO_INT32 (m_class_offsetof_element_class ()));
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
		mono_mb_emit_ldflda (mb, GINTPTR_TO_INT32 (m_class_offsetof_idepth ()));
		mono_mb_emit_byte (mb, CEE_LDIND_U2);

		mono_mb_emit_ldloc (mb, aklass);
		mono_mb_emit_ldflda (mb, GINTPTR_TO_INT32 (m_class_offsetof_idepth ()));
		mono_mb_emit_byte (mb, CEE_LDIND_U2);

		b3 = mono_mb_emit_branch (mb, CEE_BLT_UN);

		/* if (vklass->supertypes [aklass->idepth - 1] != aklass) goto failure */
		mono_mb_emit_ldloc (mb, vklass);
		mono_mb_emit_ldflda (mb, GINTPTR_TO_INT32 (m_class_offsetof_supertypes ()));
		mono_mb_emit_byte (mb, CEE_LDIND_I);

		mono_mb_emit_ldloc (mb, aklass);
		mono_mb_emit_ldflda (mb, GINTPTR_TO_INT32 (m_class_offsetof_idepth ()));
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
		mono_mb_emit_ldflda (mb, GINTPTR_TO_INT32 (m_class_offsetof_supertypes ()));
		mono_mb_emit_byte (mb, CEE_LDIND_I);

		mono_mb_emit_ldloc (mb, aklass);
		mono_mb_emit_ldflda (mb, GINTPTR_TO_INT32 (m_class_offsetof_idepth ()));
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
		mono_mb_emit_ldflda (mb, GINTPTR_TO_INT32 (m_class_offsetof_interface_id ()));
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
	mono_mb_emit_ldflda (mb, GINTPTR_TO_INT32 (m_class_offsetof_element_class ()));
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
	mono_mb_emit_ldflda (mb, GINTPTR_TO_INT32 (m_class_offsetof_idepth ()));
	mono_mb_emit_byte (mb, CEE_LDIND_U2);

	mono_mb_emit_ldloc (mb, aklass);
	mono_mb_emit_ldflda (mb, GINTPTR_TO_INT32 (m_class_offsetof_idepth ()));
	mono_mb_emit_byte (mb, CEE_LDIND_U2);

	b2 = mono_mb_emit_branch (mb, CEE_BLT_UN);

	/* if (vklass->supertypes [aklass->idepth - 1] != aklass) goto failure */
	mono_mb_emit_ldloc (mb, vklass);
	mono_mb_emit_ldflda (mb, GINTPTR_TO_INT32 (m_class_offsetof_supertypes ()));
	mono_mb_emit_byte (mb, CEE_LDIND_I);

	mono_mb_emit_ldloc (mb, aklass);
	mono_mb_emit_ldflda (mb, GINTPTR_TO_INT32 (m_class_offsetof_idepth ()));
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
	/* if (ind >= bounds [0].length) goto exception; */
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
		/* if (realidx >= bounds [i].length) goto exception; */
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
		mono_mb_emit_icon (mb, GINTPTR_TO_INT32 (m_class_offsetof_sizes ()));
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

#define MONO_TYPE_IS_PRIMITIVE(t) ((!m_type_is_byref ((t)) && ((((t)->type >= MONO_TYPE_BOOLEAN && (t)->type <= MONO_TYPE_R8) || ((t)->type >= MONO_TYPE_I && (t)->type <= MONO_TYPE_U)))))

static void
emit_delegate_invoke_internal_ilgen (MonoMethodBuilder *mb, MonoMethodSignature *sig, MonoMethodSignature *invoke_sig, MonoMethodSignature *target_method_sig, gboolean static_method_with_first_arg_bound, gboolean callvirt, gboolean closed_over_null, MonoMethod *method, MonoMethod *target_method, MonoGenericContext *ctx, MonoGenericContainer *container)
{
	int local_i, local_len, local_delegates, local_d, local_target, local_res = 0;
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
		local_res = mono_mb_add_local (mb, sig->ret);

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
		/* if bound */
		mono_mb_emit_ldarg (mb, 0);
		mono_mb_emit_ldflda (mb, MONO_STRUCT_OFFSET (MonoDelegate, bound));
		/* bound: MonoBoolean */
		mono_mb_emit_byte (mb, CEE_LDIND_I1);
		int pos_bound = mono_mb_emit_branch (mb, CEE_BRTRUE);

		/* if target != null */
		mono_mb_emit_ldloc (mb, local_target);
		pos0 = mono_mb_emit_branch (mb, CEE_BRFALSE);

		mono_mb_patch_branch (mb, pos_bound);

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
			for (i = 1; i <= sig->param_count; ++i) {
				mono_mb_emit_ldarg (mb, i);
				if (i == 1) {
					MonoType *t = sig->params [0];
					if (!m_type_is_byref (t))
						mono_mb_emit_op (mb, CEE_BOX, mono_class_from_mono_type_internal (t));
				}
			}
			mono_mb_emit_ldarg_addr (mb, 1);
			mono_mb_emit_ldarg (mb, 0);
			mono_mb_emit_icall (mb, mono_get_addr_compiled_method);
			mono_mb_emit_op (mb, CEE_CALLI, target_method_sig);
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
	mono_mb_emit_add_to_local (mb, GINT_TO_UINT16 (local_i), 1);

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
emit_unsafe_accessor_field_wrapper (MonoMethodBuilder *mb, MonoMethod *accessor_method, MonoMethodSignature *sig, MonoGenericContext *ctx, MonoUnsafeAccessorKind kind, const char *member_name)
{
	// Field access requires a single argument for target type and a return type.
	g_assert (kind == MONO_UNSAFE_ACCESSOR_FIELD || kind == MONO_UNSAFE_ACCESSOR_STATIC_FIELD);
	g_assert (member_name != NULL);

	MonoType *target_type = sig->params[0]; // params[0] is the field's parent
	MonoType *ret_type = sig->ret;
	if (sig->param_count != 1 || target_type == NULL || sig->ret->type == MONO_TYPE_VOID) {
		mono_mb_emit_exception_full (mb, "System", "BadImageFormatException", "Invalid usage of UnsafeAccessorAttribute.");
		return;
	}

	MonoClass *target_class = mono_class_from_mono_type_internal (target_type);
	gboolean target_byref = m_type_is_byref (target_type);
	gboolean target_valuetype = m_class_is_valuetype (target_class);
	gboolean ret_byref = m_type_is_byref (ret_type);
	if (!ret_byref || (kind == MONO_UNSAFE_ACCESSOR_FIELD && target_valuetype && !target_byref)) {
		mono_mb_emit_exception_full (mb, "System", "BadImageFormatException", "Invalid usage of UnsafeAccessorAttribute.");
		return;
	}

	MonoClassField *target_field = mono_class_get_field_from_name_full (target_class, member_name, NULL);
	if (target_field == NULL || !mono_metadata_type_equal_full (target_field->type, m_class_get_byval_arg (mono_class_from_mono_type_internal (ret_type)), TRUE)) {
		mono_mb_emit_exception_full (mb, "System", "MissingFieldException", 
			g_strdup_printf("No '%s' in '%s'. Or the type of '%s' doesn't match", member_name, m_class_get_name (target_class), member_name));
		return;
	}
	gboolean is_field_static = !!(target_field->type->attrs & FIELD_ATTRIBUTE_STATIC);
	if ((kind == MONO_UNSAFE_ACCESSOR_FIELD && is_field_static) || (kind == MONO_UNSAFE_ACCESSOR_STATIC_FIELD && !is_field_static)) {
		mono_mb_emit_exception_full (mb, "System", "MissingFieldException", g_strdup_printf("UnsafeAccessorKind does not match expected static modifier on field '%s' in '%s'", member_name, m_class_get_name (target_class)));
		return;
	}
	if (is_field_static && m_field_get_parent (target_field) != target_class) {
		// don't look up static fields using the inheritance hierarchy
		mono_mb_emit_exception_full (mb, "System", "MissingFieldException", g_strdup_printf("Field '%s' not found in '%s'", member_name, m_class_get_name (target_class)));
	}

	if (kind == MONO_UNSAFE_ACCESSOR_FIELD)
		mono_mb_emit_ldarg (mb, 0);
	mono_mb_emit_op (mb, kind == MONO_UNSAFE_ACCESSOR_FIELD ? CEE_LDFLDA : CEE_LDSFLDA, target_field);
	mono_mb_emit_byte (mb, CEE_RET);
}

/*
 * Given an accessor method signature (where the first arg is a target class) creates the signature
 * of the expected member method (ie, with the first arg removed)
 */
static MonoMethodSignature *
method_sig_from_accessor_sig (MonoMethodBuilder *mb, gboolean hasthis, MonoMethodSignature *accessor_sig, MonoGenericContext *ctx)
{
	MonoMethodSignature *ret = mono_metadata_signature_dup_full (get_method_image (mb->method), accessor_sig);
	g_assert (ret->param_count > 0);
	ret->hasthis = hasthis;
	for (int i = 1; i < ret->param_count; i++)
		ret->params [i - 1] = ret->params [i];
	memset (&ret->params[ret->param_count - 1], 0, sizeof (MonoType)); // just in case
	ret->param_count--;
	return ret;
}

/*
 * Given an accessor method signature (where the return type is a target class) creates the signature
 * of the expected constructor method (same args, but return type is void).
 */
static MonoMethodSignature *
ctor_sig_from_accessor_sig (MonoMethodBuilder *mb, MonoMethodSignature *accessor_sig, MonoGenericContext *ctx)
{
	MonoMethodSignature *ret = mono_metadata_signature_dup_full (get_method_image (mb->method), accessor_sig);
	ret->hasthis = TRUE; /* ctors are considered instance methods */
	ret->ret = mono_get_void_type ();
	return ret;
}

static void
emit_unsafe_accessor_ldargs (MonoMethodBuilder *mb, MonoMethodSignature *accessor_sig, int skip_count)
{
	for (int i = skip_count; i < accessor_sig->param_count; i++)
		mono_mb_emit_ldarg (mb, i);
}

static gboolean
unsafe_accessor_target_type_forbidden (MonoType *target_type)
{
	switch (target_type->type)
	{
	case MONO_TYPE_VOID:
	case MONO_TYPE_PTR:
	case MONO_TYPE_FNPTR:
		return TRUE;
	default:
		return FALSE;
	}
}

static void
emit_missing_method_error (MonoMethodBuilder *mb, MonoError *failure, const char *display_member_name)
{
	if (!is_ok (failure)) {
		mono_mb_emit_exception_full (mb, "System", "MissingMethodException", g_strdup_printf ("Could not find %s due to: %s", display_member_name, mono_error_get_message (failure)));
	} else {
		mono_mb_emit_exception_full (mb, "System", "MissingMethodException", g_strdup_printf ("Could not find %s", display_member_name));
	}
}

static void
emit_unsafe_accessor_ctor_wrapper (MonoMethodBuilder *mb, MonoMethod *accessor_method, MonoMethodSignature *sig, MonoGenericContext *ctx, MonoUnsafeAccessorKind kind, const char *member_name)
{
	g_assert (kind == MONO_UNSAFE_ACCESSOR_CTOR);
	// null or empty string member name is ok for a constructor
	if (!member_name || member_name[0] == '\0')
		member_name = ".ctor";
	if (strcmp (member_name, ".ctor") != 0) {
		mono_mb_emit_exception_full (mb, "System", "BadImageFormatException", "Invalid UnsafeAccessorAttribute for constructor.");
		return;
	}

	MonoType *target_type = sig->ret; // for constructors the return type is the target type
	if (target_type == NULL || m_type_is_byref (target_type) || unsafe_accessor_target_type_forbidden (target_type)) {
		mono_mb_emit_exception_full (mb, "System", "BadImageFormatException", "Invalid usage of UnsafeAccessorAttribute.");
		return;
	}

	MonoMethodSignature *member_sig = ctor_sig_from_accessor_sig (mb, sig, ctx);

	MonoClass *target_class = mono_class_from_mono_type_internal (target_type);

	ERROR_DECL(find_method_error);
	MonoClass *in_class = mono_class_is_ginst (target_class) ? mono_class_get_generic_class (target_class)->container_class : target_class;
	MonoMethod *target_method = mono_unsafe_accessor_find_ctor (in_class, member_sig, target_class, find_method_error);
	if (!is_ok (find_method_error) || target_method == NULL) {
		if (mono_error_get_error_code (find_method_error) == MONO_ERROR_GENERIC)
			mono_mb_emit_exception_for_error (mb, find_method_error);
		else
			emit_missing_method_error (mb, find_method_error, "constructor");
		mono_error_cleanup (find_method_error);
		return;
	}
	g_assert (target_method->klass == target_class);

	emit_unsafe_accessor_ldargs (mb, sig, 0);

	mono_mb_emit_op (mb, CEE_NEWOBJ, target_method);
	mono_mb_emit_byte (mb, CEE_RET);
}

static void
emit_unsafe_accessor_method_wrapper (MonoMethodBuilder *mb, MonoMethod *accessor_method, MonoMethodSignature *sig, MonoGenericContext *ctx, MonoUnsafeAccessorKind kind, const char *member_name)
{
	g_assert (kind == MONO_UNSAFE_ACCESSOR_METHOD || kind == MONO_UNSAFE_ACCESSOR_STATIC_METHOD);
	g_assert (member_name != NULL);

	// We explicitly allow calling a constructor as if it was an instance method, but we need some hacks in a couple of places
	gboolean ctor_as_method = !strcmp (member_name, ".ctor");

	if (sig->param_count < 1 || sig->params[0] == NULL || unsafe_accessor_target_type_forbidden (sig->params[0])) {
		mono_mb_emit_exception_full (mb, "System", "BadImageFormatException", "Invalid usage of UnsafeAccessorAttribute.");
		return;
	}
	gboolean hasthis = kind == MONO_UNSAFE_ACCESSOR_METHOD;
	MonoType *target_type = sig->params[0];

	MonoMethodSignature *member_sig = method_sig_from_accessor_sig (mb, hasthis, sig, ctx);

	MonoClass *target_class = mono_class_from_mono_type_internal (target_type);

	if (hasthis && m_class_is_valuetype (target_class) && !m_type_is_byref (target_type)) {
		// If the non-static method access is for a value type, the instance must be byref.
		mono_mb_emit_exception_full (mb, "System", "BadImageFormatException", "Invalid usage of UnsafeAccessorAttribute.");
	}

	ERROR_DECL(find_method_error);
	MonoClass *in_class = mono_class_is_ginst (target_class) ? mono_class_get_generic_class (target_class)->container_class : target_class;
	MonoMethod *target_method = NULL;
	if (!ctor_as_method)
		target_method = mono_unsafe_accessor_find_method (in_class, member_name, member_sig, target_class, find_method_error);
	else
		target_method = mono_unsafe_accessor_find_ctor (in_class, member_sig, target_class, find_method_error);
	if (!is_ok (find_method_error) || target_method == NULL) {
		if (mono_error_get_error_code (find_method_error) == MONO_ERROR_GENERIC)
			mono_mb_emit_exception_for_error (mb, find_method_error);
		else
			emit_missing_method_error (mb, find_method_error, member_name);
		mono_error_cleanup (find_method_error);
		return;
	}
	if (!hasthis && target_method->klass != target_class) {
		emit_missing_method_error (mb, find_method_error, member_name);
		return;
	}
	g_assert (target_method->klass == target_class); // are instance methods allowed to be looked up using inheritance?

	emit_unsafe_accessor_ldargs (mb, sig, !hasthis ? 1 : 0);

	mono_mb_emit_op (mb, hasthis ? CEE_CALLVIRT : CEE_CALL, target_method);
	mono_mb_emit_byte (mb, CEE_RET);
}

static void
emit_unsafe_accessor_wrapper_ilgen (MonoMethodBuilder *mb, MonoMethod *accessor_method, MonoMethodSignature *sig, MonoGenericContext *ctx, MonoUnsafeAccessorKind kind, const char *member_name)
{
	if (accessor_method->is_inflated || accessor_method->is_generic || mono_class_is_ginst (accessor_method->klass) || ctx != NULL) {
		mono_mb_emit_exception_full (mb, "System", "BadImageFormatException", "UnsafeAccessor_Generics");
		return;
	}

	if (!m_method_is_static (accessor_method)) {
		mono_mb_emit_exception_full (mb, "System", "BadImageFormatException", "UnsafeAccessor_NonStatic");
		return;
	}

	switch (kind) {
	case MONO_UNSAFE_ACCESSOR_FIELD:
	case MONO_UNSAFE_ACCESSOR_STATIC_FIELD:
		emit_unsafe_accessor_field_wrapper (mb, accessor_method, sig, ctx, kind, member_name);
		return;
	case MONO_UNSAFE_ACCESSOR_CTOR:
		emit_unsafe_accessor_ctor_wrapper (mb, accessor_method, sig, ctx, kind, member_name);
		return;
	case MONO_UNSAFE_ACCESSOR_METHOD:
	case MONO_UNSAFE_ACCESSOR_STATIC_METHOD:
		emit_unsafe_accessor_method_wrapper (mb, accessor_method, sig, ctx, kind, member_name);
		return;
	default:
		mono_mb_emit_exception_full (mb, "System", "BadImageFormatException", "UnsafeAccessor_InvalidKindValue");
		return;
	}
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
	int pos_leave;
	MonoExceptionClause *clause;
	MonoType *object_type = mono_get_object_type ();
#if defined (TARGET_WASM)
	/* in the AOT compiler emit blocking transitions if --wasm-gc-safepoints was used */
	#ifndef DISABLE_THREADS
		const gboolean do_blocking_transition = TRUE;
	#else
		const gboolean do_blocking_transition = mono_opt_wasm_gc_safepoints;
	#endif
#else
	const gboolean do_blocking_transition = TRUE;
#endif
	GCUnsafeTransitionBuilder gc_unsafe_builder = {0,};

	if (do_blocking_transition)
		gc_unsafe_transition_builder_init (&gc_unsafe_builder, mb, TRUE);

	/* local 0 (temp for exception object) */
	mono_mb_add_local (mb, object_type);

	/* local 1 (temp for result) */
	if (!MONO_TYPE_IS_VOID (sig->ret))
		mono_mb_add_local (mb, sig->ret);

	if (do_blocking_transition) {
		gc_unsafe_transition_builder_add_vars (&gc_unsafe_builder);
	}

	/* clear exception arg */
	mono_mb_emit_ldarg (mb, param_count - 1);
	mono_mb_emit_byte (mb, CEE_LDNULL);
	mono_mb_emit_byte (mb, CEE_STIND_REF);

	if (do_blocking_transition) {
		gc_unsafe_transition_builder_emit_enter (&gc_unsafe_builder);
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
		gc_unsafe_transition_builder_emit_exit (&gc_unsafe_builder);

		gc_unsafe_transition_builder_cleanup (&gc_unsafe_builder);
	}

	mono_mb_emit_byte (mb, CEE_RET);
}

static gboolean
emit_managed_wrapper_validate_signature (MonoMethodSignature* sig, MonoMarshalSpec** mspecs, MonoError* error)
{
	if (mspecs) {
		for (int i = 0; i < sig->param_count; i ++) {
			if (mspecs [i + 1] && mspecs [i + 1]->native == MONO_NATIVE_CUSTOM) {
				if (!mspecs [i + 1]->data.custom_data.custom_name || *mspecs [i + 1]->data.custom_data.custom_name == '\0') {
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
				for (guint32 field_idx = 0; field_idx < marshal_type->num_fields; ++field_idx) {
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
emit_managed_wrapper_ilgen (MonoMethodBuilder *mb, MonoMethodSignature *invoke_sig, MonoMarshalSpec **mspecs, EmitMarshalContext* m, MonoMethod *method, MonoGCHandle target_handle, gboolean runtime_init_callback, MonoError *error)
{
	MonoMethodSignature *sig, *csig;
	int i, *tmp_locals;
	gboolean closed = FALSE;
	GCUnsafeTransitionBuilder gc_unsafe_builder = {0,};

	sig = m->sig;
	csig = m->csig;

	if (!sig->hasthis && sig->param_count != invoke_sig->param_count) {
		/* Closed delegate */
		if (sig->param_count != invoke_sig->param_count + 1) {
			g_warning ("Closed delegate has incorrect number of arguments: %s.", mono_method_full_name (method, TRUE));
			g_assert_not_reached ();
		}

		closed = TRUE;
		/* Use a new signature without the first argument */
		sig = mono_metadata_signature_dup (sig);
		memmove (&sig->params [0], &sig->params [1], (sig->param_count - 1) * sizeof (MonoType*));
		sig->param_count --;
	}

	if (!emit_managed_wrapper_validate_signature (sig, mspecs, error)) {
		if (closed)
			g_free (sig);
		return;
	}

	MonoType *int_type = mono_get_int_type ();
	MonoType *boolean_type = m_class_get_byval_arg (mono_defaults.boolean_class);
	/* allocate local 0 (pointer) src_ptr */
	mono_mb_add_local (mb, int_type);
	/* allocate local 1 (pointer) dst_ptr */
	mono_mb_add_local (mb, int_type);
	/* allocate local 2 (boolean) delete_old */
	mono_mb_add_local (mb, boolean_type);

	if (!MONO_TYPE_IS_VOID(sig->ret)) {
		/* allocate local 3 to store the return value */
		mono_mb_add_local (mb, sig->ret);
	}

	if (MONO_TYPE_ISSTRUCT (sig->ret))
		m->vtaddr_var = mono_mb_add_local (mb, int_type);

	gc_unsafe_transition_builder_init (&gc_unsafe_builder, mb, TRUE);
	gc_unsafe_transition_builder_add_vars (&gc_unsafe_builder);

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

	/* delete_old = FALSE */
	mono_mb_emit_icon (mb, 0);
	mono_mb_emit_stloc (mb, 2);

	/*
	* Transformed into a direct icall when runtime init callback is enabled for a native-to-managed wrapper.
	* This icall is special cased in the JIT so it can be called in native-to-managed wrapper before
	* runtime has been initialized. On return, runtime must be fully initialized.
	*/
	if (runtime_init_callback)
		mono_mb_emit_icall (mb, mono_dummy_runtime_init_callback);

	gc_unsafe_transition_builder_emit_enter(&gc_unsafe_builder);

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
			/* TODO: marshal-lightweight: can this move to marshal-ilgen? */
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
			case MONO_TYPE_PTR:
			case MONO_TYPE_I:
				mono_emit_marshal (m, i, invoke_sig->params [i], mspecs [i + 1], tmp_locals [i], NULL, MARSHAL_ACTION_MANAGED_CONV_OUT);
				break;
			default:
				g_assert_not_reached ();
			}
		}
	}

	gc_unsafe_transition_builder_emit_exit (&gc_unsafe_builder);

	gc_unsafe_transition_builder_cleanup (&gc_unsafe_builder);

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

		mono_marshal_shared_emit_struct_conv (mb, klass, FALSE);
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

		mono_marshal_shared_emit_struct_conv (mb, klass, TRUE);
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

	mono_marshal_shared_mb_emit_exception_marshal_directive (m->mb, fullmsg);
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
	mono_mb_emit_i4 (mb, GPTRDIFF_TO_INT32 (mono_jit_icall_info_index (callinfo)));
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
mono_marshal_lightweight_init (void)
{
	MonoMarshalLightweightCallbacks cb;
	cb.version = MONO_MARSHAL_CALLBACKS_VERSION;
	cb.emit_marshal_scalar = emit_marshal_scalar_ilgen;
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
	cb.emit_unsafe_accessor_wrapper = emit_unsafe_accessor_wrapper_ilgen;
	cb.emit_generic_array_helper = emit_generic_array_helper_ilgen;
	cb.emit_thunk_invoke_wrapper = emit_thunk_invoke_wrapper_ilgen;
	cb.emit_create_string_hack = emit_create_string_hack_ilgen;
	cb.emit_native_icall_wrapper = emit_native_icall_wrapper_ilgen;
	cb.emit_icall_wrapper = emit_icall_wrapper_ilgen;
	cb.emit_return = emit_return_ilgen;
	cb.emit_vtfixup_ftnptr = emit_vtfixup_ftnptr_ilgen;
	cb.mb_skip_visibility = mb_skip_visibility_ilgen;
	cb.mb_emit_exception = mb_emit_exception_ilgen;
	cb.mb_emit_exception_for_error = mb_emit_exception_for_error_ilgen;
	cb.mb_emit_byte = mb_emit_byte_ilgen;
	cb.emit_marshal_directive_exception = emit_marshal_directive_exception_ilgen;
	mono_install_marshal_callbacks (&cb);
}
