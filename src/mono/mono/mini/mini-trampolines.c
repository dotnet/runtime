/**
 * \file
 * (C) 2003 Ximian, Inc.
 * (C) 2003-2011 Novell, Inc.
 * Copyright 2011 Xamarin, Inc (http://www.xamarin.com)
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */
#include <config.h>
#include <glib.h>

#include <mono/metadata/appdomain.h>
#include <mono/metadata/metadata-internals.h>
#include <mono/metadata/marshal.h>
#include <mono/metadata/tabledefs.h>
#include <mono/utils/mono-counters.h>
#include <mono/utils/mono-error-internals.h>
#include <mono/utils/mono-memory-model.h>
#include <mono/utils/mono-compiler.h>
#include <mono/utils/mono-threads-coop.h>
#include <mono/utils/unlocked.h>

#include "mini.h"
#include "lldb.h"
#include "aot-runtime.h"
#include "mini-runtime.h"

#include "interp/interp.h"

/*
 * Address of the trampoline code.  This is used by the debugger to check
 * whether a method is a trampoline.
 */
guint8* mono_trampoline_code [MONO_TRAMPOLINE_NUM];

static GHashTable *rgctx_lazy_fetch_trampoline_hash;
static GHashTable *rgctx_lazy_fetch_trampoline_hash_addr;

static gint32 trampoline_calls;
static gint32 jit_trampolines;
static gint32 unbox_trampolines;
static gint32 static_rgctx_trampolines;
static gint32 rgctx_unmanaged_lookups;
static gint32 rgctx_num_lazy_fetch_trampolines;

#define mono_trampolines_lock() mono_os_mutex_lock (&trampolines_mutex)
#define mono_trampolines_unlock() mono_os_mutex_unlock (&trampolines_mutex)
static mono_mutex_t trampolines_mutex;

#ifdef MONO_ARCH_GSHARED_SUPPORTED

typedef struct {
	MonoMethod *m;
	gpointer addr;
} RgctxTrampInfo;

static gint
rgctx_tramp_info_equal (gconstpointer ka, gconstpointer kb)
{
	const RgctxTrampInfo *i1 = (const RgctxTrampInfo *)ka;
	const RgctxTrampInfo *i2 = (const RgctxTrampInfo *)kb;

	if (i1->m == i2->m && i1->addr == i2->addr)
		return 1;
	else
		return 0;
}

static guint
rgctx_tramp_info_hash (gconstpointer data)
{
	const RgctxTrampInfo *info = (const RgctxTrampInfo *)data;

	return GPOINTER_TO_UINT (info->m) ^ GPOINTER_TO_UINT (info->addr);
}

/**
 * mono_create_static_rgctx_trampoline:
 * \param m the mono method to create a trampoline for
 * \param addr the address to jump to (where the compiled code for M lives)
 *
 * Creates a static rgctx trampoline for M which branches to ADDR which should
 * point to the compiled code of M.
 *
 * Static rgctx trampolines are used when a shared generic method which doesn't
 * have a this argument is called indirectly, ie. from code which can't pass in
 * the rgctx argument. The trampoline sets the rgctx argument and jumps to the
 * methods code. These trampolines are similar to the unbox trampolines, they
 * perform the same task as the static rgctx wrappers, but they are smaller/faster,
 * and can be made to work with full AOT.
 *
 * On PPC addr should be an ftnptr and the return value is an ftnptr too.
 *
 * \returns the generated static rgctx trampoline.
 */
gpointer
mono_create_static_rgctx_trampoline (MonoMethod *m, gpointer addr)
{
	gpointer ctx;
	gpointer res;
	RgctxTrampInfo tmp_info;
	RgctxTrampInfo *info;
	MonoJitMemoryManager *jit_mm;

#ifdef PPC_USES_FUNCTION_DESCRIPTOR
	g_assert (((gpointer*)addr) [2] == 0);
#endif

	ctx = mini_method_get_rgctx (m);

	jit_mm = jit_mm_for_method (m);

	/*
	 * In the AOT case, addr might point to either the method, or to an unbox trampoline,
	 * so make the hash keyed on the m+addr pair.
	 */
	jit_mm_lock (jit_mm);
	if (!jit_mm->static_rgctx_trampoline_hash)
		jit_mm->static_rgctx_trampoline_hash = g_hash_table_new (rgctx_tramp_info_hash, rgctx_tramp_info_equal);
	tmp_info.m = m;
	tmp_info.addr = addr;
	res = g_hash_table_lookup (jit_mm->static_rgctx_trampoline_hash,
							   &tmp_info);
	jit_mm_unlock (jit_mm);
	if (res)
		return res;

	if (mono_aot_only)
		res = mono_aot_get_static_rgctx_trampoline (ctx, addr);
	else
		res = mono_arch_get_static_rgctx_trampoline (jit_mm->mem_manager, ctx, addr);

	/* This address might be passed to mini_init_delegate () which needs to look up the method */
	MonoJitInfo *ji;

	ji = mini_alloc_jinfo (jit_mm, MONO_SIZEOF_JIT_INFO);
	ji->code_start = MINI_FTNPTR_TO_ADDR (res);
	/* Doesn't matter, just need to be able to look up the exact address */
	ji->code_size = 4;
	ji->d.method = m;
	mono_jit_info_table_add (ji);

	jit_mm_lock (jit_mm);
	/* Duplicates inserted while we didn't hold the lock are OK */
	info = (RgctxTrampInfo *)m_method_alloc (m, sizeof (RgctxTrampInfo));
	info->m = m;
	info->addr = addr;
	g_hash_table_insert (jit_mm->static_rgctx_trampoline_hash, info, res);
	UnlockedIncrement (&static_rgctx_trampolines);
	jit_mm_unlock (jit_mm);

	return res;
}

#else
gpointer
mono_create_static_rgctx_trampoline (MonoMethod *m, gpointer addr)
{
	/*
	 * This shouldn't happen as all arches which support generic sharing support
	 * static rgctx trampolines as well.
	 */
	g_assert_not_reached ();
}
#endif

gpointer
mono_create_ftnptr_arg_trampoline (gpointer arg, gpointer addr)
{
	gpointer res;
	MonoMemoryManager *mem_manager = mini_get_default_mem_manager ();

#ifdef MONO_ARCH_HAVE_FTNPTR_ARG_TRAMPOLINE
	if (mono_aot_only)
		res = mono_aot_get_ftnptr_arg_trampoline (arg, addr);
	else
		res = mono_arch_get_ftnptr_arg_trampoline (mem_manager, arg, addr);
#else
	if (mono_aot_only) {
		res = mono_aot_get_static_rgctx_trampoline (arg, addr);
	} else {
		res = mono_arch_get_static_rgctx_trampoline (mem_manager, arg, addr);
	}
#endif

	return res;
}

#if 0
#define DEBUG_IMT(stmt) do { stmt; } while (0)
#else
#define DEBUG_IMT(stmt) do { } while (0)
#endif

/*
 * mini_resolve_imt_method:
 *
 *   Resolve the actual method called when making an IMT call through VTABLE_SLOT with IMT_METHOD as the interface method.
 *
 * Either IMPL_METHOD or OUT_AOT_ADDR will be set on return.
 */
gpointer*
mini_resolve_imt_method (MonoVTable *vt, gpointer *vtable_slot, MonoMethod *imt_method, MonoMethod **impl_method, gpointer *out_aot_addr, gboolean *out_need_rgctx_tramp, MonoMethod **variant_iface, MonoError *error)
{
	MonoMethod *impl = NULL, *generic_virtual = NULL;
	gboolean lookup_aot, variance_used = FALSE, need_rgctx_tramp = FALSE;
	guint8 *aot_addr = NULL;
	int displacement = GPTRDIFF_TO_INT (vtable_slot - ((gpointer*)vt));
	int interface_offset;
	int imt_slot = MONO_IMT_SIZE + displacement;

	g_assert (imt_slot < MONO_IMT_SIZE);

	error_init (error);
	/* This has to be variance aware since imt_method can be from an interface that vt->klass doesn't directly implement */
	interface_offset = mono_class_interface_offset_with_variance (vt->klass, imt_method->klass, &variance_used);
	if (interface_offset < 0)
		g_error ("%s doesn't implement interface %s\n", mono_type_get_name_full (m_class_get_byval_arg (vt->klass), MONO_TYPE_NAME_FORMAT_IL), mono_type_get_name_full (m_class_get_byval_arg (imt_method->klass), MONO_TYPE_NAME_FORMAT_IL));

	*variant_iface = NULL;
	if (imt_method->is_inflated && ((MonoMethodInflated*)imt_method)->context.method_inst) {
		/* Generic virtual method */
		generic_virtual = imt_method;
		need_rgctx_tramp = TRUE;
	} else if (variance_used && mono_class_has_variant_generic_params (imt_method->klass)) {
		*variant_iface = imt_method;
	}

	/* We can only use the AOT compiled code if we don't require further processing */
	lookup_aot = !generic_virtual & !variant_iface;

	if (!mono_llvm_only)
		mono_vtable_build_imt_slot (vt, mono_method_get_imt_slot (imt_method));

	if (imt_method->is_inflated && ((MonoMethodInflated*)imt_method)->context.method_inst) {
		MonoGenericContext context = { NULL, NULL };

		/*
		 * Generic virtual method, imt_method contains the inflated interface
		 * method, need to get the inflated impl method.
		 */
		/* imt_method->slot might not be set */
		impl = mono_class_get_vtable_entry (vt->klass, interface_offset + mono_method_get_declaring_generic_method (imt_method)->slot);

		if (mono_class_is_ginst (impl->klass))
			context.class_inst = mono_class_get_generic_class (impl->klass)->context.class_inst;
		context.method_inst = ((MonoMethodInflated*)imt_method)->context.method_inst;
		impl = mono_class_inflate_generic_method_checked (impl, &context, error);
		mono_error_assert_ok (error);
	} else {

		/* Avoid loading metadata or creating a generic vtable if possible */
		if (lookup_aot && !m_class_is_valuetype (vt->klass)) {
			aot_addr = (guint8 *)mono_aot_get_method_from_vt_slot (vt, interface_offset + mono_method_get_vtable_slot (imt_method), error);
			return_val_if_nok (error, NULL);
		} else {
			aot_addr = NULL;
		}
		if (aot_addr)
			impl = NULL;
		else
			impl = mono_class_get_vtable_entry (vt->klass, interface_offset + mono_method_get_vtable_slot (imt_method));
	}

	if (impl && mono_method_needs_static_rgctx_invoke (impl, FALSE))
		need_rgctx_tramp = TRUE;
	if (impl && impl->wrapper_type == MONO_WRAPPER_MANAGED_TO_MANAGED) {
		WrapperInfo *info = mono_marshal_get_wrapper_info (impl);

		if (info && info->subtype == WRAPPER_SUBTYPE_GENERIC_ARRAY_HELPER)
			need_rgctx_tramp = TRUE;
	}
	*impl_method = impl;
	*out_need_rgctx_tramp = need_rgctx_tramp;
	*out_aot_addr = aot_addr;

	DEBUG_IMT (printf ("mono_convert_imt_slot_to_vtable_slot: method = %s.%s.%s, imt_method = %s.%s.%s\n",
					   method->klass->name_space, method->klass->name, method->name,
					   imt_method->klass->name_space, imt_method->klass->name, imt_method->name));

	if (vt->imt_collisions_bitmap & (1 << imt_slot)) {
		int slot = mono_method_get_vtable_index (imt_method);
		int vtable_offset;

		g_assert (slot != -1);
		vtable_offset = interface_offset + slot;
		vtable_slot = & (vt->vtable [vtable_offset]);
		DEBUG_IMT (printf ("mono_convert_imt_slot_to_vtable_slot: slot %p[%d] is in the IMT, and colliding becomes %p[%d] (interface_offset = %d, method->slot = %d)\n", slot, imt_slot, vtable_slot, vtable_offset, interface_offset, imt_method->slot));
		return vtable_slot;
	} else {
		DEBUG_IMT (printf ("mono_convert_imt_slot_to_vtable_slot: slot %p[%d] is in the IMT, but not colliding\n", slot, imt_slot));
		return vtable_slot;
	}
}

/*
 * This is a super-ugly hack to fix bug #616463.
 *
 * The problem is that we don't always set is_generic for generic
 * method definitions.  See the comment at the end of
 * mono_class_inflate_generic_method_full_checked() in class.c.
 */
static gboolean
is_generic_method_definition (MonoMethod *m)
{
	MonoGenericContext *context;
	if (m->is_generic)
		return TRUE;
	if (!m->is_inflated)
		return FALSE;

	context = mono_method_get_context (m);
	if (!context->method_inst)
		return FALSE;
	if (context->method_inst == mono_method_get_generic_container (((MonoMethodInflated*)m)->declaring)->context.method_inst)
		return TRUE;
	return FALSE;
}

gboolean
mini_jit_info_is_gsharedvt (MonoJitInfo *ji)
{
	if (ji && ji->has_generic_jit_info && (mono_jit_info_get_generic_sharing_context (ji)->is_gsharedvt))
		return TRUE;
	else
		return FALSE;
}

/**
 * mini_add_method_trampoline:
 * @m:
 * @compiled_method:
 * @add_static_rgctx_tramp: adds a static rgctx trampoline
 * @add_unbox_tramp: adds an unboxing trampoline
 *
 * Add static rgctx/gsharedvt_in/unbox trampolines to
 * M/COMPILED_METHOD if needed.
 *
 * Returns the trampoline address, or COMPILED_METHOD if no trampoline
 * is needed.
 */
gpointer
mini_add_method_trampoline (MonoMethod *m, gpointer compiled_method, gboolean add_static_rgctx_tramp, gboolean add_unbox_tramp)
{
	gpointer addr = compiled_method;
	gboolean callee_gsharedvt = FALSE, callee_array_helper;
	MonoMethod *jmethod = NULL;
	MonoJitInfo *ji = NULL;

	callee_array_helper = FALSE;
	if (m->wrapper_type == MONO_WRAPPER_MANAGED_TO_MANAGED) {
		WrapperInfo *info = mono_marshal_get_wrapper_info (m);

		/*
		 * generic array helpers.
		 * Have to replace the wrappers with the original generic instances.
		 */
		if (info && info->subtype == WRAPPER_SUBTYPE_GENERIC_ARRAY_HELPER) {
			callee_array_helper = TRUE;
			m = info->d.generic_array_helper.method;
		}
	} else if (m->wrapper_type == MONO_WRAPPER_OTHER) {
		WrapperInfo *info = mono_marshal_get_wrapper_info (m);

		/* Same for synchronized inner wrappers */
		if (info && info->subtype == WRAPPER_SUBTYPE_SYNCHRONIZED_INNER) {
			m = info->d.synchronized_inner.method;
		}
	}

	if (m->is_inflated || callee_array_helper) {
		// This loads information from AOT so try to avoid it if possible
		ji = mini_jit_info_table_find (mono_get_addr_from_ftnptr (compiled_method));
		callee_gsharedvt = mini_jit_info_is_gsharedvt (ji);
	}

	if (callee_gsharedvt)
		g_assert (m->is_inflated);

	addr = compiled_method;

	if (add_unbox_tramp) {
		/*
		 * The unbox trampolines call the method directly, so need to add
		 * an rgctx tramp before them.
		 */
		if (mono_aot_only) {
			addr = mono_aot_get_unbox_trampoline (m, addr);
		} else {
			unbox_trampolines ++;
			addr = mono_arch_get_unbox_trampoline (m, addr);
		}
	}

	if (ji && !ji->is_trampoline)
		jmethod = jinfo_get_method (ji);
	if (callee_gsharedvt && mini_is_gsharedvt_variable_signature (mono_method_signature_internal (jmethod))) {
		MonoMethodSignature *sig, *gsig;

		/* Here m is a generic instance, while ji->method is the gsharedvt method implementing it */

		/* Call from normal/gshared code to gsharedvt code with variable signature */
		sig = mono_method_signature_internal (m);
		gsig = mono_method_signature_internal (jmethod);

		addr = mini_get_gsharedvt_wrapper (TRUE, addr, sig, gsig, -1, FALSE);

		if (mono_llvm_only)
			g_assert_not_reached ();
		//printf ("IN: %s\n", mono_method_full_name (m, TRUE));
	}

	if (callee_array_helper) {
		add_static_rgctx_tramp = FALSE;
		/* In AOT mode, compiled_method points to one of the InternalArray methods in Array. */
		if (ji && !mono_llvm_only && mono_method_needs_static_rgctx_invoke (jinfo_get_method (ji), TRUE))
			add_static_rgctx_tramp = TRUE;
	}

	if (mono_llvm_only)
		add_static_rgctx_tramp = FALSE;

	if (add_static_rgctx_tramp && !(ji && ji->no_mrgctx))
		addr = mono_create_static_rgctx_trampoline (m, addr);

	return addr;
}

/**
 * common_call_trampoline:
 *
 *   The code to handle normal, virtual, and interface method calls and jumps, both
 * from JITted and LLVM compiled code.
 */
static gpointer
common_call_trampoline (host_mgreg_t *regs, guint8 *code, MonoMethod *m, MonoVTable *vt, gpointer *vtable_slot, MonoError *error)
{
	gpointer addr, compiled_method;
	gboolean generic_shared = FALSE;
	gboolean need_unbox_tramp = FALSE;
	gboolean need_rgctx_tramp = FALSE;
	MonoMethod *generic_virtual = NULL, *variant_iface = NULL;
	int context_used;
	gboolean imt_call, virtual_;
	gpointer *orig_vtable_slot, *vtable_slot_to_patch = NULL;
	MonoJitInfo *ji = NULL;
	MonoMethod *orig_method = m;

	error_init (error);

	virtual_ = vt && (gpointer)vtable_slot > (gpointer)vt;
	imt_call = vt && (gpointer)vtable_slot < (gpointer)vt;

	/*
	 * rgctx trampolines are needed when the call is indirect so the caller can't pass
	 * the rgctx argument needed by the callee.
	 */
	if (virtual_ && m)
		need_rgctx_tramp = mono_method_needs_static_rgctx_invoke (m, FALSE);

	orig_vtable_slot = vtable_slot;
	vtable_slot_to_patch = vtable_slot;

	/* IMT call */
	if (imt_call) {
		MonoMethod *imt_method = NULL, *impl_method = NULL;

		g_assert (vtable_slot);

		imt_method = mono_arch_find_imt_method (regs, code);

		{
			if (imt_method->is_inflated && ((MonoMethodInflated*)imt_method)->context.method_inst) {
				/* Generic virtual method */
				generic_virtual = imt_method;
				need_rgctx_tramp = TRUE;
			}

			vtable_slot = mini_resolve_imt_method (vt, vtable_slot, imt_method, &impl_method, &addr, &need_rgctx_tramp, &variant_iface, error);
			return_val_if_nok (error, NULL);

			if (mono_class_has_dim_conflicts (vt->klass) && mono_class_is_method_ambiguous (vt->klass, imt_method)) {
				char *class_name = mono_class_full_name (vt->klass);
				char *method_name = mono_method_full_name (imt_method, TRUE);
				mono_error_set_ambiguous_implementation (error, "Could not call method '%s' with type '%s' because there are multiple incompatible interface methods overriding this method.", method_name, class_name);
				g_free (class_name);
				g_free (method_name);
				return NULL;
			}

			/* We must handle magic interfaces on rank 1 arrays of ref types as if they were variant */
			if (!variant_iface && m_class_get_rank (vt->klass) == 1 && !m_class_is_valuetype (m_class_get_element_class (vt->klass)) && m_class_is_array_special_interface (imt_method->klass))
				variant_iface = imt_method;

			/* This is the vcall slot which gets called through the IMT trampoline */
			vtable_slot_to_patch = vtable_slot;

			if (addr) {
				/*
				 * We found AOT compiled code for the method, skip the rest.
				 */
				*vtable_slot = addr;
				return mono_create_ftnptr (addr);
			}

			m = impl_method;
		}
	}

	MonoMethod *declaring = NULL;

	/*
	 * The virtual check is needed because is_generic_method_definition (m) could
	 * return TRUE for methods used in IMT calls too.
	 */
	if (virtual_ && is_generic_method_definition (m)) {
		MonoGenericContext context = { NULL, NULL };

		if (m->is_inflated)
			declaring = mono_method_get_declaring_generic_method (m);
		else
			declaring = m;

		if (mono_class_is_ginst (m->klass))
			context.class_inst = mono_class_get_generic_class (m->klass)->context.class_inst;
		else
			g_assert (!mono_class_is_gtd (m->klass));

		generic_virtual = mono_arch_find_imt_method (regs, code);
		g_assert (generic_virtual);
		g_assert (generic_virtual->is_inflated);
		context.method_inst = ((MonoMethodInflated*)generic_virtual)->context.method_inst;

		m = mono_class_inflate_generic_method_checked (declaring, &context, error);
		mono_error_assert_ok (error);
		/* FIXME: only do this if the method is sharable */
		need_rgctx_tramp = TRUE;
	} else if ((context_used = mono_method_check_context_used (m))) {
		MonoClass *klass = NULL;
		MonoMethod *actual_method = NULL;
		MonoVTable *actual_vt  = NULL;
		MonoGenericInst *method_inst = NULL;

		vtable_slot = NULL;
		generic_shared = TRUE;

		/*
		 * The caller is gshared code, compute the actual method to call from M and this/rgctx.
		 */
		if (m->is_inflated && (mono_method_get_context (m)->method_inst || mini_method_is_default_method (m))) {
			MonoMethodRuntimeGenericContext *mrgctx = (MonoMethodRuntimeGenericContext*)mono_arch_find_static_call_vtable (regs, code);

			klass = mrgctx->class_vtable->klass;
			method_inst = mrgctx->method_inst;
		} else if ((m->flags & METHOD_ATTRIBUTE_STATIC) || m_class_is_valuetype (m->klass)) {
			MonoMethodRuntimeGenericContext *mrgctx = (MonoMethodRuntimeGenericContext*)mono_arch_find_static_call_vtable (regs, code);

			klass = mrgctx->class_vtable->klass;
			method_inst = mrgctx->method_inst;
			/*
			MonoVTable *vtable = mono_arch_find_static_call_vtable (regs, code);

			klass = vtable->klass;
			*/
		} else {
			MonoObject *this_argument = (MonoObject *)mono_arch_get_this_arg_from_call (regs, code);

			actual_vt  = this_argument->vtable;
			vtable_slot = orig_vtable_slot;

			g_assert (m_class_is_inited (this_argument->vtable->klass));

			if (!vtable_slot) {
				mono_class_setup_supertypes (this_argument->vtable->klass);
				klass = m_class_get_supertypes (this_argument->vtable->klass) [m_class_get_idepth (m->klass) - 1];
			}
		}

		g_assert (vtable_slot || klass);

		if (vtable_slot) {
			int displacement = GPTRDIFF_TO_INT (vtable_slot - ((gpointer*)actual_vt));

			g_assert_not_reached ();

			g_assert (displacement > 0);

			actual_method = m_class_get_vtable (actual_vt ->klass) [displacement];
		}

		if (method_inst || m->wrapper_type) {
			MonoGenericContext context = { NULL, NULL };

			if (m->is_inflated)
				declaring = mono_method_get_declaring_generic_method (m);
			else
				declaring = m;

			if (mono_class_is_ginst (klass))
				context.class_inst = mono_class_get_generic_class (klass)->context.class_inst;
			else if (mono_class_is_gtd (klass))
				context.class_inst = mono_class_get_generic_container (klass)->context.class_inst;
			context.method_inst = method_inst;

			actual_method = mono_class_inflate_generic_method_checked (declaring, &context, error);
			mono_error_assert_ok (error);
		} else {
			actual_method = mono_class_get_method_generic (klass, m, error);
			mono_error_assert_ok (error);
		}

		g_assert (klass);
		g_assert (actual_method);
		g_assert (actual_method->klass == klass);

		if (actual_method->is_inflated)
			declaring = mono_method_get_declaring_generic_method (actual_method);
		else
			declaring = NULL;

		m = actual_method;
	}

	if (m->iflags & METHOD_IMPL_ATTRIBUTE_SYNCHRONIZED) {
		m = mono_marshal_get_synchronized_wrapper (m);
		need_rgctx_tramp = FALSE;
	}

	addr = compiled_method = mono_jit_compile_method (m, error);
	if (!addr)
		return NULL;

	if (generic_virtual || variant_iface) {
		if (m_class_is_valuetype (vt->klass)  && !mini_method_is_default_method (m)) /*FIXME is this required variant iface?*/
			need_unbox_tramp = TRUE;
	} else if (orig_vtable_slot) {
		if (m_class_is_valuetype (m->klass)) {
			g_assert (!mini_method_is_default_method (m));
			need_unbox_tramp = TRUE;
		}
	}

	addr = mini_add_method_trampoline (m, compiled_method, need_rgctx_tramp, need_unbox_tramp);

	if (generic_virtual || variant_iface) {
		MonoMethod *target = generic_virtual ? generic_virtual : variant_iface;

		vtable_slot = orig_vtable_slot;
		g_assert (vtable_slot);

		mono_method_add_generic_virtual_invocation (vt, vtable_slot,
													target, addr);

		return addr;
	}

	/* the method was jumped to */
	if (!code) {
		mini_patch_jump_sites (m, mono_get_addr_from_ftnptr (addr));

		MonoJitMemoryManager *jit_mm;

		/* Patch the got entries pointing to this method */
		/*
		 * We do this here instead of in mono_codegen () to cover the case when m
		 * was loaded from an aot image.
		 */
		GSList *list, *tmp;
		MonoMethod *shared_method = mini_method_to_shared (m);
		m = shared_method ? shared_method : m;
		jit_mm = jit_mm_for_method (m);

		if (jit_mm->jump_target_got_slot_hash) {
			jit_mm_lock (jit_mm);
			list = (GSList *)g_hash_table_lookup (jit_mm->jump_target_got_slot_hash, m);
			if (list) {
				for (tmp = list; tmp; tmp = tmp->next) {
					gpointer *got_slot = (gpointer *)tmp->data;
					*got_slot = addr;
				}
				g_hash_table_remove (jit_mm->jump_target_got_slot_hash, m);
				g_slist_free (list);
			}
			jit_mm_unlock (jit_mm);
		}

		/* Patch the jump trampoline if possible */
#ifdef MONO_ARCH_HAVE_PATCH_JUMP_TRAMPOLINE
		if (!mono_aot_only) {
			jit_mm_lock (jit_mm);
			gpointer jump_tramp = g_hash_table_lookup (jit_mm->jump_trampoline_hash, m);
			jit_mm_unlock (jit_mm);

			if (jump_tramp)
				mono_arch_patch_jump_trampoline ((guint8*)jump_tramp, (guint8*)addr);
		}
#endif

		return addr;
	}

	vtable_slot = orig_vtable_slot;

	if (vtable_slot) {
		if (vtable_slot_to_patch) {
			g_assert (*vtable_slot_to_patch);
			*vtable_slot_to_patch = mono_get_addr_from_ftnptr (addr);
		}
	} else {
		guint8 *plt_entry = mono_aot_get_plt_entry (regs, code);
		gboolean no_patch = FALSE;
		MonoJitInfo *target_ji;

		if (plt_entry) {
			if (generic_shared) {
				target_ji =
					mini_jit_info_table_find (mono_get_addr_from_ftnptr (compiled_method));
				if (!ji)
					ji = mini_jit_info_table_find (code);

				if (ji && ji->has_generic_jit_info) {
					if (target_ji && !target_ji->has_generic_jit_info) {
						no_patch = TRUE;
					} else if (mono_use_interpreter && !target_ji) {
						/* compiled_method might be an interp entry trampoline and the interpreter has no generic sharing */
						no_patch = TRUE;
					}
				}
			}
			if (!no_patch)
				mono_aot_patch_plt_entry (NULL, code, plt_entry, NULL, regs, (guint8 *)addr);
		} else {
			if (generic_shared) {
				if (m->wrapper_type != MONO_WRAPPER_NONE)
					m = mono_marshal_method_from_wrapper (m);
				//g_assert (mono_method_is_generic_sharable (m, FALSE));
			}

			/* Patch calling code */
			target_ji =
				mini_jit_info_table_find (mono_get_addr_from_ftnptr (compiled_method));
			if (!ji)
				ji = mini_jit_info_table_find (code);

			if (ji && target_ji && generic_shared && ji->has_generic_jit_info && !target_ji->has_generic_jit_info) {
				/*
				 * Can't patch the call as the caller is gshared, but the callee is not. Happens when
				 * generic sharing fails.
				 * FIXME: Performance problem.
				 */
				no_patch = TRUE;
			}
			if (!no_patch)
				mini_patch_llvm_jit_callees (orig_method, addr);
			/* LLVM code doesn't make direct calls */
			if (ji && ji->from_llvm)
				no_patch = TRUE;
			if (!no_patch && ji)
				mono_arch_patch_callsite ((guint8 *)ji->code_start, code, (guint8 *)addr);
		}
	}

	return addr;
}

/**
 * mono_magic_trampoline:
 *
 * This trampoline handles normal calls from JITted code.
 */
gpointer
mono_magic_trampoline (host_mgreg_t *regs, guint8 *code, gpointer arg, guint8* tramp)
{
	gpointer res;
	ERROR_DECL (error);

	MONO_ENTER_GC_UNSAFE;

	g_assert (mono_thread_is_gc_unsafe_mode ());

	UnlockedIncrement (&trampoline_calls);

	res = common_call_trampoline (regs, code, (MonoMethod *)arg, NULL, NULL, error);
	if (!is_ok (error)) {
		mono_error_set_pending_exception (error);
		res = NULL;
	}

	MONO_EXIT_GC_UNSAFE;
	return res;
}

/**
 * mono_vcall_trampoline:
 *
 * This trampoline handles virtual calls.
 */
static gpointer
mono_vcall_trampoline (host_mgreg_t *regs, guint8 *code, int slot, guint8 *tramp)
{
	gpointer res;
	MONO_ENTER_GC_UNSAFE;

	MonoObject *this_arg;
	MonoVTable *vt;
	gpointer *vtable_slot;
	MonoMethod *m;
	ERROR_DECL (error);
	gpointer addr;
	res = NULL;

	UnlockedIncrement (&trampoline_calls);

	/*
	 * We need to obtain the following pieces of information:
	 * - the method which needs to be compiled.
	 * - the vtable slot.
	 * We use one vtable trampoline per vtable slot index, so we need only the vtable,
	 * the other two can be computed from the vtable + the slot index.
	 */

	/*
	 * Obtain the vtable from the 'this' arg.
	 */
	this_arg = (MonoObject *)mono_arch_get_this_arg_from_call (regs, code);
	g_assert (this_arg);

	vt = this_arg->vtable;

	if (slot >= 0) {
		/* Normal virtual call */
		vtable_slot = &(vt->vtable [slot]);

		/* Avoid loading metadata or creating a generic vtable if possible */
		addr = mono_aot_get_method_from_vt_slot (vt, slot, error);
		goto_if_nok (error, leave);
		if (addr && !m_class_is_valuetype (vt->klass)) {
			*vtable_slot = addr;
			res = mono_create_ftnptr (addr);
			goto leave;
		}

		/*
		 * Bug #616463 (see
		 * is_generic_method_definition() above) also
		 * goes away if we do a
		 * mono_class_setup_vtable (vt->klass) here,
		 * because we then inflate the method
		 * correctly, put it in the cache, and the
		 * "wrong" inflation invocation still looks up
		 * the correctly inflated method.
		 *
		 * The hack above seems more stable and
		 * trustworthy.
		 */
		m = mono_class_get_vtable_entry (vt->klass, slot);
	} else {
		/* IMT call */
		vtable_slot = &(((gpointer*)vt) [slot]);

		m = NULL;
	}

	res = common_call_trampoline (regs, code, m, vt, vtable_slot, error);
leave:
	if (!is_ok (error)) {
		mono_error_set_pending_exception (error);
		res = NULL;
	}
	MONO_EXIT_GC_UNSAFE;
	return res;
}

/**
 * mono_aot_trampoline:
 *
 * This trampoline handles calls made from AOT code. We try to bypass the
 * normal JIT compilation logic to avoid loading the metadata for the method.
 */
#ifdef MONO_ARCH_AOT_SUPPORTED
gpointer
mono_aot_trampoline (host_mgreg_t *regs, guint8 *code, guint8 *token_info,
					 guint8* tramp)
{
	MONO_REQ_GC_UNSAFE_MODE;

	MonoImage *image;
	guint32 token;
	MonoMethod *method = NULL;
	gpointer addr;
	guint8 *plt_entry;
	ERROR_DECL (error);

	UnlockedIncrement (&trampoline_calls);

	image = (MonoImage *)*(gpointer*)token_info;
	token_info += sizeof (gpointer);
	token = *(guint32*)token_info;

	addr = mono_aot_get_method_from_token (image, token, error);
	if (!is_ok (error))
		mono_error_cleanup (error);
	if (!addr) {
		method = mono_get_method_checked (image, token, NULL, NULL, error);
		if (!method)
			g_error ("Could not load AOT trampoline due to %s", mono_error_get_message (error));

		/* Use the generic code */
		return mono_magic_trampoline (regs, code, method, tramp);
	}

	addr = mono_create_ftnptr (addr);

	/* This is a normal call through a PLT entry */
	plt_entry = mono_aot_get_plt_entry (regs, code);
	g_assert (plt_entry);

	mono_aot_patch_plt_entry (NULL, code, plt_entry, NULL, regs, (guint8 *)addr);

	return addr;
}

/*
 * mono_aot_plt_trampoline:
 *
 *   This trampoline handles calls made from AOT code through the PLT table.
 */
gpointer
mono_aot_plt_trampoline (host_mgreg_t *regs, guint8 *code, guint8 *aot_module,
						 guint8* tramp)
{
	gpointer res;
	ERROR_DECL (error);

	MONO_ENTER_GC_UNSAFE;
	UnlockedIncrement (&trampoline_calls);

	res = mono_aot_plt_resolve (aot_module, regs, code, error);
	if (!res) {
		if (!is_ok (error)) {
			mono_error_set_pending_exception (error);
			res = NULL;
		} else {
			// FIXME: Error handling (how ?)
			g_assert (res);
		}
	}

	MONO_EXIT_GC_UNSAFE;
	return res;
}
#endif

static gpointer
mono_rgctx_lazy_fetch_trampoline (host_mgreg_t *regs, guint8 *code, gpointer data, guint8 *tramp)
{
	MONO_REQ_GC_UNSAFE_MODE;

	guint32 slot = GPOINTER_TO_UINT (data);
	gpointer arg = (gpointer)(gssize)regs [MONO_ARCH_VTABLE_REG];
	guint32 index = MONO_RGCTX_SLOT_INDEX (slot);
	gboolean mrgctx = MONO_RGCTX_SLOT_IS_MRGCTX (slot);
	ERROR_DECL (error);
	gpointer res;

	UnlockedIncrement (&trampoline_calls);
	UnlockedIncrement (&rgctx_unmanaged_lookups);

	if (mrgctx)
		res = mono_method_fill_runtime_generic_context ((MonoMethodRuntimeGenericContext *)arg, index, error);
	else
		res = mono_class_fill_runtime_generic_context ((MonoVTable *)arg, index, error);
	if (!is_ok (error)) {
		mono_error_set_pending_exception (error);
		return NULL;
	}
	return res;
}

/**
 * mono_delegate_trampoline:
 *
 *   This trampoline handles calls made to Delegate:Invoke ().
 * This is called once the first time a delegate is invoked, so it must be fast.
 */
gpointer
mono_delegate_trampoline (host_mgreg_t *regs, guint8 *code, gpointer *arg, guint8* tramp)
{
	MONO_REQ_GC_UNSAFE_MODE;

	MonoDelegate *delegate;
	MonoJitInfo *ji;
	MonoMethod *m;
	MonoMethod *method = NULL;
	ERROR_DECL (error);
	gboolean multicast, callvirt = FALSE;
	gboolean closed_static = FALSE, closed_over_null = FALSE;
	gboolean need_rgctx_tramp = FALSE;
	gboolean need_unbox_tramp = FALSE;
	gboolean enable_caching = TRUE;
	MonoDelegateTrampInfo *tramp_info = (MonoDelegateTrampInfo*)arg;
	MonoMethod *invoke = tramp_info->invoke;
	guint8 *impl_this = (guint8 *)tramp_info->impl_this;
	guint8 *impl_nothis = (guint8 *)tramp_info->impl_nothis;
	MonoMethodSignature *sig;
	gpointer addr, invoke_impl, compiled_method;

	UnlockedIncrement (&trampoline_calls);

	/* Obtain the delegate object according to the calling convention */
	delegate = (MonoDelegate *)mono_arch_get_this_arg_from_call (regs, code);
	g_assert (mono_class_has_parent (mono_object_class (delegate), mono_defaults.multicastdelegate_class));

	/*
	 * The 'arg' argument might point to a MonoDelegateTrampInfo which
	 * has no method set, if the trampoline was created by
	 * mono_create_delegate_trampoline (). Use the more precise info
	 * from the delegate itself.
	 */
	if (delegate->invoke_info)
		tramp_info = (MonoDelegateTrampInfo*)delegate->invoke_info;

	if (delegate->method) {
		method = delegate->method;

		/*
		 * delegate->method_ptr == NULL means the delegate was initialized by
		 * mini_delegate_ctor, while != NULL means it is initialized by
		 * mono_delegate_ctor_with_method (). In both cases, we need to add wrappers
		 * (ctor_with_method () does this, but it doesn't store the wrapper back into
		 * delegate->method).
		 */
		sig = tramp_info->sig;
		if (!(sig && method == tramp_info->method)) {
			sig = mono_method_signature_checked (method, error);
			if (!sig) {
				mono_error_set_pending_exception (error);
				return NULL;
			}
		}

		if (sig->hasthis && m_class_is_valuetype (method->klass)) {
			gboolean need_unbox = TRUE;

			if (tramp_info->invoke_sig->param_count > sig->param_count && m_type_is_byref (tramp_info->invoke_sig->params [0]))
				need_unbox = FALSE;

			if (need_unbox) {
				if (mono_aot_only)
					need_unbox_tramp = TRUE;
				else
					method = mono_marshal_get_unbox_wrapper (method);
			}
		}
	// If "delegate->method_ptr" is null mono_get_addr_from_ftnptr will fail if
	// ftnptrs are being used.  "method" would end up null on archtitectures without
	// ftnptrs so we can just skip this.
	} else if (delegate->method_ptr) {
		ji = mini_jit_info_table_find (mono_get_addr_from_ftnptr (delegate->method_ptr));
		if (ji)
			method = jinfo_get_method (ji);
	}

	if (method) {
		sig = tramp_info->sig;
		if (!(sig && method == tramp_info->method)) {
			sig = mono_method_signature_checked (method, error);
			if (!sig) {
				mono_error_set_pending_exception (error);
				return NULL;
			}
		}

		callvirt = !delegate->target && sig->hasthis;
		if (callvirt)
			closed_over_null = tramp_info->invoke_sig->param_count == sig->param_count;
		if (m_method_is_static (method) && sig->param_count == tramp_info->invoke_sig->param_count + 1)
			closed_static = TRUE;

		if (callvirt && !closed_over_null) {
			/*
			 * The delegate needs to make a virtual call to the target method using its
			 * first argument as the receiver. This is hard to support in full-aot, so
			 * optimize it in some cases if possible.
			 * If the target method is not virtual or is in a sealed class,
			 * the vcall will call it directly.
			 * If the call doesn't return a valuetype, then the vcall uses the same calling
			 * convention as a normal call.
			 */
			if ((mono_class_is_sealed (method->klass) || !m_method_is_virtual (method)) && !MONO_TYPE_ISSTRUCT (sig->ret)) {
				callvirt = FALSE;
				enable_caching = FALSE;
			}
		}

		if (delegate->method_is_virtual) {
			/*
			 * If the delegate was created by handle_delegate_ctor (virtual==TRUE), the
			 * ldvirtftn instruction was skipped, so we have to do it now.
			 */
			method = mono_object_get_virtual_method_internal (delegate->target, method);
			enable_caching = FALSE;
		}

		if (m_method_is_synchronized (method))
			method = mono_marshal_get_synchronized_wrapper (method);

		if (method == tramp_info->method)
			need_rgctx_tramp = tramp_info->need_rgctx_tramp;
		else if (mono_method_needs_static_rgctx_invoke (method, FALSE))
			need_rgctx_tramp = TRUE;
	}

	/*
	 * If the called address is a trampoline, replace it with the compiled method so
	 * further calls don't have to go through the trampoline.
	 */
	if (method && !callvirt) {
		/* Avoid the overhead of looking up an already compiled method if possible */
		if (enable_caching && method == tramp_info->method && tramp_info->method_ptr) {
			delegate->method_ptr = tramp_info->method_ptr;
		} else {
			compiled_method = addr = mono_jit_compile_method (method, error);
			if (!is_ok (error)) {
				mono_error_set_pending_exception (error);
				return NULL;
			}
			addr = mini_add_method_trampoline (method, compiled_method, need_rgctx_tramp, need_unbox_tramp);
			delegate->method_ptr = addr;
		}
	} else {
		if (need_rgctx_tramp)
			delegate->method_ptr = mono_create_static_rgctx_trampoline (method, delegate->method_ptr);
	}

	invoke_impl = NULL;

	multicast = ((MonoMulticastDelegate*)delegate)->delegates != NULL;
	if (!multicast && !callvirt) {
		if (closed_static)
			invoke_impl = impl_this;
		else
			invoke_impl = delegate->target ? impl_this : impl_nothis;
	}

	if (!invoke_impl) {
		/* The general, unoptimized case */
		m = mono_marshal_get_delegate_invoke (invoke, delegate);
		invoke_impl = (guint8 *)mono_jit_compile_method (m, error);
		if (!is_ok (error)) {
			mono_error_set_pending_exception (error);
			return NULL;
		}
		invoke_impl = (guint8 *)mini_add_method_trampoline (m, invoke_impl, mono_method_needs_static_rgctx_invoke (m, FALSE), FALSE);
	}

	mono_memory_barrier ();

	delegate->invoke_impl = mono_get_addr_from_ftnptr (invoke_impl);
	if (enable_caching && !callvirt && tramp_info->method) {
		tramp_info->method_ptr = delegate->method_ptr;
		tramp_info->invoke_impl = delegate->invoke_impl;
	}

	return invoke_impl;
}

/*
 * mono_get_trampoline_func:
 *
 *   Return the C function which needs to be called by the generic trampoline of type
 * TRAMP_TYPE.
 */
gconstpointer
mono_get_trampoline_func (MonoTrampolineType tramp_type)
{
#ifndef HOST_WASM
	switch (tramp_type) {
	case MONO_TRAMPOLINE_JIT:
	case MONO_TRAMPOLINE_JUMP:
		return (gconstpointer)mono_magic_trampoline;
	case MONO_TRAMPOLINE_RGCTX_LAZY_FETCH:
		return (gconstpointer)mono_rgctx_lazy_fetch_trampoline;
#ifdef MONO_ARCH_AOT_SUPPORTED
	case MONO_TRAMPOLINE_AOT:
		return (gconstpointer)mono_aot_trampoline;
	case MONO_TRAMPOLINE_AOT_PLT:
		return (gconstpointer)mono_aot_plt_trampoline;
#endif
	case MONO_TRAMPOLINE_DELEGATE:
		return (gconstpointer)mono_delegate_trampoline;
	case MONO_TRAMPOLINE_VCALL:
		return (gconstpointer)mono_vcall_trampoline;
	default:
		g_assert_not_reached ();
		return NULL;
	}
#else
	return NULL;
#endif
}

static guchar*
create_trampoline_code (MonoTrampolineType tramp_type)
{
	MonoTrampInfo *info;
	guchar *code;

	code = mono_arch_create_generic_trampoline (tramp_type, &info, FALSE);
	mono_tramp_info_register (info, NULL);

	return code;
}

void
mono_trampolines_init (void)
{
	mono_os_mutex_init_recursive (&trampolines_mutex);
	gboolean disable_tramps = FALSE;
#if TARGET_WASM
	disable_tramps = TRUE;
#endif

	if (mono_aot_only || disable_tramps)
		return;

	mono_trampoline_code [MONO_TRAMPOLINE_JIT] = create_trampoline_code (MONO_TRAMPOLINE_JIT);
	mono_trampoline_code [MONO_TRAMPOLINE_JUMP] = create_trampoline_code (MONO_TRAMPOLINE_JUMP);
	mono_trampoline_code [MONO_TRAMPOLINE_RGCTX_LAZY_FETCH] = create_trampoline_code (MONO_TRAMPOLINE_RGCTX_LAZY_FETCH);
#ifdef MONO_ARCH_AOT_SUPPORTED
	mono_trampoline_code [MONO_TRAMPOLINE_AOT] = create_trampoline_code (MONO_TRAMPOLINE_AOT);
	mono_trampoline_code [MONO_TRAMPOLINE_AOT_PLT] = create_trampoline_code (MONO_TRAMPOLINE_AOT_PLT);
#endif
	mono_trampoline_code [MONO_TRAMPOLINE_DELEGATE] = create_trampoline_code (MONO_TRAMPOLINE_DELEGATE);
	mono_trampoline_code [MONO_TRAMPOLINE_VCALL] = create_trampoline_code (MONO_TRAMPOLINE_VCALL);

	mono_counters_register ("Calls to trampolines", MONO_COUNTER_JIT | MONO_COUNTER_INT, &trampoline_calls);
	mono_counters_register ("JIT trampolines", MONO_COUNTER_JIT | MONO_COUNTER_INT, &jit_trampolines);
	mono_counters_register ("Unbox trampolines", MONO_COUNTER_JIT | MONO_COUNTER_INT, &unbox_trampolines);
	mono_counters_register ("Static rgctx trampolines", MONO_COUNTER_JIT | MONO_COUNTER_INT, &static_rgctx_trampolines);
	mono_counters_register ("RGCTX unmanaged lookups", MONO_COUNTER_GENERICS | MONO_COUNTER_INT, &rgctx_unmanaged_lookups);
	mono_counters_register ("RGCTX num lazy fetch trampolines", MONO_COUNTER_GENERICS | MONO_COUNTER_INT, &rgctx_num_lazy_fetch_trampolines);
}

guint8 *
mono_get_trampoline_code (MonoTrampolineType tramp_type)
{
	g_assert (mono_trampoline_code [tramp_type]);

	return mono_trampoline_code [tramp_type];
}

gpointer
mono_create_specific_trampoline (MonoMemoryManager *mem_manager, gpointer arg1, MonoTrampolineType tramp_type, guint32 *code_len)
{
	gpointer code;
	guint32 len;

	if (mono_aot_only)
		code = mono_aot_create_specific_trampoline (arg1, tramp_type, &len);
	else
		code = mono_arch_create_specific_trampoline (arg1, tramp_type, mem_manager, &len);
	mono_lldb_save_specific_trampoline_info (arg1, tramp_type, code, len);
	if (code_len)
		*code_len = len;
	return code;
}

gpointer
mono_create_jump_trampoline (MonoMethod *method, gboolean add_sync_wrapper, MonoError *error)
{
	MonoJitMemoryManager *jit_mm;
	MonoJitInfo *ji;
	gpointer code;
	guint32 code_size = 0;

	error_init (error);

	if (mono_use_interpreter && !mono_aot_only) {
		gpointer ret = mini_get_interp_callbacks ()->create_method_pointer (method, FALSE, error);
		if (!is_ok (error))
			return NULL;
		return ret;
	}

	code = mono_jit_find_compiled_method_with_jit_info (method, &ji);
	/*
	 * We cannot recover the correct type of a shared generic
	 * method from its native code address, so we use the
	 * trampoline instead.
	 * For synchronized methods, the trampoline adds the wrapper.
	 */
	if (code && !ji->has_generic_jit_info && !(method->iflags & METHOD_IMPL_ATTRIBUTE_SYNCHRONIZED))
		return code;

	if (mono_llvm_only) {
		code = mono_jit_compile_method (method, error);
		if (!is_ok (error))
			return NULL;
		return code;
	}

	jit_mm = jit_mm_for_method (method);

	jit_mm_lock (jit_mm);
	code = g_hash_table_lookup (jit_mm->jump_trampoline_hash, method);
	jit_mm_unlock (jit_mm);
	if (code)
		return code;

	code = mono_create_specific_trampoline (m_method_get_mem_manager (method), method, MONO_TRAMPOLINE_JUMP, &code_size);
	g_assert (code_size);

	ji = mini_alloc_jinfo (jit_mm, MONO_SIZEOF_JIT_INFO);
	ji->code_start = MINI_FTNPTR_TO_ADDR (code);
	ji->code_size = code_size;
	ji->d.method = method;

	/*
	 * mono_delegate_ctor needs to find the method metadata from the
	 * trampoline address, so we save it here.
	 */

	mono_jit_info_table_add (ji);

	jit_mm_lock (jit_mm);
	g_hash_table_insert (jit_mm->jump_trampoline_hash, method, code);
	jit_mm_unlock (jit_mm);

	return code;
}

static void
method_not_found (void)
{
	g_assert_not_reached ();
}

gpointer
mono_create_jit_trampoline (MonoMethod *method, MonoError *error)
{
	MonoJitMemoryManager *jit_mm;
	gpointer tramp;

	error_init (error);

	if (mono_aot_only) {
		if (mono_llvm_only && method->iflags & METHOD_IMPL_ATTRIBUTE_SYNCHRONIZED)
			method = mono_marshal_get_synchronized_wrapper (method);

		/* Avoid creating trampolines if possible */
		gpointer code = mono_jit_find_compiled_method (method);

		if (code)
			return code;
		if (mono_llvm_only) {
			/* Methods are lazily initialized on first call, so this can't lead recursion */
			code = mono_jit_compile_method (method, error);
			if (!is_ok (error))
				return NULL;
			return code;
		}
	}

	jit_mm = jit_mm_for_method (method);

	jit_mm_lock (jit_mm);
	tramp = g_hash_table_lookup (jit_mm->jit_trampoline_hash, method);
	jit_mm_unlock (jit_mm);
	if (tramp)
		return tramp;

	tramp = mono_create_specific_trampoline (m_method_get_mem_manager (method), method, MONO_TRAMPOLINE_JIT, NULL);

	jit_mm_lock (jit_mm);
	g_hash_table_insert (jit_mm->jit_trampoline_hash, method, tramp);
	UnlockedIncrement (&jit_trampolines);
	jit_mm_unlock (jit_mm);

	return tramp;
}

gpointer
mono_create_jit_trampoline_from_token (MonoImage *image, guint32 token)
{
	gpointer tramp;
	guint8 *buf, *start;

	buf = start = (guint8 *)mono_image_alloc0 (image, 2 * sizeof (gpointer));

	*(gpointer*)buf = image;
	buf += sizeof (gpointer);
	*(guint32*)buf = token;

	tramp = mono_create_specific_trampoline (m_image_get_mem_manager (image), start, MONO_TRAMPOLINE_AOT, NULL);

	UnlockedIncrement (&jit_trampolines);

	return tramp;
}


/*
 * mono_create_delegate_trampoline_info:
 *
 *  Create a trampoline info structure for the KLASS+METHOD pair.
 * If VIRTUAL is true, the delegate was created using ldvirtftn, so invoking it needs
 * to either do a virtual call or do a virtual method lookup.
 */
MonoDelegateTrampInfo*
mono_create_delegate_trampoline_info (MonoClass *klass, MonoMethod *method, gboolean is_virtual)
{
	MonoMethod *invoke;
	ERROR_DECL (error);
	MonoDelegateTrampInfo *tramp_info;
	MonoDelegateClassMethodPair pair, *dpair;
	MonoMemoryManager *mm_class, *mm_method, *mm;
	MonoJitMemoryManager *jit_mm;

	pair.klass = klass;
	pair.method = method;
	pair.is_virtual = is_virtual;

	if (method) {
		mm_class = m_class_get_mem_manager (klass);
		mm_method = m_method_get_mem_manager (method);
		mm = mono_mem_manager_merge (mm_class, mm_method);
		jit_mm = (MonoJitMemoryManager*)mm->runtime_info;
	} else {
		jit_mm = jit_mm_for_class (klass);
	}

	jit_mm_lock (jit_mm);
	tramp_info = (MonoDelegateTrampInfo *)g_hash_table_lookup (jit_mm->delegate_info_hash, &pair);
	jit_mm_unlock (jit_mm);
	if (tramp_info)
		return tramp_info;

	invoke = mono_get_delegate_invoke_internal (klass);
	g_assert (invoke);

	if (method && method->dynamic)
		// FIXME: Can the dynamic method outlive the delegate class if its unloadable ?
		tramp_info = (MonoDelegateTrampInfo *)mono_dyn_method_alloc0 (method, sizeof (MonoDelegateTrampInfo));
	else
		tramp_info = (MonoDelegateTrampInfo *)mono_mem_manager_alloc0 (jit_mm->mem_manager, sizeof (MonoDelegateTrampInfo));
	tramp_info->klass = klass;
	tramp_info->invoke = invoke;
	tramp_info->invoke_sig = mono_method_signature_internal (invoke);
	tramp_info->is_virtual = is_virtual;
	// FIXME: Use a different conditional
#ifndef HOST_WASM
	if (!mono_llvm_only) {
		tramp_info->impl_this = mono_arch_get_delegate_invoke_impl (mono_method_signature_internal (invoke), TRUE);
		tramp_info->impl_nothis = mono_arch_get_delegate_invoke_impl (mono_method_signature_internal (invoke), FALSE);
	}
#endif
	tramp_info->method = method;
	if (method) {
		error_init (error);
		tramp_info->sig = mono_method_signature_checked (method, error);
		tramp_info->need_rgctx_tramp = mono_method_needs_static_rgctx_invoke (method, FALSE);
	}

#ifndef HOST_WASM
	if (is_virtual) {
		tramp_info->invoke_impl = mono_get_delegate_virtual_invoke_impl (mono_method_signature_internal (invoke), method);
	} else {
		if (!mono_llvm_only) {
			guint32 code_size = 0;
			tramp_info->invoke_impl = mono_create_specific_trampoline (jit_mm->mem_manager, tramp_info, MONO_TRAMPOLINE_DELEGATE, &code_size);
			g_assert (code_size);
		}
	}
#endif

	if (method && method->dynamic)
		dpair = (MonoDelegateClassMethodPair *)mono_dyn_method_alloc0 (method, sizeof (MonoDelegateClassMethodPair));
	else
		dpair = (MonoDelegateClassMethodPair *)mono_mem_manager_alloc0 (jit_mm->mem_manager, sizeof (MonoDelegateClassMethodPair));
	memcpy (dpair, &pair, sizeof (MonoDelegateClassMethodPair));

	/* store trampoline address */
	jit_mm_lock (jit_mm);
	g_hash_table_insert (jit_mm->delegate_info_hash, dpair, tramp_info);
	jit_mm_unlock (jit_mm);

	if (method && method->dynamic) {
		jit_mm = jit_mm_for_method (method);
		jit_mm_lock (jit_mm);
		if (!jit_mm->dyn_delegate_info_hash)
			jit_mm->dyn_delegate_info_hash = g_hash_table_new (NULL, NULL);
		GSList *l = g_hash_table_lookup (jit_mm->dyn_delegate_info_hash, method);
		l = g_slist_prepend (l, dpair);
		g_hash_table_insert (jit_mm->dyn_delegate_info_hash, method, l);
		jit_mm_unlock (jit_mm);
	}

	return tramp_info;
}

static void
no_delegate_trampoline (void)
{
	g_assert_not_reached ();
}

gpointer
mono_create_delegate_trampoline (MonoClass *klass)
{
	if (mono_llvm_only || (mono_use_interpreter && !mono_aot_only))
		return (gpointer)no_delegate_trampoline;

	return mono_create_delegate_trampoline_info (klass, NULL, FALSE)->invoke_impl;
}

gpointer
mono_create_rgctx_lazy_fetch_trampoline (guint32 offset)
{
	MonoTrampInfo *info;
	gpointer tramp, ptr;

	mono_trampolines_lock ();
	if (rgctx_lazy_fetch_trampoline_hash)
		tramp = g_hash_table_lookup (rgctx_lazy_fetch_trampoline_hash, GUINT_TO_POINTER (offset));
	else
		tramp = NULL;
	mono_trampolines_unlock ();
	if (tramp)
		return tramp;

	if (mono_aot_only) {
		ptr = mono_aot_get_lazy_fetch_trampoline (offset);
	} else {
		tramp = mono_arch_create_rgctx_lazy_fetch_trampoline (offset, &info, FALSE);
		mono_tramp_info_register (info, NULL);
		ptr = mono_create_ftnptr (tramp);
	}

	mono_trampolines_lock ();
	if (!rgctx_lazy_fetch_trampoline_hash) {
		rgctx_lazy_fetch_trampoline_hash = g_hash_table_new (NULL, NULL);
		rgctx_lazy_fetch_trampoline_hash_addr = g_hash_table_new (NULL, NULL);
	}
	g_hash_table_insert (rgctx_lazy_fetch_trampoline_hash, GUINT_TO_POINTER (offset), ptr);
	g_assert (offset != -1);
	g_hash_table_insert (rgctx_lazy_fetch_trampoline_hash_addr, ptr, GUINT_TO_POINTER (offset + 1));
	rgctx_num_lazy_fetch_trampolines ++;
	mono_trampolines_unlock ();

	return ptr;
}

guint32
mono_find_rgctx_lazy_fetch_trampoline_by_addr (gconstpointer addr)
{
	int offset;

	mono_trampolines_lock ();
	if (rgctx_lazy_fetch_trampoline_hash_addr) {
		/* We store the real offset + 1 so we can detect when the lookup fails */
		offset = GPOINTER_TO_INT (g_hash_table_lookup (rgctx_lazy_fetch_trampoline_hash_addr, addr));
		if (offset)
			offset -= 1;
		else
			offset = -1;
	} else {
		offset = -1;
	}
	mono_trampolines_unlock ();
	return offset;
}

static const char* const tramp_names [MONO_TRAMPOLINE_NUM] = {
	"generic_trampoline_jit",
	"generic_trampoline_jump",
	"generic_trampoline_rgctx_lazy_fetch",
	"generic_trampoline_aot",
	"generic_trampoline_aot_plt",
	"generic_trampoline_delegate",
	"generic_trampoline_vcall"
};

/*
 * mono_get_generic_trampoline_simple_name:
 *
 */
const char*
mono_get_generic_trampoline_simple_name (MonoTrampolineType tramp_type)
{
	return tramp_names [tramp_type] + sizeof ("generic_trampoline_") - 1;
}

/*
 * mono_get_generic_trampoline_name:
 *
 *   Returns a pointer to malloc-ed memory.
 */
const char*
mono_get_generic_trampoline_name (MonoTrampolineType tramp_type)
{
	return tramp_names [tramp_type];
}

/*
 * mono_get_rgctx_fetch_trampoline_name:
 *
 *   Returns a pointer to malloc-ed memory.
 */
char*
mono_get_rgctx_fetch_trampoline_name (int slot)
{
	gboolean mrgctx;
	int index;

	mrgctx = MONO_RGCTX_SLOT_IS_MRGCTX (slot);
	index = MONO_RGCTX_SLOT_INDEX (slot);

	return g_strdup_printf ("rgctx_fetch_trampoline_%s_%d", mrgctx ? "mrgctx" : "rgctx", index);
}

/*
 * mini_get_single_step_trampoline:
 *
 *   Return a trampoline which calls debugger_agent_single_step_from_context ().
 */
gpointer
mini_get_single_step_trampoline (void)
{
	static gpointer trampoline;

	if (!trampoline) {
		gpointer tramp;

		if (mono_ee_features.use_aot_trampolines) {
			tramp = mono_aot_get_trampoline ("sdb_single_step_trampoline");
		} else {
#ifdef MONO_ARCH_HAVE_SDB_TRAMPOLINES
			MonoTrampInfo *info;
			tramp = mono_arch_create_sdb_trampoline (TRUE, &info, FALSE);
			mono_tramp_info_register (info, NULL);
#else
			tramp = NULL;
			g_assert_not_reached ();
#endif
		}
		mono_memory_barrier ();
		trampoline = tramp;
	}

	return trampoline;
}

/*
 * mini_get_breakpoint_trampoline:
 *
 *   Return a trampoline which calls mono_debugger_agent_breakpoint_from_context ().
 */
gpointer
mini_get_breakpoint_trampoline (void)
{
	static gpointer trampoline;

	if (!trampoline) {
		gpointer tramp;

		if (mono_ee_features.use_aot_trampolines) {
			tramp = mono_aot_get_trampoline ("sdb_breakpoint_trampoline");
		} else {
#ifdef MONO_ARCH_HAVE_SDB_TRAMPOLINES
			MonoTrampInfo *info;
			tramp = mono_arch_create_sdb_trampoline (FALSE, &info, FALSE);
			mono_tramp_info_register (info, NULL);
#else
			tramp = NULL;
			g_assert_not_reached ();
#endif
		}
		mono_memory_barrier ();
		trampoline = tramp;
	}

	return trampoline;
}
