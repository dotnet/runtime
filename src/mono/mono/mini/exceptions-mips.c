/*
 * exceptions-mips.c: exception support for MIPS
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
#include <ucontext.h>

#include <mono/arch/mips/mips-codegen.h>
#include <mono/metadata/appdomain.h>
#include <mono/metadata/tabledefs.h>
#include <mono/metadata/threads.h>
#include <mono/metadata/debug-helpers.h>
#include <mono/metadata/exception.h>
#include <mono/metadata/mono-debug.h>

#include "mini.h"
#include "mini-mips.h"

#define GENERIC_EXCEPTION_SIZE 256

/* XXX */
#if 1
#define restore_regs_from_context(ctx_reg,ip_reg,tmp_reg) do {	\
	} while (0)
#else
#define restore_regs_from_context(ctx_reg,pc,tmp_reg) do {	\
		int reg;	\
		ppc_lwz (code, pc, G_STRUCT_OFFSET (MonoContext, sc_pc), ctx_reg);	\
		ppc_lmw (code, ppc_r13, ctx_reg, G_STRUCT_OFFSET (MonoContext, sc_regs));	\
		for (reg = 0; reg < MONO_SAVED_FREGS; ++reg) {	\
			ppc_lfd (code, (14 + reg), G_STRUCT_OFFSET(MonoLMF, sc_fpregs) + reg * sizeof (gdouble), ctx_reg);	\
		}	\
	} while (0)
#endif

/* nothing to do */
#define setup_context(ctx) do { \
		memset ((ctx), 0, sizeof(*(ctx)));	\
	} while (0);

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
	static guint8 start [128];
	static int inited = 0;
	guint32 iregs_to_restore;

	g_assert (!aot);
	if (info)
		*info = NULL;

	if (inited)
		return start;
	inited = 1;
	code = start;

	iregs_to_restore = (MONO_ARCH_CALLEE_SAVED_REGS \
			    | (1 << mips_sp) | (1 << mips_ra));
	for (i = 0; i < MONO_SAVED_GREGS; ++i) {
		if (iregs_to_restore & (1 << i)) {
			MIPS_LW (code, i, mips_a0, G_STRUCT_OFFSET (MonoContext, sc_regs[i]));
		}
	}

	/* Get the address to return to */
	mips_lw (code, mips_t9, mips_a0, G_STRUCT_OFFSET (MonoContext, sc_pc));

	/* jump to the saved IP */
	mips_jr (code, mips_t9);
	mips_nop (code);

	/* never reached */
	mips_break (code, 0xff);

	g_assert ((code - start) < sizeof(start));
	mono_arch_flush_icache (start, code - start);
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
	return start;
}

static void
throw_exception (MonoObject *exc, unsigned long eip, unsigned long esp, gboolean rethrow)
{
	static void (*restore_context) (MonoContext *);
	MonoContext ctx;

#ifdef DEBUG_EXCEPTIONS
	g_print ("throw_exception: exc=%p eip=%p esp=%p rethrow=%d\n",
		 exc, (void *)eip, (void *) esp, rethrow);
#endif

	if (!restore_context)
		restore_context = mono_get_restore_context ();

	/* adjust eip so that it point into the call instruction */
	eip -= 8;

	setup_context (&ctx);

	/*g_print  ("stack in throw: %p\n", esp);*/
	memcpy (&ctx.sc_regs, (void *)(esp + MIPS_STACK_PARAM_OFFSET),
		sizeof (gulong) * MONO_SAVED_GREGS);
	memset (&ctx.sc_fpregs, 0, sizeof (mips_freg) * MONO_SAVED_FREGS);
	MONO_CONTEXT_SET_IP (&ctx, eip);

	if (mono_object_isinst (exc, mono_defaults.exception_class)) {
		MonoException *mono_ex = (MonoException*)exc;
		if (!rethrow)
			mono_ex->stack_trace = NULL;
	}
	mono_handle_exception (&ctx, exc, (void *)eip, FALSE);
#ifdef DEBUG_EXCEPTIONS
	g_print ("throw_exception: restore to pc=%p sp=%p fp=%p ctx=%p\n",
		 (void *) ctx.sc_pc, (void *) ctx.sc_regs[mips_sp],
		 (void *) ctx.sc_regs[mips_fp], &ctx);
#endif
	restore_context (&ctx);

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
mono_arch_get_throw_exception_generic (guint8 *start, int size, int corlib, gboolean rethrow)
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
	return start;
}

/**
 * mono_arch_get_rethrow_exception:
 *
 * Returns a function pointer which can be used to rethrow 
 * exceptions. The returned function has the following 
 * signature: void (*func) (MonoException *exc); 
 *
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
	mono_arch_get_throw_exception_generic (start, sizeof (start), FALSE, TRUE);
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
	mono_arch_get_throw_exception_generic (start, sizeof (start), FALSE, FALSE);
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
	return start;
}

/**
 * mono_arch_get_throw_corlib_exception:
 *
 * Returns a function pointer which can be used to raise 
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
	mono_arch_get_throw_exception_generic (start, sizeof (start), TRUE, FALSE);
	inited = 1;
	return start;
}	

static MonoArray *
glist_to_array (GList *list, MonoClass *eclass) 
{
	MonoDomain *domain = mono_domain_get ();
	MonoArray *res;
	int len, i;

	if (!list)
		return NULL;

	len = g_list_length (list);
	res = mono_array_new (domain, eclass, len);

	for (i = 0; list; list = list->next, i++)
		mono_array_set (res, gpointer, i, list->data);

	return res;
}

/*
 * mono_arch_find_jit_info:
 *
 * This function is used to gather information from @ctx, and store it in @frame_info.
 * It unwinds one stack frame, and stores the resulting context into @new_ctx. @lmf
 * is modified if needed.
 * Returns TRUE on success, FALSE otherwise.
 */
gboolean
mono_arch_find_jit_info (MonoDomain *domain, MonoJitTlsData *jit_tls, 
							 MonoJitInfo *ji, MonoContext *ctx, 
							 MonoContext *new_ctx, MonoLMF **lmf, 
							 mgreg_t **save_locations,
							 StackFrameInfo *frame)
{
	gpointer ip = MONO_CONTEXT_GET_IP (ctx);
	gpointer fp = MONO_CONTEXT_GET_BP (ctx);
	guint32 sp;

	memset (frame, 0, sizeof (StackFrameInfo));
	frame->ji = ji;
	frame->managed = FALSE;

	*new_ctx = *ctx;

	if (ji != NULL) {
		int i;
		gint32 address;
		int offset = 0;

		frame->type = FRAME_TYPE_MANAGED;

		if (!ji->method->wrapper_type || ji->method->wrapper_type == MONO_WRAPPER_DYNAMIC_METHOD)
			frame->managed = TRUE;

		if (*lmf && (MONO_CONTEXT_GET_BP (ctx) >= (gpointer)(*lmf)->ebp)) {
			/* remove any unused lmf */
			*lmf = (*lmf)->previous_lmf;
		}

		address = (char *)ip - (char *)ji->code_start;

		/* My stack frame */
		fp = MONO_CONTEXT_GET_BP (ctx);

		/* Compute the previous stack frame */
		sp = (guint32)(fp) - (short)(*(guint32 *)(ji->code_start));

		/* Sanity check the frame */
		if (!sp || (sp == 0xffffffff)
		    || (sp & 0x07) || (sp < 64*1024)) {
#ifdef DEBUG_EXCEPTIONS
			g_print ("mono_arch_find_jit_info: bad stack sp=%p\n", (void *) sp);
#endif
			return FALSE;
		}

		if (ji->method->save_lmf && 0) {
			/* only enable this when prologue stops emitting
			 * normal save of s-regs when save_lmf is true.
			 * Will have to sync with prologue code at that point.
			 */
			memcpy (&new_ctx->sc_fpregs,
				(char*)sp - sizeof (float) * MONO_SAVED_FREGS,
				sizeof (float) * MONO_SAVED_FREGS);
			memcpy (&new_ctx->sc_regs,
				(char*)sp - sizeof (float) * MONO_SAVED_FREGS - sizeof (gulong) * MONO_SAVED_GREGS,
				sizeof (gulong) * MONO_SAVED_GREGS);
		} else if (ji->used_regs) {
			guint32 *insn;
			guint32 mask = ji->used_regs;

			/* these all happen before adjustment of fp */
			/* Look for sw ??, ????(sp) */
			insn = ((guint32 *)ji->code_start) + 1;
			while (!*insn || ((*insn & 0xffe00000) == 0xafa00000) || ((*insn & 0xffe00000) == 0xffa00000)) {
				int reg = (*insn >> 16) & 0x1f;
				guint32 addr = (((guint32)fp) + (short)(*insn & 0x0000ffff));

				mask &= ~(1 << reg);
				if ((*insn & 0xffe00000) == 0xafa00000)
					new_ctx->sc_regs [reg] = *(guint32 *)addr;
				else
					new_ctx->sc_regs [reg] = *(guint64 *)addr;
				insn++;
			}
			MONO_CONTEXT_SET_SP (new_ctx, sp);
			MONO_CONTEXT_SET_BP (new_ctx, sp);
			/* assert that we found all registers we were supposed to */
			g_assert (!mask);
		}
		/* we substract 8, so that the IP points into the call instruction */
		MONO_CONTEXT_SET_IP (new_ctx, new_ctx->sc_regs[mips_ra] - 8);

		/* Sanity check -- we should have made progress here */
		g_assert (new_ctx->sc_pc != ctx->sc_pc);
		return TRUE;
	} else if (*lmf) {
		if (!(*lmf)->method) {
#ifdef DEBUG_EXCEPTIONS
			g_print ("mono_arch_find_jit_info: bad lmf @ %p\n", (void *) *lmf);
#endif
			return FALSE;
		}
		g_assert (((*lmf)->magic == MIPS_LMF_MAGIC1) || ((*lmf)->magic == MIPS_LMF_MAGIC2));

		ji = mini_jit_info_table_find (domain, (gpointer)(*lmf)->eip, NULL);
		if (!ji) {
			// FIXME: This can happen with multiple appdomains (bug #444383)
			return FALSE;
		}

		frame->ji = ji;
		frame->type = FRAME_TYPE_MANAGED_TO_NATIVE;

		memcpy (&new_ctx->sc_regs, (*lmf)->iregs, sizeof (gulong) * MONO_SAVED_GREGS);
		memcpy (&new_ctx->sc_fpregs, (*lmf)->fregs, sizeof (float) * MONO_SAVED_FREGS);
		MONO_CONTEXT_SET_IP (new_ctx, (*lmf)->eip);
		/* ensure that we've made progress */
		g_assert (new_ctx->sc_pc != ctx->sc_pc);
		*lmf = (*lmf)->previous_lmf;

		return TRUE;
	}

	return FALSE;
}

void
mono_arch_sigctx_to_monoctx (void *sigctx, MonoContext *mctx)
{
	int i;
	struct sigcontext *ctx = (struct sigcontext *)sigctx;

	mctx->sc_pc = ctx->sc_pc;
	for (i = 0; i < 32; ++i) {
		mctx->sc_regs[i] = ctx->sc_regs[i];
		mctx->sc_fpregs[i] = ctx->sc_fpregs[i];
	}
}

void
mono_arch_monoctx_to_sigctx (MonoContext *mctx, void *sigctx)
{
	int i;
	struct sigcontext *ctx = (struct sigcontext *)sigctx;

	ctx->sc_pc = mctx->sc_pc;
	for (i = 0; i < 32; ++i) {
		ctx->sc_regs[i] = mctx->sc_regs[i];
		ctx->sc_fpregs[i] = mctx->sc_fpregs[i];
	}
}	

gpointer
mono_arch_ip_from_context (void *sigctx)
{
	struct sigcontext *ctx = (struct sigcontext *)sigctx;
	return (gpointer)(guint32)ctx->sc_pc;
}

/*
 * This is the function called from the signal handler
 */
gboolean
mono_arch_handle_exception (void *ctx, gpointer obj, gboolean test_only)
{
	MonoContext mctx;
	gboolean result;
	
	mono_arch_sigctx_to_monoctx (ctx, &mctx);
#ifdef DEBUG_EXCEPTIONS
	g_print ("mono_arch_handle_exception: pc=%p\n", (void *) mctx.sc_pc);
#endif
	mono_handle_exception (&mctx, obj, (gpointer)mctx.sc_pc, test_only);
	result = TRUE;

#ifdef DEBUG_EXCEPTIONS
	g_print ("mono_arch_handle_exception: restore pc=%p\n", (void *)mctx.sc_pc);
#endif
	/* restore the context so that returning from the signal handler
	 * will invoke the catch clause 
	 */
	mono_arch_monoctx_to_sigctx (&mctx, ctx);

	return result;
}


gboolean
mono_arch_has_unwind_info (gconstpointer addr)
{
	return FALSE;
}

