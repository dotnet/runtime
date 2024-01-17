/**
 * \file
 * JIT trampoline code for ARM64
 *
 * Copyright 2013 Xamarin Inc
 *
 * Based on tramp-arm.c:
 *
 * Authors:
 *   Paolo Molaro (lupus@ximian.com)
 *
 * (C) 2001-2003 Ximian, Inc.
 * Copyright 2003-2011 Novell Inc
 * Copyright 2011 Xamarin Inc
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */

#include "mini.h"
#include "mini-runtime.h"

#include <mono/arch/arm64/arm64-codegen.h>
#include <mono/metadata/abi-details.h>

#ifndef DISABLE_INTERPRETER
#include "interp/interp.h"
#endif
#include "mono/utils/mono-tls-inline.h"

#include <mono/metadata/components.h>

#define JUMP_TRAMP_PATCH_OFFSET (7 * 4)

void
mono_arch_patch_callsite (guint8 *method_start, guint8 *code_ptr, guint8 *addr)
{
	MINI_BEGIN_CODEGEN ();
	mono_arm_patch (code_ptr - 4, addr, MONO_R_ARM64_BL);
	MINI_END_CODEGEN (code_ptr - 4, 4, -1, NULL);
}

void
mono_arch_patch_plt_entry (guint8 *code, gpointer *got, host_mgreg_t *regs, guint8 *addr)
{
	guint32 ins;
	guint64 slot_addr;
	int disp;

	/*
	 * Decode the address loaded by the PLT entry emitted by arch_emit_plt_entry () in
	 * aot-compiler.c
	 */

	/* adrp */
	ins = ((guint32*)code) [0];
	g_assert (((ins >> 24) & 0x1f) == 0x10);
	disp = (((ins >> 5) & 0x7ffff) << 2) | ((ins >> 29) & 0x3);
	/* FIXME: disp is signed */
	g_assert ((disp >> 20) == 0);

	slot_addr = ((guint64)code + (disp << 12)) & ~0xfff;

	/* add x16, x16, :lo12:got */
	ins = ((guint32*)code) [1];
	g_assert (((ins >> 22) & 0x3) == 0);
	slot_addr += (ins >> 10) & 0xfff;

	/* ldr x16, [x16, <offset>] */
	ins = ((guint32*)code) [2];
	g_assert (((ins >> 24) & 0x3f) == 0x39);
	slot_addr += ((ins >> 10) & 0xfff) * 8;

	g_assert (*(guint64*)slot_addr);
	*(gpointer*)slot_addr = addr;
}

void
mono_arch_patch_jump_trampoline (guint8 *jump_tramp, guint8 *addr)
{
	MINI_BEGIN_CODEGEN ();
	guint8 *patch_addr = jump_tramp + JUMP_TRAMP_PATCH_OFFSET;
	*(gpointer*)patch_addr = addr;
	MINI_END_CODEGEN (patch_addr, 8, -1, NULL);
}

guint8*
mono_arch_get_call_target (guint8 *code)
{
	code -= 4;
	guint32 ins = *(guint32 *)code;
	/* Should be a b/bl */
	if (((ins >> 26) & 0x1f) != 0x5)
		return NULL;
	gint32 disp = ((gint32)((ins & 0x3ffffff) << 6)) >> 6;
	return code + (disp * 4);
}

guint32
mono_arch_get_plt_info_offset (guint8 *plt_entry, host_mgreg_t *regs, guint8 *code)
{
	/* The offset is stored as the 5th word of the plt entry */
	return ((guint32*)plt_entry) [4];
}

#ifndef DISABLE_JIT

guchar*
mono_arch_create_generic_trampoline (MonoTrampolineType tramp_type, MonoTrampInfo **info, gboolean aot)
{
	guint8 *code, *buf, *tramp, *labels [16];
	int i, buf_len, imm;
	int frame_size, offset, gregs_offset, num_fregs, fregs_offset, arg_offset, lmf_offset, res_offset;
	guint64 gregs_regset;
	GSList *unwind_ops = NULL;
	MonoJumpInfo *ji = NULL;
	const char *tramp_name;

	buf_len = 768;
	buf = code = mono_global_codeman_reserve (buf_len);

	/*
	 * We are getting called by a specific trampoline, ip1 contains the trampoline argument.
	 */

	/* Compute stack frame size and offsets */
	offset = 0;
	/* frame block */
	offset += 2 * 8;
	/* gregs */
	gregs_offset = offset;
	offset += 32 * 8;
	/* fregs */
	/* Only have to save the argument regs */
	num_fregs = 8;
	fregs_offset = ALIGN_TO (offset, 16);
	offset += num_fregs * 16;
	/* arg */
	arg_offset = offset;
	offset += 8;
	/* result */
	res_offset = offset;
	offset += 8;
	/* LMF */
	lmf_offset = offset;
	offset += sizeof (MonoLMF);
	//offset += 22 * 8;
	frame_size = ALIGN_TO (offset, MONO_ARCH_FRAME_ALIGNMENT);

	MINI_BEGIN_CODEGEN ();

	/* Setup stack frame */
	imm = frame_size;
	mono_add_unwind_op_def_cfa (unwind_ops, code, buf, ARMREG_SP, 0);
	while (imm > 256) { // TODO: can this be changed to ARM_MAX_ARITH_IMM?
		arm_subx_imm (code, ARMREG_SP, ARMREG_SP, 256);
		imm -= 256;
		mono_add_unwind_op_def_cfa_offset (unwind_ops, code, buf, frame_size - imm);
	}
	arm_subx_imm (code, ARMREG_SP, ARMREG_SP, imm);
	mono_add_unwind_op_def_cfa_offset (unwind_ops, code, buf, frame_size);

	arm_stpx (code, ARMREG_FP, ARMREG_LR, ARMREG_SP, 0);
	mono_add_unwind_op_offset (unwind_ops, code, buf, ARMREG_LR, -frame_size + 8);
	mono_add_unwind_op_offset (unwind_ops, code, buf, ARMREG_FP, -frame_size);

	arm_movspx (code, ARMREG_FP, ARMREG_SP);
	mono_add_unwind_op_def_cfa_reg (unwind_ops, code, buf, ARMREG_FP);

	/* Save gregs */
	// FIXME: Optimize this
	gregs_regset = ~((1 << ARMREG_FP) | (1 << ARMREG_SP));
	code = mono_arm_emit_store_regarray (code, gregs_regset, ARMREG_FP, gregs_offset);
	/* Save fregs */
	for (i = 0; i < num_fregs; ++i) {
		int offs = fregs_offset + (i * 16);
		if (i+1 < num_fregs && arm_is_imm7_scaled (offs, 16)) {
			arm_neon_stp_16b (code, i, i+1, ARMREG_FP, offs);
			i++;
		} else {
			arm_strfpq (code, i, ARMREG_FP, offs);
		}
	}
	/* Save trampoline arg */
	arm_strx (code, ARMREG_IP1, ARMREG_FP, arg_offset);

	/* Setup LMF */
	arm_addx_imm (code, ARMREG_IP0, ARMREG_FP, lmf_offset);
	code = mono_arm_emit_store_regset (code, MONO_ARCH_LMF_REGS, ARMREG_IP0, MONO_STRUCT_OFFSET (MonoLMF, gregs));
	/* Save caller fp */
	arm_ldrx (code, ARMREG_IP1, ARMREG_FP, 0);
	arm_strx (code, ARMREG_IP1, ARMREG_IP0, MONO_STRUCT_OFFSET (MonoLMF, gregs) + (MONO_ARCH_LMF_REG_FP * 8));
	/* Save caller sp */
	arm_movx (code, ARMREG_IP1, ARMREG_FP);
	imm = frame_size;
	while (imm > ARM_MAX_ARITH_IMM) {
		arm_addx_imm (code, ARMREG_IP1, ARMREG_IP1, ARM_MAX_ARITH_IMM);
		imm -= ARM_MAX_ARITH_IMM;
	}
	arm_addx_imm (code, ARMREG_IP1, ARMREG_IP1, imm);
	arm_strx (code, ARMREG_IP1, ARMREG_IP0, MONO_STRUCT_OFFSET (MonoLMF, gregs) + (MONO_ARCH_LMF_REG_SP * 8));
	/* Save caller pc */
	if (tramp_type == MONO_TRAMPOLINE_JUMP)
		arm_movx (code, ARMREG_LR, ARMREG_RZR);
	else
		arm_ldrx (code, ARMREG_LR, ARMREG_FP, 8);
	arm_strx (code, ARMREG_LR, ARMREG_IP0, MONO_STRUCT_OFFSET (MonoLMF, pc));

	/* Save LMF */
	/* Similar to emit_save_lmf () */
	if (aot) {
		code = mono_arm_emit_aotconst (&ji, code, buf, ARMREG_IP0, MONO_PATCH_INFO_JIT_ICALL_ADDR, GUINT_TO_POINTER (MONO_JIT_ICALL_mono_get_lmf_addr));
	} else {
		tramp = (guint8*)mono_get_lmf_addr;
		code = mono_arm_emit_imm64 (code, ARMREG_IP0, (guint64)tramp);
	}
	code = mono_arm_emit_blrx (code, ARMREG_IP0);
	/* r0 contains the address of the tls slot holding the current lmf */
	/* ip0 = lmf */
	arm_addx_imm (code, ARMREG_IP0, ARMREG_FP, lmf_offset);
	/* lmf->lmf_addr = lmf_addr */
	arm_strp (code, ARMREG_R0, ARMREG_IP0, MONO_STRUCT_OFFSET (MonoLMF, lmf_addr));
	/* lmf->previous_lmf = *lmf_addr */
	arm_ldrp (code, ARMREG_IP1, ARMREG_R0, 0);
	arm_strp (code, ARMREG_IP1, ARMREG_IP0, MONO_STRUCT_OFFSET (MonoLMF, previous_lmf));
	/* *lmf_addr = lmf */
	arm_strp (code, ARMREG_IP0, ARMREG_R0, 0);

	/* Call the C trampoline function */
	/* Arg 1 = gregs */
	arm_addx_imm (code, ARMREG_R0, ARMREG_FP, gregs_offset);
	/* Arg 2 = caller */
	if (tramp_type == MONO_TRAMPOLINE_JUMP)
		arm_movx (code, ARMREG_R1, ARMREG_RZR);
	else
		arm_ldrx (code, ARMREG_R1, ARMREG_FP, gregs_offset + (ARMREG_LR * 8));
	/* Arg 3 = arg */
	if (MONO_TRAMPOLINE_TYPE_HAS_ARG (tramp_type))
		/* Passed in r0 */
		arm_ldrx (code, ARMREG_R2, ARMREG_FP, gregs_offset + (ARMREG_R0 * 8));
	else
		arm_ldrx (code, ARMREG_R2, ARMREG_FP, arg_offset);
	/* Arg 4 = trampoline addr */
	arm_movx (code, ARMREG_R3, ARMREG_RZR);

	if (aot) {
		code = mono_arm_emit_aotconst (&ji, code, buf, ARMREG_IP0, MONO_PATCH_INFO_JIT_ICALL_ADDR, GINT_TO_POINTER (mono_trampoline_type_to_jit_icall_id (tramp_type)));
	} else {
		tramp = (guint8*)mono_get_trampoline_func (tramp_type);
		code = mono_arm_emit_imm64 (code, ARMREG_IP0, (guint64)tramp);
	}
	code = mono_arm_emit_blrx (code, ARMREG_IP0);

	/* Save the result */
	arm_strx (code, ARMREG_R0, ARMREG_FP, res_offset);

	/* Restore LMF */
	/* Similar to emit_restore_lmf () */
	/* Clobbers ip0/ip1 */
	/* ip0 = lmf */
	arm_addx_imm (code, ARMREG_IP0, ARMREG_FP, lmf_offset);
	/* ip1 = lmf->previous_lmf */
	arm_ldrp (code, ARMREG_IP1, ARMREG_IP0, MONO_STRUCT_OFFSET (MonoLMF, previous_lmf));
	/* ip0 = lmf->lmf_addr */
	arm_ldrp (code, ARMREG_IP0, ARMREG_IP0, MONO_STRUCT_OFFSET (MonoLMF, lmf_addr));
	/* *lmf_addr = previous_lmf */
	arm_strp (code, ARMREG_IP1, ARMREG_IP0, 0);

	/* Check for thread interruption */
	/* This is not perf critical code so no need to check the interrupt flag */
	if (aot) {
		code = mono_arm_emit_aotconst (&ji, code, buf, ARMREG_IP0, MONO_PATCH_INFO_JIT_ICALL_ADDR, GUINT_TO_POINTER (MONO_JIT_ICALL_mono_thread_force_interruption_checkpoint_noraise));
	} else {
		code = mono_arm_emit_imm64 (code, ARMREG_IP0, (guint64)mono_thread_force_interruption_checkpoint_noraise);
	}
	code = mono_arm_emit_blrx (code, ARMREG_IP0);
	/* Check whenever there is an exception to be thrown */
	labels [0] = code;
	arm_cbnzx (code, ARMREG_R0, 0);

	/* Normal case */

	/* Restore gregs */
	/* Only have to load the argument regs (r0..r8) and the rgctx reg */
	code = mono_arm_emit_load_regarray (code, 0x1ff | (1 << ARMREG_LR) | (1 << MONO_ARCH_RGCTX_REG), ARMREG_FP, gregs_offset);
	/* Restore fregs */
	for (i = 0; i < num_fregs; ++i) {
		int offs = fregs_offset + (i * 16);
		if (i+1 < num_fregs && arm_is_imm7_scaled (offs, 16)) {
			arm_neon_ldp_16b (code, i, i+1, ARMREG_FP, offs);
			i++;
		} else {
			arm_ldrfpq (code, i, ARMREG_FP, offs);
		}
	}

	/* Load the result */
	arm_ldrx (code, ARMREG_IP1, ARMREG_FP, res_offset);

	/* These trampolines return a value */
	if (tramp_type == MONO_TRAMPOLINE_RGCTX_LAZY_FETCH)
		arm_movx (code, ARMREG_R0, ARMREG_IP1);

	/* Cleanup frame */
	code = mono_arm_emit_destroy_frame (code, frame_size, ((1 << ARMREG_IP0)));

	if (tramp_type == MONO_TRAMPOLINE_RGCTX_LAZY_FETCH) {
		arm_retx (code, ARMREG_LR);
	} else {
		code = mono_arm_emit_brx (code, ARMREG_IP1);
	}

	/* Exception case */
	mono_arm_patch (labels [0], code, MONO_R_ARM64_CBZ);

	/*
	 * We have an exception we want to throw in the caller's frame, so pop
	 * the trampoline frame and throw from the caller.
	 */
	code = mono_arm_emit_destroy_frame (code, frame_size, ((1 << ARMREG_IP0)));
	/* We are in the parent frame, the exception is in x0 */
	/*
	 * EH is initialized after trampolines, so get the address of the variable
	 * which contains throw_exception, and load it from there.
	 */
	if (aot) {
		/* Not really a jit icall */
		code = mono_arm_emit_aotconst (&ji, code, buf, ARMREG_IP0, MONO_PATCH_INFO_JIT_ICALL_ADDR, GUINT_TO_POINTER (MONO_JIT_ICALL_mono_rethrow_preserve_exception));
	} else {
		code = mono_arm_emit_imm64 (code, ARMREG_IP0, (guint64)mono_get_rethrow_preserve_exception_addr ());
	}
	arm_ldrx (code, ARMREG_IP0, ARMREG_IP0, 0);
	/* lr contains the return address, the trampoline will use it as the throw site */
	code = mono_arm_emit_brx (code, ARMREG_IP0);

	g_assert ((code - buf) < buf_len);

	MINI_END_CODEGEN (buf, GPTRDIFF_TO_INT (code - buf), MONO_PROFILER_CODE_BUFFER_HELPER, NULL);

	if (info) {
		tramp_name = mono_get_generic_trampoline_name (tramp_type);
		*info = mono_tramp_info_create (tramp_name, buf, GPTRDIFF_TO_UINT32 (code - buf), ji, unwind_ops);
	}

	return (guchar*)MINI_ADDR_TO_FTNPTR (buf);
}

gpointer
mono_arch_create_specific_trampoline (gpointer arg1, MonoTrampolineType tramp_type, MonoMemoryManager *mem_manager, guint32 *code_len)
{
	guint8 *code, *buf, *tramp;
	int buf_len = 64;

	/*
	 * Return a trampoline which calls generic trampoline TRAMP_TYPE passing in ARG1.
	 * Pass the argument in ip1, clobbering ip0.
	 */
	tramp = mono_get_trampoline_code (tramp_type);

	buf = code = mono_mem_manager_code_reserve (mem_manager, buf_len);

	MINI_BEGIN_CODEGEN ();

	if (tramp_type == MONO_TRAMPOLINE_JUMP) {
		/* Create a patchable trampoline by emitting the address at the end */

		guint64 imm = (guint64)arg1;
		arm_movzx (code, ARMREG_IP1, imm & 0xffff, 0);
		arm_movkx (code, ARMREG_IP1, (imm >> 16) & 0xffff, 16);
		arm_movkx (code, ARMREG_IP1, (imm >> 32) & 0xffff, 32);
		arm_movkx (code, ARMREG_IP1, (imm >> 48) & 0xffff, 48);

		arm_adrx (code, ARMREG_IP0, code + (3 * 4));
		arm_ldrx (code, ARMREG_IP0, ARMREG_IP0, 0);
		code = mono_arm_emit_brx (code, ARMREG_IP0);
		g_assert (code - buf == JUMP_TRAMP_PATCH_OFFSET);
		*(guint64*)code = (guint64)(gsize)tramp;
	} else {
		code = mono_arm_emit_imm64 (code, ARMREG_IP1, (guint64)arg1);
		code = mono_arm_emit_imm64 (code, ARMREG_IP0, (guint64)tramp);
		code = mono_arm_emit_brx (code, ARMREG_IP0);
	}

	g_assert ((code - buf) < buf_len);

	MINI_END_CODEGEN (buf, GPTRDIFF_TO_INT (code - buf), MONO_PROFILER_CODE_BUFFER_SPECIFIC_TRAMPOLINE, mono_get_generic_trampoline_simple_name (tramp_type));

	if (code_len)
		*code_len = GPTRDIFF_TO_UINT32 (code - buf);

	return (gpointer)MINI_ADDR_TO_FTNPTR (buf);
}

gpointer
mono_arch_get_unbox_trampoline (MonoMethod *m, gpointer addr)
{
	guint8 *code, *start;
	guint32 size = 32;
	MonoMemoryManager *mem_manager = m_method_get_mem_manager (m);

	start = code = mono_mem_manager_code_reserve (mem_manager, size);

	MINI_BEGIN_CODEGEN ();

	// FIXME: Maybe make a normal non-ptrauth call ?

	code = mono_arm_emit_imm64 (code, ARMREG_IP0, (guint64)addr);
	arm_addx_imm (code, ARMREG_R0, ARMREG_R0, MONO_ABI_SIZEOF (MonoObject));
	code = mono_arm_emit_brx (code, ARMREG_IP0);

	g_assert ((code - start) <= size);

	MINI_END_CODEGEN (start, GPTRDIFF_TO_INT (code - start), MONO_PROFILER_CODE_BUFFER_UNBOX_TRAMPOLINE, m);

	return (gpointer)MINI_ADDR_TO_FTNPTR (start);
}

gpointer
mono_arch_get_static_rgctx_trampoline (MonoMemoryManager *mem_manager, gpointer arg, gpointer addr)
{
	guint8 *code, *start;
	guint32 buf_len = 32;

	start = code = mono_mem_manager_code_reserve (mem_manager, buf_len);

	MINI_BEGIN_CODEGEN ();

	code = mono_arm_emit_imm64 (code, MONO_ARCH_RGCTX_REG, (guint64)arg);
	code = mono_arm_emit_imm64 (code, ARMREG_IP0, (guint64)addr);
	code = mono_arm_emit_brx (code, ARMREG_IP0);

	MINI_END_CODEGEN (start, GPTRDIFF_TO_INT (code - start), MONO_PROFILER_CODE_BUFFER_GENERICS_TRAMPOLINE, NULL);

	g_assert ((code - start) <= buf_len);

	return (gpointer)MINI_ADDR_TO_FTNPTR (start);
}

gpointer
mono_arch_create_rgctx_lazy_fetch_trampoline (guint32 slot, MonoTrampInfo **info, gboolean aot)
{
	guint8 *code, *buf;
	int buf_size;
	int i, depth, index, njumps;
	gboolean is_mrgctx;
	guint8 **rgctx_null_jumps;
	MonoJumpInfo *ji = NULL;
	GSList *unwind_ops = NULL;
	guint8 *tramp;
	guint32 code_len;

	is_mrgctx = MONO_RGCTX_SLOT_IS_MRGCTX (slot);
	index = MONO_RGCTX_SLOT_INDEX (slot);
	if (is_mrgctx)
		index += MONO_SIZEOF_METHOD_RUNTIME_GENERIC_CONTEXT / sizeof (target_mgreg_t);
	for (depth = 0; ; ++depth) {
		int size = mono_class_rgctx_get_array_size (depth, is_mrgctx);

		if (index < size - 1)
			break;
		index -= size - 1;
	}

	buf_size = 64 + 16 * depth;
	code = buf = mono_global_codeman_reserve (buf_size);

	rgctx_null_jumps = g_malloc0 (sizeof (guint8*) * (depth + 2));
	njumps = 0;

	/* The vtable/mrgtx is in R0 */
	g_assert (MONO_ARCH_VTABLE_REG == ARMREG_R0);

	MINI_BEGIN_CODEGEN ();

	if (is_mrgctx) {
		/* get mrgctx ptr */
		arm_movx (code, ARMREG_IP1, ARMREG_R0);
 	} else {
		/* load rgctx ptr from vtable */
		code = mono_arm_emit_ldrx (code, ARMREG_IP1, ARMREG_R0, MONO_STRUCT_OFFSET (MonoVTable, runtime_generic_context));
		/* is the rgctx ptr null? */
		/* if yes, jump to actual trampoline */
		rgctx_null_jumps [njumps ++] = code;
		arm_cbzx (code, ARMREG_IP1, 0);
	}

	for (i = 0; i < depth; ++i) {
		/* load ptr to next array */
		if (is_mrgctx && i == 0) {
			code = mono_arm_emit_ldrx (code, ARMREG_IP1, ARMREG_IP1, MONO_SIZEOF_METHOD_RUNTIME_GENERIC_CONTEXT);
		} else {
			code = mono_arm_emit_ldrx (code, ARMREG_IP1, ARMREG_IP1, 0);
		}
		/* is the ptr null? */
		/* if yes, jump to actual trampoline */
		rgctx_null_jumps [njumps ++] = code;
		arm_cbzx (code, ARMREG_IP1, 0);
	}

	/* fetch slot */
	code = mono_arm_emit_ldrx (code, ARMREG_IP1, ARMREG_IP1, sizeof (target_mgreg_t) * (index + 1));
	/* is the slot null? */
	/* if yes, jump to actual trampoline */
	rgctx_null_jumps [njumps ++] = code;
	arm_cbzx (code, ARMREG_IP1, 0);
	/* otherwise return, result is in IP1 */
	arm_movx (code, ARMREG_R0, ARMREG_IP1);
	arm_brx (code, ARMREG_LR);

	g_assert (njumps <= depth + 2);
	for (i = 0; i < njumps; ++i)
		mono_arm_patch (rgctx_null_jumps [i], code, MONO_R_ARM64_CBZ);

	g_free (rgctx_null_jumps);

	/* Slowpath */

	/* Call mono_rgctx_lazy_fetch_trampoline (), passing in the slot as argument */
	/* The vtable/mrgctx is still in R0 */
	if (aot) {
		code = mono_arm_emit_aotconst (&ji, code, buf, ARMREG_IP0, MONO_PATCH_INFO_SPECIFIC_TRAMPOLINE_LAZY_FETCH_ADDR, GUINT_TO_POINTER (slot));
	} else {
		MonoMemoryManager *mem_manager = mini_get_default_mem_manager ();
		tramp = (guint8*)mono_arch_create_specific_trampoline (GUINT_TO_POINTER (slot), MONO_TRAMPOLINE_RGCTX_LAZY_FETCH, mem_manager, &code_len);
		code = mono_arm_emit_imm64 (code, ARMREG_IP0, (guint64)tramp);
	}
	code = mono_arm_emit_brx (code, ARMREG_IP0);

	g_assert (code - buf <= buf_size);

	MINI_END_CODEGEN (buf, GPTRDIFF_TO_INT (code - buf), MONO_PROFILER_CODE_BUFFER_GENERICS_TRAMPOLINE, NULL);

	if (info) {
		char *name = mono_get_rgctx_fetch_trampoline_name (slot);
		*info = mono_tramp_info_create (name, buf, GPTRDIFF_TO_UINT32 (code - buf), ji, unwind_ops);
		g_free (name);
	}

	return (gpointer)MINI_ADDR_TO_FTNPTR (buf);
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

	mono_add_unwind_op_def_cfa (unwind_ops, code, buf, ARMREG_SP, 0);

	MINI_BEGIN_CODEGEN ();

	// FIXME: Currently, we always go to the slow path.
	/* Load trampoline addr */
	arm_ldrx (code, ARMREG_IP0, MONO_ARCH_RGCTX_REG, 8);
	/* The vtable/mrgctx is in R0 */
	g_assert (MONO_ARCH_VTABLE_REG == ARMREG_R0);
	code = mono_arm_emit_brx (code, ARMREG_IP0);

	g_assert (code - buf <= tramp_size);

	MINI_END_CODEGEN (buf, GPTRDIFF_TO_INT (code - buf), MONO_PROFILER_CODE_BUFFER_GENERICS_TRAMPOLINE, NULL);

	if (info)
		*info = mono_tramp_info_create ("rgctx_fetch_trampoline_general", buf, GPTRDIFF_TO_UINT32 (code - buf), ji, unwind_ops);

	return (gpointer)MINI_ADDR_TO_FTNPTR (buf);
}

/*
 * mono_arch_create_sdb_trampoline:
 *
 *   Return a trampoline which captures the current context, passes it to
 * mono_component_debugger ()->single_step_from_context ()/mono_component_debugger ()->breakpoint_from_context (),
 * then restores the (potentially changed) context.
 */
guint8*
mono_arch_create_sdb_trampoline (gboolean single_step, MonoTrampInfo **info, gboolean aot)
{
	int tramp_size = 512;
	int offset, imm, frame_size, ctx_offset;
	guint64 gregs_regset;
	guint8 *code, *buf;
	GSList *unwind_ops = NULL;
	MonoJumpInfo *ji = NULL;

	code = buf = mono_global_codeman_reserve (tramp_size);

	/* Compute stack frame size and offsets */
	offset = 0;
	/* frame block */
	offset += 2 * 8;
	/* MonoContext */
	ctx_offset = offset;
	offset += sizeof (MonoContext);
	offset = ALIGN_TO (offset, MONO_ARCH_FRAME_ALIGNMENT);
	frame_size = offset;

	// FIXME: Unwind info

	MINI_BEGIN_CODEGEN ();

	/* Setup stack frame */
	imm = frame_size;
	while (imm > ARM_MAX_ARITH_IMM) {
		arm_subx_imm (code, ARMREG_SP, ARMREG_SP, ARM_MAX_ARITH_IMM);
		imm -= ARM_MAX_ARITH_IMM;
	}
	arm_subx_imm (code, ARMREG_SP, ARMREG_SP, imm);
	arm_stpx (code, ARMREG_FP, ARMREG_LR, ARMREG_SP, 0);
	arm_movspx (code, ARMREG_FP, ARMREG_SP);

	/* Initialize a MonoContext structure on the stack */
	/* No need to save fregs */
	gregs_regset = ~((1 << ARMREG_FP) | (1 << ARMREG_SP));
	code = mono_arm_emit_store_regarray (code, gregs_regset, ARMREG_FP, ctx_offset + G_STRUCT_OFFSET (MonoContext, regs));
	/* Save caller fp */
	arm_ldrx (code, ARMREG_IP1, ARMREG_FP, 0);
	arm_strx (code, ARMREG_IP1, ARMREG_FP, ctx_offset + G_STRUCT_OFFSET (MonoContext, regs) + (ARMREG_FP * 8));
	/* Save caller sp */
	arm_movx (code, ARMREG_IP1, ARMREG_FP);
	imm = frame_size;
	while (imm > ARM_MAX_ARITH_IMM) {
		arm_addx_imm (code, ARMREG_IP1, ARMREG_IP1, ARM_MAX_ARITH_IMM);
		imm -= ARM_MAX_ARITH_IMM;
	}
	arm_addx_imm (code, ARMREG_IP1, ARMREG_IP1, imm);
	arm_strx (code, ARMREG_IP1, ARMREG_FP, ctx_offset + G_STRUCT_OFFSET (MonoContext, regs) + (ARMREG_SP * 8));
	/* Save caller ip */
	arm_ldrx (code, ARMREG_IP1, ARMREG_FP, 8);
	arm_strx (code, ARMREG_IP1, ARMREG_FP, ctx_offset + G_STRUCT_OFFSET (MonoContext, pc));

	/* Call the single step/breakpoint function in sdb */
	/* Arg1 = ctx */
	arm_addx_imm (code, ARMREG_R0, ARMREG_FP, ctx_offset);
	if (aot) {
		if (single_step)
			code = mono_arm_emit_aotconst (&ji, code, buf, ARMREG_IP0, MONO_PATCH_INFO_JIT_ICALL_ADDR, GUINT_TO_POINTER (MONO_JIT_ICALL_mono_debugger_agent_single_step_from_context));
		else
			code = mono_arm_emit_aotconst (&ji, code, buf, ARMREG_IP0, MONO_PATCH_INFO_JIT_ICALL_ADDR, GUINT_TO_POINTER (MONO_JIT_ICALL_mono_debugger_agent_breakpoint_from_context));
	} else {
		void (*addr) (MonoContext *ctx) = single_step ? mono_component_debugger ()->single_step_from_context : mono_component_debugger ()->breakpoint_from_context;

		code = mono_arm_emit_imm64 (code, ARMREG_IP0, (guint64)addr);
	}
	code = mono_arm_emit_blrx (code, ARMREG_IP0);

	/* Restore ctx */
	/* Save fp/pc into the frame block */
	arm_ldrx (code, ARMREG_IP0, ARMREG_FP, ctx_offset + G_STRUCT_OFFSET (MonoContext, regs) + (ARMREG_FP * 8));
	arm_strx (code, ARMREG_IP0, ARMREG_FP, 0);
	arm_ldrx (code, ARMREG_IP0, ARMREG_FP, ctx_offset + G_STRUCT_OFFSET (MonoContext, pc));
	arm_strx (code, ARMREG_IP0, ARMREG_FP, 8);
	gregs_regset = ~((1 << ARMREG_FP) | (1 << ARMREG_SP));
	code = mono_arm_emit_load_regarray (code, gregs_regset, ARMREG_FP, ctx_offset + G_STRUCT_OFFSET (MonoContext, regs));

	code = mono_arm_emit_destroy_frame (code, frame_size, ((1 << ARMREG_IP0) | (1 << ARMREG_IP1)));

	arm_retx (code, ARMREG_LR);

	g_assert (code - buf <= tramp_size);

	MINI_END_CODEGEN (buf, GPTRDIFF_TO_INT (code - buf), MONO_PROFILER_CODE_BUFFER_HELPER, NULL);

	const char *tramp_name = single_step ? "sdb_single_step_trampoline" : "sdb_breakpoint_trampoline";
	*info = mono_tramp_info_create (tramp_name, buf, GPTRDIFF_TO_UINT32 (code - buf), ji, unwind_ops);

	return (guint8*)MINI_ADDR_TO_FTNPTR (buf);
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
	int buf_len, i, framesize = 0, off_methodargs, off_targetaddr;

	buf_len = 512 + 1024;
	start = code = (guint8 *) mono_global_codeman_reserve (buf_len);

	/* allocate frame */
	framesize += 2 * sizeof (host_mgreg_t);

	off_methodargs = framesize;
	framesize += sizeof (host_mgreg_t);

	off_targetaddr = framesize;
	framesize += sizeof (host_mgreg_t);

	framesize = ALIGN_TO (framesize, MONO_ARCH_FRAME_ALIGNMENT);

	MINI_BEGIN_CODEGEN ();

	arm_subx_imm (code, ARMREG_SP, ARMREG_SP, framesize);
	arm_stpx (code, ARMREG_FP, ARMREG_LR, ARMREG_SP, 0);
	arm_movspx (code, ARMREG_FP, ARMREG_SP);

	/* save CallContext* onto stack */
	arm_strx (code, ARMREG_R1, ARMREG_FP, off_methodargs);

	/* save target address onto stack */
	arm_strx (code, ARMREG_R0, ARMREG_FP, off_targetaddr);

	/* allocate the stack space necessary for the call */
	arm_ldrw (code, ARMREG_R0, ARMREG_R1, MONO_STRUCT_OFFSET (CallContext, stack_size));
	arm_movspx (code, ARMREG_IP0, ARMREG_SP);
	arm_subx (code, ARMREG_IP0, ARMREG_IP0, ARMREG_R0);
	arm_movspx (code, ARMREG_SP, ARMREG_IP0);

	/* copy stack from the CallContext, IP0 = dest, IP1 = source */
	arm_movspx (code, ARMREG_IP0, ARMREG_SP);
	arm_ldrp (code, ARMREG_IP1, ARMREG_R1, MONO_STRUCT_OFFSET (CallContext, stack));

	label_start_copy = code;

	arm_cmpx_imm (code, ARMREG_R0, 0);
	label_exit_copy = code;
	arm_bcc (code, ARMCOND_EQ, 0);
	arm_ldrx (code, ARMREG_R2, ARMREG_IP1, 0);
	arm_strx (code, ARMREG_R2, ARMREG_IP0, 0);
	arm_addx_imm (code, ARMREG_IP0, ARMREG_IP0, sizeof (host_mgreg_t));
	arm_addx_imm (code, ARMREG_IP1, ARMREG_IP1, sizeof (host_mgreg_t));
	arm_subx_imm (code, ARMREG_R0, ARMREG_R0, sizeof (host_mgreg_t));
	arm_b (code, label_start_copy);
	mono_arm_patch (label_exit_copy, code, MONO_R_ARM64_BCC);

	/* Load CallContext* into IP0 */
	arm_ldrx (code, ARMREG_IP0, ARMREG_FP, off_methodargs);

	/* set all general purpose registers from CallContext */
	for (i = 0; i < PARAM_REGS + 1; i++)
		arm_ldrx (code, i, ARMREG_IP0, MONO_STRUCT_OFFSET (CallContext, gregs) + i * sizeof (host_mgreg_t));

	/* set all floating registers from CallContext  */
	for (i = 0; i < FP_PARAM_REGS; i++)
		arm_ldrfpx (code, i, ARMREG_IP0, MONO_STRUCT_OFFSET (CallContext, fregs) + i * sizeof (double));

	/* load target addr */
	arm_ldrx (code, ARMREG_IP0, ARMREG_FP, off_targetaddr);

	/* call into native function */
	code = mono_arm_emit_blrx (code, ARMREG_IP0);

	/* load CallContext* */
	arm_ldrx (code, ARMREG_IP0, ARMREG_FP, off_methodargs);

	/* set all general purpose registers to CallContext */
	for (i = 0; i < PARAM_REGS; i++)
		arm_strx (code, i, ARMREG_IP0, MONO_STRUCT_OFFSET (CallContext, gregs) + i * sizeof (host_mgreg_t));

	/* set all floating registers to CallContext  */
	for (i = 0; i < FP_PARAM_REGS; i++)
		arm_strfpx (code, i, ARMREG_IP0, MONO_STRUCT_OFFSET (CallContext, fregs) + i * sizeof (double));

	arm_movspx (code, ARMREG_SP, ARMREG_FP);
	arm_ldpx (code, ARMREG_FP, ARMREG_LR, ARMREG_SP, 0);
	arm_addx_imm (code, ARMREG_SP, ARMREG_SP, framesize);
	arm_retx (code, ARMREG_LR);

	g_assert (code - start < buf_len);

	MINI_END_CODEGEN (start, GPTRDIFF_TO_INT (code - start), MONO_PROFILER_CODE_BUFFER_HELPER, NULL);

	if (info)
		*info = mono_tramp_info_create ("interp_to_native_trampoline", start, GPTRDIFF_TO_UINT32 (code - start), ji, unwind_ops);

	return (guint8*)MINI_ADDR_TO_FTNPTR (start);
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
	int framesize, offset, ccontext_offset;

	buf_len = 512;
	start = code = (guint8 *) mono_global_codeman_reserve (buf_len);

	/* Allocate frame (FP + LR + CallContext) */
	offset = 2 * sizeof (host_mgreg_t);
	ccontext_offset = offset;
	offset += sizeof (CallContext);
	framesize = ALIGN_TO (offset, MONO_ARCH_FRAME_ALIGNMENT);

	MINI_BEGIN_CODEGEN ();

	mono_add_unwind_op_def_cfa (unwind_ops, code, start, ARMREG_SP, 0);

	arm_subx_imm (code, ARMREG_SP, ARMREG_SP, framesize);
	mono_add_unwind_op_def_cfa_offset (unwind_ops, code, start, framesize);

	arm_stpx (code, ARMREG_FP, ARMREG_LR, ARMREG_SP, 0);
	mono_add_unwind_op_offset (unwind_ops, code, start, ARMREG_LR, -framesize + 8);
	mono_add_unwind_op_offset (unwind_ops, code, start, ARMREG_FP, -framesize);

	arm_movspx (code, ARMREG_FP, ARMREG_SP);
	mono_add_unwind_op_def_cfa_reg (unwind_ops, code, start, ARMREG_FP);

	/* save all general purpose registers into the CallContext */
	for (i = 0; i < PARAM_REGS + 1; i++)
		arm_strx (code, i, ARMREG_FP, ccontext_offset + MONO_STRUCT_OFFSET (CallContext, gregs) + i * sizeof (host_mgreg_t));

	/* save all floating registers into the CallContext  */
	for (i = 0; i < FP_PARAM_REGS; i++)
		arm_strfpx (code, i, ARMREG_FP, ccontext_offset + MONO_STRUCT_OFFSET (CallContext, fregs) + i * sizeof (double));

	/* set the stack pointer to the value at call site */
	arm_addx_imm (code, ARMREG_R0, ARMREG_FP, framesize);
	arm_strp (code, ARMREG_R0, ARMREG_FP, ccontext_offset + MONO_STRUCT_OFFSET (CallContext, stack));

	/* call interp_entry with the ccontext and rmethod as arguments */
	arm_addx_imm (code, ARMREG_R0, ARMREG_FP, ccontext_offset);
	arm_ldrp (code, ARMREG_R1, MONO_ARCH_RGCTX_REG, MONO_STRUCT_OFFSET (MonoFtnDesc, arg));
	arm_ldrp (code, ARMREG_IP0, MONO_ARCH_RGCTX_REG, MONO_STRUCT_OFFSET (MonoFtnDesc, addr));
	code = mono_arm_emit_blrx (code, ARMREG_IP0);

	/* load the return values from the context */
	for (i = 0; i < PARAM_REGS; i++)
		arm_ldrx (code, i, ARMREG_FP, ccontext_offset + MONO_STRUCT_OFFSET (CallContext, gregs) + i * sizeof (host_mgreg_t));

	for (i = 0; i < FP_PARAM_REGS; i++)
		arm_ldrfpx (code, i, ARMREG_FP, ccontext_offset + MONO_STRUCT_OFFSET (CallContext, fregs) + i * sizeof (double));

	/* reset stack and return */
	arm_ldpx (code, ARMREG_FP, ARMREG_LR, ARMREG_SP, 0);
	arm_addx_imm (code, ARMREG_SP, ARMREG_SP, framesize);
	arm_retx (code, ARMREG_LR);

	g_assert (code - start < buf_len);

	MINI_END_CODEGEN (start, GPTRDIFF_TO_INT (code - start), MONO_PROFILER_CODE_BUFFER_EXCEPTION_HANDLING, NULL);

	if (info)
		*info = mono_tramp_info_create ("native_to_interp_trampoline", start, GPTRDIFF_TO_UINT32 (code - start), ji, unwind_ops);

	return (guint8*)MINI_ADDR_TO_FTNPTR (start);
#else
	g_assert_not_reached ();
	return NULL;
#endif /* DISABLE_INTERPRETER */
}

#else /* DISABLE_JIT */

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

#endif /* !DISABLE_JIT */
