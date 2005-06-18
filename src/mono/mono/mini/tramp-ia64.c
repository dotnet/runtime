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
#define GP_SCRATCH_REG2 30

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
	guint8 *buf;
	gpointer func_addr, func_gp;
	Ia64CodegenState code;
	int this_reg = 0;
	MonoDomain *domain = mono_domain_get ();

	/* FIXME: Optimize this */

	if (!mono_method_signature (m)->ret->byref && MONO_TYPE_ISSTRUCT (mono_method_signature (m)->ret))
		this_reg = 1;

	func_addr = ((gpointer*)addr) [0];
	func_gp = ((gpointer*)addr) [1];

	mono_domain_lock (domain);
	buf = mono_code_manager_reserve (domain->code_mp, 256);
	mono_domain_unlock (domain);

	/* Since the this reg is a stacked register, its a bit hard to access it */
	ia64_codegen_init (code, buf);
	ia64_alloc (code, 40, 8, 1, 0, 0);
	ia64_adds_imm (code, 32 + this_reg, sizeof (MonoObject), 32 + this_reg);
	ia64_mov_to_ar_i (code, IA64_PFS, 40);	
	ia64_movl (code, GP_SCRATCH_REG, func_addr);
	ia64_mov_to_br (code, IA64_B6, GP_SCRATCH_REG);
	ia64_br_cond_reg (code, IA64_B6);
	ia64_codegen_close (code);

	g_assert (code.buf - buf < 256);

	/* FIXME: */

	gpointer *desc = g_malloc0 (sizeof (gpointer) * 2);
	desc [0] = buf;
	desc [1] = func_gp;

	return desc;
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
	mono_runtime_class_init (vtable);
	
	/* FIXME: Patch calling code */
}

guchar*
mono_arch_create_trampoline_code (MonoTrampolineType tramp_type)
{
	guint8 *buf, *tramp;
	int i, offset, saved_regs_offset, saved_fpregs_offset, framesize;
	int in0, local0, out0, l0, l1, l2, l3, l4, l5, l6, l7, l8, o0, o1, o2, o3;
	gboolean has_caller;
	Ia64CodegenState code;
	unw_dyn_info_t *di;
	unw_dyn_region_info_t *r_pro;

	if (tramp_type == MONO_TRAMPOLINE_JUMP)
		has_caller = FALSE;
	else
		has_caller = TRUE;

	buf = mono_global_codeman_reserve (2048);

	ia64_codegen_init (code, buf);

	/* FIXME: Save/restore lmf */

	/* Stacked Registers */
	in0 = 32;
	local0 = in0 + 8;
	out0 = local0 + 16;
	l0 = 40;
	l1 = 41;
	l2 = 42;
	l3 = 43;
	l4 = 44;
	l5 = 45; /* saved ar.pfs */
	l6 = 46; /* arg */
	l7 = 47; /* code */
	l8 = 48; /* saved sp */
	o0 = out0 + 0; /* regs */
	o1 = out0 + 1; /* code */
	o2 = out0 + 2; /* arg */
	o3 = out0 + 3; /* tramp */

	framesize = (128 * 8) + 1024;
	framesize = (framesize + (MONO_ARCH_FRAME_ALIGNMENT - 1)) & ~ (MONO_ARCH_FRAME_ALIGNMENT - 1);

	/*
	 * Allocate a new register+memory stack frame.
	 * 8 input registers (the max used by the ABI)
	 * 16 locals
	 * 4 output (number of parameters passed to trampoline)
	 */
	ia64_alloc (code, l5, local0 - in0, out0 - local0, 4, 0);
	ia64_mov (code, l8, IA64_SP);
	ia64_adds_imm (code, IA64_SP, (-framesize), IA64_SP);

	offset = 16; /* scratch area */

	/* Save the argument received from the specific trampoline */
	ia64_mov (code, l6, GP_SCRATCH_REG);

	/* Save the calling address */
	ia64_mov_from_br (code, l7, IA64_B0);

	/* Create unwind info for the prolog */
	r_pro = g_malloc0 (_U_dyn_region_info_size (3));
	r_pro->op_count = 3;
	r_pro->insn_count = 16;
	i = 0;
	_U_dyn_op_save_reg (&r_pro->op[i++], _U_QP_TRUE, /* when=*/ 2,
						/* reg=*/ UNW_IA64_AR_PFS, /* dst=*/ UNW_IA64_GR + local0 + 5);
	_U_dyn_op_save_reg (&r_pro->op[i++], _U_QP_TRUE, /* when=*/ 5,
						/* reg=*/ UNW_IA64_SP, /* dst=*/ UNW_IA64_GR + local0 + 8);
	_U_dyn_op_save_reg (&r_pro->op[i++], _U_QP_TRUE, /* when=*/ 14,
						/* reg=*/ UNW_IA64_RP, /* dst=*/ UNW_IA64_GR + local0 + 7);
	g_assert ((unsigned) i <= r_pro->op_count);	

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

	/* Save fp registers */
	saved_fpregs_offset = offset;
	offset += 8 * 8;
	ia64_adds_imm (code, l1, saved_fpregs_offset, IA64_SP);
	for (i = 0; i < 8; ++i)
		ia64_stfd_inc_imm_hint (code, l1, i + 8, 8, 0);

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
	ia64_ld8_inc_imm (code, l1, l0, 8);
	ia64_mov_to_br (code, IA64_B6, l1);
	ia64_ld8 (code, IA64_GP, l0);
	ia64_br_call_reg (code, 0, IA64_B6);

	/* Restore fp regs */
	ia64_adds_imm (code, l1, saved_fpregs_offset, IA64_SP);
	for (i = 0; i < 8; ++i)
		ia64_ldfd_inc_imm (code, i + 8, l1, 8);

	/* FIXME: Handle NATs in fp regs / scratch regs */

	if (tramp_type != MONO_TRAMPOLINE_CLASS_INIT) {
		/* Load method address from function descriptor */
		ia64_ld8 (code, l0, IA64_R8);
		ia64_mov_to_br (code, IA64_B6, l0);
	}

	/* Clean up register/memory stack frame */
	ia64_adds_imm (code, IA64_SP, framesize, IA64_SP);
	ia64_mov_to_ar_i (code, IA64_PFS, l5);

	if (tramp_type == MONO_TRAMPOLINE_CLASS_INIT) {
		ia64_mov_ret_to_br (code, IA64_B0, l7);
		ia64_br_ret_reg (code, IA64_B0);
	}
	else {
		/* Call the compiled method */
		ia64_mov_to_br (code, IA64_B0, l7);
		ia64_br_cond_reg (code, IA64_B6);
	}

	ia64_codegen_close (code);

	g_assert ((code.buf - buf) <= 2048);

	/* FIXME: emit unwind info for epilog */
	di = g_malloc0 (sizeof (unw_dyn_info_t));
	di->start_ip = (unw_word_t) buf;
	di->end_ip = (unw_word_t) code.buf;
	di->gp = 0;
	di->format = UNW_INFO_FORMAT_DYNAMIC;
	di->u.pi.name_ptr = (unw_word_t)"ia64_generic_trampoline";
	di->u.pi.regions = r_pro;

	_U_dyn_register (di);

	mono_arch_flush_icache (buf, code.buf - buf);

	return buf;
}

#define TRAMPOLINE_SIZE 128

static gpointer
create_specific_trampoline (gpointer arg1, MonoTrampolineType tramp_type, MonoDomain *domain, guint32 *code_len)
{
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
	if (ia64_is_imm21 (disp)) {
		ia64_br_cond (code, disp);
	}
	else {
		ia64_movl (code, GP_SCRATCH_REG2, tramp);
		ia64_mov_to_br (code, IA64_B6, GP_SCRATCH_REG2);
		ia64_br_cond_reg (code, IA64_B6);
	}

	ia64_codegen_close (code);

	g_assert (code.buf - buf <= TRAMPOLINE_SIZE);

	mono_arch_flush_icache (buf, code.buf - buf);

	if (code_len)
		*code_len = code.buf - buf;

	return buf;
}

MonoJitInfo*
mono_arch_create_jump_trampoline (MonoMethod *method)
{
	MonoJitInfo *ji;
	gpointer code;
	guint32 code_size;

	code = create_specific_trampoline (method, MONO_TRAMPOLINE_JUMP, mono_domain_get (), &code_size);

	ji = g_new0 (MonoJitInfo, 1);
	ji->code_start = code;
	ji->code_size = code_size;
	ji->method = method;

	return ji;
}

gpointer
mono_arch_create_jit_trampoline (MonoMethod *method)
{
	return create_specific_trampoline (method, MONO_TRAMPOLINE_GENERIC, mono_domain_get (), NULL);
}

gpointer
mono_arch_create_jit_trampoline_from_token (MonoImage *image, guint32 token)
{
	MonoDomain *domain = mono_domain_get ();
	guint8 *buf, *start;

	mono_domain_lock (domain);
	buf = start = mono_code_manager_reserve (domain->code_mp, 2 * sizeof (gpointer));
	mono_domain_unlock (domain);

	*(gpointer*)(gpointer)buf = image;
	buf += sizeof (gpointer);
	*(guint32*)(gpointer)buf = token;

	return create_specific_trampoline (start, MONO_TRAMPOLINE_AOT, domain, NULL);
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
	return create_specific_trampoline (vtable, MONO_TRAMPOLINE_CLASS_INIT, vtable->domain, NULL);
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
