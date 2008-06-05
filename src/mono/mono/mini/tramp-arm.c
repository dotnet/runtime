/*
 * tramp-arm.c: JIT trampoline code for ARM
 *
 * Authors:
 *   Paolo Molaro (lupus@ximian.com)
 *
 * (C) 2001 Ximian, Inc.
 */

#include <config.h>
#include <glib.h>

#include <mono/metadata/appdomain.h>
#include <mono/metadata/marshal.h>
#include <mono/metadata/tabledefs.h>
#include <mono/arch/arm/arm-codegen.h>

#include "mini.h"
#include "mini-arm.h"

/*
 * Return the instruction to jump from code to target, 0 if not
 * reachable with a single instruction
 */
static guint32
branch_for_target_reachable (guint8 *branch, guint8 *target)
{
	gint diff = target - branch - 8;
	g_assert ((diff & 3) == 0);
	if (diff >= 0) {
		if (diff <= 33554431)
			return (ARMCOND_AL << ARMCOND_SHIFT) | (ARM_BR_TAG) | (diff >> 2);
	} else {
		/* diff between 0 and -33554432 */
		if (diff >= -33554432)
			return (ARMCOND_AL << ARMCOND_SHIFT) | (ARM_BR_TAG) | ((diff >> 2) & ~0xff000000);
	}
	return 0;
}

/*
 * mono_arch_get_unbox_trampoline:
 * @m: method pointer
 * @addr: pointer to native code for @m
 *
 * when value type methods are called through the vtable we need to unbox the
 * this argument. This method returns a pointer to a trampoline which does
 * unboxing before calling the method
 */
gpointer
mono_arch_get_unbox_trampoline (MonoMethod *m, gpointer addr)
{
	guint8 *code, *start;
	int this_pos = 0;
	MonoDomain *domain = mono_domain_get ();

	if (MONO_TYPE_ISSTRUCT (mono_method_signature (m)->ret))
		this_pos = 1;

	mono_domain_lock (domain);
	start = code = mono_code_manager_reserve (domain->code_mp, 16);
	mono_domain_unlock (domain);

	ARM_LDR_IMM (code, ARMREG_IP, ARMREG_PC, 4);
	ARM_ADD_REG_IMM8 (code, this_pos, this_pos, sizeof (MonoObject));
	ARM_MOV_REG_REG (code, ARMREG_PC, ARMREG_IP);
	*(guint32*)code = (guint32)addr;
	code += 4;
	mono_arch_flush_icache (start, code - start);
	g_assert ((code - start) <= 16);
	/*g_print ("unbox trampoline at %d for %s:%s\n", this_pos, m->klass->name, m->name);
	g_print ("unbox code is at %p for method at %p\n", start, addr);*/

	return start;
}

void
mono_arch_patch_callsite (guint8 *method_start, guint8 *code_ptr, guint8 *addr)
{
	guint32 *code = (guint32*)code_ptr;

	/* This is the 'bl' or the 'mov pc' instruction */
	--code;
	
	/*
	 * Note that methods are called also with the bl opcode.
	 */
	if ((((*code) >> 25)  & 7) == 5) {
		/*g_print ("direct patching\n");*/
		arm_patch ((guint8*)code, addr);
		mono_arch_flush_icache ((guint8*)code, 4);
		return;
	}

	if ((((*code) >> 20) & 0xFF) == 0x12) {
		/*g_print ("patching bx\n");*/
		arm_patch ((guint8*)code, addr);
		mono_arch_flush_icache ((guint8*)(code - 2), 4);
		return;
	}

	g_assert_not_reached ();
}

void
mono_arch_patch_plt_entry (guint8 *code, guint8 *addr)
{
	guint32 ins = branch_for_target_reachable (code, addr);

	if (ins)
		/* Patch the branch */
		((guint32*)code) [0] = ins;
	else
		/* Patch the jump address */
		((guint32*)code) [1] = (guint32)addr;
	mono_arch_flush_icache ((guint8*)code, 4);
}

void
mono_arch_nullify_class_init_trampoline (guint8 *code, gssize *regs)
{
	return;
}

void
mono_arch_nullify_plt_entry (guint8 *code)
{
	guint8 buf [4];
	guint8 *p;

	p = buf;
	ARM_MOV_REG_REG (p, ARMREG_PC, ARMREG_LR);

	((guint32*)code) [0] = ((guint32*)buf) [0];
	mono_arch_flush_icache ((guint8*)code, 4);
}


/* Stack size for trampoline function 
 */
#define STACK (sizeof (MonoLMF))

/* Method-specific trampoline code fragment size */
#define METHOD_TRAMPOLINE_SIZE 64

/* Jump-specific trampoline code fragment size */
#define JUMP_TRAMPOLINE_SIZE   64

#define GEN_TRAMP_SIZE 192

/*
 * Stack frame description when the generic trampoline is called.
 * caller frame
 * ------------------- old sp
 *  MonoLMF
 * ------------------- sp
 */
guchar*
mono_arch_create_trampoline_code (MonoTrampolineType tramp_type)
{
	guint8 *buf, *code = NULL;
	guint8 *load_get_lmf_addr, *load_trampoline;
	gpointer *constants;

	/* Now we'll create in 'buf' the ARM trampoline code. This
	 is the trampoline code common to all methods  */
	
	code = buf = mono_global_codeman_reserve (GEN_TRAMP_SIZE);

	/*
	 * At this point lr points to the specific arg and sp points to the saved
	 * regs on the stack (all but PC and SP). The original LR value has been
	 * saved as sp + LR_OFFSET by the push in the specific trampoline
	 */
#define LR_OFFSET (sizeof (gpointer) * 13)
	ARM_MOV_REG_REG (buf, ARMREG_V1, ARMREG_SP);
	ARM_LDR_IMM (buf, ARMREG_V2, ARMREG_LR, 0);
	ARM_LDR_IMM (buf, ARMREG_V3, ARMREG_SP, LR_OFFSET);

	/* ok, now we can continue with the MonoLMF setup, mostly untouched 
	 * from emit_prolog in mini-arm.c
	 * This is a sinthetized call to mono_get_lmf_addr ()
	 */
	load_get_lmf_addr = buf;
	buf += 4;
	ARM_MOV_REG_REG (buf, ARMREG_LR, ARMREG_PC);
	ARM_MOV_REG_REG (buf, ARMREG_PC, ARMREG_R0);

	/* we build the MonoLMF structure on the stack - see mini-arm.h
	 * The pointer to the struct is put in r1.
	 * the iregs array is already allocated on the stack by push.
	 */
	ARM_SUB_REG_IMM8 (buf, ARMREG_SP, ARMREG_SP, sizeof (MonoLMF) - sizeof (guint) * 14);
	ARM_ADD_REG_IMM8 (buf, ARMREG_R1, ARMREG_SP, STACK - sizeof (MonoLMF));
	/* r0 is the result from mono_get_lmf_addr () */
	ARM_STR_IMM (buf, ARMREG_R0, ARMREG_R1, G_STRUCT_OFFSET (MonoLMF, lmf_addr));
	/* new_lmf->previous_lmf = *lmf_addr */
	ARM_LDR_IMM (buf, ARMREG_R2, ARMREG_R0, G_STRUCT_OFFSET (MonoLMF, previous_lmf));
	ARM_STR_IMM (buf, ARMREG_R2, ARMREG_R1, G_STRUCT_OFFSET (MonoLMF, previous_lmf));
	/* *(lmf_addr) = r1 */
	ARM_STR_IMM (buf, ARMREG_R1, ARMREG_R0, G_STRUCT_OFFSET (MonoLMF, previous_lmf));
	/* save method info (it's in v2) */
	if ((tramp_type == MONO_TRAMPOLINE_GENERIC) || (tramp_type == MONO_TRAMPOLINE_JUMP))
		ARM_STR_IMM (buf, ARMREG_V2, ARMREG_R1, G_STRUCT_OFFSET (MonoLMF, method));
	ARM_STR_IMM (buf, ARMREG_SP, ARMREG_R1, G_STRUCT_OFFSET (MonoLMF, ebp));
	/* save the IP (caller ip) */
	if (tramp_type == MONO_TRAMPOLINE_JUMP) {
		ARM_MOV_REG_IMM8 (buf, ARMREG_R2, 0);
	} else {
		/* assumes STACK == sizeof (MonoLMF) */
		ARM_LDR_IMM (buf, ARMREG_R2, ARMREG_SP, (G_STRUCT_OFFSET (MonoLMF, iregs) + 13*4));
	}
	ARM_STR_IMM (buf, ARMREG_R2, ARMREG_R1, G_STRUCT_OFFSET (MonoLMF, eip));

	/*
	 * Now we're ready to call xxx_trampoline ().
	 */
	/* Arg 1: the saved registers. It was put in v1 */
	ARM_MOV_REG_REG (buf, ARMREG_R0, ARMREG_V1);

	/* Arg 2: code (next address to the instruction that called us) */
	if (tramp_type == MONO_TRAMPOLINE_JUMP) {
		ARM_MOV_REG_IMM8 (buf, ARMREG_R1, 0);
	} else {
		ARM_MOV_REG_REG (buf, ARMREG_R1, ARMREG_V3);
	}
	
	/* Arg 3: the specific argument, stored in v2
	 */
	ARM_MOV_REG_REG (buf, ARMREG_R2, ARMREG_V2);

	load_trampoline = buf;
	buf += 4;

	ARM_MOV_REG_REG (buf, ARMREG_LR, ARMREG_PC);
	ARM_MOV_REG_REG (buf, ARMREG_PC, ARMREG_IP);
	
	/* OK, code address is now on r0. Move it to the place on the stack
	 * where IP was saved (it is now no more useful to us and it can be
	 * clobbered). This way we can just restore all the regs in one inst
	 * and branch to IP.
	 */
	ARM_STR_IMM (buf, ARMREG_R0, ARMREG_V1, (ARMREG_R12 * 4));

	/* Check for thread interruption */
	/* This is not perf critical code so no need to check the interrupt flag */
	/* 
	 * Have to call the _force_ variant, since there could be a protected wrapper on the top of the stack.
	 */
	ARM_LDR_IMM (buf, ARMREG_IP, ARMREG_PC, 0);
	ARM_B (buf, 0);
	*(gpointer*)buf = mono_thread_force_interruption_checkpoint;
	buf += 4;
	ARM_MOV_REG_REG (buf, ARMREG_LR, ARMREG_PC);
	ARM_MOV_REG_REG (buf, ARMREG_PC, ARMREG_IP);

	/*
	 * Now we restore the MonoLMF (see emit_epilogue in mini-arm.c)
	 * and the rest of the registers, so the method called will see
	 * the same state as before we executed.
	 * The pointer to MonoLMF is in r2.
	 */
	ARM_MOV_REG_REG (buf, ARMREG_R2, ARMREG_SP);
	/* ip = previous_lmf */
	ARM_LDR_IMM (buf, ARMREG_IP, ARMREG_R2, G_STRUCT_OFFSET (MonoLMF, previous_lmf));
	/* lr = lmf_addr */
	ARM_LDR_IMM (buf, ARMREG_LR, ARMREG_R2, G_STRUCT_OFFSET (MonoLMF, lmf_addr));
	/* *(lmf_addr) = previous_lmf */
	ARM_STR_IMM (buf, ARMREG_IP, ARMREG_LR, G_STRUCT_OFFSET (MonoLMF, previous_lmf));

	/* Non-standard function epilogue. Instead of doing a proper
	 * return, we just jump to the compiled code.
	 */
	/* Restore the registers and jump to the code:
	 * Note that IP has been conveniently set to the method addr.
	 */
	ARM_ADD_REG_IMM8 (buf, ARMREG_SP, ARMREG_SP, sizeof (MonoLMF) - sizeof (guint) * 14);
	ARM_POP_NWB (buf, 0x5fff);
	/* do we need to set sp? */
	ARM_ADD_REG_IMM8 (buf, ARMREG_SP, ARMREG_SP, (14 * 4));
	if (tramp_type == MONO_TRAMPOLINE_CLASS_INIT)
		ARM_MOV_REG_REG (buf, ARMREG_PC, ARMREG_LR);
	else
		ARM_MOV_REG_REG (buf, ARMREG_PC, ARMREG_IP);

	constants = (gpointer*)buf;
	constants [0] = mono_get_lmf_addr;
	constants [1] = (gpointer)mono_get_trampoline_func (tramp_type);

	/* backpatch by emitting the missing instructions skipped above */
	ARM_LDR_IMM (load_get_lmf_addr, ARMREG_R0, ARMREG_PC, (buf - load_get_lmf_addr - 8));
	ARM_LDR_IMM (load_trampoline, ARMREG_IP, ARMREG_PC, (buf + 4 - load_trampoline - 8));

	buf += 8;

	/* Flush instruction cache, since we've generated code */
	mono_arch_flush_icache (code, buf - code);

	/* Sanity check */
	g_assert ((buf - code) <= GEN_TRAMP_SIZE);

	return code;
}

#define SPEC_TRAMP_SIZE 24

gpointer
mono_arch_create_specific_trampoline (gpointer arg1, MonoTrampolineType tramp_type, MonoDomain *domain, guint32 *code_len)
{
	guint8 *code, *buf, *tramp;
	gpointer *constants;
	guint32 short_branch, size = SPEC_TRAMP_SIZE;

	tramp = mono_get_trampoline_code (tramp_type);

	mono_domain_lock (domain);
	code = buf = mono_code_manager_reserve_align (domain->code_mp, size, 4);
	if ((short_branch = branch_for_target_reachable (code + 8, tramp))) {
		size = 12;
		mono_code_manager_commit (domain->code_mp, code, SPEC_TRAMP_SIZE, size);
	}
	mono_domain_unlock (domain);

	/* we could reduce this to 12 bytes if tramp is within reach:
	 * ARM_PUSH ()
	 * ARM_BL ()
	 * method-literal
	 * The called code can access method using the lr register
	 * A 20 byte sequence could be:
	 * ARM_PUSH ()
	 * ARM_MOV_REG_REG (lr, pc)
	 * ARM_LDR_IMM (pc, pc, 0)
	 * method-literal
	 * tramp-literal
	 */
	/* We save all the registers, except PC and SP */
	ARM_PUSH (buf, 0x5fff);
	if (short_branch) {
		constants = (gpointer*)buf;
		constants [0] = GUINT_TO_POINTER (short_branch | (1 << 24));
		constants [1] = arg1;
		buf += 8;
	} else {
		ARM_LDR_IMM (buf, ARMREG_R1, ARMREG_PC, 8); /* temp reg */
		ARM_MOV_REG_REG (buf, ARMREG_LR, ARMREG_PC);
		ARM_MOV_REG_REG (buf, ARMREG_PC, ARMREG_R1);

		constants = (gpointer*)buf;
		constants [0] = arg1;
		constants [1] = tramp;
		buf += 8;
	}

	/* Flush instruction cache, since we've generated code */
	mono_arch_flush_icache (code, buf - code);

	g_assert ((buf - code) <= size);

	if (code_len)
		*code_len = buf - code;

	return code;
}

gpointer
mono_arch_create_rgctx_lazy_fetch_trampoline (guint32 encoded_offset)
{
	/* FIXME: implement! */
	g_assert_not_reached ();
	return NULL;
}

guint32
mono_arch_get_rgctx_lazy_fetch_offset (gpointer *regs)
{
	/* FIXME: implement! */
	g_assert_not_reached ();
	return 0;
}
