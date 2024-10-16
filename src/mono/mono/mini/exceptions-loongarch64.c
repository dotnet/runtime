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

#include <config.h>
#include <glib.h>
#include <signal.h>
#include <string.h>

#include <mono/arch/loongarch64/loongarch64-codegen.h>
#include <mono/metadata/appdomain.h>
#include <mono/metadata/tabledefs.h>
#include <mono/metadata/threads.h>
#include <mono/metadata/debug-helpers.h>
#include <mono/metadata/exception.h>
#include <mono/metadata/mono-debug.h>
#include <mono/metadata/tokentype.h>
#include "mini.h"
#include "mini-loongarch64.h"
#include "mini-runtime.h"
#include "aot-runtime.h"
#include "mono/utils/mono-tls-inline.h"

#define GENERIC_EXCEPTION_SIZE 256

#ifndef DISABLE_JIT
/*
 * mono_arch_get_restore_context:
 *
 * Returns a pointer to a method which restores a previously saved MonoContext.
 * The first argument in a0 is the pointer to the MonoContext.
 */
gpointer
mono_arch_get_restore_context (MonoTrampInfo **info, gboolean aot)
{
	int i , size;
	guint8 *code , *start = NULL;
	MonoJumpInfo *ji = NULL;
	GSList *unwind_ops = NULL;
	static int inited = 0;
	guint32 iregs_to_restore;

	g_assert (!aot);
	if (info)
		*info = NULL;

	if (inited)
		return start;
	inited = 1;
	size = 216;
	code = start = mono_global_codeman_reserve (size);

	MINI_BEGIN_CODEGEN ();
	loongarch_or (code, loongarch_r21, loongarch_zero, loongarch_a0);

	iregs_to_restore = (MONO_ARCH_CALLEE_SAVED_REGS \
			    | (1 << loongarch_sp) | (1 << loongarch_ra));
	for (i = 0; i < MONO_MAX_IREGS; ++i) {
		if (i != loongarch_zero && i != loongarch_r21)
			loongarch_ldd (code, i, loongarch_r21, G_STRUCT_OFFSET (MonoContext, regs [i]));
	}

	/* Get the address to return to */
	loongarch_ldd (code, loongarch_r21, loongarch_r21, G_STRUCT_OFFSET (MonoContext, pc));

	/* jump to the saved IP */
	loongarch_jirl (code, 0, loongarch_r21, 0);

	/* never reached */
	loongarch_break (code, 0xd);

	g_assert ((code - start) < size);
	MINI_END_CODEGEN (start, GPTRDIFF_TO_INT (code - start), MONO_PROFILER_CODE_BUFFER_EXCEPTION_HANDLING, NULL);
	if (info)
		*info = mono_tramp_info_create ("restore_context", start, GPTRDIFF_TO_UINT32 (code - start), ji, unwind_ops);
	return MINI_ADDR_TO_FTNPTR (start);
}

/*
 * mono_arch_get_call_filter:
 *
 * Returns a pointer to a method which calls an exception filter. We
 * also use this function to call finally handlers (we pass NULL as
 * @exc object in this case).
 *
 * This function is invoked as
 *	call_handler (MonoContext *ctx, handler)
 *
 * Where 'handler' is a function to be invoked as:
 *	handler (void)
 */
gpointer
mono_arch_get_call_filter (MonoTrampInfo **info, gboolean aot)
{
	static int inited = 0;
	int size;
	guint8 *code , *start = NULL;
	MonoJumpInfo *ji = NULL;
	GSList *unwind_ops = NULL;
	int alloc_size;
	int offset;

	g_assert (!aot);
	if (info)
		*info = NULL;

	if (inited)
		return start;

	inited = 1;
	size = 512;
	code = start = mono_global_codeman_reserve (size);

	MINI_BEGIN_CODEGEN ();
	alloc_size = 112;
	g_assert ((alloc_size & (LOONGARCH_STACK_ALIGNMENT - 1)) == 0);

	loongarch_addid (code, loongarch_sp, loongarch_sp, -alloc_size);
	loongarch_std (code, loongarch_ra, loongarch_sp, alloc_size - 8);

	/* Save global registers on stack (s0 - s8) */
	offset = 16;
	loongarch_std (code, loongarch_s0, loongarch_sp, offset); offset += IREG_SIZE;
	loongarch_std (code, loongarch_s1, loongarch_sp, offset); offset += IREG_SIZE;
	loongarch_std (code, loongarch_s2, loongarch_sp, offset); offset += IREG_SIZE;
	loongarch_std (code, loongarch_s3, loongarch_sp, offset); offset += IREG_SIZE;
	loongarch_std (code, loongarch_s4, loongarch_sp, offset); offset += IREG_SIZE;
	loongarch_std (code, loongarch_s5, loongarch_sp, offset); offset += IREG_SIZE;
	loongarch_std (code, loongarch_s6, loongarch_sp, offset); offset += IREG_SIZE;
	loongarch_std (code, loongarch_s7, loongarch_sp, offset); offset += IREG_SIZE;
	loongarch_std (code, loongarch_s8, loongarch_sp, offset); offset += IREG_SIZE;
	loongarch_std (code, loongarch_fp, loongarch_sp, offset); offset += IREG_SIZE;

	/* Restore global registers from MonoContext, including the frame pointer */
	loongarch_ldd (code, loongarch_s0, loongarch_a0, G_STRUCT_OFFSET (MonoContext, regs [loongarch_s0]));
	loongarch_ldd (code, loongarch_s1, loongarch_a0, G_STRUCT_OFFSET (MonoContext, regs [loongarch_s1]));
	loongarch_ldd (code, loongarch_s2, loongarch_a0, G_STRUCT_OFFSET (MonoContext, regs [loongarch_s2]));
	loongarch_ldd (code, loongarch_s3, loongarch_a0, G_STRUCT_OFFSET (MonoContext, regs [loongarch_s3]));
	loongarch_ldd (code, loongarch_s4, loongarch_a0, G_STRUCT_OFFSET (MonoContext, regs [loongarch_s4]));
	loongarch_ldd (code, loongarch_s5, loongarch_a0, G_STRUCT_OFFSET (MonoContext, regs [loongarch_s5]));
	loongarch_ldd (code, loongarch_s6, loongarch_a0, G_STRUCT_OFFSET (MonoContext, regs [loongarch_s6]));
	loongarch_ldd (code, loongarch_s7, loongarch_a0, G_STRUCT_OFFSET (MonoContext, regs [loongarch_s7]));
	loongarch_ldd (code, loongarch_s8, loongarch_a0, G_STRUCT_OFFSET (MonoContext, regs [loongarch_s8]));
	loongarch_ldd (code, loongarch_fp, loongarch_a0, G_STRUCT_OFFSET (MonoContext, regs [loongarch_fp]));

	/* jump to the saved IP */
	loongarch_jirl (code, loongarch_ra, loongarch_a1, 0);

	/* restore all regs from the stack */
	offset = 16;
	loongarch_ldd (code, loongarch_s0, loongarch_sp, offset); offset += IREG_SIZE;
	loongarch_ldd (code, loongarch_s1, loongarch_sp, offset); offset += IREG_SIZE;
	loongarch_ldd (code, loongarch_s2, loongarch_sp, offset); offset += IREG_SIZE;
	loongarch_ldd (code, loongarch_s3, loongarch_sp, offset); offset += IREG_SIZE;
	loongarch_ldd (code, loongarch_s4, loongarch_sp, offset); offset += IREG_SIZE;
	loongarch_ldd (code, loongarch_s5, loongarch_sp, offset); offset += IREG_SIZE;
	loongarch_ldd (code, loongarch_s6, loongarch_sp, offset); offset += IREG_SIZE;
	loongarch_ldd (code, loongarch_s7, loongarch_sp, offset); offset += IREG_SIZE;
	loongarch_ldd (code, loongarch_s8, loongarch_sp, offset); offset += IREG_SIZE;
	loongarch_ldd (code, loongarch_fp, loongarch_sp, offset); offset += IREG_SIZE;

	/* epilog */
	loongarch_ldd (code, loongarch_ra, loongarch_sp, alloc_size + LOONGARCH_RET_ADDR_OFFSET);
	loongarch_addid (code, loongarch_sp, loongarch_sp, alloc_size);
	loongarch_jirl (code, 0, loongarch_ra, 0);

	g_assert ((code - start) < size);
	MINI_END_CODEGEN (start, GPTRDIFF_TO_INT (code - start), MONO_PROFILER_CODE_BUFFER_EXCEPTION_HANDLING, NULL);
	if (info)
		*info = mono_tramp_info_create ("call_filter", start, GPTRDIFF_TO_UINT32 (code - start), ji, unwind_ops);
	return MINI_ADDR_TO_FTNPTR (start);
}

/*
 * mono_loongarch_throw_exception:
 *
 *   This function is called by the exception trampolines.
 * FP_REGS points to the 8 callee saved fp regs.
 */
static void
mono_loongarch_throw_exception (gpointer arg, host_mgreg_t pc, host_mgreg_t *int_regs, gdouble *fp_regs, gboolean corlib, gboolean rethrow, gboolean preserve_ips)
{
	ERROR_DECL (error);
	MonoContext ctx;
	MonoObject *exc = NULL;
	guint32 ex_token_index, ex_token;

	if (!corlib)
		exc = (MonoObject*)arg;
	else {
		ex_token_index = GPOINTER_TO_UINT32 (arg);
		ex_token = MONO_TOKEN_TYPE_DEF | ex_token_index;
		exc = (MonoObject*)mono_exception_from_token (mono_defaults.corlib, ex_token);
	}

	/* Adjust pc so it points into the call instruction */
	pc -= 4;

	/* Initialize a ctx based on the arguments */
	memset (&ctx, 0, sizeof (MonoContext));
	memcpy (&(ctx.regs [0]), int_regs, sizeof (host_mgreg_t) * 32);
	for (int i = 0; i < 8; i++)
		*((gdouble*)&ctx.fregs [loongarch_fs0 + i]) = fp_regs [i];
	ctx.has_fregs = 1;
	ctx.pc = pc;

	if (mono_object_isinst_checked (exc, mono_defaults.exception_class, error)) {
		MonoException *mono_ex = (MonoException*)exc;
		if (!rethrow && !mono_ex->caught_in_unmanaged) {
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

static void
mono_loongarch_resume_unwind (gpointer arg, host_mgreg_t pc, host_mgreg_t *int_regs, gdouble *fp_regs, gboolean corlib, gboolean rethrow)
{
	MonoContext ctx;

	/* Adjust pc so it points into the call instruction */
	pc -= 4;

	/* Initialize a ctx based on the arguments */
	memset (&ctx, 0, sizeof (MonoContext));
	memcpy (&(ctx.regs [0]), int_regs, sizeof (host_mgreg_t) * 32);
	for (int i = 0; i < 8; i++)
		*((gdouble*)&ctx.fregs [loongarch_fs0 + i]) = fp_regs [i];
	ctx.has_fregs = 1;
	ctx.pc = pc;

	mono_resume_unwind (&ctx);
}

static gpointer
get_throw_trampoline (int size, gboolean corlib, gboolean rethrow, gboolean llvm, gboolean resume_unwind, const char *tramp_name, MonoTrampInfo **info, gboolean aot, gboolean preserve_ips)
{
	guint8 *start, *code;
	MonoJumpInfo *ji = NULL;
	GSList *unwind_ops = NULL;
	int i, offset, gregs_offset, fregs_offset, frame_size;

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
	fregs_offset = offset;
	offset += 8 * 8;
	frame_size = ALIGN_TO (offset, MONO_ARCH_FRAME_ALIGNMENT);

	MINI_BEGIN_CODEGEN ();

	/* Setup a frame */
	loongarch_addid (code, loongarch_sp, loongarch_sp, -frame_size);
	loongarch_std (code, loongarch_fp, loongarch_sp, 0);
	loongarch_std (code, loongarch_ra, loongarch_sp, 8);
	loongarch_ori (code, loongarch_fp, loongarch_sp, 0);

	/* Save gregs , skip the zero-reg */
	for (i = 1; i < MONO_MAX_IREGS; ++i)
		loongarch_std (code, i, loongarch_fp, gregs_offset + (i << 3));

	/* The real RA is in A1 */
	if (corlib && !llvm)
		loongarch_std (code, loongarch_a1, loongarch_fp, gregs_offset + (loongarch_ra << 3));
	// Save fp/sp
	loongarch_ldd (code, loongarch_r21, loongarch_fp, 0);
	loongarch_std (code, loongarch_r21, loongarch_fp, gregs_offset + (loongarch_fp << 3));
	loongarch_addid (code, loongarch_r21, loongarch_fp, frame_size);
	loongarch_std (code, loongarch_r21, loongarch_fp, gregs_offset + (loongarch_sp << 3));
	/* Save fregs */
	for (i = loongarch_fs0; i < 32; ++i)
		loongarch_fstd (code, i, loongarch_fp, fregs_offset + ((i - loongarch_fs0) << 3));

	/* Call the C trampoline function */
	/* Arg1 =  exception object/type token */
	/* Arg2 = caller ip */
	if (corlib) {
		if (llvm)
			loongarch_ldd (code, loongarch_a1, loongarch_fp, gregs_offset + (loongarch_ra << 3));
	} else {
		loongarch_ldd (code, loongarch_a1, loongarch_fp, 8);
	}
	/* Arg 3 = gregs */
	loongarch_addid (code, loongarch_a2, loongarch_fp, gregs_offset);
	/* Arg 4 = fregs */
	loongarch_addid (code, loongarch_a3, loongarch_fp, fregs_offset);
	/* Arg 5 = corlib */
	loongarch_ori (code, loongarch_a4, loongarch_zero, corlib ? 1 : 0);
	/* Arg 6 = rethrow */
	loongarch_ori (code, loongarch_a5, loongarch_zero, rethrow ? 1 : 0);
	if (!resume_unwind) {
		/* Arg 7 = preserve_ips */
		loongarch_ori (code, loongarch_a6, loongarch_zero, preserve_ips ? 1 : 0);
	}

	/* Call the function */
	gpointer icall_func;

	if (resume_unwind)
		icall_func = (gpointer)mono_loongarch_resume_unwind;
	else
		icall_func = (gpointer)mono_loongarch_throw_exception;

	code = mono_loongarch_emit_imm64 (code, loongarch_ra, (guint64)icall_func);
	loongarch_jirl (code, 1, loongarch_ra, 0);
	/* This shouldn't return */
	loongarch_break (code, 0xd);

	g_assert ((code - start) < size);

	MINI_END_CODEGEN (start, GPTRDIFF_TO_INT (code - start), MONO_PROFILER_CODE_BUFFER_EXCEPTION_HANDLING, NULL);

	if (info)
		*info = mono_tramp_info_create (tramp_name, start, GPTRDIFF_TO_UINT32 (code - start), ji, unwind_ops);

	return MINI_ADDR_TO_FTNPTR (start);
}

/**
 * mono_arch_get_rethrow_exception:
 * \returns a function pointer which can be used to rethrow
 * exceptions. The returned function has the following
 * signature: void (*func) (MonoException *exc);
 */
gpointer
mono_arch_get_rethrow_exception (MonoTrampInfo **info, gboolean aot)
{
	return get_throw_trampoline (256, FALSE, TRUE, FALSE, FALSE, "rethrow_exception", info, aot, FALSE);
}

/**
 * mono_arch_get_rethrow_preserve_exception:
 * \returns a function pointer which can be used to rethrow
 * exceptions while avoiding modification of saved trace_ips.
 * The returned function has the following
 * signature: void (*func) (MonoException *exc);
 */
gpointer
mono_arch_get_rethrow_preserve_exception (MonoTrampInfo **info, gboolean aot)
{
	return get_throw_trampoline (256, FALSE, TRUE, FALSE, FALSE, "rethrow_preserve_exception", info, aot, TRUE);
}

/**
 * arch_get_throw_exception:
 *
 * Returns a function pointer which can be used to raise
 * exceptions. The returned function has the following
 * signature: void (*func) (MonoException *exc);
 */
gpointer
mono_arch_get_throw_exception (MonoTrampInfo **info, gboolean aot)
{
	return get_throw_trampoline (256, FALSE, FALSE, FALSE, FALSE, "throw_exception", info, aot, FALSE);
}

/**
 * mono_arch_get_throw_corlib_exception:
 * \returns a function pointer which can be used to raise
 * corlib exceptions. The returned function has the following
 * signature: void (*func) (guint32 ex_token, guint32 offset);
 */
gpointer
mono_arch_get_throw_corlib_exception (MonoTrampInfo **info, gboolean aot)
{
	return get_throw_trampoline (256, TRUE, FALSE, FALSE, FALSE, "throw_corlib_exception", info, aot, FALSE);
}

GSList*
mono_loongarch_get_exception_trampolines (gboolean aot)
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
mono_loongarch_get_exception_trampolines (gboolean aot)
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
		tramp = mono_aot_get_trampoline ("llvm_throw_corlib_exception_trampoline");
		mono_register_jit_icall_info (&mono_get_jit_icall_info ()->mono_llvm_throw_corlib_exception_trampoline, tramp, "llvm_throw_corlib_exception_trampoline", NULL, TRUE, NULL);

		tramp = mono_aot_get_trampoline ("llvm_throw_corlib_exception_abs_trampoline");
		mono_register_jit_icall_info (&mono_get_jit_icall_info ()->mono_llvm_throw_corlib_exception_abs_trampoline, tramp, "llvm_throw_corlib_exception_abs_trampoline", NULL, TRUE, NULL);

		tramp = mono_aot_get_trampoline ("llvm_resume_unwind_trampoline");
		mono_register_jit_icall_info (&mono_get_jit_icall_info ()->mono_llvm_resume_unwind_trampoline, tramp, "llvm_resume_unwind_trampoline", NULL, TRUE, NULL);
	} else {
		tramps = mono_loongarch_get_exception_trampolines (FALSE);
		for (l = tramps; l; l = l->next) {
			MonoTrampInfo *info = (MonoTrampInfo*)l->data;
			mono_register_jit_icall_info (info->jit_icall_info, info->code, g_strdup (info->name), NULL, TRUE, NULL);
			mono_tramp_info_register (info, NULL);
		}
		g_slist_free (tramps);
	}
}

/*
 * mono_arch_unwind_frame:
 *
 * This function is used to gather information from @ctx, and store it in @frame_info.
 * It unwinds one stack frame, and stores the resulting context into @new_ctx. @lmf
 * is modified if needed.
 * Returns TRUE on success, FALSE otherwise.
 */
gboolean
mono_arch_unwind_frame (MonoJitTlsData *jit_tls,
							 MonoJitInfo *ji, MonoContext *ctx,
							 MonoContext *new_ctx, MonoLMF **lmf,
							 host_mgreg_t **save_locations,
							 StackFrameInfo *frame)
{
	memset (frame, 0, sizeof (StackFrameInfo));
	frame->ji = ji;

	*new_ctx = *ctx;

	if (ji != NULL) {
		int i;
		gpointer ip = MONO_CONTEXT_GET_IP (ctx);
		host_mgreg_t regs [MONO_MAX_IREGS + 1];
		guint8 *cfa;
		guint32 unwind_info_len;
		guint8 *unwind_info;

		if (ji->is_trampoline)
			frame->type = FRAME_TYPE_TRAMPOLINE;
		else
			frame->type = FRAME_TYPE_MANAGED;

		unwind_info = mono_jinfo_get_unwind_info (ji, &unwind_info_len);

		for (i = 0; i < MONO_MAX_IREGS; ++i)
			regs [i] = new_ctx->regs [i];

		gboolean success = mono_unwind_frame (unwind_info, unwind_info_len, ji->code_start,
						   (guint8*)ji->code_start + ji->code_size,
						   ip, NULL, regs, MONO_MAX_IREGS,
						   save_locations, MONO_MAX_IREGS, &cfa);

		if (!success)
			return FALSE;

		for (i = 0; i < MONO_MAX_IREGS; ++i)
			new_ctx->regs [i] = regs [i];
		new_ctx->pc = regs [loongarch_ra];
		new_ctx->regs [loongarch_sp] = (host_mgreg_t)(gsize)cfa;

		if (*lmf && (*lmf)->gregs [MONO_ARCH_LMF_REG_SP] && (MONO_CONTEXT_GET_SP (ctx) >= (gpointer)(*lmf)->gregs [MONO_ARCH_LMF_REG_SP])) {
			/* remove any unused lmf */
			*lmf = (MonoLMF*)(((gsize)(*lmf)->previous_lmf) & ~3);
		}

		/* we substract 1, so that the IP points into the call instruction */
		new_ctx->pc--;
		return TRUE;
	} else if (*lmf) {
		g_assert ((((guint64)(*lmf)->previous_lmf) & 2) == 0);

		ji = mini_jit_info_table_find ((gpointer)(*lmf)->pc);
		if (!ji) {
			// FIXME: This can happen with multiple appdomains (bug #444383)
			return FALSE;
		}

		frame->ji = ji;
		frame->type = FRAME_TYPE_MANAGED_TO_NATIVE;

		memcpy (&new_ctx->regs [loongarch_s0], &(*lmf)->gregs [2], sizeof (gulong) * 9);
		memcpy (&new_ctx->fregs, (*lmf)->fregs, sizeof (gdouble) * MONO_ARCH_NUM_LMF_FREGS);
		new_ctx->regs [loongarch_fp] = (*lmf)->gregs [MONO_ARCH_LMF_REG_FP];
		new_ctx->regs [loongarch_sp] = (*lmf)->gregs [MONO_ARCH_LMF_REG_SP];
		MONO_CONTEXT_SET_IP (new_ctx, (*lmf)->pc);
		/* ensure that we've made progress */
		new_ctx->pc--;

		*lmf = (gpointer)(((gsize)(*lmf)->previous_lmf) & ~3);

		return TRUE;
	}

	return FALSE;
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

	MONO_ENTER_GC_UNSAFE_UNBALANCED;

	mono_handle_exception (&ctx, obj);

	MONO_EXIT_GC_UNSAFE_UNBALANCED;

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
#elif defined(MONO_ARCH_USE_SIGACTION)
	void *sigctx = ctx;

	/*
	 * Handling the exception in the signal handler is problematic, since the original
	 * signal is disabled, and we could run arbitrary code though the debugger. So
	 * resume into the normal stack and do most work there if possible.
	 */
	MonoJitTlsData *jit_tls = mono_tls_get_jit_tls ();
	guint64 sp = UCONTEXT_GREGS (sigctx) [loongarch_sp];

	/* Pass the ctx parameter in TLS */
	mono_sigctx_to_monoctx (sigctx, &jit_tls->ex_ctx);
	/* The others in registers */
	UCONTEXT_GREGS (sigctx)[loongarch_a0] = (gsize)obj;

	/* Allocate a stack frame */
	sp -= 256;
	UCONTEXT_GREGS (sigctx) [loongarch_sp] = sp;

	UCONTEXT_REG_PC (sigctx) = (gsize)handle_signal_exception;

	return TRUE;
#else
	MonoContext mctx;
	gboolean result;

	mono_sigctx_to_monoctx (ctx, &mctx);

	result = mono_handle_exception (&mctx, obj);
	/* restore the context so that returning from the signal handler will invoke
	 * the catch clause
	 */
	mono_monoctx_to_sigctx (&mctx, ctx);
	return result;
#endif
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

void
mono_arch_undo_ip_adjustment (MonoContext *ctx)
{
	gpointer pc = (gpointer)ctx->pc;
	pc = (gpointer)((guint64)MINI_FTNPTR_TO_ADDR (pc) + 1);
	ctx->pc = (host_mgreg_t)MINI_ADDR_TO_FTNPTR (pc);
}

void
mono_arch_do_ip_adjustment (MonoContext *ctx)
{
	gpointer pc = (gpointer)ctx->pc;
	pc = (gpointer)((guint64)MINI_FTNPTR_TO_ADDR (pc) - 1);
	ctx->pc = (host_mgreg_t)MINI_ADDR_TO_FTNPTR (pc);
}
