
#include <config.h>
#include <glib.h>

#include <mono/metadata/appdomain.h>
#include <mono/metadata/metadata-internals.h>
#include <mono/metadata/marshal.h>
#include <mono/metadata/tabledefs.h>

#ifdef HAVE_VALGRIND_MEMCHECK_H
#include <valgrind/memcheck.h>
#endif

#include "mini.h"

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

	addr = mono_compile_method (m);
	g_assert (addr);

	/* the method was jumped to */
	if (!code)
		return addr;

	vtable_slot = mono_arch_get_vcall_slot_addr (code, (gpointer*)regs);

	if (vtable_slot) {
		if (m->klass->valuetype)
			addr = mono_arch_get_unbox_trampoline (m, addr);

		g_assert (*vtable_slot);

		if (mono_aot_is_got_entry (code, (guint8*)vtable_slot) || mono_domain_owns_vtable_slot (mono_domain_get (), vtable_slot))
			*vtable_slot = mono_get_addr_from_ftnptr (addr);
	}
	else {
		/* Patch calling code */

		MonoJitInfo *ji = 
			mono_jit_info_table_find (mono_domain_get (), code);
		MonoJitInfo *target_ji = 
			mono_jit_info_table_find (mono_domain_get (), mono_get_addr_from_ftnptr (addr));

		if (mono_method_same_domain (ji, target_ji))
			mono_arch_patch_callsite (code, addr);
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
	g_assert (vtable_slot);

	is_got_entry = mono_aot_is_got_entry (code, (guint8*)vtable_slot);

	if (!is_got_entry) {
		if (!method)
			method = mono_get_method (image, token, NULL);
		if (method->klass->valuetype)
			addr = mono_arch_get_unbox_trampoline (method, addr);
	}

	/*
	 * Since AOT code is only used in the root domain, 
	 * mono_domain_get () != mono_get_root_domain () means the calling method
	 * is AppDomain:InvokeInDomain, so this is the same check as in 
	 * mono_method_same_domain () but without loading the metadata for the method.
	 */
	if ((is_got_entry && (mono_domain_get () == mono_get_root_domain ())) || mono_domain_owns_vtable_slot (mono_domain_get (), vtable_slot))
		*vtable_slot = addr;

	return addr;
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
	mono_runtime_class_init (vtable);

	if (!mono_running_on_valgrind ())
		mono_arch_nullify_class_init_trampoline (code, regs);
}

/**
 * mono_delegate_trampoline:
 *
 *   This trampoline handles calls made from the delegate invoke wrapper. It patches
 * the function address inside the delegate.
 */
gpointer
mono_delegate_trampoline (gssize *regs, guint8 *code, MonoMethod *m, guint8* tramp)
{
	gpointer addr;
	gpointer *vtable_slot;

	addr = mono_compile_method (m);
	g_assert (addr);

	mono_arch_patch_delegate_trampoline (code, regs, addr);

	return addr;
}
