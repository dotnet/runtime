/*
 * (C) 2003 Ximian, Inc.
 * (C) 2003-2011 Novell, Inc.
 * Copyright 2011 Xamarin, Inc (http://www.xamarin.com)
 */
#include <config.h>
#include <glib.h>

#include <mono/metadata/appdomain.h>
#include <mono/metadata/metadata-internals.h>
#include <mono/metadata/marshal.h>
#include <mono/metadata/tabledefs.h>
#include <mono/utils/mono-counters.h>
#include <mono/utils/mono-error-internals.h>

#include "mini.h"
#include "debug-mini.h"

/*
 * Address of the trampoline code.  This is used by the debugger to check
 * whether a method is a trampoline.
 */
guint8* mono_trampoline_code [MONO_TRAMPOLINE_NUM];

static GHashTable *class_init_hash_addr = NULL;
static GHashTable *rgctx_lazy_fetch_trampoline_hash = NULL;
static GHashTable *rgctx_lazy_fetch_trampoline_hash_addr = NULL;
static guint32 trampoline_calls, jit_trampolines, unbox_trampolines, static_rgctx_trampolines;

#define mono_trampolines_lock() EnterCriticalSection (&trampolines_mutex)
#define mono_trampolines_unlock() LeaveCriticalSection (&trampolines_mutex)
static CRITICAL_SECTION trampolines_mutex;

static gpointer
get_unbox_trampoline (MonoMethod *m, gpointer addr, gboolean need_rgctx_tramp)
{
	if (mono_aot_only) {
		if (need_rgctx_tramp)
			/* 
			 * The unbox trampolines call the method directly, so need to add
			 * an rgctx tramp before them.
			 */
			return mono_create_static_rgctx_trampoline (m, mono_aot_get_unbox_trampoline (m));
		else
			return mono_aot_get_unbox_trampoline (m);
	} else {
		unbox_trampolines ++;
		return mono_arch_get_unbox_trampoline (m, addr);
	}
}

#ifdef MONO_ARCH_GSHARED_SUPPORTED

typedef struct {
	MonoMethod *m;
	gpointer addr;
} RgctxTrampInfo;

static gint
rgctx_tramp_info_equal (gconstpointer ka, gconstpointer kb)
{
	const RgctxTrampInfo *i1 = ka;
	const RgctxTrampInfo *i2 = kb;

	if (i1->m == i2->m && i1->addr == i2->addr)
		return 1;
	else
		return 0;
}

static guint
rgctx_tramp_info_hash (gconstpointer data)
{
	const RgctxTrampInfo *info = data;

	return GPOINTER_TO_UINT (info->m) ^ GPOINTER_TO_UINT (info->addr);
}

/*
 * mono_create_static_rgctx_trampoline:
 *
 *   Return a static rgctx trampoline for M which branches to ADDR which should
 * point to the compiled code of M.
 *
 *   Static rgctx trampolines are used when a shared generic method which doesn't
 * have a this argument is called indirectly, ie. from code which can't pass in
 * the rgctx argument. The trampoline sets the rgctx argument and jumps to the
 * methods code. These trampolines are similar to the unbox trampolines, they
 * perform the same task as the static rgctx wrappers, but they are smaller/faster,
 * and can be made to work with full AOT.
 * On PPC addr should be an ftnptr and the return value is an ftnptr too.
 */
gpointer
mono_create_static_rgctx_trampoline (MonoMethod *m, gpointer addr)
{
	gpointer ctx;
	gpointer res;
	MonoDomain *domain;
	RgctxTrampInfo tmp_info;
	RgctxTrampInfo *info;

#ifdef PPC_USES_FUNCTION_DESCRIPTOR
	g_assert (((gpointer*)addr) [2] == 0);
#endif

	ctx = mini_method_get_rgctx (m);

	domain = mono_domain_get ();

	/* 
	 * In the AOT case, addr might point to either the method, or to an unbox trampoline,
	 * so make the hash keyed on the m+addr pair.
	 */
	mono_domain_lock (domain);
	if (!domain_jit_info (domain)->static_rgctx_trampoline_hash)
		domain_jit_info (domain)->static_rgctx_trampoline_hash = g_hash_table_new (rgctx_tramp_info_hash, rgctx_tramp_info_equal);
	tmp_info.m = m;
	tmp_info.addr = addr;
	res = g_hash_table_lookup (domain_jit_info (domain)->static_rgctx_trampoline_hash,
							   &tmp_info);
	mono_domain_unlock (domain);
	if (res)
		return res;

	if (mono_aot_only)
		res = mono_aot_get_static_rgctx_trampoline (ctx, addr);
	else
		res = mono_arch_get_static_rgctx_trampoline (m, ctx, addr);

	mono_domain_lock (domain);
	/* Duplicates inserted while we didn't hold the lock are OK */
	info = mono_domain_alloc (domain, sizeof (RgctxTrampInfo));
	info->m = m;
	info->addr = addr;
	g_hash_table_insert (domain_jit_info (domain)->static_rgctx_trampoline_hash, info, res);
	mono_domain_unlock (domain);

	static_rgctx_trampolines ++;

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

#ifdef MONO_ARCH_HAVE_IMT

/*
 * Either IMPL_METHOD or AOT_ADDR will be set on return.
 */
static gpointer*
#ifdef __GNUC__
/*
 * This works against problems when compiling with gcc 4.6 on arm. The 'then' part of
 * this line gets executed, even when the condition is false:
 *		if (impl && mono_method_needs_static_rgctx_invoke (impl, FALSE))
 *			*need_rgctx_tramp = TRUE;
 */
__attribute__ ((noinline))
#endif
mono_convert_imt_slot_to_vtable_slot (gpointer* slot, mgreg_t *regs, guint8 *code, MonoMethod *method, MonoMethod **impl_method, gboolean *need_rgctx_tramp, gboolean *variance_used, gpointer *aot_addr)
{
	MonoObject *this_argument = mono_arch_get_this_arg_from_call (regs, code);
	MonoVTable *vt = this_argument->vtable;
	int displacement = slot - ((gpointer*)vt);

	if (displacement > 0) {
		/* slot is in the vtable, not in the IMT */
#if DEBUG_IMT
		printf ("mono_convert_imt_slot_to_vtable_slot: slot %p is in the vtable, not in the IMT\n", slot);
#endif
		return slot;
	} else {
		MonoMethod *imt_method = mono_arch_find_imt_method (regs, code);
		MonoMethod *impl;
		int interface_offset;
		int imt_slot = MONO_IMT_SIZE + displacement;

		/*This has to be variance aware since imt_method can be from an interface that vt->klass doesn't directly implement*/
		interface_offset = mono_class_interface_offset_with_variance (vt->klass, imt_method->klass, variance_used);

		if (interface_offset < 0) {
			g_error ("%s doesn't implement interface %s\n", mono_type_get_name_full (&vt->klass->byval_arg, 0), mono_type_get_name_full (&imt_method->klass->byval_arg, 0));
		}
		mono_vtable_build_imt_slot (vt, mono_method_get_imt_slot (imt_method));

		if (imt_method->is_inflated && ((MonoMethodInflated*)imt_method)->context.method_inst) {
			MonoGenericContext context = { NULL, NULL };

			/* 
			 * Generic virtual method, imt_method contains the inflated interface 
			 * method, need to get the inflated impl method.
			 */
			/* imt_method->slot might not be set */
			impl = mono_class_get_vtable_entry (vt->klass, interface_offset + mono_method_get_declaring_generic_method (imt_method)->slot);

			if (impl->klass->generic_class)
				context.class_inst = impl->klass->generic_class->context.class_inst;
			context.method_inst = ((MonoMethodInflated*)imt_method)->context.method_inst;
			impl = mono_class_inflate_generic_method (impl, &context);
		} else {
			/* Avoid loading metadata or creating a generic vtable if possible */
			if (!vt->klass->valuetype)
				*aot_addr = mono_aot_get_method_from_vt_slot (mono_domain_get (), vt, interface_offset + mono_method_get_vtable_slot (imt_method));
			else
				*aot_addr = NULL;
			if (*aot_addr)
				impl = NULL;
			else
				impl = mono_class_get_vtable_entry (vt->klass, interface_offset + mono_method_get_vtable_slot (imt_method));
		}

		if (impl && mono_method_needs_static_rgctx_invoke (impl, FALSE))
			*need_rgctx_tramp = TRUE;
		if (impl && impl->wrapper_type == MONO_WRAPPER_MANAGED_TO_MANAGED) {
			WrapperInfo *info = mono_marshal_get_wrapper_info (impl);

			if (info && info->subtype == WRAPPER_SUBTYPE_GENERIC_ARRAY_HELPER) {
				// FIXME: This needs a gsharedvt-out trampoline, since the caller uses the gsharedvt calling conv, but the
				// wrapper is a normal non-generic method.
				*need_rgctx_tramp = TRUE;
				//g_assert_not_reached ();
			}
		}

		*impl_method = impl;
#if DEBUG_IMT
		printf ("mono_convert_imt_slot_to_vtable_slot: method = %s.%s.%s, imt_method = %s.%s.%s\n",
				method->klass->name_space, method->klass->name, method->name, 
				imt_method->klass->name_space, imt_method->klass->name, imt_method->name);
#endif

		g_assert (imt_slot < MONO_IMT_SIZE);
		if (vt->imt_collisions_bitmap & (1 << imt_slot)) {
			int slot = mono_method_get_vtable_index (imt_method);
			int vtable_offset;
			gpointer *vtable_slot;

			g_assert (slot != -1);
			vtable_offset = interface_offset + slot;
			vtable_slot = & (vt->vtable [vtable_offset]);
#if DEBUG_IMT
			printf ("mono_convert_imt_slot_to_vtable_slot: slot %p[%d] is in the IMT, and colliding becomes %p[%d] (interface_offset = %d, method->slot = %d)\n", slot, imt_slot, vtable_slot, vtable_offset, interface_offset, imt_method->slot);
#endif
			return vtable_slot;
		} else {
#if DEBUG_IMT
			printf ("mono_convert_imt_slot_to_vtable_slot: slot %p[%d] is in the IMT, but not colliding\n", slot, imt_slot);
#endif
			return slot;
		}
	}
}
#endif

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
static gboolean
ji_is_gsharedvt (MonoJitInfo *ji)
{
	if (ji && ji->has_generic_jit_info && (mono_jit_info_get_generic_sharing_context (ji)->var_is_vt ||
										   mono_jit_info_get_generic_sharing_context (ji)->mvar_is_vt))
		return TRUE;
	else
		return FALSE;
}

/*
 * mini_add_method_trampoline:
 *
 *   Add static rgctx/gsharedvt_in trampoline to M/COMPILED_METHOD if needed. Return the trampoline address, or
 * COMPILED_METHOD if no trampoline is needed.
 * ORIG_METHOD is the method the caller originally called i.e. an iface method, or NULL.
 */
gpointer
mini_add_method_trampoline (MonoMethod *orig_method, MonoMethod *m, gpointer compiled_method, MonoJitInfo *caller_ji, gboolean add_static_rgctx_tramp)
{
	gpointer addr = compiled_method;
	gboolean callee_gsharedvt, callee_array_helper;
	MonoJitInfo *ji = 
		mini_jit_info_table_find (mono_domain_get (), mono_get_addr_from_ftnptr (compiled_method), NULL);

	// FIXME: This loads information from AOT
	callee_gsharedvt = ji_is_gsharedvt (ji);

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
	} else if (m->wrapper_type == MONO_WRAPPER_UNKNOWN) {
		WrapperInfo *info = mono_marshal_get_wrapper_info (m);

		/* Same for synchronized inner wrappers */
		if (info && info->subtype == WRAPPER_SUBTYPE_SYNCHRONIZED_INNER) {
			m = info->d.synchronized_inner.method;
		}
	}

	if (!orig_method)
		orig_method = m;

	if (callee_gsharedvt)
		g_assert (m->is_inflated);

	addr = compiled_method;

	if (callee_gsharedvt && mini_is_gsharedvt_variable_signature (mono_method_signature (ji->method))) {
		MonoGenericSharingContext *gsctx;
		MonoMethodSignature *sig, *gsig;

		/* Here m is a generic instance, while ji->method is the gsharedvt method implementing it */

		/* Call from normal/gshared code to gsharedvt code with variable signature */
		gsctx = mono_jit_info_get_generic_sharing_context (ji);

		sig = mono_method_signature (m);
		gsig = mono_method_signature (ji->method); 

		addr = mini_get_gsharedvt_wrapper (TRUE, compiled_method, sig, gsig, gsctx, -1, FALSE);

		//printf ("IN: %s\n", mono_method_full_name (m, TRUE));
	}

	if (add_static_rgctx_tramp && !callee_array_helper)
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
common_call_trampoline (mgreg_t *regs, guint8 *code, MonoMethod *m, guint8* tramp, MonoVTable *vt, gpointer *vtable_slot, gboolean need_rgctx_tramp)
{
	gpointer addr, compiled_method;
	gboolean generic_shared = FALSE;
	MonoMethod *declaring = NULL;
	MonoMethod *generic_virtual = NULL, *variant_iface = NULL, *orig_method = NULL;
	int context_used;
	gboolean virtual, variance_used = FALSE;
	gpointer *orig_vtable_slot, *vtable_slot_to_patch = NULL;
	MonoJitInfo *ji = NULL;

	virtual = (gpointer)vtable_slot > (gpointer)vt;

	orig_vtable_slot = vtable_slot;
	vtable_slot_to_patch = vtable_slot;

#ifdef MONO_ARCH_HAVE_IMT
	/* IMT call */
	if (vt && (gpointer)vtable_slot < (gpointer)vt) {
		MonoMethod *impl_method = NULL;
		MonoObject *this_arg;

		/* we get the interface method because mono_convert_imt_slot_to_vtable_slot ()
		 * needs the signature to be able to find the this argument
		 */
		m = mono_arch_find_imt_method (regs, code);
		vtable_slot = orig_vtable_slot;
		g_assert (vtable_slot);

		orig_method = m;

		this_arg = mono_arch_get_this_arg_from_call (regs, code);

		if (this_arg->vtable->klass == mono_defaults.transparent_proxy_class) {
			/* Use the slow path for now */
		    m = mono_object_get_virtual_method (this_arg, m);
			vtable_slot_to_patch = NULL;
		} else {
			addr = NULL;
			vtable_slot = mono_convert_imt_slot_to_vtable_slot (vtable_slot, regs, code, m, &impl_method, &need_rgctx_tramp, &variance_used, &addr);
			/* This is the vcall slot which gets called through the IMT thunk */
			vtable_slot_to_patch = vtable_slot;
			/* mono_convert_imt_slot_to_vtable_slot () also gives us the method that is supposed
			 * to be called, so we compile it and go ahead as usual.
			 */
			/*g_print ("imt found method %p (%s) at %p\n", impl_method, impl_method->name, code);*/
			if (m->is_inflated && ((MonoMethodInflated*)m)->context.method_inst) {
				/* Generic virtual method */
				generic_virtual = m;
				need_rgctx_tramp = TRUE;
			} else if (variance_used && mono_class_has_variant_generic_params (m->klass)) {
				variant_iface = m;
			}

			if (addr && !generic_virtual && !variant_iface) {
				/*
				 * We found AOT compiled code for the method, skip the rest.
				 */
				if (mono_domain_owns_vtable_slot (mono_domain_get (), vtable_slot))
					*vtable_slot = addr;

				return mono_create_ftnptr (mono_domain_get (), addr);
			}

			m = impl_method;
		}
	}
#endif

	/*
	 * The virtual check is needed because is_generic_method_definition (m) could
	 * return TRUE for methods used in IMT calls too.
	 */
	if (virtual && is_generic_method_definition (m)) {
		MonoGenericContext context = { NULL, NULL };
		MonoMethod *declaring;

		if (m->is_inflated)
			declaring = mono_method_get_declaring_generic_method (m);
		else
			declaring = m;

		if (m->klass->generic_class)
			context.class_inst = m->klass->generic_class->context.class_inst;
		else
			g_assert (!m->klass->generic_container);

#ifdef MONO_ARCH_HAVE_IMT
		generic_virtual = mono_arch_find_imt_method (regs, code);
#endif
		if (generic_virtual) {
			g_assert (generic_virtual->is_inflated);
			context.method_inst = ((MonoMethodInflated*)generic_virtual)->context.method_inst;
		}

		m = mono_class_inflate_generic_method (declaring, &context);
		/* FIXME: only do this if the method is sharable */
		need_rgctx_tramp = TRUE;
	} else if ((context_used = mono_method_check_context_used (m))) {
		MonoClass *klass = NULL;
		MonoMethod *actual_method = NULL;
		MonoVTable *vt = NULL;
		MonoGenericInst *method_inst = NULL;

		vtable_slot = NULL;
		generic_shared = TRUE;

		g_assert (code);

		if (m->is_inflated && mono_method_get_context (m)->method_inst) {
#ifdef MONO_ARCH_RGCTX_REG
			MonoMethodRuntimeGenericContext *mrgctx = (MonoMethodRuntimeGenericContext*)mono_arch_find_static_call_vtable (regs, code);

			klass = mrgctx->class_vtable->klass;
			method_inst = mrgctx->method_inst;
#else
			g_assert_not_reached ();
#endif
		} else if ((m->flags & METHOD_ATTRIBUTE_STATIC) || m->klass->valuetype) {
#ifdef MONO_ARCH_RGCTX_REG
			MonoVTable *vtable = mono_arch_find_static_call_vtable (regs, code);

			klass = vtable->klass;
#else
			g_assert_not_reached ();
#endif
		} else {
#ifdef MONO_ARCH_HAVE_IMT
			MonoObject *this_argument = mono_arch_get_this_arg_from_call (regs, code);

			vt = this_argument->vtable;
			vtable_slot = orig_vtable_slot;

			g_assert (this_argument->vtable->klass->inited);
			//mono_class_init (this_argument->vtable->klass);

			if (!vtable_slot) {
				mono_class_setup_supertypes (this_argument->vtable->klass);
				klass = this_argument->vtable->klass->supertypes [m->klass->idepth - 1];
			}
#else
			NOT_IMPLEMENTED;
#endif
		}

		g_assert (vtable_slot || klass);

		if (vtable_slot) {
			int displacement = vtable_slot - ((gpointer*)vt);

			g_assert_not_reached ();

			g_assert (displacement > 0);

			actual_method = vt->klass->vtable [displacement];
		}

		if (method_inst) {
			MonoGenericContext context = { NULL, NULL };

			if (m->is_inflated)
				declaring = mono_method_get_declaring_generic_method (m);
			else
				declaring = m;

			if (klass->generic_class)
				context.class_inst = klass->generic_class->context.class_inst;
			else if (klass->generic_container)
				context.class_inst = klass->generic_container->context.class_inst;
			context.method_inst = method_inst;

			actual_method = mono_class_inflate_generic_method (declaring, &context);
		} else {
			actual_method = mono_class_get_method_generic (klass, m);
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

	/* Calls made through delegates on platforms without delegate trampolines */
	if (!code && mono_method_needs_static_rgctx_invoke (m, FALSE))
		need_rgctx_tramp = TRUE;

	addr = compiled_method = mono_compile_method (m);
	g_assert (addr);

	mono_debugger_trampoline_compiled (code, m, addr);

	// FIXME: Is this needed ?
	if (!ji)
		ji = mini_jit_info_table_find (mono_domain_get (), (char*)code, NULL);
	addr = mini_add_method_trampoline (orig_method, m, compiled_method, ji, need_rgctx_tramp);

	if (generic_virtual || variant_iface) {
		MonoMethod *target = generic_virtual ? generic_virtual : variant_iface;

		vtable_slot = orig_vtable_slot;
		g_assert (vtable_slot);

		if (vt->klass->valuetype) /*FIXME is this required variant iface?*/
			addr = get_unbox_trampoline (m, addr, need_rgctx_tramp);

		mono_method_add_generic_virtual_invocation (mono_domain_get (), 
													vt, vtable_slot,
													target, addr);

		return addr;
	}

	/* the method was jumped to */
	if (!code) {
		MonoDomain *domain = mono_domain_get ();

		/* Patch the got entries pointing to this method */
		/* 
		 * We do this here instead of in mono_codegen () to cover the case when m
		 * was loaded from an aot image.
		 */
		if (domain_jit_info (domain)->jump_target_got_slot_hash) {
			GSList *list, *tmp;

			mono_domain_lock (domain);
			list = g_hash_table_lookup (domain_jit_info (domain)->jump_target_got_slot_hash, m);
			if (list) {
				for (tmp = list; tmp; tmp = tmp->next) {
					gpointer *got_slot = tmp->data;
					*got_slot = addr;
				}
				g_hash_table_remove (domain_jit_info (domain)->jump_target_got_slot_hash, m);
				g_slist_free (list);
			}
			mono_domain_unlock (domain);
		}

		return addr;
	}

	vtable_slot = orig_vtable_slot;

	if (vtable_slot) {
		if (m->klass->valuetype)
			addr = get_unbox_trampoline (m, addr, need_rgctx_tramp);

		if (vtable_slot_to_patch && (mono_aot_is_got_entry (code, (guint8*)vtable_slot_to_patch) || mono_domain_owns_vtable_slot (mono_domain_get (), vtable_slot_to_patch))) {
			g_assert (*vtable_slot_to_patch);
			*vtable_slot_to_patch = mono_get_addr_from_ftnptr (addr);
		}
	}
	else {
		guint8 *plt_entry = mono_aot_get_plt_entry (code);
		gboolean no_patch = FALSE;
		MonoJitInfo *target_ji;

		if (plt_entry) {
			if (generic_shared) {
				target_ji =
					mini_jit_info_table_find (mono_domain_get (), mono_get_addr_from_ftnptr (compiled_method), NULL);
				if (!ji)
					ji = mini_jit_info_table_find (mono_domain_get (), (char*)code, NULL);

				if (ji && target_ji && generic_shared && ji->has_generic_jit_info && !target_ji->has_generic_jit_info) {
					no_patch = TRUE;
				}
			}
			if (!no_patch)
				mono_aot_patch_plt_entry (plt_entry, NULL, regs, addr);
		} else {
			if (generic_shared) {
				if (m->wrapper_type != MONO_WRAPPER_NONE)
					m = mono_marshal_method_from_wrapper (m);
				//g_assert (mono_method_is_generic_sharable_impl (m, FALSE));
			}

			/* Patch calling code */
			target_ji =
				mini_jit_info_table_find (mono_domain_get (), mono_get_addr_from_ftnptr (compiled_method), NULL);
			if (!ji)
				ji = mini_jit_info_table_find (mono_domain_get (), (char*)code, NULL);

			if (ji && target_ji && generic_shared && ji->has_generic_jit_info && !target_ji->has_generic_jit_info) {
				/* 
				 * Can't patch the call as the caller is gshared, but the callee is not. Happens when
				 * generic sharing fails.
				 * FIXME: Performance problem.
				 */
				no_patch = TRUE;
			}

			if (!no_patch && mono_method_same_domain (ji, target_ji))
				mono_arch_patch_callsite (ji->code_start, code, addr);
		}
	}

	return addr;
}

/**
 * mono_magic_trampoline:
 *
 *   This trampoline handles normal calls from JITted code.
 */
gpointer
mono_magic_trampoline (mgreg_t *regs, guint8 *code, gpointer arg, guint8* tramp)
{
	trampoline_calls ++;

	return common_call_trampoline (regs, code, arg, tramp, NULL, NULL, FALSE);
}

/*
 * mono_vcall_trampoline:
 *
 *   This trampoline handles virtual calls.
 */
static gpointer
mono_vcall_trampoline (mgreg_t *regs, guint8 *code, int slot, guint8 *tramp)
{
	MonoObject *this;
	MonoVTable *vt;
	gpointer *vtable_slot;
	MonoMethod *m;
	gboolean need_rgctx_tramp = FALSE;
	gpointer addr;

	trampoline_calls ++;

	/*
	 * We need to obtain the following pieces of information:
	 * - the method which needs to be compiled.
	 * - the vtable slot.
	 * We use one vtable trampoline per vtable slot index, so we need only the vtable,
	 * the other two can be computed from the vtable + the slot index.
	 */
#ifndef MONO_ARCH_THIS_AS_FIRST_ARG
	/* All architectures should support this */
	g_assert_not_reached ();
#endif

	/*
	 * Obtain the vtable from the 'this' arg.
	 */
	this = mono_arch_get_this_arg_from_call (regs, code);
	g_assert (this);

	vt = this->vtable;

	if (slot >= 0) {
		/* Normal virtual call */
		vtable_slot = &(vt->vtable [slot]);

		/* Avoid loading metadata or creating a generic vtable if possible */
		addr = mono_aot_get_method_from_vt_slot (mono_domain_get (), vt, slot);
		if (addr && !vt->klass->valuetype) {
			if (mono_domain_owns_vtable_slot (mono_domain_get (), vtable_slot))
				*vtable_slot = addr;

			return mono_create_ftnptr (mono_domain_get (), addr);
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

		need_rgctx_tramp = mono_method_needs_static_rgctx_invoke (m, 0);
	} else {
		/* IMT call */
		vtable_slot = &(((gpointer*)vt) [slot]);

		m = NULL;
		need_rgctx_tramp = FALSE;
	}

	return common_call_trampoline (regs, code, m, tramp, vt, vtable_slot, need_rgctx_tramp);
}

gpointer
mono_generic_virtual_remoting_trampoline (mgreg_t *regs, guint8 *code, MonoMethod *m, guint8 *tramp)
{
	MonoGenericContext context = { NULL, NULL };
	MonoMethod *imt_method, *declaring;
	gpointer addr;

	trampoline_calls ++;

	g_assert (m->is_generic);

	if (m->is_inflated)
		declaring = mono_method_get_declaring_generic_method (m);
	else
		declaring = m;

	if (m->klass->generic_class)
		context.class_inst = m->klass->generic_class->context.class_inst;
	else
		g_assert (!m->klass->generic_container);

#ifdef MONO_ARCH_HAVE_IMT
	imt_method = mono_arch_find_imt_method (regs, code);
	if (imt_method->is_inflated)
		context.method_inst = ((MonoMethodInflated*)imt_method)->context.method_inst;
#endif
	m = mono_class_inflate_generic_method (declaring, &context);
	m = mono_marshal_get_remoting_invoke_with_check (m);

	addr = mono_compile_method (m);
	g_assert (addr);

	mono_debugger_trampoline_compiled (NULL, m, addr);

	return addr;
}

/*
 * mono_aot_trampoline:
 *
 *   This trampoline handles calls made from AOT code. We try to bypass the 
 * normal JIT compilation logic to avoid loading the metadata for the method.
 */
#ifdef MONO_ARCH_AOT_SUPPORTED
gpointer
mono_aot_trampoline (mgreg_t *regs, guint8 *code, guint8 *token_info, 
					 guint8* tramp)
{
	MonoImage *image;
	guint32 token;
	MonoMethod *method = NULL;
	gpointer addr;
	guint8 *plt_entry;

	trampoline_calls ++;

	image = *(gpointer*)(gpointer)token_info;
	token_info += sizeof (gpointer);
	token = *(guint32*)(gpointer)token_info;

	addr = mono_aot_get_method_from_token (mono_domain_get (), image, token);
	if (!addr) {
		method = mono_get_method (image, token, NULL);
		g_assert (method);

		/* Use the generic code */
		return mono_magic_trampoline (regs, code, method, tramp);
	}

	addr = mono_create_ftnptr (mono_domain_get (), addr);

	/* This is a normal call through a PLT entry */
	plt_entry = mono_aot_get_plt_entry (code);
	g_assert (plt_entry);

	mono_aot_patch_plt_entry (plt_entry, NULL, regs, addr);

	return addr;
}

/*
 * mono_aot_plt_trampoline:
 *
 *   This trampoline handles calls made from AOT code through the PLT table.
 */
gpointer
mono_aot_plt_trampoline (mgreg_t *regs, guint8 *code, guint8 *aot_module, 
						 guint8* tramp)
{
	guint32 plt_info_offset = mono_aot_get_plt_info_offset (regs, code);
	gpointer res;

	trampoline_calls ++;

	res = mono_aot_plt_resolve (aot_module, plt_info_offset, code);
	if (!res) {
		if (mono_loader_get_last_error ())
			mono_raise_exception (mono_loader_error_prepare_exception (mono_loader_get_last_error ()));
		// FIXME: Error handling (how ?)
		g_assert (res);
	}

	return res;
}
#endif

/**
 * mono_class_init_trampoline:
 *
 * This method calls mono_runtime_class_init () to run the static constructor
 * for the type, then patches the caller code so it is not called again.
 */
void
mono_class_init_trampoline (mgreg_t *regs, guint8 *code, MonoVTable *vtable, guint8 *tramp)
{
	guint8 *plt_entry = mono_aot_get_plt_entry (code);

	trampoline_calls ++;

	mono_runtime_class_init (vtable);

	if (plt_entry) {
		mono_arch_nullify_plt_entry (plt_entry, regs);
	} else {
		mono_arch_nullify_class_init_trampoline (code, regs);
	}
}

/**
 * mono_generic_class_init_trampoline:
 *
 * This method calls mono_runtime_class_init () to run the static constructor
 * for the type.
 */
void
mono_generic_class_init_trampoline (mgreg_t *regs, guint8 *code, MonoVTable *vtable, guint8 *tramp)
{
	trampoline_calls ++;

	mono_runtime_class_init (vtable);
}

static gpointer
mono_rgctx_lazy_fetch_trampoline (mgreg_t *regs, guint8 *code, gpointer data, guint8 *tramp)
{
#ifdef MONO_ARCH_VTABLE_REG
	static gboolean inited = FALSE;
	static int num_lookups = 0;
	guint32 slot = GPOINTER_TO_UINT (data);
	mgreg_t *r = (mgreg_t*)regs;
	gpointer arg = (gpointer)(gssize)r [MONO_ARCH_VTABLE_REG];
	guint32 index = MONO_RGCTX_SLOT_INDEX (slot);
	gboolean mrgctx = MONO_RGCTX_SLOT_IS_MRGCTX (slot);

	trampoline_calls ++;

	if (!inited) {
		mono_counters_register ("RGCTX unmanaged lookups", MONO_COUNTER_GENERICS | MONO_COUNTER_INT, &num_lookups);
		inited = TRUE;
	}

	num_lookups++;

	if (mrgctx)
		return mono_method_fill_runtime_generic_context (arg, code, index);
	else
		return mono_class_fill_runtime_generic_context (arg, code, index);
#else
	g_assert_not_reached ();
#endif
}

void
mono_monitor_enter_trampoline (mgreg_t *regs, guint8 *code, MonoObject *obj, guint8 *tramp)
{
	mono_monitor_enter (obj);
}

void
mono_monitor_exit_trampoline (mgreg_t *regs, guint8 *code, MonoObject *obj, guint8 *tramp)
{
	mono_monitor_exit (obj);
}

#ifdef MONO_ARCH_HAVE_CREATE_DELEGATE_TRAMPOLINE

/**
 * mono_delegate_trampoline:
 *
 *   This trampoline handles calls made to Delegate:Invoke ().
 * This is called once the first time a delegate is invoked, so it must be fast.
 */
gpointer
mono_delegate_trampoline (mgreg_t *regs, guint8 *code, gpointer *tramp_data, guint8* tramp)
{
	MonoDomain *domain = mono_domain_get ();
	MonoDelegate *delegate;
	MonoJitInfo *ji;
	MonoMethod *m;
	MonoMethod *method = NULL;
	gboolean multicast, callvirt = FALSE;
	gboolean need_rgctx_tramp = FALSE;
	gboolean need_unbox_tramp = FALSE;
	gboolean enable_caching = TRUE;
	MonoMethod *invoke = tramp_data [0];
	guint8 *impl_this = tramp_data [1];
	guint8 *impl_nothis = tramp_data [2];
	MonoError err;
	MonoMethodSignature *sig;
	gpointer addr, compiled_method;

	trampoline_calls ++;

	/* Obtain the delegate object according to the calling convention */
	delegate = mono_arch_get_this_arg_from_call (regs, code);

	if (delegate->method) {
		method = delegate->method;

		/*
		 * delegate->method_ptr == NULL means the delegate was initialized by 
		 * mini_delegate_ctor, while != NULL means it is initialized by 
		 * mono_delegate_ctor_with_method (). In both cases, we need to add wrappers
		 * (ctor_with_method () does this, but it doesn't store the wrapper back into
		 * delegate->method).
		 */
		if (delegate->target && delegate->target->vtable->klass == mono_defaults.transparent_proxy_class) {
#ifndef DISABLE_COM
			if (((MonoTransparentProxy *)delegate->target)->remote_class->proxy_class != mono_defaults.com_object_class && 
			   !((MonoTransparentProxy *)delegate->target)->remote_class->proxy_class->is_com_object)
#endif
				method = mono_marshal_get_remoting_invoke (method);
		}
		else {
			mono_error_init (&err);
			sig = mono_method_signature_checked (method, &err);
			if (!sig)
				mono_error_raise_exception (&err);
				
			if (sig->hasthis && method->klass->valuetype) {
				if (mono_aot_only)
					need_unbox_tramp = TRUE;
				else
					method = mono_marshal_get_unbox_wrapper (method);
			}
		}
	} else {
		ji = mono_jit_info_table_find (domain, mono_get_addr_from_ftnptr (delegate->method_ptr));
		if (ji)
			method = ji->method;
	}

	if (method) {
		mono_error_init (&err);
		sig = mono_method_signature_checked (method, &err);
		if (!sig)
			mono_error_raise_exception (&err);

		callvirt = !delegate->target && sig->hasthis;
		if (delegate->target && 
			method->flags & METHOD_ATTRIBUTE_VIRTUAL && 
			method->flags & METHOD_ATTRIBUTE_ABSTRACT &&
			method->klass->flags & TYPE_ATTRIBUTE_ABSTRACT) {
			method = mono_object_get_virtual_method (delegate->target, method);
			enable_caching = FALSE;
		}
	}

	if (method && method->iflags & METHOD_IMPL_ATTRIBUTE_SYNCHRONIZED)
		method = mono_marshal_get_synchronized_wrapper (method);

	if (method && mono_method_needs_static_rgctx_invoke (method, FALSE))
		need_rgctx_tramp = TRUE;

	/* 
	 * If the called address is a trampoline, replace it with the compiled method so
	 * further calls don't have to go through the trampoline.
	 */
	if (method && !callvirt) {
		/* Avoid the overhead of looking up an already compiled method if possible */
		if (enable_caching && delegate->method_code && *delegate->method_code) {
			delegate->method_ptr = *delegate->method_code;
		} else {
			compiled_method = addr = mono_compile_method (method);
			if (need_unbox_tramp)
				// FIXME: GSHAREDVT
				addr = get_unbox_trampoline (method, addr, need_rgctx_tramp);
			else
				addr = mini_add_method_trampoline (NULL, method, compiled_method, NULL, need_rgctx_tramp);
			delegate->method_ptr = addr;
			if (enable_caching && delegate->method_code)
				*delegate->method_code = delegate->method_ptr;
			mono_debugger_trampoline_compiled (NULL, method, delegate->method_ptr);
		}
	} else {
		if (need_rgctx_tramp)
			delegate->method_ptr = mono_create_static_rgctx_trampoline (method, delegate->method_ptr);
	}

	multicast = ((MonoMulticastDelegate*)delegate)->prev != NULL;
	if (!multicast && !callvirt) {
		if (method && (method->flags & METHOD_ATTRIBUTE_STATIC) && mono_method_signature (method)->param_count == mono_method_signature (invoke)->param_count + 1)
			/* Closed static delegate */
			code = impl_this;
		else
			code = delegate->target ? impl_this : impl_nothis;

		if (code) {
			delegate->invoke_impl = mono_get_addr_from_ftnptr (code);
			return code;
		}
	}

	/* The general, unoptimized case */
	m = mono_marshal_get_delegate_invoke (invoke, delegate);
	code = mono_compile_method (m);
	code = mini_add_method_trampoline (NULL, m, code, NULL, mono_method_needs_static_rgctx_invoke (m, FALSE));
	delegate->invoke_impl = mono_get_addr_from_ftnptr (code);
	mono_debugger_trampoline_compiled (NULL, m, delegate->invoke_impl);

	return code;
}

#endif

#ifdef MONO_ARCH_HAVE_HANDLER_BLOCK_GUARD
static gpointer
mono_handler_block_guard_trampoline (mgreg_t *regs, guint8 *code, gpointer *tramp_data, guint8* tramp)
{
	MonoContext ctx;
	MonoException *exc;
	MonoJitTlsData *jit_tls = mono_native_tls_get_value (mono_jit_tls_id);
	gpointer resume_ip = jit_tls->handler_block_return_address;

	memcpy (&ctx, &jit_tls->handler_block_context, sizeof (MonoContext));
	MONO_CONTEXT_SET_IP (&ctx, jit_tls->handler_block_return_address);

	jit_tls->handler_block_return_address = NULL;
	jit_tls->handler_block = NULL;

	if (!resume_ip) /*this should not happen, but we should avoid crashing */
		exc = mono_get_exception_execution_engine ("Invalid internal state, resuming abort after handler block but no resume ip found");
	else
		exc = mono_thread_resume_interruption ();

	if (exc) {
		static void (*restore_context) (MonoContext *);

		if (!restore_context)
			restore_context = mono_get_restore_context ();

		mono_handle_exception (&ctx, exc);
		restore_context (&ctx);
	}

	return resume_ip;
}

gpointer
mono_create_handler_block_trampoline (void)
{
	static gpointer code;

	if (mono_aot_only) {
		g_assert (0);
		return code;
	}

	mono_trampolines_lock ();

	if (!code)
		code = mono_arch_create_handler_block_trampoline ();

	mono_trampolines_unlock ();

	return code;
}
#endif

/*
 * mono_get_trampoline_func:
 *
 *   Return the C function which needs to be called by the generic trampoline of type
 * TRAMP_TYPE.
 */
gconstpointer
mono_get_trampoline_func (MonoTrampolineType tramp_type)
{
	switch (tramp_type) {
	case MONO_TRAMPOLINE_JIT:
	case MONO_TRAMPOLINE_JUMP:
		return mono_magic_trampoline;
	case MONO_TRAMPOLINE_CLASS_INIT:
		return mono_class_init_trampoline;
	case MONO_TRAMPOLINE_GENERIC_CLASS_INIT:
		return mono_generic_class_init_trampoline;
	case MONO_TRAMPOLINE_RGCTX_LAZY_FETCH:
		return mono_rgctx_lazy_fetch_trampoline;
#ifdef MONO_ARCH_AOT_SUPPORTED
	case MONO_TRAMPOLINE_AOT:
		return mono_aot_trampoline;
	case MONO_TRAMPOLINE_AOT_PLT:
		return mono_aot_plt_trampoline;
#endif
#ifdef MONO_ARCH_HAVE_CREATE_DELEGATE_TRAMPOLINE
	case MONO_TRAMPOLINE_DELEGATE:
		return mono_delegate_trampoline;
#endif
	case MONO_TRAMPOLINE_RESTORE_STACK_PROT:
		return mono_altstack_restore_prot;
	case MONO_TRAMPOLINE_GENERIC_VIRTUAL_REMOTING:
		return mono_generic_virtual_remoting_trampoline;
	case MONO_TRAMPOLINE_MONITOR_ENTER:
		return mono_monitor_enter_trampoline;
	case MONO_TRAMPOLINE_MONITOR_EXIT:
		return mono_monitor_exit_trampoline;
	case MONO_TRAMPOLINE_VCALL:
		return mono_vcall_trampoline;
#ifdef MONO_ARCH_HAVE_HANDLER_BLOCK_GUARD
	case MONO_TRAMPOLINE_HANDLER_BLOCK_GUARD:
		return mono_handler_block_guard_trampoline;
#endif
	default:
		g_assert_not_reached ();
		return NULL;
	}
}

static guchar*
create_trampoline_code (MonoTrampolineType tramp_type)
{
	MonoTrampInfo *info;
	guchar *code;

	code = mono_arch_create_generic_trampoline (tramp_type, &info, FALSE);
	if (info) {
		mono_save_trampoline_xdebug_info (info);
		if (mono_jit_map_is_enabled ())
			mono_emit_jit_tramp (info->code, info->code_size, info->name);
		mono_tramp_info_free (info);
	}

	return code;
}

void
mono_trampolines_init (void)
{
	InitializeCriticalSection (&trampolines_mutex);

	if (mono_aot_only)
		return;

	mono_trampoline_code [MONO_TRAMPOLINE_JIT] = create_trampoline_code (MONO_TRAMPOLINE_JIT);
	mono_trampoline_code [MONO_TRAMPOLINE_JUMP] = create_trampoline_code (MONO_TRAMPOLINE_JUMP);
	mono_trampoline_code [MONO_TRAMPOLINE_CLASS_INIT] = create_trampoline_code (MONO_TRAMPOLINE_CLASS_INIT);
	mono_trampoline_code [MONO_TRAMPOLINE_GENERIC_CLASS_INIT] = create_trampoline_code (MONO_TRAMPOLINE_GENERIC_CLASS_INIT);
	mono_trampoline_code [MONO_TRAMPOLINE_RGCTX_LAZY_FETCH] = create_trampoline_code (MONO_TRAMPOLINE_RGCTX_LAZY_FETCH);
#ifdef MONO_ARCH_AOT_SUPPORTED
	mono_trampoline_code [MONO_TRAMPOLINE_AOT] = create_trampoline_code (MONO_TRAMPOLINE_AOT);
	mono_trampoline_code [MONO_TRAMPOLINE_AOT_PLT] = create_trampoline_code (MONO_TRAMPOLINE_AOT_PLT);
#endif
#ifdef MONO_ARCH_HAVE_CREATE_DELEGATE_TRAMPOLINE
	mono_trampoline_code [MONO_TRAMPOLINE_DELEGATE] = create_trampoline_code (MONO_TRAMPOLINE_DELEGATE);
#endif
	mono_trampoline_code [MONO_TRAMPOLINE_RESTORE_STACK_PROT] = create_trampoline_code (MONO_TRAMPOLINE_RESTORE_STACK_PROT);
	mono_trampoline_code [MONO_TRAMPOLINE_GENERIC_VIRTUAL_REMOTING] = create_trampoline_code (MONO_TRAMPOLINE_GENERIC_VIRTUAL_REMOTING);
	mono_trampoline_code [MONO_TRAMPOLINE_MONITOR_ENTER] = create_trampoline_code (MONO_TRAMPOLINE_MONITOR_ENTER);
	mono_trampoline_code [MONO_TRAMPOLINE_MONITOR_EXIT] = create_trampoline_code (MONO_TRAMPOLINE_MONITOR_EXIT);
	mono_trampoline_code [MONO_TRAMPOLINE_VCALL] = create_trampoline_code (MONO_TRAMPOLINE_VCALL);
#ifdef MONO_ARCH_HAVE_HANDLER_BLOCK_GUARD
	mono_trampoline_code [MONO_TRAMPOLINE_HANDLER_BLOCK_GUARD] = create_trampoline_code (MONO_TRAMPOLINE_HANDLER_BLOCK_GUARD);
	mono_create_handler_block_trampoline ();
#endif

	mono_counters_register ("Calls to trampolines", MONO_COUNTER_JIT | MONO_COUNTER_INT, &trampoline_calls);
	mono_counters_register ("JIT trampolines", MONO_COUNTER_JIT | MONO_COUNTER_INT, &jit_trampolines);
	mono_counters_register ("Unbox trampolines", MONO_COUNTER_JIT | MONO_COUNTER_INT, &unbox_trampolines);
	mono_counters_register ("Static rgctx trampolines", MONO_COUNTER_JIT | MONO_COUNTER_INT, &static_rgctx_trampolines);
}

void
mono_trampolines_cleanup (void)
{
	if (class_init_hash_addr)
		g_hash_table_destroy (class_init_hash_addr);
	if (rgctx_lazy_fetch_trampoline_hash)
		g_hash_table_destroy (rgctx_lazy_fetch_trampoline_hash);
	if (rgctx_lazy_fetch_trampoline_hash_addr)
		g_hash_table_destroy (rgctx_lazy_fetch_trampoline_hash_addr);

	DeleteCriticalSection (&trampolines_mutex);
}

guint8 *
mono_get_trampoline_code (MonoTrampolineType tramp_type)
{
	g_assert (mono_trampoline_code [tramp_type]);

	return mono_trampoline_code [tramp_type];
}

gpointer
mono_create_specific_trampoline (gpointer arg1, MonoTrampolineType tramp_type, MonoDomain *domain, guint32 *code_len)
{
	if (mono_aot_only)
		return mono_aot_create_specific_trampoline (mono_defaults.corlib, arg1, tramp_type, domain, code_len);
	else
		return mono_arch_create_specific_trampoline (arg1, tramp_type, domain, code_len);
}

gpointer
mono_create_class_init_trampoline (MonoVTable *vtable)
{
	gpointer code, ptr;
	MonoDomain *domain = vtable->domain;

	g_assert (!vtable->klass->generic_container);

	/* previously created trampoline code */
	mono_domain_lock (domain);
	ptr = 
		g_hash_table_lookup (domain_jit_info (domain)->class_init_trampoline_hash,
								  vtable);
	mono_domain_unlock (domain);
	if (ptr)
		return ptr;

	code = mono_create_specific_trampoline (vtable, MONO_TRAMPOLINE_CLASS_INIT, domain, NULL);

	ptr = mono_create_ftnptr (domain, code);

	/* store trampoline address */
	mono_domain_lock (domain);
	g_hash_table_insert (domain_jit_info (domain)->class_init_trampoline_hash,
							  vtable, ptr);
	mono_domain_unlock (domain);

	mono_trampolines_lock ();
	if (!class_init_hash_addr)
		class_init_hash_addr = g_hash_table_new (NULL, NULL);
	g_hash_table_insert (class_init_hash_addr, ptr, vtable);
	mono_trampolines_unlock ();

	return ptr;
}

gpointer
mono_create_generic_class_init_trampoline (void)
{
#ifdef MONO_ARCH_VTABLE_REG
	static gpointer code;
	MonoTrampInfo *info;

	mono_trampolines_lock ();

	if (!code) {
		if (mono_aot_only)
			/* get_named_code () might return an ftnptr, but our caller expects a direct pointer */
			code = mono_get_addr_from_ftnptr (mono_aot_get_trampoline ("generic_class_init_trampoline"));
		else {
			code = mono_arch_create_generic_class_init_trampoline (&info, FALSE);

			if (info) {
				mono_save_trampoline_xdebug_info (info);
				if (mono_jit_map_is_enabled ())
					mono_emit_jit_tramp (info->code, info->code_size, info->name);
				mono_tramp_info_free (info);
			}
		}
	}

	mono_trampolines_unlock ();

	return code;
#else
	g_assert_not_reached ();
#endif
}

gpointer
mono_create_jump_trampoline (MonoDomain *domain, MonoMethod *method, gboolean add_sync_wrapper)
{
	MonoJitInfo *ji;
	gpointer code;
	guint32 code_size = 0;

	code = mono_jit_find_compiled_method_with_jit_info (domain, method, &ji);
	/*
	 * We cannot recover the correct type of a shared generic
	 * method from its native code address, so we use the
	 * trampoline instead.
	 * For synchronized methods, the trampoline adds the wrapper.
	 */
	if (code && !ji->has_generic_jit_info && !(method->iflags & METHOD_IMPL_ATTRIBUTE_SYNCHRONIZED))
		return code;

	mono_domain_lock (domain);
	code = g_hash_table_lookup (domain_jit_info (domain)->jump_trampoline_hash, method);
	mono_domain_unlock (domain);
	if (code)
		return code;

	code = mono_create_specific_trampoline (method, MONO_TRAMPOLINE_JUMP, mono_domain_get (), &code_size);
	g_assert (code_size);

	ji = mono_domain_alloc0 (domain, MONO_SIZEOF_JIT_INFO);
	ji->code_start = code;
	ji->code_size = code_size;
	ji->method = method;

	/*
	 * mono_delegate_ctor needs to find the method metadata from the 
	 * trampoline address, so we save it here.
	 */

	mono_jit_info_table_add (domain, ji);

	mono_domain_lock (domain);
	g_hash_table_insert (domain_jit_info (domain)->jump_trampoline_hash, method, ji->code_start);
	mono_domain_unlock (domain);

	return ji->code_start;
}

gpointer
mono_create_jit_trampoline_in_domain (MonoDomain *domain, MonoMethod *method)
{
	gpointer tramp;

	if (mono_aot_only) {
		/* Avoid creating trampolines if possible */
		gpointer code = mono_jit_find_compiled_method (domain, method);
		
		if (code)
			return code;
	}

	mono_domain_lock (domain);
	tramp = g_hash_table_lookup (domain_jit_info (domain)->jit_trampoline_hash, method);
	mono_domain_unlock (domain);
	if (tramp)
		return tramp;

	tramp = mono_create_specific_trampoline (method, MONO_TRAMPOLINE_JIT, domain, NULL);
	
	mono_domain_lock (domain);
	g_hash_table_insert (domain_jit_info (domain)->jit_trampoline_hash, method, tramp);
	mono_domain_unlock (domain);

	jit_trampolines++;

	return tramp;
}	

gpointer
mono_create_jit_trampoline (MonoMethod *method)
{
	return mono_create_jit_trampoline_in_domain (mono_domain_get (), method);
}

gpointer
mono_create_jit_trampoline_from_token (MonoImage *image, guint32 token)
{
	gpointer tramp;

	MonoDomain *domain = mono_domain_get ();
	guint8 *buf, *start;

	buf = start = mono_domain_alloc0 (domain, 2 * sizeof (gpointer));

	*(gpointer*)(gpointer)buf = image;
	buf += sizeof (gpointer);
	*(guint32*)(gpointer)buf = token;

	tramp = mono_create_specific_trampoline (start, MONO_TRAMPOLINE_AOT, domain, NULL);

	jit_trampolines++;

	return tramp;
}	

gpointer
mono_create_delegate_trampoline (MonoDomain *domain, MonoClass *klass)
{
#ifdef MONO_ARCH_HAVE_CREATE_DELEGATE_TRAMPOLINE
	gpointer ptr;
	guint32 code_size = 0;
	gpointer *tramp_data;
	MonoMethod *invoke;

	mono_domain_lock (domain);
	ptr = g_hash_table_lookup (domain_jit_info (domain)->delegate_trampoline_hash, klass);
	mono_domain_unlock (domain);
	if (ptr)
		return ptr;

	// Precompute the delegate invoke impl and pass it to the delegate trampoline
	invoke = mono_get_delegate_invoke (klass);
	g_assert (invoke);

	tramp_data = mono_domain_alloc (domain, sizeof (gpointer) * 3);
	tramp_data [0] = invoke;
	tramp_data [1] = mono_arch_get_delegate_invoke_impl (mono_method_signature (invoke), TRUE);
	tramp_data [2] = mono_arch_get_delegate_invoke_impl (mono_method_signature (invoke), FALSE);

	ptr = mono_create_specific_trampoline (tramp_data, MONO_TRAMPOLINE_DELEGATE, domain, &code_size);
	g_assert (code_size);

	/* store trampoline address */
	mono_domain_lock (domain);
	g_hash_table_insert (domain_jit_info (domain)->delegate_trampoline_hash,
							  klass, ptr);
	mono_domain_unlock (domain);

	return ptr;
#else
	return NULL;
#endif
}

gpointer
mono_create_rgctx_lazy_fetch_trampoline (guint32 offset)
{
	static gboolean inited = FALSE;
	static int num_trampolines = 0;
	MonoTrampInfo *info;

	gpointer tramp, ptr;

	if (mono_aot_only)
		return mono_aot_get_lazy_fetch_trampoline (offset);

	mono_trampolines_lock ();
	if (rgctx_lazy_fetch_trampoline_hash)
		tramp = g_hash_table_lookup (rgctx_lazy_fetch_trampoline_hash, GUINT_TO_POINTER (offset));
	else
		tramp = NULL;
	mono_trampolines_unlock ();
	if (tramp)
		return tramp;

	tramp = mono_arch_create_rgctx_lazy_fetch_trampoline (offset, &info, FALSE);
	if (info) {
		mono_save_trampoline_xdebug_info (info);
		if (mono_jit_map_is_enabled ())
			mono_emit_jit_tramp (info->code, info->code_size, info->name);
		mono_tramp_info_free (info);
	}
	ptr = mono_create_ftnptr (mono_get_root_domain (), tramp);

	mono_trampolines_lock ();
	if (!rgctx_lazy_fetch_trampoline_hash) {
		rgctx_lazy_fetch_trampoline_hash = g_hash_table_new (NULL, NULL);
		rgctx_lazy_fetch_trampoline_hash_addr = g_hash_table_new (NULL, NULL);
	}
	g_hash_table_insert (rgctx_lazy_fetch_trampoline_hash, GUINT_TO_POINTER (offset), ptr);
	g_assert (offset != -1);
	g_hash_table_insert (rgctx_lazy_fetch_trampoline_hash_addr, ptr, GUINT_TO_POINTER (offset + 1));
	mono_trampolines_unlock ();

	if (!inited) {
		mono_counters_register ("RGCTX num lazy fetch trampolines",
				MONO_COUNTER_GENERICS | MONO_COUNTER_INT, &num_trampolines);
		inited = TRUE;
	}
	num_trampolines++;

	return ptr;
}

gpointer
mono_create_monitor_enter_trampoline (void)
{
	static gpointer code;

	if (mono_aot_only) {
		if (!code)
			code = mono_aot_get_trampoline ("monitor_enter_trampoline");
		return code;
	}

#ifdef MONO_ARCH_MONITOR_OBJECT_REG
	mono_trampolines_lock ();

	if (!code) {
		MonoTrampInfo *info;

		code = mono_arch_create_monitor_enter_trampoline (&info, FALSE);
		if (info) {
			mono_save_trampoline_xdebug_info (info);
			if (mono_jit_map_is_enabled ())
				mono_emit_jit_tramp (info->code, info->code_size, info->name);
			mono_tramp_info_free (info);
		}
	}

	mono_trampolines_unlock ();
#else
	code = NULL;
	g_assert_not_reached ();
#endif

	return code;
}

gpointer
mono_create_monitor_exit_trampoline (void)
{
	static gpointer code;

	if (mono_aot_only) {
		if (!code)
			code = mono_aot_get_trampoline ("monitor_exit_trampoline");
		return code;
	}

#ifdef MONO_ARCH_MONITOR_OBJECT_REG
	mono_trampolines_lock ();

	if (!code) {
		MonoTrampInfo *info;

		code = mono_arch_create_monitor_exit_trampoline (&info, FALSE);
		if (info) {
			mono_save_trampoline_xdebug_info (info);
			if (mono_jit_map_is_enabled ())
				mono_emit_jit_tramp (info->code, info->code_size, info->name);
			mono_tramp_info_free (info);
		}
	}

	mono_trampolines_unlock ();
#else
	code = NULL;
	g_assert_not_reached ();
#endif
	return code;
}
 
#ifdef MONO_ARCH_LLVM_SUPPORTED
/*
 * mono_create_llvm_imt_trampoline:
 *
 *   LLVM compiled code can't pass in the IMT argument, so we use this trampoline, which
 * sets the IMT argument, then branches to the contents of the vtable slot given by
 * vt_offset in the vtable which is obtained from the argument list.
 */
gpointer
mono_create_llvm_imt_trampoline (MonoDomain *domain, MonoMethod *m, int vt_offset)
{
#ifdef MONO_ARCH_HAVE_LLVM_IMT_TRAMPOLINE
	return mono_arch_get_llvm_imt_trampoline (domain, m, vt_offset);
#else
	g_assert_not_reached ();
	return NULL;
#endif
}
#endif

MonoVTable*
mono_find_class_init_trampoline_by_addr (gconstpointer addr)
{
	MonoVTable *res;

	mono_trampolines_lock ();
	if (class_init_hash_addr)
		res = g_hash_table_lookup (class_init_hash_addr, addr);
	else
		res = NULL;
	mono_trampolines_unlock ();
	return res;
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

static const char*tramp_names [MONO_TRAMPOLINE_NUM] = {
	"jit",
	"jump",
	"class_init",
	"generic_class_init",
	"rgctx_lazy_fetch",
	"aot",
	"aot_plt",
	"delegate",
	"restore_stack_prot",
	"generic_virtual_remoting",
	"monitor_enter",
	"monitor_exit",
	"vcall",
#ifdef MONO_ARCH_HAVE_HANDLER_BLOCK_GUARD
	"handler_block_guard"
#endif
};

/*
 * mono_get_generic_trampoline_name:
 *
 *   Returns a pointer to malloc-ed memory.
 */
char*
mono_get_generic_trampoline_name (MonoTrampolineType tramp_type)
{
	return g_strdup_printf ("generic_trampoline_%s", tramp_names [tramp_type]);
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

