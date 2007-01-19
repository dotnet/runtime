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

#ifdef CUSTOM_EXCEPTION_HANDLING
static gboolean arch_handle_exception (MonoContext *ctx, gpointer obj, gboolean test_only);
#endif

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
mono_arch_get_restore_context (void)
{
	int i;
	guint8 *code;
	static guint8 start [128];
	static int inited = 0;

	if (inited)
		return start;
	inited = 1;
	code = start;

	for (i = 0; i < MONO_SAVED_GREGS; ++i) {
		if (MONO_ARCH_CALLEE_SAVED_REGS & (1 << i)) {
			mips_lw (code, i, mips_a0, G_STRUCT_OFFSET (MonoContext, sc_regs[i]));
		}
	}

	/* restore also the sp, fp and ra */
	mips_lw (code, mips_sp, mips_a0, G_STRUCT_OFFSET (MonoContext, sc_regs[mips_sp]));
	mips_lw (code, mips_fp, mips_a0, G_STRUCT_OFFSET (MonoContext, sc_regs[mips_fp]));
	mips_lw (code, mips_ra, mips_a0, G_STRUCT_OFFSET (MonoContext, sc_regs[mips_ra]));

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
mono_arch_get_call_filter (void)
{
	static guint8 start [320];
	static int inited = 0;
	guint8 *code;
	int alloc_size;
	int offset;

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
	mips_sw (code, mips_s0, mips_sp, offset); offset += 4;
	mips_sw (code, mips_s1, mips_sp, offset); offset += 4;
	mips_sw (code, mips_s2, mips_sp, offset); offset += 4;
	mips_sw (code, mips_s3, mips_sp, offset); offset += 4;
	mips_sw (code, mips_s4, mips_sp, offset); offset += 4;
	mips_sw (code, mips_s5, mips_sp, offset); offset += 4;
	mips_sw (code, mips_s6, mips_sp, offset); offset += 4;
	mips_sw (code, mips_s7, mips_sp, offset); offset += 4;
	mips_sw (code, mips_fp, mips_sp, offset); offset += 4;

	/* Restore global registers from MonoContext, including the frame pointer */
	mips_lw (code, mips_s0, mips_a0, G_STRUCT_OFFSET (MonoContext, sc_regs[mips_s0]));
	mips_lw (code, mips_s1, mips_a0, G_STRUCT_OFFSET (MonoContext, sc_regs[mips_s1]));
	mips_lw (code, mips_s2, mips_a0, G_STRUCT_OFFSET (MonoContext, sc_regs[mips_s2]));
	mips_lw (code, mips_s3, mips_a0, G_STRUCT_OFFSET (MonoContext, sc_regs[mips_s3]));
	mips_lw (code, mips_s4, mips_a0, G_STRUCT_OFFSET (MonoContext, sc_regs[mips_s4]));
	mips_lw (code, mips_s5, mips_a0, G_STRUCT_OFFSET (MonoContext, sc_regs[mips_s5]));
	mips_lw (code, mips_s6, mips_a0, G_STRUCT_OFFSET (MonoContext, sc_regs[mips_s6]));
	mips_lw (code, mips_s7, mips_a0, G_STRUCT_OFFSET (MonoContext, sc_regs[mips_s7]));
	mips_lw (code, mips_fp, mips_a0, G_STRUCT_OFFSET (MonoContext, sc_regs[mips_fp]));

	/* a1 is the handler to call */
	mips_move (code, mips_t9, mips_a1);

	/* jump to the saved IP */
	mips_jalr (code, mips_t9, mips_ra);
	mips_nop (code);

	/* restore all regs from the stack */
	offset = 16;
	mips_lw (code, mips_s0, mips_sp, offset); offset += 4;
	mips_lw (code, mips_s1, mips_sp, offset); offset += 4;
	mips_lw (code, mips_s2, mips_sp, offset); offset += 4;
	mips_lw (code, mips_s3, mips_sp, offset); offset += 4;
	mips_lw (code, mips_s4, mips_sp, offset); offset += 4;
	mips_lw (code, mips_s5, mips_sp, offset); offset += 4;
	mips_lw (code, mips_s6, mips_sp, offset); offset += 4;
	mips_lw (code, mips_s7, mips_sp, offset); offset += 4;
	mips_lw (code, mips_fp, mips_sp, offset); offset += 4;

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
	g_print ("throw_exception: exc=%p eip=%x esp=%x rethrow=%d\n",
	       exc, (unsigned int) eip, (unsigned int) esp, rethrow);
#endif

	if (!restore_context)
		restore_context = mono_arch_get_restore_context ();

	/* adjust eip so that it point into the call instruction */
	eip -= 8;

	setup_context (&ctx);

	/*g_print  ("stack in throw: %p\n", esp);*/
	memcpy (&ctx.sc_regs, (void *)(esp + MIPS_STACK_PARAM_OFFSET),
		sizeof (gulong) * MONO_SAVED_GREGS);
#if 0
	memcpy (&ctx.sc_fpregs, fp_regs, sizeof (float) * MONO_SAVED_FREGS);
#else
	memset (&ctx.sc_fpregs, 0, sizeof (float) * MONO_SAVED_FREGS);
#endif
	MONO_CONTEXT_SET_IP (&ctx, eip);

	if (mono_object_isinst (exc, mono_defaults.exception_class)) {
		MonoException *mono_ex = (MonoException*)exc;
		if (!rethrow)
			mono_ex->stack_trace = NULL;
	}
#ifdef CUSTOM_EXCEPTION_HANDLING
	arch_handle_exception (&ctx, exc, FALSE);
#else
	mono_handle_exception (&ctx, exc, (void *)eip, FALSE);
#endif
#ifdef DEBUG_EXCEPTIONS
	g_print ("throw_exception: restore to %p\n", (void *) ctx.sc_pc);
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
mono_arch_get_throw_exception_generic (guint8 *start, int size, int by_name, gboolean rethrow)
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
			mips_sw (code, i, mips_sp, i*sizeof(guint32) + MIPS_STACK_PARAM_OFFSET);
		else {
			mips_addiu (code, mips_at, mips_sp, alloc_size);
			mips_sw (code, mips_at, mips_sp, i*sizeof(guint32) + MIPS_STACK_PARAM_OFFSET);
		}
	}

	if (by_name) {
		mips_move (code, mips_a2, mips_a0);
		mips_load (code, mips_a0, mono_defaults.corlib);
		mips_load (code, mips_a1, "System");
		mips_load (code, mips_t9, mono_exception_from_name);
		mips_jalr (code, mips_t9, mips_ra);
		mips_nop (code);
		mips_move (code, mips_a0, mips_v0);
	}
	/* call throw_exception (exc, ip, sp, rethrow) */

	/* exc is already in place in a0 */

	/* pointer to ip */
	if (by_name)
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
mono_arch_get_rethrow_exception (void)
{
	static guint8 start [GENERIC_EXCEPTION_SIZE];
	static int inited = 0;

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
mono_arch_get_throw_exception (void)
{
	static guint8 start [GENERIC_EXCEPTION_SIZE];
	static int inited = 0;

	if (inited)
		return start;
	mono_arch_get_throw_exception_generic (start, sizeof (start), FALSE, FALSE);
	inited = 1;
	return start;
}

/**
 * arch_get_throw_exception_by_name:
 *
 * Returns a function pointer which can be used to raise 
 * corlib exceptions. The returned function has the following 
 * signature: void (*func) (char *exc_name); 
 * For example to raise an arithmetic exception you can use:
 *
 * x86_push_imm (code, "ArithmeticException"); 
 * x86_call_code (code, arch_get_throw_exception_by_name ()); 
 *
 */
gpointer 
mono_arch_get_throw_exception_by_name (void)
{
	static guint8 start [GENERIC_EXCEPTION_SIZE];
	static int inited = 0;

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

/* mono_arch_find_jit_info:
 *
 * This function is used to gather information from @ctx. It returns the 
 * MonoJitInfo of the corresponding function, unwinds one stack frame and
 * stores the resulting context into @new_ctx. It also stores a string 
 * describing the stack location into @trace (if not NULL), and modifies
 * the @lmf if necessary. @native_offset return the IP offset from the 
 * start of the function or -1 if that info is not available.
 */
MonoJitInfo *
mono_arch_find_jit_info (MonoDomain *domain, MonoJitTlsData *jit_tls,
			 MonoJitInfo *res, MonoJitInfo *prev_ji,
			 MonoContext *ctx, MonoContext *new_ctx,
			 char **trace, MonoLMF **lmf,
			 int *native_offset, gboolean *managed)
{
	MonoJitInfo *ji;
	gpointer ip = MONO_CONTEXT_GET_IP (ctx);
	guint32 sp;

	/* Avoid costly table lookup during stack overflow */
	if (prev_ji && (ip > prev_ji->code_start && ((guint8*)ip < ((guint8*)prev_ji->code_start) + prev_ji->code_size)))
		ji = prev_ji;
	else
		ji = mono_jit_info_table_find (domain, ip);

	if (trace)
		*trace = NULL;

	if (native_offset)
		*native_offset = -1;

	if (managed)
		*managed = FALSE;

	if (ji != NULL) {
		int i;
		gint32 address;
		int offset = 0;

		*new_ctx = *ctx;
		setup_context (new_ctx);

		if (*lmf && (MONO_CONTEXT_GET_BP (ctx) >= (gpointer)(*lmf)->ebp)) {
			/* remove any unused lmf */
			*lmf = (*lmf)->previous_lmf;
		}

		address = (char *)ip - (char *)ji->code_start;

		if (native_offset)
			*native_offset = address;

		if (managed)
			if (!ji->method->wrapper_type)
				*managed = TRUE;

		if (trace) {
			*trace = mono_debug_print_stack_frame (ji->method, offset, domain);
		}
		/* Compute the previous stack frame */
		sp = (guint32)(MONO_CONTEXT_GET_BP (ctx)) - (short)(*(guint32 *)(ji->code_start));

		/* Sanity check the frame */
		if (!sp || (sp == 0xffffffff)
		    || (sp & 0x07) || (sp < 64*1024))
			return (gpointer)-1;
		MONO_CONTEXT_SET_BP (new_ctx, sp);

		if (ji->method->save_lmf && 0) {
			memcpy (&new_ctx->sc_fpregs, (char*)sp - sizeof (float) * MONO_SAVED_FREGS, sizeof (float) * MONO_SAVED_FREGS);
			memcpy (&new_ctx->sc_regs, (char*)sp - sizeof (float) * MONO_SAVED_FREGS - sizeof (gulong) * MONO_SAVED_GREGS, sizeof (gulong) * MONO_SAVED_GREGS);
		} else if (ji->used_regs) {
			/* keep updated with emit_prolog in mini-mips.c */
			offset = 0;
			/* FIXME handle floating point args */
			for (i = 0; i < MONO_MAX_IREGS; i++) {
				new_ctx->sc_regs [i] = 0;
			}
			new_ctx->sc_regs [mips_fp] = sp;
			new_ctx->sc_regs [mips_sp] = sp;
			new_ctx->sc_regs [mips_ra] = *(guint32 *)(sp-4);
		}
		/* we substract 8, so that the IP points into the call instruction */
		MONO_CONTEXT_SET_IP (new_ctx, new_ctx->sc_regs[mips_ra] - 8);

		return ji;
	} else if (*lmf) {
		
		*new_ctx = *ctx;
		setup_context (new_ctx);

		if (!(*lmf)->method)
			return (gpointer)-1;

		if (trace) {
			char *fname = mono_method_full_name ((*lmf)->method, TRUE);
			*trace = g_strdup_printf ("in (unmanaged) %s", fname);
			g_free (fname);
		}
		
		if ((ji = mono_jit_info_table_find (domain, (gpointer)(*lmf)->eip))) {
		} else {
			memset (res, 0, sizeof (MonoJitInfo));
			res->method = (*lmf)->method;
		}

#if 0
		memcpy (&new_ctx->sc_regs, (*lmf)->iregs, sizeof (gulong) * MONO_SAVED_GREGS);
		memcpy (&new_ctx->sc_fpregs, (*lmf)->fregs, sizeof (float) * MONO_SAVED_FREGS);
#endif
		MONO_CONTEXT_SET_BP (new_ctx, (*lmf)->ebp);
		MONO_CONTEXT_SET_IP (new_ctx, (*lmf)->eip);
		*lmf = (*lmf)->previous_lmf;

		return ji ? ji : res;
	}

	return NULL;
}

#ifdef CUSTOM_STACK_WALK

void
mono_jit_walk_stack (MonoStackWalk func, gboolean do_il_offset, gpointer user_data) {
	MonoDomain *domain = mono_domain_get ();
	MonoJitTlsData *jit_tls = TlsGetValue (mono_jit_tls_id);
	MonoLMF *lmf = jit_tls->lmf;
	MonoJitInfo *ji, rji;
	gint native_offset, il_offset;
	gboolean managed;
	MonoMipsStackFrame *sframe;

	MonoContext ctx, new_ctx;

	setup_context (&ctx);
	setup_context (&new_ctx);

	MONO_INIT_CONTEXT_FROM_FUNC (&ctx, mono_jit_walk_stack);
	MONO_INIT_CONTEXT_FROM_FUNC (&new_ctx, mono_jit_walk_stack);

	while (MONO_CONTEXT_GET_BP (&ctx) < jit_tls->end_of_stack) {
		
		ji = mono_find_jit_info (domain, jit_tls, &rji, NULL, &ctx, &new_ctx, NULL, &lmf, &native_offset, &managed);
		g_assert (ji);

		if (ji == (gpointer)-1)
			return;

		if (do_il_offset) {
			MonoDebugSourceLocation *source;

			source = mono_debug_lookup_source_location (ji->method, native_offset, domain);
			il_offset = source ? source->il_offset : -1;
			mono_debug_free_source_location (source);
		} else
			il_offset = -1;

		if (func (ji->method, native_offset, il_offset, managed, user_data))
			return;
		
		ctx = new_ctx;
		setup_context (&ctx);
	}
}


MonoBoolean
ves_icall_get_frame_info (gint32 skip, MonoBoolean need_file_info, 
			  MonoReflectionMethod **method, 
			  gint32 *iloffset, gint32 *native_offset,
			  MonoString **file, gint32 *line, gint32 *column)
{
	MonoDomain *domain = mono_domain_get ();
	MonoJitTlsData *jit_tls = TlsGetValue (mono_jit_tls_id);
	MonoLMF *lmf = jit_tls->lmf;
	MonoJitInfo *ji, rji;
	MonoContext ctx, new_ctx;
	MonoMipsStackFrame *sframe;
	MonoDebugSourceLocation *location;

#if 0
#ifdef __APPLE__
	__asm__ volatile("lwz   %0,0(r1)" : "=r" (sframe));
#else
	__asm__ volatile("lwz   %0,0(1)" : "=r" (sframe));
#endif
#endif
	MONO_CONTEXT_SET_BP (&ctx, sframe->sp);
	sframe = (MonoMipsStackFrame*)sframe->sp;
	MONO_CONTEXT_SET_IP (&ctx, sframe->ra);
	/*MONO_CONTEXT_SET_IP (&ctx, ves_icall_get_frame_info);
	MONO_CONTEXT_SET_BP (&ctx, __builtin_frame_address (0));*/

	skip++;

	do {
		ji = mono_find_jit_info (domain, jit_tls, &rji, NULL, &ctx, &new_ctx, NULL, &lmf, native_offset, NULL);

		ctx = new_ctx;
		
		if (!ji || ji == (gpointer)-1 || MONO_CONTEXT_GET_BP (&ctx) >= jit_tls->end_of_stack)
			return FALSE;

		/* skip all wrappers ??*/
		if (ji->method->wrapper_type == MONO_WRAPPER_RUNTIME_INVOKE ||
		    ji->method->wrapper_type == MONO_WRAPPER_REMOTING_INVOKE_WITH_CHECK ||
		    ji->method->wrapper_type == MONO_WRAPPER_XDOMAIN_INVOKE ||
		    ji->method->wrapper_type == MONO_WRAPPER_XDOMAIN_DISPATCH ||
		    ji->method->wrapper_type == MONO_WRAPPER_REMOTING_INVOKE)
			continue;

		skip--;

	} while (skip >= 0);

	*method = mono_method_get_object (domain, ji->method, NULL);

	location = mono_debug_lookup_source_location (ji->method, *native_offset, domain);
	if (location)
		*iloffset = location->il_offset;
	else
		*iloffset = 0;

	if (need_file_info) {
		if (location) {
			*file = mono_string_new (domain, location->source_file);
			*line = location->row;
			*column = location->column;
		} else {
			*file = NULL;
			*line = *column = 0;
		}
	}

	mono_debug_free_source_location (location);

	return TRUE;
}

#endif /* CUSTOM_STACK_WALK */

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
	g_print ("mono_arch_handle_exception: pc=%p\n", mctx.sc_pc);
#endif
#ifdef CUSTOM_EXCEPTION_HANDLING
	result = arch_handle_exception (&mctx, obj, test_only);
#else
	mono_handle_exception (&mctx, obj, (gpointer)mctx.sc_pc, test_only);
	result = TRUE;
#endif

#ifdef DEBUG_EXCEPTIONS
	g_print ("mono_arch_handle_exception: restore pc=%p\n", mctx.sc_pc);
#endif
	/* restore the context so that returning from the signal handler
	 * will invoke the catch clause 
	 */
	mono_arch_monoctx_to_sigctx (&mctx, ctx);

	return result;
}

#ifdef CUSTOM_EXCEPTION_HANDLING
/**
 * arch_handle_exception:
 * @ctx: saved processor state
 * @obj: the exception object
 * @test_only: only test if the exception is caught, but dont call handlers
 *
 *
 */
static gboolean
arch_handle_exception (MonoContext *ctx, gpointer obj, gboolean test_only)
{
	MonoDomain *domain = mono_domain_get ();
	MonoJitInfo *ji, rji;
	static int (*call_filter) (MonoContext *, gpointer, gpointer) = NULL;
	MonoJitTlsData *jit_tls = TlsGetValue (mono_jit_tls_id);
	MonoLMF *lmf = jit_tls->lmf;		
	GList *trace_ips = NULL;
	MonoException *mono_ex;
	MonoArray *initial_trace_ips = NULL;
	int frame_count = 0;
	gboolean has_dynamic_methods = FALSE;

	g_assert (ctx != NULL);
	if (!obj) {
		MonoException *ex = mono_get_exception_null_reference ();
		ex->message = mono_string_new (domain, 
		        "Object reference not set to an instance of an object");
		obj = (MonoObject *)ex;
	} 

	if (mono_object_isinst (obj, mono_defaults.exception_class)) {
		mono_ex = (MonoException*)obj;
		initial_trace_ips = mono_ex->trace_ips;
	} else {
		mono_ex = NULL;
	}


	if (!call_filter)
		call_filter = mono_arch_get_call_filter ();

	g_assert (jit_tls->end_of_stack);
	g_assert (jit_tls->abort_func);

	if (!test_only) {
		MonoContext ctx_cp = *ctx;
		setup_context (&ctx_cp);
		if (mono_jit_trace_calls != NULL)
			g_print ("EXCEPTION handling: %s\n", mono_object_class (obj)->name);
		if (!arch_handle_exception (&ctx_cp, obj, TRUE)) {
			if (mono_break_on_exc)
				G_BREAKPOINT ();
			mono_unhandled_exception (obj);
		}
	}

	memset (&rji, 0, sizeof (rji));

	while (1) {
		MonoContext new_ctx;

		setup_context (&new_ctx);
		ji = mono_arch_find_jit_info (domain, jit_tls, &rji, &rji, ctx, &new_ctx, 
					      NULL, &lmf, NULL, NULL);
		if (!ji) {
			g_warning ("Exception inside function without unwind info");
			g_assert_not_reached ();
		}

		if (ji != (gpointer)-1) {
			frame_count ++;
			
			if (test_only && ji->method->wrapper_type != MONO_WRAPPER_RUNTIME_INVOKE && mono_ex) {
				/* 
				 * Avoid overwriting the stack trace if the exception is
				 * rethrown. Also avoid giant stack traces during a stack
				 * overflow.
				 */
				if (!initial_trace_ips && (frame_count < 1000)) {
					trace_ips = g_list_prepend (trace_ips, MONO_CONTEXT_GET_IP (ctx));

				}
			}

			if (ji->method->dynamic)
				has_dynamic_methods = TRUE;

			if (ji->num_clauses) {
				int i;
				
				g_assert (ji->clauses);
			
				for (i = 0; i < ji->num_clauses; i++) {
					MonoJitExceptionInfo *ei = &ji->clauses [i];
					gboolean filtered = FALSE;

					if (ei->try_start <= MONO_CONTEXT_GET_IP (ctx) && 
					    MONO_CONTEXT_GET_IP (ctx) <= ei->try_end) { 
						/* catch block */

						if ((ei->flags == MONO_EXCEPTION_CLAUSE_NONE) || (ei->flags == MONO_EXCEPTION_CLAUSE_FILTER)) {
							/* store the exception object int cfg->excvar */
							g_assert (ei->exvar_offset);
							/* need to use the frame pointer (mips_fp): methods with clauses always have mips_fp */
							*((gpointer *)(void*)((char *)(ctx->sc_regs [mips_fp]) + ei->exvar_offset)) = obj;
						}

						if (ei->flags == MONO_EXCEPTION_CLAUSE_FILTER)
							filtered = call_filter (ctx, ei->data.filter, mono_ex);

						if ((ei->flags == MONO_EXCEPTION_CLAUSE_NONE && 
						     mono_object_isinst (obj, ei->data.catch_class)) || filtered) {
							if (test_only) {
								if (mono_ex && !initial_trace_ips) {
									trace_ips = g_list_reverse (trace_ips);
									mono_ex->trace_ips = glist_to_array (trace_ips, mono_defaults.int_class);
									if (has_dynamic_methods)
										/* These methods could go away anytime, so compute the stack trace now */
										mono_ex->stack_trace = ves_icall_System_Exception_get_trace (mono_ex);
								}
								g_list_free (trace_ips);
								return TRUE;
							}
							if (mono_jit_trace_calls != NULL)
								g_print ("EXCEPTION: catch found at clause %d of %s\n", i, mono_method_full_name (ji->method, TRUE));
							/*g_print  ("stack for catch: %p\n", MONO_CONTEXT_GET_BP (ctx));*/
							MONO_CONTEXT_SET_IP (ctx, ei->handler_start);
							jit_tls->lmf = lmf;
							return 0;
						}
						if (!test_only && ei->try_start <= MONO_CONTEXT_GET_IP (ctx) && 
						    MONO_CONTEXT_GET_IP (ctx) < ei->try_end &&
						    (ei->flags & MONO_EXCEPTION_CLAUSE_FINALLY)) {
							if (mono_jit_trace_calls != NULL)
								g_print ("EXCEPTION: finally clause %d of %s\n", i, mono_method_full_name (ji->method, TRUE));
							call_filter (ctx, ei->handler_start, NULL);
						}
						
					}
				}
			}
		}

		*ctx = new_ctx;
		setup_context (ctx);

		if ((ji == (gpointer)-1) || MONO_CONTEXT_GET_BP (ctx) >= jit_tls->end_of_stack) {
			if (!test_only) {
				jit_tls->lmf = lmf;
				jit_tls->abort_func (obj);
				g_assert_not_reached ();
			} else {
				if (mono_ex && !initial_trace_ips) {
					trace_ips = g_list_reverse (trace_ips);
					mono_ex->trace_ips = glist_to_array (trace_ips, mono_defaults.int_class);
					if (has_dynamic_methods)
						/* These methods could go away anytime, so compute the stack trace now */
						mono_ex->stack_trace = ves_icall_System_Exception_get_trace (mono_ex);
				}
				g_list_free (trace_ips);
				return FALSE;
			}
		}
	}

	g_assert_not_reached ();
}
#endif /* CUSTOM_EXCEPTION_HANDLING */

gboolean
mono_arch_has_unwind_info (gconstpointer addr)
{
	return FALSE;
}

