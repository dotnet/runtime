
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

#define mono_trampolines_lock() EnterCriticalSection (&trampolines_mutex)
#define mono_trampolines_unlock() LeaveCriticalSection (&trampolines_mutex)
static CRITICAL_SECTION trampolines_mutex;

static MonoGenericSharingContext*
get_generic_context (guint8 *code)
{
	MonoJitInfo *jit_info = mono_jit_info_table_find (mono_domain_get (), (char*)code);

	g_assert (jit_info);

	return mono_jit_info_get_generic_sharing_context (jit_info);
}

#ifdef MONO_ARCH_HAVE_IMT

static gpointer*
mono_convert_imt_slot_to_vtable_slot (gpointer* slot, gpointer *regs, guint8 *code, MonoMethod *method, MonoMethod **impl_method)
{
	MonoGenericSharingContext *gsctx = get_generic_context (code);
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

		mono_class_setup_vtable (vt->klass);
		interface_offset = mono_class_interface_offset (vt->klass, imt_method->klass);

		if (interface_offset < 0) {
			g_print ("%s doesn't implement interface %s\n", mono_type_get_name_full (&vt->klass->byval_arg, 0), mono_type_get_name_full (&imt_method->klass->byval_arg, 0));
			g_assert_not_reached ();
		}
		mono_vtable_build_imt_slot (vt, mono_method_get_imt_slot (imt_method));

		if (impl_method)
			*impl_method = vt->klass->vtable [interface_offset + imt_method->slot];
#if DEBUG_IMT
		printf ("mono_convert_imt_slot_to_vtable_slot: method = %s.%s.%s, imt_method = %s.%s.%s\n",
				method->klass->name_space, method->klass->name, method->name, 
				imt_method->klass->name_space, imt_method->klass->name, imt_method->name);
#endif
		g_assert (imt_slot < MONO_IMT_SIZE);
		if (vt->imt_collisions_bitmap & (1 << imt_slot)) {
			int vtable_offset = interface_offset + imt_method->slot;
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

#if MONO_ARCH_COMMON_VTABLE_TRAMPOLINE
	if (m == MONO_FAKE_VTABLE_METHOD) {
		int displacement;
		MonoVTable *vt = mono_arch_get_vcall_slot (code, (gpointer*)regs, &displacement);
		g_assert (vt);
		if (displacement > 0) {
			displacement -= G_STRUCT_OFFSET (MonoVTable, vtable);
			g_assert (displacement >= 0);
			displacement /= sizeof (gpointer);

			/* Avoid loading metadata or creating a generic vtable if possible */
			addr = mono_aot_get_method_from_vt_slot (mono_domain_get (), vt, displacement);
			if (addr && !vt->klass->valuetype) {
				vtable_slot = mono_arch_get_vcall_slot_addr (code, (gpointer*)regs);
				if (mono_aot_is_got_entry (code, (guint8*)vtable_slot) || mono_domain_owns_vtable_slot (mono_domain_get (), vtable_slot)) {
					*vtable_slot = mono_get_addr_from_ftnptr (addr);
				}

				return addr;
			}

			mono_class_setup_vtable (vt->klass);
			m = vt->klass->vtable [displacement];
			if (m->iflags & METHOD_IMPL_ATTRIBUTE_SYNCHRONIZED)
				m = mono_marshal_get_synchronized_wrapper (m);
			/*g_print ("%s with disp %d: %s at %p\n", vt->klass->name, displacement, m->name, code);*/
		} else {
			/* We got here from an interface method: redirect to IMT handling */
			m = MONO_FAKE_IMT_METHOD;
			/*g_print ("vtable with disp %d at %p\n", displacement, code);*/
		}
	}
#endif
	/* this is the IMT trampoline */
#ifdef MONO_ARCH_HAVE_IMT
	if (m == MONO_FAKE_IMT_METHOD) {
		MonoMethod *impl_method;
		/* we get the interface method because mono_convert_imt_slot_to_vtable_slot ()
		 * needs the signature to be able to find the this argument
		 */
		m = mono_arch_find_imt_method ((gpointer*)regs, code);
		vtable_slot = mono_arch_get_vcall_slot_addr (code, (gpointer*)regs);
		g_assert (vtable_slot);
		vtable_slot = mono_convert_imt_slot_to_vtable_slot (vtable_slot, (gpointer*)regs, code, m, &impl_method);
		/* mono_convert_imt_slot_to_vtable_slot () also gives us the method that is supposed
		 * to be called, so we compile it and go ahead as usual.
		 */
		/*g_print ("imt found method %p (%s) at %p\n", impl_method, impl_method->name, code);*/
		m = impl_method;
	}
#endif

	if (mono_method_check_context_used (m)) {
		MonoClass *klass = NULL;
		MonoMethod *actual_method = NULL;
		MonoVTable *vt = NULL;

		vtable_slot = NULL;
		generic_shared = TRUE;

		g_assert (code);


		if (m->flags & METHOD_ATTRIBUTE_STATIC) {
#ifdef MONO_ARCH_RGCTX_REG
			MonoVTable *vtable = mono_arch_find_static_call_vtable ((gpointer*)regs, code);

			klass = vtable->klass;
#else
			g_assert_not_reached ();
#endif
		} else {
#ifdef MONO_ARCH_HAVE_IMT
			MonoObject *this_argument = mono_arch_find_this_argument ((gpointer*)regs, m,
				get_generic_context (code));

			vt = this_argument->vtable;
			vtable_slot = mono_arch_get_vcall_slot_addr (code, (gpointer*)regs);

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
		} else {
			int i;

			if (m->is_inflated)
				declaring = mono_method_get_declaring_generic_method (m);
			else
				declaring = m;

			if (klass->generic_class && !klass->methods) {
				/* Avoid calling setup_methods () if possible */
				actual_method = mono_class_inflate_generic_method_full (declaring, klass, mono_class_get_context (klass));
			} else {
				mono_class_setup_methods (klass);
				for (i = 0; i < klass->method.count; ++i) {
					actual_method = klass->methods [i];
					if (actual_method->is_inflated) {
						if (mono_method_get_declaring_generic_method (actual_method) == declaring)
							break;
					}
				}
			}

			g_assert (mono_method_get_declaring_generic_method (actual_method) == declaring);
		}

		g_assert (actual_method);
		m = actual_method;
	}

	addr = mono_compile_method (m);
	g_assert (addr);

	mono_debugger_trampoline_compiled (m, addr);

	/* the method was jumped to */
	if (!code)
		return addr;

	vtable_slot = mono_arch_get_vcall_slot_addr (code, (gpointer*)regs);

	if (vtable_slot) {
		if (m->klass->valuetype)
			addr = mono_arch_get_unbox_trampoline (m, addr);

		g_assert (*vtable_slot);

		if (mono_aot_is_got_entry (code, (guint8*)vtable_slot) || mono_domain_owns_vtable_slot (mono_domain_get (), vtable_slot)) {
#ifdef MONO_ARCH_HAVE_IMT
			vtable_slot = mono_convert_imt_slot_to_vtable_slot (vtable_slot, (gpointer*)regs, code, m, NULL);
#endif
			*vtable_slot = mono_get_addr_from_ftnptr (addr);
		}
	}
	else if (!generic_shared || mono_domain_lookup_shared_generic (mono_domain_get (), declaring)) {
		guint8 *plt_entry = mono_aot_get_plt_entry (code);

		/* Patch calling code */
		if (plt_entry) {
			mono_arch_patch_plt_entry (plt_entry, addr);
		} else {
			MonoJitInfo *ji = 
				mono_jit_info_table_find (mono_domain_get (), (char*)code);
			MonoJitInfo *target_ji = 
				mono_jit_info_table_find (mono_domain_get (), mono_get_addr_from_ftnptr (addr));

			if (mono_method_same_domain (ji, target_ji))
				mono_arch_patch_callsite (ji->code_start, code, addr);
		}
	}

	return addr;
}

/*
 * mono_aot_trampoline:
 *
 *   This trampoline handles calls made from AOT code. We try to bypass the 
 * normal JIT compilation logic to avoid loading the metadata for the method.
 */
#ifdef MONO_ARCH_HAVE_CREATE_TRAMPOLINE_FROM_TOKEN
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

	image = *(gpointer*)(gpointer)token_info;
	token_info += sizeof (gpointer);
	token = *(guint32*)(gpointer)token_info;

	addr = mono_aot_get_method_from_token (mono_domain_get (), image, token);
	if (!addr) {
		method = mono_get_method (image, token, NULL);
		g_assert (method);

		//printf ("F: %s\n", mono_method_full_name (method, TRUE));

		if (method->iflags & METHOD_IMPL_ATTRIBUTE_SYNCHRONIZED)
			method = mono_marshal_get_synchronized_wrapper (method);

		addr = mono_compile_method (method);
		g_assert (addr);
	}

	vtable_slot = mono_arch_get_vcall_slot_addr (code, (gpointer*)regs);

	if (vtable_slot) {
		is_got_entry = mono_aot_is_got_entry (code, (guint8*)vtable_slot);

		if (!is_got_entry) {
			if (!method)
				method = mono_get_method (image, token, NULL);
			if (method->klass->valuetype)
				addr = mono_arch_get_unbox_trampoline (method, addr);
		}
	} else {
		/* This is a normal call through a PLT entry */
		guint8 *plt_entry = mono_aot_get_plt_entry (code);

		g_assert (plt_entry);

		mono_arch_patch_plt_entry (plt_entry, addr);

		is_got_entry = FALSE;
	}

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
#ifdef MONO_ARCH_AOT_PLT_OFFSET_REG
	guint32 plt_info_offset = regs [MONO_ARCH_AOT_PLT_OFFSET_REG];
#else
	guint32 plt_info_offset = -1;
#endif

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

	if (!mono_running_on_valgrind ()) {
		if (plt_entry) {
			mono_arch_nullify_plt_entry (plt_entry);
		} else {
			mono_arch_nullify_class_init_trampoline (code, regs);
		}
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
	//g_print ("generic class init for class %s.%s\n", vtable->klass->name_space, vtable->klass->name);

	mono_runtime_class_init (vtable);

	//g_print ("done initing generic\n");
}

static gpointer
mono_rgctx_lazy_fetch_trampoline (gssize *regs, guint8 *code, MonoVTable *vtable, guint8 *tramp)
{
	static gboolean inited = FALSE;
	static int num_lookups = 0;

	guint32 slot = mono_arch_get_rgctx_lazy_fetch_offset ((gpointer*)regs);

	if (!inited) {
		mono_counters_register ("RGCTX unmanaged lookups", MONO_COUNTER_GENERICS | MONO_COUNTER_INT, &num_lookups);
		inited = TRUE;
	}

	num_lookups++;

	return mono_class_fill_runtime_generic_context (vtable, slot);
}

#ifdef MONO_ARCH_HAVE_CREATE_DELEGATE_TRAMPOLINE

/**
 * mono_delegate_trampoline:
 *
 *   This trampoline handles calls made to Delegate:Invoke ().
 */
gpointer
mono_delegate_trampoline (gssize *regs, guint8 *code, MonoClass *klass, guint8* tramp)
{
	MonoDomain *domain = mono_domain_get ();
	MonoDelegate *delegate;
	MonoJitInfo *ji;
	MonoMethod *invoke, *m;
	MonoMethod *method = NULL;
	gboolean multicast, callvirt;

	invoke = mono_get_delegate_invoke (klass);
	g_assert (invoke);

	/* Obtain the delegate object according to the calling convention */

	delegate = mono_arch_get_this_arg_from_call (mono_method_signature (invoke), regs, code);

	if (!delegate->method_ptr && delegate->method) {
		/* The delegate was initialized by mini_delegate_ctor */
		method = delegate->method;

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

	/* 
	 * If the called address is a trampoline, replace it with the compiled method so
	 * further calls don't have to go through the trampoline.
	 */
	if (method && !callvirt) {
		delegate->method_ptr = mono_compile_method (method);
		mono_debugger_trampoline_compiled (method, delegate->method_ptr);
	}

	multicast = ((MonoMulticastDelegate*)delegate)->prev != NULL;
	if (!multicast && !callvirt) {
		code = mono_arch_get_delegate_invoke_impl (mono_method_signature (invoke), delegate->target != NULL);

		if (code) {
			delegate->invoke_impl = code;
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
	case MONO_TRAMPOLINE_GENERIC:
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
	default:
		g_assert_not_reached ();
		return NULL;
	}
}

void
mono_trampolines_init (void)
{
	InitializeCriticalSection (&trampolines_mutex);

	mono_trampoline_code [MONO_TRAMPOLINE_GENERIC] = mono_arch_create_trampoline_code (MONO_TRAMPOLINE_GENERIC);
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
	return mono_trampoline_code [tramp_type];
}

gpointer
mono_create_class_init_trampoline (MonoVTable *vtable)
{
	gpointer code, ptr;

	g_assert (!vtable->klass->generic_container);

	/* previously created trampoline code */
	mono_domain_lock (vtable->domain);
	ptr = 
		g_hash_table_lookup (vtable->domain->class_init_trampoline_hash,
								  vtable);
	mono_domain_unlock (vtable->domain);
	if (ptr)
		return ptr;

	code = mono_arch_create_specific_trampoline (vtable, MONO_TRAMPOLINE_CLASS_INIT, vtable->domain, NULL);

	ptr = mono_create_ftnptr (vtable->domain, code);

	/* store trampoline address */
	mono_domain_lock (vtable->domain);
	g_hash_table_insert (vtable->domain->class_init_trampoline_hash,
							  vtable, ptr);
	mono_domain_unlock (vtable->domain);

	mono_trampolines_lock ();
	if (!class_init_hash_addr)
		class_init_hash_addr = g_hash_table_new (NULL, NULL);
	g_hash_table_insert (class_init_hash_addr, ptr, vtable);
	mono_trampolines_unlock ();

	return ptr;
}

gpointer
mono_create_jump_trampoline (MonoDomain *domain, MonoMethod *method, 
							 gboolean add_sync_wrapper)
{
	MonoJitInfo *ji;
	gpointer code;
	guint32 code_size = 0;

	if (add_sync_wrapper && method->iflags & METHOD_IMPL_ATTRIBUTE_SYNCHRONIZED)
		return mono_create_jump_trampoline (domain, mono_marshal_get_synchronized_wrapper (method), FALSE);

	code = mono_jit_find_compiled_method (domain, method);
	if (code)
		return code;

	mono_domain_lock (domain);
	code = g_hash_table_lookup (domain->jump_trampoline_hash, method);
	mono_domain_unlock (domain);
	if (code)
		return code;

	code = mono_arch_create_specific_trampoline (method, MONO_TRAMPOLINE_JUMP, mono_domain_get (), &code_size);
	g_assert (code_size);

	mono_domain_lock (domain);
	ji = mono_mempool_alloc0 (domain->mp, sizeof (MonoJitInfo));
	mono_domain_unlock (domain);
	ji->code_start = code;
	ji->code_size = code_size;
	ji->method = method;

	/*
	 * mono_delegate_ctor needs to find the method metadata from the 
	 * trampoline address, so we save it here.
	 */

	mono_jit_info_table_add (domain, ji);

	mono_domain_lock (domain);
	g_hash_table_insert (domain->jump_trampoline_hash, method, ji->code_start);
	mono_domain_unlock (domain);

	return ji->code_start;
}

gpointer
mono_create_jit_trampoline_in_domain (MonoDomain *domain, MonoMethod *method)
{
	gpointer tramp;

	mono_domain_lock (domain);
	tramp = g_hash_table_lookup (domain->jit_trampoline_hash, method);
	mono_domain_unlock (domain);
	if (tramp)
		return tramp;

	if (method->iflags & METHOD_IMPL_ATTRIBUTE_SYNCHRONIZED)
		return mono_create_jit_trampoline (mono_marshal_get_synchronized_wrapper (method));

	tramp = mono_arch_create_specific_trampoline (method, MONO_TRAMPOLINE_GENERIC, domain, NULL);
	
	mono_domain_lock (domain);
	g_hash_table_insert (domain->jit_trampoline_hash, method, tramp);
	mono_domain_unlock (domain);

	mono_jit_stats.method_trampolines++;

	return tramp;
}	

gpointer
mono_create_jit_trampoline (MonoMethod *method)
{
	return mono_create_jit_trampoline_in_domain (mono_domain_get (), method);
}

#ifdef MONO_ARCH_HAVE_CREATE_TRAMPOLINE_FROM_TOKEN
gpointer
mono_create_jit_trampoline_from_token (MonoImage *image, guint32 token)
{
	gpointer tramp;

	MonoDomain *domain = mono_domain_get ();
	guint8 *buf, *start;

	mono_domain_lock (domain);
	buf = start = mono_code_manager_reserve (domain->code_mp, 2 * sizeof (gpointer));
	mono_domain_unlock (domain);

	*(gpointer*)(gpointer)buf = image;
	buf += sizeof (gpointer);
	*(guint32*)(gpointer)buf = token;

	tramp = mono_arch_create_specific_trampoline (start, MONO_TRAMPOLINE_AOT, domain, NULL);

	mono_jit_stats.method_trampolines++;

	return tramp;
}	
#endif

gpointer
mono_create_delegate_trampoline (MonoClass *klass)
{
#ifdef MONO_ARCH_HAVE_CREATE_DELEGATE_TRAMPOLINE
	MonoDomain *domain = mono_domain_get ();
	gpointer ptr;
	guint32 code_size = 0;

	mono_domain_lock (domain);
	ptr = g_hash_table_lookup (domain->delegate_trampoline_hash, klass);
	mono_domain_unlock (domain);
	if (ptr)
		return ptr;

    ptr = mono_arch_create_specific_trampoline (klass, MONO_TRAMPOLINE_DELEGATE, mono_domain_get (), &code_size);
	g_assert (code_size);

	/* store trampoline address */
	mono_domain_lock (domain);
	g_hash_table_insert (domain->delegate_trampoline_hash,
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
