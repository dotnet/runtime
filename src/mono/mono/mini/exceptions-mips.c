/**
 * \file
 * exception support for MIPS
 *
 * Authors:
 *   Mark Mason (mason@broadcom.com)
 *
 * Based on exceptions-ppc.c by:
 *   Dietmar Maurer (dietmar@ximian.com)
 *   Paolo Molaro (lupus@ximian.com)
 *
 * (C) 2006 Broadcom
 * (C) 2001 Ximian, Inc.
 */

#include <config.h>
#include <glib.h>
#include <signal.h>
#include <string.h>

#include <mono/arch/mips/mips-codegen.h>
#include <mono/metadata/tabledefs.h>
#include <mono/metadata/threads.h>
#include <mono/metadata/debug-helpers.h>
#include <mono/metadata/exception.h>
#include <mono/metadata/mono-debug.h>

#include "mini.h"
#include "mini-mips.h"
#include "mini-runtime.h"
#include "aot-runtime.h"
#include "mono/utils/mono-tls-inline.h"

#define GENERIC_EXCEPTION_SIZE 256

/*
 * mono_arch_get_restore_context:
 *
 * Returns a pointer to a method which restores a previously saved MonoContext.
 * The first argument in a0 is the pointer to the MonoContext.
 */
gpointer
mono_arch_get_restore_context (MonoTrampInfo **info, gboolean aot)
{
	int i;
	guint8 *code;
	static guint8 start [512];
	static int inited = 0;
	guint32 iregs_to_restore;

	g_assert (!aot);
	if (info)
		*info = NULL;

	if (inited)
		return start;
	inited = 1;
	code = start;

	mips_move (code, mips_at, mips_a0);

	iregs_to_restore = (MONO_ARCH_CALLEE_SAVED_REGS \
			    | (1 << mips_sp) | (1 << mips_ra));
	for (i = 0; i < MONO_SAVED_GREGS; ++i) {
		//if (iregs_to_restore & (1 << i)) {
		if (i != mips_zero && i != mips_at) {
			MIPS_LW (code, i, mips_at, G_STRUCT_OFFSET (MonoContext, sc_regs[i]));
		}
	}

	/* Get the address to return to */
	mips_lw (code, mips_t9, mips_at, G_STRUCT_OFFSET (MonoContext, sc_pc));

	/* jump to the saved IP */
	mips_jr (code, mips_t9);
	mips_nop (code);

	/* never reached */
	mips_break (code, 0xff);

	g_assert ((code - start) < sizeof(start));
	mono_arch_flush_icache (start, code - start);
	MONO_PROFILER_RAISE (jit_code_buffer, (start, code - start, MONO_PROFILER_CODE_BUFFER_EXCEPTION_HANDLING, NULL));
	return start;
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
	static guint8 start [320];
	static int inited = 0;
	guint8 *code;
	int alloc_size;
	int offset;

	g_assert (!aot);
	if (info)
		*info = NULL;

	if (inited)
		return start;

	inited = 1;
	code = start;

	alloc_size = 64;
	g_assert ((alloc_size & (MIPS_STACK_ALIGNMENT-1)) == 0);

	mips_addiu (code, mips_sp, mips_sp, -alloc_size);
	mips_sw (code, mips_ra, mips_sp, alloc_size + MIPS_RET_ADDR_OFFSET);

	/* Save global registers on stack (s0 - s7) */
	offset = 16;
	MIPS_SW (code, mips_s0, mips_sp, offset); offset += IREG_SIZE;
	MIPS_SW (code, mips_s1, mips_sp, offset); offset += IREG_SIZE;
	MIPS_SW (code, mips_s2, mips_sp, offset); offset += IREG_SIZE;
	MIPS_SW (code, mips_s3, mips_sp, offset); offset += IREG_SIZE;
	MIPS_SW (code, mips_s4, mips_sp, offset); offset += IREG_SIZE;
	MIPS_SW (code, mips_s5, mips_sp, offset); offset += IREG_SIZE;
	MIPS_SW (code, mips_s6, mips_sp, offset); offset += IREG_SIZE;
	MIPS_SW (code, mips_s7, mips_sp, offset); offset += IREG_SIZE;
	MIPS_SW (code, mips_fp, mips_sp, offset); offset += IREG_SIZE;

	/* Restore global registers from MonoContext, including the frame pointer */
	MIPS_LW (code, mips_s0, mips_a0, G_STRUCT_OFFSET (MonoContext, sc_regs[mips_s0]));
	MIPS_LW (code, mips_s1, mips_a0, G_STRUCT_OFFSET (MonoContext, sc_regs[mips_s1]));
	MIPS_LW (code, mips_s2, mips_a0, G_STRUCT_OFFSET (MonoContext, sc_regs[mips_s2]));
	MIPS_LW (code, mips_s3, mips_a0, G_STRUCT_OFFSET (MonoContext, sc_regs[mips_s3]));
	MIPS_LW (code, mips_s4, mips_a0, G_STRUCT_OFFSET (MonoContext, sc_regs[mips_s4]));
	MIPS_LW (code, mips_s5, mips_a0, G_STRUCT_OFFSET (MonoContext, sc_regs[mips_s5]));
	MIPS_LW (code, mips_s6, mips_a0, G_STRUCT_OFFSET (MonoContext, sc_regs[mips_s6]));
	MIPS_LW (code, mips_s7, mips_a0, G_STRUCT_OFFSET (MonoContext, sc_regs[mips_s7]));
	MIPS_LW (code, mips_fp, mips_a0, G_STRUCT_OFFSET (MonoContext, sc_regs[mips_fp]));

	/* a1 is the handler to call */
	mips_move (code, mips_t9, mips_a1);

	/* jump to the saved IP */
	mips_jalr (code, mips_t9, mips_ra);
	mips_nop (code);

	/* restore all regs from the stack */
	offset = 16;
	MIPS_LW (code, mips_s0, mips_sp, offset); offset += IREG_SIZE;
	MIPS_LW (code, mips_s1, mips_sp, offset); offset += IREG_SIZE;
	MIPS_LW (code, mips_s2, mips_sp, offset); offset += IREG_SIZE;
	MIPS_LW (code, mips_s3, mips_sp, offset); offset += IREG_SIZE;
	MIPS_LW (code, mips_s4, mips_sp, offset); offset += IREG_SIZE;
	MIPS_LW (code, mips_s5, mips_sp, offset); offset += IREG_SIZE;
	MIPS_LW (code, mips_s6, mips_sp, offset); offset += IREG_SIZE;
	MIPS_LW (code, mips_s7, mips_sp, offset); offset += IREG_SIZE;
	MIPS_LW (code, mips_fp, mips_sp, offset); offset += IREG_SIZE;

	/* epilog */
	mips_lw (code, mips_ra, mips_sp, alloc_size + MIPS_RET_ADDR_OFFSET);
	mips_addiu (code, mips_sp, mips_sp, alloc_size);
	mips_jr (code, mips_ra);
	mips_nop (code);

	g_assert ((code - start) < sizeof(start));
	mono_arch_flush_icache (start, code - start);
	MONO_PROFILER_RAISE (jit_code_buffer, (start, code - start, MONO_PROFILER_CODE_BUFFER_EXCEPTION_HANDLING, NULL));
	return start;
}

static void
throw_exception (MonoObject *exc, unsigned long eip, unsigned long esp, gboolean rethrow, gboolean preserve_ips)
{
	ERROR_DECL (error);
	MonoContext ctx;

#ifdef DEBUG_EXCEPTIONS
	g_print ("throw_exception: exc=%p eip=%p esp=%p rethrow=%d\n",
		 exc, (void *)eip, (void *) esp, rethrow);
#endif

	/* adjust eip so that it point into the call instruction */
	eip -= 8;

	memset (&ctx, 0, sizeof (MonoContext));

	/*g_print  ("stack in throw: %p\n", esp);*/
	memcpy (&ctx.sc_regs, (void *)(esp + MIPS_STACK_PARAM_OFFSET),
		sizeof (gulong) * MONO_SAVED_GREGS);
	memset (&ctx.sc_fpregs, 0, sizeof (mips_freg) * MONO_SAVED_FREGS);
	MONO_CONTEXT_SET_IP (&ctx, eip);

	if (mono_object_isinst_checked (exc, mono_defaults.exception_class, error)) {
		MonoException *mono_ex = (MonoException*)exc;
		if (!rethrow && !mono_ex->caught_in_unmanaged) {
			mono_ex->stack_trace = NULL;
			mono_ex->trace_ips = NULL;
		} if (preserve_ips) {
			mono_ex->caught_in_unmanaged = TRUE;
		}
	}
	mono_error_assert_ok (error);
	mono_handle_exception (&ctx, exc);
#ifdef DEBUG_EXCEPTIONS
	g_print ("throw_exception: restore to pc=%p sp=%p fp=%p ctx=%p\n",
		 (void *) ctx.sc_pc, (void *) ctx.sc_regs[mips_sp],
		 (void *) ctx.sc_regs[mips_fp], &ctx);
#endif
	mono_restore_context (&ctx);

	g_assert_not_reached ();
}

/**
 * arch_get_throw_exception_generic:
 *
 * Returns a function pointer which can be used to raise 
 * exceptions. The returned function has the following 
 * signature: void (*func) (MonoException *exc); or
 * void (*func) (char *exc_name);
 *
 */
static gpointer 
mono_arch_get_throw_exception_generic (guint8 *start, int size, int corlib, gboolean rethrow, gboolean preserve_ips)
{
	guint8 *code;
	int alloc_size, pos, i;

	code = start;

	//g_print ("mono_arch_get_throw_exception_generic: code=%p\n", code);

	pos = 0;
	/* XXX - save all the FP regs on the stack ? */

	pos += MONO_MAX_IREGS * sizeof(guint32);

	alloc_size = MIPS_MINIMAL_STACK_SIZE + pos + 64;
	// align to MIPS_STACK_ALIGNMENT bytes
	alloc_size += MIPS_STACK_ALIGNMENT - 1;
	alloc_size &= ~(MIPS_STACK_ALIGNMENT - 1);

	g_assert ((alloc_size & (MIPS_STACK_ALIGNMENT-1)) == 0);
	mips_addiu (code, mips_sp, mips_sp, -alloc_size);
	mips_sw (code, mips_ra, mips_sp, alloc_size + MIPS_RET_ADDR_OFFSET);

	/* Save all the regs on the stack */
	for (i = 0; i < MONO_MAX_IREGS; i++) {
		if (i != mips_sp)
			MIPS_SW (code, i, mips_sp, i*IREG_SIZE + MIPS_STACK_PARAM_OFFSET);
		else {
			mips_addiu (code, mips_at, mips_sp, alloc_size);
			MIPS_SW (code, mips_at, mips_sp, i*IREG_SIZE + MIPS_STACK_PARAM_OFFSET);
		}
	}

	if (corlib) {
		mips_move (code, mips_a1, mips_a0);
		mips_load (code, mips_a0, mono_defaults.corlib);
		mips_load (code, mips_t9, mono_exception_from_token);
		mips_jalr (code, mips_t9, mips_ra);
		mips_nop (code);
		mips_move (code, mips_a0, mips_v0);
	}
	/* call throw_exception (exc, ip, sp, rethrow) */

	/* exc is already in place in a0 */

	/* pointer to ip */
	if (corlib)
		mips_lw (code, mips_a1, mips_sp, alloc_size + MIPS_RET_ADDR_OFFSET);
	else
		mips_move (code, mips_a1, mips_ra);

	/* current sp & rethrow */
	mips_move (code, mips_a2, mips_sp);
	mips_addiu (code, mips_a3, mips_zero, rethrow);

	mips_load (code, mips_t9, throw_exception);
	mips_jr (code, mips_t9);
	mips_nop (code);
	/* we should never reach this breakpoint */
	mips_break (code, 0xfe);

	g_assert ((code - start) < size);
	mono_arch_flush_icache (start, code - start);
	MONO_PROFILER_RAISE (jit_code_buffer, (start, code - start, MONO_PROFILER_CODE_BUFFER_EXCEPTION_HANDLING, NULL));
	return start;
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
	static guint8 start [GENERIC_EXCEPTION_SIZE];
	static int inited = 0;

	g_assert (!aot);
	if (info)
		*info = NULL;

	if (inited)
		return start;
	mono_arch_get_throw_exception_generic (start, sizeof (start), FALSE, TRUE, FALSE);
	inited = 1;
	return start;
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
	static guint8 start [GENERIC_EXCEPTION_SIZE];
	static int inited = 0;

	g_assert (!aot);
	if (info)
		*info = NULL;

	if (inited)
		return start;
	mono_arch_get_throw_exception_generic (start, sizeof (start), FALSE, TRUE, TRUE);
	inited = 1;
	return start;
}

/**
 * arch_get_throw_exception:
 *
 * Returns a function pointer which can be used to raise 
 * exceptions. The returned function has the following 
 * signature: void (*func) (MonoException *exc); 
 * For example to raise an arithmetic exception you can use:
 *
 * x86_push_imm (code, mono_get_exception_arithmetic ()); 
 * x86_call_code (code, arch_get_throw_exception ()); 
 *
 */
gpointer
mono_arch_get_throw_exception (MonoTrampInfo **info, gboolean aot)
{
	static guint8 start [GENERIC_EXCEPTION_SIZE];
	static int inited = 0;

	g_assert (!aot);
	if (info)
		*info = NULL;

	if (inited)
		return start;
	mono_arch_get_throw_exception_generic (start, sizeof (start), FALSE, FALSE, FALSE);
	inited = 1;
	return start;
}

gpointer 
mono_arch_get_throw_exception_by_name (void)
{
	guint8 *start, *code;
	int size = 64;

	/* Not used on MIPS */	
	start = code = mono_global_codeman_reserve (size);
	mips_break (code, 0xfd);
	mono_arch_flush_icache (start, code - start);
	MONO_PROFILER_RAISE (jit_code_buffer, (start, code - start, MONO_PROFILER_CODE_BUFFER_EXCEPTION_HANDLING, NULL));
	return start;
}

/**
 * mono_arch_get_throw_corlib_exception:
 * \returns a function pointer which can be used to raise 
 * corlib exceptions. The returned function has the following 
 * signature: void (*func) (guint32 ex_token, guint32 offset); 
 * On MIPS, the offset argument is missing.
 */
gpointer
mono_arch_get_throw_corlib_exception (MonoTrampInfo **info, gboolean aot)
{
	static guint8 start [GENERIC_EXCEPTION_SIZE];
	static int inited = 0;

	g_assert (!aot);
	if (info)
		*info = NULL;

	if (inited)
		return start;
	mono_arch_get_throw_exception_generic (start, sizeof (start), TRUE, FALSE, FALSE);
	inited = 1;
	return start;
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
			regs [i] = new_ctx->sc_regs [i];

		gboolean success = mono_unwind_frame (unwind_info, unwind_info_len, ji->code_start, 
						   (guint8*)ji->code_start + ji->code_size,
						   ip, NULL, regs, MONO_MAX_IREGS,
						   save_locations, MONO_MAX_IREGS, &cfa);

		if (!success)
			return FALSE;

		for (i = 0; i < MONO_MAX_IREGS; ++i)
			new_ctx->sc_regs [i] = regs [i];
		new_ctx->sc_pc = regs [mips_ra];
		new_ctx->sc_regs [mips_sp] = (host_mgreg_t)(gsize)cfa;

		/* we substract 8, so that the IP points into the call instruction */
		MONO_CONTEXT_SET_IP (new_ctx, new_ctx->sc_pc - 8);

		/* Sanity check -- we should have made progress here */
		g_assert (MONO_CONTEXT_GET_SP (new_ctx) != MONO_CONTEXT_GET_SP (ctx));
		return TRUE;
	} else if (*lmf) {
		g_assert ((((guint64)(*lmf)->previous_lmf) & 2) == 0);

		if (!(*lmf)->method) {
#ifdef DEBUG_EXCEPTIONS
			g_print ("mono_arch_unwind_frame: bad lmf @ %p\n", (void *) *lmf);
#endif
			return FALSE;
		}
		g_assert (((*lmf)->magic == MIPS_LMF_MAGIC1) || ((*lmf)->magic == MIPS_LMF_MAGIC2));

		ji = mini_jit_info_table_find ((gpointer)(*lmf)->eip);
		if (!ji)
			return FALSE;

		frame->ji = ji;
		frame->type = FRAME_TYPE_MANAGED_TO_NATIVE;

		memcpy (&new_ctx->sc_regs, (*lmf)->iregs, sizeof (gulong) * MONO_SAVED_GREGS);
		memcpy (&new_ctx->sc_fpregs, (*lmf)->fregs, sizeof (float) * MONO_SAVED_FREGS);
		MONO_CONTEXT_SET_IP (new_ctx, (*lmf)->eip);
		/* ensure that we've made progress */
		g_assert (new_ctx->sc_pc != ctx->sc_pc);

		*lmf = (gpointer)(((gsize)(*lmf)->previous_lmf) & ~3);

		return TRUE;
	}

	return FALSE;
}

gpointer
mono_arch_ip_from_context (void *sigctx)
{
	return (gpointer)(gsize)UCONTEXT_REG_PC (sigctx);
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

	mono_handle_exception (&ctx, obj);

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
	guint64 sp = UCONTEXT_GREGS (sigctx) [mips_sp];

	/* Pass the ctx parameter in TLS */
	mono_sigctx_to_monoctx (sigctx, &jit_tls->ex_ctx);
	/* The others in registers */
	UCONTEXT_GREGS (sigctx)[mips_a0] = (gsize)obj;

	/* Allocate a stack frame */
	sp -= 256;
	UCONTEXT_GREGS (sigctx)[mips_sp] = sp;

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
