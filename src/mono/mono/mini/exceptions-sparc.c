/*
 * exceptions-sparc.c: exception support for 64 bit sparc
 *
 * Authors:
 *   Mark Crichton (crichton@gimp.org)
 *   Dietmar Maurer (dietmar@ximian.com)
 *
 * (C) 2003 Ximian, Inc.
 */

#include <config.h>
#include <glib.h>
#include <signal.h>
#include <string.h>
#include <sys/ucontext.h>

#include <mono/arch/sparc/sparc-codegen.h>
#include <mono/metadata/appdomain.h>
#include <mono/metadata/tabledefs.h>
#include <mono/metadata/threads.h>
#include <mono/metadata/debug-helpers.h>
#include <mono/metadata/exception.h>
#include <mono/metadata/mono-debug.h>
#include <mono/metadata/gc-internal.h>

#include "mini.h"
#include "mini-sparc.h"

#ifndef REG_SP
#define REG_SP REG_O6
#endif

#define MONO_SPARC_WINDOW_ADDR(sp) ((gpointer*)(((guint8*)(sp)) + MONO_SPARC_STACK_BIAS))

/*
 * mono_arch_get_restore_context:
 *
 * Returns a pointer to a method which restores a previously saved sigcontext.
 */
gpointer
mono_arch_get_restore_context (void)
{
	static guint32 start [32];
	static int inited = 0;
	guint32 *code;

	if (inited)
		return start;

	code = start;

	sparc_ldi_imm (code, sparc_o0, G_STRUCT_OFFSET (MonoContext, ip), sparc_i7);
	sparc_ldi_imm (code, sparc_o0, G_STRUCT_OFFSET (MonoContext, sp), sparc_i6);

	sparc_jmpl_imm (code, sparc_i7, 0, sparc_g0);
	/* FIXME: This does not return to the correct window */
	sparc_restore_imm (code, sparc_g0, 0, sparc_g0);

	g_assert ((code - start) < 32);

	inited = 1;

	return start;
}

/*
 * mono_arch_get_call_filter:
 *
 * Returns a pointer to a method which calls an exception filter. We
 * also use this function to call finally handlers (we pass NULL as 
 * @exc object in this case).
 *
 * call_filter (MonoContext *ctx, gpointer ip)
 */
gpointer
mono_arch_get_call_filter (void)
{
	static guint32 start [64];
	static int inited = 0;
	guint32 *code;
	int i;

	if (inited)
		return start;

	code = start;

	/*
	 * There are two frames here:
	 * - the first frame is used by call_filter
	 * - the second frame is used to run the filter code
	 */

	/* Create first frame */
	sparc_save_imm (code, sparc_sp, -256, sparc_sp);

	sparc_mov_reg_reg (code, sparc_i1, sparc_o0);
	sparc_ldi_imm (code, sparc_i0, G_STRUCT_OFFSET (MonoContext, sp), sparc_o1);

	/* Create second frame */
	sparc_save_imm (code, sparc_sp, -256, sparc_sp);

	sparc_mov_reg_reg (code, sparc_i0, sparc_o0);
	sparc_mov_reg_reg (code, sparc_i1, sparc_o1);

	/*
	 * We need to change %fp to point to the stack frame of the method
	 * containing the filter. But changing %fp also changes the %sp of
	 * the parent frame (the first frame), so if the OS saves the first frame,
	 * it saves it to the stack frame of the method, which is not good.
	 * So flush all register windows to memory before changing %fp.
	 */
	sparc_flushw (code);

	sparc_mov_reg_reg (code, sparc_fp, sparc_o7);

	/* 
	 * Modify the second frame so it is identical to the one used in the
	 * method containing the filter.
	 */
	for (i = 0; i < 16; ++i)
		sparc_ldi_imm (code, sparc_o1, MONO_SPARC_STACK_BIAS + i * sizeof (gpointer), sparc_l0 + i);

	/* Save %fp to a location reserved in mono_arch_allocate_vars */
	sparc_sti_imm (code, sparc_o7, sparc_fp, MONO_SPARC_STACK_BIAS - sizeof (gpointer));

	/* Call the filter code, after this returns, %o0 will hold the result */
	sparc_call_imm (code, sparc_o0, 0);
	sparc_nop (code);

	/* Restore original %fp */
	sparc_ldi_imm (code, sparc_fp, MONO_SPARC_STACK_BIAS - sizeof (gpointer), sparc_fp);

	sparc_mov_reg_reg (code, sparc_o0, sparc_i0);

	/* Return to first frame */
	sparc_restore (code, sparc_g0, sparc_g0, sparc_g0);

	/* FIXME: Save locals to the stack */

	/* Return to caller */
	sparc_ret (code);
	/* Return result in delay slot */
	sparc_restore (code, sparc_o0, sparc_g0, sparc_o0);

	g_assert ((code - start) < 64);

	inited = 1;

	return start;
}

static void
throw_exception (MonoObject *exc, gpointer sp, gpointer ip)
{
	MonoContext ctx;
	static void (*restore_context) (MonoContext *);
	gpointer *window;
	
	if (!restore_context)
		restore_context = mono_arch_get_restore_context ();

	window = MONO_SPARC_WINDOW_ADDR (sp);
	ctx.sp = (gpointer*)sp;
	ctx.ip = ip;
	ctx.fp = (gpointer*)(MONO_SPARC_WINDOW_ADDR (sp) [sparc_i6 - 16]);

	if (mono_object_isinst (exc, mono_defaults.exception_class)) {
		MonoException *mono_ex = (MonoException*)exc;
		mono_ex->stack_trace = NULL;
	}
	mono_handle_exception (&ctx, exc, ip, FALSE);
	restore_context (&ctx);

	g_assert_not_reached ();
}

/**
 * mono_arch_get_throw_exception_by_name:
 *
 * Returns a function pointer which can be used to raise exceptions.
 * The returned function has the following 
 * signature: void (*func) (char *exc_name); 
 */
gpointer 
mono_arch_get_throw_exception (void)
{
	static guint32 start [32];
	static int inited = 0;
	guint32 *code;

	if (inited)
		return start;

	inited = 1;
	code = start;

	sparc_save_imm (code, sparc_sp, -512, sparc_sp);

	sparc_flushw (code);
	sparc_mov_reg_reg (code, sparc_i0, sparc_o0);
	sparc_mov_reg_reg (code, sparc_fp, sparc_o1);
	sparc_mov_reg_reg (code, sparc_i7, sparc_o2);
	sparc_set (code, throw_exception, sparc_o7);
	sparc_jmpl (code, sparc_o7, sparc_g0, sparc_callsite);
	sparc_nop (code);

	g_assert ((code - start) < 32);

	return start;
}

/**
 * mono_arch_get_throw_exception_by_name:
 *
 * Returns a function pointer which can be used to raise 
 * corlib exceptions. The returned function has the following 
 * signature: void (*func) (char *exc_name, gpointer ip); 
 */
gpointer 
mono_arch_get_throw_exception_by_name (void)
{
	static guint32 start [64];
	static int inited = 0;
	guint32 *code;
	int reg;

	if (inited)
		return start;

	inited = 1;
	code = start;

#ifdef SPARCV9
	reg = sparc_g4;
#else
	reg = sparc_g1;
#endif

	sparc_save_imm (code, sparc_sp, -160, sparc_sp);

	sparc_mov_reg_reg (code, sparc_i0, sparc_o2);
	sparc_set (code, mono_defaults.corlib, sparc_o0);
	sparc_set (code, "System", sparc_o1);
	sparc_set (code, mono_exception_from_name, sparc_o7);
	sparc_jmpl (code, sparc_o7, sparc_g0, sparc_callsite);
	sparc_nop (code);

	/* Return to the caller, so exception handling does not see this frame */
	sparc_restore (code, sparc_o0, sparc_g0, sparc_o0);

	/* Put original return address into %o7 */
	sparc_mov_reg_reg (code, sparc_o1, sparc_o7);
	sparc_set (code, mono_arch_get_throw_exception (), reg);
	/* Use a jmp instead of a call so o7 is preserved */
	sparc_jmpl_imm (code, reg, 0, sparc_g0);
	sparc_nop (code);

	g_assert ((code - start) < 32);

	return start;
}	

/* mono_arch_find_jit_info:
 *
 * This function is used to gather information from @ctx. It return the 
 * MonoJitInfo of the corresponding function, unwinds one stack frame and
 * stores the resulting context into @new_ctx. It also stores a string 
 * describing the stack location into @trace (if not NULL), and modifies
 * the @lmf if necessary. @native_offset return the IP offset from the 
 * start of the function or -1 if that info is not available.
 */
MonoJitInfo *
mono_arch_find_jit_info (MonoDomain *domain, MonoJitTlsData *jit_tls, MonoJitInfo *res, MonoJitInfo *prev_ji, MonoContext *ctx, 
			 MonoContext *new_ctx, char **trace, MonoLMF **lmf, int *native_offset,
			 gboolean *managed)
{
	MonoJitInfo *ji;
	gpointer ip = MONO_CONTEXT_GET_IP (ctx);
	gpointer *window;

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
		char *source_location, *tmpaddr, *fname;
		gint32 address, iloffset;

		*new_ctx = *ctx;

		address = (char *)ip - (char *)ji->code_start;

		if (native_offset)
			*native_offset = address;

		if (managed)
			if (!ji->method->wrapper_type)
				*managed = TRUE;

		if (trace) {
			source_location = mono_debug_source_location_from_address (ji->method, address, NULL, domain);
			iloffset = mono_debug_il_offset_from_address (ji->method, address, domain);

			if (iloffset < 0)
				tmpaddr = g_strdup_printf ("<0x%05x>", address);
			else
				tmpaddr = g_strdup_printf ("[0x%05x]", iloffset);
		
			fname = mono_method_full_name (ji->method, TRUE);

			if (source_location)
				*trace = g_strdup_printf ("in %s (at %s) %s", tmpaddr, source_location, fname);
			else
				*trace = g_strdup_printf ("in %s %s", tmpaddr, fname);

			g_free (fname);
			g_free (source_location);
			g_free (tmpaddr);
		}

		if (*lmf && (MONO_CONTEXT_GET_BP (ctx) >= (gpointer)(*lmf)->ebp)) {
			/* remove any unused lmf */
			*lmf = (*lmf)->previous_lmf;
		}

		/* Restore ip and sp from the saved register window */
		window = MONO_SPARC_WINDOW_ADDR (ctx->sp);
		new_ctx->ip = window [sparc_i7 - 16];
		new_ctx->sp = (gpointer*)(window [sparc_i6 - 16]);
		new_ctx->fp = (gpointer*)(MONO_SPARC_WINDOW_ADDR (new_ctx->sp) [sparc_i6 - 16]);

		*res = *ji;
		return res;
	}
	else {
		if (!(*lmf))
			return NULL;

		*new_ctx = *ctx;

		if (!(*lmf)->method)
			return (gpointer)-1;

		if (trace)
			*trace = g_strdup_printf ("in (unmanaged) %s", mono_method_full_name ((*lmf)->method, TRUE));
		
		if ((ji = mono_jit_info_table_find (domain, (gpointer)(*lmf)->ip))) {
			*res = *ji;
		} else {
			memset (res, 0, sizeof (MonoJitInfo));
			res->method = (*lmf)->method;
		}

		new_ctx->ip = (*lmf)->ip;
		new_ctx->sp = (*lmf)->sp;
		new_ctx->fp = (*lmf)->ebp;

		*lmf = (*lmf)->previous_lmf;

		return res;
	}
}

gboolean
mono_arch_has_unwind_info (gconstpointer addr)
{
	return FALSE;
}

gboolean
mono_arch_handle_exception (void *sigctx, gpointer obj, gboolean test_only)
{
	MonoContext mctx;
	ucontext_t *ctx = (ucontext_t*)sigctx;
	gpointer *window;

	/*
	 * Access to the machine state using the ucontext_t parameter is somewhat
	 * under documented under solaris. The code below seems to work under
	 * Solaris 9.
	 */
#ifndef __linux__
	g_assert (!ctx->uc_mcontext.gwins);
#else
	/* better, but doesn't work all the time.  need to rethink! */
	g_assert (!ctx->uc_mcontext.gregs);
#endif

	mctx.ip = ctx->uc_mcontext.gregs [REG_PC];
	mctx.sp = ctx->uc_mcontext.gregs [REG_SP];
	window = (gpointer*)(((guint8*)mctx.sp) + MONO_SPARC_STACK_BIAS);
	mctx.fp = window [sparc_fp - 16];

	mono_handle_exception (&mctx, obj, mctx.ip, test_only);
	
	/* We can't use restore_context to return from a signal handler */
	ctx->uc_mcontext.gregs [REG_PC] = mctx.ip;
	ctx->uc_mcontext.gregs [REG_nPC] = mctx.ip + 4;
	ctx->uc_mcontext.gregs [REG_SP] = mctx.sp;
	window = (gpointer*)(((guint8*)mctx.sp) + MONO_SPARC_STACK_BIAS);
	window [sparc_fp - 16] = mctx.fp;

	return TRUE;
}

gpointer
mono_arch_ip_from_context (void *sigctx)
{
	ucontext_t *ctx = (ucontext_t*)sigctx;
	return (gpointer)ctx->uc_mcontext.gregs [REG_PC];
}

