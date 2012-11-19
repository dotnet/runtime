/*
 * mini-exceptions.c: generic exception support
 *
 * Authors:
 *   Dietmar Maurer (dietmar@ximian.com)
 *   Mono Team (mono-list@lists.ximian.com)
 *
 * Copyright 2001-2003 Ximian, Inc.
 * Copyright 2003-2008 Novell, Inc.
 * Copyright 2011 Xamarin Inc (http://www.xamarin.com).
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
#include <mono/metadata/mono-endian.h>
#include <mono/metadata/environment.h>
#include <mono/utils/mono-mmap.h>
#include <mono/utils/mono-logger-internal.h>

#include "mini.h"
#include "debug-mini.h"
#include "trace.h"
#include "debugger-agent.h"

#ifndef MONO_ARCH_CONTEXT_DEF
#define MONO_ARCH_CONTEXT_DEF
#endif

static gpointer restore_context_func, call_filter_func;
static gpointer throw_exception_func, rethrow_exception_func;
static gpointer throw_corlib_exception_func;

static gpointer try_more_restore_tramp = NULL;
static gpointer restore_stack_protection_tramp = NULL;

static MonoUnhandledExceptionFunc unhandled_exception_hook = NULL;
static gpointer unhandled_exception_hook_data = NULL;

static void try_more_restore (void);
static void restore_stack_protection (void);
static void mono_walk_stack_full (MonoJitStackWalk func, MonoContext *start_ctx, MonoDomain *domain, MonoJitTlsData *jit_tls, MonoLMF *lmf, MonoUnwindOptions unwind_options, gpointer user_data);
static void mono_raise_exception_with_ctx (MonoException *exc, MonoContext *ctx);
static void mono_runtime_walk_stack_with_ctx (MonoJitStackWalk func, MonoContext *start_ctx, MonoUnwindOptions unwind_options, void *user_data);

void
mono_exceptions_init (void)
{
	MonoRuntimeExceptionHandlingCallbacks cbs;
	if (mono_aot_only) {
		restore_context_func = mono_aot_get_trampoline ("restore_context");
		call_filter_func = mono_aot_get_trampoline ("call_filter");
		throw_exception_func = mono_aot_get_trampoline ("throw_exception");
		rethrow_exception_func = mono_aot_get_trampoline ("rethrow_exception");
	} else {
		MonoTrampInfo *info;

		restore_context_func = mono_arch_get_restore_context (&info, FALSE);
		if (info) {
			mono_save_trampoline_xdebug_info (info);
			mono_tramp_info_free (info);
		}
		call_filter_func = mono_arch_get_call_filter (&info, FALSE);
		if (info) {
			mono_save_trampoline_xdebug_info (info);
			mono_tramp_info_free (info);
		}
		throw_exception_func = mono_arch_get_throw_exception (&info, FALSE);
		if (info) {
			mono_save_trampoline_xdebug_info (info);
			mono_tramp_info_free (info);
		}
		rethrow_exception_func = mono_arch_get_rethrow_exception (&info, FALSE);
		if (info) {
			mono_save_trampoline_xdebug_info (info);
			mono_tramp_info_free (info);
		}
	}
#ifdef MONO_ARCH_HAVE_RESTORE_STACK_SUPPORT
	try_more_restore_tramp = mono_create_specific_trampoline (try_more_restore, MONO_TRAMPOLINE_RESTORE_STACK_PROT, mono_domain_get (), NULL);
	restore_stack_protection_tramp = mono_create_specific_trampoline (restore_stack_protection, MONO_TRAMPOLINE_RESTORE_STACK_PROT, mono_domain_get (), NULL);
#endif

#ifdef MONO_ARCH_HAVE_EXCEPTIONS_INIT
	mono_arch_exceptions_init ();
#endif
	cbs.mono_walk_stack_with_ctx = mono_runtime_walk_stack_with_ctx;
	cbs.mono_walk_stack_with_state = mono_walk_stack_with_state;
	cbs.mono_raise_exception = mono_get_throw_exception ();
	cbs.mono_raise_exception_with_ctx = mono_raise_exception_with_ctx;
	cbs.mono_install_handler_block_guard = mono_install_handler_block_guard;
	mono_install_eh_callbacks (&cbs);
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
mono_get_throw_corlib_exception (void)
{
	gpointer code = NULL;
	MonoTrampInfo *info;

	/* This depends on corlib classes so cannot be inited in mono_exceptions_init () */
	if (throw_corlib_exception_func)
		return throw_corlib_exception_func;

	if (mono_aot_only)
		code = mono_aot_get_trampoline ("throw_corlib_exception");
	else {
		code = mono_arch_get_throw_corlib_exception (&info, FALSE);
		if (info) {
			mono_save_trampoline_xdebug_info (info);
			mono_tramp_info_free (info);
		}
	}

	mono_memory_barrier ();

	throw_corlib_exception_func = code;

	return throw_corlib_exception_func;
}

static gboolean
is_address_protected (MonoJitInfo *ji, MonoJitExceptionInfo *ei, gpointer ip)
{
	MonoTryBlockHoleTableJitInfo *table;
	int i;
	guint32 offset;
	guint16 clause;

	if (ei->try_start > ip || ip >= ei->try_end)
		return FALSE;

	if (!ji->has_try_block_holes)
		return TRUE;

	table = mono_jit_info_get_try_block_hole_table_info (ji);
	offset = (guint32)((char*)ip - (char*)ji->code_start);
	clause = (guint16)(ei - ji->clauses);
	g_assert (clause < ji->num_clauses);

	for (i = 0; i < table->num_holes; ++i) {
		MonoTryBlockHoleJitInfo *hole = &table->holes [i];
		if (hole->clause == clause && hole->offset <= offset && hole->offset + hole->length > offset)
			return FALSE;
	}
	return TRUE;
}

/*
 * find_jit_info:
 *
 * Translate between the mono_arch_find_jit_info function and the old API.
 */
static MonoJitInfo *
find_jit_info (MonoDomain *domain, MonoJitTlsData *jit_tls, MonoJitInfo *res, MonoJitInfo *prev_ji, MonoContext *ctx, 
			   MonoContext *new_ctx, MonoLMF **lmf, gboolean *managed)
{
	StackFrameInfo frame;
	MonoJitInfo *ji;
	gboolean err;
	gpointer ip = MONO_CONTEXT_GET_IP (ctx);

	/* Avoid costly table lookup during stack overflow */
	if (prev_ji && (ip > prev_ji->code_start && ((guint8*)ip < ((guint8*)prev_ji->code_start) + prev_ji->code_size)))
		ji = prev_ji;
	else
		ji = mini_jit_info_table_find (domain, ip, NULL);

	if (managed)
		*managed = FALSE;

	err = mono_arch_find_jit_info (domain, jit_tls, ji, ctx, new_ctx, lmf, NULL, &frame);
	if (!err)
		return (gpointer)-1;

	/* Convert between the new and the old APIs */
	switch (frame.type) {
	case FRAME_TYPE_MANAGED:
		if (managed)
			*managed = TRUE;
		return frame.ji;
	case FRAME_TYPE_MANAGED_TO_NATIVE:
		if (frame.ji)
			return frame.ji;
		else {
			memset (res, 0, sizeof (MonoJitInfo));
			res->method = frame.method;
			return res;
		}
	case FRAME_TYPE_DEBUGGER_INVOKE: {
		MonoContext tmp_ctx;

		/*
		 * The normal exception handling code can't handle this frame, so just
		 * skip it.
		 */
		ji = find_jit_info (domain, jit_tls, res, NULL, new_ctx, &tmp_ctx, lmf, managed);
		memcpy (new_ctx, &tmp_ctx, sizeof (MonoContext));
		return ji;
	}
	default:
		g_assert_not_reached ();
		return NULL;
	}
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

	ji = find_jit_info (domain, jit_tls, res, prev_ji, ctx, new_ctx, lmf, &managed2);

	if (ji == (gpointer)-1)
		return ji;

	if (managed2 || (ji && ji->method->wrapper_type)) {
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
			if (!ji->method->wrapper_type || ji->method->wrapper_type == MONO_WRAPPER_DYNAMIC_METHOD)
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

/*
 * mono_find_jit_info_ext:
 *
 *   A version of mono_find_jit_info which returns all data in the StackFrameInfo
 * structure.
 * A note about frames of type FRAME_TYPE_MANAGED_TO_NATIVE:
 * - These frames are used to mark managed-to-native transitions, so CTX will refer to native
 * code, and new_ctx will refer to the last managed frame. The caller should unwind once more
 * to obtain the last managed frame.
 * If SAVE_LOCATIONS is not NULL, it should point to an array of size MONO_MAX_IREGS.
 * On return, it will be filled with the locations where callee saved registers are saved
 * by the current frame. This is returned outside of StackFrameInfo because it can be
 * quite large on some platforms.
 */
gboolean
mono_find_jit_info_ext (MonoDomain *domain, MonoJitTlsData *jit_tls, 
						MonoJitInfo *prev_ji, MonoContext *ctx,
						MonoContext *new_ctx, char **trace, MonoLMF **lmf,
						mgreg_t **save_locations,
						StackFrameInfo *frame)
{
	gboolean err;
	gpointer ip = MONO_CONTEXT_GET_IP (ctx);
	MonoJitInfo *ji;
	MonoDomain *target_domain;

	if (trace)
		*trace = NULL;

	/* Avoid costly table lookup during stack overflow */
	if (prev_ji && (ip > prev_ji->code_start && ((guint8*)ip < ((guint8*)prev_ji->code_start) + prev_ji->code_size)))
		ji = prev_ji;
	else
		ji = mini_jit_info_table_find (domain, ip, &target_domain);

	if (!target_domain)
		target_domain = domain;

	if (save_locations)
		memset (save_locations, 0, MONO_MAX_IREGS * sizeof (mgreg_t*));

	err = mono_arch_find_jit_info (target_domain, jit_tls, ji, ctx, new_ctx, lmf, save_locations, frame);
	if (!err)
		return FALSE;

	if (frame->type == FRAME_TYPE_MANAGED) {
		if (!frame->ji->method->wrapper_type || frame->ji->method->wrapper_type == MONO_WRAPPER_DYNAMIC_METHOD)
			frame->managed = TRUE;
	}

	if (frame->type == FRAME_TYPE_MANAGED_TO_NATIVE) {
		/*
		 * This type of frame is just a marker, the caller should unwind once more to get the
		 * last managed frame.
		 */
		frame->ji = NULL;
		frame->method = NULL;
	}

	frame->native_offset = -1;
	frame->domain = target_domain;

	ji = frame->ji;

	if (frame->type == FRAME_TYPE_MANAGED)
		frame->method = ji->method;

	if (ji && (frame->managed || ji->method->wrapper_type)) {
		const char *real_ip, *start;

		start = (const char *)ji->code_start;
		if (!frame->managed)
			/* ctx->ip points into native code */
			real_ip = (const char*)MONO_CONTEXT_GET_IP (new_ctx);
		else
			real_ip = (const char*)ip;

		if ((real_ip >= start) && (real_ip <= start + ji->code_size))
			frame->native_offset = real_ip - start;
		else
			frame->native_offset = -1;

		if (trace)
			*trace = mono_debug_print_stack_frame (ji->method, frame->native_offset, domain);
	} else {
		if (trace && frame->method) {
			char *fname = mono_method_full_name (frame->method, TRUE);
			*trace = g_strdup_printf ("in (unmanaged) %s", fname);
			g_free (fname);
		}
	}

	return TRUE;
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

	info = NULL;
	/*
	 * Search location list if available, it contains the precise location of the
	 * argument for every pc offset, even if the method was interrupted while it was in
	 * its prolog.
	 */
	if (gi->nlocs) {
		int offset = (mgreg_t)MONO_CONTEXT_GET_IP (ctx) - (mgreg_t)ji->code_start;
		int i;

		for (i = 0; i < gi->nlocs; ++i) {
			MonoDwarfLocListEntry *entry = &gi->locations [i];

			if (offset >= entry->from && (offset < entry->to || entry->to == 0)) {
				if (entry->is_reg)
					info = (gpointer)mono_arch_context_get_int_reg (ctx, entry->reg);
				else
					info = *(gpointer*)(gpointer)((char*)mono_arch_context_get_int_reg (ctx, entry->reg) + entry->offset);
				break;
			}
		}
		g_assert (i < gi->nlocs);
	} else {
		if (gi->this_in_reg)
			info = (gpointer)mono_arch_context_get_int_reg (ctx, gi->this_reg);
		else
			info = *(gpointer*)(gpointer)((char*)mono_arch_context_get_int_reg (ctx, gi->this_reg) +
										  gi->this_offset);
	}

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

	//g_assert (!ji->method->klass->generic_container);
	if (ji->method->klass->generic_class)
		method_container_class = ji->method->klass->generic_class->container_class;
	else
		method_container_class = ji->method->klass;

	/* class might refer to a subclass of ji->method's class */
	while (!(class == ji->method->klass || (class->generic_class && class->generic_class->container_class == method_container_class))) {
		class = class->parent;
		g_assert (class);
	}

	if (class->generic_class || class->generic_container)
		context.class_inst = mini_class_get_context (class)->class_inst;

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

static void
mono_runtime_walk_stack_with_ctx (MonoJitStackWalk func, MonoContext *start_ctx, MonoUnwindOptions unwind_options, void *user_data)
{
	if (!start_ctx) {
		MonoJitTlsData *jit_tls = mono_native_tls_get_value (mono_jit_tls_id);
		if (jit_tls && jit_tls->orig_ex_ctx_set)
			start_ctx = &jit_tls->orig_ex_ctx;
	}
	mono_walk_stack_with_ctx (func, start_ctx, unwind_options, user_data);
}
/**
 * mono_walk_stack_with_ctx:
 *
 * Unwind the current thread starting at @start_ctx.
 * 
 * If @start_ctx is null, we capture the current context.
 */
void
mono_walk_stack_with_ctx (MonoJitStackWalk func, MonoContext *start_ctx, MonoUnwindOptions unwind_options, void *user_data)
{
	MonoContext extra_ctx;
	MonoInternalThread *thread = mono_thread_internal_current ();
	MONO_ARCH_CONTEXT_DEF

	if (!thread || !thread->jit_data)
		return;

	if (!start_ctx) {
		mono_arch_flush_register_windows ();

#ifdef MONO_INIT_CONTEXT_FROM_CURRENT
		MONO_INIT_CONTEXT_FROM_CURRENT (&extra_ctx);
#else
		MONO_INIT_CONTEXT_FROM_FUNC (&extra_ctx, mono_walk_stack_with_ctx);
#endif
		start_ctx = &extra_ctx;
	}

	mono_walk_stack_full (func, start_ctx, mono_domain_get (), thread->jit_data, mono_get_lmf (), unwind_options, user_data);
}

/**
 * mono_walk_stack_with_state:
 *
 * Unwind a thread described by @state.
 *
 * State must be valid (state->valid == TRUE).
 *
 * If you are using this function to unwind another thread, make sure it is suspended.
 * 
 * If @state is null, we capture the current context.
 */
void
mono_walk_stack_with_state (MonoJitStackWalk func, MonoThreadUnwindState *state, MonoUnwindOptions unwind_options, void *user_data)
{
	MonoThreadUnwindState extra_state;
	if (!state) {
		if (!mono_thread_state_init_from_current (&extra_state))
			return;
		state = &extra_state;
	}

	g_assert (state->valid);

	mono_walk_stack_full (func,
		&state->ctx, 
		state->unwind_data [MONO_UNWIND_DATA_DOMAIN],
		state->unwind_data [MONO_UNWIND_DATA_JIT_TLS],
		state->unwind_data [MONO_UNWIND_DATA_LMF],
		unwind_options, user_data);
}

void
mono_walk_stack (MonoJitStackWalk func, MonoUnwindOptions options, void *user_data)
{
	MonoThreadUnwindState state;
	if (!mono_thread_state_init_from_current (&state))
		return;
	mono_walk_stack_with_state (func, &state, options, user_data);
}

/**
 * mono_walk_stack_full:
 * @func: callback to call for each stack frame
 * @domain: starting appdomain, can be NULL to use the current domain
 * @unwind_options: what extra information the unwinder should gather
 * @start_ctx: starting state of the stack walk, can be NULL.
 * @thread: the thread whose stack to walk, can be NULL to use the current thread
 * @lmf: the LMF of @thread, can be NULL to use the LMF of the current thread
 * @user_data: data passed to the callback
 *
 * This function walks the stack of a thread, starting from the state
 * represented by start_ctx. For each frame the callback
 * function is called with the relevant info. The walk ends when no more
 * managed stack frames are found or when the callback returns a TRUE value.
 */
static void
mono_walk_stack_full (MonoJitStackWalk func, MonoContext *start_ctx, MonoDomain *domain, MonoJitTlsData *jit_tls, MonoLMF *lmf, MonoUnwindOptions unwind_options, gpointer user_data)
{
	gint il_offset, i;
	MonoContext ctx, new_ctx;
	StackFrameInfo frame;
	gboolean res;
	mgreg_t *reg_locations [MONO_MAX_IREGS];
	mgreg_t *new_reg_locations [MONO_MAX_IREGS];
	gboolean get_reg_locations = unwind_options & MONO_UNWIND_REG_LOCATIONS;

	g_assert (start_ctx);
	g_assert (domain);
	g_assert (jit_tls);
	/*The LMF will be null if the target have no managed frames.*/
 	/* g_assert (lmf); */

	memcpy (&ctx, start_ctx, sizeof (MonoContext));
	memset (reg_locations, 0, sizeof (reg_locations));

	while (MONO_CONTEXT_GET_SP (&ctx) < jit_tls->end_of_stack) {
		frame.lmf = lmf;
		res = mono_find_jit_info_ext (domain, jit_tls, NULL, &ctx, &new_ctx, NULL, &lmf, get_reg_locations ? new_reg_locations : NULL, &frame);
		if (!res)
			return;

		if ((unwind_options & MONO_UNWIND_LOOKUP_IL_OFFSET) && frame.ji) {
			MonoDebugSourceLocation *source;

			source = mono_debug_lookup_source_location (frame.ji->method, frame.native_offset, domain);
			il_offset = source ? source->il_offset : -1;
			mono_debug_free_source_location (source);
		} else
			il_offset = -1;

		frame.il_offset = il_offset;

		if ((unwind_options & MONO_UNWIND_LOOKUP_ACTUAL_METHOD) && frame.ji) {
			frame.actual_method = get_method_from_stack_frame (frame.ji, get_generic_info_from_stack_frame (frame.ji, &ctx));
		} else {
			frame.actual_method = frame.method;
		}

		if (get_reg_locations)
			frame.reg_locations = reg_locations;

		if (func (&frame, &ctx, user_data))
			return;

		if (get_reg_locations) {
			for (i = 0; i < MONO_MAX_IREGS; ++i)
				if (new_reg_locations [i])
					reg_locations [i] = new_reg_locations [i];
		}
		
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
	MonoJitTlsData *jit_tls = mono_native_tls_get_value (mono_jit_tls_id);
	MonoLMF *lmf = mono_get_lmf ();
	MonoJitInfo *ji = NULL;
	MonoContext ctx, new_ctx;
	MonoDebugSourceLocation *location;
	MonoMethod *actual_method;
	StackFrameInfo frame;
	gboolean res;

	MONO_ARCH_CONTEXT_DEF;

	mono_arch_flush_register_windows ();

#ifdef MONO_INIT_CONTEXT_FROM_CURRENT
	MONO_INIT_CONTEXT_FROM_CURRENT (&ctx);
#else
	MONO_INIT_CONTEXT_FROM_FUNC (&ctx, ves_icall_get_frame_info);
#endif

	new_ctx = ctx;
	do {
		ctx = new_ctx;
		res = mono_find_jit_info_ext (domain, jit_tls, NULL, &ctx, &new_ctx, NULL, &lmf, NULL, &frame);
		if (!res)
			return FALSE;

		if (frame.type == FRAME_TYPE_MANAGED_TO_NATIVE || frame.type == FRAME_TYPE_DEBUGGER_INVOKE)
			continue;

		ji = frame.ji;
		*native_offset = frame.native_offset;

		/* The skip count passed by the caller depends on us not filtering out MANAGED_TO_NATIVE */
		if (ji->method->wrapper_type != MONO_WRAPPER_NONE && ji->method->wrapper_type != MONO_WRAPPER_DYNAMIC_METHOD && ji->method->wrapper_type != MONO_WRAPPER_MANAGED_TO_NATIVE)
			continue;
		skip--;
	} while (skip >= 0);

	actual_method = get_method_from_stack_frame (ji, get_generic_info_from_stack_frame (ji, &ctx));

	mono_gc_wbarrier_generic_store (method, (MonoObject*) mono_method_get_object (domain, actual_method, NULL));

	location = mono_debug_lookup_source_location (ji->method, *native_offset, domain);
	if (location)
		*iloffset = location->il_offset;
	else
		*iloffset = 0;

	if (need_file_info) {
		if (location) {
			mono_gc_wbarrier_generic_store (file, (MonoObject*) mono_string_new (domain, location->source_file));
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
callback_get_first_frame_security_info (StackFrameInfo *frame, MonoContext *ctx, gpointer data)
{
	MonoFrameSecurityInfo *si = (MonoFrameSecurityInfo*) data;
	MonoJitInfo *ji = frame->ji;

	if (!ji)
		return FALSE;

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

	si->frame = mono_declsec_create_frame (frame->domain, ji);

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
	MonoFrameSecurityInfo si;

	si.skips = skip;
	si.frame = NULL;

	mono_walk_stack (callback_get_first_frame_security_info, MONO_UNWIND_DEFAULT, &si);

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
callback_get_stack_frames_security_info (StackFrameInfo *frame, MonoContext *ctx, gpointer data)
{
	MonoSecurityStack *ss = (MonoSecurityStack*) data;
	MonoJitInfo *ji = frame->ji;

	if (!ji)
		return FALSE;

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

	mono_array_setref (ss->stack, ss->count++, mono_declsec_create_frame (frame->domain, ji));

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
	MonoSecurityStack ss;

#if	defined(__ia64__) || defined(__s390__) || defined(__s390x__)
	skip--;
#endif

	ss.skips = skip;
	ss.count = 0;
	ss.maximum = MONO_CAS_INITIAL_STACK_SIZE;
	ss.stack = mono_array_new (mono_domain_get (), mono_defaults.runtimesecurityframe_class, ss.maximum);
	mono_walk_stack (callback_get_stack_frames_security_info, MONO_UNWIND_DEFAULT, &ss);
	/* g_warning ("STACK RESULT: %d out of %d", ss.count, ss.maximum); */
	return ss.stack;
}

static MonoClass*
get_exception_catch_class (MonoJitExceptionInfo *ei, MonoJitInfo *ji, MonoContext *ctx)
{
	MonoClass *catch_class = ei->data.catch_class;
	MonoType *inflated_type;
	MonoGenericContext context;

	/*MonoJitExceptionInfo::data is an union used by filter and finally clauses too.*/
	if (!catch_class || ei->flags != MONO_EXCEPTION_CLAUSE_NONE)
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

/*
 * mini_jit_info_table_find:
 *
 *   Same as mono_jit_info_table_find, but search all the domains of the current thread
 * if ADDR is not found in DOMAIN. The domain where the method was found is stored into
 * OUT_DOMAIN if it is not NULL.
 */
MonoJitInfo*
mini_jit_info_table_find (MonoDomain *domain, char *addr, MonoDomain **out_domain)
{
	MonoJitInfo *ji;
	MonoInternalThread *t = mono_thread_internal_current ();
	gpointer *refs;

	if (out_domain)
		*out_domain = NULL;

	ji = mono_jit_info_table_find (domain, addr);
	if (ji) {
		if (out_domain)
			*out_domain = domain;
		return ji;
	}

	/* maybe it is shared code, so we also search in the root domain */
	if (domain != mono_get_root_domain ()) {
		ji = mono_jit_info_table_find (mono_get_root_domain (), addr);
		if (ji) {
			if (out_domain)
				*out_domain = mono_get_root_domain ();
			return ji;
		}
	}

	refs = (t->appdomain_refs) ? *(gpointer *) t->appdomain_refs : NULL;
	for (; refs && *refs; refs++) {
		if (*refs != domain && *refs != mono_get_root_domain ()) {
			ji = mono_jit_info_table_find ((MonoDomain*) *refs, addr);
			if (ji) {
				if (out_domain)
					*out_domain = (MonoDomain*) *refs;
				return ji;
			}
		}
	}

	return NULL;
}

/*
 * wrap_non_exception_throws:
 *
 *   Determine whenever M's assembly has a RuntimeCompatibilityAttribute with the
 * WrapNonExceptionThrows flag set.
 */
static gboolean
wrap_non_exception_throws (MonoMethod *m)
{
	MonoAssembly *ass = m->klass->image->assembly;
	MonoCustomAttrInfo* attrs;
	static MonoClass *klass;
	int i;
	gboolean val = FALSE;

	g_assert (ass);
	if (ass->wrap_non_exception_throws_inited)
		return ass->wrap_non_exception_throws;

	klass = mono_class_from_name_cached (mono_defaults.corlib, "System.Runtime.CompilerServices", "RuntimeCompatibilityAttribute");

	attrs = mono_custom_attrs_from_assembly (ass);
	if (attrs) {
		for (i = 0; i < attrs->num_attrs; ++i) {
			MonoCustomAttrEntry *attr = &attrs->attrs [i];
			const gchar *p;
			int len, num_named, named_type, data_type, name_len;
			char *name;

			if (!attr->ctor || attr->ctor->klass != klass)
				continue;
			/* Decode the RuntimeCompatibilityAttribute. See reflection.c */
			len = attr->data_size;
			p = (const char*)attr->data;
			g_assert (read16 (p) == 0x0001);
			p += 2;
			num_named = read16 (p);
			if (num_named != 1)
				continue;
			p += 2;
			named_type = *p;
			p ++;
			data_type = *p;
			p ++;
			/* Property */
			if (named_type != 0x54)
				continue;
			name_len = mono_metadata_decode_blob_size (p, &p);
			name = g_malloc (name_len + 1);
			memcpy (name, p, name_len);
			name [name_len] = 0;
			p += name_len;
			g_assert (!strcmp (name, "WrapNonExceptionThrows"));
			g_free (name);
			/* The value is a BOOLEAN */
			val = *p;
		}
		mono_custom_attrs_free (attrs);
	}

	ass->wrap_non_exception_throws = val;
	mono_memory_barrier ();
	ass->wrap_non_exception_throws_inited = TRUE;

	return val;
}

#ifndef MONO_ARCH_STACK_GROWS_UP
#define DOES_STACK_GROWS_UP 1
#else
#define DOES_STACK_GROWS_UP 0
#endif

#define MAX_UNMANAGED_BACKTRACE 128
static MonoArray*
build_native_trace (void)
{
/* This puppy only makes sense on mobile, IOW, ARM. */
#if defined (HAVE_BACKTRACE_SYMBOLS) && defined (TARGET_ARM)
	MonoArray *res;
	void *native_trace [MAX_UNMANAGED_BACKTRACE];
	int size = backtrace (native_trace, MAX_UNMANAGED_BACKTRACE);
	int i;

	if (!size)
		return NULL;
	res = mono_array_new (mono_domain_get (), mono_defaults.int_class, size);

	for (i = 0; i < size; i++)
		mono_array_set (res, gpointer, i, native_trace [i]);
	return res;
#else
	return NULL;
#endif
}

#define setup_managed_stacktrace_information() do {	\
	if (mono_ex && !initial_trace_ips) {	\
		trace_ips = g_list_reverse (trace_ips);	\
		MONO_OBJECT_SETREF (mono_ex, trace_ips, glist_to_array (trace_ips, mono_defaults.int_class));	\
		MONO_OBJECT_SETREF (mono_ex, native_trace_ips, build_native_trace ());	\
		if (has_dynamic_methods)	\
			/* These methods could go away anytime, so compute the stack trace now */	\
			MONO_OBJECT_SETREF (mono_ex, stack_trace, ves_icall_System_Exception_get_trace (mono_ex));	\
	}	\
	g_list_free (trace_ips);	\
	trace_ips = NULL;	\
} while (0)
/*
 * mono_handle_exception_internal_first_pass:
 *
 *   The first pass of exception handling. Unwind the stack until a catch clause which can catch
 * OBJ is found. Run the index of the filter clause which caught the exception into
 * OUT_FILTER_IDX. Return TRUE if the exception is caught, FALSE otherwise.
 */
static gboolean
mono_handle_exception_internal_first_pass (MonoContext *ctx, gpointer obj, gint32 *out_filter_idx, MonoJitInfo **out_ji, MonoObject *non_exception)
{
	MonoDomain *domain = mono_domain_get ();
	MonoJitInfo *ji;
	static int (*call_filter) (MonoContext *, gpointer) = NULL;
	MonoJitTlsData *jit_tls = mono_native_tls_get_value (mono_jit_tls_id);
	MonoLMF *lmf = mono_get_lmf ();
	MonoArray *initial_trace_ips = NULL;
	GList *trace_ips = NULL;
	MonoException *mono_ex;
	gboolean stack_overflow = FALSE;
	MonoContext initial_ctx;
	int frame_count = 0;
	gboolean has_dynamic_methods = FALSE;
	gint32 filter_idx;
	int i;
	MonoObject *ex_obj;

	g_assert (ctx != NULL);

	if (obj == domain->stack_overflow_ex)
		stack_overflow = TRUE;

	mono_ex = (MonoException*)obj;
	initial_trace_ips = mono_ex->trace_ips;

	if (mono_object_isinst (obj, mono_defaults.exception_class)) {
		mono_ex = (MonoException*)obj;
		initial_trace_ips = mono_ex->trace_ips;
	} else {
		mono_ex = NULL;
	}

	if (!call_filter)
		call_filter = mono_get_call_filter ();

	g_assert (jit_tls->end_of_stack);
	g_assert (jit_tls->abort_func);

	if (out_filter_idx)
		*out_filter_idx = -1;
	if (out_ji)
		*out_ji = NULL;
	filter_idx = 0;
	initial_ctx = *ctx;

	while (1) {
		MonoContext new_ctx;
		guint32 free_stack;
		int clause_index_start = 0;
		gboolean unwind_res = TRUE;
		
		StackFrameInfo frame;

		unwind_res = mono_find_jit_info_ext (domain, jit_tls, NULL, ctx, &new_ctx, NULL, &lmf, NULL, &frame);
		if (unwind_res) {
			if (frame.type == FRAME_TYPE_DEBUGGER_INVOKE || frame.type == FRAME_TYPE_MANAGED_TO_NATIVE) {
				*ctx = new_ctx;
				continue;
			}
			g_assert (frame.type == FRAME_TYPE_MANAGED);
			ji = frame.ji;
		}

		if (!unwind_res) {
			setup_managed_stacktrace_information ();
			return FALSE;
		}

		frame_count ++;
		//printf ("M: %s %d.\n", mono_method_full_name (ji->method, TRUE), frame_count);

		if (mini_get_debug_options ()->reverse_pinvoke_exceptions && ji->method->wrapper_type == MONO_WRAPPER_NATIVE_TO_MANAGED) {
			g_error ("A native frame was found while unwinding the stack after an exception.\n"
					 "The native frame called the managed method:\n%s\n",
					 mono_method_full_name (ji->method, TRUE));
		}

		if (ji->method->wrapper_type != MONO_WRAPPER_RUNTIME_INVOKE && mono_ex) {
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

		if (stack_overflow) {
			if (DOES_STACK_GROWS_UP)
				free_stack = (guint8*)(MONO_CONTEXT_GET_SP (ctx)) - (guint8*)(MONO_CONTEXT_GET_SP (&initial_ctx));
			else
				free_stack = (guint8*)(MONO_CONTEXT_GET_SP (&initial_ctx)) - (guint8*)(MONO_CONTEXT_GET_SP (ctx));
		} else {
			free_stack = 0xffffff;
		}
				
		for (i = clause_index_start; i < ji->num_clauses; i++) {
			MonoJitExceptionInfo *ei = &ji->clauses [i];
			gboolean filtered = FALSE;

			/* 
			 * During stack overflow, wait till the unwinding frees some stack
			 * space before running handlers/finalizers.
			 */
			if (free_stack <= (64 * 1024))
				continue;

			if (is_address_protected (ji, ei, MONO_CONTEXT_GET_IP (ctx))) {
				/* catch block */
				MonoClass *catch_class = get_exception_catch_class (ei, ji, ctx);

				/*
				 * Have to unwrap RuntimeWrappedExceptions if the
				 * method's assembly doesn't have a RuntimeCompatibilityAttribute.
				 */
				if (non_exception && !wrap_non_exception_throws (ji->method))
					ex_obj = non_exception;
				else
					ex_obj = obj;

				if (ei->flags == MONO_EXCEPTION_CLAUSE_FILTER) {
					gboolean is_user_frame = ji->method->wrapper_type == MONO_WRAPPER_NONE || ji->method->wrapper_type == MONO_WRAPPER_DYNAMIC_METHOD;
#ifndef DISABLE_PERFCOUNTERS
					mono_perfcounters->exceptions_filters++;
#endif
					mono_debugger_call_exception_handler (ei->data.filter, MONO_CONTEXT_GET_SP (ctx), ex_obj);

					/*
					Here's the thing, if this is a filter clause done by a wrapper like runtime invoke, we don't want to
					trim the stackframe since if it returns FALSE we lose information.

					FIXME Not 100% sure if it's a good idea even with user clauses.
					*/
					if (is_user_frame)
						setup_managed_stacktrace_information ();

					if (ji->from_llvm) {
#ifdef MONO_CONTEXT_SET_LLVM_EXC_REG
						MONO_CONTEXT_SET_LLVM_EXC_REG (ctx, ex_obj);
#else
						g_assert_not_reached ();
#endif
					} else {
						/* store the exception object in bp + ei->exvar_offset */
						*((gpointer *)(gpointer)((char *)MONO_CONTEXT_GET_BP (ctx) + ei->exvar_offset)) = ex_obj;
					}

					mono_debugger_agent_begin_exception_filter (mono_ex, ctx, &initial_ctx);
					filtered = call_filter (ctx, ei->data.filter);
					mono_debugger_agent_end_exception_filter (mono_ex, ctx, &initial_ctx);
					if (filtered && out_filter_idx)
						*out_filter_idx = filter_idx;
					if (out_ji)
						*out_ji = ji;
					filter_idx ++;

					if (filtered) {
						if (!is_user_frame)
							setup_managed_stacktrace_information ();
						/* mono_debugger_agent_handle_exception () needs this */
						MONO_CONTEXT_SET_IP (ctx, ei->handler_start);
						return TRUE;
					}
				}

				if (ei->flags == MONO_EXCEPTION_CLAUSE_NONE && mono_object_isinst (ex_obj, catch_class)) {
					setup_managed_stacktrace_information ();

					if (out_ji)
						*out_ji = ji;

					/* mono_debugger_agent_handle_exception () needs this */
					MONO_CONTEXT_SET_IP (ctx, ei->handler_start);
					return TRUE;
				}
			}
		}

		*ctx = new_ctx;
	}

	g_assert_not_reached ();
}

/**
 * mono_handle_exception_internal:
 * @ctx: saved processor state
 * @obj: the exception object
 * @resume: whenever to resume unwinding based on the state in MonoJitTlsData.
 */
static gboolean
mono_handle_exception_internal (MonoContext *ctx, gpointer obj, gboolean resume, MonoJitInfo **out_ji)
{
	MonoDomain *domain = mono_domain_get ();
	MonoJitInfo *ji;
	static int (*call_filter) (MonoContext *, gpointer) = NULL;
	static void (*restore_context) (void *);
	MonoJitTlsData *jit_tls = mono_native_tls_get_value (mono_jit_tls_id);
	MonoLMF *lmf = mono_get_lmf ();
	MonoException *mono_ex;
	gboolean stack_overflow = FALSE;
	MonoContext initial_ctx;
	int frame_count = 0;
	gint32 filter_idx, first_filter_idx;
	int i;
	MonoObject *ex_obj;
	MonoObject *non_exception = NULL;

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

	if (!mono_object_isinst (obj, mono_defaults.exception_class)) {
		non_exception = obj;
		obj = mono_get_exception_runtime_wrapped (obj);
	}

	mono_ex = (MonoException*)obj;

	if (mono_object_isinst (obj, mono_defaults.exception_class)) {
		mono_ex = (MonoException*)obj;
	} else {
		mono_ex = NULL;
	}

	if (mono_ex && jit_tls->class_cast_from) {
		if (!strcmp (mono_ex->object.vtable->klass->name, "InvalidCastException")) {
			char *from_name = mono_type_get_full_name (jit_tls->class_cast_from);
			char *to_name = mono_type_get_full_name (jit_tls->class_cast_to);
			char *msg = g_strdup_printf ("Unable to cast object of type '%s' to type '%s'.", from_name, to_name);
			mono_ex->message = mono_string_new (domain, msg);
			g_free (from_name);
			g_free (to_name);
			g_free (msg);
		}
		if (!strcmp (mono_ex->object.vtable->klass->name, "ArrayTypeMismatchException")) {
			char *from_name = mono_type_get_full_name (jit_tls->class_cast_from);
			char *to_name = mono_type_get_full_name (jit_tls->class_cast_to);
			char *msg = g_strdup_printf ("Source array of type '%s' cannot be cast to destination array type '%s'.", from_name, to_name);
			mono_ex->message = mono_string_new (domain, msg);
			g_free (from_name);
			g_free (to_name);
			g_free (msg);
		}
	}

	if (!call_filter)
		call_filter = mono_get_call_filter ();

	if (!restore_context)
		restore_context = mono_get_restore_context ();

	g_assert (jit_tls->end_of_stack);
	g_assert (jit_tls->abort_func);

	/*
	 * We set orig_ex_ctx_set to TRUE/FALSE around profiler calls to make sure it doesn't
	 * end up being TRUE on any code path.
	 */
	memcpy (&jit_tls->orig_ex_ctx, ctx, sizeof (MonoContext));

	if (!resume) {
		gboolean res;

		MonoContext ctx_cp = *ctx;
		if (mono_trace_is_enabled ()) {
			MonoMethod *system_exception_get_message = mono_class_get_method_from_name (mono_defaults.exception_class, "get_Message", 0);
			MonoMethod *get_message = system_exception_get_message == NULL ? NULL : mono_object_get_virtual_method (obj, system_exception_get_message);
			MonoObject *message;
			const char *type_name = mono_class_get_name (mono_object_class (mono_ex));
			char *msg = NULL;
			MonoObject *exc = NULL;
			if (get_message == NULL) {
				message = NULL;
			} else if (!strcmp (type_name, "OutOfMemoryException") || !strcmp (type_name, "StackOverflowException")) {
				message = NULL;
				msg = g_strdup_printf ("(No exception message for: %s)\n", type_name);
			} else {
				message = mono_runtime_invoke (get_message, obj, NULL, &exc);
				
			}
			if (msg == NULL) {
				msg = message ? mono_string_to_utf8 ((MonoString *) message) : g_strdup ("(System.Exception.Message property not available)");
			}
			g_print ("[%p:] EXCEPTION handling: %s.%s: %s\n", (void*)GetCurrentThreadId (), mono_object_class (obj)->name_space, mono_object_class (obj)->name, msg);
			g_free (msg);
			if (mono_ex && mono_trace_eval_exception (mono_object_class (mono_ex)))
				mono_print_thread_dump_from_ctx (ctx);
		}
		jit_tls->orig_ex_ctx_set = TRUE;
		mono_profiler_exception_thrown (obj);
		jit_tls->orig_ex_ctx_set = FALSE;

		res = mono_handle_exception_internal_first_pass (&ctx_cp, obj, &first_filter_idx, &ji, non_exception);

		if (!res) {
			if (mini_get_debug_options ()->break_on_exc)
				G_BREAKPOINT ();
			mono_debugger_agent_handle_exception (obj, ctx, NULL);

			if (mini_get_debug_options ()->suspend_on_unhandled) {
				mono_runtime_printf_err ("Unhandled exception, suspending...");
				while (1)
					;
			}

			// FIXME: This runs managed code so it might cause another stack overflow when
			// we are handling a stack overflow
			mono_unhandled_exception (obj);
		} else {
			//
			// Treat exceptions that are "handled" by mono_runtime_invoke() as unhandled.
			// See bug #669836.
			//
			if (ji && ji->method->wrapper_type == MONO_WRAPPER_RUNTIME_INVOKE)
				mono_debugger_agent_handle_exception (obj, ctx, NULL);
			else
				mono_debugger_agent_handle_exception (obj, ctx, &ctx_cp);
		}
	}

	if (out_ji)
		*out_ji = NULL;
	filter_idx = 0;
	initial_ctx = *ctx;

	while (1) {
		MonoContext new_ctx;
		guint32 free_stack;
		int clause_index_start = 0;
		gboolean unwind_res = TRUE;
		
		if (resume) {
			resume = FALSE;
			ji = jit_tls->resume_state.ji;
			new_ctx = jit_tls->resume_state.new_ctx;
			clause_index_start = jit_tls->resume_state.clause_index;
			lmf = jit_tls->resume_state.lmf;
			first_filter_idx = jit_tls->resume_state.first_filter_idx;
			filter_idx = jit_tls->resume_state.filter_idx;
		} else {
			StackFrameInfo frame;

			unwind_res = mono_find_jit_info_ext (domain, jit_tls, NULL, ctx, &new_ctx, NULL, &lmf, NULL, &frame);
			if (unwind_res) {
				if (frame.type == FRAME_TYPE_DEBUGGER_INVOKE || frame.type == FRAME_TYPE_MANAGED_TO_NATIVE) {
					*ctx = new_ctx;
					continue;
				}
				g_assert (frame.type == FRAME_TYPE_MANAGED);
				ji = frame.ji;
			}
		}

		if (!unwind_res) {
			*(mono_get_lmf_addr ()) = lmf;

			jit_tls->abort_func (obj);
			g_assert_not_reached ();
		}

		frame_count ++;
		//printf ("M: %s %d.\n", mono_method_full_name (ji->method, TRUE), frame_count);

		if (stack_overflow) {
			if (DOES_STACK_GROWS_UP)
				free_stack = (guint8*)(MONO_CONTEXT_GET_SP (ctx)) - (guint8*)(MONO_CONTEXT_GET_SP (&initial_ctx));
			else
				free_stack = (guint8*)(MONO_CONTEXT_GET_SP (&initial_ctx)) - (guint8*)(MONO_CONTEXT_GET_SP (ctx));
		} else {
			free_stack = 0xffffff;
		}
				
		for (i = clause_index_start; i < ji->num_clauses; i++) {
			MonoJitExceptionInfo *ei = &ji->clauses [i];
			gboolean filtered = FALSE;

			/* 
			 * During stack overflow, wait till the unwinding frees some stack
			 * space before running handlers/finalizers.
			 */
			if (free_stack <= (64 * 1024))
				continue;

			if (is_address_protected (ji, ei, MONO_CONTEXT_GET_IP (ctx))) {
				/* catch block */
				MonoClass *catch_class = get_exception_catch_class (ei, ji, ctx);

				/*
				 * Have to unwrap RuntimeWrappedExceptions if the
				 * method's assembly doesn't have a RuntimeCompatibilityAttribute.
				 */
				if (non_exception && !wrap_non_exception_throws (ji->method))
					ex_obj = non_exception;
				else
					ex_obj = obj;

				if (((ei->flags == MONO_EXCEPTION_CLAUSE_NONE) || (ei->flags == MONO_EXCEPTION_CLAUSE_FILTER))) {
					if (ji->from_llvm) {
#ifdef MONO_CONTEXT_SET_LLVM_EXC_REG
						MONO_CONTEXT_SET_LLVM_EXC_REG (ctx, ex_obj);
#else
						g_assert_not_reached ();
#endif
					} else {
						/* store the exception object in bp + ei->exvar_offset */
						*((gpointer *)(gpointer)((char *)MONO_CONTEXT_GET_BP (ctx) + ei->exvar_offset)) = ex_obj;
					}
				}

				if (ei->flags == MONO_EXCEPTION_CLAUSE_FILTER) {
					/* 
					 * Filter clauses should only be run in the 
					 * first pass of exception handling.
					 */
					filtered = (filter_idx == first_filter_idx);
					filter_idx ++;
				}

				if ((ei->flags == MONO_EXCEPTION_CLAUSE_NONE && 
					 mono_object_isinst (ex_obj, catch_class)) || filtered) {
					/*
					 * This guards against the situation that we abort a thread that is executing a finally clause
					 * that was called by the EH machinery. It won't have a guard trampoline installed, so we must
					 * check for this situation here and resume interruption if we are below the guarded block.
					 */
					if (G_UNLIKELY (jit_tls->handler_block_return_address)) {
						gboolean is_outside = FALSE;
						gpointer prot_bp = MONO_CONTEXT_GET_BP (&jit_tls->handler_block_context);
						gpointer catch_bp = MONO_CONTEXT_GET_BP (ctx);
						//FIXME make this stack direction aware
						if (catch_bp > prot_bp) {
							is_outside = TRUE;
						} else if (catch_bp == prot_bp) {
							/* Can be either try { try { } catch {} } finally {} or try { try { } finally {} } catch {}
							 * So we check if the catch handler_start is protected by the guarded handler protected region
							 *
							 * Assumptions:
							 *	If there is an outstanding guarded_block return address, it means the current thread must be aborted.
							 *	This is the only way to reach out the guarded block as other cases are handled by the trampoline.
							 *	There aren't any further finally/fault handler blocks down the stack over this exception.
							 *   This must be ensured by the code that installs the guard trampoline.
							 */
							g_assert (ji == mini_jit_info_table_find (domain, MONO_CONTEXT_GET_IP (&jit_tls->handler_block_context), NULL));

							if (!is_address_protected (ji, jit_tls->handler_block, ei->handler_start)) {
								is_outside = TRUE;
							}
						}
						if (is_outside) {
							jit_tls->handler_block_return_address = NULL;
							jit_tls->handler_block = NULL;
							mono_thread_resume_interruption (); /*We ignore the exception here, it will be raised later*/
						}
					}

					if (mono_trace_is_enabled () && mono_trace_eval (ji->method))
						g_print ("EXCEPTION: catch found at clause %d of %s\n", i, mono_method_full_name (ji->method, TRUE));
					jit_tls->orig_ex_ctx_set = TRUE;
					mono_profiler_exception_clause_handler (ji->method, ei->flags, i);
					jit_tls->orig_ex_ctx_set = FALSE;
					mono_debugger_call_exception_handler (ei->handler_start, MONO_CONTEXT_GET_SP (ctx), ex_obj);
					MONO_CONTEXT_SET_IP (ctx, ei->handler_start);
					*(mono_get_lmf_addr ()) = lmf;
#ifndef DISABLE_PERFCOUNTERS
					mono_perfcounters->exceptions_depth += frame_count;
#endif
					if (obj == domain->stack_overflow_ex)
						jit_tls->handling_stack_ovf = FALSE;

					return 0;
				}
				if (is_address_protected (ji, ei, MONO_CONTEXT_GET_IP (ctx)) &&
					(ei->flags == MONO_EXCEPTION_CLAUSE_FAULT)) {
					if (mono_trace_is_enabled () && mono_trace_eval (ji->method))
						g_print ("EXCEPTION: fault clause %d of %s\n", i, mono_method_full_name (ji->method, TRUE));
					jit_tls->orig_ex_ctx_set = TRUE;
					mono_profiler_exception_clause_handler (ji->method, ei->flags, i);
					jit_tls->orig_ex_ctx_set = FALSE;
					mono_debugger_call_exception_handler (ei->handler_start, MONO_CONTEXT_GET_SP (ctx), ex_obj);
					call_filter (ctx, ei->handler_start);
				}
				if (is_address_protected (ji, ei, MONO_CONTEXT_GET_IP (ctx)) &&
					(ei->flags == MONO_EXCEPTION_CLAUSE_FINALLY)) {
					if (mono_trace_is_enabled () && mono_trace_eval (ji->method))
						g_print ("EXCEPTION: finally clause %d of %s\n", i, mono_method_full_name (ji->method, TRUE));
					jit_tls->orig_ex_ctx_set = TRUE;
					mono_profiler_exception_clause_handler (ji->method, ei->flags, i);
					jit_tls->orig_ex_ctx_set = FALSE;
					mono_debugger_call_exception_handler (ei->handler_start, MONO_CONTEXT_GET_SP (ctx), ex_obj);
#ifndef DISABLE_PERFCOUNTERS
					mono_perfcounters->exceptions_finallys++;
#endif
					*(mono_get_lmf_addr ()) = lmf;
					if (ji->from_llvm) {
						/* 
						 * LLVM compiled finally handlers follow the design
						 * of the c++ ehabi, i.e. they call a resume function
						 * at the end instead of returning to the caller.
						 * So save the exception handling state,
						 * mono_resume_unwind () will call us again to continue
						 * the unwinding.
						 */
						jit_tls->resume_state.ex_obj = obj;
						jit_tls->resume_state.ji = ji;
						jit_tls->resume_state.clause_index = i + 1;
						jit_tls->resume_state.ctx = *ctx;
						jit_tls->resume_state.new_ctx = new_ctx;
						jit_tls->resume_state.lmf = lmf;
						jit_tls->resume_state.first_filter_idx = first_filter_idx;
						jit_tls->resume_state.filter_idx = filter_idx;
						MONO_CONTEXT_SET_IP (ctx, ei->handler_start);
						return 0;
					} else {
						call_filter (ctx, ei->handler_start);
					}
				}
			}
		}

		jit_tls->orig_ex_ctx_set = TRUE;
		mono_profiler_exception_method_leave (ji->method);
		jit_tls->orig_ex_ctx_set = FALSE;

		*ctx = new_ctx;
	}

	g_assert_not_reached ();
}

/*
 * mono_debugger_handle_exception:
 *
 *  Notify the debugger about exceptions.  Returns TRUE if the debugger wants us to stop
 *  at the exception and FALSE to resume with the normal exception handling.
 *
 *  The arch code is responsible to setup @ctx in a way that MONO_CONTEXT_GET_IP () and
 *  MONO_CONTEXT_GET_SP () point to the throw instruction; ie. before executing the
 *  `callq throw' instruction.
 */
gboolean
mono_debugger_handle_exception (MonoContext *ctx, MonoObject *obj)
{
	MonoDebuggerExceptionAction action;

	if (!mono_debug_using_mono_debugger ())
		return FALSE;

	if (!obj) {
		MonoException *ex = mono_get_exception_null_reference ();
		MONO_OBJECT_SETREF (ex, message, mono_string_new (mono_domain_get (), "Object reference not set to an instance of an object"));
		obj = (MonoObject *)ex;
	}

	action = _mono_debugger_throw_exception (MONO_CONTEXT_GET_IP (ctx), MONO_CONTEXT_GET_SP (ctx), obj);

	if (action == MONO_DEBUGGER_EXCEPTION_ACTION_STOP) {
		/*
		 * The debugger wants us to stop on the `throw' instruction.
		 * By the time we get here, it already inserted a breakpoint there.
		 */
		return TRUE;
	} else if (action == MONO_DEBUGGER_EXCEPTION_ACTION_STOP_UNHANDLED) {
		MonoContext ctx_cp = *ctx;
		MonoJitInfo *ji = NULL;
		gboolean ret;

		/*
		 * The debugger wants us to stop only if this exception is user-unhandled.
		 */

		ret = mono_handle_exception_internal_first_pass (&ctx_cp, obj, NULL, &ji, NULL);
		if (ret && (ji != NULL) && (ji->method->wrapper_type == MONO_WRAPPER_RUNTIME_INVOKE)) {
			/*
			 * The exception is handled in a runtime-invoke wrapper, that means that it's unhandled
			 * inside the method being invoked, so we handle it like a user-unhandled exception.
			 */
			ret = FALSE;
		}

		if (!ret) {
			/*
			 * The exception is user-unhandled - tell the debugger to stop.
			 */
			return _mono_debugger_unhandled_exception (MONO_CONTEXT_GET_IP (ctx), MONO_CONTEXT_GET_SP (ctx), obj);
		}

		/*
		 * The exception is catched somewhere - resume with the normal exception handling and don't
		 * stop in the debugger.
		 */
	}

	return FALSE;
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
	MonoJitTlsData *jit_tls = mono_native_tls_get_value (mono_jit_tls_id);
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

		if (is_address_protected (ji, ei, MONO_CONTEXT_GET_IP (&ctx)) &&
		    (ei->flags & MONO_EXCEPTION_CLAUSE_FINALLY)) {
			call_filter (&ctx, ei->handler_start);
		}
	}
}

/**
 * mono_handle_exception:
 * @ctx: saved processor state
 * @obj: the exception object
 */
gboolean
mono_handle_exception (MonoContext *ctx, gpointer obj)
{
#ifndef DISABLE_PERFCOUNTERS
	mono_perfcounters->exceptions_thrown++;
#endif

	return mono_handle_exception_internal (ctx, obj, FALSE, NULL);
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
	stack_t sa;
	guint8 *staddr = NULL;

	if (mono_running_on_valgrind ())
		return;

	mono_thread_get_stack_bounds (&staddr, &stsize);

	g_assert (staddr);

	tls->end_of_stack = staddr + stsize;
	tls->stack_size = stsize;

	/*g_print ("thread %p, stack_base: %p, stack_size: %d\n", (gpointer)pthread_self (), staddr, stsize);*/

	tls->stack_ovf_guard_base = staddr + mono_pagesize ();
	tls->stack_ovf_guard_size = ALIGN_TO (8 * 4096, mono_pagesize ());

	g_assert ((guint8*)&sa >= (guint8*)tls->stack_ovf_guard_base + tls->stack_ovf_guard_size);

	if (mono_mprotect (tls->stack_ovf_guard_base, tls->stack_ovf_guard_size, MONO_MMAP_NONE)) {
		/* mprotect can fail for the main thread stack */
		gpointer gaddr = mono_valloc (tls->stack_ovf_guard_base, tls->stack_ovf_guard_size, MONO_MMAP_NONE|MONO_MMAP_PRIVATE|MONO_MMAP_ANON|MONO_MMAP_FIXED);
		g_assert (gaddr == tls->stack_ovf_guard_base);
		tls->stack_ovf_valloced = TRUE;
	}

	/* Setup an alternate signal stack */
	tls->signal_stack = mono_valloc (0, MONO_ARCH_SIGNAL_STACK_SIZE, MONO_MMAP_READ|MONO_MMAP_WRITE|MONO_MMAP_PRIVATE|MONO_MMAP_ANON);
	tls->signal_stack_size = MONO_ARCH_SIGNAL_STACK_SIZE;

	g_assert (tls->signal_stack);

	sa.ss_sp = tls->signal_stack;
	sa.ss_size = MONO_ARCH_SIGNAL_STACK_SIZE;
#if __APPLE__
	sa.ss_flags = 0;
#else
	sa.ss_flags = SS_ONSTACK;
#endif
	g_assert (sigaltstack (&sa, NULL) == 0);

	mono_gc_register_altstack ((char*)tls->stack_ovf_guard_base + tls->stack_ovf_guard_size, (char*)staddr + stsize - ((char*)tls->stack_ovf_guard_base + tls->stack_ovf_guard_size), tls->signal_stack, tls->signal_stack_size);
}

void
mono_free_altstack (MonoJitTlsData *tls)
{
	stack_t sa;
	int err;

	sa.ss_sp = tls->signal_stack;
	sa.ss_size = MONO_ARCH_SIGNAL_STACK_SIZE;
	sa.ss_flags = SS_DISABLE;
	err = sigaltstack  (&sa, NULL);
	g_assert (err == 0);

	if (tls->signal_stack)
		mono_vfree (tls->signal_stack, MONO_ARCH_SIGNAL_STACK_SIZE);
	if (tls->stack_ovf_valloced)
		mono_vfree (tls->stack_ovf_guard_base, tls->stack_ovf_guard_size);
	else
		mono_mprotect (tls->stack_ovf_guard_base, tls->stack_ovf_guard_size, MONO_MMAP_READ|MONO_MMAP_WRITE);
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

static G_GNUC_UNUSED void
try_more_restore (void)
{
	MonoJitTlsData *jit_tls = mono_native_tls_get_value (mono_jit_tls_id);
	if (try_restore_stack_protection (jit_tls, 500))
		jit_tls->restore_stack_prot = NULL;
}

static G_GNUC_UNUSED void
restore_stack_protection (void)
{
	MonoJitTlsData *jit_tls = mono_native_tls_get_value (mono_jit_tls_id);
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
mono_altstack_restore_prot (mgreg_t *regs, guint8 *code, gpointer *tramp_data, guint8* tramp)
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
			mono_runtime_printf_err ("Stack overflow in unmanaged: IP: %p, fault addr: %p", mono_arch_ip_from_context (ctx), fault_addr);
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

typedef struct {
	MonoMethod *omethod;
	int count;
} PrintOverflowUserData;

static gboolean
print_overflow_stack_frame (StackFrameInfo *frame, MonoContext *ctx, gpointer data)
{
	MonoMethod *method = NULL;
	PrintOverflowUserData *user_data = data;
	gchar *location;

	if (frame->ji)
		method = frame->ji->method;

	if (method) {
		if (user_data->count == 0) {
			/* The first frame is in its prolog, so a line number cannot be computed */
			user_data->count ++;
			return FALSE;
		}

		/* If this is a one method overflow, skip the other instances */
		if (method == user_data->omethod)
			return FALSE;

		location = mono_debug_print_stack_frame (method, frame->native_offset, mono_domain_get ());
		mono_runtime_printf_err ("  %s", location);
		g_free (location);

		if (user_data->count == 1) {
			mono_runtime_printf_err ("  <...>");
			user_data->omethod = method;
		} else {
			user_data->omethod = NULL;
		}

		user_data->count ++;
	} else
		mono_runtime_printf_err ("  at <unknown> <0x%05x>", frame->native_offset);

	return FALSE;
}

void
mono_handle_hard_stack_ovf (MonoJitTlsData *jit_tls, MonoJitInfo *ji, void *ctx, guint8* fault_addr)
{
	PrintOverflowUserData ud;
	MonoContext mctx;

	/* we don't do much now, but we can warn the user with a useful message */
	mono_runtime_printf_err ("Stack overflow: IP: %p, fault addr: %p", mono_arch_ip_from_context (ctx), fault_addr);

#ifdef MONO_ARCH_HAVE_SIGCTX_TO_MONOCTX
	mono_arch_sigctx_to_monoctx (ctx, &mctx);
			
	mono_runtime_printf_err ("Stacktrace:");

	memset (&ud, 0, sizeof (ud));

	mono_walk_stack_with_ctx (print_overflow_stack_frame, &mctx, MONO_UNWIND_LOOKUP_ACTUAL_METHOD, &ud);
#else
	if (ji && ji->method)
		mono_runtime_printf_err ("At %s", mono_method_full_name (ji->method, TRUE));
	else
		mono_runtime_printf_err ("At <unmanaged>.");
#endif

	_exit (1);
}

static gboolean
print_stack_frame_to_stderr (StackFrameInfo *frame, MonoContext *ctx, gpointer data)
{
	MonoMethod *method = NULL;
	if (frame->ji)
		method = frame->ji->method;

	if (method) {
		gchar *location = mono_debug_print_stack_frame (method, frame->native_offset, mono_domain_get ());
		mono_runtime_printf_err ("  %s", location);
		g_free (location);
	} else
		mono_runtime_printf_err ("  at <unknown> <0x%05x>", frame->native_offset);

	return FALSE;
}

static G_GNUC_UNUSED gboolean
print_stack_frame_to_string (StackFrameInfo *frame, MonoContext *ctx, gpointer data)
{
	GString *p = (GString*)data;
	MonoMethod *method = NULL;
	if (frame->ji)
		method = frame->ji->method;

	if (method) {
		gchar *location = mono_debug_print_stack_frame (method, frame->native_offset, mono_domain_get ());
		g_string_append_printf (p, "  %s\n", location);
		g_free (location);
	} else
		g_string_append_printf (p, "  at <unknown> <0x%05x>\n", frame->native_offset);

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
	MonoJitTlsData *jit_tls = mono_native_tls_get_value (mono_jit_tls_id);

	if (handling_sigsegv)
		return;

	if (mini_get_debug_options ()->suspend_on_sigsegv) {
		mono_runtime_printf_err ("Received SIGSEGV, suspending...");
		while (1)
			;
	}

	/* To prevent infinite loops when the stack walk causes a crash */
	handling_sigsegv = TRUE;

	/* !jit_tls means the thread was not registered with the runtime */
	if (jit_tls && mono_thread_internal_current ()) {
		mono_runtime_printf_err ("Stacktrace:\n");

		mono_walk_stack (print_stack_frame_to_stderr, TRUE, NULL);
	}

#ifdef HAVE_BACKTRACE_SYMBOLS
 {
	void *array [256];
	char **names;
	int i, size;
	const char *signal_str = (signal == SIGSEGV) ? "SIGSEGV" : "SIGABRT";

	mono_runtime_printf_err ("\nNative stacktrace:\n");

	size = backtrace (array, 256);
	names = backtrace_symbols (array, size);
	for (i =0; i < size; ++i) {
		mono_runtime_printf_err ("\t%s", names [i]);
	}
	free (names);

	/* Try to get more meaningful information using gdb */

#if !defined(HOST_WIN32) && defined(HAVE_SYS_SYSCALL_H) && defined(SYS_fork)
	if (!mini_get_debug_options ()->no_gdb_backtrace && !mono_debug_using_mono_debugger ()) {
		/* From g_spawn_command_line_sync () in eglib */
		pid_t pid;
		int status;
		pid_t crashed_pid = getpid ();

		//pid = fork ();
		/*
		 * glibc fork acquires some locks, so if the crash happened inside malloc/free,
		 * it will deadlock. Call the syscall directly instead.
		 */
		pid = mono_runtime_syscall_fork ();

		if (pid == 0) {
			dup2 (STDERR_FILENO, STDOUT_FILENO);

			mono_gdb_render_native_backtraces (crashed_pid);
			exit (1);
		}

		mono_runtime_printf_err ("\nDebug info from gdb:\n");
		waitpid (pid, &status, 0);
	}
#endif
	/*
	 * A SIGSEGV indicates something went very wrong so we can no longer depend
	 * on anything working. So try to print out lots of diagnostics, starting 
	 * with ones which have a greater chance of working.
	 */
	mono_runtime_printf_err (
			 "\n"
			 "=================================================================\n"
			 "Got a %s while executing native code. This usually indicates\n"
			 "a fatal error in the mono runtime or one of the native libraries \n"
			 "used by your application.\n"
			 "=================================================================\n",
			signal_str);

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

static void
mono_print_thread_dump_internal (void *sigctx, MonoContext *start_ctx)
{
	MonoInternalThread *thread = mono_thread_internal_current ();
#ifdef MONO_ARCH_HAVE_SIGCTX_TO_MONOCTX
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

#ifndef HOST_WIN32
	wapi_desc = wapi_current_thread_desc ();
	g_string_append_printf (text, " tid=0x%p this=0x%p %s\n", (gpointer)(gsize)thread->tid, thread,  wapi_desc);
	free (wapi_desc);
#endif

#ifdef MONO_ARCH_HAVE_SIGCTX_TO_MONOCTX
	if (start_ctx) {
		memcpy (&ctx, start_ctx, sizeof (MonoContext));
	} else if (!sigctx)
		MONO_INIT_CONTEXT_FROM_FUNC (&ctx, mono_print_thread_dump);
	else
		mono_arch_sigctx_to_monoctx (sigctx, &ctx);

	mono_walk_stack_with_ctx (print_stack_frame_to_string, &ctx, MONO_UNWIND_LOOKUP_ALL, text);
#else
	mono_runtime_printf ("\t<Stack traces in thread dumps not supported on this platform>");
#endif

	mono_runtime_printf ("%s", text->str);

#if PLATFORM_WIN32 && TARGET_WIN32 && _DEBUG
	OutputDebugStringA(text->str);
#endif

	g_string_free (text, TRUE);
	mono_runtime_stdout_fflush ();
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
	mono_print_thread_dump_internal (sigctx, NULL);
}

void
mono_print_thread_dump_from_ctx (MonoContext *ctx)
{
	mono_print_thread_dump_internal (NULL, ctx);
}

/*
 * mono_resume_unwind:
 *
 *   This is called by a trampoline from LLVM compiled finally clauses to continue
 * unwinding.
 */
void
mono_resume_unwind (MonoContext *ctx)
{
	MonoJitTlsData *jit_tls = mono_native_tls_get_value (mono_jit_tls_id);
	static void (*restore_context) (MonoContext *);
	MonoContext new_ctx;

	MONO_CONTEXT_SET_IP (ctx, MONO_CONTEXT_GET_IP (&jit_tls->resume_state.ctx));
	MONO_CONTEXT_SET_SP (ctx, MONO_CONTEXT_GET_SP (&jit_tls->resume_state.ctx));
	new_ctx = *ctx;

	mono_handle_exception_internal (&new_ctx, jit_tls->resume_state.ex_obj, TRUE, NULL);

	if (!restore_context)
		restore_context = mono_get_restore_context ();

	restore_context (&new_ctx);
}

#ifdef MONO_ARCH_HAVE_HANDLER_BLOCK_GUARD

typedef struct {
	MonoJitInfo *ji;
	MonoContext ctx;
	MonoJitExceptionInfo *ei;
} FindHandlerBlockData;

static gboolean
find_last_handler_block (StackFrameInfo *frame, MonoContext *ctx, gpointer data)
{
	int i;
	gpointer ip;
	FindHandlerBlockData *pdata = data;
	MonoJitInfo *ji = frame->ji;

	if (!ji)
		return FALSE;

	if (ji->method->wrapper_type)
		return FALSE;

	ip = MONO_CONTEXT_GET_IP (ctx);

	for (i = 0; i < ji->num_clauses; ++i) {
		MonoJitExceptionInfo *ei = ji->clauses + i;
		if (ei->flags != MONO_EXCEPTION_CLAUSE_FINALLY)
			continue;
		/*If ip points to the first instruction it means the handler block didn't start
		 so we can leave its execution to the EH machinery*/
		if (ei->handler_start < ip && ip < ei->data.handler_end) {
			pdata->ji = ji;
			pdata->ei = ei;
			pdata->ctx = *ctx;
			break;
		}
	}
	return FALSE;
}


static gpointer
install_handler_block_guard (MonoJitInfo *ji, MonoContext *ctx)
{
	int i;
	MonoJitExceptionInfo *clause = NULL;
	gpointer ip;

	ip = MONO_CONTEXT_GET_IP (ctx);

	for (i = 0; i < ji->num_clauses; ++i) {
		clause = &ji->clauses [i];
		if (clause->flags != MONO_EXCEPTION_CLAUSE_FINALLY)
			continue;
		if (clause->handler_start < ip && clause->data.handler_end > ip)
			break;
	}

	/*no matching finally */
	if (i == ji->num_clauses)
		return NULL;

	/*If we stopped on the instruction right before the try, we haven't actually started executing it*/
	if (ip == clause->handler_start)
		return NULL;

	return mono_arch_install_handler_block_guard (ji, clause, ctx, mono_create_handler_block_trampoline ());
}

/*
 * Finds the bottom handler block running and install a block guard if needed.
 * FIXME add full-aot support.
 */
gboolean
mono_install_handler_block_guard (MonoThreadUnwindState *ctx)
{
	FindHandlerBlockData data = { 0 };
	MonoJitTlsData *jit_tls = ctx->unwind_data [MONO_UNWIND_DATA_JIT_TLS];
	gpointer resume_ip;

	/* FIXME */
	if (mono_aot_only)
		return FALSE;

	/* Guard against a null MonoJitTlsData. This can happens if the thread receives the
         * interrupt signal before the JIT has time to initialize its TLS data for the given thread.
	 */
	if (!jit_tls || jit_tls->handler_block_return_address)
		return FALSE;

	mono_walk_stack_with_state (find_last_handler_block, ctx, MONO_UNWIND_SIGNAL_SAFE, &data);

	if (!data.ji)
		return FALSE;

	memcpy (&jit_tls->handler_block_context, &data.ctx, sizeof (MonoContext));

	resume_ip = install_handler_block_guard (data.ji, &data.ctx);
	if (resume_ip == NULL)
		return FALSE;

	jit_tls->handler_block_return_address = resume_ip;
	jit_tls->handler_block = data.ei;

	return TRUE;
}

#else
gboolean
mono_install_handler_block_guard (MonoThreadUnwindState *ctx)
{
	return FALSE;
}

#endif

void
mono_set_cast_details (MonoClass *from, MonoClass *to)
{
	MonoJitTlsData *jit_tls = NULL;

	if (mini_get_debug_options ()->better_cast_details) {
		jit_tls = mono_native_tls_get_value (mono_jit_tls_id);
		jit_tls->class_cast_from = from;
		jit_tls->class_cast_to = to;
	}
}


/*returns false if the thread is not attached*/
gboolean
mono_thread_state_init_from_sigctx (MonoThreadUnwindState *ctx, void *sigctx)
{
#ifdef MONO_ARCH_HAVE_SIGCTX_TO_MONOCTX
	MonoInternalThread *thread = mono_thread_internal_current ();
	if (!thread || !thread->jit_data) {
		ctx->valid = FALSE;
		return FALSE;
	}

	if (sigctx)
		mono_arch_sigctx_to_monoctx (sigctx, &ctx->ctx);
	else
#if MONO_ARCH_HAS_MONO_CONTEXT && !defined(MONO_CROSS_COMPILE)
		MONO_CONTEXT_GET_CURRENT (ctx->ctx);
#else
		g_error ("Use a null sigctx requires a working mono-context");
#endif

	ctx->unwind_data [MONO_UNWIND_DATA_DOMAIN] = mono_domain_get ();
	ctx->unwind_data [MONO_UNWIND_DATA_LMF] = mono_get_lmf ();
	ctx->unwind_data [MONO_UNWIND_DATA_JIT_TLS] = thread->jit_data;
	ctx->valid = TRUE;
	return TRUE;
#else
	g_error ("Implement mono_arch_sigctx_to_monoctx for the current target");
	return FALSE;
#endif
}

gboolean
mono_thread_state_init_from_monoctx (MonoThreadUnwindState *ctx, MonoContext *mctx)
{
	MonoInternalThread *thread = mono_thread_internal_current ();
	if (!thread || !thread->jit_data) {
		ctx->valid = FALSE;
		return FALSE;
	}

	ctx->ctx = *mctx;
	ctx->unwind_data [MONO_UNWIND_DATA_DOMAIN] = mono_domain_get ();
	ctx->unwind_data [MONO_UNWIND_DATA_LMF] = mono_get_lmf ();
	ctx->unwind_data [MONO_UNWIND_DATA_JIT_TLS] = thread->jit_data;
	ctx->valid = TRUE;
	return TRUE;
}

/*returns false if the thread is not attached*/
gboolean
mono_thread_state_init_from_current (MonoThreadUnwindState *ctx)
{
	MonoInternalThread *thread = mono_thread_internal_current ();
	MONO_ARCH_CONTEXT_DEF

	mono_arch_flush_register_windows ();

	if (!thread || !thread->jit_data) {
		ctx->valid = FALSE;
		return FALSE;
	}
#ifdef MONO_INIT_CONTEXT_FROM_CURRENT
	MONO_INIT_CONTEXT_FROM_CURRENT (&ctx->ctx);
#else
	MONO_INIT_CONTEXT_FROM_FUNC (&ctx->ctx, mono_thread_state_init_from_current);
#endif
		
	ctx->unwind_data [MONO_UNWIND_DATA_DOMAIN] = mono_domain_get ();
	ctx->unwind_data [MONO_UNWIND_DATA_LMF] = mono_get_lmf ();
	ctx->unwind_data [MONO_UNWIND_DATA_JIT_TLS] = thread->jit_data;
	ctx->valid = TRUE;
	return TRUE;
}

static void
mono_raise_exception_with_ctx (MonoException *exc, MonoContext *ctx)
{
	void (*restore_context) (MonoContext *);
	restore_context = mono_get_restore_context ();

	mono_handle_exception (ctx, exc);
	restore_context (ctx);
}

/*FIXME Move all monoctx -> sigctx conversion to signal handlers once all archs support utils/mono-context */
void
mono_setup_async_callback (MonoContext *ctx, void (*async_cb)(void *fun), gpointer user_data)
{
#ifdef MONO_ARCH_HAVE_SETUP_ASYNC_CALLBACK
	MonoJitTlsData *jit_tls = mono_native_tls_get_value (mono_jit_tls_id);
	jit_tls->ex_ctx = *ctx;

	mono_arch_setup_async_callback (ctx, async_cb, user_data);
#else
	g_error ("This target doesn't support mono_arch_setup_async_callback");
#endif
}

void
mono_install_unhandled_exception_hook (MonoUnhandledExceptionFunc func, gpointer user_data)
{
	unhandled_exception_hook = func;
	unhandled_exception_hook_data = user_data;
}

void
mono_invoke_unhandled_exception_hook (MonoObject *exc)
{
	if (unhandled_exception_hook) {
		unhandled_exception_hook (exc, unhandled_exception_hook_data);
	} else {
		MonoObject *other = NULL;
		MonoString *str = mono_object_to_string (exc, &other);
		if (str) {
			char *msg = mono_string_to_utf8 (str);
			mono_runtime_printf_err ("[ERROR] FATAL UNHANDLED EXCEPTION: %s", msg);
			g_free (msg);
		}

#if defined(__APPLE__) && defined(__arm__)
		g_assertion_message ("Terminating runtime due to unhandled exception");
#else
		exit (mono_environment_exitcode_get ());
#endif
	}

	g_assert_not_reached ();
}
