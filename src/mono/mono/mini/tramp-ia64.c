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

#define GP_SCRATCH_REG 31

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

	addr = mono_compile_method (m);
	g_assert (addr);

	//printf ("ENTER: %s\n", mono_method_full_name (m, TRUE));

	/* the method was jumped to */
	if (!code)
		/* FIXME: Optimize the case when the call is from a delegate wrapper */
		return addr;

	vtable_slot = mono_arch_get_vcall_slot_addr (code, (gpointer*)regs);

	if (vtable_slot) {
		if (m->klass->valuetype)
			addr = get_unbox_trampoline (m, addr);

		g_assert (*vtable_slot);

		if (mono_aot_is_got_entry (code, (guint8*)vtable_slot) || mono_domain_owns_vtable_slot (mono_domain_get (), vtable_slot))
			*vtable_slot = addr;
	}
	else {
		/* FIXME: Patch calling code */
	}

	return addr;
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
	guint8 *buf, *tramp;
	int i, lmf_offset, offset, tramp_offset, saved_regs_offset, saved_fpregs_offset, framesize;
	int l0, l1, l2, l3, l4, l5, l6, l7, o0, o1, o2, o3;
	gint64 disp;
	gboolean has_caller;
	Ia64CodegenState code;

	if (tramp_type == MONO_TRAMPOLINE_JUMP)
		has_caller = FALSE;
	else
		has_caller = TRUE;

	buf = mono_global_codeman_reserve (2048);

	ia64_codegen_init (code, buf);

	/* FIXME: Save/restore lmf */

	/* Stacked Registers */
	l0 = 40;
	l1 = 41;
	l2 = 42;
	l3 = 43;
	l4 = 44;
	l5 = 45; /* saved ar.pfs */
	l6 = 46; /* arg */
	l7 = 47; /* code */
	o0 = 48; /* regs */
	o1 = 49; /* code */
	o2 = 50; /* arg */
	o3 = 51; /* tramp */

	framesize = (128 * 8) + 1024;
	framesize = (framesize + (MONO_ARCH_FRAME_ALIGNMENT - 1)) & ~ (MONO_ARCH_FRAME_ALIGNMENT - 1);

	/*
	 * Allocate a new register+memory stack frame.
	 * 8 input registers (the max used by the ABI)
	 * 8 locals
	 * 4 output (number of parameters passed to trampoline)
	 */
	ia64_alloc (code, l5, 8, 8, 4, 0);
	ia64_adds_imm (code, IA64_SP, (-framesize), IA64_SP);

	offset = 16; /* scratch area */

	/* Save the argument received from the specific trampoline */
	ia64_mov (code, l6, GP_SCRATCH_REG);

	/* Save the calling address */
	ia64_mov_from_br (code, l7, IA64_B0);

	/* Save registers */
	saved_regs_offset = offset;
	offset += 128 * 8;
	/* 
	 * Only the registers which are needed for computing vtable slots need
	 * to be saved.
	 */
	for (i = 0; i < 64; ++i)
		if ((1 << i) & MONO_ARCH_CALLEE_REGS) {
			ia64_adds_imm (code, l1, saved_regs_offset + (i * 8), IA64_SP);
			ia64_st8_hint (code, l1, i, 0);
		}
	saved_fpregs_offset = offset;
	offset += 8 * 8;
	for (i = 0; i < 8; ++i) {
		ia64_adds_imm (code, l1, saved_fpregs_offset + (i * 8), IA64_SP);
		ia64_stfd_hint (code, l1, i + 8, 0);
	}

	g_assert (offset < framesize);

	/* Arg1 is the pointer to the saved registers */
	ia64_adds_imm (code, o0, saved_regs_offset, IA64_SP);

	/* Arg2 is the address of the calling code */
	if (has_caller)
		ia64_mov (code, o1, l7);
	else
		ia64_mov (code, o1, 0);

	/* Arg3 is the method/vtable ptr */
	ia64_mov (code, o2, l6);

	/* Arg4 is the trampoline address */
	/* FIXME: */
	ia64_mov (code, o3, 0);

	if (tramp_type == MONO_TRAMPOLINE_CLASS_INIT)
		tramp = (guint8*)ia64_class_init_trampoline;
	else if (tramp_type == MONO_TRAMPOLINE_AOT)
		tramp = (guint8*)ia64_aot_trampoline;
	else
		tramp = (guint8*)ia64_magic_trampoline;

	/* Call the trampoline using an indirect call */
	ia64_movl (code, l0, tramp);
	ia64_ld8_inc_imm_hint (code, l1, l0, 8, 0);
	ia64_mov_to_br (code, IA64_B6, l1, 0, 0, 0);
	ia64_ld8 (code, IA64_GP, l0);
	ia64_br_call_reg (code, 0, IA64_B6);

	/* Restore fp regs */
	for (i = 0; i < 8; ++i) {
		ia64_adds_imm (code, l1, saved_fpregs_offset + (i * 8), IA64_SP);
		ia64_ldfd_hint (code, i + 8, l1, 0);
	}

	/* FIXME: Handle NATs in fp regs / scratch regs */

	/* Load method address from function descriptor */
	ia64_ld8 (code, l0, IA64_R8);
	ia64_mov_to_br (code, IA64_B6, l0, 0, 0, 0);

	/* Clean up register/memory stack frame */
	ia64_adds_imm (code, IA64_SP, framesize, IA64_SP);
	ia64_mov_to_ar_i (code, IA64_PFS, l5);

	if (tramp_type == MONO_TRAMPOLINE_CLASS_INIT) {
		ia64_mov_ret_to_br (code, IA64_B0, l7, 0, 0, 0);
		ia64_br_ret_reg_hint (code, IA64_B0, 0, 0, 0);
	}
	else {
		/* Call the compiled method */
		ia64_mov_to_br (code, IA64_B0, l7, 0, 0, 0);
		ia64_br_cond_reg_hint (code, IA64_B6, 0, 0, 0);
	}

	ia64_codegen_close (code);

	g_assert ((code.buf - buf) <= 2048);

	mono_arch_flush_icache (buf, code.buf - buf);

	return buf;
}

#define TRAMPOLINE_SIZE 128

static MonoJitInfo*
create_specific_trampoline (gpointer arg1, MonoTrampolineType tramp_type, MonoDomain *domain)
{
	MonoJitInfo *ji;
	guint8 *buf, *tramp;
	gint64 disp;
	Ia64CodegenState code;

	tramp = mono_get_trampoline_code (tramp_type);

	mono_domain_lock (domain);
	buf = mono_code_manager_reserve (domain->code_mp, TRAMPOLINE_SIZE);
	mono_domain_unlock (domain);

	/* FIXME: Optimize this */

	ia64_codegen_init (code, buf);

	ia64_movl (code, GP_SCRATCH_REG, arg1);

	ia64_begin_bundle (code);
	disp = (tramp - code.buf) >> 4;
	ia64_br_cond_hint (code, disp, 0, 0, 0);

	ia64_codegen_close (code);

	mono_arch_flush_icache (buf, code.buf - buf);

	ji = g_new0 (MonoJitInfo, 1);
	ji->code_start = buf;
	ji->code_size = code.buf - buf;

	return ji;
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
