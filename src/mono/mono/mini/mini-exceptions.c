/*
 * mini-exceptions.c: generic exception support
 *
 * Authors:
 *   Dietmar Maurer (dietmar@ximian.com)
 *
 * (C) 2001 Ximian, Inc.
 */

#include <config.h>
#include <glib.h>
#include <signal.h>
#include <string.h>

#include <mono/metadata/appdomain.h>
#include <mono/metadata/tabledefs.h>
#include <mono/metadata/threads.h>
#include <mono/metadata/debug-helpers.h>
#include <mono/metadata/exception.h>
#include <mono/metadata/gc-internal.h>
#include <mono/metadata/mono-debug.h>

#include "mini.h"

#define IS_ON_SIGALTSTACK(jit_tls) ((jit_tls) && ((guint8*)&(jit_tls) > (guint8*)(jit_tls)->signal_stack) && ((guint8*)&(jit_tls) < ((guint8*)(jit_tls)->signal_stack + (jit_tls)->signal_stack_size)))

/* mono_find_jit_info:
 *
 * This function is used to gather information from @ctx. It return the 
 * MonoJitInfo of the corresponding function, unwinds one stack frame and
 * stores the resulting context into @new_ctx. It also stores a string 
 * describing the stack location into @trace (if not NULL), and modifies
 * the @lmf if necessary. @native_offset return the IP offset from the 
 * start of the function or -1 if that info is not available.
 */
static MonoJitInfo *
mono_find_jit_info (MonoDomain *domain, MonoJitTlsData *jit_tls, MonoJitInfo *res, MonoJitInfo *prev_ji, MonoContext *ctx, 
			 MonoContext *new_ctx, char **trace, MonoLMF **lmf, int *native_offset,
			 gboolean *managed)
{
	gboolean managed2;
	gpointer ip = MONO_CONTEXT_GET_IP (ctx);
	MonoJitInfo *ji;

	if (trace)
		*trace = NULL;

	if (native_offset)
		*native_offset = -1;

	if (managed)
		*managed = FALSE;

	ji = mono_arch_find_jit_info (domain, jit_tls, res, prev_ji, ctx, new_ctx, NULL, lmf, NULL, &managed2);

	if (ji == (gpointer)-1)
		return ji;

	if (managed2 || ji->method->wrapper_type) {
		char *source_location, *tmpaddr, *fname;
		gint32 address, iloffset;

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
	}
	else {
		if (trace) {
			char *fname = mono_method_full_name (res->method, TRUE);
			*trace = g_strdup_printf ("in (unmanaged) %s", fname);
			g_free (fname);
		}
	}

	return ji;
}

MonoArray *
ves_icall_get_trace (MonoException *exc, gint32 skip, MonoBoolean need_file_info)
{
	MonoDomain *domain = mono_domain_get ();
	MonoArray *res;
	MonoArray *ta = exc->trace_ips;
	int i, len;

	if (ta == NULL) {
		/* Exception is not thrown yet */
		return mono_array_new (domain, mono_defaults.stack_frame_class, 0);
	}
	
	len = mono_array_length (ta);

	res = mono_array_new (domain, mono_defaults.stack_frame_class, len > skip ? len - skip : 0);

	for (i = skip; i < len; i++) {
		MonoJitInfo *ji;
		MonoStackFrame *sf = (MonoStackFrame *)mono_object_new (domain, mono_defaults.stack_frame_class);
		gpointer ip = mono_array_get (ta, gpointer, i);

		ji = mono_jit_info_table_find (domain, ip);
		if (ji == NULL) {
			/* Unmanaged frame */
			mono_array_set (res, gpointer, i, sf);
			continue;
		}

		g_assert (ji != NULL);

		sf->method = mono_method_get_object (domain, ji->method, NULL);
		sf->native_offset = (char *)ip - (char *)ji->code_start;

		sf->il_offset = mono_debug_il_offset_from_address (ji->method, sf->native_offset, domain);

		if (need_file_info) {
			gchar *filename;
			
			filename = mono_debug_source_location_from_address (ji->method, sf->native_offset, &sf->line, domain);

			sf->filename = filename? mono_string_new (domain, filename): NULL;
			sf->column = 0;

			g_free (filename);
		}

		mono_array_set (res, gpointer, i, sf);
	}

	return res;
}

void
mono_jit_walk_stack (MonoStackWalk func, gboolean do_il_offset, gpointer user_data) {
	MonoDomain *domain = mono_domain_get ();
	MonoJitTlsData *jit_tls = TlsGetValue (mono_jit_tls_id);
	MonoLMF *lmf = jit_tls->lmf;
	MonoJitInfo *ji, rji;
	gint native_offset, il_offset;
	gboolean managed;

	MonoContext ctx, new_ctx;

	mono_arch_flush_register_windows ();

	MONO_CONTEXT_SET_IP (&ctx, __builtin_return_address (0));
	MONO_CONTEXT_SET_BP (&ctx, __builtin_frame_address (1));

	while (MONO_CONTEXT_GET_BP (&ctx) < jit_tls->end_of_stack) {
		
		ji = mono_find_jit_info (domain, jit_tls, &rji, NULL, &ctx, &new_ctx, NULL, &lmf, &native_offset, &managed);
		g_assert (ji);

		if (ji == (gpointer)-1)
			return;

		il_offset = do_il_offset ? mono_debug_il_offset_from_address (ji->method, native_offset, domain): -1;

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
	MonoDomain *domain = mono_domain_get ();
	MonoJitTlsData *jit_tls = TlsGetValue (mono_jit_tls_id);
	MonoLMF *lmf = jit_tls->lmf;
	MonoJitInfo *ji, rji;
	MonoContext ctx, new_ctx;

	mono_arch_flush_register_windows ();

	MONO_CONTEXT_SET_IP (&ctx, ves_icall_get_frame_info);
	MONO_CONTEXT_SET_BP (&ctx, __builtin_frame_address (0));

	skip++;

	do {
		ji = mono_find_jit_info (domain, jit_tls, &rji, NULL, &ctx, &new_ctx, NULL, &lmf, native_offset, NULL);

		ctx = new_ctx;
		
		if (!ji || ji == (gpointer)-1 || MONO_CONTEXT_GET_BP (&ctx) >= jit_tls->end_of_stack)
			return FALSE;

		/* skip all wrappers ??*/
		if (ji->method->wrapper_type == MONO_WRAPPER_RUNTIME_INVOKE ||
		    ji->method->wrapper_type == MONO_WRAPPER_XDOMAIN_INVOKE ||
		    ji->method->wrapper_type == MONO_WRAPPER_XDOMAIN_DISPATCH ||
		    ji->method->wrapper_type == MONO_WRAPPER_REMOTING_INVOKE_WITH_CHECK ||
		    ji->method->wrapper_type == MONO_WRAPPER_REMOTING_INVOKE)
			continue;

		skip--;

	} while (skip >= 0);

	*method = mono_method_get_object (domain, ji->method, NULL);
	*iloffset = mono_debug_il_offset_from_address (ji->method, *native_offset, domain);

	if (need_file_info) {
		gchar *filename;

		filename = mono_debug_source_location_from_address (ji->method, *native_offset, line, domain);

		*file = filename? mono_string_new (domain, filename): NULL;
		*column = 0;

		g_free (filename);
	}

	return TRUE;
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

/**
 * mono_handle_exception:
 * @ctx: saved processor state
 * @obj: the exception object
 * @test_only: only test if the exception is caught, but dont call handlers
 *
 */
gboolean
mono_handle_exception (MonoContext *ctx, gpointer obj, gpointer original_ip, gboolean test_only)
{
	MonoDomain *domain = mono_domain_get ();
	MonoJitInfo *ji, rji;
	static int (*call_filter) (MonoContext *, gpointer) = NULL;
	static void (*restore_context) (void *);
	MonoJitTlsData *jit_tls = TlsGetValue (mono_jit_tls_id);
	MonoLMF *lmf = jit_tls->lmf;		
	GList *trace_ips = NULL;
	MonoException *mono_ex;
	gboolean stack_overflow = FALSE;
	MonoContext initial_ctx;
	int frame_count = 0;
	gboolean gc_disabled = FALSE;
	MonoString *initial_stack_trace = NULL;
	GString *trace_str = NULL;
	
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

	/*
	 * Allocate a new exception object instead of the preconstructed ones.
	 * We can't do this in sigsegv_signal_handler, since GC is not yet
	 * disabled.
	 */
	if (obj == domain->stack_overflow_ex) {
		obj = mono_get_exception_stack_overflow ();
	}
	else if (obj == domain->null_reference_ex) {
		obj = mono_get_exception_null_reference ();
	}

	if (mono_object_isinst (obj, mono_defaults.exception_class)) {
		mono_ex = (MonoException*)obj;
		initial_stack_trace = mono_ex->stack_trace;
	} else {
		mono_ex = NULL;
	}

	if (obj == domain->stack_overflow_ex)
		stack_overflow = TRUE;

	if (!call_filter)
		call_filter = mono_arch_get_call_filter ();

	if (!restore_context)
		restore_context = mono_arch_get_restore_context ();

	g_assert (jit_tls->end_of_stack);
	g_assert (jit_tls->abort_func);

	if (!test_only) {
		MonoContext ctx_cp = *ctx;
		if (mono_jit_trace_calls != NULL)
			g_print ("EXCEPTION handling: %s\n", mono_object_class (obj)->name);
		if (!mono_handle_exception (&ctx_cp, obj, original_ip, TRUE)) {
			if (mono_break_on_exc)
				G_BREAKPOINT ();
			mono_unhandled_exception (obj);

			if (mono_debugger_unhandled_exception (original_ip, MONO_CONTEXT_GET_SP (ctx), obj)) {
				/*
				 * If this returns true, then we're running inside the
				 * Mono Debugger and the debugger wants us to restore the
				 * context and continue (normally, the debugger inserts
				 * a breakpoint on the `original_ip', so it regains control
				 * immediately after restoring the context).
				 */
				MONO_CONTEXT_SET_IP (ctx, original_ip);
				restore_context (ctx);
				g_assert_not_reached ();
			}
		}
	}

	initial_ctx = *ctx;
	memset (&rji, 0, sizeof (rji));

	while (1) {
		MonoContext new_ctx;
		char *trace = NULL;
		gboolean need_trace = FALSE;
		guint32 free_stack;

		if (test_only && (frame_count < 1000)) {
			need_trace = TRUE;
			if (!trace_str)
				trace_str = g_string_new ("");
		}

		ji = mono_find_jit_info (domain, jit_tls, &rji, &rji, ctx, &new_ctx, 
								 need_trace ? &trace : NULL, &lmf, NULL, NULL);
		if (!ji) {
			g_warning ("Exception inside function without unwind info");
			g_assert_not_reached ();
		}

		if (ji != (gpointer)-1) {
			frame_count ++;
			//printf ("M: %s %d %d.\n", mono_method_full_name (ji->method, TRUE), frame_count, test_only);

			if (test_only && ji->method->wrapper_type != MONO_WRAPPER_RUNTIME_INVOKE && mono_ex) {
				/* 
				 * Avoid overwriting the stack trace if the exception is
				 * rethrown. Also avoid giant stack traces during a stack
				 * overflow.
				 */
				if (!initial_stack_trace && (frame_count < 1000)) {
					trace_ips = g_list_prepend (trace_ips, MONO_CONTEXT_GET_IP (ctx));

					g_string_append (trace_str, trace);
					g_string_append_c (trace_str, '\n');
				}
			}

			if (stack_overflow)
				free_stack = (guint8*)(MONO_CONTEXT_GET_BP (ctx)) - (guint8*)(MONO_CONTEXT_GET_BP (&initial_ctx));
			else
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
					gboolean filtered = FALSE;

					if (ei->try_start <= MONO_CONTEXT_GET_IP (ctx) && 
					    MONO_CONTEXT_GET_IP (ctx) <= ei->try_end) { 
						/* catch block */

						if ((ei->flags == MONO_EXCEPTION_CLAUSE_NONE) || (ei->flags == MONO_EXCEPTION_CLAUSE_FILTER)) {
							/* store the exception object int cfg->excvar */
							g_assert (ji->exvar_offset);
							*((gpointer *)((char *)MONO_CONTEXT_GET_BP (ctx) + ji->exvar_offset)) = obj;
							if (!initial_stack_trace && trace_str) {
								mono_ex->stack_trace = mono_string_new (domain, trace_str->str);
							}
						}

						if (ei->flags == MONO_EXCEPTION_CLAUSE_FILTER) {
							mono_debugger_handle_exception (ei->data.filter, MONO_CONTEXT_GET_SP (ctx), obj);
							filtered = call_filter (ctx, ei->data.filter);
						}

						if ((ei->flags == MONO_EXCEPTION_CLAUSE_NONE && 
						     mono_object_isinst (obj, ei->data.catch_class)) || filtered) {
							if (test_only) {
								if (mono_ex) {
									trace_ips = g_list_reverse (trace_ips);
									mono_ex->trace_ips = glist_to_array (trace_ips);
								}
								g_list_free (trace_ips);
								g_free (trace);

								if (gc_disabled)
									mono_gc_enable ();
								if (trace_str)
									g_string_free (trace_str, TRUE);
								return TRUE;
							}
							if (mono_jit_trace_calls != NULL && mono_trace_eval (ji->method))
								g_print ("EXCEPTION: catch found at clause %d of %s\n", i, mono_method_full_name (ji->method, TRUE));
							mono_debugger_handle_exception (ei->handler_start, MONO_CONTEXT_GET_SP (ctx), obj);
							MONO_CONTEXT_SET_IP (ctx, ei->handler_start);
							jit_tls->lmf = lmf;
							g_free (trace);

							if (gc_disabled)
								mono_gc_enable ();
							if (trace_str)
								g_string_free (trace_str, TRUE);
							return 0;
						}
						if (!test_only && ei->try_start <= MONO_CONTEXT_GET_IP (ctx) && 
						    MONO_CONTEXT_GET_IP (ctx) < ei->try_end &&
						    (ei->flags & MONO_EXCEPTION_CLAUSE_FINALLY)) {
							if (mono_jit_trace_calls != NULL && mono_trace_eval (ji->method))
								g_print ("EXCEPTION: finally clause %d of %s\n", i, mono_method_full_name (ji->method, TRUE));
							mono_debugger_handle_exception (ei->handler_start, MONO_CONTEXT_GET_SP (ctx), obj);
							call_filter (ctx, ei->handler_start);
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
						MONO_CONTEXT_SET_SP (&initial_ctx, (guint32)(MONO_CONTEXT_GET_SP (&initial_ctx)) + (64 * 1024));
					MONO_CONTEXT_SET_IP (&initial_ctx, (unsigned int)jit_tls->abort_func);
					restore_context (&initial_ctx);
				}
				else
					jit_tls->abort_func (obj);
				g_assert_not_reached ();
			} else {
				if (mono_ex) {
					trace_ips = g_list_reverse (trace_ips);
					mono_ex->trace_ips = glist_to_array (trace_ips);
				}
				g_list_free (trace_ips);
				if (trace_str)
					g_string_free (trace_str, TRUE);
				return FALSE;
			}
		}
	}

	g_assert_not_reached ();
}
