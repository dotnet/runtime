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

typedef struct MonoContext {
	guint32 ip;
	guint32 *sp;
	guint32 *fp;
} MonoContext;

gboolean  mono_sparc_handle_exception (MonoContext *ctx, gpointer obj, gboolean test_only);

#define MONO_CONTEXT_SET_IP(ctx,eip) do { (ctx)->ip = (long)(eip); } while (0); 
#define MONO_CONTEXT_SET_BP(ctx,ebp) do { (ctx)->fp = (long)(ebp); } while (0); 

#define MONO_CONTEXT_GET_IP(ctx) ((gpointer)((ctx)->ip))
#define MONO_CONTEXT_GET_BP(ctx) ((gpointer)((ctx)->fp))

#define IS_ON_SIGALTSTACK(jit_tls) FALSE

/*
 * arch_get_restore_context:
 *
 * Returns a pointer to a method which restores a previously saved sigcontext.
 */
static gpointer
arch_get_restore_context (void)
{
	static guint32 start [32];
	static int inited = 0;
	guint32 *code;

	if (inited)
		return start;

	code = start;

	sparc_ld_imm (code, sparc_o0, G_STRUCT_OFFSET (MonoContext, ip), sparc_i7);
	sparc_ld_imm (code, sparc_o0, G_STRUCT_OFFSET (MonoContext, sp), sparc_i6);

	sparc_jmpl_imm (code, sparc_i7, 0, sparc_g0);
	/* FIXME: This does not return to the correct window */
	sparc_restore_imm (code, sparc_g0, 0, sparc_g0);

	g_assert ((code - start) < 32);

	inited = 1;

	return start;
}

/*
 * arch_get_call_filter:
 *
 * Returns a pointer to a method which calls an exception filter. We
 * also use this function to call finally handlers (we pass NULL as 
 * @exc object in this case).
 *
 * call_filter (MonoContext *ctx, gpointer ip)
 */
static gpointer
arch_get_call_filter (void)
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
	sparc_save_imm (code, sparc_sp, -160, sparc_sp);

	sparc_mov_reg_reg (code, sparc_i1, sparc_o0);
	sparc_ld_imm (code, sparc_i0, G_STRUCT_OFFSET (MonoContext, sp), sparc_o1);

	/* Create second frame */
	sparc_save_imm (code, sparc_sp, -160, sparc_sp);

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
		sparc_ld_imm (code, sparc_o1, i * 4, sparc_l0 + i);

	/* Save %fp to a location reserved in mono_arch_allocate_vars */
	sparc_st_imm (code, sparc_o7, sparc_fp, -4);

	/* Call the filter code, after this returns, %i0 will hold the result */
	sparc_call_imm (code, sparc_o0, 0);
	sparc_nop (code);

	/* Restore original %fp */
	sparc_ld_imm (code, sparc_fp, -4, sparc_fp);

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
throw_exception (MonoObject *ex, guint32 sp, guint32 ip)
{
	MonoContext ctx;
	static void (*restore_context) (MonoContext *);

	if (!restore_context)
		restore_context = arch_get_restore_context ();

	ctx.sp = (guint32*)sp;
	ctx.ip = ip;
	ctx.fp = (guint32*)ctx.sp [sparc_i6 - 16];

	mono_sparc_handle_exception (&ctx, ex, FALSE);
	restore_context (&ctx);

	g_assert_not_reached ();
}

/**
 * arch_get_throw_exception_by_name:
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

	sparc_save_imm (code, sparc_sp, -160, sparc_sp);

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
 * arch_get_throw_exception_by_name:
 *
 * Returns a function pointer which can be used to raise 
 * corlib exceptions. The returned function has the following 
 * signature: void (*func) (char *exc_name, gpointer ip); 
 */
gpointer 
mono_arch_get_throw_exception_by_name (void)
{
	static guint32 start [32];
	static int inited = 0;
	guint32 *code;

	if (inited)
		return start;

	inited = 1;
	code = start;

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
	sparc_set (code, mono_arch_get_throw_exception (), sparc_g1);
	/* Use a jmp instead of a call so o7 is preserved */
	sparc_jmpl_imm (code, sparc_g1, 0, sparc_g0);
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
static MonoJitInfo *
mono_arch_find_jit_info (MonoDomain *domain, MonoJitTlsData *jit_tls, MonoJitInfo *res, MonoJitInfo *prev_ji, MonoContext *ctx, 
			 MonoContext *new_ctx, char **trace, MonoLMF **lmf, int *native_offset,
			 gboolean *managed)
{
	MonoJitInfo *ji;
	gpointer ip = MONO_CONTEXT_GET_IP (ctx);
	guint32 *window;

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

		/* FIXME: lmf */

		/* Restore ip and sp from the saved register window */
		window = (guint32*)ctx->sp;
		new_ctx->ip = window [sparc_i7 - 16];
		new_ctx->sp = (guint32*)(window [sparc_i6 - 16]);
		new_ctx->fp = (guint32*)(new_ctx->sp [sparc_i6 - 16]);

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

static MonoArray *
glist_to_array (GList *list) 
{
	MonoDomain *domain = mono_domain_get ();
	MonoArray *res;
	int len, i;

	if (!list)
		return NULL;

	len = g_list_length (list);
	res = mono_array_new (domain, mono_defaults.int_class, len);

	for (i = 0; list; list = list->next, i++)
		mono_array_set (res, gpointer, i, list->data);

	return res;
}

MonoArray *
ves_icall_get_trace (MonoException *exc, gint32 skip, MonoBoolean need_file_info)
{
	return NULL;
}

void
mono_jit_walk_stack (MonoStackWalk func, gpointer user_data)
{
	MonoDomain *domain = mono_domain_get ();
	MonoJitTlsData *jit_tls = TlsGetValue (mono_jit_tls_id);
	MonoLMF *lmf = jit_tls->lmf;
	MonoJitInfo *ji, rji;
	gint native_offset, il_offset;
	gboolean managed;

	MonoContext ctx, new_ctx;

	mono_sparc_flushw ();

	MONO_CONTEXT_SET_IP (&ctx, __builtin_return_address (0));
	MONO_CONTEXT_SET_BP (&ctx, __builtin_frame_address (0));

	while (MONO_CONTEXT_GET_BP (&ctx) < jit_tls->end_of_stack) {
		
		ji = mono_arch_find_jit_info (domain, jit_tls, &rji, NULL, &ctx, &new_ctx, NULL, &lmf, &native_offset, &managed);
		g_assert (ji);

		if (ji == (gpointer)-1)
			return;

		il_offset = mono_debug_il_offset_from_address (ji->method, native_offset, domain);

		if (func (ji->method, native_offset, il_offset, managed, user_data))
			return;
		
		ctx = new_ctx;
	}
}

MonoBoolean
ves_icall_get_frame_info (gint32 skip, MonoBoolean need_file_info, 
			  MonoReflectionMethod **method, 
			  gint32 *iloffset, gint32 *native_offset,
			  MonoString **file, gint32 *line, gint32 *column)
{
	return FALSE;
}

/**
 * mono_sparc_handle_exception:
 * @ctx: saved processor state
 * @obj: the exception object
 * @test_only: only test if the exception is caught, but dont call handlers
 *
 *
 */
gboolean
mono_sparc_handle_exception (MonoContext *ctx, gpointer obj, gboolean test_only)
{
	MonoDomain *domain = mono_domain_get ();
	MonoJitInfo *ji, rji;
	static int (*call_filter) (MonoContext *, gpointer) = NULL;
	static void (*restore_context) (MonoContext *);
	MonoJitTlsData *jit_tls = TlsGetValue (mono_jit_tls_id);
	MonoLMF *lmf = jit_tls->lmf;		
	GList *trace_ips = NULL;
	MonoException *mono_ex;
	gboolean stack_overflow = FALSE;
	MonoContext initial_ctx;
	int frame_count = 0;
	gboolean gc_disabled = FALSE;

	/*
	 * This function might execute on an alternate signal stack, and Boehm GC
	 * can't handle that.
	 * Also, since the altstack is small, stack space intensive operations like
	 * JIT compilation should be avoided.
	 */
	if (IS_ON_SIGALTSTACK (jit_tls)) {
		/* 
		 * FIXME: disabling/enabling GC while already on a signal stack might
		 * not be safe either.
		 */
		/* Have to reenable it later */
		gc_disabled = TRUE;
		mono_gc_disable ();
	}

	g_assert (ctx != NULL);
	if (!obj) {
		MonoException *ex = mono_get_exception_null_reference ();
		ex->message = mono_string_new (domain, "Object reference not set to an instance of an object");
		obj = (MonoObject *)ex;
	} 

	if (mono_object_isinst (obj, mono_defaults.exception_class)) {
		mono_ex = (MonoException*)obj;
		mono_ex->stack_trace = NULL;
	} else {
		mono_ex = NULL;
	}

	//printf ("HANDLING EXCEPTION: %s\n", ((MonoObject*)obj)->vtable->klass->name);
	
	if (obj == domain->stack_overflow_ex)
		stack_overflow = TRUE;

	if (!call_filter)
		call_filter = arch_get_call_filter ();

	if (!restore_context)
		restore_context = arch_get_restore_context ();

	g_assert (jit_tls->end_of_stack);
	g_assert (jit_tls->abort_func);

	if (!test_only) {
		MonoContext ctx_cp = *ctx;
		if (mono_jit_trace_calls != NULL)
			g_print ("EXCEPTION handling: %s\n", mono_object_class (obj)->name);
		if (!mono_sparc_handle_exception (&ctx_cp, obj, TRUE)) {
			if (mono_break_on_exc)
				G_BREAKPOINT ();
			mono_unhandled_exception (obj);
		}
	}

	initial_ctx = *ctx;
	memset (&rji, 0, sizeof (rji));

	while (1) {
		MonoContext new_ctx;
		char *trace = NULL;
		gboolean need_trace = FALSE;
		guint32 free_stack;

		if (test_only && (frame_count < 1000))
			need_trace = TRUE;

		ji = mono_arch_find_jit_info (domain, jit_tls, &rji, &rji, ctx, &new_ctx, 
					      need_trace ? &trace : NULL, &lmf, NULL, NULL);
		if (!ji) {
			g_warning ("Exception inside function without unwind info");
			g_assert_not_reached ();
		}

		//printf ("JI: %p IP: %p SP: %p.\n", ji, (gpointer)new_ctx.ip, new_ctx.sp);

		if (ji != (gpointer)-1) {
			frame_count ++;
			//printf ("M: %s %p %p %d.\n", mono_method_full_name (ji->method, TRUE), jit_tls->end_of_stack, ctx->ebp, count);

			if (test_only && ji->method->wrapper_type != MONO_WRAPPER_RUNTIME_INVOKE && mono_ex) {
				char *tmp, *strace;

				/* Avoid giant stack traces */
				if (frame_count < 1000) {
					trace_ips = g_list_append (trace_ips, MONO_CONTEXT_GET_IP (ctx));

					if (!mono_ex->stack_trace)
						strace = g_strdup ("");
					else
						strace = mono_string_to_utf8 (mono_ex->stack_trace);
			
					tmp = g_strdup_printf ("%s%s\n", strace, trace);
					g_free (strace);
					
					mono_ex->stack_trace = mono_string_new (domain, tmp);

					g_free (tmp);
				}
			}

/*
			if (stack_overflow)
				free_stack = (guint8*)(MONO_CONTEXT_GET_BP (ctx)) - (guint8*)(MONO_CONTEXT_GET_BP (&initial_ctx));
			else
				free_stack = 0xffffff;
*/
			free_stack = 0xffffff;

			/* 
			 * During stack overflow, wait till the unwinding frees some stack
			 * space before running handlers/finalizers.
			 */
			if ((free_stack > (64 * 1024)) && ji->num_clauses) {
				int i;
				
				g_assert (ji->clauses);
			
				for (i = 0; i < ji->num_clauses; i++) {
					MonoJitExceptionInfo *ei = &ji->clauses [i];

					if (ei->try_start <= MONO_CONTEXT_GET_IP (ctx) && 
					    MONO_CONTEXT_GET_IP (ctx) <= ei->try_end) { 
						/* catch block */

						if ((ei->flags == MONO_EXCEPTION_CLAUSE_NONE) || (ei->flags == MONO_EXCEPTION_CLAUSE_FILTER)) {
							/* store the exception object int cfg->excvar */
							g_assert (ji->exvar_offset);
							*((gpointer *)((char *)MONO_CONTEXT_GET_BP (ctx) + ji->exvar_offset)) = obj;
						}

						if ((ei->flags == MONO_EXCEPTION_CLAUSE_NONE && 
						     mono_object_isinst (obj, mono_class_get (ji->method->klass->image, ei->data.token))) ||
						    ((ei->flags == MONO_EXCEPTION_CLAUSE_FILTER &&
						      call_filter (ctx, ei->data.filter)))) {
							if (test_only) {
								if (mono_ex)
									mono_ex->trace_ips = glist_to_array (trace_ips);
								g_list_free (trace_ips);
								g_free (trace);

								if (gc_disabled)
									mono_gc_enable ();
								return TRUE;
							}
							if (mono_jit_trace_calls != NULL && mono_trace_eval (ji->method))
								g_print ("EXCEPTION: catch found at clause %d of %s\n", i, mono_method_full_name (ji->method, TRUE));
							MONO_CONTEXT_SET_IP (ctx, ei->handler_start);
							jit_tls->lmf = lmf;
							g_free (trace);

							if (gc_disabled)
								mono_gc_enable ();
							return 0;
						}
						if (!test_only && ei->try_start <= MONO_CONTEXT_GET_IP (ctx) && 
						    MONO_CONTEXT_GET_IP (ctx) < ei->try_end &&
						    (ei->flags & MONO_EXCEPTION_CLAUSE_FINALLY)) {
							if (mono_jit_trace_calls != NULL && mono_trace_eval (ji->method))
								g_print ("EXCEPTION: finally clause %d of %s\n", i, mono_method_full_name (ji->method, TRUE));
							printf ("BEFORE FILTER.\n");
							call_filter (ctx, ei->handler_start);
							printf ("AFTER FILTER.\n");
						}
						
					}
				}
			}
		}

		g_free (trace);
			
		*ctx = new_ctx;

		if ((ji == (gpointer)-1) || MONO_CONTEXT_GET_BP (ctx) >= jit_tls->end_of_stack) {
			if (gc_disabled)
				mono_gc_enable ();

			if (!test_only) {
				jit_tls->lmf = lmf;

				if (IS_ON_SIGALTSTACK (jit_tls)) {
					/* Switch back to normal stack */
					if (stack_overflow)
						/* Free up some stack space */
						initial_ctx.sp += (64 * 1024);
					initial_ctx.ip = (unsigned int)jit_tls->abort_func;
					restore_context (&initial_ctx);
				}
				else
					jit_tls->abort_func (obj);
				g_assert_not_reached ();
			} else {
				if (mono_ex)
					mono_ex->trace_ips = glist_to_array (trace_ips);
				g_list_free (trace_ips);
				return FALSE;
			}
		}
	}

	g_assert_not_reached ();
}

gboolean
mono_arch_handle_exception (ucontext_t *ctx, gpointer obj, gboolean test_only)
{
	MonoContext mctx;

	/*
	 * Access to the machine state using the ucontext_t parameter is somewhat
	 * under documented under solaris. The code below seems to work under
	 * Solaris 9.
	 */
	g_assert (!ctx->uc_mcontext.gwins);

	mctx.ip = ctx->uc_mcontext.gregs [REG_PC];
	mctx.sp = ctx->uc_mcontext.gregs [REG_SP];
	mctx.fp = mctx.sp [sparc_fp - 16];

	mono_sparc_handle_exception (&mctx, obj, test_only);
	
	/* We can't use restore_context to return from a signal handler */
	ctx->uc_mcontext.gregs [REG_PC] = mctx.ip;
	ctx->uc_mcontext.gregs [REG_nPC] = mctx.ip + 4;
	ctx->uc_mcontext.gregs [REG_SP] = mctx.sp;
	mctx.sp [sparc_fp - 16] = mctx.fp;

	return TRUE;
}
