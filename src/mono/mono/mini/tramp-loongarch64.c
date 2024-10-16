/**
 * \file
 * LOONGARCH64 backend for the Mono code generator
 *
 * Authors:
 *   Qiao Pengcheng (qiaopengcheng@loongson.cn), Liu An(liuan@loongson.cn)
 *
 * Copyright (c) 2021 Loongson Technology, Inc
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */

#include "mini.h"
#include "mini-runtime.h"
#include "mono/utils/mono-tls-inline.h"
#ifndef DISABLE_INTERPRETER
#include "interp/interp.h"
#endif

#include <mono/metadata/abi-details.h>
#include <mono/arch/loongarch64/loongarch64-codegen.h>
#include <mono/metadata/components.h>

void
mono_arch_patch_callsite (guint8 *method_start, guint8 *code_ptr, guint8 *addr)
{
	MINI_BEGIN_CODEGEN ();
	mono_loongarch64_patch (code_ptr - 4, addr, MONO_R_LOONGARCH64_BL);
	MINI_END_CODEGEN (code_ptr - 4, 4, -1, NULL);
}

void
mono_arch_patch_plt_entry (guint8 *code, gpointer *got, host_mgreg_t *regs, guint8 *addr)
{
	g_assert_not_reached ();
}

guint8*
mono_arch_get_call_target (guint8 *code)
{
	code -= 4;
	guint32 ins = *(guint32 *)code;
	/* Should be a b/bl */
	if ((((ins >> 26) & 0x1f) == 0x14) || (((ins >> 26) & 0x1f) == 0x15)) {
		gint32 disp = ((gint32)((((ins & 0x3ff) << 16) | ((ins >> 10) & 0xffff)) << 6)) >> 6;
		return code + (disp * 4);
	}
	return NULL;
}

guint32
mono_arch_get_plt_info_offset (guint8 *plt_entry, host_mgreg_t *regs, guint8 *code)
{
	/* The offset is stored as the 6th word of the plt entry */
	return ((guint32*)plt_entry) [5];
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
	 * We are getting called by a specific trampoline, r21 contains the trampoline argument.
	 */

	/* Compute stack frame size and offsets */
	offset = 0;
	/* frame block */
	offset += 2 * 8;
	/* gregs */
	gregs_offset = offset;
	offset += 32 * 8;
	/* fregs */
	// FIXME: Save 128 bits
	/* Only have to save the argument regs */
	num_fregs = 8;
	fregs_offset = offset;
	offset += num_fregs * 8;
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
	mono_add_unwind_op_def_cfa (unwind_ops, code, buf, loongarch_sp, 0);
	while (imm > 256) {
		loongarch_addid (code, loongarch_sp, loongarch_sp, -256);
		imm -= 256;
		mono_add_unwind_op_def_cfa_offset (unwind_ops, code, buf, frame_size - imm);
	}
	loongarch_addid (code, loongarch_sp, loongarch_sp, -imm);
	mono_add_unwind_op_def_cfa_offset (unwind_ops, code, buf, frame_size);

	loongarch_std (code, loongarch_fp, loongarch_sp, 0);
	loongarch_std (code, loongarch_ra, loongarch_sp, 8);
	mono_add_unwind_op_offset (unwind_ops, code, buf, loongarch_fp, -frame_size);
	mono_add_unwind_op_offset (unwind_ops, code, buf, loongarch_ra, -frame_size + 8);

	loongarch_ori (code, loongarch_fp, loongarch_sp, 0);
	mono_add_unwind_op_def_cfa_reg (unwind_ops, code, buf, loongarch_fp);

	/* Save gregs */
	// FIXME: Optimize this
	gregs_regset = ~((1 << loongarch_fp) | (1 << loongarch_sp));
	code = mono_loongarch_emit_store_regarray (code, gregs_regset, loongarch_fp, gregs_offset);
	/* Save fregs */
	for (i = 0; i < num_fregs; ++i)
		loongarch_fstd (code, i, loongarch_fp, fregs_offset + (i * 8));
	/* Save trampoline arg */
	loongarch_std (code, loongarch_t0, loongarch_fp, arg_offset);

	/* Setup LMF */
	loongarch_addid (code, loongarch_r21, loongarch_fp, lmf_offset);

	code = mono_loongarch_emit_store_regset (code, MONO_ARCH_LMF_REGS, loongarch_r21, MONO_STRUCT_OFFSET (MonoLMF, gregs));

	/* Save caller fp */
	loongarch_ldd (code, loongarch_t0, loongarch_fp, 0);
	loongarch_std (code, loongarch_t0, loongarch_r21, MONO_STRUCT_OFFSET (MonoLMF, gregs) + (MONO_ARCH_LMF_REG_FP * 8));
	/* Save caller sp */
	loongarch_move (code, loongarch_t0, loongarch_fp);
	imm = frame_size;
	while (imm > 256) {
		loongarch_addid (code, loongarch_t0, loongarch_t0, 256);
		imm -= 256;
	}
	loongarch_addid (code, loongarch_t0, loongarch_t0, imm);
	loongarch_std (code, loongarch_t0, loongarch_r21, MONO_STRUCT_OFFSET (MonoLMF, gregs) + (MONO_ARCH_LMF_REG_SP * 8));
	/* Save caller pc */
	if (tramp_type == MONO_TRAMPOLINE_JUMP)
		loongarch_move (code, loongarch_ra, loongarch_zero);
	else
		loongarch_ldd (code, loongarch_ra, loongarch_fp, 8);
	loongarch_std (code, loongarch_ra, loongarch_r21, MONO_STRUCT_OFFSET (MonoLMF, pc));

	/* Save LMF */
	/* Similar to emit_save_lmf () */
	tramp = (guint8*)mono_get_lmf_addr;
	code = mono_loongarch_emit_imm64 (code, loongarch_r21, (guint64)tramp);
	loongarch_jirl (code, 1, loongarch_r21, 0);

	/* a0 contains the address of the tls slot holding the current lmf */
	/* r21 = lmf */
	loongarch_addid (code, loongarch_r21, loongarch_fp, lmf_offset);
	/* lmf->lmf_addr = lmf_addr */
	loongarch_std (code, loongarch_a0, loongarch_r21, MONO_STRUCT_OFFSET (MonoLMF, lmf_addr));
	/* lmf->previous_lmf = *lmf_addr */
	loongarch_ldd (code, loongarch_t0, loongarch_a0, 0);
	loongarch_std (code, loongarch_t0, loongarch_r21, MONO_STRUCT_OFFSET (MonoLMF, previous_lmf));
	/* *lmf_addr = lmf */
	loongarch_std (code, loongarch_r21, loongarch_a0, 0);

	/* Call the C trampoline function */
	/* Arg 1 = gregs */
	loongarch_addid (code, loongarch_a0, loongarch_fp, gregs_offset);
	/* Arg 2 = caller */
	if (tramp_type == MONO_TRAMPOLINE_JUMP)
		loongarch_move (code, loongarch_a1, loongarch_zero);
	else
		loongarch_ldd (code, loongarch_a1, loongarch_fp, gregs_offset + (loongarch_ra * 8));
	/* Arg 3 = arg */
	if (MONO_TRAMPOLINE_TYPE_HAS_ARG (tramp_type))
		/* Passed in a0 */
		loongarch_ldd (code, loongarch_a2, loongarch_fp, gregs_offset + (loongarch_a0 * 8));
	else
		loongarch_ldd (code, loongarch_a2, loongarch_fp, arg_offset);
	/* Arg 4 = trampoline addr */
	loongarch_move (code, loongarch_a3, loongarch_zero);

	tramp = (guint8*)mono_get_trampoline_func (tramp_type);
	code = mono_loongarch_emit_imm64 (code, loongarch_r21, (guint64)tramp);
	loongarch_jirl (code, 1, loongarch_r21, 0);

	/* Save the result */
	loongarch_std (code, loongarch_a0, loongarch_fp, res_offset);

	/* Restore LMF */
	/* Similar to emit_restore_lmf () */
	/* Clobbers r21/t0 */
	/* r21 = lmf */
	loongarch_addid (code, loongarch_r21, loongarch_fp, lmf_offset);
	/* t0 = lmf->previous_lmf */
	loongarch_ldd (code, loongarch_t0, loongarch_r21, MONO_STRUCT_OFFSET (MonoLMF, previous_lmf));
	/* r21 = lmf->lmf_addr */
	loongarch_ldd (code, loongarch_r21, loongarch_r21, MONO_STRUCT_OFFSET (MonoLMF, lmf_addr));
	/* *lmf_addr = previous_lmf */
	loongarch_std (code, loongarch_t0, loongarch_r21, 0);

	/* Check for thread interruption */
	/* This is not perf critical code so no need to check the interrupt flag */
	code = mono_loongarch_emit_imm64 (code, loongarch_r21, (guint64)mono_thread_force_interruption_checkpoint_noraise);
	loongarch_jirl (code, 1, loongarch_r21, 0);

	/* Check whenever there is an exception to be thrown */
	labels [0] = code;
	loongarch_bnez (code, loongarch_a0, 0);

	/* Normal case */

	/* Restore gregs */
	/* Only have to load the argument regs (a0..a7) and the rgctx reg .*/
	code = mono_loongarch_emit_load_regarray (code, 0xff0 | (1 << loongarch_ra) | (1 << MONO_ARCH_RGCTX_REG), loongarch_fp, gregs_offset);

	/* Restore fregs */
	for (i = 0; i < num_fregs; ++i)
		loongarch_fldd (code, i, loongarch_fp, fregs_offset + (i * 8));

	/* Load the result */
	loongarch_ldd (code, loongarch_t0, loongarch_fp, res_offset);

	/* These trampolines return a value */
	if (tramp_type == MONO_TRAMPOLINE_RGCTX_LAZY_FETCH)
		loongarch_move (code, loongarch_a0, loongarch_t0);

	/* Cleanup frame */
	code = mono_loongarch64_emit_destroy_frame (code, frame_size);

	if (tramp_type == MONO_TRAMPOLINE_RGCTX_LAZY_FETCH) {
		loongarch_jirl (code, 0, loongarch_ra, 0);
	} else {
		loongarch_jirl (code, 0, loongarch_t0, 0);
	}

	/* Exception case */
	mono_loongarch64_patch (labels [0], code, MONO_R_LOONGARCH64_BZ);

	/*
	 * We have an exception we want to throw in the caller's frame, so pop
	 * the trampoline frame and throw from the caller.
	 */
	code = mono_loongarch64_emit_destroy_frame (code, frame_size);

	/* We are in the parent frame, the exception is in a0 */
	/*
	 * EH is initialized after trampolines, so get the address of the variable
	 * which contains throw_exception, and load it from there.
	 */
	code = mono_loongarch_emit_imm64 (code, loongarch_r21, (guint64)mono_get_rethrow_preserve_exception_addr ());
	loongarch_ldd (code, loongarch_r21, loongarch_r21, 0);
	/* lr contains the return address, the trampoline will use it as the throw site */
	loongarch_jirl (code, 0, loongarch_r21, 0);

	g_assert ((code - buf) < buf_len);

	MINI_END_CODEGEN (buf, code - buf, MONO_PROFILER_CODE_BUFFER_HELPER, NULL);

	if (info) {
		tramp_name = mono_get_generic_trampoline_name (tramp_type);
		*info = mono_tramp_info_create (tramp_name, buf, code - buf, ji, unwind_ops);
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
	 * Pass the argument in t0, clobbering r21.
	 */
	tramp = mono_get_trampoline_code (tramp_type);

	buf = code = mono_mem_manager_code_reserve (mem_manager, buf_len);

	MINI_BEGIN_CODEGEN ();

	code = mono_loongarch_emit_imm64 (code, loongarch_t0, (guint64)arg1);
	code = mono_loongarch_emit_imm64 (code, loongarch_r21, (guint64)tramp);
	code = mono_loongarch_emit_jirl (code, loongarch_r21);

	g_assert ((code - buf) < buf_len);

	MINI_END_CODEGEN (buf, code - buf, MONO_PROFILER_CODE_BUFFER_SPECIFIC_TRAMPOLINE, mono_get_generic_trampoline_simple_name (tramp_type));

	if (code_len)
		*code_len = code - buf;

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

	code = mono_loongarch_emit_imm64 (code, loongarch_r21, (guint64)addr);
	loongarch_addid (code, loongarch_a0, loongarch_a0, MONO_ABI_SIZEOF (MonoObject));
	code = mono_loongarch_emit_jirl (code, loongarch_r21);

	g_assert ((code - start) <= size);

	MINI_END_CODEGEN (start, GPTRDIFF_TO_INT (code - start), MONO_PROFILER_CODE_BUFFER_UNBOX_TRAMPOLINE, m);

	return (gpointer)MINI_ADDR_TO_FTNPTR (start);
}

gpointer
mono_arch_get_static_rgctx_trampoline (MonoMemoryManager *mem_manager, gpointer arg, gpointer addr)
{
	guint8 *code, *start;
	guint32 buf_len = 36;

	start = code = mono_mem_manager_code_reserve (mem_manager, buf_len);

	MINI_BEGIN_CODEGEN ();

	code = mono_loongarch_emit_imm64 (code, MONO_ARCH_RGCTX_REG, (guint64)arg);
	code = mono_loongarch_emit_imm64 (code, loongarch_r21, (guint64)addr);
	code = mono_loongarch_emit_jirl (code, loongarch_r21);

	MINI_END_CODEGEN (start, code - start, MONO_PROFILER_CODE_BUFFER_GENERICS_TRAMPOLINE, NULL);

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

	/* The vtable/mrgtx is in a0 */
	g_assert (MONO_ARCH_VTABLE_REG == loongarch_a0);

	MINI_BEGIN_CODEGEN ();

	if (is_mrgctx) {
		/* get mrgctx ptr */
		loongarch_move (code, loongarch_t0, loongarch_a0);
	} else {
		/* load rgctx ptr from vtable */
		loongarch_ldd (code, loongarch_t0, loongarch_a0, MONO_STRUCT_OFFSET (MonoVTable, runtime_generic_context));
		/* is the rgctx ptr null? */
		/* if yes, jump to actual trampoline */
		rgctx_null_jumps [njumps ++] = code;
		loongarch_beqz (code, loongarch_t0, 0);
	}

	for (i = 0; i < depth; ++i) {
		/* load ptr to next array */
		if (is_mrgctx && i == 0) {
			loongarch_ldd (code, loongarch_t0, loongarch_t0, MONO_SIZEOF_METHOD_RUNTIME_GENERIC_CONTEXT);
		} else {
			loongarch_ldd (code, loongarch_t0, loongarch_t0, 0);
		}
		/* is the ptr null? */
		/* if yes, jump to actual trampoline */
		rgctx_null_jumps [njumps ++] = code;
		loongarch_beqz (code, loongarch_t0, 0);
	}

	/* fetch slot */
	loongarch_ldd (code, loongarch_t0, loongarch_t0, sizeof (target_mgreg_t) * (index + 1));
	/* is the slot null? */
	/* if yes, jump to actual trampoline */
	rgctx_null_jumps [njumps ++] = code;
	loongarch_beqz (code, loongarch_t0, 0);
	/* otherwise return, result is in t0 */
	loongarch_move (code, loongarch_a0, loongarch_t0);
	loongarch_jirl (code, 0, loongarch_ra, 0);

	g_assert (njumps <= depth + 2);
	for (i = 0; i < njumps; ++i)
		mono_loongarch64_patch (rgctx_null_jumps [i], code, MONO_R_LOONGARCH64_BZ);

	g_free (rgctx_null_jumps);

	/* Slowpath */

	/* Call mono_rgctx_lazy_fetch_trampoline (), passing in the slot as argument */
	/* The vtable/mrgctx is still in a0 */
	MonoMemoryManager *mem_manager = mini_get_default_mem_manager ();
	tramp = (guint8*)mono_arch_create_specific_trampoline (GUINT_TO_POINTER (slot), MONO_TRAMPOLINE_RGCTX_LAZY_FETCH, mem_manager, &code_len);
	code = mono_loongarch_emit_imm64 (code, loongarch_r21, (guint64)tramp);
	loongarch_jirl (code, 0, loongarch_r21, 0);

	g_assert (code - buf <= buf_size);

	MINI_END_CODEGEN (buf, code - buf, MONO_PROFILER_CODE_BUFFER_GENERICS_TRAMPOLINE, NULL);

	if (info) {
		char *name = mono_get_rgctx_fetch_trampoline_name (slot);
		*info = mono_tramp_info_create (name, buf, code - buf, ji, unwind_ops);
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

	mono_add_unwind_op_def_cfa (unwind_ops, code, buf, loongarch_sp, 0);

	MINI_BEGIN_CODEGEN ();

	// FIXME: Currently, we always go to the slow path.
	/* Load trampoline addr */
	loongarch_ldd (code, loongarch_r21, MONO_ARCH_RGCTX_REG, 8);
	/* The vtable/mrgctx is in a0 */
	g_assert (MONO_ARCH_VTABLE_REG == loongarch_a0);
	loongarch_jirl (code, 0, loongarch_r21, 0);

	g_assert (code - buf <= tramp_size);

	MINI_END_CODEGEN (buf, code - buf, MONO_PROFILER_CODE_BUFFER_GENERICS_TRAMPOLINE, NULL);

	if (info)
		*info = mono_tramp_info_create ("rgctx_fetch_trampoline_general", buf, code - buf, ji, unwind_ops);

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
	while (imm > 256) {
		loongarch_addid (code, loongarch_sp, loongarch_sp, -256);
		imm -= 256;
	}
	loongarch_addid (code, loongarch_sp, loongarch_sp, -imm);
	loongarch_std (code, loongarch_fp, loongarch_sp, 0);
	loongarch_std (code, loongarch_ra, loongarch_sp, 8);
	loongarch_ori (code, loongarch_fp, loongarch_sp, 0);

	/* Initialize a MonoContext structure on the stack */
	/* No need to save fregs */
	gregs_regset = ~((1 << loongarch_fp) | (1 << loongarch_sp));
	code = mono_loongarch_emit_store_regarray (code, gregs_regset, loongarch_fp, ctx_offset + G_STRUCT_OFFSET (MonoContext, regs));
	/* Save caller fp */
	loongarch_ldd (code, loongarch_t0, loongarch_fp, 0);
	loongarch_std (code, loongarch_t0, loongarch_fp, ctx_offset + G_STRUCT_OFFSET (MonoContext, regs) + (loongarch_fp * 8));
	/* Save caller sp */
	loongarch_move (code, loongarch_t0, loongarch_fp);
	imm = frame_size;
	while (imm > 256) {
		loongarch_addid (code, loongarch_t0, loongarch_t0, 256);
		imm -= 256;
	}
	loongarch_addid (code, loongarch_t0, loongarch_t0, imm);
	loongarch_std (code, loongarch_t0, loongarch_fp, ctx_offset + G_STRUCT_OFFSET (MonoContext, regs) + (loongarch_sp * 8));
	/* Save caller ip */
	loongarch_ldd (code, loongarch_t0, loongarch_fp, 8);
	loongarch_std (code, loongarch_t0, loongarch_fp, ctx_offset + G_STRUCT_OFFSET (MonoContext, pc));

	/* Call the single step/breakpoint function in sdb */
	/* Arg1 = ctx */
	loongarch_addid (code, loongarch_a0, loongarch_fp, ctx_offset);
	void (*addr) (MonoContext *ctx) = single_step ? mono_component_debugger ()->single_step_from_context : mono_component_debugger ()->breakpoint_from_context;
	code = mono_loongarch_emit_imm64 (code, loongarch_r21, (guint64)addr);
	loongarch_jirl (code, 1, loongarch_r21, 0);

	/* Restore ctx */
	/* Save fp/pc into the frame block */
	loongarch_ldd (code, loongarch_r21, loongarch_fp, ctx_offset + G_STRUCT_OFFSET (MonoContext, regs) + (loongarch_fp * 8));
	loongarch_std (code, loongarch_r21, loongarch_fp, 0);
	loongarch_ldd (code, loongarch_r21, loongarch_fp, ctx_offset + G_STRUCT_OFFSET (MonoContext, pc));
	loongarch_std (code, loongarch_r21, loongarch_fp, 8);
	gregs_regset = ~((1 << loongarch_fp) | (1 << loongarch_sp));

	code = mono_loongarch_emit_load_regarray (code, gregs_regset, loongarch_fp, ctx_offset + G_STRUCT_OFFSET (MonoContext, regs));
	code = mono_loongarch64_emit_destroy_frame (code, frame_size);

	loongarch_jirl (code, 0, loongarch_ra, 0);

	g_assert (code - buf <= tramp_size);

	MINI_END_CODEGEN (buf, code - buf, MONO_PROFILER_CODE_BUFFER_HELPER, NULL);

	const char *tramp_name = single_step ? "sdb_single_step_trampoline" : "sdb_breakpoint_trampoline";
	*info = mono_tramp_info_create (tramp_name, buf, code - buf, ji, unwind_ops);

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
	guint8 *label_start_copy;
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

	loongarch_addid (code, loongarch_sp, loongarch_sp, -framesize);
	loongarch_std (code, loongarch_fp, loongarch_sp, 0);
	loongarch_std (code, loongarch_ra, loongarch_sp, 8);
	loongarch_ori (code, loongarch_fp, loongarch_sp, 0);

	/* save CallContext* onto stack */
	loongarch_std (code, loongarch_a1, loongarch_fp, off_methodargs);

	/* save target address onto stack */
	loongarch_std (code, loongarch_a0, loongarch_fp, off_targetaddr);

	/* allocate the stack space necessary for the call */
	loongarch_ldw (code, loongarch_a0, loongarch_a1, MONO_STRUCT_OFFSET (CallContext, stack_size));
	loongarch_ori (code, loongarch_r21, loongarch_sp, 0);
	loongarch_subd (code, loongarch_r21, loongarch_r21, loongarch_a0);
	loongarch_ori (code, loongarch_sp, loongarch_r21, 0);

	/* copy stack from the CallContext, r21 = dest, t0 = source */
	loongarch_ori (code, loongarch_r21, loongarch_sp, 0);
	loongarch_ldd (code, loongarch_t0, loongarch_a1, MONO_STRUCT_OFFSET (CallContext, stack));

	label_start_copy = code;

	loongarch_beqz(code, loongarch_a0, 0);
	loongarch_ldd (code, loongarch_a2, loongarch_t0, 0);
	loongarch_std (code, loongarch_a2, loongarch_r21, 0);
	loongarch_addid (code, loongarch_r21, loongarch_r21, sizeof (host_mgreg_t));
	loongarch_addid (code, loongarch_t0, loongarch_t0, sizeof (host_mgreg_t));
	loongarch_addid (code, loongarch_a0, loongarch_a0, -sizeof (host_mgreg_t));
	loongarch_b (code, (label_start_copy - code)>>2);
	mono_loongarch64_patch(label_start_copy, code, MONO_R_LOONGARCH64_BZ);

	/* Load CallContext* into r21 */
	loongarch_ldd (code, loongarch_r21, loongarch_fp, off_methodargs);

	/* set all general purpose registers from CallContext */
	for (i = 4; i < PARAM_REGS + 1; i++)
		loongarch_ldd (code, i, loongarch_r21, MONO_STRUCT_OFFSET (CallContext, gregs) + i * sizeof (host_mgreg_t));

	/* set all floating registers from CallContext  */
	for (i = 0; i < FP_PARAM_REGS; i++)
		loongarch_fldd (code, i, loongarch_r21, MONO_STRUCT_OFFSET (CallContext, fregs) + i * sizeof (double));

	/* load target addr */
	loongarch_ldd (code, loongarch_r21, loongarch_fp, off_targetaddr);

	/* call into native function */
	loongarch_jirl (code, 1, loongarch_r21, 0);

	/* load CallContext* */
	loongarch_ldd (code, loongarch_r21, loongarch_fp, off_methodargs);

	/* set all general purpose registers to CallContext */
	for (i = 4; i < PARAM_REGS; i++)
		loongarch_std (code, i, loongarch_r21, MONO_STRUCT_OFFSET (CallContext, gregs) + i * sizeof (host_mgreg_t));

	/* set all floating registers to CallContext  */
	for (i = 0; i < FP_PARAM_REGS; i++)
		loongarch_fstd (code, i, loongarch_r21, MONO_STRUCT_OFFSET (CallContext, fregs) + i * sizeof (double));

	loongarch_ori (code, loongarch_sp, loongarch_fp, 0);
	loongarch_ldd (code, loongarch_fp, loongarch_sp, 0);
	loongarch_ldd (code, loongarch_ra, loongarch_sp, 8);
	loongarch_addid (code, loongarch_sp, loongarch_sp, framesize);
	loongarch_jirl (code, 0, loongarch_ra, 0);

	g_assert (code - start < buf_len);

	MINI_END_CODEGEN (start, code - start, MONO_PROFILER_CODE_BUFFER_HELPER, NULL);

	if (info)
		*info = mono_tramp_info_create ("interp_to_native_trampoline", start, code - start, ji, unwind_ops);

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

	/* Allocate frame (FP + RA + CallContext) */
	offset = 2 * sizeof (host_mgreg_t);
	ccontext_offset = offset;
	offset += sizeof (CallContext);
	framesize = ALIGN_TO (offset, MONO_ARCH_FRAME_ALIGNMENT);

	MINI_BEGIN_CODEGEN ();

	mono_add_unwind_op_def_cfa (unwind_ops, code, start, loongarch_sp, 0);

	loongarch_addid (code, loongarch_sp, loongarch_sp, -framesize);
	mono_add_unwind_op_def_cfa_offset (unwind_ops, code, start, framesize);

	loongarch_std (code, loongarch_fp, loongarch_sp, 0);
	loongarch_std (code, loongarch_ra, loongarch_sp, 8);
	mono_add_unwind_op_offset (unwind_ops, code, start, loongarch_fp, -framesize);
	mono_add_unwind_op_offset (unwind_ops, code, start, loongarch_ra, -framesize + 8);

	loongarch_ori (code, loongarch_fp, loongarch_sp, 0);
	mono_add_unwind_op_def_cfa_reg (unwind_ops, code, start, loongarch_fp);

	/* save all general purpose registers into the CallContext */
	for (i = 0; i < PARAM_REGS + 1; i++)
		loongarch_std (code, i, loongarch_fp, ccontext_offset + MONO_STRUCT_OFFSET (CallContext, gregs) + i * sizeof (host_mgreg_t));

	/* save all floating registers into the CallContext */
	for (i = 0; i < FP_PARAM_REGS; i++)
		loongarch_fstd (code, i, loongarch_fp, ccontext_offset + MONO_STRUCT_OFFSET (CallContext, fregs) + i * sizeof (double));

	/* set the stack pointer to the value at call site */
	loongarch_addid (code, loongarch_a0, loongarch_fp, framesize);
	loongarch_std (code, loongarch_a0, loongarch_fp, ccontext_offset + MONO_STRUCT_OFFSET (CallContext, stack));

	/* call interp_entry with the ccontext and rmethod as arguments */
	loongarch_addid (code, loongarch_a0, loongarch_fp, ccontext_offset);
	loongarch_ldd (code, loongarch_a1, MONO_ARCH_RGCTX_REG, MONO_STRUCT_OFFSET (MonoFtnDesc, arg));
	loongarch_ldd (code, loongarch_r21, MONO_ARCH_RGCTX_REG, MONO_STRUCT_OFFSET (MonoFtnDesc, addr));
	loongarch_jirl (code, 1, loongarch_r21, 0);

	/* load the return values from the context */
	for (i = 0; i < PARAM_REGS; i++)
		loongarch_ldd (code, i, loongarch_fp, ccontext_offset + MONO_STRUCT_OFFSET (CallContext, gregs) + i * sizeof (host_mgreg_t));

	for (i = 0; i < FP_PARAM_REGS; i++)
		loongarch_fldd (code, i, loongarch_fp, ccontext_offset + MONO_STRUCT_OFFSET (CallContext, fregs) + i * sizeof (double));

	/* reset stack and return */
	loongarch_ldd (code, loongarch_fp, loongarch_sp, 0);
	loongarch_ldd (code, loongarch_ra, loongarch_sp, 8);
	loongarch_addid (code, loongarch_sp, loongarch_sp, framesize);
	loongarch_jirl (code, 0, loongarch_ra, 0);

	g_assert (code - start < buf_len);

	MINI_END_CODEGEN (start, code - start, MONO_PROFILER_CODE_BUFFER_EXCEPTION_HANDLING, NULL);

	if (info)
		*info = mono_tramp_info_create ("native_to_interp_trampoline", start, code - start, ji, unwind_ops);

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
