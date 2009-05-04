
#include <config.h>
#include <glib.h>

#include <mono/metadata/appdomain.h>
#include <mono/metadata/metadata-internals.h>
#include <mono/metadata/marshal.h>
#include <mono/metadata/tabledefs.h>
#include <mono/utils/mono-counters.h>

#ifdef HAVE_VALGRIND_MEMCHECK_H
#include <valgrind/memcheck.h>
#endif

#include "mini.h"
#include "debug-mini.h"

/*
 * Address of the trampoline code.  This is used by the debugger to check
 * whether a method is a trampoline.
 */
guint8* mono_trampoline_code [MONO_TRAMPOLINE_NUM];

static GHashTable *class_init_hash_addr = NULL;
static GHashTable *delegate_trampoline_hash_addr = NULL;
static GHashTable *rgctx_lazy_fetch_trampoline_hash = NULL;
static GHashTable *rgctx_lazy_fetch_trampoline_hash_addr = NULL;

#define mono_trampolines_lock() EnterCriticalSection (&trampolines_mutex)
#define mono_trampolines_unlock() LeaveCriticalSection (&trampolines_mutex)
static CRITICAL_SECTION trampolines_mutex;

static gpointer
get_unbox_trampoline (MonoGenericSharingContext *gsctx, MonoMethod *m, gpointer addr)
{
	if (mono_aot_only)
		return mono_aot_get_unbox_trampoline (m);
	else
		return mono_arch_get_unbox_trampoline (gsctx, m, addr);
}

#ifdef MONO_ARCH_HAVE_STATIC_RGCTX_TRAMPOLINE
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
 */
gpointer
mono_create_static_rgctx_trampoline (MonoMethod *m, gpointer addr)
{
	gpointer ctx;
	gpointer res;
	MonoDomain *domain;

	if (mini_method_get_context (m)->method_inst)
		ctx = mono_method_lookup_rgctx (mono_class_vtable (mono_domain_get (), m->klass), mini_method_get_context (m)->method_inst);
	else
		ctx = mono_class_vtable (mono_domain_get (), m->klass);

	if (mono_aot_only)
		return mono_aot_get_static_rgctx_trampoline (ctx, addr);

	domain = mono_domain_get ();

	mono_domain_lock (domain);
	res = g_hash_table_lookup (domain_jit_info (domain)->static_rgctx_trampoline_hash,
							   m);
	mono_domain_unlock (domain);
	if (res)
		return res;

	res = mono_arch_get_static_rgctx_trampoline (m, ctx, addr);

	mono_domain_lock (domain);
	/* Duplicates inserted while we didn't hold the lock are OK */
	g_hash_table_insert (domain_jit_info (domain)->static_rgctx_trampoline_hash, m, res);
	mono_domain_unlock (domain);

	return res;
}
#endif

gpointer*
mono_get_vcall_slot_addr (guint8* code, gpointer *regs)
{
	gpointer vt;
	int displacement;
	vt = mono_arch_get_vcall_slot (code, regs, &displacement);
	if (!vt)
		return NULL;
	return (gpointer*)((char*)vt + displacement);
}

#ifdef MONO_ARCH_HAVE_IMT

static gpointer*
mono_convert_imt_slot_to_vtable_slot (gpointer* slot, gpointer *regs, guint8 *code, MonoMethod *method, MonoMethod **impl_method)
{
	MonoGenericSharingContext *gsctx = mono_get_generic_context_from_code (code);
	MonoObject *this_argument = mono_arch_find_this_argument (regs, method, gsctx);
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
		int interface_offset;
		int imt_slot = MONO_IMT_SIZE + displacement;

		interface_offset = mono_class_interface_offset (vt->klass, imt_method->klass);

		if (interface_offset < 0) {
			g_print ("%s doesn't implement interface %s\n", mono_type_get_name_full (&vt->klass->byval_arg, 0), mono_type_get_name_full (&imt_method->klass->byval_arg, 0));
			g_assert_not_reached ();
		}
		mono_vtable_build_imt_slot (vt, mono_method_get_imt_slot (imt_method));

		if (impl_method) {
			MonoMethod *impl;

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
				impl = mono_class_get_vtable_entry (vt->klass, interface_offset + imt_method->slot);
			}

			*impl_method = impl;
#if DEBUG_IMT
		printf ("mono_convert_imt_slot_to_vtable_slot: method = %s.%s.%s, imt_method = %s.%s.%s\n",
				method->klass->name_space, method->klass->name, method->name, 
				imt_method->klass->name_space, imt_method->klass->name, imt_method->name);
#endif
		}
		g_assert (imt_slot < MONO_IMT_SIZE);
		if (vt->imt_collisions_bitmap & (1 << imt_slot)) {
			int vtable_offset = interface_offset + mono_method_get_vtable_index (imt_method);
			gpointer *vtable_slot = & (vt->vtable [vtable_offset]);
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

/**
 * mono_magic_trampoline:
 *
 *   This trampoline handles calls from JITted code.
 */
gpointer
mono_magic_trampoline (gssize *regs, guint8 *code, MonoMethod *m, guint8* tramp)
{
	gpointer addr;
	gpointer *vtable_slot;
	gboolean generic_shared = FALSE;
	MonoMethod *declaring = NULL;
	MonoMethod *generic_virtual = NULL;
	int context_used;
	gboolean proxy = FALSE;
#ifdef MONO_ARCH_HAVE_STATIC_RGCTX_TRAMPOLINE
	gboolean need_rgctx_tramp = FALSE;
#endif

	if (m == MONO_FAKE_VTABLE_METHOD) {
		int displacement;
		MonoVTable *vt = mono_arch_get_vcall_slot (code, (gpointer*)regs, &displacement);
		if (!vt) {
			int i;
			MonoJitInfo *ji;

			ji = mono_jit_info_table_find (mono_domain_get (), (char*)code);
			if (ji)
				printf ("Caller: %s\n", mono_method_full_name (ji->method, TRUE));
			/* Print some debug info */
			for (i = 0; i < 32; ++i)
				printf ("0x%x ", code [-32 + i]);
			printf ("\n");
			g_assert (vt);
		}
		if (displacement > 0) {
			displacement -= G_STRUCT_OFFSET (MonoVTable, vtable);
			g_assert (displacement >= 0);
			displacement /= sizeof (gpointer);

			/* Avoid loading metadata or creating a generic vtable if possible */
			addr = mono_aot_get_method_from_vt_slot (mono_domain_get (), vt, displacement);
			if (addr && !vt->klass->valuetype) {
				vtable_slot = mono_get_vcall_slot_addr (code, (gpointer*)regs);
				if (mono_aot_is_got_entry (code, (guint8*)vtable_slot) || mono_domain_owns_vtable_slot (mono_domain_get (), vtable_slot)) {
					*vtable_slot = mono_get_addr_from_ftnptr (addr);
				}

				return addr;
			}

			m = mono_class_get_vtable_entry (vt->klass, displacement);
			/*g_print ("%s with disp %d: %s at %p\n", vt->klass->name, displacement, m->name, code);*/
		} else {
			/* We got here from an interface method: redirect to IMT handling */
			m = MONO_FAKE_IMT_METHOD;
			/*g_print ("vtable with disp %d at %p\n", displacement, code);*/
		}
	}

	/* this is the IMT trampoline */
#ifdef MONO_ARCH_HAVE_IMT
	if (m == MONO_FAKE_IMT_METHOD) {
		MonoMethod *impl_method;
		MonoGenericSharingContext *gsctx;
		MonoObject *this_arg;

		/* we get the interface method because mono_convert_imt_slot_to_vtable_slot ()
		 * needs the signature to be able to find the this argument
		 */
		m = mono_arch_find_imt_method ((gpointer*)regs, code);
		vtable_slot = mono_get_vcall_slot_addr (code, (gpointer*)regs);
		g_assert (vtable_slot);

		gsctx = mono_get_generic_context_from_code (code);
		this_arg = mono_arch_find_this_argument ((gpointer*)regs, m, gsctx);

		if (this_arg->vtable->klass == mono_defaults.transparent_proxy_class) {
			/* Use the slow path for now */
			proxy = TRUE;
		    m = mono_object_get_virtual_method (this_arg, m);
		} else {
			vtable_slot = mono_convert_imt_slot_to_vtable_slot (vtable_slot, (gpointer*)regs, code, m, &impl_method);
			/* mono_convert_imt_slot_to_vtable_slot () also gives us the method that is supposed
			 * to be called, so we compile it and go ahead as usual.
			 */
			/*g_print ("imt found method %p (%s) at %p\n", impl_method, impl_method->name, code);*/
			if (m->is_inflated && ((MonoMethodInflated*)m)->context.method_inst) {
				/* Generic virtual method */
				generic_virtual = m;
				m = impl_method;
#ifdef MONO_ARCH_HAVE_STATIC_RGCTX_TRAMPOLINE
				need_rgctx_tramp = TRUE;
#else
				m = mono_marshal_get_static_rgctx_invoke (m);
#endif
			} else {
				m = impl_method;
			}
		}
	}
#endif

	if (m->is_generic) {
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
		generic_virtual = mono_arch_find_imt_method ((gpointer*)regs, code);
#endif
		if (generic_virtual) {
			g_assert (generic_virtual->is_inflated);
			context.method_inst = ((MonoMethodInflated*)generic_virtual)->context.method_inst;
		}

		m = mono_class_inflate_generic_method (declaring, &context);
#ifdef MONO_ARCH_HAVE_STATIC_RGCTX_TRAMPOLINE
		need_rgctx_tramp = TRUE;
#else
		/* FIXME: only do this if the method is sharable */
		m = mono_marshal_get_static_rgctx_invoke (m);
#endif
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
			MonoMethodRuntimeGenericContext *mrgctx = (MonoMethodRuntimeGenericContext*)mono_arch_find_static_call_vtable ((gpointer*)regs, code);

			klass = mrgctx->class_vtable->klass;
			method_inst = mrgctx->method_inst;
#else
			g_assert_not_reached ();
#endif
		} else if ((m->flags & METHOD_ATTRIBUTE_STATIC) || m->klass->valuetype) {
#ifdef MONO_ARCH_RGCTX_REG
			MonoVTable *vtable = mono_arch_find_static_call_vtable ((gpointer*)regs, code);

			klass = vtable->klass;
#else
			g_assert_not_reached ();
#endif
		} else {
#ifdef MONO_ARCH_HAVE_IMT
			MonoObject *this_argument = mono_arch_find_this_argument ((gpointer*)regs, m,
				mono_get_generic_context_from_code (code));

			vt = this_argument->vtable;
			vtable_slot = mono_get_vcall_slot_addr (code, (gpointer*)regs);

			g_assert (this_argument->vtable->klass->inited);
			//mono_class_init (this_argument->vtable->klass);

			if (!vtable_slot)
				klass = this_argument->vtable->klass->supertypes [m->klass->idepth - 1];
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
		g_assert (actual_method->klass == klass);

		if (actual_method->is_inflated)
			declaring = mono_method_get_declaring_generic_method (actual_method);
		else
			declaring = NULL;

		m = actual_method;
	}

	if (m->iflags & METHOD_IMPL_ATTRIBUTE_SYNCHRONIZED) {
		MonoJitInfo *ji;

		if (code)
			ji = mono_jit_info_table_find (mono_domain_get (), (char*)code);
		else
			ji = NULL;

		/* Avoid recursion */
		if (!(ji && ji->method->wrapper_type == MONO_WRAPPER_SYNCHRONIZED))
			m = mono_marshal_get_synchronized_wrapper (m);
	}

	/* Calls made through delegates on platforms without delegate trampolines */
	if (!code && mono_method_needs_static_rgctx_invoke (m, FALSE))
		m = mono_marshal_get_static_rgctx_invoke (m);

	addr = mono_compile_method (m);
	g_assert (addr);

	mono_debugger_trampoline_compiled (m, addr);

#ifdef MONO_ARCH_HAVE_STATIC_RGCTX_TRAMPOLINE
	if (need_rgctx_tramp)
		addr = mono_create_static_rgctx_trampoline (m, addr);
#endif

	if (generic_virtual) {
		int displacement;
 		MonoVTable *vt = mono_arch_get_vcall_slot (code, (gpointer*)regs, &displacement);

		vtable_slot = mono_get_vcall_slot_addr (code, (gpointer*)regs);
		g_assert (vtable_slot);

		if (vt->klass->valuetype)
			addr = get_unbox_trampoline (mono_get_generic_context_from_code (code), m, addr);			

		mono_method_add_generic_virtual_invocation (mono_domain_get (), 
													vt, vtable_slot,
													generic_virtual, addr);

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

	vtable_slot = mono_get_vcall_slot_addr (code, (gpointer*)regs);

	if (vtable_slot) {
		if (m->klass->valuetype)
			addr = get_unbox_trampoline (mono_get_generic_context_from_code (code), m, addr);
		g_assert (*vtable_slot);

		if (!proxy && (mono_aot_is_got_entry (code, (guint8*)vtable_slot) || mono_domain_owns_vtable_slot (mono_domain_get (), vtable_slot))) {
#ifdef MONO_ARCH_HAVE_IMT
			vtable_slot = mono_convert_imt_slot_to_vtable_slot (vtable_slot, (gpointer*)regs, code, m, NULL);
#endif
			*vtable_slot = mono_get_addr_from_ftnptr (addr);
		}
	}
	else {
		guint8 *plt_entry = mono_aot_get_plt_entry (code);

		if (plt_entry) {
			mono_arch_patch_plt_entry (plt_entry, addr);
		} else if (!generic_shared || (m->iflags & METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL) ||
			mono_domain_lookup_shared_generic (mono_domain_get (), declaring)) {
			if (generic_shared) {
				if (m->wrapper_type != MONO_WRAPPER_NONE)
					m = mono_marshal_method_from_wrapper (m);
				g_assert (mono_method_is_generic_sharable_impl (m, FALSE));
			}

			/* Patch calling code */
			if (plt_entry) {

			} else {
				MonoJitInfo *ji = 
					mono_jit_info_table_find (mono_domain_get (), (char*)code);
				MonoJitInfo *target_ji = 
					mono_jit_info_table_find (mono_domain_get (), mono_get_addr_from_ftnptr (addr));

				if (mono_method_same_domain (ji, target_ji))
					mono_arch_patch_callsite (ji->code_start, code, addr);
			}
		}
	}

	return addr;
}
 
#ifdef ENABLE_LLVM
/*
 * mono_llvm_vcall_trampoline:
 *
 *   This trampoline handles virtual calls when using LLVM.
 */
static gpointer
mono_llvm_vcall_trampoline (gssize *regs, guint8 *code, MonoMethod *m, guint8 *tramp)
{
	MonoObject *this;
	gpointer addr;
	MonoVTable *vt;
	gpointer *vtable_slot;
	gboolean proxy = FALSE;

	/* 
	 * We have the method which is called, we need to obtain the vtable slot without
	 * disassembly which is impossible with LLVM.
	 * So we use the this argument.
	 */
	this = mono_arch_get_this_arg_from_call (NULL, mono_method_signature (m), regs, code);
	g_assert (this);

	vt = this->vtable;

	g_assert (!m->is_generic);

	/* This is a simplified version of mono_magic_trampoline () */
	/* FIXME: Avoid code duplication */

	if (m->iflags & METHOD_IMPL_ATTRIBUTE_SYNCHRONIZED) {
		MonoJitInfo *ji;

		if (code)
			ji = mono_jit_info_table_find (mono_domain_get (), (char*)code);
		else
			ji = NULL;

		/* Avoid recursion */
		if (!(ji && ji->method->wrapper_type == MONO_WRAPPER_SYNCHRONIZED))
			m = mono_marshal_get_synchronized_wrapper (m);
	}

	addr = mono_compile_method (m);
	g_assert (addr);

	if (m->klass->valuetype)
		addr = get_unbox_trampoline (mono_get_generic_context_from_code (code), m, addr);

	vtable_slot = &(vt->vtable [mono_method_get_vtable_slot (m)]);
	g_assert (*vtable_slot);

	if (!proxy && (mono_aot_is_got_entry (code, (guint8*)vtable_slot) || mono_domain_owns_vtable_slot (mono_domain_get (), vtable_slot))) {
#ifdef MONO_ARCH_HAVE_IMT
		vtable_slot = mono_convert_imt_slot_to_vtable_slot (vtable_slot, (gpointer*)regs, code, m, NULL);
#endif
		*vtable_slot = mono_get_addr_from_ftnptr (addr);
	  }

	mono_debugger_trampoline_compiled (m, addr);

	return addr;
}
#endif

gpointer
mono_generic_virtual_remoting_trampoline (gssize *regs, guint8 *code, MonoMethod *m, guint8 *tramp)
{
	MonoGenericContext context = { NULL, NULL };
	MonoMethod *imt_method, *declaring;
	gpointer addr;

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
	imt_method = mono_arch_find_imt_method ((gpointer*)regs, code);
	if (imt_method->is_inflated)
		context.method_inst = ((MonoMethodInflated*)imt_method)->context.method_inst;
#endif
	m = mono_class_inflate_generic_method (declaring, &context);
	m = mono_marshal_get_remoting_invoke_with_check (m);

	addr = mono_compile_method (m);
	g_assert (addr);

	mono_debugger_trampoline_compiled (m, addr);

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
mono_aot_trampoline (gssize *regs, guint8 *code, guint8 *token_info, 
					 guint8* tramp)
{
	MonoImage *image;
	guint32 token;
	MonoMethod *method = NULL;
	gpointer addr;
	gpointer *vtable_slot;
	gboolean is_got_entry;
	guint8 *plt_entry;

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

	vtable_slot = mono_get_vcall_slot_addr (code, (gpointer*)regs);
	g_assert (!vtable_slot);

	/* This is a normal call through a PLT entry */
	plt_entry = mono_aot_get_plt_entry (code);
	g_assert (plt_entry);

	mono_arch_patch_plt_entry (plt_entry, addr);

	is_got_entry = FALSE;

	/*
	 * Since AOT code is only used in the root domain, 
	 * mono_domain_get () != mono_get_root_domain () means the calling method
	 * is AppDomain:InvokeInDomain, so this is the same check as in 
	 * mono_method_same_domain () but without loading the metadata for the method.
	 */
	if ((is_got_entry && (mono_domain_get () == mono_get_root_domain ())) || mono_domain_owns_vtable_slot (mono_domain_get (), vtable_slot)) {
#ifdef MONO_ARCH_HAVE_IMT
		if (!method)
			method = mono_get_method (image, token, NULL);
		vtable_slot = mono_convert_imt_slot_to_vtable_slot (vtable_slot, (gpointer*)regs, code, method, NULL);
#endif
		*vtable_slot = addr;
	}

	return addr;
}

/*
 * mono_aot_plt_trampoline:
 *
 *   This trampoline handles calls made from AOT code through the PLT table.
 */
gpointer
mono_aot_plt_trampoline (gssize *regs, guint8 *code, guint8 *aot_module, 
						 guint8* tramp)
{
	guint32 plt_info_offset = mono_aot_get_plt_info_offset (regs, code);

	return mono_aot_plt_resolve (aot_module, plt_info_offset, code);
}
#endif

/**
 * mono_class_init_trampoline:
 *
 * This method calls mono_runtime_class_init () to run the static constructor
 * for the type, then patches the caller code so it is not called again.
 */
void
mono_class_init_trampoline (gssize *regs, guint8 *code, MonoVTable *vtable, guint8 *tramp)
{
	guint8 *plt_entry = mono_aot_get_plt_entry (code);

	mono_runtime_class_init (vtable);

	if (plt_entry) {
		mono_arch_nullify_plt_entry (plt_entry);
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
mono_generic_class_init_trampoline (gssize *regs, guint8 *code, MonoVTable *vtable, guint8 *tramp)
{
	mono_runtime_class_init (vtable);
}

static gpointer
mono_rgctx_lazy_fetch_trampoline (gssize *regs, guint8 *code, gpointer data, guint8 *tramp)
{
#ifdef MONO_ARCH_VTABLE_REG
	static gboolean inited = FALSE;
	static int num_lookups = 0;
	guint32 slot = GPOINTER_TO_UINT (data);
	gpointer arg = (gpointer)(gssize)regs [MONO_ARCH_VTABLE_REG];
	guint32 index = MONO_RGCTX_SLOT_INDEX (slot);
	gboolean mrgctx = MONO_RGCTX_SLOT_IS_MRGCTX (slot);

	if (!inited) {
		mono_counters_register ("RGCTX unmanaged lookups", MONO_COUNTER_GENERICS | MONO_COUNTER_INT, &num_lookups);
		inited = TRUE;
	}

	num_lookups++;

	if (mrgctx)
		return mono_method_fill_runtime_generic_context (arg, index);
	else
		return mono_class_fill_runtime_generic_context (arg, index);
#else
	g_assert_not_reached ();
#endif
}

void
mono_monitor_enter_trampoline (gssize *regs, guint8 *code, MonoObject *obj, guint8 *tramp)
{
	mono_monitor_enter (obj);
}

void
mono_monitor_exit_trampoline (gssize *regs, guint8 *code, MonoObject *obj, guint8 *tramp)
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
mono_delegate_trampoline (gssize *regs, guint8 *code, gpointer *tramp_data, guint8* tramp)
{
	MonoDomain *domain = mono_domain_get ();
	MonoDelegate *delegate;
	MonoJitInfo *ji;
	MonoMethod *m;
	MonoMethod *method = NULL;
	gboolean multicast, callvirt;
#ifdef MONO_ARCH_HAVE_STATIC_RGCTX_TRAMPOLINE
	gboolean need_rgctx_tramp = FALSE;
#endif
	MonoMethod *invoke = tramp_data [0];
	guint8 *impl_this = tramp_data [1];
	guint8 *impl_nothis = tramp_data [2];

	/* Obtain the delegate object according to the calling convention */

	/* 
	 * Avoid calling mono_get_generic_context_from_code () now since it is expensive, 
	 * get_this_arg_from_call will call it if needed.
	 */
	delegate = mono_arch_get_this_arg_from_call (NULL, mono_method_signature (invoke), regs, code);

	if (delegate->method) {
		method = delegate->method;

		/*
		 * delegate->method_ptr == NULL means the delegate was initialized by 
		 * mini_delegate_ctor, while != NULL means it is initialized by 
		 * mono_delegate_ctor_with_method (). In both cases, we need to add wrappers
		 * (ctor_with_method () does this, but it doesn't store the wrapper back into
		 * delegate->method).
		 */
		if (delegate->target && delegate->target->vtable->klass == mono_defaults.transparent_proxy_class)
			method = mono_marshal_get_remoting_invoke (method);
		else if (mono_method_signature (method)->hasthis && method->klass->valuetype)
			method = mono_marshal_get_unbox_wrapper (method);
	} else {
		ji = mono_jit_info_table_find (domain, mono_get_addr_from_ftnptr (delegate->method_ptr));
		if (ji)
			method = ji->method;
	}
	callvirt = !delegate->target && method && mono_method_signature (method)->hasthis;

	if (method && method->iflags & METHOD_IMPL_ATTRIBUTE_SYNCHRONIZED)
		method = mono_marshal_get_synchronized_wrapper (method);

	if (method && mono_method_needs_static_rgctx_invoke (method, FALSE)) {
#ifdef MONO_ARCH_HAVE_STATIC_RGCTX_TRAMPOLINE
		need_rgctx_tramp = TRUE;
#else
		method = mono_marshal_get_static_rgctx_invoke (method);
#endif
	}

	/* 
	 * If the called address is a trampoline, replace it with the compiled method so
	 * further calls don't have to go through the trampoline.
	 */
	if (method && !callvirt) {
		/* Avoid the overhead of looking up an already compiled method if possible */
		if (delegate->method_code && *delegate->method_code) {
			delegate->method_ptr = *delegate->method_code;
		} else {
			delegate->method_ptr = mono_compile_method (method);
			if (delegate->method_code)
				*delegate->method_code = delegate->method_ptr;
			mono_debugger_trampoline_compiled (method, delegate->method_ptr);
		}
	}

#ifdef MONO_ARCH_HAVE_STATIC_RGCTX_TRAMPOLINE
	if (need_rgctx_tramp)
		delegate->method_ptr = mono_create_static_rgctx_trampoline (method, delegate->method_ptr);
#endif

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
	delegate->invoke_impl = mono_get_addr_from_ftnptr (code);
	mono_debugger_trampoline_compiled (m, delegate->invoke_impl);

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
#ifdef ENABLE_LLVM
	case MONO_TRAMPOLINE_LLVM_VCALL:
		return mono_llvm_vcall_trampoline;
#endif
	default:
		g_assert_not_reached ();
		return NULL;
	}
}

void
mono_trampolines_init (void)
{
	InitializeCriticalSection (&trampolines_mutex);

	if (mono_aot_only)
		return;

	mono_trampoline_code [MONO_TRAMPOLINE_JIT] = mono_arch_create_trampoline_code (MONO_TRAMPOLINE_JIT);
	mono_trampoline_code [MONO_TRAMPOLINE_JUMP] = mono_arch_create_trampoline_code (MONO_TRAMPOLINE_JUMP);
	mono_trampoline_code [MONO_TRAMPOLINE_CLASS_INIT] = mono_arch_create_trampoline_code (MONO_TRAMPOLINE_CLASS_INIT);
 	mono_trampoline_code [MONO_TRAMPOLINE_GENERIC_CLASS_INIT] = mono_arch_create_trampoline_code (MONO_TRAMPOLINE_GENERIC_CLASS_INIT);
	mono_trampoline_code [MONO_TRAMPOLINE_RGCTX_LAZY_FETCH] = mono_arch_create_trampoline_code (MONO_TRAMPOLINE_RGCTX_LAZY_FETCH);
#ifdef MONO_ARCH_AOT_SUPPORTED
	mono_trampoline_code [MONO_TRAMPOLINE_AOT] = mono_arch_create_trampoline_code (MONO_TRAMPOLINE_AOT);
	mono_trampoline_code [MONO_TRAMPOLINE_AOT_PLT] = mono_arch_create_trampoline_code (MONO_TRAMPOLINE_AOT_PLT);
#endif
#ifdef MONO_ARCH_HAVE_CREATE_DELEGATE_TRAMPOLINE
	mono_trampoline_code [MONO_TRAMPOLINE_DELEGATE] = mono_arch_create_trampoline_code (MONO_TRAMPOLINE_DELEGATE);
#endif
	mono_trampoline_code [MONO_TRAMPOLINE_RESTORE_STACK_PROT] = mono_arch_create_trampoline_code (MONO_TRAMPOLINE_RESTORE_STACK_PROT);
	mono_trampoline_code [MONO_TRAMPOLINE_GENERIC_VIRTUAL_REMOTING] = mono_arch_create_trampoline_code (MONO_TRAMPOLINE_GENERIC_VIRTUAL_REMOTING);
	mono_trampoline_code [MONO_TRAMPOLINE_MONITOR_ENTER] = mono_arch_create_trampoline_code (MONO_TRAMPOLINE_MONITOR_ENTER);
	mono_trampoline_code [MONO_TRAMPOLINE_MONITOR_EXIT] = mono_arch_create_trampoline_code (MONO_TRAMPOLINE_MONITOR_EXIT);
#ifdef ENABLE_LLVM
	mono_trampoline_code [MONO_TRAMPOLINE_LLVM_VCALL] = mono_arch_create_trampoline_code (MONO_TRAMPOLINE_LLVM_VCALL);
#endif
}

void
mono_trampolines_cleanup (void)
{
	if (class_init_hash_addr)
		g_hash_table_destroy (class_init_hash_addr);
	if (delegate_trampoline_hash_addr)
		g_hash_table_destroy (delegate_trampoline_hash_addr);

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

	mono_trampolines_lock ();

	if (!code) {
		if (mono_aot_only)
			code = mono_aot_get_named_code ("generic_class_init_trampoline");
		else
			code = mono_arch_create_generic_class_init_trampoline ();
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
	 */
	if (code && !ji->has_generic_jit_info)
		return code;

	mono_domain_lock (domain);
	code = g_hash_table_lookup (domain_jit_info (domain)->jump_trampoline_hash, method);
	mono_domain_unlock (domain);
	if (code)
		return code;

	code = mono_create_specific_trampoline (method, MONO_TRAMPOLINE_JUMP, mono_domain_get (), &code_size);
	g_assert (code_size);

	ji = mono_domain_alloc0 (domain, sizeof (MonoJitInfo));
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

	mono_jit_stats.method_trampolines++;

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

	buf = start = mono_domain_code_reserve (domain, 2 * sizeof (gpointer));

	*(gpointer*)(gpointer)buf = image;
	buf += sizeof (gpointer);
	*(guint32*)(gpointer)buf = token;

	tramp = mono_create_specific_trampoline (start, MONO_TRAMPOLINE_AOT, domain, NULL);

	mono_jit_stats.method_trampolines++;

	return tramp;
}	

gpointer
mono_create_delegate_trampoline (MonoClass *klass)
{
#ifdef MONO_ARCH_HAVE_CREATE_DELEGATE_TRAMPOLINE
	MonoDomain *domain = mono_domain_get ();
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

	ptr = mono_create_specific_trampoline (tramp_data, MONO_TRAMPOLINE_DELEGATE, mono_domain_get (), &code_size);
	g_assert (code_size);

	/* store trampoline address */
	mono_domain_lock (domain);
	g_hash_table_insert (domain_jit_info (domain)->delegate_trampoline_hash,
							  klass, ptr);
	mono_domain_unlock (domain);

	mono_trampolines_lock ();
	if (!delegate_trampoline_hash_addr)
		delegate_trampoline_hash_addr = g_hash_table_new (NULL, NULL);
	g_hash_table_insert (delegate_trampoline_hash_addr, ptr, klass);
	mono_trampolines_unlock ();

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

	tramp = mono_arch_create_rgctx_lazy_fetch_trampoline (offset);
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
			code = mono_aot_get_named_code ("monitor_enter_trampoline");
		return code;
	}

#ifdef MONO_ARCH_MONITOR_OBJECT_REG
	mono_trampolines_lock ();

	if (!code)
		code = mono_arch_create_monitor_enter_trampoline ();

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
			code = mono_aot_get_named_code ("monitor_exit_trampoline");
		return code;
	}

#ifdef MONO_ARCH_MONITOR_OBJECT_REG
	mono_trampolines_lock ();

	if (!code)
		code = mono_arch_create_monitor_exit_trampoline ();

	mono_trampolines_unlock ();
#else
	code = NULL;
	g_assert_not_reached ();
#endif
	return code;
}
 
#ifdef ENABLE_LLVM
/*
 * mono_create_llvm_vcall_trampoline:
 *
 *  LLVM emits code for virtual calls which mono_get_vcall_slot is unable to
 * decode, i.e. only the final branch address is available:
 * mov <offset>(%rax), %rax
 * <random code inserted by instruction scheduling>
 * call *%rax
 *
 * To work around this problem, we don't use the common vtable trampoline when
 * llvm is enabled. Instead, we use one trampoline per method.
 */
gpointer
mono_create_llvm_vcall_trampoline (MonoMethod *method)
{
	MonoDomain *domain;
	gpointer res;

	domain = mono_domain_get ();

	mono_domain_lock (domain);
	res = g_hash_table_lookup (domain_jit_info (domain)->llvm_vcall_trampoline_hash, method);
	mono_domain_unlock (domain);
	if (res)
		return res;

	res = mono_create_specific_trampoline (method, MONO_TRAMPOLINE_LLVM_VCALL, domain, NULL);

	mono_domain_lock (domain);
	g_hash_table_insert (domain_jit_info (domain)->llvm_vcall_trampoline_hash, method, res);
	mono_domain_unlock (domain);

	return res;
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

MonoClass*
mono_find_delegate_trampoline_by_addr (gconstpointer addr)
{
	MonoClass *res;

	mono_trampolines_lock ();
	if (delegate_trampoline_hash_addr)
		res = g_hash_table_lookup (delegate_trampoline_hash_addr, addr);
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
