/**
 * \file
 * exception support for ARM64
 *
 * Copyright 2013 Xamarin Inc
 *
 * Based on exceptions-arm.c:
 *
 * Authors:
 *   Dietmar Maurer (dietmar@ximian.com)
 *   Paolo Molaro (lupus@ximian.com)
 *
 * (C) 2001 Ximian, Inc.
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */

#include "mini.h"
#include "mini-runtime.h"
#include "aot-runtime.h"

#include <mono/arch/arm64/arm64-codegen.h>
#include <mono/metadata/abi-details.h>

#ifndef DISABLE_JIT

gpointer
mono_arch_get_restore_context (MonoTrampInfo **info, gboolean aot)
{
	guint8 *start, *code;
	MonoJumpInfo *ji = NULL;
	GSList *unwind_ops = NULL;
	int i, ctx_reg, size;
	guint8 *labels [16];

	size = 256;
	code = start = mono_global_codeman_reserve (size);

	arm_movx (code, ARMREG_IP0, ARMREG_R0);
	ctx_reg = ARMREG_IP0;

	/* Restore fregs */
	arm_ldrx (code, ARMREG_IP1, ctx_reg, MONO_STRUCT_OFFSET (MonoContext, has_fregs));
	labels [0] = code;
	arm_cbzx (code, ARMREG_IP1, 0);
	for (i = 0; i < 32; ++i)
		arm_ldrfpx (code, i, ctx_reg, MONO_STRUCT_OFFSET (MonoContext, fregs) + (i * sizeof (MonoContextSimdReg)));
	mono_arm_patch (labels [0], code, MONO_R_ARM64_CBZ);
	/* Restore gregs */
	// FIXME: Restore less registers
	// FIXME: fp should be restored later
	code = mono_arm_emit_load_regarray (code, 0xffffffff & ~(1 << ctx_reg) & ~(1 << ARMREG_SP), ctx_reg, MONO_STRUCT_OFFSET (MonoContext, regs));
	/* ip0/ip1 doesn't need to be restored */
	/* ip1 = pc */
	arm_ldrx (code, ARMREG_IP1, ctx_reg, MONO_STRUCT_OFFSET (MonoContext, pc));
	/* ip0 = sp */
	arm_ldrx (code, ARMREG_IP0, ctx_reg, MONO_STRUCT_OFFSET (MonoContext, regs) + (ARMREG_SP * 8));
	/* Restore sp, ctx is no longer valid */
	arm_movspx (code, ARMREG_SP, ARMREG_IP0); 
	/* Branch to pc */
	arm_brx (code, ARMREG_IP1);
	/* Not reached */
	arm_brk (code, 0);

	g_assert ((code - start) < size);
	mono_arch_flush_icache (start, code - start);
	MONO_PROFILER_RAISE (jit_code_buffer, (start, code - start, MONO_PROFILER_CODE_BUFFER_EXCEPTION_HANDLING, NULL));

	if (info)
		*info = mono_tramp_info_create ("restore_context", start, code - start, ji, unwind_ops);

	return start;
}

gpointer
mono_arch_get_call_filter (MonoTrampInfo **info, gboolean aot)
{
	guint8 *code;
	guint8* start;
	int i, size, offset, gregs_offset, fregs_offset, ctx_offset, num_fregs, frame_size;
	MonoJumpInfo *ji = NULL;
	GSList *unwind_ops = NULL;
	guint8 *labels [16];

	size = 512;
	start = code = mono_global_codeman_reserve (size);

	/* Compute stack frame size and offsets */
	offset = 0;
	/* frame block */
	offset += 2 * 8;
	/* gregs */
	gregs_offset = offset;
	offset += 32 * 8;
	/* fregs */
	num_fregs = 8;
	fregs_offset = offset;
	offset += num_fregs * 8;
	ctx_offset = offset;
	ctx_offset += 8;
	frame_size = ALIGN_TO (offset, MONO_ARCH_FRAME_ALIGNMENT);

	/*
	 * We are being called from C code, ctx is in r0, the address to call is in r1.
	 * We need to save state, restore ctx, make the call, then restore the previous state,
	 * returning the value returned by the call.
	 */

	/* Setup a frame */
	arm_stpx_pre (code, ARMREG_FP, ARMREG_LR, ARMREG_SP, -frame_size);
	arm_movspx (code, ARMREG_FP, ARMREG_SP);

	/* Save ctx */
	arm_strx (code, ARMREG_R0, ARMREG_FP, ctx_offset);
	/* Save gregs */
	code = mono_arm_emit_store_regarray (code, MONO_ARCH_CALLEE_SAVED_REGS | (1 << ARMREG_FP), ARMREG_FP, gregs_offset);
	/* Save fregs */
	for (i = 0; i < num_fregs; ++i)
		arm_strfpx (code, ARMREG_D8 + i, ARMREG_FP, fregs_offset + (i * 8));

	/* Load regs from ctx */
	code = mono_arm_emit_load_regarray (code, MONO_ARCH_CALLEE_SAVED_REGS, ARMREG_R0, MONO_STRUCT_OFFSET (MonoContext, regs));
	/* Load fregs */
	arm_ldrx (code, ARMREG_IP0, ARMREG_R0, MONO_STRUCT_OFFSET (MonoContext, has_fregs));
	labels [0] = code;
	arm_cbzx (code, ARMREG_IP0, 0);
	for (i = 0; i < num_fregs; ++i)
		arm_ldrfpx (code, ARMREG_D8 + i, ARMREG_R0, MONO_STRUCT_OFFSET (MonoContext, fregs) + ((i + 8) * sizeof (MonoContextSimdReg)));
	mono_arm_patch (labels [0], code, MONO_R_ARM64_CBZ);
	/* Load fp */
	arm_ldrx (code, ARMREG_FP, ARMREG_R0, MONO_STRUCT_OFFSET (MonoContext, regs) + (ARMREG_FP * 8));

	/* Make the call */
	arm_blrx (code, ARMREG_R1);
	/* For filters, the result is in R0 */

	/* Restore fp */
	arm_ldrx (code, ARMREG_FP, ARMREG_SP, gregs_offset + (ARMREG_FP * 8));
	/* Load ctx */
	arm_ldrx (code, ARMREG_IP0, ARMREG_FP, ctx_offset);
	/* Save registers back to ctx */
	/* This isn't strictly neccessary since we don't allocate variables used in eh clauses to registers */
	code = mono_arm_emit_store_regarray (code, MONO_ARCH_CALLEE_SAVED_REGS, ARMREG_IP0, MONO_STRUCT_OFFSET (MonoContext, regs));

	/* Restore regs */
	code = mono_arm_emit_load_regarray (code, MONO_ARCH_CALLEE_SAVED_REGS, ARMREG_FP, gregs_offset);
	/* Restore fregs */
	for (i = 0; i < num_fregs; ++i)
		arm_ldrfpx (code, ARMREG_D8 + i, ARMREG_FP, fregs_offset + (i * 8));
	/* Destroy frame */
	code = mono_arm_emit_destroy_frame (code, frame_size, (1 << ARMREG_IP0));
	arm_retx (code, ARMREG_LR);

	g_assert ((code - start) < size);
	mono_arch_flush_icache (start, code - start);
	MONO_PROFILER_RAISE (jit_code_buffer, (start, code - start, MONO_PROFILER_CODE_BUFFER_EXCEPTION_HANDLING, NULL));

	if (info)
		*info = mono_tramp_info_create ("call_filter", start, code - start, ji, unwind_ops);

	return start;
}

static gpointer 
get_throw_trampoline (int size, gboolean corlib, gboolean rethrow, gboolean llvm, gboolean resume_unwind, const char *tramp_name, MonoTrampInfo **info, gboolean aot, gboolean preserve_ips)
{
	guint8 *start, *code;
	MonoJumpInfo *ji = NULL;
	GSList *unwind_ops = NULL;
	int i, offset, gregs_offset, fregs_offset, frame_size, num_fregs;

	code = start = mono_global_codeman_reserve (size);

	/* We are being called by JITted code, the exception object/type token is in R0 */

	/* Compute stack frame size and offsets */
	offset = 0;
	/* frame block */
	offset += 2 * 8;
	/* gregs */
	gregs_offset = offset;
	offset += 32 * 8;
	/* fregs */
	num_fregs = 8;
	fregs_offset = offset;
	offset += num_fregs * 8;
	frame_size = ALIGN_TO (offset, MONO_ARCH_FRAME_ALIGNMENT);

	/* Setup a frame */
	arm_stpx_pre (code, ARMREG_FP, ARMREG_LR, ARMREG_SP, -frame_size);
	arm_movspx (code, ARMREG_FP, ARMREG_SP);

	/* Save gregs */
	code = mono_arm_emit_store_regarray (code, 0xffffffff, ARMREG_FP, gregs_offset);
	if (corlib && !llvm)
		/* The real LR is in R1 */
		arm_strx (code, ARMREG_R1, ARMREG_FP, gregs_offset + (ARMREG_LR * 8));
	/* Save fp/sp */
	arm_ldrx (code, ARMREG_IP0, ARMREG_FP, 0);
	arm_strx (code, ARMREG_IP0, ARMREG_FP, gregs_offset + (ARMREG_FP * 8));
	arm_addx_imm (code, ARMREG_IP0, ARMREG_FP, frame_size);
	arm_strx (code, ARMREG_IP0, ARMREG_FP, gregs_offset + (ARMREG_SP * 8));	
	/* Save fregs */
	for (i = 0; i < num_fregs; ++i)
		arm_strfpx (code, ARMREG_D8 + i, ARMREG_FP, fregs_offset + (i * 8));

	/* Call the C trampoline function */
	/* Arg1 =  exception object/type token */
	arm_movx (code, ARMREG_R0, ARMREG_R0);
	/* Arg2 = caller ip */
	if (corlib) {
		if (llvm)
			arm_ldrx (code, ARMREG_R1, ARMREG_FP, gregs_offset + (ARMREG_LR * 8));
		else
			arm_movx (code, ARMREG_R1, ARMREG_R1);
	} else {
		arm_ldrx (code, ARMREG_R1, ARMREG_FP, 8);
	}
	/* Arg 3 = gregs */
	arm_addx_imm (code, ARMREG_R2, ARMREG_FP, gregs_offset);
	/* Arg 4 = fregs */
	arm_addx_imm (code, ARMREG_R3, ARMREG_FP, fregs_offset);
	/* Arg 5 = corlib */
	arm_movzx (code, ARMREG_R4, corlib ? 1 : 0, 0);
	/* Arg 6 = rethrow */
	arm_movzx (code, ARMREG_R5, rethrow ? 1 : 0, 0);
	if (!resume_unwind) {
		/* Arg 7 = preserve_ips */
		arm_movzx (code, ARMREG_R6, preserve_ips ? 1 : 0, 0);
	}

	/* Call the function */
	if (aot) {
		const char *icall_name;

		if (resume_unwind)
			icall_name = "mono_arm_resume_unwind";
		else
			icall_name = "mono_arm_throw_exception";

		code = mono_arm_emit_aotconst (&ji, code, start, ARMREG_LR, MONO_PATCH_INFO_JIT_ICALL_ADDR, icall_name);
	} else {
		gpointer icall_func;

		if (resume_unwind)
			icall_func = (gpointer)mono_arm_resume_unwind;
		else
			icall_func = (gpointer)mono_arm_throw_exception;

		code = mono_arm_emit_imm64 (code, ARMREG_LR, (guint64)icall_func);
	}
	arm_blrx (code, ARMREG_LR);
	/* This shouldn't return */
	arm_brk (code, 0x0);

	g_assert ((code - start) < size);
	mono_arch_flush_icache (start, code - start);
	MONO_PROFILER_RAISE (jit_code_buffer, (start, code - start, MONO_PROFILER_CODE_BUFFER_EXCEPTION_HANDLING, NULL));

	if (info)
		*info = mono_tramp_info_create (tramp_name, start, code - start, ji, unwind_ops);

	return start;
}

gpointer 
mono_arch_get_throw_exception (MonoTrampInfo **info, gboolean aot)
{
	return get_throw_trampoline (256, FALSE, FALSE, FALSE, FALSE, "throw_exception", info, aot, FALSE);
}

gpointer
mono_arch_get_rethrow_exception (MonoTrampInfo **info, gboolean aot)
{
	return get_throw_trampoline (256, FALSE, TRUE, FALSE, FALSE, "rethrow_exception", info, aot, FALSE);
}

gpointer
mono_arch_get_rethrow_preserve_exception (MonoTrampInfo **info, gboolean aot)
{
	return get_throw_trampoline (256, FALSE, TRUE, FALSE, FALSE, "rethrow_preserve_exception", info, aot, TRUE);
}

gpointer 
mono_arch_get_throw_corlib_exception (MonoTrampInfo **info, gboolean aot)
{
	return get_throw_trampoline (256, TRUE, FALSE, FALSE, FALSE, "throw_corlib_exception", info, aot, FALSE);
}

GSList*
mono_arm_get_exception_trampolines (gboolean aot)
{
	MonoTrampInfo *info;
	GSList *tramps = NULL;

	// FIXME Macroize.

	/* LLVM uses the normal trampolines, but with a different name */
	get_throw_trampoline (256, TRUE, FALSE, FALSE, FALSE, "llvm_throw_corlib_exception_trampoline", &info, aot, FALSE);
	info->jit_icall_info = &mono_get_jit_icall_info ()->mono_llvm_throw_corlib_exception_trampoline;
	tramps = g_slist_prepend (tramps, info);

	get_throw_trampoline (256, TRUE, FALSE, TRUE, FALSE, "llvm_throw_corlib_exception_abs_trampoline", &info, aot, FALSE);
	info->jit_icall_info = &mono_get_jit_icall_info ()->mono_llvm_throw_corlib_exception_abs_trampoline;
	tramps = g_slist_prepend (tramps, info);

	get_throw_trampoline (256, FALSE, FALSE, FALSE, TRUE, "llvm_resume_unwind_trampoline", &info, aot, FALSE);
	info->jit_icall_info = &mono_get_jit_icall_info ()->mono_llvm_resume_unwind_trampoline;
	tramps = g_slist_prepend (tramps, info);

	return tramps;
}

#else

GSList*
mono_arm_get_exception_trampolines (gboolean aot)
{
	g_assert_not_reached ();
	return NULL;
}

#endif /* DISABLE_JIT */

void
mono_arch_exceptions_init (void)
{
	gpointer tramp;
	GSList *tramps, *l;

	if (mono_aot_only) {

		// FIXME Macroize.

		tramp = mono_aot_get_trampoline ("llvm_throw_corlib_exception_trampoline");
		mono_register_jit_icall_info (&mono_get_jit_icall_info ()->mono_llvm_throw_corlib_exception_trampoline, tramp, "llvm_throw_corlib_exception_trampoline", NULL, TRUE, NULL);

		tramp = mono_aot_get_trampoline ("llvm_throw_corlib_exception_abs_trampoline");
		mono_register_jit_icall_info (&mono_get_jit_icall_info ()->mono_llvm_throw_corlib_exception_abs_trampoline, tramp, "llvm_throw_corlib_exception_abs_trampoline", NULL, TRUE, NULL);

		tramp = mono_aot_get_trampoline ("llvm_resume_unwind_trampoline");
		mono_register_jit_icall_info (&mono_get_jit_icall_info ()->mono_llvm_resume_unwind_trampoline, tramp, "llvm_resume_unwind_trampoline", NULL, TRUE, NULL);

	} else {
		tramps = mono_arm_get_exception_trampolines (FALSE);
		for (l = tramps; l; l = l->next) {
			MonoTrampInfo *info = (MonoTrampInfo*)l->data;
			mono_register_jit_icall_info (info->jit_icall_info, info->code, g_strdup (info->name), NULL, TRUE, NULL);
			mono_tramp_info_register (info, NULL);
		}
		g_slist_free (tramps);
	}
}

/*
 * mono_arm_throw_exception:
 *
 *   This function is called by the exception trampolines.
 * FP_REGS points to the 8 callee saved fp regs.
 */
void
mono_arm_throw_exception (gpointer arg, host_mgreg_t pc, host_mgreg_t *int_regs, gdouble *fp_regs, gboolean corlib, gboolean rethrow, gboolean preserve_ips)
{
	ERROR_DECL (error);
	MonoContext ctx;
	MonoObject *exc = NULL;
	guint32 ex_token_index, ex_token;

	if (!corlib)
		exc = (MonoObject*)arg;
	else {
		ex_token_index = (guint64)arg;
		ex_token = MONO_TOKEN_TYPE_DEF | ex_token_index;
		exc = (MonoObject*)mono_exception_from_token (mono_defaults.corlib, ex_token);
	}

	/* Adjust pc so it points into the call instruction */
	pc -= 4;

	/* Initialize a ctx based on the arguments */
	memset (&ctx, 0, sizeof (MonoContext));
	memcpy (&(ctx.regs [0]), int_regs, sizeof (host_mgreg_t) * 32);
	for (int i = 0; i < 8; i++)
		*((gdouble*)&ctx.fregs [ARMREG_D8 + i]) = fp_regs [i];
	ctx.has_fregs = 1;
	ctx.pc = pc;

	if (mono_object_isinst_checked (exc, mono_defaults.exception_class, error)) {
		MonoException *mono_ex = (MonoException*)exc;
		if (!rethrow) {
			mono_ex->stack_trace = NULL;
			mono_ex->trace_ips = NULL;
		} else if (preserve_ips) {
			mono_ex->caught_in_unmanaged = TRUE;
		}
	}
	mono_error_assert_ok (error);

	mono_handle_exception (&ctx, exc);

	mono_restore_context (&ctx);
}

void
mono_arm_resume_unwind (gpointer arg, host_mgreg_t pc, host_mgreg_t *int_regs, gdouble *fp_regs, gboolean corlib, gboolean rethrow)
{
	MonoContext ctx;

	/* Adjust pc so it points into the call instruction */
	pc -= 4;

	/* Initialize a ctx based on the arguments */
	memset (&ctx, 0, sizeof (MonoContext));
	memcpy (&(ctx.regs [0]), int_regs, sizeof (host_mgreg_t) * 32);
	for (int i = 0; i < 8; i++)
		*((gdouble*)&ctx.fregs [ARMREG_D8 + i]) = fp_regs [i];
	ctx.has_fregs = 1;
	ctx.pc = pc;

	mono_resume_unwind (&ctx);
}

/* 
 * mono_arch_unwind_frame:
 *
 * See exceptions-amd64.c for docs;
 */
gboolean
mono_arch_unwind_frame (MonoDomain *domain, MonoJitTlsData *jit_tls, 
							 MonoJitInfo *ji, MonoContext *ctx, 
							 MonoContext *new_ctx, MonoLMF **lmf,
							 host_mgreg_t **save_locations,
							 StackFrameInfo *frame)
{
	gpointer ip = MONO_CONTEXT_GET_IP (ctx);

	memset (frame, 0, sizeof (StackFrameInfo));
	frame->ji = ji;

	*new_ctx = *ctx;

	if (ji != NULL) {
		host_mgreg_t regs [MONO_MAX_IREGS + 8 + 1];
		guint8 *cfa;
		guint32 unwind_info_len;
		guint8 *unwind_info;


		if (ji->is_trampoline)
			frame->type = FRAME_TYPE_TRAMPOLINE;
		else
			frame->type = FRAME_TYPE_MANAGED;

		unwind_info = mono_jinfo_get_unwind_info (ji, &unwind_info_len);

		memcpy (regs, &new_ctx->regs, sizeof (host_mgreg_t) * 32);
		/* v8..v15 are callee saved */
		for (int i = 0; i < 8; i++)
			(regs + MONO_MAX_IREGS) [i] = *((host_mgreg_t*)&new_ctx->fregs [8 + i]);

		gboolean success = mono_unwind_frame (unwind_info, unwind_info_len, (guint8*)ji->code_start,
						   (guint8*)ji->code_start + ji->code_size,
						   (guint8*)ip, NULL, regs, MONO_MAX_IREGS + 8,
						   save_locations, MONO_MAX_IREGS, (guint8**)&cfa);

		if (!success)
			return FALSE;

		memcpy (&new_ctx->regs, regs, sizeof (host_mgreg_t) * 32);
		for (int i = 0; i < 8; i++)
			*((host_mgreg_t*)&new_ctx->fregs [8 + i]) = (regs + MONO_MAX_IREGS) [i];

		new_ctx->pc = regs [ARMREG_LR];
		new_ctx->regs [ARMREG_SP] = (host_mgreg_t)(gsize)cfa;

		if (*lmf && (*lmf)->gregs [MONO_ARCH_LMF_REG_SP] && (MONO_CONTEXT_GET_SP (ctx) >= (gpointer)(*lmf)->gregs [MONO_ARCH_LMF_REG_SP])) {
			/* remove any unused lmf */
			*lmf = (MonoLMF*)(((gsize)(*lmf)->previous_lmf) & ~3);
		}

		/* we substract 1, so that the IP points into the call instruction */
		new_ctx->pc--;

		return TRUE;
	} else if (*lmf) {
		g_assert ((((guint64)(*lmf)->previous_lmf) & 2) == 0);

		frame->type = FRAME_TYPE_MANAGED_TO_NATIVE;

		ji = mini_jit_info_table_find (domain, (gpointer)(*lmf)->pc, NULL);
		if (!ji)
			return FALSE;

		g_assert (MONO_ARCH_LMF_REGS == ((0x3ff << 19) | (1 << ARMREG_FP) | (1 << ARMREG_SP)));
		memcpy (&new_ctx->regs [ARMREG_R19], &(*lmf)->gregs [0], sizeof (host_mgreg_t) * 10);
		new_ctx->regs [ARMREG_FP] = (*lmf)->gregs [MONO_ARCH_LMF_REG_FP];
		new_ctx->regs [ARMREG_SP] = (*lmf)->gregs [MONO_ARCH_LMF_REG_SP];
		new_ctx->pc = (*lmf)->pc;

		/* we substract 1, so that the IP points into the call instruction */
		new_ctx->pc--;

		*lmf = (MonoLMF*)(((gsize)(*lmf)->previous_lmf) & ~3);

		return TRUE;
	}

	return FALSE;
}

/*
 * handle_exception:
 *
 *   Called by resuming from a signal handler.
 */
static void
handle_signal_exception (gpointer obj)
{
	MonoJitTlsData *jit_tls = mono_tls_get_jit_tls ();
	MonoContext ctx;

	memcpy (&ctx, &jit_tls->ex_ctx, sizeof (MonoContext));

	mono_handle_exception (&ctx, (MonoObject*)obj);

	mono_restore_context (&ctx);
}

/*
 * This is the function called from the signal handler
 */
gboolean
mono_arch_handle_exception (void *ctx, gpointer obj)
{
#if defined(MONO_CROSS_COMPILE)
	g_assert_not_reached ();
#else
	MonoJitTlsData *jit_tls;
	void *sigctx = ctx;

	/*
	 * Resume into the normal stack and handle the exception there.
	 */
	jit_tls = mono_tls_get_jit_tls ();

	/* Pass the ctx parameter in TLS */
	mono_sigctx_to_monoctx (sigctx, &jit_tls->ex_ctx);
	/* The others in registers */
	UCONTEXT_REG_R0 (sigctx) = (gsize)obj;

	UCONTEXT_REG_PC (sigctx) = (gsize)handle_signal_exception;
	UCONTEXT_REG_SP (sigctx) = UCONTEXT_REG_SP (sigctx) - MONO_ARCH_REDZONE_SIZE;
#endif

	return TRUE;
}

gpointer
mono_arch_ip_from_context (void *sigctx)
{
#ifdef MONO_CROSS_COMPILE
	g_assert_not_reached ();
	return NULL;
#else
	return (gpointer)UCONTEXT_REG_PC (sigctx);
#endif
}

void
mono_arch_setup_async_callback (MonoContext *ctx, void (*async_cb)(void *fun), gpointer user_data)
{
	host_mgreg_t sp = (host_mgreg_t)MONO_CONTEXT_GET_SP (ctx);

	// FIXME:
	g_assert (!user_data);

	/* Allocate a stack frame */
	sp -= 32;
	MONO_CONTEXT_SET_SP (ctx, sp);

	mono_arch_setup_resume_sighandler_ctx (ctx, (gpointer)async_cb);
}

/*
 * mono_arch_setup_resume_sighandler_ctx:
 *
 *   Setup CTX so execution continues at FUNC.
 */
void
mono_arch_setup_resume_sighandler_ctx (MonoContext *ctx, gpointer func)
{
	MONO_CONTEXT_SET_IP (ctx,func);
}

void
mono_arch_undo_ip_adjustment (MonoContext *ctx)
{
	ctx->pc++;
}

void
mono_arch_do_ip_adjustment (MonoContext *ctx)
{
	ctx->pc--;
}
