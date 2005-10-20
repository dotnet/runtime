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
#include <mono/metadata/mono-debug-debugger.h>

#include "mini.h"

#ifdef MONO_ARCH_SIGSEGV_ON_ALTSTACK
#include <unistd.h>
#include <sys/mman.h>
#endif

#define IS_ON_SIGALTSTACK(jit_tls) ((jit_tls) && ((guint8*)&(jit_tls) > (guint8*)(jit_tls)->signal_stack) && ((guint8*)&(jit_tls) < ((guint8*)(jit_tls)->signal_stack + (jit_tls)->signal_stack_size)))

#ifndef MONO_ARCH_CONTEXT_DEF
#define MONO_ARCH_CONTEXT_DEF
#endif

#ifndef MONO_INIT_CONTEXT_FROM_CALLER
#define MONO_INIT_CONTEXT_FROM_CALLER(ctx) do { \
	MONO_CONTEXT_SET_IP ((ctx), __builtin_return_address (0)); \
	MONO_CONTEXT_SET_BP ((ctx), __builtin_frame_address (1)); \
} while (0)
#endif

#ifndef mono_find_jit_info

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
		gint32 offset, iloffset;

		if (!managed2)
			/* ctx->ip points into native code */
			offset = (char*)MONO_CONTEXT_GET_IP (new_ctx) - (char*)ji->code_start;
		else
			offset = (char *)ip - (char *)ji->code_start;

		if (native_offset)
			*native_offset = offset;

		if (managed)
			if (!ji->method->wrapper_type)
				*managed = TRUE;

		if (trace) {
			source_location = mono_debug_source_location_from_address (ji->method, offset, NULL, domain);
			iloffset = mono_debug_il_offset_from_address (ji->method, offset, domain);

			if (iloffset < 0)
				tmpaddr = g_strdup_printf ("<0x%05x>", offset);
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

#endif /* mono_find_jit_info */

MonoString *
ves_icall_System_Exception_get_trace (MonoException *ex)
{
	MonoDomain *domain = mono_domain_get ();
	MonoString *res;
	MonoArray *ta = ex->trace_ips;
	int i, len;
	GString *trace_str;
	char tmpaddr [256];

	if (ta == NULL)
		/* Exception is not thrown yet */
		return NULL;

	len = mono_array_length (ta);
	trace_str = g_string_new ("");
	for (i = 0; i < len; i++) {
		MonoJitInfo *ji;
		gpointer ip = mono_array_get (ta, gpointer, i);

		ji = mono_jit_info_table_find (domain, ip);
		if (ji == NULL) {
			/* Unmanaged frame */
			g_string_append_printf (trace_str, "in (unmanaged) %p\n", ip);
		} else {
			char *source_location, *fname;
			gint32 address, iloffset;

			address = (char *)ip - (char *)ji->code_start;

			source_location = mono_debug_source_location_from_address (ji->method, address, NULL, ex->object.vtable->domain);
			iloffset = mono_debug_il_offset_from_address (ji->method, address, ex->object.vtable->domain);

			if (iloffset < 0)
				sprintf (tmpaddr, "<0x%05x>", address);
			else
				sprintf (tmpaddr, "[0x%05x]", iloffset);
		
			fname = mono_method_full_name (ji->method, TRUE);

			if (source_location)
				g_string_append_printf (trace_str, "in %s (at %s) %s\n", tmpaddr, source_location, fname);
			else
				g_string_append_printf (trace_str, "in %s %s\n", tmpaddr, fname);

			g_free (fname);
			g_free (source_location);
		}
	}

	res = mono_string_new (ex->object.vtable->domain, trace_str->str);
	g_string_free (trace_str, TRUE);

	return res;
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

		if (ji->method->wrapper_type) {
			char *s;

			sf->method = NULL;
			s = mono_method_full_name (ji->method, TRUE);
			sf->internal_method_name = mono_string_new (domain, s);
			g_free (s);
		}
		else
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

/**
 * mono_walk_stack:
 * @domain: starting appdomain
 * @jit_tls: JIT data for the thread
 * @start_ctx: starting state of the stack frame
 * @func: callback to call for each stack frame
 * @user_data: data passed to the callback
 *
 * This function walks the stack of a thread, starting from the state
 * represented by jit_tls and start_ctx. For each frame the callback
 * function is called with the relevant info. The walk ends when no more
 * managed stack frames are found or when the callback returns a TRUE value.
 * Note that the function can be used to walk the stack of a thread 
 * different from the current.
 */
void
mono_walk_stack (MonoDomain *domain, MonoJitTlsData *jit_tls, MonoContext *start_ctx, MonoStackFrameWalk func, gpointer user_data)
{
	MonoLMF *lmf = jit_tls->lmf;
	MonoJitInfo *ji, rji;
	gint native_offset;
	gboolean managed;
	MonoContext ctx, new_ctx;

	ctx = *start_ctx;

	while (MONO_CONTEXT_GET_BP (&ctx) < jit_tls->end_of_stack) {
		/* 
		 * FIXME: mono_find_jit_info () will need to be able to return a different
		 * MonoDomain when apddomain transitions are found on the stack.
		 */
		ji = mono_find_jit_info (domain, jit_tls, &rji, NULL, &ctx, &new_ctx, NULL, &lmf, &native_offset, &managed);
		if (!ji || ji == (gpointer)-1)
			return;

		if (func (domain, &new_ctx, ji, user_data))
			return;

		ctx = new_ctx;
	}
}

#ifndef CUSTOM_STACK_WALK

void
mono_jit_walk_stack (MonoStackWalk func, gboolean do_il_offset, gpointer user_data) {
	MonoDomain *domain = mono_domain_get ();
	MonoJitTlsData *jit_tls = TlsGetValue (mono_jit_tls_id);
	MonoLMF *lmf = jit_tls->lmf;
	MonoJitInfo *ji, rji;
	gint native_offset, il_offset;
	gboolean managed;
	MonoContext ctx, new_ctx;

	MONO_ARCH_CONTEXT_DEF

	mono_arch_flush_register_windows ();

#ifdef MONO_INIT_CONTEXT_FROM_CURRENT
	MONO_INIT_CONTEXT_FROM_CURRENT (&ctx);
#else
    MONO_INIT_CONTEXT_FROM_CALLER (&ctx);
#endif

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

	MONO_ARCH_CONTEXT_DEF;

	mono_arch_flush_register_windows ();

	MONO_INIT_CONTEXT_FROM_FUNC (&ctx, ves_icall_get_frame_info);

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

#endif /* CUSTOM_STACK_WALK */

typedef struct {
	guint32 skips;
	MonoSecurityFrame *frame;
} MonoFrameSecurityInfo;

static gboolean
callback_get_first_frame_security_info (MonoDomain *domain, MonoContext *ctx, MonoJitInfo *ji, gpointer data)
{
	MonoFrameSecurityInfo *si = (MonoFrameSecurityInfo*) data;

	/* FIXME: skip all wrappers ?? probably not - case by case testing is required */
	if (ji->method->wrapper_type == MONO_WRAPPER_RUNTIME_INVOKE ||
	    ji->method->wrapper_type == MONO_WRAPPER_XDOMAIN_INVOKE ||
	    ji->method->wrapper_type == MONO_WRAPPER_XDOMAIN_DISPATCH ||
	    ji->method->wrapper_type == MONO_WRAPPER_REMOTING_INVOKE_WITH_CHECK ||
	    ji->method->wrapper_type == MONO_WRAPPER_REMOTING_INVOKE) {
		return FALSE;
	}

	if (si->skips > 0) {
		si->skips--;
		return FALSE;
	}

	si->frame = mono_declsec_create_frame (domain, ji);

	/* Stop - we only want the first frame (e.g. LinkDemand and InheritanceDemand) */
	return TRUE;
}

/**
 * ves_icall_System_Security_SecurityFrame_GetSecurityFrame:
 * @skip: the number of stack frames to skip
 *
 * This function returns a the security informations of a single stack frame 
 * (after the skipped ones). This is required for [NonCas]LinkDemand[Choice]
 * and [NonCas]InheritanceDemand[Choice] as only the caller security is 
 * evaluated.
 */
MonoSecurityFrame*
ves_icall_System_Security_SecurityFrame_GetSecurityFrame (gint32 skip)
{
	MonoDomain *domain = mono_domain_get ();
	MonoJitTlsData *jit_tls = TlsGetValue (mono_jit_tls_id);
	MonoFrameSecurityInfo si;
	MonoContext ctx;

	MONO_ARCH_CONTEXT_DEF

	MONO_INIT_CONTEXT_FROM_FUNC (&ctx, ves_icall_System_Security_SecurityFrame_GetSecurityFrame);

	si.skips = skip;
	si.frame = NULL;
	mono_walk_stack (domain, jit_tls, &ctx, callback_get_first_frame_security_info, (gpointer)&si);

	return (si.skips == 0) ? si.frame : NULL;
}


typedef struct {
	guint32 skips;
	MonoArray *stack;
	guint32 count;
	guint32 maximum;
} MonoSecurityStack;

static void
grow_array (MonoSecurityStack *stack)
{
	MonoDomain *domain = mono_domain_get ();
	guint32 newsize = (stack->maximum << 1);
	MonoArray *newstack = mono_array_new (domain, mono_defaults.runtimesecurityframe_class, newsize);
	int i;
	for (i=0; i < stack->maximum; i++) {
		gpointer frame = mono_array_get (stack->stack, gpointer, i);
		mono_array_set (newstack, gpointer, i, frame);
	}
	stack->maximum = newsize;
	stack->stack = newstack;
}

static gboolean
callback_get_stack_frames_security_info (MonoDomain *domain, MonoContext *ctx, MonoJitInfo *ji, gpointer data)
{
	MonoSecurityStack *ss = (MonoSecurityStack*) data;

	/* FIXME: skip all wrappers ?? probably not - case by case testing is required */
	if (ji->method->wrapper_type == MONO_WRAPPER_RUNTIME_INVOKE ||
	    ji->method->wrapper_type == MONO_WRAPPER_XDOMAIN_INVOKE ||
	    ji->method->wrapper_type == MONO_WRAPPER_XDOMAIN_DISPATCH ||
	    ji->method->wrapper_type == MONO_WRAPPER_REMOTING_INVOKE_WITH_CHECK ||
	    ji->method->wrapper_type == MONO_WRAPPER_REMOTING_INVOKE) {
		return FALSE;
	}

	if (ss->skips > 0) {
		ss->skips--;
		return FALSE;
	}

	if (ss->count == ss->maximum)
		grow_array (ss);
	
	mono_array_set (ss->stack, gpointer, ss->count++, mono_declsec_create_frame (domain, ji));

	/* continue down the stack */
	return FALSE;
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

/**
 * ves_icall_System_Security_SecurityFrame_GetSecurityStack:
 * @skip: the number of stack frames to skip
 *
 * This function returns an managed array of containing the security
 * informations for each frame (after the skipped ones). This is used for
 * [NonCas]Demand[Choice] where the complete evaluation of the stack is 
 * required.
 */
MonoArray*
ves_icall_System_Security_SecurityFrame_GetSecurityStack (gint32 skip)
{
	MonoDomain *domain = mono_domain_get ();
	MonoJitTlsData *jit_tls = TlsGetValue (mono_jit_tls_id);
	MonoSecurityStack ss;
	MonoContext ctx;

	MONO_ARCH_CONTEXT_DEF

	MONO_INIT_CONTEXT_FROM_FUNC (&ctx, ves_icall_System_Security_SecurityFrame_GetSecurityStack);

	ss.skips = skip;
	ss.count = 0;
	ss.maximum = MONO_CAS_INITIAL_STACK_SIZE;
	ss.stack = mono_array_new (domain, mono_defaults.runtimesecurityframe_class, ss.maximum);
	mono_walk_stack (domain, jit_tls, &ctx, callback_get_stack_frames_security_info, (gpointer)&ss);
	/* g_warning ("STACK RESULT: %d out of %d", ss.count, ss.maximum); */
	return ss.stack;
}

#ifndef CUSTOM_EXCEPTION_HANDLING

/**
 * mono_handle_exception_internal:
 * @ctx: saved processor state
 * @obj: the exception object
 * @test_only: only test if the exception is caught, but dont call handlers
 * @out_filter_idx: out parameter. if test_only is true, set to the index of 
 * the first filter clause which caught the exception.
 */
static gboolean
mono_handle_exception_internal (MonoContext *ctx, gpointer obj, gpointer original_ip, gboolean test_only, gint32 *out_filter_idx)
{
	MonoDomain *domain = mono_domain_get ();
	MonoJitInfo *ji, rji;
	static int (*call_filter) (MonoContext *, gpointer) = NULL;
	static void (*restore_context) (void *);
	MonoJitTlsData *jit_tls = TlsGetValue (mono_jit_tls_id);
	MonoLMF *lmf = jit_tls->lmf;		
	MonoArray *initial_trace_ips = NULL;
	GList *trace_ips = NULL;
	MonoException *mono_ex;
	gboolean stack_overflow = FALSE;
	MonoContext initial_ctx;
	int frame_count = 0;
	gboolean gc_disabled = FALSE;
	gboolean has_dynamic_methods = FALSE;
	gint32 filter_idx, first_filter_idx;
	
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
		stack_overflow = TRUE;
	}
	else if (obj == domain->null_reference_ex) {
		obj = mono_get_exception_null_reference ();
	}

	if (mono_object_isinst (obj, mono_defaults.exception_class)) {
		mono_ex = (MonoException*)obj;
		initial_trace_ips = mono_ex->trace_ips;
	} else {
		mono_ex = NULL;
	}

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
		if (!mono_handle_exception_internal (&ctx_cp, obj, original_ip, TRUE, &first_filter_idx)) {
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

	if (out_filter_idx)
		*out_filter_idx = -1;
	filter_idx = 0;
	initial_ctx = *ctx;
	memset (&rji, 0, sizeof (rji));

	while (1) {
		MonoContext new_ctx;
		guint32 free_stack;

		ji = mono_find_jit_info (domain, jit_tls, &rji, &rji, ctx, &new_ctx, 
								 NULL, &lmf, NULL, NULL);
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
				if (!initial_trace_ips && (frame_count < 1000)) {
					trace_ips = g_list_prepend (trace_ips, MONO_CONTEXT_GET_IP (ctx));
				}
			}

			if (ji->method->dynamic)
				has_dynamic_methods = TRUE;

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
				
				for (i = 0; i < ji->num_clauses; i++) {
					MonoJitExceptionInfo *ei = &ji->clauses [i];
					gboolean filtered = FALSE;

#ifdef __s390__
					if (ei->try_start < MONO_CONTEXT_GET_IP (ctx) && 
#else
					if (ei->try_start <= MONO_CONTEXT_GET_IP (ctx) && 
#endif
					    MONO_CONTEXT_GET_IP (ctx) <= ei->try_end) { 
						/* catch block */

						if ((ei->flags == MONO_EXCEPTION_CLAUSE_NONE) || (ei->flags == MONO_EXCEPTION_CLAUSE_FILTER)) {
							/* store the exception object in cfg->excvar */
							g_assert (ei->exvar_offset);
							*((gpointer *)(gpointer)((char *)MONO_CONTEXT_GET_BP (ctx) + ei->exvar_offset)) = obj;
						}

						if (ei->flags == MONO_EXCEPTION_CLAUSE_FILTER) {
							// mono_debugger_handle_exception (ei->data.filter, MONO_CONTEXT_GET_SP (ctx), obj);
							if (test_only) {
								filtered = call_filter (ctx, ei->data.filter);
								if (filtered && out_filter_idx)
									*out_filter_idx = filter_idx;
							}
							else {
								/* 
								 * Filter clauses should only be run in the 
								 * first pass of exception handling.
								 */
								filtered = (filter_idx == first_filter_idx);
							}
							filter_idx ++;
						}

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

								if (gc_disabled)
									mono_gc_enable ();
								return TRUE;
							}
							if (mono_jit_trace_calls != NULL && mono_trace_eval (ji->method))
								g_print ("EXCEPTION: catch found at clause %d of %s\n", i, mono_method_full_name (ji->method, TRUE));
							mono_debugger_handle_exception (ei->handler_start, MONO_CONTEXT_GET_SP (ctx), obj);
							MONO_CONTEXT_SET_IP (ctx, ei->handler_start);
							jit_tls->lmf = lmf;

							if (gc_disabled)
								mono_gc_enable ();
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

		*ctx = new_ctx;

		if ((ji == (gpointer)-1) || MONO_CONTEXT_GET_BP (ctx) >= jit_tls->end_of_stack) {
			if (gc_disabled)
				mono_gc_enable ();

			if (!test_only) {
				jit_tls->lmf = lmf;

				if (IS_ON_SIGALTSTACK (jit_tls)) {
					/* Switch back to normal stack */
					if (stack_overflow) {
						/* Free up some stack space */
						MONO_CONTEXT_SET_SP (&initial_ctx, (gssize)(MONO_CONTEXT_GET_SP (&initial_ctx)) + (64 * 1024));
						g_assert ((gssize)MONO_CONTEXT_GET_SP (&initial_ctx) < (gssize)jit_tls->end_of_stack);
					}
					MONO_CONTEXT_SET_IP (&initial_ctx, (gssize)jit_tls->abort_func);
					restore_context (&initial_ctx);
				}
				else
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

/**
 * mono_debugger_run_finally:
 * @start_ctx: saved processor state
 *
 * This method is called by the Mono Debugger to call all `finally' clauses of the
 * current stack frame.  It's used when the user issues a `return' command to make
 * the current stack frame return.  After returning from this method, the debugger
 * unwinds the stack one frame and gives control back to the user.
 *
 * NOTE: This method is only used when running inside the Mono Debugger.
 */
void
mono_debugger_run_finally (MonoContext *start_ctx)
{
	static int (*call_filter) (MonoContext *, gpointer) = NULL;
	MonoDomain *domain = mono_domain_get ();
	MonoJitTlsData *jit_tls = TlsGetValue (mono_jit_tls_id);
	MonoLMF *lmf = jit_tls->lmf;
	MonoContext ctx, new_ctx;
	MonoJitInfo *ji, rji;
	int i;

	ctx = *start_ctx;

	ji = mono_find_jit_info (domain, jit_tls, &rji, NULL, &ctx, &new_ctx, NULL, &lmf, NULL, NULL);
	if (!ji || ji == (gpointer)-1)
		return;

	if (!call_filter)
		call_filter = mono_arch_get_call_filter ();

	for (i = 0; i < ji->num_clauses; i++) {
		MonoJitExceptionInfo *ei = &ji->clauses [i];

		if ((ei->try_start <= MONO_CONTEXT_GET_IP (&ctx)) && 
		    (MONO_CONTEXT_GET_IP (&ctx) < ei->try_end) &&
		    (ei->flags & MONO_EXCEPTION_CLAUSE_FINALLY)) {
			call_filter (&ctx, ei->handler_start);
		}
	}
}

/**
 * mono_handle_exception:
 * @ctx: saved processor state
 * @obj: the exception object
 * @test_only: only test if the exception is caught, but dont call handlers
 */
gboolean
mono_handle_exception (MonoContext *ctx, gpointer obj, gpointer original_ip, gboolean test_only)
{
	return mono_handle_exception_internal (ctx, obj, original_ip, test_only, NULL);
}

#endif /* CUSTOM_EXCEPTION_HANDLING */

#ifdef MONO_ARCH_SIGSEGV_ON_ALTSTACK

void
mono_setup_altstack (MonoJitTlsData *tls)
{
	pthread_t self = pthread_self();
	pthread_attr_t attr;
	size_t stsize = 0;
	struct sigaltstack sa;
	guint8 *staddr = NULL;
	guint8 *current = (guint8*)&staddr;

	if (mono_running_on_valgrind ())
		return;

	/* Determine stack boundaries */
	pthread_attr_init( &attr );
#ifdef HAVE_PTHREAD_GETATTR_NP
	pthread_getattr_np( self, &attr );
#else
#ifdef HAVE_PTHREAD_ATTR_GET_NP
	pthread_attr_get_np( self, &attr );
#elif defined(sun)
	pthread_attr_getstacksize( &attr, &stsize );
#else
#error "Not implemented"
#endif
#endif
#ifndef sun
	pthread_attr_getstack( &attr, (void**)&staddr, &stsize );
#endif

	g_assert (staddr);

	g_assert ((current > staddr) && (current < staddr + stsize));

	tls->end_of_stack = staddr + stsize;

	/*
	 * threads created by nptl does not seem to have a guard page, and
	 * since the main thread is not created by us, we can't even set one.
	 * Increasing stsize fools the SIGSEGV signal handler into thinking this
	 * is a stack overflow exception.
	 */
	tls->stack_size = stsize + getpagesize ();

	/* Setup an alternate signal stack */
	tls->signal_stack = mmap (0, MONO_ARCH_SIGNAL_STACK_SIZE, PROT_READ|PROT_WRITE|PROT_EXEC, MAP_PRIVATE|MAP_ANONYMOUS, -1, 0);
	tls->signal_stack_size = MONO_ARCH_SIGNAL_STACK_SIZE;

	g_assert (tls->signal_stack);

	sa.ss_sp = tls->signal_stack;
	sa.ss_size = MONO_ARCH_SIGNAL_STACK_SIZE;
	sa.ss_flags = SS_ONSTACK;
	sigaltstack (&sa, NULL);
}

void
mono_free_altstack (MonoJitTlsData *tls)
{
	struct sigaltstack sa;
	int err;

	sa.ss_sp = tls->signal_stack;
	sa.ss_size = MONO_ARCH_SIGNAL_STACK_SIZE;
	sa.ss_flags = SS_DISABLE;
	err = sigaltstack  (&sa, NULL);
	g_assert (err == 0);

	if (tls->signal_stack)
		munmap (tls->signal_stack, MONO_ARCH_SIGNAL_STACK_SIZE);
}

#endif /* MONO_ARCH_SIGSEGV_ON_ALTSTACK */

static gboolean
print_stack_frame (MonoMethod *method, gint32 native_offset, gint32 il_offset, gboolean managed, gpointer data)
{
	if (method) {
		if (il_offset != -1)
			printf ("in [0x%lx] %s\n", (long)il_offset, mono_method_full_name (method, TRUE));
		else
			printf ("in <0x%lx> %s\n", (long)native_offset, mono_method_full_name (method, TRUE));
	} else
		printf ("in <%lx> <unknown>\n", (long)native_offset);

	return FALSE;
}

/*
 * mono_handle_native_sigsegv:
 *
 *   Handle a SIGSEGV received while in native code by printing diagnostic 
 * information and aborting.
 */
void
mono_handle_native_sigsegv (void *ctx)
{
	/*
	 * A SIGSEGV indicates something went very wrong so we can no longer depend
	 * on anything working. So try to print out lots of diagnostics, starting 
	 * with ones which have a greater change of working.
	 */
	fprintf (stderr,
			 "\n"
			 "=================================================================\n"
			 "Got a SIGSEGV while executing native code. This usually indicates\n"
			 "a fatal error in the mono runtime or one of the native libraries \n"
			 "used by your application.\n"
			 "=================================================================\n"
			 "\n");

	fprintf (stderr, "Stacktrace:\n\n");

	mono_jit_walk_stack (print_stack_frame, TRUE, NULL);

	abort ();
}
