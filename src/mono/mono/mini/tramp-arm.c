/**
 * \file
 * JIT trampoline code for ARM
 *
 * Authors:
 *   Paolo Molaro (lupus@ximian.com)
 *
 * (C) 2001-2003 Ximian, Inc.
 * Copyright 2003-2011 Novell Inc
 * Copyright 2011 Xamarin Inc
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */

#include <config.h>
#include <glib.h>

#include <mono/metadata/abi-details.h>
#include <mono/metadata/marshal.h>
#include <mono/metadata/tabledefs.h>
#include <mono/metadata/profiler-private.h>
#include <mono/arch/arm/arm-codegen.h>
#include <mono/arch/arm/arm-vfp-codegen.h>

#include "mini.h"
#include "mini-arm.h"
#include "mini-runtime.h"
#include "debugger-agent.h"
#include "jit-icalls.h"

#ifndef DISABLE_INTERPRETER
#include "interp/interp.h"
#endif
#include "mono/utils/mono-tls-inline.h"

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
mono_arch_patch_plt_entry (guint8 *code, gpointer *got, host_mgreg_t *regs, guint8 *addr)
{
	guint8 *jump_entry;

	/* Patch the jump table entry used by the plt entry */
	if (*(guint32*)code == 0xe59fc000) {
		/* ARM_LDR_IMM (code, ARMREG_IP, ARMREG_PC, 0); */
		guint32 offset = ((guint32*)code)[2];
		
		jump_entry = code + offset + 12;
	} else if (*(guint16*)(code - 4) == 0xf8df) {
		/* 
		 * Thumb PLT entry, begins with ldr.w ip, [pc, #8], code points to entry + 4, see
		 * mono_arm_get_thumb_plt_entry ().
		 */
		guint32 offset;

		code -= 4;
		offset = *(guint32*)(code + 12);
		jump_entry = code + offset + 8;
	} else {
		g_assert_not_reached ();
	}

	*(guint8**)jump_entry = addr;
}

#ifndef DISABLE_JIT

#define arm_is_imm12(v) ((int)(v) > -4096 && (int)(v) < 4096)

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

static guint8*
emit_bx (guint8* code, int reg)
{
	if (mono_arm_thumb_supported ())
		ARM_BX (code, reg);
	else
		ARM_MOV_REG_REG (code, ARMREG_PC, reg);
	return code;
}

/* Stack size for trampoline function
 */
#define STACK ALIGN_TO (MONO_ABI_SIZEOF (MonoLMF), MONO_ARCH_FRAME_ALIGNMENT)

/* Method-specific trampoline code fragment size */
#define METHOD_TRAMPOLINE_SIZE 64

/* Jump-specific trampoline code fragment size */
#define JUMP_TRAMPOLINE_SIZE   64

guchar*
mono_arch_create_generic_trampoline (MonoTrampolineType tramp_type, MonoTrampInfo **info, gboolean aot)
{
	const char *tramp_name;
	guint8 *buf, *code = NULL;
	guint8 *load_get_lmf_addr  = NULL, *load_trampoline  = NULL;
	guint8 *labels [16];
	gpointer *constants;
	int i, orig_cfa_offset, cfa_offset, regsave_size, lr_offset;
	GSList *unwind_ops = NULL;
	MonoJumpInfo *ji = NULL;
	int buf_len;

	/* Now we'll create in 'buf' the ARM trampoline code. This
	 is the trampoline code common to all methods  */

	buf_len = 272;

	/* Add space for saving/restoring VFP regs. */
	if (mono_arm_is_hard_float ())
		buf_len += 8 * 2;

	code = buf = mono_global_codeman_reserve (buf_len);

	/*
	 * At this point lr points to the specific arg and sp points to the saved
	 * regs on the stack (all but PC and SP). The original LR value has been
	 * saved as sp + LR_OFFSET by the push in the specific trampoline
	 */

	/* The size of the area already allocated by the push in the specific trampoline */
	regsave_size = 14 * sizeof (target_mgreg_t);
	/* The offset where lr was saved inside the regsave area */
	lr_offset = 13 * sizeof (target_mgreg_t);

	// CFA = SP + (num registers pushed) * 4
	cfa_offset = 14 * sizeof (target_mgreg_t);
	mono_add_unwind_op_def_cfa (unwind_ops, code, buf, ARMREG_SP, cfa_offset);
	// PC saved at sp+LR_OFFSET
	mono_add_unwind_op_offset (unwind_ops, code, buf, ARMREG_LR, -4);
	/* Callee saved regs */
	for (i = 0; i < 8; ++i)
		mono_add_unwind_op_offset (unwind_ops, code, buf, ARMREG_R4 + i, -regsave_size + ((4 + i) * 4));

	if (aot) {
		/* 
		 * For page trampolines the data is in r1, so just move it, otherwise use the got slot as below.
		 * The trampoline contains a pc-relative offset to the got slot 
		 * preceeding the got slot where the value is stored. The offset can be
		 * found at [lr + 0].
		 */
		/* See if emit_trampolines () in aot-compiler.c for the '2' */
		if (aot == 2) {
			ARM_MOV_REG_REG (code, ARMREG_V2, ARMREG_R1);
		} else {
			ARM_LDR_IMM (code, ARMREG_V2, ARMREG_LR, 0);
			ARM_ADD_REG_IMM (code, ARMREG_V2, ARMREG_V2, 4, 0);
			ARM_LDR_REG_REG (code, ARMREG_V2, ARMREG_V2, ARMREG_LR);
		}
	} else {
		ARM_LDR_IMM (code, ARMREG_V2, ARMREG_LR, 0);
	}
	ARM_LDR_IMM (code, ARMREG_V3, ARMREG_SP, lr_offset);

	/* we build the MonoLMF structure on the stack - see mini-arm.h
	 * The pointer to the struct is put in r1.
	 * the iregs array is already allocated on the stack by push.
	 */
	code = mono_arm_emit_load_imm (code, ARMREG_R2, STACK - regsave_size);
	ARM_SUB_REG_REG (code, ARMREG_SP, ARMREG_SP, ARMREG_R2);
	cfa_offset += STACK - regsave_size;
	mono_add_unwind_op_def_cfa_offset (unwind_ops, code, buf, cfa_offset);
	/* V1 == lmf */
	code = mono_arm_emit_load_imm (code, ARMREG_R2, STACK - MONO_ABI_SIZEOF (MonoLMF));
	ARM_ADD_REG_REG (code, ARMREG_V1, ARMREG_SP, ARMREG_R2);

	/* ok, now we can continue with the MonoLMF setup, mostly untouched 
	 * from emit_prolog in mini-arm.c
	 * This is a synthetized call to mono_get_lmf_addr ()
	 */
	if (aot) {
		ji = mono_patch_info_list_prepend (ji, code - buf, MONO_PATCH_INFO_JIT_ICALL_ADDR, GUINT_TO_POINTER (MONO_JIT_ICALL_mono_get_lmf_addr));
		ARM_LDR_IMM (code, ARMREG_R0, ARMREG_PC, 0);
		ARM_B (code, 0);
		*(gpointer*)code = NULL;
		code += 4;
		ARM_LDR_REG_REG (code, ARMREG_R0, ARMREG_PC, ARMREG_R0);
	} else {
		load_get_lmf_addr = code;
		code += 4;
	}
	ARM_MOV_REG_REG (code, ARMREG_LR, ARMREG_PC);
	code = emit_bx (code, ARMREG_R0);

	/*
	 * The stack now looks like:
	 *       <saved regs>
	 * v1 -> <rest of LMF>
	 * sp -> <alignment>
	 */

	/* r0 is the result from mono_get_lmf_addr () */
	ARM_STR_IMM (code, ARMREG_R0, ARMREG_V1, MONO_STRUCT_OFFSET (MonoLMF, lmf_addr));
	/* new_lmf->previous_lmf = *lmf_addr */
	ARM_LDR_IMM (code, ARMREG_R2, ARMREG_R0, MONO_STRUCT_OFFSET (MonoLMF, previous_lmf));
	ARM_STR_IMM (code, ARMREG_R2, ARMREG_V1, MONO_STRUCT_OFFSET (MonoLMF, previous_lmf));
	/* *(lmf_addr) = r1 */
	ARM_STR_IMM (code, ARMREG_V1, ARMREG_R0, MONO_STRUCT_OFFSET (MonoLMF, previous_lmf));
	/* save method info (it's in v2) */
	if ((tramp_type == MONO_TRAMPOLINE_JIT) || (tramp_type == MONO_TRAMPOLINE_JUMP))
		ARM_STR_IMM (code, ARMREG_V2, ARMREG_V1, MONO_STRUCT_OFFSET (MonoLMF, method));
	else {
		ARM_MOV_REG_IMM8 (code, ARMREG_R2, 0);
		ARM_STR_IMM (code, ARMREG_R2, ARMREG_V1, MONO_STRUCT_OFFSET (MonoLMF, method));
	}
	/* save caller SP */
	code = mono_arm_emit_load_imm (code, ARMREG_R2, cfa_offset);
	ARM_ADD_REG_REG (code, ARMREG_R2, ARMREG_SP, ARMREG_R2);
	ARM_STR_IMM (code, ARMREG_R2, ARMREG_V1, MONO_STRUCT_OFFSET (MonoLMF, sp));
	/* save caller FP */
	ARM_LDR_IMM (code, ARMREG_R2, ARMREG_V1, (MONO_STRUCT_OFFSET (MonoLMF, iregs) + ARMREG_FP*4));
	ARM_STR_IMM (code, ARMREG_R2, ARMREG_V1, MONO_STRUCT_OFFSET (MonoLMF, fp));
	/* save the IP (caller ip) */
	if (tramp_type == MONO_TRAMPOLINE_JUMP) {
		ARM_MOV_REG_IMM8 (code, ARMREG_R2, 0);
	} else {
		ARM_LDR_IMM (code, ARMREG_R2, ARMREG_V1, (MONO_STRUCT_OFFSET (MonoLMF, iregs) + 13*4));
	}
	ARM_STR_IMM (code, ARMREG_R2, ARMREG_V1, MONO_STRUCT_OFFSET (MonoLMF, ip));

	/* Save VFP registers. */
	if (mono_arm_is_hard_float ()) {
		/*
		 * Strictly speaking, we don't have to save d0-d7 in the LMF, but
		 * it's easier than attempting to store them on the stack since
		 * this trampoline code is pretty messy.
		 */
		ARM_ADD_REG_IMM8 (code, ARMREG_R0, ARMREG_V1, MONO_STRUCT_OFFSET (MonoLMF, fregs));
		ARM_FSTMD (code, ARM_VFP_D0, 8, ARMREG_R0);
	}

	/*
	 * Now we're ready to call xxx_trampoline ().
	 */
	/* Arg 1: the saved registers */
	ARM_ADD_REG_IMM (code, ARMREG_R0, ARMREG_V1, MONO_STRUCT_OFFSET (MonoLMF, iregs), 0);

	/* Arg 2: code (next address to the instruction that called us) */
	if (tramp_type == MONO_TRAMPOLINE_JUMP) {
		ARM_MOV_REG_IMM8 (code, ARMREG_R1, 0);
	} else {
		ARM_MOV_REG_REG (code, ARMREG_R1, ARMREG_V3);
	}
	
	/* Arg 3: the specific argument, stored in v2
	 */
	ARM_MOV_REG_REG (code, ARMREG_R2, ARMREG_V2);

	if (aot) {
		ji = mono_patch_info_list_prepend (ji, code - buf, MONO_PATCH_INFO_JIT_ICALL_ADDR, GINT_TO_POINTER (mono_trampoline_type_to_jit_icall_id (tramp_type)));
		ARM_LDR_IMM (code, ARMREG_IP, ARMREG_PC, 0);
		ARM_B (code, 0);
		*(gpointer*)code = NULL;
		code += 4;
		ARM_LDR_REG_REG (code, ARMREG_IP, ARMREG_PC, ARMREG_IP);
	} else {
		load_trampoline = code;
		code += 4;
	}

	ARM_MOV_REG_REG (code, ARMREG_LR, ARMREG_PC);
	code = emit_bx (code, ARMREG_IP);

	/* OK, code address is now on r0. Move it to the place on the stack
	 * where IP was saved (it is now no more useful to us and it can be
	 * clobbered). This way we can just restore all the regs in one inst
	 * and branch to IP.
	 */
	ARM_STR_IMM (code, ARMREG_R0, ARMREG_V1, MONO_STRUCT_OFFSET (MonoLMF, iregs) + (ARMREG_R12 * sizeof (target_mgreg_t)));

	/*
	 * Now we restore the MonoLMF (see emit_epilogue in mini-arm.c)
	 * and the rest of the registers, so the method called will see
	 * the same state as before we executed.
	 */
	/* ip = previous_lmf */
	ARM_LDR_IMM (code, ARMREG_IP, ARMREG_V1, MONO_STRUCT_OFFSET (MonoLMF, previous_lmf));
	/* lr = lmf_addr */
	ARM_LDR_IMM (code, ARMREG_LR, ARMREG_V1, MONO_STRUCT_OFFSET (MonoLMF, lmf_addr));
	/* *(lmf_addr) = previous_lmf */
	ARM_STR_IMM (code, ARMREG_IP, ARMREG_LR, MONO_STRUCT_OFFSET (MonoLMF, previous_lmf));

	/* Check for thread interruption */
	/* This is not perf critical code so no need to check the interrupt flag */
	if (aot) {
		code = mono_arm_emit_aotconst (&ji, code, buf, ARMREG_IP, MONO_PATCH_INFO_JIT_ICALL_ADDR, GUINT_TO_POINTER (MONO_JIT_ICALL_mono_thread_force_interruption_checkpoint_noraise));
	} else {
		ARM_LDR_IMM (code, ARMREG_IP, ARMREG_PC, 0);
		ARM_B (code, 0);
		*(gpointer*)code = (gpointer)mono_thread_force_interruption_checkpoint_noraise;
		code += 4;
	}
	ARM_MOV_REG_REG (code, ARMREG_LR, ARMREG_PC);
	code = emit_bx (code, ARMREG_IP);

	/* Check whenever an exception needs to be thrown */
	ARM_CMP_REG_IMM (code, ARMREG_R0, 0, 0);
	labels [0] = code;
	ARM_B_COND (code, ARMCOND_NE, 0);

	orig_cfa_offset = cfa_offset;

	/* Normal case */

	/* Restore VFP registers. */
	if (mono_arm_is_hard_float ()) {
		ARM_ADD_REG_IMM8 (code, ARMREG_R0, ARMREG_V1, MONO_STRUCT_OFFSET (MonoLMF, fregs));
		ARM_FLDMD (code, ARM_VFP_D0, 8, ARMREG_R0);
	}

	/* Non-standard function epilogue. Instead of doing a proper
	 * return, we just jump to the compiled code.
	 */
	/* Restore the registers and jump to the code:
	 * Note that IP has been conveniently set to the method addr.
	 */
	ARM_ADD_REG_IMM8 (code, ARMREG_SP, ARMREG_SP, STACK - regsave_size);
	cfa_offset -= STACK - regsave_size;
	mono_add_unwind_op_def_cfa_offset (unwind_ops, code, buf, cfa_offset);
	ARM_POP_NWB (code, 0x5fff);
	mono_add_unwind_op_same_value (unwind_ops, code, buf, ARMREG_LR);
	if (tramp_type == MONO_TRAMPOLINE_RGCTX_LAZY_FETCH)
		ARM_MOV_REG_REG (code, ARMREG_R0, ARMREG_IP);
	ARM_ADD_REG_IMM8 (code, ARMREG_SP, ARMREG_SP, regsave_size);
	cfa_offset -= regsave_size;
	g_assert (cfa_offset == 0);
	mono_add_unwind_op_def_cfa_offset (unwind_ops, code, buf, cfa_offset);
	if (MONO_TRAMPOLINE_TYPE_MUST_RETURN (tramp_type))
		code = emit_bx (code, ARMREG_LR);
	else
		code = emit_bx (code, ARMREG_IP);

	if (!aot) {
		constants = (gpointer*)code;
		constants [0] = (gpointer)mono_get_lmf_addr;
		constants [1] = (gpointer)mono_get_trampoline_func (tramp_type);

		/* backpatch by emitting the missing instructions skipped above */
		ARM_LDR_IMM (load_get_lmf_addr, ARMREG_R0, ARMREG_PC, (code - load_get_lmf_addr - 8));
		ARM_LDR_IMM (load_trampoline, ARMREG_IP, ARMREG_PC, (code + 4 - load_trampoline - 8));
		code += 8;
	}

	/* Exception case */
	arm_patch (labels [0], code);

	cfa_offset = orig_cfa_offset;

	/*
	 * We have an exception we want to throw in the caller's frame, so pop
	 * the trampoline frame and throw from the caller.
	 */
	/* Store the exception in place of IP */
	ARM_STR_IMM (code, ARMREG_R0, ARMREG_V1, MONO_STRUCT_OFFSET (MonoLMF, iregs) + (ARMREG_R12 * sizeof (target_mgreg_t)));

	ARM_ADD_REG_IMM8 (code, ARMREG_SP, ARMREG_SP, STACK - regsave_size);
	cfa_offset -= STACK - regsave_size;
	mono_add_unwind_op_def_cfa_offset (unwind_ops, code, buf, cfa_offset);
	/* Restore all regs */
	ARM_POP_NWB (code, 0x5fff);
	mono_add_unwind_op_same_value (unwind_ops, code, buf, ARMREG_LR);
	ARM_ADD_REG_IMM8 (code, ARMREG_SP, ARMREG_SP, regsave_size);
	cfa_offset -= regsave_size;
	g_assert (cfa_offset == 0);
	mono_add_unwind_op_def_cfa_offset (unwind_ops, code, buf, cfa_offset);
	/* We are in the parent frame, the exception is in ip */
	ARM_MOV_REG_REG (code, ARMREG_R0, ARMREG_IP);
	/*
	 * EH is initialized after trampolines, so get the address of the variable
	 * which contains throw_exception, and load it from there.
	 */
	if (aot) {
		/* Not really a jit icall */
		code = mono_arm_emit_aotconst (&ji, code, buf, ARMREG_IP, MONO_PATCH_INFO_JIT_ICALL_ADDR, GUINT_TO_POINTER (MONO_JIT_ICALL_mono_rethrow_preserve_exception));
	} else {
		ARM_LDR_IMM (code, ARMREG_IP, ARMREG_PC, 0);
		ARM_B (code, 0);
		*(gpointer*)code = mono_get_rethrow_preserve_exception_addr ();
		code += 4;
	}
	ARM_LDR_IMM (code, ARMREG_IP, ARMREG_IP, 0);
	/* Branch to the throw trampoline */
	/* lr contains the return address, the trampoline will use it as the throw site */
	code = emit_bx (code, ARMREG_IP);

	/* Flush instruction cache, since we've generated code */
	mono_arch_flush_icache (buf, code - buf);
	MONO_PROFILER_RAISE (jit_code_buffer, (buf, code - buf, MONO_PROFILER_CODE_BUFFER_HELPER, NULL));

	/* Sanity check */
	g_assert ((code - buf) <= buf_len);

	g_assert (info);
	tramp_name = mono_get_generic_trampoline_name (tramp_type);
	*info = mono_tramp_info_create (tramp_name, buf, code - buf, ji, unwind_ops);

	return buf;
}

#define SPEC_TRAMP_SIZE 24

gpointer
mono_arch_create_specific_trampoline (gpointer arg1, MonoTrampolineType tramp_type, MonoMemoryManager *mem_manager, guint32 *code_len)
{
	guint8 *code, *buf, *tramp;
	guint32 *constants;
	guint32 short_branch = FALSE;
	guint32 size = SPEC_TRAMP_SIZE;

	tramp = mono_get_trampoline_code (tramp_type);

	if (mem_manager) {
		code = buf = (guint8 *)mono_mem_manager_code_reserve_align (mem_manager, size, 4);
		if ((short_branch = branch_for_target_reachable (code + 4, tramp))) {
			size = 12;
			mono_mem_manager_code_commit (mem_manager, code, SPEC_TRAMP_SIZE, size);
		}
	} else {
		code = buf = mono_global_codeman_reserve (size);
		short_branch = FALSE;
	}

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
	ARM_PUSH (code, 0x5fff);
	if (short_branch) {
		constants = (guint32*)code;
		constants [0] = short_branch | (1 << 24);
		constants [1] = GPOINTER_TO_UINT (arg1);
		code += 8;
	} else {
		ARM_LDR_IMM (code, ARMREG_R1, ARMREG_PC, 8); /* temp reg */
		ARM_MOV_REG_REG (code, ARMREG_LR, ARMREG_PC);
		code = emit_bx (code, ARMREG_R1);

		constants = (guint32*)code;
		constants [0] = GPOINTER_TO_UINT (arg1);
		constants [1] = GPOINTER_TO_UINT (tramp);
		code += 8;
	}

	/* Flush instruction cache, since we've generated code */
	mono_arch_flush_icache (buf, code - buf);
	MONO_PROFILER_RAISE (jit_code_buffer, (buf, code - buf, MONO_PROFILER_CODE_BUFFER_SPECIFIC_TRAMPOLINE, mono_get_generic_trampoline_simple_name (tramp_type)));

	g_assert ((code - buf) <= size);

	if (code_len)
		*code_len = code - buf;

	return buf;
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
	MonoMemoryManager *mem_manager = m_method_get_mem_manager (m);
	GSList *unwind_ops;
	guint32 size = 16;

	start = code = mono_mem_manager_code_reserve (mem_manager, size);

	unwind_ops = mono_arch_get_cie_program ();

	ARM_LDR_IMM (code, ARMREG_IP, ARMREG_PC, 4);
	ARM_ADD_REG_IMM8 (code, ARMREG_R0, ARMREG_R0, MONO_ABI_SIZEOF (MonoObject));
	code = emit_bx (code, ARMREG_IP);
	*(guint32*)code = (guint32)(gsize)addr;
	code += 4;
	mono_arch_flush_icache (start, code - start);
	MONO_PROFILER_RAISE (jit_code_buffer, (start, code - start, MONO_PROFILER_CODE_BUFFER_UNBOX_TRAMPOLINE, m));
	g_assert ((code - start) <= size);
	/*g_print ("unbox trampoline at %d for %s:%s\n", this_pos, m->klass->name, m->name);
	g_print ("unbox code is at %p for method at %p\n", start, addr);*/

	mono_tramp_info_register (mono_tramp_info_create (NULL, start, code - start, NULL, unwind_ops), NULL);

	return start;
}

gpointer
mono_arch_get_static_rgctx_trampoline (MonoMemoryManager *mem_manager, gpointer arg, gpointer addr)
{
	guint8 *code, *start;
	GSList *unwind_ops;
	int buf_len = 16;

	start = code = mono_mem_manager_code_reserve (mem_manager, buf_len);

	unwind_ops = mono_arch_get_cie_program ();

	ARM_LDR_IMM (code, MONO_ARCH_RGCTX_REG, ARMREG_PC, 0);
	ARM_LDR_IMM (code, ARMREG_PC, ARMREG_PC, 0);
	*(guint32*)code = (guint32)(gsize)arg;
	code += 4;
	*(guint32*)code = (guint32)(gsize)addr;
	code += 4;

	g_assert ((code - start) <= buf_len);

	mono_arch_flush_icache (start, code - start);
	MONO_PROFILER_RAISE (jit_code_buffer, (start, code - start, MONO_PROFILER_CODE_BUFFER_GENERICS_TRAMPOLINE, NULL));

	mono_tramp_info_register (mono_tramp_info_create (NULL, start, code - start, NULL, unwind_ops), NULL);

	return start;
}

/* Same as static rgctx trampoline, but clobbering ARMREG_IP, which is scratch */
gpointer
mono_arch_get_ftnptr_arg_trampoline (MonoMemoryManager *mem_manager, gpointer arg, gpointer addr)
{
	guint8 *code, *start;
	GSList *unwind_ops;
	int buf_len = 16;

	start = code = mono_mem_manager_code_reserve (mem_manager, buf_len);

	unwind_ops = mono_arch_get_cie_program ();

	ARM_LDR_IMM (code, ARMREG_IP, ARMREG_PC, 0);
	ARM_LDR_IMM (code, ARMREG_PC, ARMREG_PC, 0);
	*(guint32*)code = (guint32)(gsize)arg;
	code += 4;
	*(guint32*)code = (guint32)(gsize)addr;
	code += 4;

	g_assert ((code - start) <= buf_len);

	mono_arch_flush_icache (start, code - start);
	MONO_PROFILER_RAISE (jit_code_buffer, (start, code - start, MONO_PROFILER_CODE_BUFFER_GENERICS_TRAMPOLINE, NULL));

	mono_tramp_info_register (mono_tramp_info_create (NULL, start, code - start, NULL, unwind_ops), NULL);

	return start;
}

gpointer
mono_arch_create_rgctx_lazy_fetch_trampoline (guint32 slot, MonoTrampInfo **info, gboolean aot)
{
	guint8 *tramp;
	guint8 *code, *buf;
	int tramp_size;
	guint32 code_len;
	guint8 **rgctx_null_jumps;
	int depth, index;
	int i, njumps;
	gboolean mrgctx;
	MonoJumpInfo *ji = NULL;
	GSList *unwind_ops = NULL;

	mrgctx = MONO_RGCTX_SLOT_IS_MRGCTX (slot);
	index = MONO_RGCTX_SLOT_INDEX (slot);
	if (mrgctx)
		index += MONO_SIZEOF_METHOD_RUNTIME_GENERIC_CONTEXT / TARGET_SIZEOF_VOID_P;
	for (depth = 0; ; ++depth) {
		int size = mono_class_rgctx_get_array_size (depth, mrgctx);

		if (index < size - 1)
			break;
		index -= size - 1;
	}

	tramp_size = 64 + 16 * depth;

	code = buf = mono_global_codeman_reserve (tramp_size);

	unwind_ops = mono_arch_get_cie_program ();

	rgctx_null_jumps = g_malloc (sizeof (guint8*) * (depth + 2));
	njumps = 0;

	/* The vtable/mrgctx is in R0 */
	g_assert (MONO_ARCH_VTABLE_REG == ARMREG_R0);

	if (mrgctx) {
		/* get mrgctx ptr */
		ARM_MOV_REG_REG (code, ARMREG_R1, ARMREG_R0);
 	} else {
		/* load rgctx ptr from vtable */
		g_assert (arm_is_imm12 (MONO_STRUCT_OFFSET (MonoVTable, runtime_generic_context)));
		ARM_LDR_IMM (code, ARMREG_R1, ARMREG_R0, MONO_STRUCT_OFFSET (MonoVTable, runtime_generic_context));
		/* is the rgctx ptr null? */
		ARM_CMP_REG_IMM (code, ARMREG_R1, 0, 0);
		/* if yes, jump to actual trampoline */
		rgctx_null_jumps [njumps ++] = code;
		ARM_B_COND (code, ARMCOND_EQ, 0);
	}

	for (i = 0; i < depth; ++i) {
		/* load ptr to next array */
		if (mrgctx && i == 0) {
			g_assert (arm_is_imm12 (MONO_SIZEOF_METHOD_RUNTIME_GENERIC_CONTEXT));
			ARM_LDR_IMM (code, ARMREG_R1, ARMREG_R1, MONO_SIZEOF_METHOD_RUNTIME_GENERIC_CONTEXT);
		} else {
			ARM_LDR_IMM (code, ARMREG_R1, ARMREG_R1, 0);
		}
		/* is the ptr null? */
		ARM_CMP_REG_IMM (code, ARMREG_R1, 0, 0);
		/* if yes, jump to actual trampoline */
		rgctx_null_jumps [njumps ++] = code;
		ARM_B_COND (code, ARMCOND_EQ, 0);
	}

	/* fetch slot */
	code = mono_arm_emit_load_imm (code, ARMREG_R2, TARGET_SIZEOF_VOID_P * (index + 1));
	ARM_LDR_REG_REG (code, ARMREG_R1, ARMREG_R1, ARMREG_R2);
	/* is the slot null? */
	ARM_CMP_REG_IMM (code, ARMREG_R1, 0, 0);
	/* if yes, jump to actual trampoline */
	rgctx_null_jumps [njumps ++] = code;
	ARM_B_COND (code, ARMCOND_EQ, 0);
	/* otherwise return, result is in R1 */
	ARM_MOV_REG_REG (code, ARMREG_R0, ARMREG_R1);
	code = emit_bx (code, ARMREG_LR);

	g_assert (njumps <= depth + 2);
	for (i = 0; i < njumps; ++i)
		arm_patch (rgctx_null_jumps [i], code);

	g_free (rgctx_null_jumps);

	/* Slowpath */

	/* The vtable/mrgctx is still in R0 */

	if (aot) {
		ji = mono_patch_info_list_prepend (ji, code - buf, MONO_PATCH_INFO_SPECIFIC_TRAMPOLINE_LAZY_FETCH_ADDR, GUINT_TO_POINTER (slot));
		ARM_LDR_IMM (code, ARMREG_R1, ARMREG_PC, 0);
		ARM_B (code, 0);
		*(gpointer*)code = NULL;
		code += 4;
		ARM_LDR_REG_REG (code, ARMREG_PC, ARMREG_PC, ARMREG_R1);
	} else {
		MonoMemoryManager *mem_manager = mini_get_default_mem_manager ();
		tramp = (guint8*)mono_arch_create_specific_trampoline (GUINT_TO_POINTER (slot), MONO_TRAMPOLINE_RGCTX_LAZY_FETCH, mem_manager, &code_len);

		/* Jump to the actual trampoline */
		ARM_LDR_IMM (code, ARMREG_R1, ARMREG_PC, 0); /* temp reg */
		code = emit_bx (code, ARMREG_R1);
		*(gpointer*)code = tramp;
		code += 4;
	}

	mono_arch_flush_icache (buf, code - buf);
	MONO_PROFILER_RAISE (jit_code_buffer, (buf, code - buf, MONO_PROFILER_CODE_BUFFER_GENERICS_TRAMPOLINE, NULL));

	g_assert (code - buf <= tramp_size);

	char *name = mono_get_rgctx_fetch_trampoline_name (slot);
	*info = mono_tramp_info_create (name, buf, code - buf, ji, unwind_ops);
	g_free (name);

	return buf;
}

gpointer
mono_arch_create_general_rgctx_lazy_fetch_trampoline (MonoTrampInfo **info, gboolean aot)
{
	guint8 *code, *buf;
	int tramp_size;
	MonoJumpInfo *ji = NULL;
	GSList *unwind_ops = NULL;

	g_assert (aot);

	tramp_size = 32;

	code = buf = mono_global_codeman_reserve (tramp_size);

	unwind_ops = mono_arch_get_cie_program ();

	// FIXME: Currently, we always go to the slow path.
	/* Load trampoline addr */
	ARM_LDR_IMM (code, ARMREG_R1, MONO_ARCH_RGCTX_REG, 4);
	/* The vtable/mrgctx is in R0 */
	g_assert (MONO_ARCH_VTABLE_REG == ARMREG_R0);
	code = emit_bx (code, ARMREG_R1);

	mono_arch_flush_icache (buf, code - buf);
	MONO_PROFILER_RAISE (jit_code_buffer, (buf, code - buf, MONO_PROFILER_CODE_BUFFER_GENERICS_TRAMPOLINE, NULL));

	g_assert (code - buf <= tramp_size);

	*info = mono_tramp_info_create ("rgctx_fetch_trampoline_general", buf, code - buf, ji, unwind_ops);

	return buf;
}

guint8*
mono_arch_create_sdb_trampoline (gboolean single_step, MonoTrampInfo **info, gboolean aot)
{
	guint8 *buf, *code;
	GSList *unwind_ops = NULL;
	MonoJumpInfo *ji = NULL;
	int frame_size;

	buf = code = mono_global_codeman_reserve (96);

	/*
	 * Construct the MonoContext structure on the stack.
	 */

	frame_size = MONO_ABI_SIZEOF (MonoContext);
	frame_size = ALIGN_TO (frame_size, MONO_ARCH_FRAME_ALIGNMENT);
	ARM_SUB_REG_IMM8 (code, ARMREG_SP, ARMREG_SP, frame_size);

	/* save ip, lr and pc into their correspodings ctx.regs slots. */
	ARM_STR_IMM (code, ARMREG_IP, ARMREG_SP, MONO_STRUCT_OFFSET (MonoContext, regs) + sizeof (target_mgreg_t) * ARMREG_IP);
	ARM_STR_IMM (code, ARMREG_LR, ARMREG_SP, MONO_STRUCT_OFFSET (MonoContext, regs) + 4 * ARMREG_LR);
	ARM_STR_IMM (code, ARMREG_LR, ARMREG_SP, MONO_STRUCT_OFFSET (MonoContext, regs) + 4 * ARMREG_PC);

	/* save r0..r10 and fp */
	ARM_ADD_REG_IMM8 (code, ARMREG_IP, ARMREG_SP, MONO_STRUCT_OFFSET (MonoContext, regs));
	ARM_STM (code, ARMREG_IP, 0x0fff);

	/* now we can update fp. */
	ARM_MOV_REG_REG (code, ARMREG_FP, ARMREG_SP);

	/* make ctx.esp hold the actual value of sp at the beginning of this method. */
	ARM_ADD_REG_IMM8 (code, ARMREG_R0, ARMREG_FP, frame_size);
	ARM_STR_IMM (code, ARMREG_R0, ARMREG_IP, 4 * ARMREG_SP);
	ARM_STR_IMM (code, ARMREG_R0, ARMREG_FP, MONO_STRUCT_OFFSET (MonoContext, regs) + 4 * ARMREG_SP);

	/* make ctx.eip hold the address of the call. */
	//ARM_SUB_REG_IMM8 (code, ARMREG_LR, ARMREG_LR, 4);
	ARM_STR_IMM (code, ARMREG_LR, ARMREG_FP, MONO_STRUCT_OFFSET (MonoContext, pc));

	/* r0 now points to the MonoContext */
	ARM_MOV_REG_REG (code, ARMREG_R0, ARMREG_FP);

	/* call */
	if (aot) {
		if (single_step)
			ji = mono_patch_info_list_prepend (ji, code - buf, MONO_PATCH_INFO_JIT_ICALL_ADDR, GUINT_TO_POINTER (MONO_JIT_ICALL_mono_debugger_agent_single_step_from_context));
		else
			ji = mono_patch_info_list_prepend (ji, code - buf, MONO_PATCH_INFO_JIT_ICALL_ADDR, GUINT_TO_POINTER (MONO_JIT_ICALL_mono_debugger_agent_breakpoint_from_context));
		ARM_LDR_IMM (code, ARMREG_IP, ARMREG_PC, 0);
		ARM_B (code, 0);
		*(gpointer*)code = NULL;
		code += 4;
		ARM_LDR_REG_REG (code, ARMREG_IP, ARMREG_PC, ARMREG_IP);
		ARM_BLX_REG (code, ARMREG_IP);
	} else {
		ARM_LDR_IMM (code, ARMREG_IP, ARMREG_PC, 0);
		ARM_B (code, 0);
		if (single_step)
			*(gpointer*)code = (gpointer)mini_get_dbg_callbacks ()->single_step_from_context;
		else
			*(gpointer*)code = (gpointer)mini_get_dbg_callbacks ()->breakpoint_from_context;
		code += 4;
		ARM_BLX_REG (code, ARMREG_IP);
	}

	/* we're back; save ctx.eip and ctx.esp into the corresponding regs slots. */
	ARM_LDR_IMM (code, ARMREG_R0, ARMREG_FP, MONO_STRUCT_OFFSET (MonoContext, pc));
	ARM_STR_IMM (code, ARMREG_R0, ARMREG_FP, MONO_STRUCT_OFFSET (MonoContext, regs) + 4 * ARMREG_LR);
	ARM_STR_IMM (code, ARMREG_R0, ARMREG_FP, MONO_STRUCT_OFFSET (MonoContext, regs) + 4 * ARMREG_PC);

	/* make ip point to the regs array, then restore everything, including pc. */
	ARM_ADD_REG_IMM8 (code, ARMREG_IP, ARMREG_FP, MONO_STRUCT_OFFSET (MonoContext, regs));
	ARM_LDM (code, ARMREG_IP, 0xffff);

	mono_arch_flush_icache (buf, code - buf);
	MONO_PROFILER_RAISE (jit_code_buffer, (buf, code - buf, MONO_PROFILER_CODE_BUFFER_HELPER, NULL));

	const char *tramp_name = single_step ? "sdb_single_step_trampoline" : "sdb_breakpoint_trampoline";
	*info = mono_tramp_info_create (tramp_name, buf, code - buf, ji, unwind_ops);

	return buf;
}

/*
 * mono_arch_get_interp_to_native_trampoline:
 *
 *   See tramp-amd64.c for documentation.
 */
gpointer
mono_arch_get_interp_to_native_trampoline (MonoTrampInfo **info)
{
#ifndef DISABLE_INTERPRETER
	guint8 *start = NULL, *code;
	guint8 *label_start_copy, *label_exit_copy;
	MonoJumpInfo *ji = NULL;
	GSList *unwind_ops = NULL;
	int buf_len, i, off_methodargs, off_targetaddr;
	const int fp_reg = ARMREG_R7;
	int framesize;

	buf_len = 512 + 1024;
	start = code = (guint8 *) mono_global_codeman_reserve (buf_len);

	/*
	* iOS ABI
	*
	* FIXME We save rgctx reg here so we don't regress tests. It should
	* not be clobbered by native->interp transition.
	*/
	ARM_PUSH (code, (1 << MONO_ARCH_RGCTX_REG) | (1 << fp_reg) | (1 << ARMREG_LR));
	ARM_MOV_REG_REG (code, fp_reg, ARMREG_SP);

	/* allocate space for saving the target addr and the call context and align stack */
	framesize = sizeof (target_mgreg_t) + ALIGN_TO (2 * sizeof (target_mgreg_t), MONO_ARCH_FRAME_ALIGNMENT);
	ARM_SUB_REG_IMM8 (code, ARMREG_SP, ARMREG_SP, framesize);

	/* save CallContext* onto stack */
	off_methodargs = -4;
	ARM_STR_IMM (code, ARMREG_R1, fp_reg, off_methodargs);

	/* save target address onto stack */
	off_targetaddr = -8;
	ARM_STR_IMM (code, ARMREG_R0, fp_reg, off_targetaddr);

	/* allocate the stack space necessary for the call */
	ARM_LDR_IMM (code, ARMREG_R3, ARMREG_R1, MONO_STRUCT_OFFSET (CallContext, stack_size));
	ARM_SUB_REG_REG (code, ARMREG_SP, ARMREG_SP, ARMREG_R3);

	/* copy stack from the CallContext, R0 = dest, R1 = source */
	ARM_MOV_REG_REG (code, ARMREG_R0, ARMREG_SP);
	ARM_LDR_IMM (code, ARMREG_R1, ARMREG_R1, MONO_STRUCT_OFFSET (CallContext, stack));

	label_start_copy = code;

	ARM_CMP_REG_IMM (code, ARMREG_R3, 0, 0);
	label_exit_copy = code;
	ARM_B_COND (code, ARMCOND_EQ, 0);
	ARM_LDR_IMM (code, ARMREG_R2, ARMREG_R1, 0);
	ARM_STR_IMM (code, ARMREG_R2, ARMREG_R0, 0);
	ARM_ADD_REG_IMM8 (code, ARMREG_R0, ARMREG_R0, sizeof (target_mgreg_t));
	ARM_ADD_REG_IMM8 (code, ARMREG_R1, ARMREG_R1, sizeof (target_mgreg_t));
	ARM_SUB_REG_IMM8 (code, ARMREG_R3, ARMREG_R3, sizeof (target_mgreg_t));
	ARM_B (code, 0);
	arm_patch (code - 4, label_start_copy);
	arm_patch (label_exit_copy, code);

	ARM_LDR_IMM (code, ARMREG_IP, fp_reg, off_methodargs);
	/* set all general purpose registers from CallContext */
	for (i = 0; i < PARAM_REGS; i++)
		ARM_LDR_IMM (code, i, ARMREG_IP, MONO_STRUCT_OFFSET (CallContext, gregs) + i * sizeof (target_mgreg_t));

	/* set all floating registers from CallContext  */
	for (i = 0; i < FP_PARAM_REGS; i++)
		ARM_FLDD (code, i * 2, ARMREG_IP, MONO_STRUCT_OFFSET (CallContext, fregs) + i * sizeof (double));

	/* load target addr */
	ARM_LDR_IMM (code, ARMREG_IP, fp_reg, off_targetaddr);

	/* call into native function */
	ARM_BLX_REG (code, ARMREG_IP);

	/* load CallContext*/
	ARM_LDR_IMM (code, ARMREG_IP, fp_reg, off_methodargs);

	/* set all general purpose registers to CallContext */
	for (i = 0; i < PARAM_REGS; i++)
		ARM_STR_IMM (code, i, ARMREG_IP, MONO_STRUCT_OFFSET (CallContext, gregs) + i * sizeof (target_mgreg_t));

	/* set all floating registers to CallContext  */
	for (i = 0; i < FP_PARAM_REGS; i++)
		ARM_FSTD (code, i * 2, ARMREG_IP, MONO_STRUCT_OFFSET (CallContext, fregs) + i * sizeof (double));

	ARM_MOV_REG_REG (code, ARMREG_SP, fp_reg);
	ARM_POP (code, (1 << MONO_ARCH_RGCTX_REG) | (1 << fp_reg) | (1 << ARMREG_PC));

	g_assert (code - start < buf_len);

	mono_arch_flush_icache (start, code - start);
	MONO_PROFILER_RAISE (jit_code_buffer, (start, code - start, MONO_PROFILER_CODE_BUFFER_HELPER, NULL));

	if (info)
		*info = mono_tramp_info_create ("interp_to_native_trampoline", start, code - start, ji, unwind_ops);

	return start;
#else
	g_assert_not_reached ();
	return NULL;
#endif /* DISABLE_INTERPRETER */
}

gpointer
mono_arch_get_native_to_interp_trampoline (MonoTrampInfo **info)
{
#ifndef DISABLE_INTERPRETER
	guint8 *start = NULL, *code;
	MonoJumpInfo *ji = NULL;
	GSList *unwind_ops = NULL;
	int buf_len, i;
	const int fp_reg = ARMREG_R7;
	int framesize;

	buf_len = 512;
	start = code = (guint8 *) mono_global_codeman_reserve (buf_len);

	unwind_ops = mono_arch_get_cie_program ();

	/* iOS ABI */
	ARM_PUSH (code, (1 << fp_reg) | (1 << ARMREG_LR));
	mono_add_unwind_op_def_cfa_offset (unwind_ops, code, start, 2 * sizeof (target_mgreg_t));
	mono_add_unwind_op_offset (unwind_ops, code, start, ARMREG_LR, -4);
	mono_add_unwind_op_offset (unwind_ops, code, start, fp_reg, -8);

	ARM_MOV_REG_REG (code, fp_reg, ARMREG_SP);
	mono_add_unwind_op_def_cfa_reg (unwind_ops, code, start, fp_reg);

	/* allocate the CallContext on the stack */
	framesize = ALIGN_TO (MONO_ABI_SIZEOF (CallContext), MONO_ARCH_FRAME_ALIGNMENT);
	ARM_SUB_REG_IMM8 (code, ARMREG_SP, ARMREG_SP, framesize);

	/* save all general purpose registers into the CallContext */
	for (i = 0; i < PARAM_REGS; i++)
		ARM_STR_IMM (code, i, ARMREG_SP, MONO_STRUCT_OFFSET (CallContext, gregs) + i * sizeof (target_mgreg_t));

        /* save all floating registers into the CallContext  */
	for (i = 0; i < FP_PARAM_REGS; i++)
		ARM_FSTD (code, i * 2, ARMREG_SP, MONO_STRUCT_OFFSET (CallContext, fregs) + i * sizeof (double));

	/* set the stack pointer to the value at call site */
	ARM_ADD_REG_IMM8 (code, ARMREG_R0, fp_reg, 2 * sizeof (target_mgreg_t));
	ARM_STR_IMM (code, ARMREG_R0, ARMREG_SP, MONO_STRUCT_OFFSET (CallContext, stack));

	/* call interp_entry with the ccontext and rmethod as arguments */
	ARM_MOV_REG_REG (code, ARMREG_R0, ARMREG_SP);
	ARM_LDR_IMM (code, ARMREG_R1, ARMREG_IP, MONO_STRUCT_OFFSET (MonoFtnDesc, arg));
	ARM_LDR_IMM (code, ARMREG_IP, ARMREG_IP, MONO_STRUCT_OFFSET (MonoFtnDesc, addr));
	ARM_BLX_REG (code, ARMREG_IP);

	/* load the return values from the context */
	for (i = 0; i < PARAM_REGS; i++)
		ARM_LDR_IMM (code, i, ARMREG_SP, MONO_STRUCT_OFFSET (CallContext, gregs) + i * sizeof (target_mgreg_t));

	for (i = 0; i < FP_PARAM_REGS; i++)
		ARM_FLDD (code, i * 2, ARMREG_SP, MONO_STRUCT_OFFSET (CallContext, fregs) + i * sizeof (double));

	/* reset stack and return */
	ARM_MOV_REG_REG (code, ARMREG_SP, fp_reg);
	ARM_POP (code, (1 << fp_reg) | (1 << ARMREG_PC));

	g_assert (code - start < buf_len);

	mono_arch_flush_icache (start, code - start);
	MONO_PROFILER_RAISE (jit_code_buffer, (start, code - start, MONO_PROFILER_CODE_BUFFER_EXCEPTION_HANDLING, NULL));

	if (info)
		*info = mono_tramp_info_create ("native_to_interp_trampoline", start, code - start, ji, unwind_ops);

	return start;
#else
	g_assert_not_reached ();
	return NULL;
#endif /* DISABLE_INTERPRETER */
}

#else

guchar*
mono_arch_create_generic_trampoline (MonoTrampolineType tramp_type, MonoTrampInfo **info, gboolean aot)
{
	g_assert_not_reached ();
	return NULL;
}

gpointer
mono_arch_create_specific_trampoline (gpointer arg1, MonoTrampolineType tramp_type, MonoMemoryManager *mem_manager, guint32 *code_len)
{
	g_assert_not_reached ();
	return NULL;
}

gpointer
mono_arch_get_unbox_trampoline (MonoMethod *m, gpointer addr)
{
	g_assert_not_reached ();
	return NULL;
}

gpointer
mono_arch_get_static_rgctx_trampoline (MonoMemoryManager *mem_manager, gpointer arg, gpointer addr)
{
	g_assert_not_reached ();
	return NULL;
}

gpointer
mono_arch_get_ftnptr_arg_trampoline (MonoMemoryManager *mem_manager, gpointer arg, gpointer addr)
{
	g_assert_not_reached ();
	return NULL;
}

gpointer
mono_arch_create_rgctx_lazy_fetch_trampoline (guint32 slot, MonoTrampInfo **info, gboolean aot)
{
	g_assert_not_reached ();
	return NULL;
}

guint8*
mono_arch_create_sdb_trampoline (gboolean single_step, MonoTrampInfo **info, gboolean aot)
{
	g_assert_not_reached ();
	return NULL;
}

gpointer
mono_arch_get_interp_to_native_trampoline (MonoTrampInfo **info)
{
	g_assert_not_reached ();
	return NULL;
}

gpointer
mono_arch_get_native_to_interp_trampoline (MonoTrampInfo **info)
{
	g_assert_not_reached ();
	return NULL;
}
#endif /* DISABLE_JIT */

guint8*
mono_arch_get_call_target (guint8 *code)
{
	guint32 ins = ((guint32*)code) [-1];

	/* Should be a 'bl' or a 'b' */
	if (((ins >> 25) & 0x7) == 0x5) {
		gint32 disp = ((((gint32)ins) & 0xffffff) << 8) >> 8;
		guint8 *target = code - 4 + 8 + (disp * 4);

		return target;
	} else {
		return NULL;
	}
}

guint32
mono_arch_get_plt_info_offset (guint8 *plt_entry, host_mgreg_t *regs, guint8 *code)
{
	/* The offset is stored as the 4th word of the plt entry */
	return ((guint32*)plt_entry) [3];
}

/*
 * Return the address of the PLT entry called by the thumb code CODE.
 */
guint8*
mono_arm_get_thumb_plt_entry (guint8 *code)
{
	int s, j1, j2, imm10, imm11, i1, i2, imm32;
	guint8 *bl, *base;
	guint16 t1, t2;
	guint8 *target;

	/* code should be right after a BL */
	code = (guint8*)((gsize)code & ~1);
	base = (guint8*)((gsize)code & ~3);
	bl = code - 4;
	t1 = ((guint16*)bl) [0];
	t2 = ((guint16*)bl) [1];

	g_assert ((t1 >> 11) == 0x1e);

	s = (t1 >> 10) & 0x1;
	imm10 = (t1 >> 0) & 0x3ff;
	j1 = (t2 >> 13) & 0x1;
	j2 = (t2 >> 11) & 0x1;
	imm11 = t2 & 0x7ff;

	i1 = (s ^ j1) ? 0 : 1;
	i2 = (s ^ j2) ? 0 : 1;

	imm32 = (imm11 << 1) | (imm10 << 12) | (i2 << 22) | (i1 << 23) | (s << 24);
	if (s)
		/* Sign extend from 25 bits to 32 bits */
		imm32 = ((gint32)imm32 << 7) >> 7;

	target = code + imm32;

	/* target now points to the thumb plt entry */
	/* ldr.w r12, [pc, #8] */
	g_assert (((guint16*)target) [0] == 0xf8df);
	g_assert (((guint16*)target) [1] == 0xc008);

	/* 
	 * The PLT info offset is at offset 16, but mono_arch_get_plt_entry_offset () returns
	 * the 3rd word, so compensate by returning a different value.
	 */
	target += 4;

	return target;
}

#ifndef DISABLE_JIT

/*
 * mono_arch_get_gsharedvt_arg_trampoline:
 *
 *   See tramp-x86.c for documentation.
 */
gpointer
mono_arch_get_gsharedvt_arg_trampoline (gpointer arg, gpointer addr)
{
	guint8 *code, *buf;
	int buf_len;
	gpointer *constants;
	MonoMemoryManager *mem_manager = mini_get_default_mem_manager ();

	buf_len = 24;

	buf = code = mono_mem_manager_code_reserve (mem_manager, buf_len);

	/* Similar to the specialized trampoline code */
	ARM_PUSH (code, (1 << ARMREG_R0) | (1 << ARMREG_R1) | (1 << ARMREG_R2) | (1 << ARMREG_R3) | (1 << ARMREG_LR));
	ARM_LDR_IMM (code, ARMREG_IP, ARMREG_PC, 2 * sizeof (target_mgreg_t));
	/* arg is passed in LR */
	ARM_LDR_IMM (code, ARMREG_LR, ARMREG_PC, 0);
	code = emit_bx (code, ARMREG_IP);
	constants = (gpointer*)code;
	constants [0] = arg;
	constants [1] = addr;
	code += 2 * sizeof (gpointer);

	g_assert ((code - buf) <= buf_len);

	mono_arch_flush_icache (buf, code - buf);
	MONO_PROFILER_RAISE (jit_code_buffer, (buf, code - buf, MONO_PROFILER_CODE_BUFFER_GENERICS_TRAMPOLINE, NULL));

	mono_tramp_info_register (mono_tramp_info_create (NULL, buf, code - buf, NULL, NULL), NULL);

	return buf;
}

#else

gpointer
mono_arch_get_gsharedvt_arg_trampoline (gpointer arg, gpointer addr)
{
	g_assert_not_reached ();
	return NULL;
}

#endif
