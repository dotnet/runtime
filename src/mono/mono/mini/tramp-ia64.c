/*
 * tramp-ia64.c: JIT trampoline code for ia64
 *
 * Authors:
 *   Zoltan Varga (vargaz@gmail.com)
 *
 * (C) 2001 Ximian, Inc.
 */

#include <config.h>
#include <glib.h>

#include <mono/metadata/appdomain.h>
#include <mono/metadata/marshal.h>
#include <mono/metadata/tabledefs.h>
#include <mono/arch/ia64/ia64-codegen.h>
#include <mono/metadata/mono-debug-debugger.h>

#include "mini.h"
#include "mini-ia64.h"

#define NOT_IMPLEMENTED g_assert_not_reached ()

/*
 * get_unbox_trampoline:
 * @m: method pointer
 * @addr: pointer to native code for @m
 *
 * when value type methods are called through the vtable we need to unbox the
 * this argument. This method returns a pointer to a trampoline which does
 * unboxing before calling the method
 */
static gpointer
get_unbox_trampoline (MonoMethod *m, gpointer addr)
{
	NOT_IMPLEMENTED;
	return NULL;
}

/**
 * ia64_magic_trampoline:
 */
static gpointer
ia64_magic_trampoline (long *regs, guint8 *code, MonoMethod *m, guint8* tramp)
{
	gpointer addr;
	gpointer *vtable_slot;

	NOT_IMPLEMENTED;

	return NULL;
}

/*
 * ia64_aot_trampoline:
 *
 *   This trampoline handles calls made from AOT code. We try to bypass the 
 * normal JIT compilation logic to avoid loading the metadata for the method.
 */
static gpointer
ia64_aot_trampoline (long *regs, guint8 *code, guint8 *token_info, 
					  guint8* tramp)
{
	NOT_IMPLEMENTED;

	return NULL;
}

/**
 * ia64_class_init_trampoline:
 *
 * This method calls mono_runtime_class_init () to run the static constructor
 * for the type, then patches the caller code so it is not called again.
 */
static void
ia64_class_init_trampoline (long *regs, guint8 *code, MonoVTable *vtable, guint8 *tramp)
{
	NOT_IMPLEMENTED;

	return NULL;
}

guchar*
mono_arch_create_trampoline_code (MonoTrampolineType tramp_type)
{
	guint8 *buf;
	int i, lmf_offset, offset, method_offset, tramp_offset, saved_regs_offset, saved_fpregs_offset, framesize;
	gboolean has_caller;
	Ia64CodegenState code;

	if (tramp_type == MONO_TRAMPOLINE_JUMP)
		has_caller = FALSE;
	else
		has_caller = TRUE;

	buf = mono_global_codeman_reserve (512);

	ia64_codegen_init (code, buf);

	/* FIXME: */
	ia64_break_i (code, 0);
	ia64_codegen_close (code);

	g_assert ((code.buf - buf) <= 512);

	mono_arch_flush_icache (buf, code.buf - buf);

	return buf;
}

#define TRAMPOLINE_SIZE 34

static MonoJitInfo*
create_specific_trampoline (gpointer arg1, MonoTrampolineType tramp_type, MonoDomain *domain)
{
	NOT_IMPLEMENTED;

	return NULL;
}

MonoJitInfo*
mono_arch_create_jump_trampoline (MonoMethod *method)
{
	MonoJitInfo *ji = create_specific_trampoline (method, MONO_TRAMPOLINE_JUMP, mono_domain_get ());

	ji->method = method;
	return ji;
}

gpointer
mono_arch_create_jit_trampoline (MonoMethod *method)
{
	MonoJitInfo *ji;
	gpointer code_start;

	ji = create_specific_trampoline (method, MONO_TRAMPOLINE_GENERIC, mono_domain_get ());
	code_start = ji->code_start;
	g_free (ji);

	return code_start;
}

gpointer
mono_arch_create_jit_trampoline_from_token (MonoImage *image, guint32 token)
{
	MonoDomain *domain = mono_domain_get ();
	MonoJitInfo *ji;
	gpointer code_start;
	guint8 *buf, *start;

	mono_domain_lock (domain);
	buf = start = mono_code_manager_reserve (domain->code_mp, 2 * sizeof (gpointer));
	mono_domain_unlock (domain);

	*(gpointer*)buf = image;
	buf += sizeof (gpointer);
	*(guint32*)buf = token;

	ji = create_specific_trampoline (start, MONO_TRAMPOLINE_AOT, domain);
	code_start = ji->code_start;
	g_free (ji);

	return code_start;
}

/**
 * mono_arch_create_class_init_trampoline:
 *  @vtable: the type to initialize
 *
 * Creates a trampoline function to run a type initializer. 
 * If the trampoline is called, it calls mono_runtime_class_init with the
 * given vtable, then patches the caller code so it does not get called any
 * more.
 * 
 * Returns: a pointer to the newly created code 
 */
gpointer
mono_arch_create_class_init_trampoline (MonoVTable *vtable)
{
	MonoJitInfo *ji;
	gpointer code;

	ji = create_specific_trampoline (vtable, MONO_TRAMPOLINE_CLASS_INIT, vtable->domain);
	code = ji->code_start;
	g_free (ji);

	return code;
}

void
mono_arch_invalidate_method (MonoJitInfo *ji, void *func, gpointer func_arg)
{
	NOT_IMPLEMENTED;
}

gpointer
mono_debugger_create_notification_function (gpointer *notification_address)
{
	NOT_IMPLEMENTED;

	return NULL;
}
