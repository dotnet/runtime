/*
 * Licensed to the .NET Foundation under one or more agreements.
 * The .NET Foundation licenses this file to you under the MIT license.
 */

#include "mini-runtime.h"

#include <mono/metadata/abi-details.h>
#include <mono/metadata/tokentype.h>
#include <mono/utils/mono-sigcontext.h>
#include "mono/utils/mono-tls-inline.h"

#ifndef DISABLE_JIT

static gpointer
nop_stub (unsigned int pattern)
{
	guint8 *code, *start;

	start = code = mono_global_codeman_reserve (0x50);

	/* hang in debugger */
	riscv_addi (code, RISCV_X0, RISCV_X0, pattern);
	riscv_ebreak (code);

	mono_arch_flush_icache (start, code - start);

	return start;
}

gpointer
mono_arch_get_restore_context (MonoTrampInfo **info, gboolean aot)
{
	guint8 *start, *code;
	MonoJumpInfo *ji = NULL;
	GSList *unwind_ops = NULL;
	int i, ctx_reg, size;

	size = 512;
	code = start = mono_global_codeman_reserve (size);

	riscv_addi (code, RISCV_T0, RISCV_A0, 0);
	ctx_reg = RISCV_T0;

	/* Restore fregs */
	for (i = 0; i < RISCV_N_FREGS; ++i)
		riscv_flw (code, i, ctx_reg, MONO_STRUCT_OFFSET (MonoContext, fregs) + (i * sizeof (double)));

	/* Restore gregs */
	for (i = 0; i < RISCV_N_FREGS; ++i) {
		if (i == ctx_reg)
			continue;
		riscv_ld (code, i, ctx_reg, MONO_STRUCT_OFFSET (MonoContext, gregs) + (i * sizeof (double)));
	}

	riscv_ld (code, ctx_reg, ctx_reg, MONO_STRUCT_OFFSET (MonoContext, gregs) + (RISCV_ZERO * sizeof (double)));
	riscv_jalr (code, RISCV_ZERO, ctx_reg, 0);
	/* Not reached */
	riscv_ebreak (code);

	g_assert ((code - start) < size);
	mono_arch_flush_icache (start, code - start);
	MONO_PROFILER_RAISE (jit_code_buffer, (start, code - start, MONO_PROFILER_CODE_BUFFER_EXCEPTION_HANDLING, NULL));

	if (info)
		*info = mono_tramp_info_create ("restore_context", start, code - start, ji, unwind_ops);

	return start;
}

void
mono_riscv_throw_exception (gpointer arg, host_mgreg_t pc, host_mgreg_t *int_regs, gdouble *fp_regs, gboolean corlib, gboolean rethrow, gboolean preserve_ips){
	ERROR_DECL (error);
	MonoContext ctx;
	MonoObject *exc = NULL;
	guint32 ex_token_index, ex_token;
	if (!corlib)
		exc = (MonoObject *)arg;
	else {
		ex_token_index = (guint64)arg;
		ex_token = MONO_TOKEN_TYPE_DEF | ex_token_index;
		exc = (MonoObject *)mono_exception_from_token (mono_defaults.corlib, ex_token);
	}

	/* Adjust pc so it points into the call instruction */
	pc--;

	/* Initialize a ctx based on the arguments */
	memset (&ctx, 0, sizeof (MonoContext));
	memcpy (&(ctx.gregs [0]), int_regs, sizeof (host_mgreg_t) * RISCV_N_GREGS);
	memcpy (&(ctx.fregs [0]), fp_regs, sizeof (host_mgreg_t) * RISCV_N_FREGS);

	ctx.gregs [0] = pc;

	if (mono_object_isinst_checked (exc, mono_defaults.exception_class, error)) {
		MonoException *mono_ex = (MonoException *)exc;
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

gpointer
mono_arch_get_call_filter (MonoTrampInfo **info, gboolean aot)
{
	guint8 *code;
	guint8 *start;
	int size, offset, gregs_offset, fregs_offset, ctx_offset, frame_size;
	MonoJumpInfo *ji = NULL;
	GSList *unwind_ops = NULL;

	size = 632;
	start = code = mono_global_codeman_reserve (size);

	/* Compute stack frame size and offsets */
	offset = 0;
	/* ra & fp */
	offset += 2 * sizeof (host_mgreg_t);

	/* gregs */
	offset += RISCV_N_GREGS * sizeof (host_mgreg_t);
	gregs_offset = offset;

	/* fregs */
	offset += RISCV_N_FREGS * sizeof (host_mgreg_t);
	fregs_offset = offset;

	/* ctx */
	offset += sizeof (host_mgreg_t);
	ctx_offset = offset;
	frame_size = ALIGN_TO (offset, MONO_ARCH_FRAME_ALIGNMENT);

	/*
	 * We are being called from C code, ctx is in a0, the address to call is in a1.
	 * We need to save state, restore ctx, make the call, then restore the previous state,
	 * returning the value returned by the call.
	 */

	MINI_BEGIN_CODEGEN ();

	// riscv_ebreak (code);

	/* Setup a frame */
	g_assert (RISCV_VALID_I_IMM (-frame_size));
	riscv_addi (code, RISCV_SP, RISCV_SP, -frame_size);
	code = mono_riscv_emit_store (code, RISCV_RA, RISCV_SP, frame_size - sizeof (host_mgreg_t), 0);
	code = mono_riscv_emit_store (code, RISCV_FP, RISCV_SP, frame_size - 2 * sizeof (host_mgreg_t), 0);
	riscv_addi (code, RISCV_FP, RISCV_SP, frame_size);

	/* Save ctx */
	code = mono_riscv_emit_store (code, RISCV_A0, RISCV_FP, -ctx_offset, 0);
	/* Save gregs */
	code = mono_riscv_emit_store_stack (code, MONO_ARCH_CALLEE_SAVED_REGS, RISCV_FP, -gregs_offset, FALSE);
	/* Save fregs */
	if (riscv_stdext_f || riscv_stdext_d)
		code = mono_riscv_emit_store_stack (code, 0xffffffff, RISCV_FP, -fregs_offset, TRUE);

	/* Load regs from ctx */
	code = mono_riscv_emit_load_regarray (code, MONO_ARCH_CALLEE_SAVED_REGS, RISCV_A0,
	                                      MONO_STRUCT_OFFSET (MonoContext, gregs), FALSE);

	/* Load fregs */
	if (riscv_stdext_f || riscv_stdext_d)
		code =
		    mono_riscv_emit_load_regarray (code, 0xffffffff, RISCV_A0, MONO_STRUCT_OFFSET (MonoContext, fregs), TRUE);

	/* Load fp */
	// code = mono_riscv_emit_load (code, RISCV_FP, RISCV_A0, MONO_STRUCT_OFFSET (MonoContext, gregs) + (RISCV_FP *
	// sizeof (host_mgreg_t)), 0);

	/* Make the call */
	riscv_jalr (code, RISCV_RA, RISCV_A1, 0);
	/* For filters, the result is in R0 */

	/* Restore fp */
	riscv_addi (code, RISCV_FP, RISCV_SP, frame_size);

	/* Load ctx */
	code = mono_riscv_emit_load (code, RISCV_T0, RISCV_FP, -ctx_offset, 0);
	/* Save registers back to ctx, except FP*/
	/* This isn't strictly necessary since we don't allocate variables used in eh clauses to registers */
	code = mono_riscv_emit_store_regarray (code, MONO_ARCH_CALLEE_SAVED_REGS ^ (1 << RISCV_FP), RISCV_T0,
	                                       MONO_STRUCT_OFFSET (MonoContext, gregs), FALSE);

	/* Restore regs */
	code = mono_riscv_emit_load_stack (code, MONO_ARCH_CALLEE_SAVED_REGS, RISCV_FP, -gregs_offset, FALSE);
	/* Restore fregs */
	if (riscv_stdext_f || riscv_stdext_d)
		code = mono_riscv_emit_load_stack (code, 0xffffffff, RISCV_FP, -fregs_offset, TRUE);

	/* Destroy frame */
	code = mono_riscv_emit_destroy_frame (code);

	riscv_jalr (code, RISCV_X0, RISCV_RA, 0);

	g_assert ((code - start) < size);

	MINI_END_CODEGEN (start, code - start, MONO_PROFILER_CODE_BUFFER_EXCEPTION_HANDLING, NULL);

	if (info)
		*info = mono_tramp_info_create ("call_filter", start, code - start, ji, unwind_ops);

	return MINI_ADDR_TO_FTNPTR (start);
}

static gpointer
get_throw_trampoline (int size, gboolean corlib, gboolean rethrow, gboolean llvm, gboolean resume_unwind, const char *tramp_name, MonoTrampInfo **info, gboolean aot, gboolean preserve_ips){
	guint8 *start, *code;
	MonoJumpInfo *ji = NULL;
	GSList *unwind_ops = NULL;
	int offset, gregs_offset, fregs_offset, frame_size, num_fregs;

	code = start = mono_global_codeman_reserve (size);

	/* This will being called by JITted code, the exception object/type token is in A0 */

	/* Compute stack frame size and offsets */
	offset = 0;
	/* ra & fp */
	offset += 2 * sizeof (host_mgreg_t);

	/* gregs */
	offset += RISCV_N_GREGS * sizeof (host_mgreg_t);
	gregs_offset = offset;

	/* fregs */
	num_fregs = RISCV_N_FREGS;
	offset += num_fregs * sizeof (host_mgreg_t);
	fregs_offset = offset;
	frame_size = ALIGN_TO (offset, MONO_ARCH_FRAME_ALIGNMENT);

	MINI_BEGIN_CODEGEN ();

	/* Setup a frame */
	g_assert (RISCV_VALID_I_IMM (-frame_size));
	riscv_addi (code, RISCV_SP, RISCV_SP, -frame_size);
	code = mono_riscv_emit_store (code, RISCV_RA, RISCV_SP, frame_size - sizeof (host_mgreg_t), 0);
	code = mono_riscv_emit_store (code, RISCV_FP, RISCV_SP, frame_size - 2 * sizeof (host_mgreg_t), 0);
	riscv_addi (code, RISCV_FP, RISCV_SP, frame_size);

	/* Save gregs */
	code = mono_riscv_emit_store_stack (code, 0xffffffff, RISCV_FP, -gregs_offset, FALSE);
	if (corlib && !llvm)
		/* The real ra is in A1 */
		code = mono_riscv_emit_store (code, RISCV_A1, RISCV_FP, -gregs_offset + (RISCV_RA * sizeof (host_mgreg_t)), 0);

	/* Save previous fp/sp */
	code = mono_riscv_emit_load (code, RISCV_T0, RISCV_FP, -2 * (gint32)sizeof (host_mgreg_t), 0);
	code = mono_riscv_emit_store (code, RISCV_T0, RISCV_FP, -gregs_offset + (RISCV_FP * sizeof (host_mgreg_t)), 0);
	// current fp is previous sp
	code = mono_riscv_emit_store (code, RISCV_FP, RISCV_FP, -gregs_offset + (RISCV_SP * sizeof (host_mgreg_t)), 0);

	/* Save fregs */
	if (riscv_stdext_f || riscv_stdext_d)
		code = mono_riscv_emit_store_stack (code, 0xffffffff, RISCV_FP, -fregs_offset, TRUE);

	/* Call the C trampoline function */
	/* Arg1 =  exception object/type token */
	// riscv_addi (code, RISCV_A0, RISCV_A0, 0);
	/* Arg2 = caller ip, should be return address in this case */
	if (corlib) {
		// caller ip are set to A1 already
		if (llvm)
			NOT_IMPLEMENTED;
	} else
		code = mono_riscv_emit_load (code, RISCV_A1, RISCV_FP, -(gint32)sizeof (host_mgreg_t), 0);
	/* Arg 3 = gregs */
	riscv_addi (code, RISCV_A2, RISCV_FP, -gregs_offset);
	/* Arg 4 = fregs */
	riscv_addi (code, RISCV_A3, RISCV_FP, -fregs_offset);
	/* Arg 5 = corlib */
	riscv_addi (code, RISCV_A4, RISCV_ZERO, corlib ? 1 : 0);
	/* Arg 6 = rethrow */
	riscv_addi (code, RISCV_A5, RISCV_ZERO, rethrow ? 1 : 0);
	if (!resume_unwind) {
		/* Arg 7 = preserve_ips */
		riscv_addi (code, RISCV_A6, RISCV_ZERO, preserve_ips ? 1 : 0);
	}

	/* Call the function */
	if (aot) {
		NOT_IMPLEMENTED;
	} else {
		gpointer icall_func;

		if (resume_unwind)
			// icall_func = (gpointer)mono_riscv_resume_unwind;
			NOT_IMPLEMENTED;
		else
			icall_func = (gpointer)mono_riscv_throw_exception;

		code = mono_riscv_emit_imm (code, RISCV_RA, (guint64)icall_func);
	}
	riscv_jalr (code, RISCV_ZERO, RISCV_RA, 0);
	/* This shouldn't return */
	/* hang in debugger */
	riscv_ebreak (code);

	g_assert ((code - start) < size);
	MINI_END_CODEGEN (start, code - start, MONO_PROFILER_CODE_BUFFER_EXCEPTION_HANDLING, NULL);

	if (info)
		*info = mono_tramp_info_create (tramp_name, start, code - start, ji, unwind_ops);

	return MINI_ADDR_TO_FTNPTR (start);
}

gpointer
mono_arch_get_throw_exception (MonoTrampInfo **info, gboolean aot)
{
	return get_throw_trampoline (384, FALSE, FALSE, FALSE, FALSE, "throw_exception", info, aot, FALSE);
}

gpointer
mono_arch_get_rethrow_exception (MonoTrampInfo **info, gboolean aot)
{
	return get_throw_trampoline (384, FALSE, TRUE, FALSE, FALSE, "rethrow_exception", info, aot, FALSE);
}

gpointer
mono_arch_get_rethrow_preserve_exception (MonoTrampInfo **info, gboolean aot)
{
	return get_throw_trampoline (384, FALSE, TRUE, FALSE, FALSE, "rethrow_preserve_exception", info, aot, TRUE);
}

gpointer
mono_arch_get_throw_corlib_exception (MonoTrampInfo **info, gboolean aot)
{
	return get_throw_trampoline (384, TRUE, FALSE, FALSE, FALSE, "throw_corlib_exception", info, aot, FALSE);
}

#else

gpointer
mono_arch_get_restore_context (MonoTrampInfo **info, gboolean aot)
{
    g_assert_not_reached ();
    return NULL;
}

gpointer
mono_arch_get_call_filter (MonoTrampInfo **info, gboolean aot)
{
    g_assert_not_reached ();
    return NULL;
}

gpointer
mono_arch_get_throw_exception (MonoTrampInfo **info, gboolean aot)
{
    g_assert_not_reached ();
    return NULL;
}

gpointer
mono_arch_get_rethrow_exception (MonoTrampInfo **info, gboolean aot)
{
    g_assert_not_reached ();
    return NULL;
}

gpointer
mono_arch_get_throw_corlib_exception (MonoTrampInfo **info, gboolean aot)
{
	g_assert_not_reached ();
	return NULL;
}

#endif

void
mono_arch_exceptions_init (void)
{
	// NOT_IMPLEMENTED;
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
mono_arch_unwind_frame (MonoJitTlsData *jit_tls, MonoJitInfo *ji,
                        MonoContext *ctx, MonoContext *new_ctx, MonoLMF **lmf,
                        host_mgreg_t **save_locations, StackFrameInfo *frame)
{
	memset (frame, 0, sizeof (StackFrameInfo));
	frame->ji = ji;

	*new_ctx = *ctx;

	if (ji != NULL) {
		// all GREG + Callee saved FREG
		host_mgreg_t regs [MONO_MAX_IREGS + 12 + 1];
		guint8 *cfa;
		guint32 unwind_info_len;
		guint8 *unwind_info;

		if (ji->is_trampoline)
			frame->type = FRAME_TYPE_TRAMPOLINE;
		else
			frame->type = FRAME_TYPE_MANAGED;

		unwind_info = mono_jinfo_get_unwind_info (ji, &unwind_info_len);

		memcpy (regs, &new_ctx->gregs, sizeof (host_mgreg_t) * 32);

		/* f8..f9 & f18..f27 are callee saved */
		for (int i = 0; i < 2; i++)
			(regs + MONO_MAX_IREGS) [i] = *((host_mgreg_t*)&new_ctx->fregs [RISCV_F8 + i]);
		for (int i = 0; i < 10; i++)
			(regs + MONO_MAX_IREGS) [i] = *((host_mgreg_t*)&new_ctx->fregs [RISCV_F18 + i]);

		gpointer ip = MINI_FTNPTR_TO_ADDR (MONO_CONTEXT_GET_IP (ctx));

		// printf ("%s %p %p\n", ji->d.method->name, ji->code_start, ip);
		// mono_print_unwind_info (unwind_info, unwind_info_len);

		gboolean success = mono_unwind_frame (unwind_info, unwind_info_len, (guint8 *)ji->code_start,
		                                      (guint8 *)ji->code_start + ji->code_size, (guint8 *)ip, NULL, regs,
		                                      MONO_MAX_IREGS + 12 + 1, save_locations, MONO_MAX_IREGS, (guint8 **)&cfa);

		if (!success)
			return FALSE;

		memcpy (new_ctx->gregs, regs, sizeof (host_mgreg_t) * MONO_MAX_IREGS);
		for (int i = 0; i < 2; i++)
			*((host_mgreg_t*)&new_ctx->fregs [RISCV_F8 + i]) = (regs + MONO_MAX_IREGS) [i];
		for (int i = 0; i < 10; i++)
			*((host_mgreg_t *)&new_ctx->fregs [RISCV_F18 + i]) = (regs + MONO_MAX_IREGS) [2 + i];

		new_ctx->gregs [0] = regs [RISCV_RA];
		new_ctx->gregs [RISCV_SP] = (host_mgreg_t)(gsize)cfa;

		if (*lmf && (*lmf)->gregs [MONO_ARCH_LMF_REG_SP] &&
		    (MONO_CONTEXT_GET_SP (ctx) >= (gpointer)(*lmf)->gregs [MONO_ARCH_LMF_REG_SP])) {
			/* remove any unused lmf */
			*lmf = (MonoLMF *)(((gsize)(*lmf)->previous_lmf) & ~3);
		}

		/* we subtract 1, so that the PC points into the call instruction */
		new_ctx->gregs [0]--;

		return TRUE;
	} else if (*lmf) {
		g_assert ((((guint64)(*lmf)->previous_lmf) & 2) == 0);

		frame->type = FRAME_TYPE_MANAGED_TO_NATIVE;

		ji = mini_jit_info_table_find ((gpointer)(*lmf)->pc);
		if (!ji)
			return FALSE;

		g_assert (MONO_ARCH_LMF_REGS == ((MONO_ARCH_CALLEE_SAVED_REGS) | (1 << RISCV_SP)));

		memcpy (&new_ctx->gregs [0], &(*lmf)->gregs [0], sizeof (host_mgreg_t) * RISCV_N_GREGS);
		for (int i = 0; i < RISCV_N_GREGS; i++) {
			if (!(MONO_ARCH_LMF_REGS & (1 << i))) {
				new_ctx->gregs [i] = ctx->gregs [i];
			}
		}
		// new_ctx->gregs [RISCV_FP] = (*lmf)->gregs [MONO_ARCH_LMF_REG_FP];
		// new_ctx->gregs [RISCV_SP] = (*lmf)->gregs [MONO_ARCH_LMF_REG_SP];
		new_ctx->gregs [0] = (*lmf)->pc; // use [0] as pc reg since x0 is hard-wired zero

		/* we subtract 1, so that the PC points into the call instruction */
		new_ctx->gregs [0]--;

		*lmf = (MonoLMF *)(((gsize)(*lmf)->previous_lmf) & ~3);

		return TRUE;
	}

	return FALSE;
}

static void
handle_signal_exception (gpointer obj)
{
	MonoJitTlsData *jit_tls = mono_tls_get_jit_tls ();
	MonoContext ctx = jit_tls->ex_ctx;

	MONO_ENTER_GC_UNSAFE_UNBALANCED;

	mono_handle_exception (&ctx, obj);

	MONO_EXIT_GC_UNSAFE_UNBALANCED;

	mono_restore_context (&ctx);
}

gboolean
mono_arch_handle_exception (void *ctx, gpointer obj)
{
	MonoJitTlsData *jit_tls = mono_tls_get_jit_tls ();

	mono_sigctx_to_monoctx (ctx, &jit_tls->ex_ctx);

	// Call handle_signal_exception () on the normal stack.
	UCONTEXT_GREGS (ctx) [RISCV_A0] = (long) obj;
	UCONTEXT_REG_PC (ctx) = (long) handle_signal_exception;

	return TRUE;
}

gpointer
mono_arch_ip_from_context (void *sigctx)
{
	return (gpointer) UCONTEXT_REG_PC (sigctx);
}

void
mono_arch_setup_async_callback (MonoContext *ctx, void (*async_cb)(void *fun), gpointer user_data)
{
	// Allocate a stack frame and redirect PC.
	MONO_CONTEXT_SET_SP (ctx, (host_mgreg_t) MONO_CONTEXT_GET_SP (ctx) - 32);

	mono_arch_setup_resume_sighandler_ctx (ctx, async_cb);
}

void
mono_arch_setup_resume_sighandler_ctx (MonoContext *ctx, gpointer func)
{
	MONO_CONTEXT_SET_IP (ctx, func);
}

void
mono_arch_undo_ip_adjustment (MonoContext *context)
{
	context->gregs[RISCV_ZERO]++;
}

void
mono_arch_do_ip_adjustment (MonoContext *context)
{
	context->gregs[RISCV_ZERO]--;
}
