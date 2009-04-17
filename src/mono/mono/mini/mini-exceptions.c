/*
 * mini-exceptions.c: generic exception support
 *
 * Authors:
 *   Dietmar Maurer (dietmar@ximian.com)
 *   Mono Team (mono-list@lists.ximian.com)
 *
 * Copyright 2001-2003 Ximian, Inc.
 * Copyright 2003-2008 Ximian, Inc.
 */

#include <config.h>
#include <glib.h>
#include <signal.h>
#include <string.h>

#ifdef HAVE_EXECINFO_H
#include <execinfo.h>
#endif

#ifdef HAVE_SYS_TYPES_H
#include <sys/types.h>
#endif

#ifdef HAVE_SYS_WAIT_H
#include <sys/wait.h>
#endif

#ifdef HAVE_UNISTD_H
#include <unistd.h>
#endif

#ifdef HAVE_SYS_SYSCALL_H
#include <sys/syscall.h>
#endif

#include <mono/metadata/appdomain.h>
#include <mono/metadata/tabledefs.h>
#include <mono/metadata/threads.h>
#include <mono/metadata/threads-types.h>
#include <mono/metadata/debug-helpers.h>
#include <mono/metadata/exception.h>
#include <mono/metadata/gc-internal.h>
#include <mono/metadata/mono-debug.h>
#include <mono/metadata/profiler.h>
#include <mono/utils/mono-mmap.h>

#include "mini.h"
#include "debug-mini.h"
#include "trace.h"

#ifndef MONO_ARCH_CONTEXT_DEF
#define MONO_ARCH_CONTEXT_DEF
#endif

static gpointer restore_context_func, call_filter_func;
static gpointer throw_exception_func, rethrow_exception_func;
static gpointer throw_exception_by_name_func, throw_corlib_exception_func;

static gpointer try_more_restore_tramp = NULL;
static gpointer restore_stack_protection_tramp = NULL;

static void try_more_restore (void);
static void restore_stack_protection (void);

void
mono_exceptions_init (void)
{
#ifdef MONO_ARCH_HAVE_FULL_AOT_TRAMPOLINES
	guint32 code_size;
	MonoJumpInfo *ji;

	if (mono_aot_only) {
		restore_context_func = mono_aot_get_named_code ("restore_context");
		call_filter_func = mono_aot_get_named_code ("call_filter");
		throw_exception_func = mono_aot_get_named_code ("throw_exception");
		rethrow_exception_func = mono_aot_get_named_code ("rethrow_exception");
	} else {
		restore_context_func = mono_arch_get_restore_context_full (&code_size, &ji, FALSE);
		call_filter_func = mono_arch_get_call_filter_full (&code_size, &ji, FALSE);
		throw_exception_func = mono_arch_get_throw_exception_full (&code_size, &ji, FALSE);
		rethrow_exception_func = mono_arch_get_rethrow_exception_full (&code_size, &ji, FALSE);
	}
#else
	restore_context_func = mono_arch_get_restore_context ();
	call_filter_func = mono_arch_get_call_filter ();
	throw_exception_func = mono_arch_get_throw_exception ();
	rethrow_exception_func = mono_arch_get_rethrow_exception ();
#endif
#ifdef MONO_ARCH_HAVE_RESTORE_STACK_SUPPORT
	try_more_restore_tramp = mono_create_specific_trampoline (try_more_restore, MONO_TRAMPOLINE_RESTORE_STACK_PROT, mono_domain_get (), NULL);
	restore_stack_protection_tramp = mono_create_specific_trampoline (restore_stack_protection, MONO_TRAMPOLINE_RESTORE_STACK_PROT, mono_domain_get (), NULL);
#endif

#ifdef MONO_ARCH_HAVE_EXCEPTIONS_INIT
	mono_arch_exceptions_init ();
#endif
}

gpointer
mono_get_throw_exception (void)
{
	g_assert (throw_exception_func);
	return throw_exception_func;
}

gpointer
mono_get_rethrow_exception (void)
{
	g_assert (rethrow_exception_func);
	return rethrow_exception_func;
}

gpointer
mono_get_call_filter (void)
{
	g_assert (call_filter_func);
	return call_filter_func;
}

gpointer
mono_get_restore_context (void)
{
	g_assert (restore_context_func);
	return restore_context_func;
}

gpointer
mono_get_throw_exception_by_name (void)
{
	gpointer code = NULL;
#ifdef MONO_ARCH_HAVE_FULL_AOT_TRAMPOLINES
	guint32 code_size;
	MonoJumpInfo *ji;
#endif

	/* This depends on corlib classes so cannot be inited in mono_exceptions_init () */
	if (throw_exception_by_name_func)
		return throw_exception_by_name_func;

#ifdef MONO_ARCH_HAVE_FULL_AOT_TRAMPOLINES
	if (mono_aot_only)
		code = mono_aot_get_named_code ("throw_exception_by_name");
	else
		code = mono_arch_get_throw_exception_by_name_full (&code_size, &ji, FALSE);
#else
		code = mono_arch_get_throw_exception_by_name ();
#endif

	mono_memory_barrier ();

	throw_exception_by_name_func = code;

	return throw_exception_by_name_func;
}

gpointer
mono_get_throw_corlib_exception (void)
{
	gpointer code = NULL;
#ifdef MONO_ARCH_HAVE_FULL_AOT_TRAMPOLINES
	guint32 code_size;
	MonoJumpInfo *ji;
#endif

	/* This depends on corlib classes so cannot be inited in mono_exceptions_init () */
	if (throw_corlib_exception_func)
		return throw_corlib_exception_func;

#if MONO_ARCH_HAVE_THROW_CORLIB_EXCEPTION
#ifdef MONO_ARCH_HAVE_FULL_AOT_TRAMPOLINES
	if (mono_aot_only)
		code = mono_aot_get_named_code ("throw_corlib_exception");
	else
		code = mono_arch_get_throw_corlib_exception_full (&code_size, &ji, FALSE);
#else
		code = mono_arch_get_throw_corlib_exception ();
#endif
#else
	g_assert_not_reached ();
#endif

	mono_memory_barrier ();

	throw_corlib_exception_func = code;

	return throw_corlib_exception_func;
}

/* mono_find_jit_info:
 *
 * This function is used to gather information from @ctx. It return the 
 * MonoJitInfo of the corresponding function, unwinds one stack frame and
 * stores the resulting context into @new_ctx. It also stores a string 
 * describing the stack location into @trace (if not NULL), and modifies
 * the @lmf if necessary. @native_offset return the IP offset from the 
 * start of the function or -1 if that info is not available.
 */
MonoJitInfo *
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

	ji = mono_arch_find_jit_info (domain, jit_tls, res, prev_ji, ctx, new_ctx, lmf, &managed2);

	if (ji == (gpointer)-1)
		return ji;

	if (managed2 || ji->method->wrapper_type) {
		const char *real_ip, *start;
		gint32 offset;

		start = (const char *)ji->code_start;
		if (!managed2)
			/* ctx->ip points into native code */
			real_ip = (const char*)MONO_CONTEXT_GET_IP (new_ctx);
		else
			real_ip = (const char*)ip;

		if ((real_ip >= start) && (real_ip <= start + ji->code_size))
			offset = real_ip - start;
		else
			offset = -1;

		if (native_offset)
			*native_offset = offset;

		if (managed)
			if (!ji->method->wrapper_type)
				*managed = TRUE;

		if (trace)
			*trace = mono_debug_print_stack_frame (ji->method, offset, domain);
	} else {
		if (trace) {
			char *fname = mono_method_full_name (res->method, TRUE);
			*trace = g_strdup_printf ("in (unmanaged) %s", fname);
			g_free (fname);
		}
	}

	return ji;
}

static gpointer
get_generic_info_from_stack_frame (MonoJitInfo *ji, MonoContext *ctx)
{
	MonoGenericJitInfo *gi;
	gpointer info;

	if (!ji->has_generic_jit_info)
		return NULL;
	gi = mono_jit_info_get_generic_jit_info (ji);
	if (!gi->has_this)
		return NULL;

	if (gi->this_in_reg)
		info = mono_arch_context_get_int_reg (ctx, gi->this_reg);
	else
		info = *(gpointer*)(gpointer)((char*)mono_arch_context_get_int_reg (ctx, gi->this_reg) +
									  gi->this_offset);
	if (mono_method_get_context (ji->method)->method_inst) {
		return info;
	} else if ((ji->method->flags & METHOD_ATTRIBUTE_STATIC) || ji->method->klass->valuetype) {
		return info;
	} else {
		/* Avoid returning a managed object */
		MonoObject *this_obj = info;

		return this_obj->vtable->klass;
	}
}

static MonoGenericContext
get_generic_context_from_stack_frame (MonoJitInfo *ji, gpointer generic_info)
{
	MonoGenericContext context = { NULL, NULL };
	MonoClass *class, *method_container_class;

	g_assert (generic_info);

	g_assert (ji->method->is_inflated);
	if (mono_method_get_context (ji->method)->method_inst) {
		MonoMethodRuntimeGenericContext *mrgctx = generic_info;

		class = mrgctx->class_vtable->klass;
		context.method_inst = mrgctx->method_inst;
		g_assert (context.method_inst);
	} else if ((ji->method->flags & METHOD_ATTRIBUTE_STATIC) || ji->method->klass->valuetype) {
		MonoVTable *vtable = generic_info;

		class = vtable->klass;
	} else {
		class = generic_info;
	}

	if (class->generic_class || class->generic_container)
		context.class_inst = mini_class_get_context (class)->class_inst;

	g_assert (!ji->method->klass->generic_container);
	if (ji->method->klass->generic_class)
		method_container_class = ji->method->klass->generic_class->container_class;
	else
		method_container_class = ji->method->klass;

	if (class->generic_class)
		g_assert (mono_class_has_parent_and_ignore_generics (class->generic_class->container_class, method_container_class));
	else
		g_assert (mono_class_has_parent_and_ignore_generics (class, method_container_class));

	return context;
}

static MonoMethod*
get_method_from_stack_frame (MonoJitInfo *ji, gpointer generic_info)
{
	MonoGenericContext context;
	MonoMethod *method;

	if (!ji->has_generic_jit_info || !mono_jit_info_get_generic_jit_info (ji)->has_this)
		return ji->method;
	context = get_generic_context_from_stack_frame (ji, generic_info);

	method = mono_method_get_declaring_generic_method (ji->method);
	method = mono_class_inflate_generic_method (method, &context);

	return method;
}

MonoString *
ves_icall_System_Exception_get_trace (MonoException *ex)
{
	MonoDomain *domain = mono_domain_get ();
	MonoString *res;
	MonoArray *ta = ex->trace_ips;
	int i, len;
	GString *trace_str;

	if (ta == NULL)
		/* Exception is not thrown yet */
		return NULL;

	len = mono_array_length (ta) >> 1;
	trace_str = g_string_new ("");
	for (i = 0; i < len; i++) {
		MonoJitInfo *ji;
		gpointer ip = mono_array_get (ta, gpointer, i * 2 + 0);
		gpointer generic_info = mono_array_get (ta, gpointer, i * 2 + 1);

		ji = mono_jit_info_table_find (domain, ip);
		if (ji == NULL) {
			/* Unmanaged frame */
			g_string_append_printf (trace_str, "in (unmanaged) %p\n", ip);
		} else {
			gchar *location;
			gint32 address;
			MonoMethod *method = get_method_from_stack_frame (ji, generic_info);

			address = (char *)ip - (char *)ji->code_start;
			location = mono_debug_print_stack_frame (
				method, address, ex->object.vtable->domain);

			g_string_append_printf (trace_str, "%s\n", location);
			g_free (location);
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
	MonoDebugSourceLocation *location;
	int i, len;

	if (ta == NULL) {
		/* Exception is not thrown yet */
		return mono_array_new (domain, mono_defaults.stack_frame_class, 0);
	}

	len = mono_array_length (ta) >> 1;

	res = mono_array_new (domain, mono_defaults.stack_frame_class, len > skip ? len - skip : 0);

	for (i = skip; i < len; i++) {
		MonoJitInfo *ji;
		MonoStackFrame *sf = (MonoStackFrame *)mono_object_new (domain, mono_defaults.stack_frame_class);
		gpointer ip = mono_array_get (ta, gpointer, i * 2 + 0);
		gpointer generic_info = mono_array_get (ta, gpointer, i * 2 + 1);
		MonoMethod *method;

		ji = mono_jit_info_table_find (domain, ip);
		if (ji == NULL) {
			/* Unmanaged frame */
			mono_array_setref (res, i, sf);
			continue;
		}

		g_assert (ji != NULL);

		method = get_method_from_stack_frame (ji, generic_info);
		if (ji->method->wrapper_type) {
			char *s;

			sf->method = NULL;
			s = mono_method_full_name (method, TRUE);
			MONO_OBJECT_SETREF (sf, internal_method_name, mono_string_new (domain, s));
			g_free (s);
		}
		else
			MONO_OBJECT_SETREF (sf, method, mono_method_get_object (domain, method, NULL));
		sf->native_offset = (char *)ip - (char *)ji->code_start;

		/*
		 * mono_debug_lookup_source_location() returns both the file / line number information
		 * and the IL offset.  Note that computing the IL offset is already an expensive
		 * operation, so we shouldn't call this method twice.
		 */
		location = mono_debug_lookup_source_location (ji->method, sf->native_offset, domain);
		if (location)
			sf->il_offset = location->il_offset;
		else
			sf->il_offset = 0;

		if (need_file_info) {
			if (location && location->source_file) {
				MONO_OBJECT_SETREF (sf, filename, mono_string_new (domain, location->source_file));
				sf->line = location->row;
				sf->column = location->column;
			} else {
				sf->line = sf->column = 0;
				sf->filename = NULL;
			}
		}

		mono_debug_free_source_location (location);
		mono_array_setref (res, i, sf);
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
	MonoLMF *lmf = mono_get_lmf ();
	MonoJitInfo *ji, rji;
	gint native_offset;
	gboolean managed;
	MonoContext ctx, new_ctx;

	ctx = *start_ctx;

	while (MONO_CONTEXT_GET_SP (&ctx) < jit_tls->end_of_stack) {
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

void
mono_jit_walk_stack_from_ctx (MonoStackWalk func, MonoContext *start_ctx, gboolean do_il_offset, gpointer user_data)
{
	MonoDomain *domain = mono_domain_get ();
	MonoJitTlsData *jit_tls = TlsGetValue (mono_jit_tls_id);
	MonoLMF *lmf = mono_get_lmf ();
	MonoJitInfo *ji, rji;
	gint native_offset, il_offset;
	gboolean managed;
	MonoContext ctx, new_ctx;

	MONO_ARCH_CONTEXT_DEF

	mono_arch_flush_register_windows ();

	if (start_ctx) {
		memcpy (&ctx, start_ctx, sizeof (MonoContext));
	} else {
#ifdef MONO_INIT_CONTEXT_FROM_CURRENT
	MONO_INIT_CONTEXT_FROM_CURRENT (&ctx);
#else
    MONO_INIT_CONTEXT_FROM_FUNC (&ctx, mono_jit_walk_stack_from_ctx);
#endif
	}

	while (MONO_CONTEXT_GET_SP (&ctx) < jit_tls->end_of_stack) {
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
	}
}

void
mono_jit_walk_stack (MonoStackWalk func, gboolean do_il_offset, gpointer user_data)
{
	mono_jit_walk_stack_from_ctx (func, NULL, do_il_offset, user_data);
}

MonoBoolean
ves_icall_get_frame_info (gint32 skip, MonoBoolean need_file_info, 
			  MonoReflectionMethod **method, 
			  gint32 *iloffset, gint32 *native_offset,
			  MonoString **file, gint32 *line, gint32 *column)
{
	MonoDomain *domain = mono_domain_get ();
	MonoJitTlsData *jit_tls = TlsGetValue (mono_jit_tls_id);
	MonoLMF *lmf = mono_get_lmf ();
	MonoJitInfo *ji, rji;
	MonoContext ctx, new_ctx, ji_ctx;
	MonoDebugSourceLocation *location;
	MonoMethod *last_method = NULL, *actual_method;

	MONO_ARCH_CONTEXT_DEF;

	mono_arch_flush_register_windows ();

#ifdef MONO_INIT_CONTEXT_FROM_CURRENT
	MONO_INIT_CONTEXT_FROM_CURRENT (&ctx);
#else
	MONO_INIT_CONTEXT_FROM_FUNC (&ctx, ves_icall_get_frame_info);
#endif

	do {
		ji_ctx = ctx;
		ji = mono_find_jit_info (domain, jit_tls, &rji, NULL, &ctx, &new_ctx, NULL, &lmf, (int*) native_offset, NULL);
		ctx = new_ctx;

		if (ji && ji != (gpointer)-1 &&
				MONO_CONTEXT_GET_IP (&ctx) >= ji->code_start &&
				(guint8*)MONO_CONTEXT_GET_IP (&ctx) < (guint8*)ji->code_start + ji->code_size) {
			ji_ctx = ctx;
		}

		if (!ji || ji == (gpointer)-1 || MONO_CONTEXT_GET_SP (&ctx) >= jit_tls->end_of_stack)
			return FALSE;

		/* skip all wrappers ??*/
		if (ji->method->wrapper_type == MONO_WRAPPER_RUNTIME_INVOKE ||
		    ji->method->wrapper_type == MONO_WRAPPER_XDOMAIN_INVOKE ||
		    ji->method->wrapper_type == MONO_WRAPPER_XDOMAIN_DISPATCH ||
		    ji->method->wrapper_type == MONO_WRAPPER_REMOTING_INVOKE_WITH_CHECK ||
		    ji->method->wrapper_type == MONO_WRAPPER_REMOTING_INVOKE ||
			ji->method->wrapper_type == MONO_WRAPPER_NATIVE_TO_MANAGED)
			continue;

		if (ji->method->wrapper_type == MONO_WRAPPER_MANAGED_TO_NATIVE && ji->method == last_method) {
			/*
			 * FIXME: Native-to-managed wrappers sometimes show up twice.
			 * Probably the whole mono_find_jit_info () stuff needs to be fixed so this 
			 * isn't needed.
			 */
			continue;
		}

		last_method = ji->method;

		skip--;

	} while (skip >= 0);

	actual_method = get_method_from_stack_frame (ji, get_generic_info_from_stack_frame (ji, &ji_ctx));

	*method = mono_method_get_object (domain, actual_method, NULL);

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

#ifdef MONO_INIT_CONTEXT_FROM_CURRENT
	MONO_INIT_CONTEXT_FROM_CURRENT (&ctx);
#else
	MONO_INIT_CONTEXT_FROM_FUNC (&ctx, ves_icall_System_Security_SecurityFrame_GetSecurityFrame);
#endif

#if	defined(__ia64__) || defined(__s390__) || defined(__s390x__)
	skip--;
#endif

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
		mono_array_setref (newstack, i, frame);
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

	mono_array_setref (ss->stack, ss->count++, mono_declsec_create_frame (domain, ji));

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

#ifdef MONO_INIT_CONTEXT_FROM_CURRENT
	MONO_INIT_CONTEXT_FROM_CURRENT (&ctx);
#else
	MONO_INIT_CONTEXT_FROM_FUNC (&ctx, ves_icall_System_Security_SecurityFrame_GetSecurityStack);
#endif

#if	defined(__ia64__) || defined(__s390__) || defined(__s390x__)
	skip--;
#endif

	ss.skips = skip;
	ss.count = 0;
	ss.maximum = MONO_CAS_INITIAL_STACK_SIZE;
	ss.stack = mono_array_new (domain, mono_defaults.runtimesecurityframe_class, ss.maximum);
	mono_walk_stack (domain, jit_tls, &ctx, callback_get_stack_frames_security_info, (gpointer)&ss);
	/* g_warning ("STACK RESULT: %d out of %d", ss.count, ss.maximum); */
	return ss.stack;
}

static MonoClass*
get_exception_catch_class (MonoJitExceptionInfo *ei, MonoJitInfo *ji, MonoContext *ctx)
{
	MonoClass *catch_class = ei->data.catch_class;
	MonoType *inflated_type;
	MonoGenericContext context;

	if (!catch_class)
		return NULL;

	if (!ji->has_generic_jit_info || !mono_jit_info_get_generic_jit_info (ji)->has_this)
		return catch_class;
	context = get_generic_context_from_stack_frame (ji, get_generic_info_from_stack_frame (ji, ctx));

	/* FIXME: we shouldn't inflate but instead put the
	   type in the rgctx and fetch it from there.  It
	   might be a good idea to do this lazily, i.e. only
	   when the exception is actually thrown, so as not to
	   waste space for exception clauses which might never
	   be encountered. */
	inflated_type = mono_class_inflate_generic_type (&catch_class->byval_arg, &context);
	catch_class = mono_class_from_mono_type (inflated_type);
	mono_metadata_free_type (inflated_type);

	return catch_class;
}

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
	MonoLMF *lmf = mono_get_lmf ();
	MonoArray *initial_trace_ips = NULL;
	GList *trace_ips = NULL;
	MonoException *mono_ex;
	gboolean stack_overflow = FALSE;
	MonoContext initial_ctx;
	int frame_count = 0;
	gboolean has_dynamic_methods = FALSE;
	gint32 filter_idx, first_filter_idx;

	g_assert (ctx != NULL);
	if (!obj) {
		MonoException *ex = mono_get_exception_null_reference ();
		MONO_OBJECT_SETREF (ex, message, mono_string_new (domain, "Object reference not set to an instance of an object"));
		obj = (MonoObject *)ex;
	} 

	/*
	 * Allocate a new exception object instead of the preconstructed ones.
	 */
	if (obj == domain->stack_overflow_ex) {
		/*
		 * It is not a good idea to try and put even more pressure on the little stack available.
		 * obj = mono_get_exception_stack_overflow ();
		 */
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

	if (mono_ex && jit_tls->class_cast_from && !strcmp (mono_ex->object.vtable->klass->name, "InvalidCastException")) {
		char *from_name = mono_type_get_full_name (jit_tls->class_cast_from);
		char *to_name = mono_type_get_full_name (jit_tls->class_cast_to);
		char *msg = g_strdup_printf ("Unable to cast object of type '%s' to type '%s'.", from_name, to_name);
		mono_ex->message = mono_string_new (domain, msg);
		g_free (from_name);
		g_free (to_name);
		g_free (msg);
	}

	if (!call_filter)
		call_filter = mono_get_call_filter ();

	if (!restore_context)
		restore_context = mono_get_restore_context ();

	g_assert (jit_tls->end_of_stack);
	g_assert (jit_tls->abort_func);

	if (!test_only) {
		MonoContext ctx_cp = *ctx;
		if (mono_trace_is_enabled ())
			g_print ("[%p:] EXCEPTION handling: %s\n", (void*)GetCurrentThreadId (), mono_object_class (obj)->name);
		mono_profiler_exception_thrown (obj);
		if (!mono_handle_exception_internal (&ctx_cp, obj, original_ip, TRUE, &first_filter_idx)) {
			if (mono_break_on_exc)
				G_BREAKPOINT ();
			// FIXME: This runs managed code so it might cause another stack overflow when
			// we are handling a stack overflow
			mono_unhandled_exception (obj);
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

		if (ji != (gpointer)-1 && !(ji->code_start <= MONO_CONTEXT_GET_IP (ctx) && (((guint8*)ji->code_start + ji->code_size >= (guint8*)MONO_CONTEXT_GET_IP (ctx))))) {
			/*
			 * The exception was raised in native code and we got back to managed code 
			 * using the LMF.
			 */
			*ctx = new_ctx;
			continue;
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
					trace_ips = g_list_prepend (trace_ips,
						get_generic_info_from_stack_frame (ji, ctx));
				}
			}

			if (ji->method->dynamic)
				has_dynamic_methods = TRUE;

			if (stack_overflow)
#ifndef MONO_ARCH_STACK_GROWS_UP
				free_stack = (guint8*)(MONO_CONTEXT_GET_SP (ctx)) - (guint8*)(MONO_CONTEXT_GET_SP (&initial_ctx));
#else
				free_stack = (guint8*)(MONO_CONTEXT_GET_SP (&initial_ctx)) - (guint8*)(MONO_CONTEXT_GET_SP (ctx));
#endif
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

#if defined(__s390__)
					/* 
					 * This is required in cases where a try block starts immediately after
					 * a call which causes an exception. Testcase: tests/exception8.cs.
					 * FIXME: Clean this up.
					 */
					if (ei->try_start < MONO_CONTEXT_GET_IP (ctx) && 
#else
					if (ei->try_start <= MONO_CONTEXT_GET_IP (ctx) && 
#endif
					    MONO_CONTEXT_GET_IP (ctx) <= ei->try_end) { 
						/* catch block */
						MonoClass *catch_class = get_exception_catch_class (ei, ji, ctx);

						if ((ei->flags == MONO_EXCEPTION_CLAUSE_NONE) || (ei->flags == MONO_EXCEPTION_CLAUSE_FILTER)) {
							/* store the exception object in bp + ei->exvar_offset */
							*((gpointer *)(gpointer)((char *)MONO_CONTEXT_GET_BP (ctx) + ei->exvar_offset)) = obj;
						}

						if (ei->flags == MONO_EXCEPTION_CLAUSE_FILTER) {
							// mono_debugger_call_exception_handler (ei->data.filter, MONO_CONTEXT_GET_SP (ctx), obj);
							if (test_only) {
								mono_perfcounters->exceptions_filters++;
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
						     mono_object_isinst (obj, catch_class)) || filtered) {
							if (test_only) {
								if (mono_ex && !initial_trace_ips) {
									trace_ips = g_list_reverse (trace_ips);
									MONO_OBJECT_SETREF (mono_ex, trace_ips, glist_to_array (trace_ips, mono_defaults.int_class));
									if (has_dynamic_methods)
										/* These methods could go away anytime, so compute the stack trace now */
										MONO_OBJECT_SETREF (mono_ex, stack_trace, ves_icall_System_Exception_get_trace (mono_ex));
								}
								g_list_free (trace_ips);

								return TRUE;
							}
							if (mono_trace_is_enabled () && mono_trace_eval (ji->method))
								g_print ("EXCEPTION: catch found at clause %d of %s\n", i, mono_method_full_name (ji->method, TRUE));
							mono_profiler_exception_clause_handler (ji->method, ei->flags, i);
							mono_debugger_call_exception_handler (ei->handler_start, MONO_CONTEXT_GET_SP (ctx), obj);
							MONO_CONTEXT_SET_IP (ctx, ei->handler_start);
							*(mono_get_lmf_addr ()) = lmf;
							mono_perfcounters->exceptions_depth += frame_count;
							if (obj == domain->stack_overflow_ex)
								jit_tls->handling_stack_ovf = FALSE;

							return 0;
						}
						if (!test_only && ei->try_start <= MONO_CONTEXT_GET_IP (ctx) && 
						    MONO_CONTEXT_GET_IP (ctx) < ei->try_end &&
						    (ei->flags == MONO_EXCEPTION_CLAUSE_FAULT)) {
							if (mono_trace_is_enabled () && mono_trace_eval (ji->method))
								g_print ("EXCEPTION: fault clause %d of %s\n", i, mono_method_full_name (ji->method, TRUE));
							mono_profiler_exception_clause_handler (ji->method, ei->flags, i);
							mono_debugger_call_exception_handler (ei->handler_start, MONO_CONTEXT_GET_SP (ctx), obj);
							call_filter (ctx, ei->handler_start);
						}
						if (!test_only && ei->try_start <= MONO_CONTEXT_GET_IP (ctx) && 
						    MONO_CONTEXT_GET_IP (ctx) < ei->try_end &&
						    (ei->flags == MONO_EXCEPTION_CLAUSE_FINALLY)) {
							if (mono_trace_is_enabled () && mono_trace_eval (ji->method))
								g_print ("EXCEPTION: finally clause %d of %s\n", i, mono_method_full_name (ji->method, TRUE));
							mono_profiler_exception_clause_handler (ji->method, ei->flags, i);
							mono_debugger_call_exception_handler (ei->handler_start, MONO_CONTEXT_GET_SP (ctx), obj);
							mono_perfcounters->exceptions_finallys++;
							call_filter (ctx, ei->handler_start);
						}
						
					}
				}
			}
			if (!test_only)
				mono_profiler_exception_method_leave (ji->method);
		}

		*ctx = new_ctx;

		if (ji == (gpointer)-1) {

			if (!test_only) {
				*(mono_get_lmf_addr ()) = lmf;

				jit_tls->abort_func (obj);
				g_assert_not_reached ();
			} else {
				if (mono_ex && !initial_trace_ips) {
					trace_ips = g_list_reverse (trace_ips);
					MONO_OBJECT_SETREF (mono_ex, trace_ips, glist_to_array (trace_ips, mono_defaults.int_class));
					if (has_dynamic_methods)
						/* These methods could go away anytime, so compute the stack trace now */
						MONO_OBJECT_SETREF (mono_ex, stack_trace, ves_icall_System_Exception_get_trace (mono_ex));
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
	MonoLMF *lmf = mono_get_lmf ();
	MonoContext ctx, new_ctx;
	MonoJitInfo *ji, rji;
	int i;

	ctx = *start_ctx;

	ji = mono_find_jit_info (domain, jit_tls, &rji, NULL, &ctx, &new_ctx, NULL, &lmf, NULL, NULL);
	if (!ji || ji == (gpointer)-1)
		return;

	if (!call_filter)
		call_filter = mono_get_call_filter ();

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
	if (!test_only)
		mono_perfcounters->exceptions_thrown++;
	return mono_handle_exception_internal (ctx, obj, original_ip, test_only, NULL);
}

#ifdef MONO_ARCH_SIGSEGV_ON_ALTSTACK

#ifndef MONO_ARCH_USE_SIGACTION
#error "Can't use sigaltstack without sigaction"
#endif

#define ALIGN_TO(val,align) ((((guint64)val) + ((align) - 1)) & ~((align) - 1))

void
mono_setup_altstack (MonoJitTlsData *tls)
{
	size_t stsize = 0;
	struct sigaltstack sa;
	guint8 *staddr = NULL;

	if (mono_running_on_valgrind ())
		return;

	mono_thread_get_stack_bounds (&staddr, &stsize);

	g_assert (staddr);

	tls->end_of_stack = staddr + stsize;

	/*g_print ("thread %p, stack_base: %p, stack_size: %d\n", (gpointer)pthread_self (), staddr, stsize);*/

	tls->stack_ovf_guard_base = staddr + mono_pagesize ();
	tls->stack_ovf_guard_size = ALIGN_TO (8 * 4096, mono_pagesize ());

	if (mono_mprotect (tls->stack_ovf_guard_base, tls->stack_ovf_guard_size, MONO_MMAP_NONE)) {
		/* mprotect can fail for the main thread stack */
		gpointer gaddr = mono_valloc (tls->stack_ovf_guard_base, tls->stack_ovf_guard_size, MONO_MMAP_NONE|MONO_MMAP_PRIVATE|MONO_MMAP_ANON|MONO_MMAP_FIXED);
		g_assert (gaddr == tls->stack_ovf_guard_base);
	}

	/*
	 * threads created by nptl does not seem to have a guard page, and
	 * since the main thread is not created by us, we can't even set one.
	 * Increasing stsize fools the SIGSEGV signal handler into thinking this
	 * is a stack overflow exception.
	 */
	tls->stack_size = stsize + mono_pagesize ();

	/* Setup an alternate signal stack */
	tls->signal_stack = mono_valloc (0, MONO_ARCH_SIGNAL_STACK_SIZE, MONO_MMAP_READ|MONO_MMAP_WRITE|MONO_MMAP_PRIVATE|MONO_MMAP_ANON);
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
		mono_vfree (tls->signal_stack, MONO_ARCH_SIGNAL_STACK_SIZE);
}

#else /* !MONO_ARCH_SIGSEGV_ON_ALTSTACK */

void
mono_setup_altstack (MonoJitTlsData *tls)
{
}

void
mono_free_altstack (MonoJitTlsData *tls)
{
}

#endif /* MONO_ARCH_SIGSEGV_ON_ALTSTACK */

static gboolean
try_restore_stack_protection (MonoJitTlsData *jit_tls, int extra_bytes)
{
	gint32 unprotect_size = jit_tls->stack_ovf_guard_size;
	/* we need to leave some room for throwing the exception */
	while (unprotect_size >= 0 && (char*)jit_tls->stack_ovf_guard_base + unprotect_size > ((char*)&unprotect_size - extra_bytes))
		unprotect_size -= mono_pagesize ();
	/* at this point we could try and build a new domain->stack_overflow_ex, but only if there
	 * is sufficient stack
	 */
	//fprintf (stderr, "restoring stack protection: %p-%p (%d)\n", jit_tls->stack_ovf_guard_base, (char*)jit_tls->stack_ovf_guard_base + unprotect_size, unprotect_size);
	if (unprotect_size)
		mono_mprotect (jit_tls->stack_ovf_guard_base, unprotect_size, MONO_MMAP_NONE);
	return unprotect_size == jit_tls->stack_ovf_guard_size;
}

static void
try_more_restore (void)
{
	MonoJitTlsData *jit_tls = TlsGetValue (mono_jit_tls_id);
	if (try_restore_stack_protection (jit_tls, 500))
		jit_tls->restore_stack_prot = NULL;
}

static void
restore_stack_protection (void)
{
	MonoJitTlsData *jit_tls = TlsGetValue (mono_jit_tls_id);
	MonoException *ex = mono_domain_get ()->stack_overflow_ex;
	/* if we can't restore the stack protection, keep a callback installed so
	 * we'll try to restore as much stack as we can at each return from unmanaged
	 * code.
	 */
	if (try_restore_stack_protection (jit_tls, 4096))
		jit_tls->restore_stack_prot = NULL;
	else
		jit_tls->restore_stack_prot = try_more_restore_tramp;
	/* here we also throw a stack overflow exception */
	ex->trace_ips = NULL;
	ex->stack_trace = NULL;
	mono_raise_exception (ex);
}

gpointer
mono_altstack_restore_prot (gssize *regs, guint8 *code, gpointer *tramp_data, guint8* tramp)
{
	void (*func)(void) = (gpointer)tramp_data;
	func ();
	return NULL;
}

gboolean
mono_handle_soft_stack_ovf (MonoJitTlsData *jit_tls, MonoJitInfo *ji, void *ctx, guint8* fault_addr)
{
	/* we got a stack overflow in the soft-guard pages
	 * There are two cases:
	 * 1) managed code caused the overflow: we unprotect the soft-guard page
	 * and let the arch-specific code trigger the exception handling mechanism
	 * in the thread stack. The soft-guard pages will be protected again as the stack is unwound.
	 * 2) unmanaged code caused the overflow: we unprotect the soft-guard page
	 * and hope we can continue with those enabled, at least until the hard-guard page
	 * is hit. The alternative to continuing here is to just print a message and abort.
	 * We may add in the future the code to protect the pages again in the codepath
	 * when we return from unmanaged to managed code.
	 */
	if (jit_tls->stack_ovf_guard_size && fault_addr >= (guint8*)jit_tls->stack_ovf_guard_base &&
			fault_addr < (guint8*)jit_tls->stack_ovf_guard_base + jit_tls->stack_ovf_guard_size) {
		/* we unprotect the minimum amount we can */
		guint32 guard_size;
		gboolean handled = FALSE;

		guard_size = jit_tls->stack_ovf_guard_size - (mono_pagesize () * SIZEOF_VOID_P / 4);
		while (guard_size && fault_addr < (guint8*)jit_tls->stack_ovf_guard_base + guard_size) {
			guard_size -= mono_pagesize ();
		}
		guard_size = jit_tls->stack_ovf_guard_size - guard_size;
		/*fprintf (stderr, "unprotecting: %d\n", guard_size);*/
		mono_mprotect ((char*)jit_tls->stack_ovf_guard_base + jit_tls->stack_ovf_guard_size - guard_size, guard_size, MONO_MMAP_READ|MONO_MMAP_WRITE);
#ifdef MONO_ARCH_SIGSEGV_ON_ALTSTACK
		if (ji) {
			mono_arch_handle_altstack_exception (ctx, fault_addr, TRUE);
			handled = TRUE;
		}
#endif
		if (!handled) {
			/* We print a message: after this even managed stack overflows
			 * may crash the runtime
			 */
			fprintf (stderr, "Stack overflow in unmanaged: IP: %p, fault addr: %p\n", mono_arch_ip_from_context (ctx), fault_addr);
			if (!jit_tls->handling_stack_ovf) {
				jit_tls->restore_stack_prot = restore_stack_protection_tramp;
				jit_tls->handling_stack_ovf = 1;
			} else {
				/*fprintf (stderr, "Already handling stack overflow\n");*/
			}
		}
		return TRUE;
	}
	return FALSE;
}

static gboolean
print_stack_frame (MonoMethod *method, gint32 native_offset, gint32 il_offset, gboolean managed, gpointer data)
{
	FILE *stream = (FILE*)data;

	if (method) {
		gchar *location = mono_debug_print_stack_frame (method, native_offset, mono_domain_get ());
		fprintf (stream, "  %s\n", location);
		g_free (location);
	} else
		fprintf (stream, "  at <unknown> <0x%05x>\n", native_offset);

	return FALSE;
}

static G_GNUC_UNUSED gboolean
print_stack_frame_to_string (MonoMethod *method, gint32 native_offset, gint32 il_offset, gboolean managed,
			     gpointer data)
{
	GString *p = (GString*)data;

	if (method) {
		gchar *location = mono_debug_print_stack_frame (method, native_offset, mono_domain_get ());
		g_string_append_printf (p, "  %s\n", location);
		g_free (location);
	} else
		g_string_append_printf (p, "  at <unknown> <0x%05x>\n", native_offset);

	return FALSE;
}

static gboolean handling_sigsegv = FALSE;

/*
 * mono_handle_native_sigsegv:
 *
 *   Handle a SIGSEGV received while in native code by printing diagnostic 
 * information and aborting.
 */
void
mono_handle_native_sigsegv (int signal, void *ctx)
{
#ifdef MONO_ARCH_USE_SIGACTION
	struct sigaction sa;
#endif
	MonoJitTlsData *jit_tls = TlsGetValue (mono_jit_tls_id);

	if (handling_sigsegv)
		return;

	if (mini_get_debug_options ()->suspend_on_sigsegv) {
		fprintf (stderr, "Received SIGSEGV, suspending...");
		while (1)
			;
	}

	/* To prevent infinite loops when the stack walk causes a crash */
	handling_sigsegv = TRUE;

	/* !jit_tls means the thread was not registered with the runtime */
	if (jit_tls) {
		fprintf (stderr, "Stacktrace:\n\n");

		mono_jit_walk_stack (print_stack_frame, TRUE, stderr);

		fflush (stderr);
	}

#ifdef HAVE_BACKTRACE_SYMBOLS
 {
	void *array [256];
	char **names;
	int i, size;
	const char *signal_str = (signal == SIGSEGV) ? "SIGSEGV" : "SIGABRT";

	fprintf (stderr, "\nNative stacktrace:\n\n");

	size = backtrace (array, 256);
	names = backtrace_symbols (array, size);
	for (i =0; i < size; ++i) {
		fprintf (stderr, "\t%s\n", names [i]);
	}
	free (names);

	fflush (stderr);

	/* Try to get more meaningful information using gdb */

#if !defined(PLATFORM_WIN32) && defined(HAVE_SYS_SYSCALL_H) && defined(SYS_fork)
	if (!mini_get_debug_options ()->no_gdb_backtrace && !mono_debug_using_mono_debugger ()) {
		/* From g_spawn_command_line_sync () in eglib */
		int res;
		int stdout_pipe [2] = { -1, -1 };
		pid_t pid;
		const char *argv [16];
		char buf1 [128];
		int status;
		char buffer [1024];

		res = pipe (stdout_pipe);
		g_assert (res != -1);
			
		//pid = fork ();
		/*
		 * glibc fork acquires some locks, so if the crash happened inside malloc/free,
		 * it will deadlock. Call the syscall directly instead.
		 */
		pid = syscall (SYS_fork);
		if (pid == 0) {
			close (stdout_pipe [0]);
			dup2 (stdout_pipe [1], STDOUT_FILENO);

			for (i = getdtablesize () - 1; i >= 3; i--)
				close (i);

			argv [0] = g_find_program_in_path ("gdb");
			if (argv [0] == NULL) {
				close (STDOUT_FILENO);
				exit (1);
			}

			argv [1] = "-ex";
			sprintf (buf1, "attach %ld", (long)getpid ());
			argv [2] = buf1;
			argv [3] = "--ex";
			argv [4] = "info threads";
			argv [5] = "--ex";
			argv [6] = "thread apply all bt";
			argv [7] = "--batch";
			argv [8] = 0;

			execv (argv [0], (char**)argv);
			exit (1);
		}

		close (stdout_pipe [1]);

		fprintf (stderr, "\nDebug info from gdb:\n\n");

		while (1) {
			int nread = read (stdout_pipe [0], buffer, 1024);

			if (nread <= 0)
				break;
			write (STDERR_FILENO, buffer, nread);
		}		

		waitpid (pid, &status, WNOHANG);
	}
#endif
	/*
	 * A SIGSEGV indicates something went very wrong so we can no longer depend
	 * on anything working. So try to print out lots of diagnostics, starting 
	 * with ones which have a greater chance of working.
	 */
	fprintf (stderr,
			 "\n"
			 "=================================================================\n"
			 "Got a %s while executing native code. This usually indicates\n"
			 "a fatal error in the mono runtime or one of the native libraries \n"
			 "used by your application.\n"
			 "=================================================================\n"
			 "\n", signal_str);

 }
#endif

#ifdef MONO_ARCH_USE_SIGACTION

	/* Remove our SIGABRT handler */
	sa.sa_handler = SIG_DFL;
	sigemptyset (&sa.sa_mask);
	sa.sa_flags = 0;

	g_assert (sigaction (SIGABRT, &sa, NULL) != -1);

#endif

	abort ();
}

/*
 * mono_print_thread_dump:
 *
 *   Print information about the current thread to stdout.
 * SIGCTX can be NULL, allowing this to be called from gdb.
 */
void
mono_print_thread_dump (void *sigctx)
{
	MonoThread *thread = mono_thread_current ();
#if defined(__i386__) || defined(__x86_64__)
	MonoContext ctx;
#endif
	GString* text = g_string_new (0);
	char *name, *wapi_desc;
	GError *error = NULL;

	if (thread->name) {
		name = g_utf16_to_utf8 (thread->name, thread->name_len, NULL, NULL, &error);
		g_assert (!error);
		g_string_append_printf (text, "\n\"%s\"", name);
		g_free (name);
	}
	else if (thread->threadpool_thread)
		g_string_append (text, "\n\"<threadpool thread>\"");
	else
		g_string_append (text, "\n\"<unnamed thread>\"");

#ifndef PLATFORM_WIN32
	wapi_desc = wapi_current_thread_desc ();
	g_string_append_printf (text, " tid=0x%p this=0x%p %s\n", (gpointer)(gsize)thread->tid, thread,  wapi_desc);
	free (wapi_desc);
#endif

#ifdef MONO_ARCH_HAVE_SIGCTX_TO_MONOCTX
	if (!sigctx)
		MONO_INIT_CONTEXT_FROM_FUNC (&ctx, mono_print_thread_dump);
	else
		mono_arch_sigctx_to_monoctx (sigctx, &ctx);

	mono_jit_walk_stack_from_ctx (print_stack_frame_to_string, &ctx, TRUE, text);
#else
	printf ("\t<Stack traces in thread dumps not supported on this platform>\n");
#endif

	fprintf (stdout, text->str);
	g_string_free (text, TRUE);
	fflush (stdout);
}
