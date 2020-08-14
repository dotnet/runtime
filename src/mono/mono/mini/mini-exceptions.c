/**
 * \file
 * generic exception support
 *
 * Authors:
 *   Dietmar Maurer (dietmar@ximian.com)
 *   Mono Team (mono-list@lists.ximian.com)
 *
 * Copyright 2001-2003 Ximian, Inc.
 * Copyright 2003-2008 Novell, Inc.
 * Copyright 2011 Xamarin Inc (http://www.xamarin.com).
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */

#include <config.h>
#include <glib.h>
#include <string.h>
#include <signal.h>

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

#ifdef HAVE_SYS_PRCTL_H
#include <sys/prctl.h>
#endif

#ifdef HAVE_UNWIND_H
#include <unwind.h>
#endif

#include <mono/metadata/appdomain.h>
#include <mono/metadata/tabledefs.h>
#include <mono/metadata/threads.h>
#include <mono/metadata/threads-types.h>
#include <mono/metadata/debug-helpers.h>
#include <mono/metadata/exception.h>
#include <mono/metadata/exception-internals.h>
#include <mono/metadata/object-internals.h>
#include <mono/metadata/reflection-internals.h>
#include <mono/metadata/gc-internals.h>
#include <mono/metadata/debug-internals.h>
#include <mono/metadata/mono-debug.h>
#include <mono/metadata/profiler-private.h>
#include <mono/metadata/mono-endian.h>
#include <mono/metadata/environment.h>
#include <mono/metadata/mono-mlist.h>
#include <mono/metadata/handle.h>
#include <mono/utils/mono-merp.h>
#include <mono/utils/mono-mmap.h>
#include <mono/utils/mono-logger-internals.h>
#include <mono/utils/mono-error.h>
#include <mono/utils/mono-error-internals.h>
#include <mono/utils/mono-state.h>
#include <mono/utils/mono-threads-debug.h>

#include "mini.h"
#include "trace.h"
#include "debugger-agent.h"
#include "debugger-engine.h"
#include "seq-points.h"
#include "llvm-runtime.h"
#include "mini-llvm.h"
#include "aot-runtime.h"
#include "mini-runtime.h"
#include "interp/interp.h"

#ifdef ENABLE_LLVM
#include "mini-llvm-cpp.h"
#endif

#ifdef TARGET_ARM
#include "mini-arm.h"
#endif

#ifndef MONO_ARCH_CONTEXT_DEF
#define MONO_ARCH_CONTEXT_DEF
#endif

#if !defined(DISABLE_CRASH_REPORTING)
#include <gmodule.h>
#endif
#include "mono/utils/mono-tls-inline.h"

/*
 * Raw frame information is stored in MonoException.trace_ips as an IntPtr[].
 * This structure represents one entry.
 * This should consists of pointers only.
 */
typedef struct
{
	gpointer ip;
	gpointer generic_info;
	/* Only for interpreter frames */
	MonoJitInfo *ji;
}  ExceptionTraceIp;

/* Number of words in trace_ips belonging to one entry */
#define TRACE_IP_ENTRY_SIZE (sizeof (ExceptionTraceIp) / sizeof (gpointer))

static gpointer restore_context_func, call_filter_func;
static gpointer throw_exception_func, rethrow_exception_func, rethrow_preserve_exception_func;
static gpointer throw_corlib_exception_func;

static MonoFtnPtrEHCallback ftnptr_eh_callback;

static void mono_walk_stack_full (MonoJitStackWalk func, MonoContext *start_ctx, MonoDomain *domain, MonoJitTlsData *jit_tls, MonoLMF *lmf, MonoUnwindOptions unwind_options, gpointer user_data, gboolean crash_context);
static void mono_raise_exception_with_ctx (MonoException *exc, MonoContext *ctx);
static void mono_runtime_walk_stack_with_ctx (MonoJitStackWalk func, MonoContext *start_ctx, MonoUnwindOptions unwind_options, void *user_data);
static gboolean mono_current_thread_has_handle_block_guard (void);
static gboolean mono_install_handler_block_guard (MonoThreadUnwindState *ctx);
static void mono_uninstall_current_handler_block_guard (void);
static gboolean mono_exception_walk_trace_internal (MonoException *ex, MonoExceptionFrameWalk func, gpointer user_data);
static void throw_exception (MonoObject *ex, gboolean rethrow);

static void mono_summarize_managed_stack (MonoThreadSummary *out);
static void mono_summarize_unmanaged_stack (MonoThreadSummary *out);
static void mono_summarize_exception (MonoException *exc, MonoThreadSummary *out);
static void mono_crash_reporting_register_native_library (const char *module_path, const char *module_name);
static void mono_crash_reporting_allow_all_native_libraries (void);

static gboolean
first_managed (MonoStackFrameInfo *frame, MonoContext *ctx, gpointer addr)
{
	gpointer *data = (gpointer *)addr;

	if (!frame->managed)
		return FALSE;

	if (!ctx) {
		// FIXME: Happens with llvm_only
		*data = NULL;
		return TRUE;
	}

	*data = frame->frame_addr;
	g_assert (*data);
	return TRUE;
}

static gpointer
mono_thread_get_managed_sp (void)
{
	gpointer addr = NULL;
	mono_walk_stack (first_managed, MONO_UNWIND_SIGNAL_SAFE, &addr);
	return addr;
}

static void
mini_clear_abort_threshold (void)
{
	MonoJitTlsData *jit_tls = mono_get_jit_tls ();
	jit_tls->abort_exc_stack_threshold = NULL;
}

static void
mini_set_abort_threshold (StackFrameInfo *frame)
{
	gpointer sp = frame->frame_addr;
	MonoJitTlsData *jit_tls = mono_get_jit_tls ();
	// Only move it up, to avoid thrown/caught
	// exceptions lower in the stack from triggering
	// a rethrow
	gboolean above_threshold = (gsize) sp >= (gsize) jit_tls->abort_exc_stack_threshold;
	if (!jit_tls->abort_exc_stack_threshold || above_threshold) {
		jit_tls->abort_exc_stack_threshold = sp;
	}
}

// Note: In the case that the frame is above where the thread abort
// was set we bump the threshold so that functions called from the new,
// higher threshold don't trigger the thread abort exception
static gboolean
mini_above_abort_threshold (void)
{
	gpointer sp = mono_thread_get_managed_sp ();
	MonoJitTlsData *jit_tls = mono_tls_get_jit_tls ();

	if (!sp)
		return TRUE;

	gboolean above_threshold = (gsize) sp >= (gsize) jit_tls->abort_exc_stack_threshold;

	if (above_threshold)
		jit_tls->abort_exc_stack_threshold = sp;

	return above_threshold;
}

static int
mono_get_seq_point_for_native_offset (MonoDomain *domain, MonoMethod *method, gint32 native_offset)
{
	SeqPoint sp;
	if (mono_find_prev_seq_point_for_native_offset (domain, method, native_offset, NULL, &sp))
		return sp.il_offset;
	return -1;
}

void
mono_exceptions_init (void)
{
	MonoRuntimeExceptionHandlingCallbacks cbs;
	if (mono_ee_features.use_aot_trampolines) {
		restore_context_func = mono_aot_get_trampoline ("restore_context");
		call_filter_func = mono_aot_get_trampoline ("call_filter");
		throw_exception_func = mono_aot_get_trampoline ("throw_exception");
		rethrow_exception_func = mono_aot_get_trampoline ("rethrow_exception");
		rethrow_preserve_exception_func = mono_aot_get_trampoline ("rethrow_preserve_exception");
	} else if (!mono_llvm_only) {
		MonoTrampInfo *info;

		restore_context_func = mono_arch_get_restore_context (&info, FALSE);
		mono_tramp_info_register (info, NULL);
		call_filter_func = mono_arch_get_call_filter (&info, FALSE);
		mono_tramp_info_register (info, NULL);
		throw_exception_func = mono_arch_get_throw_exception (&info, FALSE);
		mono_tramp_info_register (info, NULL);
		rethrow_exception_func = mono_arch_get_rethrow_exception (&info, FALSE);
		mono_tramp_info_register (info, NULL);
		rethrow_preserve_exception_func = mono_arch_get_rethrow_preserve_exception (&info, FALSE);
		mono_tramp_info_register (info, NULL);
	}

	mono_arch_exceptions_init ();

	cbs.mono_walk_stack_with_ctx = mono_runtime_walk_stack_with_ctx;
	cbs.mono_walk_stack_with_state = mono_walk_stack_with_state;
	cbs.mono_summarize_managed_stack = mono_summarize_managed_stack;
	cbs.mono_summarize_unmanaged_stack = mono_summarize_unmanaged_stack;
	cbs.mono_summarize_exception = mono_summarize_exception;
	cbs.mono_register_native_library = mono_crash_reporting_register_native_library;
	cbs.mono_allow_all_native_libraries = mono_crash_reporting_allow_all_native_libraries;

	if (mono_llvm_only) {
		cbs.mono_raise_exception = mono_llvm_raise_exception;
		cbs.mono_reraise_exception = mono_llvm_reraise_exception;
	} else {
		cbs.mono_raise_exception = (void (*)(MonoException *))mono_get_throw_exception ();
		cbs.mono_reraise_exception = (void (*)(MonoException *))mono_get_rethrow_exception ();
	}
	cbs.mono_raise_exception_with_ctx = mono_raise_exception_with_ctx;
	cbs.mono_exception_walk_trace = mono_exception_walk_trace;
	cbs.mono_install_handler_block_guard = mono_install_handler_block_guard;
	cbs.mono_uninstall_current_handler_block_guard = mono_uninstall_current_handler_block_guard;
	cbs.mono_current_thread_has_handle_block_guard = mono_current_thread_has_handle_block_guard;
	cbs.mono_clear_abort_threshold = mini_clear_abort_threshold;
	cbs.mono_above_abort_threshold = mini_above_abort_threshold;
	mono_install_eh_callbacks (&cbs);
	mono_install_get_seq_point (mono_get_seq_point_for_native_offset);
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
mono_get_rethrow_preserve_exception (void)
{
	g_assert (rethrow_preserve_exception_func);
	return rethrow_preserve_exception_func;
}

static void
no_call_filter (void)
{
	g_assert_not_reached ();
}

gpointer
mono_get_call_filter (void)
{
	/* This is called even in llvmonly mode etc. */
	if (!call_filter_func)
		return (gpointer)no_call_filter;
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

	if (mono_ee_features.use_aot_trampolines)
		code = mono_aot_get_trampoline ("throw_corlib_exception");
	else {
		code = mono_arch_get_throw_corlib_exception (&info, FALSE);
		mono_tramp_info_register (info, NULL);
	}

	mono_memory_barrier ();

	throw_corlib_exception_func = code;

	return throw_corlib_exception_func;
}

/*
 * mono_get_throw_exception_addr:
 *
 *   Return an address which stores the result of
 * mono_get_throw_exception.
 */
gpointer
mono_get_throw_exception_addr (void)
{
	return &throw_exception_func;
}

gpointer
mono_get_rethrow_preserve_exception_addr (void)
{
	return &rethrow_preserve_exception_func;
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

#ifdef MONO_ARCH_HAVE_UNWIND_BACKTRACE

#if 0
static gboolean show_native_addresses = TRUE;
#else
static gboolean show_native_addresses = FALSE;
#endif

static _Unwind_Reason_Code
build_stack_trace (struct _Unwind_Context *frame_ctx, void *state)
{
	MonoDomain *domain = mono_domain_get ();
	uintptr_t ip = _Unwind_GetIP (frame_ctx);

	if (show_native_addresses || mono_jit_info_table_find (domain, (char*)ip)) {
		GList **trace_ips = (GList **)state;
		*trace_ips = g_list_prepend (*trace_ips, (gpointer)ip);
	}

	return _URC_NO_REASON;
}

static GSList*
get_unwind_backtrace (void)
{
	GSList *ips = NULL;

	_Unwind_Backtrace (build_stack_trace, &ips);

	return g_slist_reverse (ips);
}

#else

static GSList*
get_unwind_backtrace (void)
{
	return NULL;
}

#endif

static gboolean
arch_unwind_frame (MonoDomain *domain, MonoJitTlsData *jit_tls,
				   MonoJitInfo *ji, MonoContext *ctx,
				   MonoContext *new_ctx, MonoLMF **lmf,
				   host_mgreg_t **save_locations,
				   StackFrameInfo *frame)
{
	if (!ji && *lmf) {
		if (((gsize)(*lmf)->previous_lmf) & 2) {
			MonoLMFExt *ext = (MonoLMFExt*)(*lmf);

			memset (frame, 0, sizeof (StackFrameInfo));
			frame->ji = ji;

			*new_ctx = *ctx;

			if (ext->kind == MONO_LMFEXT_DEBUGGER_INVOKE) {
				/*
				 * This LMF entry is created by the soft debug code to mark transitions to
				 * managed code done during invokes.
				 */
				frame->type = FRAME_TYPE_DEBUGGER_INVOKE;
				memcpy (new_ctx, &ext->ctx, sizeof (MonoContext));
			} else if (ext->kind == MONO_LMFEXT_INTERP_EXIT || ext->kind == MONO_LMFEXT_INTERP_EXIT_WITH_CTX) {
				frame->type = FRAME_TYPE_INTERP_TO_MANAGED;
				frame->interp_exit_data = ext->interp_exit_data;
				if (ext->kind == MONO_LMFEXT_INTERP_EXIT_WITH_CTX) {
					frame->type = FRAME_TYPE_INTERP_TO_MANAGED_WITH_CTX;
					memcpy (new_ctx, &ext->ctx, sizeof (MonoContext));
				}
			} else {
				g_assert_not_reached ();
			}

			*lmf = (MonoLMF *)(((gsize)(*lmf)->previous_lmf) & ~3);

			return TRUE;
		}
	}

	return mono_arch_unwind_frame (domain, jit_tls, ji, ctx, new_ctx, lmf, save_locations, frame);
}

/*
 * find_jit_info:
 *
 * Translate between the mono_arch_unwind_frame function and the old API.
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

	err = arch_unwind_frame (domain, jit_tls, ji, ctx, new_ctx, lmf, NULL, &frame);
	if (!err)
		return (MonoJitInfo *)-1;

	if (*lmf && ((*lmf) != jit_tls->first_lmf) && ((gpointer)MONO_CONTEXT_GET_SP (new_ctx) >= (gpointer)(*lmf))) {
		/*
		 * Remove any unused lmf.
		 * Mask out the lower bits which might be used to hold additional information.
		 */
		*lmf = (MonoLMF *)(((gsize)(*lmf)->previous_lmf) & ~(TARGET_SIZEOF_VOID_P -1));
	}

	/* Convert between the new and the old APIs */
	switch (frame.type) {
	case FRAME_TYPE_MANAGED:
		if (managed)
			*managed = TRUE;
		return frame.ji;
	case FRAME_TYPE_TRAMPOLINE:
		return frame.ji;
	case FRAME_TYPE_MANAGED_TO_NATIVE:
		if (frame.ji)
			return frame.ji;
		else {
			memset (res, 0, sizeof (MonoJitInfo));
			res->d.method = frame.method;
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
	MonoMethod *method = NULL;

	if (trace)
		*trace = NULL;

	if (native_offset)
		*native_offset = -1;

	if (managed)
		*managed = FALSE;

	ji = find_jit_info (domain, jit_tls, res, prev_ji, ctx, new_ctx, lmf, &managed2);

	if (ji == (gpointer)-1)
		return ji;

	if (ji && !ji->is_trampoline)
		method = jinfo_get_method (ji);

	if (managed2 || (method && method->wrapper_type)) {
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
			if (!method->wrapper_type || method->wrapper_type == MONO_WRAPPER_DYNAMIC_METHOD)
				*managed = TRUE;

		if (trace)
			*trace = mono_debug_print_stack_frame (method, offset, domain);
	} else {
		if (trace) {
			char *fname = mono_method_full_name (jinfo_get_method (res), TRUE);
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
 * If ASYNC true, this function will be async safe, but some fields of frame and frame->ji will
 * not be set.
 */
gboolean
mono_find_jit_info_ext (MonoDomain *domain, MonoJitTlsData *jit_tls, 
						MonoJitInfo *prev_ji, MonoContext *ctx,
						MonoContext *new_ctx, char **trace, MonoLMF **lmf,
						host_mgreg_t **save_locations,
						StackFrameInfo *frame)
{
	gboolean err;
	gpointer ip = MONO_CONTEXT_GET_IP (ctx);
	MonoJitInfo *ji;
	MonoDomain *target_domain = domain;
	MonoMethod *method = NULL;
	gboolean async = mono_thread_info_is_async_context ();

	if (trace)
		*trace = NULL;

	/* Avoid costly table lookup during stack overflow */
	if (prev_ji && (ip > prev_ji->code_start && ((guint8*)ip < ((guint8*)prev_ji->code_start) + prev_ji->code_size)))
		ji = prev_ji;
	else
		ji = mini_jit_info_table_find_ext (domain, ip, TRUE, &target_domain);

	if (!target_domain)
		target_domain = domain;

	if (save_locations)
		memset (save_locations, 0, MONO_MAX_IREGS * sizeof (host_mgreg_t*));

	err = arch_unwind_frame (target_domain, jit_tls, ji, ctx, new_ctx, lmf, save_locations, frame);
	if (!err)
		return FALSE;

	gboolean not_i2m = frame->type != FRAME_TYPE_INTERP_TO_MANAGED && frame->type != FRAME_TYPE_INTERP_TO_MANAGED_WITH_CTX;

	if (not_i2m && *lmf && ((*lmf) != jit_tls->first_lmf) && ((gpointer)MONO_CONTEXT_GET_SP (new_ctx) >= (gpointer)(*lmf))) {
		/*
		 * Remove any unused lmf.
		 * Mask out the lower bits which might be used to hold additional information.
		 */
		*lmf = (MonoLMF *)(((gsize)(*lmf)->previous_lmf) & ~(TARGET_SIZEOF_VOID_P -1));
	}

	if (frame->ji && !frame->ji->is_trampoline && !frame->ji->async)
		method = jinfo_get_method (frame->ji);

	if (frame->type == FRAME_TYPE_MANAGED && method) {
		if (!method->wrapper_type || method->wrapper_type == MONO_WRAPPER_DYNAMIC_METHOD)
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
	frame->async_context = async;
	frame->frame_addr = MONO_CONTEXT_GET_SP (ctx);

	ji = frame->ji;

	if (frame->type == FRAME_TYPE_MANAGED)
		frame->method = method;

	if (ji && (frame->managed || (method && method->wrapper_type))) {
		const char *real_ip, *start;

		start = (const char *)ji->code_start;
		if (frame->type == FRAME_TYPE_MANAGED)
			real_ip = (const char*)ip;
		else
			/* ctx->ip points into native code */
			real_ip = (const char*)MONO_CONTEXT_GET_IP (new_ctx);

		if ((real_ip >= start) && (real_ip <= start + ji->code_size))
			frame->native_offset = real_ip - start;
		else {
			frame->native_offset = -1;
		}

		if (trace)
			*trace = mono_debug_print_stack_frame (method, frame->native_offset, domain);
	} else {
		if (trace && frame->method) {
			char *fname = mono_method_full_name (frame->method, TRUE);
			*trace = g_strdup_printf ("in (unmanaged) %s", fname);
			g_free (fname);
		}
	}

	return TRUE;
}

typedef struct {
	gboolean in_interp;
	MonoInterpStackIter interp_iter;
	gpointer last_frame_addr;
} Unwinder;

static void
unwinder_init (Unwinder *unwinder)
{
	memset (unwinder, 0, sizeof (Unwinder));
}

#if defined(__GNUC__) && defined(TARGET_ARM64)
/* gcc 4.9.2 seems to miscompile this on arm64 */
static __attribute__((optimize("O0"))) gboolean
#else
static gboolean
#endif
unwinder_unwind_frame (Unwinder *unwinder,
					   MonoDomain *domain, MonoJitTlsData *jit_tls,
					   MonoJitInfo *prev_ji, MonoContext *ctx,
					   MonoContext *new_ctx, char **trace, MonoLMF **lmf,
					   host_mgreg_t **save_locations,
					   StackFrameInfo *frame)
{
	if (unwinder->in_interp) {
		memcpy (new_ctx, ctx, sizeof (MonoContext));

		/* Process debugger invokes */
		/* The DEBUGGER_INVOKE should be returned before the first interpreter frame for the invoke */
		if (unwinder->last_frame_addr < (gpointer)(*lmf)) {
			if (((gsize)(*lmf)->previous_lmf) & 2) {
				MonoLMFExt *ext = (MonoLMFExt*)(*lmf);
				if (ext->kind == MONO_LMFEXT_DEBUGGER_INVOKE) {
					*lmf = (MonoLMF *)(((gsize)(*lmf)->previous_lmf) & ~7);
					frame->type = FRAME_TYPE_DEBUGGER_INVOKE;
					return TRUE;
				}
			}
		}

		unwinder->in_interp = mini_get_interp_callbacks ()->frame_iter_next (&unwinder->interp_iter, frame);
		if (frame->type == FRAME_TYPE_INTERP) {
			const gpointer parent = mini_get_interp_callbacks ()->frame_get_parent (frame->interp_frame);
			unwinder->last_frame_addr = parent;
		}

		if (!unwinder->in_interp)
			return unwinder_unwind_frame (unwinder, domain, jit_tls, prev_ji, ctx, new_ctx, trace, lmf, save_locations, frame);
		return TRUE;
	} else {
		gboolean res = mono_find_jit_info_ext (domain, jit_tls, prev_ji, ctx, new_ctx, trace, lmf,
											   save_locations, frame);
		if (!res)
			return FALSE;
		if (frame->type == FRAME_TYPE_INTERP_TO_MANAGED || frame->type == FRAME_TYPE_INTERP_TO_MANAGED_WITH_CTX) {
			unwinder->in_interp = TRUE;
			mini_get_interp_callbacks ()->frame_iter_init (&unwinder->interp_iter, frame->interp_exit_data);
		}
		unwinder->last_frame_addr = frame->frame_addr;
		return TRUE;
	}
}

/*
 * This function is async-safe.
 */
static gpointer
get_generic_info_from_stack_frame (MonoJitInfo *ji, MonoContext *ctx)
{
	MonoGenericJitInfo *gi;
	MonoMethod *method;
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
		int offset = (gsize)MONO_CONTEXT_GET_IP (ctx) - (gsize)ji->code_start;
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

	method = jinfo_get_method (ji);
	if (mono_method_get_context (method)->method_inst) {
		/* A MonoMethodRuntimeGenericContext* */
		return info;
	} else if ((method->flags & METHOD_ATTRIBUTE_STATIC) || m_class_is_valuetype (method->klass)) {
		/* A MonoVTable* */
		return info;
	} else {
		/* Avoid returning a managed object */
		MonoObject *this_obj = (MonoObject *)info;

		return this_obj->vtable;
	}
}

/*
 * generic_info is either a MonoMethodRuntimeGenericContext or a MonoVTable.
 */
MonoGenericContext
mono_get_generic_context_from_stack_frame (MonoJitInfo *ji, gpointer generic_info)
{
	MonoGenericContext context = { NULL, NULL };
	MonoClass *klass, *method_container_class;
	MonoMethod *method;

	g_assert (generic_info);

	method = jinfo_get_method (ji);
	g_assert (method->is_inflated);
	if (mono_method_get_context (method)->method_inst) {
		MonoMethodRuntimeGenericContext *mrgctx = (MonoMethodRuntimeGenericContext *)generic_info;

		klass = mrgctx->class_vtable->klass;
		context.method_inst = mrgctx->method_inst;
		g_assert (context.method_inst);
	} else {
		MonoVTable *vtable = (MonoVTable *)generic_info;

		klass = vtable->klass;
	}

	//g_assert (!mono_class_is_gtd (method->klass));
	if (mono_class_is_ginst (method->klass))
		method_container_class = mono_class_get_generic_class (method->klass)->container_class;
	else
		method_container_class = method->klass;

	/* class might refer to a subclass of method's class */
	while (!(klass == method->klass || (mono_class_is_ginst (klass) && mono_class_get_generic_class (klass)->container_class == method_container_class))) {
		klass = m_class_get_parent (klass);
		g_assert (klass);
	}

	if (mono_class_is_ginst (klass) || mono_class_is_gtd (klass))
		context.class_inst = mini_class_get_context (klass)->class_inst;

	if (mono_class_is_ginst (klass))
		g_assert (mono_class_has_parent_and_ignore_generics (mono_class_get_generic_class (klass)->container_class, method_container_class));
	else
		g_assert (mono_class_has_parent_and_ignore_generics (klass, method_container_class));

	return context;
}


static MonoMethod*
get_method_from_stack_frame (MonoJitInfo *ji, gpointer generic_info)
{
	ERROR_DECL (error);
	MonoGenericContext context;
	MonoMethod *method;
	
	if (!ji->has_generic_jit_info || !mono_jit_info_get_generic_jit_info (ji)->has_this)
		return jinfo_get_method (ji);
	context = mono_get_generic_context_from_stack_frame (ji, generic_info);

	method = jinfo_get_method (ji);
	method = mono_method_get_declaring_generic_method (method);
	method = mono_class_inflate_generic_method_checked (method, &context, error);
	g_assert (is_ok (error)); /* FIXME don't swallow the error */

	return method;
}

/**
 * mono_exception_walk_native_trace:
 * \param ex The exception object whose frames should be walked
 * \param func callback to call for each stack frame
 * \param user_data data passed to the callback
 * This function walks the stacktrace of an exception. For
 * each frame the callback function is called with the relevant info.
 * The walk ends when no more stack frames are found or when the callback
 * returns a TRUE value.
 */
 
gboolean
mono_exception_walk_trace (MonoException *ex, MonoExceptionFrameWalk func, gpointer user_data)
{
	gboolean res;

	MONO_ENTER_GC_UNSAFE;
	res = mono_exception_walk_trace_internal (ex, func, user_data);
	MONO_EXIT_GC_UNSAFE;
	return res;
}

static gboolean
mono_exception_stackframe_obj_walk (MonoStackFrame *captured_frame, MonoExceptionFrameWalk func, gpointer user_data)
{
	if (!captured_frame)
		return TRUE;

	gpointer ip = (gpointer) (captured_frame->method_address + captured_frame->native_offset);
	MonoJitInfo *ji = mono_jit_info_table_find_internal (mono_domain_get (), ip, TRUE, TRUE);

	// Other domain maybe?
	if (!ji)
		return FALSE;
	MonoMethod *method = jinfo_get_method (ji);

	gboolean r = func (method, (gpointer) captured_frame->method_address, captured_frame->native_offset, TRUE, user_data);
	if (r)
		return TRUE;

	return FALSE;
}

static gboolean
mono_exception_stacktrace_obj_walk (MonoStackTrace *st, MonoExceptionFrameWalk func, gpointer user_data)
{
	int num_captured = st->captured_traces ? mono_array_length_internal (st->captured_traces) : 0;
	for (int i=0; i < num_captured; i++) {
		MonoStackTrace *curr_trace = mono_array_get_fast (st->captured_traces, MonoStackTrace *, i);
		mono_exception_stacktrace_obj_walk (curr_trace, func, user_data);
	}

	int num_frames = st->frames ? mono_array_length_internal (st->frames) : 0;
	for (int frame = 0; frame < num_frames; frame++) {
		gboolean r = mono_exception_stackframe_obj_walk (mono_array_get_fast (st->frames, MonoStackFrame *, frame), func, user_data);
		if (r)
			return TRUE;
	}

	return TRUE;
}

gboolean
mono_exception_walk_trace_internal (MonoException *ex, MonoExceptionFrameWalk func, gpointer user_data)
{
	MONO_REQ_GC_UNSAFE_MODE;

	MonoDomain *domain = mono_domain_get ();
	MonoArray *ta = ex->trace_ips;

	/* Exception is not thrown yet */
	if (ta == NULL)
		return FALSE;

	int len = mono_array_length_internal (ta) / TRACE_IP_ENTRY_SIZE;
	gboolean otherwise_has_traces = len > 0;

	for (int i = 0; i < len; i++) {
		ExceptionTraceIp trace_ip;

		memcpy (&trace_ip, mono_array_addr_fast (ta, ExceptionTraceIp, i), sizeof (ExceptionTraceIp));
		gpointer ip = trace_ip.ip;
		gpointer generic_info = trace_ip.generic_info;

		MonoJitInfo *ji = NULL;
		if (trace_ip.ji) {
			ji = trace_ip.ji;
		} else {
			ji = mono_jit_info_table_find (domain, ip);
		}

		if (ji == NULL) {
			gboolean r;
			MONO_ENTER_GC_SAFE;
			r = func (NULL, ip, 0, FALSE, user_data);
			MONO_EXIT_GC_SAFE;
			if (r)
				break;
		} else {
			MonoMethod *method = get_method_from_stack_frame (ji, generic_info);
			if (func (method, ji->code_start, (char *) ip - (char *) ji->code_start, TRUE, user_data))
				break;
		}
	}

	ta = (MonoArray *) ex->captured_traces;
	len = ta ? mono_array_length_internal (ta) : 0;
	gboolean captured_has_traces = len > 0;

	for (int i = 0; i < len; i++) {
		MonoStackTrace *captured_trace = mono_array_get_fast (ta, MonoStackTrace *, i);
		if (!captured_trace)
			break;

		mono_exception_stacktrace_obj_walk (captured_trace, func, user_data);
	}

	return captured_has_traces || otherwise_has_traces;
}

MonoArray *
ves_icall_get_trace (MonoException *exc, gint32 skip, MonoBoolean need_file_info)
{
	ERROR_DECL (error);
	MonoDomain *domain = mono_domain_get ();
	MonoArray *res;
	MonoArray *ta = exc->trace_ips;
	MonoDebugSourceLocation *location;
	int i, len;

	if (ta == NULL) {
		/* Exception is not thrown yet */
		res = mono_array_new_checked (domain, mono_defaults.stack_frame_class, 0, error);
		mono_error_set_pending_exception (error);
		return res;
	}

	HANDLE_FUNCTION_ENTER ();

	MONO_HANDLE_PIN (ta);

	len = mono_array_length_internal (ta) / TRACE_IP_ENTRY_SIZE;

	res = mono_array_new_checked (domain, mono_defaults.stack_frame_class, len > skip ? len - skip : 0, error);
	if (!is_ok (error))
		goto fail;

	MONO_HANDLE_PIN (res);

	MonoObjectHandle sf_h;
	sf_h = MONO_HANDLE_NEW (MonoObject, NULL);

	for (i = skip; i < len; i++) {
		MonoJitInfo *ji;
		MonoStackFrame *sf = (MonoStackFrame *)mono_object_new_checked (domain, mono_defaults.stack_frame_class, error);
		if (!is_ok (error))
			goto fail;
		MONO_HANDLE_ASSIGN_RAW (sf_h, sf);

		ExceptionTraceIp trace_ip;
		memcpy (&trace_ip, mono_array_addr_fast (ta, ExceptionTraceIp, i), sizeof (ExceptionTraceIp));
		gpointer ip = trace_ip.ip;
		gpointer generic_info = trace_ip.generic_info;
		MonoMethod *method;

		if (trace_ip.ji) {
			ji = trace_ip.ji;
		} else {
			ji = mono_jit_info_table_find (domain, ip);
			if (ji == NULL) {
				/* Unmanaged frame */
				mono_array_setref_internal (res, i, sf);
				continue;
			}
		}

		g_assert (ji != NULL);

		if (mono_llvm_only || !generic_info)
			/* Can't resolve actual method */
			method = jinfo_get_method (ji);
		else
			method = get_method_from_stack_frame (ji, generic_info);
		if (jinfo_get_method (ji)->wrapper_type) {
			char *s;

			sf->method = NULL;
			s = mono_method_get_name_full (method, TRUE, FALSE, MONO_TYPE_NAME_FORMAT_REFLECTION);
			MonoString *name = mono_string_new_checked (domain, s, error);
			g_free (s);
			if (!is_ok (error))
				goto fail;
			MONO_OBJECT_SETREF_INTERNAL (sf, internal_method_name, name);
		}
		else {
			MonoReflectionMethod *rm = mono_method_get_object_checked (domain, method, NULL, error);
			if (!is_ok (error))
				goto fail;
			MONO_OBJECT_SETREF_INTERNAL (sf, method, rm);
		}

		sf->method_index = ji->from_aot ? mono_aot_find_method_index (method) : 0xffffff;
		sf->method_address = (gsize) ji->code_start;
		sf->native_offset = (char *)ip - (char *)ji->code_start;

		/*
		 * mono_debug_lookup_source_location() returns both the file / line number information
		 * and the IL offset.  Note that computing the IL offset is already an expensive
		 * operation, so we shouldn't call this method twice.
		 */
		location = mono_debug_lookup_source_location (jinfo_get_method (ji), sf->native_offset, domain);
		if (location) {
			sf->il_offset = location->il_offset;
		} else {
			SeqPoint sp;
			if (mono_find_prev_seq_point_for_native_offset (domain, jinfo_get_method (ji), sf->native_offset, NULL, &sp))
				sf->il_offset = sp.il_offset;
			else
				sf->il_offset = -1;
		}

		if (need_file_info) {
			if (location && location->source_file) {
				MonoString *filename = mono_string_new_checked (domain, location->source_file, error);
				if (!is_ok (error))
					goto fail;
				MONO_OBJECT_SETREF_INTERNAL (sf, filename, filename);
				sf->line = location->row;
				sf->column = location->column;
			} else {
				sf->line = sf->column = 0;
				sf->filename = NULL;
			}
		}

		mono_debug_free_source_location (location);
		mono_array_setref_internal (res, i - skip, sf);
	}
	goto exit;

 fail:
	mono_error_set_pending_exception (error);
	res = NULL;
 exit:
	HANDLE_FUNCTION_RETURN_VAL (res);
}

static void
mono_runtime_walk_stack_with_ctx (MonoJitStackWalk func, MonoContext *start_ctx, MonoUnwindOptions unwind_options, void *user_data)
{
	if (!start_ctx) {
		MonoJitTlsData *jit_tls = mono_tls_get_jit_tls ();
		if (jit_tls && jit_tls->orig_ex_ctx_set)
			start_ctx = &jit_tls->orig_ex_ctx;
	}
	mono_walk_stack_with_ctx (func, start_ctx, unwind_options, user_data);
}
/**
 * mono_walk_stack_with_ctx:
 * Unwind the current thread starting at \p start_ctx.
 * If \p start_ctx is null, we capture the current context.
 */
void
mono_walk_stack_with_ctx (MonoJitStackWalk func, MonoContext *start_ctx, MonoUnwindOptions unwind_options, void *user_data)
{
	MonoContext extra_ctx;
	MonoThreadInfo *thread = mono_thread_info_current_unchecked ();
	MONO_ARCH_CONTEXT_DEF

	if (!thread || !thread->jit_data)
		return;

	if (!start_ctx) {
		mono_arch_flush_register_windows ();
		MONO_INIT_CONTEXT_FROM_FUNC (&extra_ctx, mono_walk_stack_with_ctx);
		start_ctx = &extra_ctx;
	}

	mono_walk_stack_full (func, start_ctx, mono_domain_get (), thread->jit_data, mono_get_lmf (), unwind_options, user_data, FALSE);
}

/**
 * mono_walk_stack_with_state:
 * Unwind a thread described by \p state.
 *
 * State must be valid (state->valid == TRUE).
 *
 * If you are using this function to unwind another thread, make sure it is suspended.
 * 
 * If \p state is null, we capture the current context.
 */
void
mono_walk_stack_with_state (MonoJitStackWalk func, MonoThreadUnwindState *state, MonoUnwindOptions unwind_options, void *user_data)
{
	MonoThreadUnwindState extra_state;
	if (!state) {
		g_assert (!mono_thread_info_is_async_context ());
		if (!mono_thread_state_init_from_current (&extra_state))
			return;
		state = &extra_state;
	}

	g_assert (state->valid);

	if (!state->unwind_data [MONO_UNWIND_DATA_DOMAIN])
		/* Not attached */
		return;

	mono_walk_stack_full (func,
		&state->ctx, 
		(MonoDomain *)state->unwind_data [MONO_UNWIND_DATA_DOMAIN],
		(MonoJitTlsData *)state->unwind_data [MONO_UNWIND_DATA_JIT_TLS],
		(MonoLMF *)state->unwind_data [MONO_UNWIND_DATA_LMF],
		unwind_options, user_data, FALSE);
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
 * \param func callback to call for each stack frame
 * \param domain starting appdomain, can be NULL to use the current domain
 * \param unwind_options what extra information the unwinder should gather
 * \param start_ctx starting state of the stack walk, can be NULL.
 * \param thread the thread whose stack to walk, can be NULL to use the current thread
 * \param lmf the LMF of \p thread, can be NULL to use the LMF of the current thread
 * \param user_data data passed to the callback
 * \param crash_context tells us that we're in a context where it's not safe to lock or allocate
 * This function walks the stack of a thread, starting from the state
 * represented by \p start_ctx. For each frame the callback
 * function is called with the relevant info. The walk ends when no more
 * managed stack frames are found or when the callback returns a TRUE value.
 */
static void
mono_walk_stack_full (MonoJitStackWalk func, MonoContext *start_ctx, MonoDomain *domain, MonoJitTlsData *jit_tls, MonoLMF *lmf, MonoUnwindOptions unwind_options, gpointer user_data, gboolean crash_context)
{
	gint il_offset;
	MonoContext ctx, new_ctx;
	StackFrameInfo frame;
	gboolean res;
	host_mgreg_t *reg_locations [MONO_MAX_IREGS];
	host_mgreg_t *new_reg_locations [MONO_MAX_IREGS];
	gboolean get_reg_locations = unwind_options & MONO_UNWIND_REG_LOCATIONS;
	gboolean async = mono_thread_info_is_async_context ();
	Unwinder unwinder;

	memset (&frame, 0, sizeof (StackFrameInfo));

#ifndef TARGET_WASM
	if (mono_llvm_only) {
		GSList *l, *ips;

		if (async)
			return;

		ips = get_unwind_backtrace ();
		for (l = ips; l; l = l->next) {
			guint8 *ip = (guint8*)l->data;
			memset (&frame, 0, sizeof (StackFrameInfo));
			frame.ji = mini_jit_info_table_find (domain, ip, &frame.domain);
			if (!frame.ji || frame.ji->is_trampoline)
				continue;
			frame.type = FRAME_TYPE_MANAGED;
			frame.method = jinfo_get_method (frame.ji);
			// FIXME: Cannot lookup the actual method
			frame.actual_method = frame.method;
			if (frame.type == FRAME_TYPE_MANAGED) {
				if (!frame.method->wrapper_type || frame.method->wrapper_type == MONO_WRAPPER_DYNAMIC_METHOD)
					frame.managed = TRUE;
			}
			frame.native_offset = ip - (guint8*)frame.ji->code_start;
			frame.il_offset = -1;

			if (func (&frame, NULL, user_data))
				break;
		}
		g_slist_free (ips);
		return;
	}
#endif

	if (!start_ctx) {
		g_warning ("start_ctx required for stack walk");
		return;
	}

	if (!domain) {
		g_warning ("domain required for stack walk");
		return;
	}

	if (!jit_tls) {
		g_warning ("jit_tls required for stack walk");
		return;
	}

	/*The LMF will be null if the target have no managed frames.*/
 	/* g_assert (lmf); */
	if (async && (unwind_options & MONO_UNWIND_LOOKUP_ACTUAL_METHOD)) {
		g_warning ("async && (unwind_options & MONO_UNWIND_LOOKUP_ACTUAL_METHOD) not legal");
		return;
	}

	memcpy (&ctx, start_ctx, sizeof (MonoContext));
	memset (reg_locations, 0, sizeof (reg_locations));

	unwinder_init (&unwinder);

	while (MONO_CONTEXT_GET_SP (&ctx) < jit_tls->end_of_stack) {
		frame.lmf = lmf;
		res = unwinder_unwind_frame (&unwinder, domain, jit_tls, NULL, &ctx, &new_ctx, NULL, &lmf, get_reg_locations ? new_reg_locations : NULL, &frame);
		if (!res)
			return;

		if (frame.type == FRAME_TYPE_TRAMPOLINE)
			goto next;

		if ((unwind_options & MONO_UNWIND_LOOKUP_IL_OFFSET) && frame.ji) {
			MonoDebugSourceLocation *source = NULL;

			// Don't do this when we can be in a signal handler
			if (!crash_context)
				source = mono_debug_lookup_source_location (jinfo_get_method (frame.ji), frame.native_offset, domain);
			if (source) {
				il_offset = source->il_offset;
			} else {
				MonoSeqPointInfo *seq_points = NULL;

				// It's more reliable to look into the global cache if possible
				if (crash_context)
					seq_points = (MonoSeqPointInfo *) frame.ji->seq_points;
				else
					seq_points = mono_get_seq_points (domain, jinfo_get_method (frame.ji));

				SeqPoint sp;
				if (seq_points && mono_seq_point_find_prev_by_native_offset (seq_points, frame.native_offset, &sp))
					il_offset = sp.il_offset;
				else
					il_offset = -1;
			}
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

next:
		if (get_reg_locations) {
			for (int i = 0; i < MONO_MAX_IREGS; ++i)
				if (new_reg_locations [i])
					reg_locations [i] = new_reg_locations [i];
		}
		
		ctx = new_ctx;
	}
}

#ifdef DISABLE_CRASH_REPORTING

static void 
mono_summarize_managed_stack (MonoThreadSummary *out)
{
	return;
}

static void 
mono_summarize_unmanaged_stack (MonoThreadSummary *out)
{
	return;
}

static void
mono_summarize_exception (MonoException *exc, MonoThreadSummary *out)
{
	return;
}

static void
mono_crash_reporting_register_native_library (const char *module_path, const char *module_name)
{
	return;
}

static void
mono_crash_reporting_allow_all_native_libraries ()
{
	return;
}


#else

typedef struct {
	MonoFrameSummary *frames;
	int num_frames;
	int max_frames;
	MonoStackHash *hashes;
	const char *error;
} MonoSummarizeUserData;

static void
copy_summary_string_safe (char *dest, const char *src)
{
	g_strlcpy (dest, src, MONO_MAX_SUMMARY_NAME_LEN);
}

static void
fill_frame_managed_info (MonoFrameSummary *frame, MonoMethod * method)
{
		MonoImage *image = mono_class_get_image (method->klass);
		// Used for hashing, more stable across rebuilds than using GUID
		copy_summary_string_safe (frame->str_descr, image->assembly_name);

		frame->managed_data.guid = image->guid;
		frame->managed_data.token = method->token;
		frame->managed_data.filename = image->module_name;

		MonoDotNetHeader *header = &image->image_info->cli_header;
		frame->managed_data.image_size = header->nt.pe_image_size;
		frame->managed_data.time_date_stamp = image->time_date_stamp;
}

typedef struct {
	char *suffix;
	char *exported_name;
} MonoLibAllowlistEntry;

static GList *native_library_allowlist;
static gboolean allow_all_native_libraries = FALSE;

static void
mono_crash_reporting_register_native_library (const char *module_path, const char *module_name)
{
	// Examples: libsystem_pthread.dylib -> "pthread"
	// Examples: libsystem_platform.dylib -> "platform"
	// Examples: mono-sgen -> "mono" from above line
	MonoLibAllowlistEntry*entry = g_new0 (MonoLibAllowlistEntry, 1);
	entry->suffix = g_strdup (module_path);
	entry->exported_name = g_strdup (module_name);
	native_library_allowlist = g_list_append (native_library_allowlist, entry);
}

static void
mono_crash_reporting_allow_all_native_libraries ()
{
	allow_all_native_libraries = TRUE;
}

static gboolean
check_allowlisted_module (const char *in_name, const char **out_module)
{
#ifndef MONO_PRIVATE_CRASHES
		return TRUE;
#else
	if (g_str_has_suffix (in_name, "mono-sgen")) {
		if (out_module)
			copy_summary_string_safe ((char *) *out_module, "mono");
		return TRUE;
	}
	if (allow_all_native_libraries) {
		if (out_module) {
			/* for a module name, use the basename of the full path in in_name */
			char *basename = (char *) in_name, *p = (char *) in_name;
			while (*p != '\0') {
				if (*p == '/')
					basename = p + 1;
				p++;
			}
			if (*basename)
				copy_summary_string_safe ((char *) *out_module, basename);
			else
				copy_summary_string_safe ((char *) *out_module, "unknown");

		}
		return TRUE;
	}

	for (GList *cursor = native_library_allowlist; cursor; cursor = cursor->next) {
		MonoLibAllowlistEntry*iter = (MonoLibAllowlistEntry*) cursor->data;
		if (!g_str_has_suffix (in_name, iter->suffix))
			continue;
		if (out_module)
			copy_summary_string_safe ((char *) *out_module, iter->exported_name);
		return TRUE;
	}

	return FALSE;
#endif
}

static intptr_t
mono_make_portable_ip (intptr_t in_ip, intptr_t module_base)
{
	// FIXME: Make generalize away from llvm tools?
	// So lldb starts the pointer base at 0x100000000
	// and expects to get pointers as (offset + constant)
	//
	// Quirk shared by:
	// /usr/bin/symbols  -- symbols version:			@(#)PROGRAM:symbols  PROJECT:SamplingTools-63501
	// *CoreSymbolicationDT.framework version:	63750*/
	intptr_t offset = in_ip - module_base;
	intptr_t magic_value = offset + 0x100000000;
	return magic_value;
}

static gboolean
mono_get_portable_ip (intptr_t in_ip, intptr_t *out_ip, gint32 *out_offset, const char **out_module, char *out_name)
{
	// Note: it's not safe for us to be interrupted while inside of dl_addr, because if we
	// try to call dl_addr while interrupted while inside the lock, we will try to take a
	// non-recursive lock twice on this thread, and will deadlock.
	char sname [256], fname [256];
	void *saddr = NULL, *fbase = NULL;
	gboolean success = g_module_address ((void*)in_ip, fname, 256, &fbase, sname, 256, &saddr);
	if (!success)
		return FALSE;

	if (!check_allowlisted_module (fname, out_module))
		return FALSE;

	*out_ip = mono_make_portable_ip ((intptr_t) saddr, (intptr_t) fbase);
	*out_offset = in_ip - (intptr_t) saddr;

	if (saddr && out_name)
		copy_summary_string_safe (out_name, sname);
	return TRUE;
}

static guint64
summarize_offset_free_hash (guint64 accum, MonoFrameSummary *frame)
{
	if (!frame->is_managed)
		return accum;

	// See: mono_ptrarray_hash
	guint64 hash_accum = accum;

	// The assembly and the method token, no offsets
	hash_accum += mono_metadata_str_hash (frame->str_descr);
	hash_accum += frame->managed_data.token;

	return hash_accum;
}

static guint64
summarize_offset_rich_hash (guint64 accum, MonoFrameSummary *frame)
{
	// See: mono_ptrarray_hash
	guint64 hash_accum = accum;

	if (!frame->is_managed) {
		hash_accum += frame->unmanaged_data.ip;
	} else {
		hash_accum += mono_metadata_str_hash (frame->str_descr);
		hash_accum += frame->managed_data.token;
		hash_accum += frame->managed_data.il_offset;
	}

	return hash_accum;
}

static gboolean
summarize_frame_internal (MonoMethod *method, gpointer ip, size_t native_offset, int il_offset, gboolean managed, gpointer user_data)
{
	MonoSummarizeUserData *ud = (MonoSummarizeUserData *) user_data;

	gboolean valid_state = ud->num_frames + 1 < ud->max_frames;
	if (!valid_state) {
		ud->error = "Exceeded the maximum number of frames";
		return TRUE;
	}

	MonoFrameSummary *dest = &ud->frames [ud->num_frames];

	dest->unmanaged_data.ip = (intptr_t) ip;
	dest->is_managed = managed;
	dest->unmanaged_data.module [0] = '\0';

	if (!managed && method && method->wrapper_type != MONO_WRAPPER_NONE && method->wrapper_type < MONO_WRAPPER_NUM) {
		dest->is_managed = FALSE;
		dest->unmanaged_data.has_name = TRUE;
		copy_summary_string_safe (dest->str_descr, mono_wrapper_type_to_str (method->wrapper_type));
	}
	
#ifndef MONO_PRIVATE_CRASHES
	if (method)
		dest->managed_data.name = (char *) method->name;
#endif

	if (managed) {
		if (!method) {
			ud->error = "Managed method frame, but no provided managed method";
			return TRUE;
		}
		fill_frame_managed_info (dest, method);
		dest->managed_data.native_offset = native_offset;
		dest->managed_data.il_offset = il_offset;
	} else {
		dest->managed_data.token = -1;
	}


	ud->hashes->offset_free_hash = summarize_offset_free_hash (ud->hashes->offset_free_hash, dest);
	ud->hashes->offset_rich_hash = summarize_offset_rich_hash (ud->hashes->offset_rich_hash, dest);

	// We return FALSE, so we're continuing walking
	// And we increment the pointer because we're done with this cell in the array
	ud->num_frames++;
	return FALSE;
}

static gboolean
summarize_frame_managed_walk (MonoMethod *method, gpointer ip, size_t frame_native_offset, gboolean managed, gpointer user_data)
{
	int il_offset = -1;

	if (managed && method) {
		MonoDebugSourceLocation *location = mono_debug_lookup_source_location (method, frame_native_offset, mono_domain_get ());
		if (location) {
			il_offset = location->il_offset;
			mono_debug_free_source_location (location);
		}
	}

	intptr_t portable_ip = 0;
	gint32 offset = 0;
	mono_get_portable_ip ((intptr_t) ip, &portable_ip, &offset, NULL, NULL);

	return summarize_frame_internal (method, (gpointer) portable_ip, frame_native_offset, il_offset, managed, user_data);
}


static gboolean
summarize_frame (StackFrameInfo *frame, MonoContext *ctx, gpointer data)
{
	// Don't record trampolines between managed frames
	if (frame->ji && frame->ji->is_trampoline)
		return TRUE;

	if (frame->ji && (frame->ji->is_trampoline || frame->ji->async))
		return FALSE; // Keep unwinding

	intptr_t ip = 0;
	gint32 offset = 0;
	mono_get_portable_ip ((intptr_t) MONO_CONTEXT_GET_IP (ctx), &ip, &offset, NULL, NULL);
	// Don't need to handle return status "success" because this ip is stored below only, NULL is okay

	gboolean is_managed = (frame->type == FRAME_TYPE_MANAGED || frame->type == FRAME_TYPE_INTERP);
	MonoMethod *method = NULL;
	if (frame && frame->ji && frame->type != FRAME_TYPE_TRAMPOLINE)
		method = jinfo_get_method (frame->ji);

	if (is_managed)
		method = jinfo_get_method (frame->ji);

	return summarize_frame_internal (method, (gpointer) ip, offset, frame->il_offset, is_managed, data);
}

static void
mono_summarize_exception (MonoException *exc, MonoThreadSummary *out)
{
	memset (out, 0, sizeof (MonoThreadSummary));

	MonoException *inner_exc = exc;
	int exc_index = 0;

	for (exc_index = 0; exc_index < MONO_MAX_SUMMARY_EXCEPTIONS; exc_index++) {
		if (inner_exc == NULL)
			break;

		// Set up state to walk this MonoException's stack
		MonoSummarizeUserData data;
		memset (&data, 0, sizeof (MonoSummarizeUserData));
		data.max_frames = MONO_MAX_SUMMARY_FRAMES;
		data.num_frames = 0;
		data.frames = out->exceptions [exc_index].managed_frames;

		// Accumulate all hashes from all exceptions in traveral order
		data.hashes = &out->hashes;

		mono_exception_walk_trace (inner_exc, summarize_frame_managed_walk, &data);

		// Save per-MonoException info
		out->exceptions [exc_index].managed_exc_type = inner_exc->object.vtable->klass;
		out->exceptions [exc_index].num_managed_frames = data.num_frames;

		// Continue to traverse nesting of exceptions
		inner_exc = (MonoException *) inner_exc->inner_ex;
	}

	out->num_exceptions = exc_index;
}


static void 
mono_summarize_managed_stack (MonoThreadSummary *out)
{
	MonoSummarizeUserData data;
	memset (&data, 0, sizeof (MonoSummarizeUserData));
	data.max_frames = MONO_MAX_SUMMARY_FRAMES;
	data.num_frames = 0;
	data.frames = out->managed_frames;
	data.hashes = &out->hashes;

	// FIXME: collect stack pointer for both and sort frames by SP
	// so people can see relative ordering of both managed and unmanaged frames.

	// 
	// Summarize managed stack
	// 
	mono_walk_stack_full (summarize_frame, out->ctx, out->domain, out->jit_tls, out->lmf, MONO_UNWIND_LOOKUP_IL_OFFSET, &data, TRUE);
	out->num_managed_frames = data.num_frames;

	if (data.error != NULL)
		out->error_msg = data.error;
	out->is_managed = (out->num_managed_frames != 0);
}

// Always runs on the dumped thread
static void 
mono_summarize_unmanaged_stack (MonoThreadSummary *out)
{
	MONO_ARCH_CONTEXT_DEF
	// 
	// Summarize unmanaged stack
	// 
#ifdef HAVE_BACKTRACE_SYMBOLS
	intptr_t frame_ips [MONO_MAX_SUMMARY_FRAMES];

	out->num_unmanaged_frames = backtrace ((void **)frame_ips, MONO_MAX_SUMMARY_FRAMES);

	for (int i =0; i < out->num_unmanaged_frames; ++i) {
		intptr_t ip = frame_ips [i];
		MonoFrameSummary *frame = &out->unmanaged_frames [i];
		const char* module_buf = frame->unmanaged_data.module;
		int success = mono_get_portable_ip (ip, &frame->unmanaged_data.ip, &frame->unmanaged_data.offset, &module_buf, (char *) frame->str_descr);

		/* attempt to look up any managed method at that ip */
		/* TODO: Trampolines - follow examples from mono_print_method_from_ip() */

		MonoJitInfo *ji;
		MonoDomain *domain = mono_domain_get ();
		MonoDomain *target_domain;
		ji = mini_jit_info_table_find_ext (domain, (char *)ip, TRUE, &target_domain);
		if (ji) {
			frame->is_managed = TRUE;
			if (!ji->async && !ji->is_trampoline) {
				MonoMethod *method = jinfo_get_method (ji);
				fill_frame_managed_info (frame, method);
#ifndef MONO_PRIVATE_CRASHES
				frame->managed_data.name = method->name;
#endif
			}
		}

		if (!success && !ji) {
			frame->unmanaged_data.ip = ip;
			continue;
		}

		if (out->unmanaged_frames [i].str_descr [0] != '\0')
			out->unmanaged_frames [i].unmanaged_data.has_name = TRUE;

		out->hashes.offset_free_hash = summarize_offset_free_hash (out->hashes.offset_free_hash, frame);
		out->hashes.offset_rich_hash = summarize_offset_rich_hash (out->hashes.offset_rich_hash, frame);
	}
#endif

	out->lmf = mono_get_lmf ();

	MonoThreadInfo *thread = mono_thread_info_current_unchecked ();
	out->info_addr = (intptr_t) thread;
	out->jit_tls = thread->jit_data;
	out->domain = mono_domain_get ();

	if (!out->ctx) {
		out->ctx = &out->ctx_mem;
		mono_arch_flush_register_windows ();
		MONO_INIT_CONTEXT_FROM_FUNC (out->ctx, mono_summarize_unmanaged_stack);
	}

	return;
}
#endif


MonoBoolean
ves_icall_get_frame_info (gint32 skip, MonoBoolean need_file_info, 
			  MonoReflectionMethod **method, 
			  gint32 *iloffset, gint32 *native_offset,
			  MonoString **file, gint32 *line, gint32 *column)
{
	ERROR_DECL (error);
	MonoDomain *domain = mono_domain_get ();
	MonoJitTlsData *jit_tls = mono_tls_get_jit_tls ();
	MonoLMF *lmf = mono_get_lmf ();
	MonoJitInfo *ji = NULL;
	MonoContext ctx, new_ctx;
	MonoDebugSourceLocation *location;
	MonoMethod *jmethod = NULL, *actual_method;
	StackFrameInfo frame;
	gboolean res;
	Unwinder unwinder;
	int il_offset = -1;

	MONO_ARCH_CONTEXT_DEF;

	g_assert (skip >= 0);

	if (mono_llvm_only) {
		GSList *l, *ips;
		MonoDomain *frame_domain;
		guint8 *frame_ip = NULL;

		/* FIXME: Generalize this code with an interface which returns an array of StackFrame structures */
		jmethod = NULL;
		ips = get_unwind_backtrace ();
		for (l = ips; l && skip >= 0; l = l->next) {
			guint8 *ip = (guint8*)l->data;

			frame_ip = ip;

			ji = mini_jit_info_table_find (mono_domain_get (), ip, &frame_domain);
			if (!ji || ji->is_trampoline)
				continue;

			/* The skip count passed by the caller depends on us not filtering out MANAGED_TO_NATIVE */
			jmethod = jinfo_get_method (ji);
			if (jmethod->wrapper_type != MONO_WRAPPER_NONE && jmethod->wrapper_type != MONO_WRAPPER_DYNAMIC_METHOD && jmethod->wrapper_type != MONO_WRAPPER_MANAGED_TO_NATIVE)
				continue;
			skip--;
		}
		g_slist_free (ips);
		if (!jmethod || !l)
			return FALSE;
		/* No way to resolve generic instances */
		actual_method = jmethod;
		*native_offset = frame_ip - (guint8*)ji->code_start;
	} else {
		mono_arch_flush_register_windows ();
		MONO_INIT_CONTEXT_FROM_FUNC (&ctx, ves_icall_get_frame_info);

		unwinder_init (&unwinder);

		new_ctx = ctx;
		do {
			ctx = new_ctx;
			res = unwinder_unwind_frame (&unwinder, domain, jit_tls, NULL, &ctx, &new_ctx, NULL, &lmf, NULL, &frame);
			if (!res)
				return FALSE;
			switch (frame.type) {
			case FRAME_TYPE_MANAGED_TO_NATIVE:
			case FRAME_TYPE_DEBUGGER_INVOKE:
			case FRAME_TYPE_TRAMPOLINE:
			case FRAME_TYPE_INTERP_TO_MANAGED:
			case FRAME_TYPE_INTERP_TO_MANAGED_WITH_CTX:
				continue;
			case FRAME_TYPE_INTERP:
			case FRAME_TYPE_MANAGED:
				ji = frame.ji;
				*native_offset = frame.native_offset;

				/* The skip count passed by the caller depends on us not filtering out MANAGED_TO_NATIVE */
				jmethod = jinfo_get_method (ji);
				if (jmethod->wrapper_type != MONO_WRAPPER_NONE && jmethod->wrapper_type != MONO_WRAPPER_DYNAMIC_METHOD && jmethod->wrapper_type != MONO_WRAPPER_MANAGED_TO_NATIVE)
					continue;
				skip--;
				break;
			default:
				g_assert_not_reached ();
			}
		} while (skip >= 0);

		if (frame.type == FRAME_TYPE_INTERP) {
			jmethod = frame.method;
			actual_method = frame.actual_method;
		} else {
			actual_method = get_method_from_stack_frame (ji, get_generic_info_from_stack_frame (ji, &ctx));
		}
	}

	MonoReflectionMethod *rm = mono_method_get_object_checked (domain, actual_method, NULL, error);
	if (!is_ok (error)) {
		mono_error_set_pending_exception (error);
		return FALSE;
	}
	mono_gc_wbarrier_generic_store_internal (method, (MonoObject*) rm);

	if (il_offset != -1) {
		location = mono_debug_lookup_source_location_by_il (jmethod, il_offset, domain);
	} else {
		location = mono_debug_lookup_source_location (jmethod, *native_offset, domain);
	}
	if (location)
		*iloffset = location->il_offset;
	else
		*iloffset = 0;

	if (need_file_info) {
		if (location) {
			MonoString *filename = mono_string_new_checked (domain, location->source_file, error);
			if (!is_ok (error)) {
				mono_error_set_pending_exception (error);
				return FALSE;
			}
			mono_gc_wbarrier_generic_store_internal (file, (MonoObject*)filename);
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

static MonoClass*
get_exception_catch_class (MonoJitExceptionInfo *ei, MonoJitInfo *ji, MonoContext *ctx)
{
	ERROR_DECL (error);
	MonoClass *catch_class = ei->data.catch_class;
	MonoType *inflated_type;
	MonoGenericContext context;

	/*MonoJitExceptionInfo::data is an union used by filter and finally clauses too.*/
	if (!catch_class || ei->flags != MONO_EXCEPTION_CLAUSE_NONE)
		return NULL;

	if (!ji->has_generic_jit_info || !mono_jit_info_get_generic_jit_info (ji)->has_this)
		return catch_class;
	context = mono_get_generic_context_from_stack_frame (ji, get_generic_info_from_stack_frame (ji, ctx));

	/* FIXME: we shouldn't inflate but instead put the
	   type in the rgctx and fetch it from there.  It
	   might be a good idea to do this lazily, i.e. only
	   when the exception is actually thrown, so as not to
	   waste space for exception clauses which might never
	   be encountered. */
	inflated_type = mono_class_inflate_generic_type_checked (m_class_get_byval_arg (catch_class), &context, error);
	mono_error_assert_ok (error); /* FIXME don't swallow the error */

	catch_class = mono_class_from_mono_type_internal (inflated_type);
	mono_metadata_free_type (inflated_type);

	return catch_class;
}

/*
 * mini_jit_info_table_find_ext:
 *
 *   Same as mono_jit_info_table_find, but search all the domains of the current thread
 * if ADDR is not found in DOMAIN. The domain where the method was found is stored into
 * OUT_DOMAIN if it is not NULL.
 */
MonoJitInfo*
mini_jit_info_table_find_ext (MonoDomain *domain, gpointer addr, gboolean allow_trampolines, MonoDomain **out_domain)
{
	MonoJitInfo *ji;
	MonoInternalThread *t = mono_thread_internal_current ();
	gpointer *refs;

	if (out_domain)
		*out_domain = NULL;

	ji = mono_jit_info_table_find_internal (domain, addr, TRUE, allow_trampolines);
	if (ji) {
		if (out_domain)
			*out_domain = domain;
		return ji;
	}

	/* maybe it is shared code, so we also search in the root domain */
	if (domain != mono_get_root_domain ()) {
		ji = mono_jit_info_table_find_internal (mono_get_root_domain (), addr, TRUE, allow_trampolines);
		if (ji) {
			if (out_domain)
				*out_domain = mono_get_root_domain ();
			return ji;
		}
	}

	if (!t)
		return NULL;

	refs = (gpointer *)((t->appdomain_refs) ? *(gpointer *) t->appdomain_refs : NULL);
	for (; refs && *refs; refs++) {
		if (*refs != domain && *refs != mono_get_root_domain ()) {
			ji = mono_jit_info_table_find_internal ((MonoDomain*) *refs, addr, TRUE, allow_trampolines);
			if (ji) {
				if (out_domain)
					*out_domain = (MonoDomain*) *refs;
				return ji;
			}
		}
	}

	return NULL;
}

MonoJitInfo*
mini_jit_info_table_find (MonoDomain *domain, gpointer addr, MonoDomain **out_domain)
{
	return mini_jit_info_table_find_ext (domain, addr, FALSE, out_domain);
}

/* Class lazy loading functions */
static GENERATE_GET_CLASS_WITH_CACHE (runtime_compat_attr, "System.Runtime.CompilerServices", "RuntimeCompatibilityAttribute")

/*
 * wrap_non_exception_throws:
 *
 *   Determine whenever M's assembly has a RuntimeCompatibilityAttribute with the
 * WrapNonExceptionThrows flag set.
 */
static gboolean
wrap_non_exception_throws (MonoMethod *m)
{
	ERROR_DECL (error);
	MonoAssembly *ass = m_class_get_image (m->klass)->assembly;
	MonoCustomAttrInfo* attrs;
	MonoClass *klass;
	int i;
	gboolean val = FALSE;

	if (m->wrapper_type == MONO_WRAPPER_DYNAMIC_METHOD) {
		MonoDynamicMethod *dm = (MonoDynamicMethod *)m;
		if (dm->assembly)
			ass = dm->assembly;
	}
	g_assert (ass);
	if (ass->wrap_non_exception_throws_inited)
		return ass->wrap_non_exception_throws;

	klass = mono_class_get_runtime_compat_attr_class ();

	attrs = mono_custom_attrs_from_assembly_checked (ass, FALSE, error);
	mono_error_cleanup (error); /* FIXME don't swallow the error */
	if (attrs) {
		for (i = 0; i < attrs->num_attrs; ++i) {
			MonoCustomAttrEntry *attr = &attrs->attrs [i];
			const gchar *p;
			int num_named, named_type, name_len;
			char *name;

			if (!attr->ctor || attr->ctor->klass != klass)
				continue;
			/* Decode the RuntimeCompatibilityAttribute. See reflection.c */
			p = (const char*)attr->data;
			g_assert (read16 (p) == 0x0001);
			p += 2;
			num_named = read16 (p);
			if (num_named != 1)
				continue;
			p += 2;
			named_type = *p;
			p ++;
			/* data_type = *p; */
			p ++;
			/* Property */
			if (named_type != 0x54)
				continue;
			name_len = mono_metadata_decode_blob_size (p, &p);
			name = (char *)g_malloc (name_len + 1);
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

#define MAX_UNMANAGED_BACKTRACE 128
static MonoArray*
build_native_trace (MonoError *error)
{
	error_init (error);
/* This puppy only makes sense on mobile, IOW, ARM. */
#if defined (HAVE_BACKTRACE_SYMBOLS) && defined (TARGET_ARM)
	MonoArray *res;
	void *native_trace [MAX_UNMANAGED_BACKTRACE];
	int size = -1;
	MONO_ENTER_GC_SAFE;
	size = backtrace (native_trace, MAX_UNMANAGED_BACKTRACE);
	MONO_EXIT_GC_SAFE;
	int i;

	if (!size)
		return NULL;
	res = mono_array_new_checked (mono_domain_get (), mono_defaults.int_class, size, error);
	return_val_if_nok (error, NULL);

	for (i = 0; i < size; i++)
		mono_array_set_internal (res, gpointer, i, native_trace [i]);
	return res;
#else
	return NULL;
#endif
}

static void
remove_wrappers_from_trace (GList **trace_ips_p)
{
	GList *trace_ips = *trace_ips_p;
	GList *p = trace_ips;

	/* jit info, generic info, ip */
	while (p) {
		MonoJitInfo *jinfo = (MonoJitInfo*) p->data;
		GList *next_p = p->next->next->next;
		/* FIXME Maybe remove more wrapper types */
		if (jinfo->d.method->wrapper_type == MONO_WRAPPER_OTHER) {
			trace_ips = g_list_delete_link (trace_ips, p->next->next);
			trace_ips = g_list_delete_link (trace_ips, p->next);
			trace_ips = g_list_delete_link (trace_ips, p);
		}
		p = next_p;
	}

	*trace_ips_p = trace_ips;
}

/* This can be called more than once on a MonoException. */
static void
setup_stack_trace (MonoException *mono_ex, GSList **dynamic_methods, GList *trace_ips, gboolean remove_wrappers)
{
	if (mono_ex) {
		GList *trace_ips_copy = g_list_copy (trace_ips);
		if (remove_wrappers)
			remove_wrappers_from_trace (&trace_ips_copy);
		trace_ips_copy = g_list_reverse (trace_ips_copy);
		ERROR_DECL (error);
		MonoArray *ips_arr = mono_glist_to_array (trace_ips_copy, mono_defaults.int_class, error);
		mono_error_assert_ok (error);
		MONO_OBJECT_SETREF_INTERNAL (mono_ex, trace_ips, ips_arr);
		MONO_OBJECT_SETREF_INTERNAL (mono_ex, native_trace_ips, build_native_trace (error));
		mono_error_assert_ok (error);
		if (*dynamic_methods) {
			/* These methods could go away anytime, so save a reference to them in the exception object */
			GSList *l;
			MonoMList *list = (MonoMList*)mono_ex->dynamic_methods;

			for (l = *dynamic_methods; l; l = l->next) {
				MonoGCHandle dis_link;
				MonoDomain *domain = mono_domain_get ();

				if (domain->method_to_dyn_method) {
					mono_domain_lock (domain);
					dis_link = (MonoGCHandle)g_hash_table_lookup (domain->method_to_dyn_method, l->data);
					mono_domain_unlock (domain);
					if (dis_link) {
						MonoObject *o = mono_gchandle_get_target_internal (dis_link);
						if (o) {
							list = mono_mlist_prepend_checked (list, o, error);
							mono_error_assert_ok (error);
						}
					}
				}
			}

			MONO_OBJECT_SETREF_INTERNAL (mono_ex, dynamic_methods, list);

			g_slist_free (*dynamic_methods);
			*dynamic_methods = NULL;
		}

		g_list_free (trace_ips_copy);
	}
}

typedef enum {
	MONO_FIRST_PASS_UNHANDLED,
	MONO_FIRST_PASS_CALLBACK_TO_NATIVE,
	MONO_FIRST_PASS_HANDLED,
} MonoFirstPassResult;

/*
 * handle_exception_first_pass:
 *
 *   The first pass of exception handling. Unwind the stack until a catch
 * clause which can catch OBJ is found. Store the index of the filter clause
 * which caught the exception into OUT_FILTER_IDX. Return
 * \c MONO_FIRST_PASS_HANDLED if the exception is caught,
 * \c MONO_FIRST_PASS_UNHANDLED otherwise, unless there is a native-to-managed
 * wrapper and an exception handling callback is installed (in which case
 * return \c MONO_FIRST_PASS_CALLBACK_TO_NATIVE).
 */
static MonoFirstPassResult
handle_exception_first_pass (MonoContext *ctx, MonoObject *obj, gint32 *out_filter_idx, MonoJitInfo **out_ji, MonoJitInfo **out_prev_ji, MonoObject *non_exception, StackFrameInfo *catch_frame, gboolean *last_mono_wrapper_runtime_invoke)
{
	ERROR_DECL (error);
	MonoDomain *domain = mono_domain_get ();
	MonoJitInfo *ji = NULL;
	static int (*call_filter) (MonoContext *, gpointer) = NULL;
	MonoJitTlsData *jit_tls = mono_tls_get_jit_tls ();
	MonoLMF *lmf = mono_get_lmf ();
	GList *trace_ips = NULL;
	GSList *dynamic_methods = NULL;
	MonoException *mono_ex;
	gboolean stack_overflow = FALSE;
	MonoContext initial_ctx;
	MonoMethod *method;
	int frame_count = 0;
	gint32 filter_idx;
	int i;
	MonoObject *ex_obj;
	Unwinder unwinder;
	gboolean in_interp;

	MonoFirstPassResult result = MONO_FIRST_PASS_UNHANDLED;

	g_assert (ctx != NULL);
	*last_mono_wrapper_runtime_invoke = TRUE;
	if (obj == (MonoObject *)domain->stack_overflow_ex)
		stack_overflow = TRUE;

	mono_ex = (MonoException*)obj;
	MonoArray *initial_trace_ips = mono_ex->trace_ips;
	if (initial_trace_ips) {
		int len = mono_array_length_internal (initial_trace_ips) / TRACE_IP_ENTRY_SIZE;

		// If we catch in managed/non-wrapper, we don't save the catching frame
		if (!mono_ex->caught_in_unmanaged)
			len -= 1;

		for (i = 0; i < len; i++) {
			for (int j = 0; j < TRACE_IP_ENTRY_SIZE; ++j) {
				gpointer p = mono_array_get_internal (initial_trace_ips, gpointer, (i * TRACE_IP_ENTRY_SIZE) + j);
				trace_ips = g_list_prepend (trace_ips, p);
			}
		}
	}

	// Reset the state because we're making it be caught somewhere
	if (mono_ex->caught_in_unmanaged)
		MONO_OBJECT_SETREF_INTERNAL (mono_ex, caught_in_unmanaged, 0);

	if (!mono_object_isinst_checked (obj, mono_defaults.exception_class, error)) {
		mono_error_assert_ok (error);
		mono_ex = NULL;
	}

	if (!call_filter)
		call_filter = (int (*) (MonoContext *, void *))mono_get_call_filter ();

	g_assert (jit_tls->end_of_stack);
	g_assert (jit_tls->abort_func);

	if (out_filter_idx)
		*out_filter_idx = -1;
	if (out_ji)
		*out_ji = NULL;
	if (out_prev_ji)
		*out_prev_ji = NULL;
	filter_idx = 0;
	initial_ctx = *ctx;

	unwinder_init (&unwinder);

	while (1) {
		MonoContext new_ctx;
		guint32 free_stack;
		int clause_index_start = 0;
		gboolean unwind_res = TRUE;
		
		StackFrameInfo frame;

		if (out_prev_ji)
			*out_prev_ji = ji;

		unwind_res = unwinder_unwind_frame (&unwinder, domain, jit_tls, NULL, ctx, &new_ctx, NULL, &lmf, NULL, &frame);
		if (!unwind_res) {
			setup_stack_trace (mono_ex, &dynamic_methods, trace_ips, FALSE);
			g_list_free (trace_ips);
			return result;
		}

		switch (frame.type) {
		case FRAME_TYPE_DEBUGGER_INVOKE:
		case FRAME_TYPE_MANAGED_TO_NATIVE:
		case FRAME_TYPE_TRAMPOLINE:
		case FRAME_TYPE_INTERP_TO_MANAGED:
		case FRAME_TYPE_INTERP_TO_MANAGED_WITH_CTX:
			*ctx = new_ctx;
			continue;
		case FRAME_TYPE_INTERP:
		case FRAME_TYPE_MANAGED:
			break;
		default:
			g_assert_not_reached ();
			break;
		}

		in_interp = frame.type == FRAME_TYPE_INTERP;
		ji = frame.ji;

		gpointer ip;
		if (in_interp)
			ip = (guint8*)ji->code_start + frame.native_offset;
		else
			ip = MONO_CONTEXT_GET_IP (ctx);

		frame_count ++;
		method = jinfo_get_method (ji);
		//printf ("M: %s %d.\n", mono_method_full_name (method, TRUE), frame_count);

		if (mini_debug_options.reverse_pinvoke_exceptions && method->wrapper_type == MONO_WRAPPER_NATIVE_TO_MANAGED) {
			g_error ("A native frame was found while unwinding the stack after an exception.\n"
					 "The native frame called the managed method:\n%s\n",
					 mono_method_full_name (method, TRUE));
		}

		if (method->wrapper_type != MONO_WRAPPER_RUNTIME_INVOKE && mono_ex) {
			// avoid giant stack traces during a stack overflow
			if (frame_count < 1000) {
				trace_ips = g_list_prepend (trace_ips, ip);
				trace_ips = g_list_prepend (trace_ips, get_generic_info_from_stack_frame (ji, ctx));
				trace_ips = g_list_prepend (trace_ips, ji);
			}
		}

		if (method->dynamic)
			dynamic_methods = g_slist_prepend (dynamic_methods, method);

		if (stack_overflow) {
			free_stack = (guint8*)(MONO_CONTEXT_GET_SP (ctx)) - (guint8*)(MONO_CONTEXT_GET_SP (&initial_ctx));
		} else {
			free_stack = 0xffffff;
		}

		if (method->wrapper_type == MONO_WRAPPER_NATIVE_TO_MANAGED && ftnptr_eh_callback) {
			result = MONO_FIRST_PASS_CALLBACK_TO_NATIVE;
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

			if (is_address_protected (ji, ei, ip)) {
				/* catch block */
				MonoClass *catch_class = get_exception_catch_class (ei, ji, ctx);

				/*
				 * Have to unwrap RuntimeWrappedExceptions if the
				 * method's assembly doesn't have a RuntimeCompatibilityAttribute.
				 */
				if (non_exception && !wrap_non_exception_throws (method))
					ex_obj = non_exception;
				else
					ex_obj = obj;

				if (ei->flags == MONO_EXCEPTION_CLAUSE_FILTER) {
					setup_stack_trace (mono_ex, &dynamic_methods, trace_ips, FALSE);

#ifndef DISABLE_PERFCOUNTERS
					mono_atomic_inc_i32 (&mono_perfcounters->exceptions_filters);
#endif

					if (!ji->is_interp) {
#ifndef MONO_CROSS_COMPILE
#ifdef MONO_CONTEXT_SET_LLVM_EXC_REG
						if (ji->from_llvm)
							MONO_CONTEXT_SET_LLVM_EXC_REG (ctx, ex_obj);
						else
							/* Can't pass the ex object in a register yet to filter clauses, because call_filter () might not support it */
							*((gpointer *)(gpointer)((char *)MONO_CONTEXT_GET_BP (ctx) + ei->exvar_offset)) = ex_obj;
#else
						g_assert (!ji->from_llvm);
						/* store the exception object in bp + ei->exvar_offset */
						*((gpointer *)(gpointer)((char *)MONO_CONTEXT_GET_BP (ctx) + ei->exvar_offset)) = ex_obj;
#endif
#endif

#ifdef MONO_CONTEXT_SET_LLVM_EH_SELECTOR_REG
						/*
						 * Pass the original il clause index to the landing pad so it can
						 * branch to the landing pad associated with the il clause.
						 * This is needed because llvm compiled code assumes that the EH
						 * code always branches to the innermost landing pad.
						 */
						if (ji->from_llvm)
							MONO_CONTEXT_SET_LLVM_EH_SELECTOR_REG (ctx, ei->clause_index);
#endif
					}

					mini_get_dbg_callbacks ()->begin_exception_filter (mono_ex, ctx, &initial_ctx);

					if (G_UNLIKELY (mono_profiler_clauses_enabled ())) {
						jit_tls->orig_ex_ctx_set = TRUE;
						MONO_PROFILER_RAISE (exception_clause, (method, i, (MonoExceptionEnum)ei->flags, ex_obj));
						jit_tls->orig_ex_ctx_set = FALSE;
					}

					if (ji->is_interp) {
						/* The filter ends where the exception handler starts */
						filtered = mini_get_interp_callbacks ()->run_filter (&frame, (MonoException*)ex_obj, i, ei->data.filter, ei->handler_start);
					} else {
						filtered = call_filter (ctx, ei->data.filter);
					}
					mini_get_dbg_callbacks ()->end_exception_filter (mono_ex, ctx, &initial_ctx);
					if (filtered && out_filter_idx)
						*out_filter_idx = filter_idx;
					if (out_ji)
						*out_ji = ji;
					filter_idx ++;

					if (filtered) {
						g_list_free (trace_ips);
						/* mono_debugger_agent_handle_exception () needs this */
						mini_set_abort_threshold (&frame);
						MONO_CONTEXT_SET_IP (ctx, ei->handler_start);
						frame.native_offset = (char*)ei->handler_start - (char*)ji->code_start;
						*catch_frame = frame;
						result = MONO_FIRST_PASS_HANDLED;
						return result;
					}
				}

				ERROR_DECL (isinst_error); // FIXME not used https://github.com/mono/mono/pull/3055/files#r240548187
				if (ei->flags == MONO_EXCEPTION_CLAUSE_NONE && mono_object_isinst_checked (ex_obj, catch_class, error)) {
					/* runtime invokes catch even unhandled exceptions */
					setup_stack_trace (mono_ex, &dynamic_methods, trace_ips, method->wrapper_type != MONO_WRAPPER_RUNTIME_INVOKE);
					g_list_free (trace_ips);

					if (out_ji)
						*out_ji = ji;

					/* mono_debugger_agent_handle_exception () needs this */
					if (!in_interp)
						MONO_CONTEXT_SET_IP (ctx, ei->handler_start);
					frame.native_offset = (char*)ei->handler_start - (char*)ji->code_start;
					*catch_frame = frame;
					result = MONO_FIRST_PASS_HANDLED;
					if (method->wrapper_type == MONO_WRAPPER_RUNTIME_INVOKE) {
						//try to find threadpool_perform_wait_callback_method
						unwind_res = unwinder_unwind_frame (&unwinder, domain, jit_tls, NULL, &new_ctx, &new_ctx, NULL, &lmf, NULL, &frame);
						while (unwind_res) {
							if (frame.ji && !frame.ji->is_trampoline && jinfo_get_method (frame.ji)->wrapper_type == MONO_WRAPPER_RUNTIME_INVOKE) {
								*last_mono_wrapper_runtime_invoke = FALSE;
								break;
							}
							unwind_res = unwinder_unwind_frame (&unwinder, domain, jit_tls, NULL, &new_ctx, &new_ctx, NULL, &lmf, NULL, &frame);
						}
					}
					return result;
				}
				mono_error_cleanup (isinst_error);
			}
		}

		*ctx = new_ctx;
	}

	g_assert_not_reached ();
}

/*
 * We implement delaying of aborts when in finally blocks by reusing the
 * abort protected block mechanism. The problem is that when throwing an
 * exception in a finally block we don't get to exit the protected block.
 * We exit it here when unwinding. Given that the order of the clauses
 * in the jit info is from inner clauses to the outer clauses, when we
 * want to exit the finally blocks inner to the clause that handles the
 * exception, we need to search up to its index.
 *
 * FIXME We should do this inside interp, but with mixed mode we can
 * resume directly, without giving control back to the interp.
 */
static void
interp_exit_finally_abort_blocks (MonoJitInfo *ji, int start_clause, int end_clause, gpointer ip)
{
	int i;
	for (i = start_clause; i < end_clause; i++) {
		MonoJitExceptionInfo *ei = &ji->clauses [i];
		if (ei->flags == MONO_EXCEPTION_CLAUSE_FINALLY &&
				ip >= ei->handler_start &&
				ip < ei->data.handler_end) {
			mono_threads_end_abort_protected_block ();
		}
	}
}

static MonoException *
mono_get_exception_runtime_wrapped_checked (MonoObject *wrapped_exception_raw, MonoError *error)
{
	HANDLE_FUNCTION_ENTER ();
	MONO_HANDLE_DCL (MonoObject, wrapped_exception);
	MonoExceptionHandle ret = mono_get_exception_runtime_wrapped_handle (wrapped_exception, error);
	HANDLE_FUNCTION_RETURN_OBJ (ret);
}

/**
 * mono_handle_exception_internal:
 * \param ctx saved processor state
 * \param obj the exception object
 * \param resume whenever to resume unwinding based on the state in \c MonoJitTlsData.
 */
static gboolean
mono_handle_exception_internal (MonoContext *ctx, MonoObject *obj, gboolean resume, MonoJitInfo **out_ji)
{
	ERROR_DECL (error);
	MonoDomain *domain = mono_domain_get ();
	MonoJitInfo *ji, *prev_ji;
	static int (*call_filter) (MonoContext *, gpointer) = NULL;
	MonoJitTlsData *jit_tls = mono_tls_get_jit_tls ();
	MonoLMF *lmf = mono_get_lmf ();
	MonoException *mono_ex;
	gboolean stack_overflow = FALSE;
	MonoContext initial_ctx;
	MonoMethod *method;
	int frame_count = 0;
	gint32 filter_idx, first_filter_idx = 0;
	int i;
	MonoObject *ex_obj = NULL;
	MonoObject *non_exception = NULL;
	Unwinder unwinder;
	gboolean in_interp;
	gboolean is_caught_unmanaged = FALSE;
	gboolean last_mono_wrapper_runtime_invoke = TRUE;

	g_assert (ctx != NULL);
	if (!obj) {
		MonoException *ex = mono_get_exception_null_reference ();
		MonoString *msg = mono_string_new_checked (domain, "Object reference not set to an instance of an object", error);
		mono_error_assert_ok (error);
		MONO_OBJECT_SETREF_INTERNAL (ex, message, msg);
		obj = (MonoObject *)ex;
	} 

	/*
	 * Allocate a new exception object instead of the preconstructed ones.
	 */
	if (obj == (MonoObject *)domain->stack_overflow_ex) {
		/*
		 * It is not a good idea to try and put even more pressure on the little stack available.
		 * obj = mono_get_exception_stack_overflow ();
		 */
		stack_overflow = TRUE;
	}
	else if (obj == (MonoObject *)domain->null_reference_ex) {
		obj = (MonoObject *)mono_get_exception_null_reference ();
	}

	if (!mono_object_isinst_checked (obj, mono_defaults.exception_class, error)) {
		mono_error_assert_ok (error);
		non_exception = obj;
		obj = (MonoObject *)mono_get_exception_runtime_wrapped_checked (obj, error);
		mono_error_assert_ok (error);
	}

	mono_ex = (MonoException*)obj;

	if (mini_debug_options.suspend_on_exception) {
		mono_runtime_printf_err ("Exception thrown, suspending...");
		while (1)
			;
	}

	if (mono_ex->caught_in_unmanaged)
		is_caught_unmanaged = TRUE;
	

	if (mono_object_isinst_checked (obj, mono_defaults.exception_class, error)) {
		mono_ex = (MonoException*)obj;
	} else {
		mono_error_assert_ok (error);
		mono_ex = NULL;
	}

	if (mono_ex && jit_tls->class_cast_from) {
		if (!strcmp (m_class_get_name (mono_ex->object.vtable->klass), "InvalidCastException")) {
			char *from_name = mono_type_get_full_name (jit_tls->class_cast_from);
			char *to_name = mono_type_get_full_name (jit_tls->class_cast_to);
			char *msg = g_strdup_printf ("Unable to cast object of type '%s' to type '%s'.", from_name, to_name);
			mono_ex->message = mono_string_new_checked (domain, msg, error);
			g_free (from_name);
			g_free (to_name);
			if (!is_ok (error)) {
				mono_runtime_printf_err ("Error creating class cast exception message '%s'\n", msg);
				mono_error_assert_ok (error);
			}
			g_free (msg);
		}
		if (!strcmp (m_class_get_name (mono_ex->object.vtable->klass), "ArrayTypeMismatchException")) {
			char *from_name = mono_type_get_full_name (jit_tls->class_cast_from);
			char *to_name = mono_type_get_full_name (jit_tls->class_cast_to);
			char *msg = g_strdup_printf ("Source array of type '%s' cannot be cast to destination array type '%s'.", from_name, to_name);
			mono_ex->message = mono_string_new_checked (domain, msg, error);
			g_free (from_name);
			g_free (to_name);
			if (!is_ok (error)) {
				mono_runtime_printf_err ("Error creating array type mismatch exception message '%s'\n", msg);
				mono_error_assert_ok (error);
			}
			g_free (msg);
		}
	}

	if (!call_filter)
		call_filter = (int (*)(MonoContext *, void*))mono_get_call_filter ();

	g_assert (jit_tls->end_of_stack);
	g_assert (jit_tls->abort_func);

	/*
	 * We set orig_ex_ctx_set to TRUE/FALSE around profiler calls to make sure it doesn't
	 * end up being TRUE on any code path.
	 */
	memcpy (&jit_tls->orig_ex_ctx, ctx, sizeof (MonoContext));

	if (!resume) {
		MonoContext ctx_cp = *ctx;
		if (mono_trace_is_enabled ()) {
			ERROR_DECL (error);
			MonoMethod *system_exception_get_message = mono_class_get_method_from_name_checked (mono_defaults.exception_class, "get_Message", 0, 0, error);
			mono_error_cleanup (error);
			error_init (error);
			MonoMethod *get_message = system_exception_get_message == NULL ? NULL : mono_object_get_virtual_method_internal (obj, system_exception_get_message);
			MonoObject *message;
			const char *type_name = m_class_get_name (mono_object_class (mono_ex));
			char *msg = NULL;
			if (get_message == NULL) {
				message = NULL;
			} else if (!strcmp (type_name, "OutOfMemoryException") || !strcmp (type_name, "StackOverflowException")) {
				message = NULL;
				msg = g_strdup_printf ("(No exception message for: %s)\n", type_name);
			} else {
				MonoObject *exc = NULL;
				message = mono_runtime_try_invoke (get_message, obj, NULL, &exc, error);
				g_assert (exc == NULL);
				mono_error_assert_ok (error);
			}
			if (msg == NULL) {
				if (message) {
					msg = mono_string_to_utf8_checked_internal ((MonoString *) message, error);
					if (!is_ok (error)) {
						mono_error_cleanup (error);
						msg = g_strdup ("(error while display System.Exception.Message property)");
					}
				} else {
					msg = g_strdup ("(System.Exception.Message property not available)");
				}
			}
			g_print ("[%p:] EXCEPTION handling: %s.%s: %s\n", (void*)(gsize)mono_native_thread_id_get (), m_class_get_name_space (mono_object_class (obj)), m_class_get_name (mono_object_class (obj)), msg);
			g_free (msg);
			if (mono_ex && mono_trace_eval_exception (mono_object_class (mono_ex)))
				mono_print_thread_dump_from_ctx (ctx);
		}
		jit_tls->orig_ex_ctx_set = TRUE;
		MONO_PROFILER_RAISE (exception_throw, (obj));
		jit_tls->orig_ex_ctx_set = FALSE;

#ifdef ENABLE_NETCORE
		mono_first_chance_exception_internal (obj);
#endif

		StackFrameInfo catch_frame;
		MonoFirstPassResult res;
		res = handle_exception_first_pass (&ctx_cp, obj, &first_filter_idx, &ji, &prev_ji, non_exception, &catch_frame, &last_mono_wrapper_runtime_invoke);

		if (res == MONO_FIRST_PASS_UNHANDLED) {
			if (mono_aot_mode == MONO_AOT_MODE_LLVMONLY_INTERP) {
				/* Reached the top interpreted frames, but there might be native frames above us */
				throw_exception (obj, TRUE);
				g_assert_not_reached ();
			}
			if (mini_debug_options.break_on_exc)
				G_BREAKPOINT ();
			mini_get_dbg_callbacks ()->handle_exception ((MonoException *)obj, ctx, NULL, NULL);

			// FIXME: This runs managed code so it might cause another stack overflow when
			// we are handling a stack overflow
			mini_set_abort_threshold (&catch_frame);
			mono_unhandled_exception_internal (obj);
		} else {
			gboolean unhandled = FALSE;

			/*
			 * The exceptions caught by the mono_runtime_invoke_checked () calls
			 * in the threadpool needs to be treated as unhandled (#669836).
			 *
			 * FIXME: The check below is hackish, but its hard to distinguish
			 * these runtime invoke calls from others in the runtime.
			 */
#ifndef ENABLE_NETCORE
			if (ji && jinfo_get_method (ji)->wrapper_type == MONO_WRAPPER_RUNTIME_INVOKE) {
				if (prev_ji && jinfo_get_method (prev_ji) == mono_defaults.threadpool_perform_wait_callback_method)
					unhandled = TRUE;
			}
#endif

			if (unhandled)
				mini_get_dbg_callbacks ()->handle_exception ((MonoException *)obj, ctx, NULL, NULL);
			else if (!ji || (jinfo_get_method (ji)->wrapper_type == MONO_WRAPPER_RUNTIME_INVOKE)) {
				if (last_mono_wrapper_runtime_invoke && !mono_thread_internal_current ()->threadpool_thread) {
					mini_get_dbg_callbacks ()->handle_exception ((MonoException *)obj, ctx, NULL, NULL);
					if (mini_get_debug_options ()->top_runtime_invoke_unhandled) {
						mini_set_abort_threshold (&catch_frame);
						mono_unhandled_exception_internal (obj);
					}
				} else {
					mini_get_dbg_callbacks ()->handle_exception ((MonoException *)obj, ctx, &ctx_cp, &catch_frame);
				}
			}
			else if (res != MONO_FIRST_PASS_CALLBACK_TO_NATIVE)
				if (!is_caught_unmanaged)
					mini_get_dbg_callbacks ()->handle_exception ((MonoException *)obj, ctx, &ctx_cp, &catch_frame);
		}
	}

	if (out_ji)
		*out_ji = NULL;
	filter_idx = 0;
	initial_ctx = *ctx;

	unwinder_init (&unwinder);

	while (1) {
		MonoContext new_ctx;
		guint32 free_stack;
		int clause_index_start = 0;
		gboolean unwind_res = TRUE;
		StackFrameInfo frame;
		gpointer ip;
		
		if (resume) {
			resume = FALSE;
			ji = jit_tls->resume_state.ji;
			new_ctx = jit_tls->resume_state.new_ctx;
			clause_index_start = jit_tls->resume_state.clause_index;
			lmf = jit_tls->resume_state.lmf;
			first_filter_idx = jit_tls->resume_state.first_filter_idx;
			filter_idx = jit_tls->resume_state.filter_idx;
			in_interp = FALSE;
		} else {
			unwind_res = unwinder_unwind_frame (&unwinder, domain, jit_tls, NULL, ctx, &new_ctx, NULL, &lmf, NULL, &frame);
			if (!unwind_res) {
				*(mono_get_lmf_addr ()) = lmf;

				jit_tls->abort_func (obj);
				g_assert_not_reached ();
			}
			switch (frame.type) {
			case FRAME_TYPE_DEBUGGER_INVOKE:
			case FRAME_TYPE_MANAGED_TO_NATIVE:
			case FRAME_TYPE_TRAMPOLINE:
			case FRAME_TYPE_INTERP_TO_MANAGED_WITH_CTX:
				*ctx = new_ctx;
				continue;
			case FRAME_TYPE_INTERP_TO_MANAGED:
				continue;
			case FRAME_TYPE_INTERP:
			case FRAME_TYPE_MANAGED:
				break;
			default:
				g_assert_not_reached ();
				break;
			}
			in_interp = frame.type == FRAME_TYPE_INTERP;
			ji = frame.ji;
		}

		if (in_interp)
			ip = (guint8*)ji->code_start + frame.native_offset;
		else
			ip = MONO_CONTEXT_GET_IP (ctx);

		method = jinfo_get_method (ji);
		frame_count ++;
		//printf ("M: %s %d.\n", mono_method_full_name (method, TRUE), frame_count);

		if (stack_overflow) {
			free_stack = (guint8*)(MONO_CONTEXT_GET_SP (ctx)) - (guint8*)(MONO_CONTEXT_GET_SP (&initial_ctx));
		} else {
			free_stack = 0xffffff;
		}

		if (method->wrapper_type == MONO_WRAPPER_NATIVE_TO_MANAGED && ftnptr_eh_callback) {
			MonoGCHandle handle = mono_gchandle_new_internal (obj, FALSE);
			MONO_STACKDATA (stackptr);

			mono_threads_enter_gc_safe_region_unbalanced_internal (&stackptr);
			mono_set_lmf (lmf);
			ftnptr_eh_callback (handle);
			g_error ("Did not expect ftnptr_eh_callback to return.");
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

			if (is_address_protected (ji, ei, ip)) {
				/* catch block */
				MonoClass *catch_class = get_exception_catch_class (ei, ji, ctx);

				/*
				 * Have to unwrap RuntimeWrappedExceptions if the
				 * method's assembly doesn't have a RuntimeCompatibilityAttribute.
				 */
				if (non_exception && !wrap_non_exception_throws (method))
					ex_obj = non_exception;
				else
					ex_obj = obj;

				if (((ei->flags == MONO_EXCEPTION_CLAUSE_NONE) || (ei->flags == MONO_EXCEPTION_CLAUSE_FILTER))) {
#ifndef MONO_CROSS_COMPILE
#ifdef MONO_CONTEXT_SET_LLVM_EXC_REG
					MONO_CONTEXT_SET_LLVM_EXC_REG (ctx, ex_obj);
#else
					g_assert (!ji->from_llvm);
					/* store the exception object in bp + ei->exvar_offset */
					*((gpointer *)(gpointer)((char *)MONO_CONTEXT_GET_BP (ctx) + ei->exvar_offset)) = ex_obj;
#endif
#endif
				}

#ifdef MONO_CONTEXT_SET_LLVM_EH_SELECTOR_REG
				if (ji->from_llvm)
					MONO_CONTEXT_SET_LLVM_EH_SELECTOR_REG (ctx, ei->clause_index);
#endif

				if (ei->flags == MONO_EXCEPTION_CLAUSE_FILTER) {
					/* 
					 * Filter clauses should only be run in the 
					 * first pass of exception handling.
					 */
					filtered = (filter_idx == first_filter_idx);
					filter_idx ++;
				}

				error_init (error);
				if ((ei->flags == MONO_EXCEPTION_CLAUSE_NONE && 
				     mono_object_isinst_checked (ex_obj, catch_class, error)) || filtered) {
					/*
					 * This guards against the situation that we abort a thread that is executing a finally clause
					 * that was called by the EH machinery. It won't have a guard trampoline installed, so we must
					 * check for this situation here and resume interruption if we are below the guarded block.
					 */
					if (G_UNLIKELY (jit_tls->handler_block)) {
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
							g_assert (ji == mini_jit_info_table_find (domain, (char *)MONO_CONTEXT_GET_IP (&jit_tls->handler_block_context), NULL));

							if (!is_address_protected (ji, jit_tls->handler_block, ei->handler_start)) {
								is_outside = TRUE;
							}
						}
						if (is_outside) {
							jit_tls->handler_block = NULL;
							mono_thread_resume_interruption (TRUE); /*We ignore the exception here, it will be raised later*/
						}
					}

					if (mono_trace_is_enabled () && mono_trace_eval (method))
						g_print ("EXCEPTION: catch found at clause %d of %s\n", i, mono_method_full_name (method, TRUE));

					/*
					 * At this point, ei->flags can be either MONO_EXCEPTION_CLAUSE_NONE for a
					 * a try-catch clause or MONO_EXCEPTION_CLAUSE_FILTER for a try-filter-catch
					 * clause. Since we specifically want to indicate that we're executing the
					 * catch portion of this EH clause, pass MONO_EXCEPTION_CLAUSE_NONE explicitly
					 * instead of ei->flags.
					 */
					if (G_UNLIKELY (mono_profiler_clauses_enabled ())) {
						jit_tls->orig_ex_ctx_set = TRUE;
						MONO_PROFILER_RAISE (exception_clause, (method, i, MONO_EXCEPTION_CLAUSE_NONE, ex_obj));
						jit_tls->orig_ex_ctx_set = FALSE;
					}

					mini_set_abort_threshold (&frame);

					if (in_interp) {
						interp_exit_finally_abort_blocks (ji, clause_index_start, i, ip);
						/*
						 * ctx->pc points into the interpreter, after the call which transitioned to
						 * JITted code. Store the unwind state into the
						 * interpeter state, then resume, the interpreter will unwind itself until
						 * it reaches the target frame and will continue execution from there.
						 * The resuming is kinda hackish, from the native code standpoint, it looks
						 * like the call which transitioned to JITted code has succeeded, but the
						 * return value register etc. is not set, so we have to be careful.
						 */
						mini_get_interp_callbacks ()->set_resume_state (jit_tls, ex_obj, ei, frame.interp_frame, ei->handler_start);
						/* Undo the IP adjustment done by mono_arch_unwind_frame () */
						/* ip == 0 means an interpreter frame */
						if (MONO_CONTEXT_GET_IP (ctx) != 0)
							mono_arch_undo_ip_adjustment (ctx);
					} else {
						MONO_CONTEXT_SET_IP (ctx, ei->handler_start);
					}
					mono_set_lmf (lmf);
#ifndef DISABLE_PERFCOUNTERS
					mono_atomic_fetch_add_i32 (&mono_perfcounters->exceptions_depth, frame_count);
#endif
					if (obj == (MonoObject *)domain->stack_overflow_ex)
						jit_tls->handling_stack_ovf = FALSE;

					return 0;
				}
				mono_error_cleanup (error);
				if (ei->flags == MONO_EXCEPTION_CLAUSE_FAULT) {
					if (mono_trace_is_enabled () && mono_trace_eval (method))
						g_print ("EXCEPTION: fault clause %d of %s\n", i, mono_method_full_name (method, TRUE));

					if (G_UNLIKELY (mono_profiler_clauses_enabled ())) {
						jit_tls->orig_ex_ctx_set = TRUE;
						MONO_PROFILER_RAISE (exception_clause, (method, i, (MonoExceptionEnum)ei->flags, ex_obj));
						jit_tls->orig_ex_ctx_set = FALSE;
					}
				}
				if (ei->flags == MONO_EXCEPTION_CLAUSE_FINALLY) {
					if (mono_trace_is_enabled () && mono_trace_eval (method))
						g_print ("EXCEPTION: finally clause %d of %s\n", i, mono_method_full_name (method, TRUE));

					if (G_UNLIKELY (mono_profiler_clauses_enabled ())) {
						jit_tls->orig_ex_ctx_set = TRUE;
						MONO_PROFILER_RAISE (exception_clause, (method, i, (MonoExceptionEnum)ei->flags, ex_obj));
						jit_tls->orig_ex_ctx_set = FALSE;
					}

#ifndef DISABLE_PERFCOUNTERS
					mono_atomic_inc_i32 (&mono_perfcounters->exceptions_finallys);
#endif
				}
				if (ei->flags == MONO_EXCEPTION_CLAUSE_FAULT || ei->flags == MONO_EXCEPTION_CLAUSE_FINALLY) {
					mono_set_lmf (lmf);
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
						mini_set_abort_threshold (&frame);
						MONO_CONTEXT_SET_IP (ctx, ei->handler_start);
						return 0;
					} else {
						mini_set_abort_threshold (&frame);
						if (in_interp) {
							gboolean has_ex = mini_get_interp_callbacks ()->run_finally (&frame, i, ei->handler_start, ei->data.handler_end);
							if (has_ex) {
								/*
								 * If run_finally didn't resume to a context, it means that the handler frame
								 * is linked to the frame calling finally through interpreter frames. This
								 * means that we will reach the handler frame by resuming the current context.
								 */
								if (MONO_CONTEXT_GET_IP (ctx) != 0)
									mono_arch_undo_ip_adjustment (ctx);
								return 0;
							}
						} else {
							call_filter (ctx, ei->handler_start);
						}
					}
				}
			}
		}

		if (in_interp)
			interp_exit_finally_abort_blocks (ji, clause_index_start, ji->num_clauses, ip);

		if (MONO_PROFILER_ENABLED (method_exception_leave) &&
		    mono_profiler_get_call_instrumentation_flags (method) & MONO_PROFILER_CALL_INSTRUMENTATION_EXCEPTION_LEAVE) {
			jit_tls->orig_ex_ctx_set = TRUE;
			MONO_PROFILER_RAISE (method_exception_leave, (method, ex_obj));
			jit_tls->orig_ex_ctx_set = FALSE;
		}

		*ctx = new_ctx;
	}

	g_assert_not_reached ();
}

/**
 * mono_debugger_run_finally:
 * \param start_ctx saved processor state
 * This method is called by the Mono Debugger to call all \c finally clauses of the
 * current stack frame.  It's used when the user issues a \c return command to make
 * the current stack frame return.  After returning from this method, the debugger
 * unwinds the stack one frame and gives control back to the user.
 * NOTE: This method is only used when running inside the Mono Debugger.
 */
void
mono_debugger_run_finally (MonoContext *start_ctx)
{
	static int (*call_filter) (MonoContext *, gpointer) = NULL;
	MonoDomain *domain = mono_domain_get ();
	MonoJitTlsData *jit_tls = mono_tls_get_jit_tls ();
	MonoLMF *lmf = mono_get_lmf ();
	MonoContext ctx, new_ctx;
	MonoJitInfo *ji, rji;
	int i;

	ctx = *start_ctx;

	ji = mono_find_jit_info (domain, jit_tls, &rji, NULL, &ctx, &new_ctx, NULL, &lmf, NULL, NULL);
	if (!ji || ji == (gpointer)-1)
		return;

	if (!call_filter)
		call_filter = (int (*)(MonoContext *, void *))mono_get_call_filter ();

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
 * \param ctx saved processor state
 * \param obj the exception object
 *
 *   Handle the exception OBJ starting from the state CTX. Modify CTX to point to the handler clause if the exception is caught, and
 * return TRUE.
 */
gboolean
mono_handle_exception (MonoContext *ctx, gpointer void_obj)
{
	MonoObject *obj = (MonoObject*)void_obj;

	MONO_REQ_GC_UNSAFE_MODE;

#ifndef DISABLE_PERFCOUNTERS
	mono_atomic_inc_i32 (&mono_perfcounters->exceptions_thrown);
#endif

	return mono_handle_exception_internal (ctx, obj, FALSE, NULL);
}

#ifdef MONO_ARCH_SIGSEGV_ON_ALTSTACK

#ifndef MONO_ARCH_USE_SIGACTION
#error "Can't use sigaltstack without sigaction"
#endif

void
mono_setup_altstack (MonoJitTlsData *tls)
{
	size_t stsize = 0;
	stack_t sa;
	guint8 *staddr = NULL;
#if defined(TARGET_OSX) || defined(_AIX)
	/*
	 * On macOS Mojave we are encountering a bug when changing mapping for main thread
	 * stack pages. Stack overflow on main thread will kill the app.
	 *
	 * AIX seems problematic as well; it gives ENOMEM for mprotect and valloc, if we
	 * do this for thread 1 with its stack at the top of memory. Other threads seem
	 * fine for the altstack guard page, though.
	 */
	gboolean disable_stack_guard = mono_threads_platform_is_main_thread ();
#else
	gboolean disable_stack_guard = FALSE;
#endif

	if (mono_running_on_valgrind ())
		return;

	mono_thread_info_get_stack_bounds (&staddr, &stsize);

	g_assert (staddr);

	tls->end_of_stack = staddr + stsize;
	tls->stack_size = stsize;

	/*g_print ("thread %p, stack_base: %p, stack_size: %d\n", (gpointer)pthread_self (), staddr, stsize);*/

	if (!disable_stack_guard) {
		tls->stack_ovf_guard_base = staddr + mono_pagesize ();
		tls->stack_ovf_guard_size = ALIGN_TO (MONO_STACK_OVERFLOW_GUARD_SIZE, mono_pagesize ());

		g_assert ((guint8*)&sa >= (guint8*)tls->stack_ovf_guard_base + tls->stack_ovf_guard_size);

		if (mono_mprotect (tls->stack_ovf_guard_base, tls->stack_ovf_guard_size, MONO_MMAP_NONE)) {
			/* mprotect can fail for the main thread stack */
			gpointer gaddr = mono_valloc (tls->stack_ovf_guard_base, tls->stack_ovf_guard_size, MONO_MMAP_NONE|MONO_MMAP_PRIVATE|MONO_MMAP_ANON|MONO_MMAP_FIXED, MONO_MEM_ACCOUNT_EXCEPTIONS);
			if (gaddr) {
				g_assert (gaddr == tls->stack_ovf_guard_base);
				tls->stack_ovf_valloced = TRUE;
			} else {
				g_warning ("couldn't allocate guard page, continue without it");
				tls->stack_ovf_guard_base = NULL;
				tls->stack_ovf_guard_size = 0;
			}
		}
	}

	/* Setup an alternate signal stack */
	tls->signal_stack = mono_valloc (0, MONO_ARCH_SIGNAL_STACK_SIZE, MONO_MMAP_READ|MONO_MMAP_WRITE|MONO_MMAP_PRIVATE|MONO_MMAP_ANON, MONO_MEM_ACCOUNT_EXCEPTIONS);
	tls->signal_stack_size = MONO_ARCH_SIGNAL_STACK_SIZE;

	g_assert (tls->signal_stack);

	sa.ss_sp = tls->signal_stack;
	sa.ss_size = MONO_ARCH_SIGNAL_STACK_SIZE;
	sa.ss_flags = 0;
	g_assert (sigaltstack (&sa, NULL) == 0);

	if (tls->stack_ovf_guard_base)
		mono_gc_register_altstack ((char*)tls->stack_ovf_guard_base + tls->stack_ovf_guard_size, (char*)staddr + stsize - ((char*)tls->stack_ovf_guard_base + tls->stack_ovf_guard_size), tls->signal_stack, tls->signal_stack_size);
	else
		mono_gc_register_altstack (staddr, stsize, tls->signal_stack, tls->signal_stack_size);

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
		mono_vfree (tls->signal_stack, MONO_ARCH_SIGNAL_STACK_SIZE, MONO_MEM_ACCOUNT_EXCEPTIONS);

	if (!tls->stack_ovf_guard_base)
		return;
	if (tls->stack_ovf_valloced)
		mono_vfree (tls->stack_ovf_guard_base, tls->stack_ovf_guard_size, MONO_MEM_ACCOUNT_EXCEPTIONS);
	else
		mono_mprotect (tls->stack_ovf_guard_base, tls->stack_ovf_guard_size, MONO_MMAP_READ|MONO_MMAP_WRITE);
}

#elif G_HAVE_API_SUPPORT(HAVE_CLASSIC_WINAPI_SUPPORT) && defined(HOST_WIN32)
void
mono_setup_altstack (MonoJitTlsData *tls)
{
	// Alt stack is not supported on Windows, but we can use this point to at least
	// reserve a stack guarantee of available stack memory when handling stack overflow.
	ULONG new_stack_guarantee = (ULONG)ALIGN_TO (MONO_STACK_OVERFLOW_GUARD_SIZE, ((gssize)mono_pagesize ()));
	SetThreadStackGuarantee (&new_stack_guarantee);
}

void
mono_free_altstack (MonoJitTlsData *tls)
{
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

gboolean
mono_handle_soft_stack_ovf (MonoJitTlsData *jit_tls, MonoJitInfo *ji, void *ctx, MONO_SIG_HANDLER_INFO_TYPE *siginfo, guint8* fault_addr)
{
	if (!jit_tls)
		return FALSE;

	if (mono_llvm_only)
		return FALSE;

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
		gboolean handled = FALSE;

		mono_mprotect (jit_tls->stack_ovf_guard_base, jit_tls->stack_ovf_guard_size, MONO_MMAP_READ|MONO_MMAP_WRITE);
#ifdef MONO_ARCH_SIGSEGV_ON_ALTSTACK
		if (ji) {
			mono_arch_handle_altstack_exception (ctx, siginfo, fault_addr, TRUE);
			handled = TRUE;
		}
#endif
		if (!handled) {
			/* We print a message: after this even managed stack overflows
			 * may crash the runtime
			 */
			mono_runtime_printf_err ("Stack overflow in unmanaged: IP: %p, fault addr: %p", mono_arch_ip_from_context (ctx), fault_addr);
			if (!jit_tls->handling_stack_ovf) {
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
	PrintOverflowUserData *user_data = (PrintOverflowUserData *)data;
	gchar *location;

	if (frame->ji && frame->type != FRAME_TYPE_TRAMPOLINE)
		method = jinfo_get_method (frame->ji);

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
mono_handle_hard_stack_ovf (MonoJitTlsData *jit_tls, MonoJitInfo *ji, MonoContext *mctx, guint8* fault_addr)
{
	PrintOverflowUserData ud;

	/* we don't do much now, but we can warn the user with a useful message */
	mono_runtime_printf_err ("Stack overflow: IP: %p, fault addr: %p", MONO_CONTEXT_GET_IP (mctx), fault_addr);

	mono_runtime_printf_err ("Stacktrace:");

	memset (&ud, 0, sizeof (ud));

	mono_walk_stack_with_ctx (print_overflow_stack_frame, mctx, MONO_UNWIND_LOOKUP_ACTUAL_METHOD, &ud);

	_exit (1);
}

static gboolean
print_stack_frame_signal_safe (StackFrameInfo *frame, MonoContext *ctx, gpointer data)
{
	MonoMethod *method = NULL;

	if (frame->ji && frame->type != FRAME_TYPE_TRAMPOLINE)
		method = jinfo_get_method (frame->ji);

	if (method) {
		const char *name_space = m_class_get_name_space (method->klass);
		g_async_safe_printf("\t  at %s%s%s:%s <0x%05x>\n", name_space, (name_space [0] != '\0' ? "." : ""), m_class_get_name (method->klass), method->name, frame->native_offset);
	} else {
		g_async_safe_printf("\t  at <unknown> <0x%05x>\n", frame->native_offset);
	}

	return FALSE;
}

static G_GNUC_UNUSED gboolean
print_stack_frame_to_string (StackFrameInfo *frame, MonoContext *ctx, gpointer data)
{
	GString *p = (GString*)data;
	MonoMethod *method = NULL;

	if (frame->ji && frame->type != FRAME_TYPE_TRAMPOLINE)
		method = jinfo_get_method (frame->ji);

	if (method && frame->domain) {
		gchar *location = mono_debug_print_stack_frame (method, frame->native_offset, frame->domain);
		g_string_append_printf (p, "  %s\n", location);
		g_free (location);
	} else
		g_string_append_printf (p, "  at <unknown> <0x%05x>\n", frame->native_offset);

	return FALSE;
}

#ifndef MONO_CROSS_COMPILE

/*
 * mono_handle_native_crash:
 *
 *   Handle a native crash (e.g. SIGSEGV) while in native code by
 *   printing diagnostic information and aborting.
 */
void
mono_handle_native_crash (const char *signal, MonoContext *mctx, MONO_SIG_HANDLER_INFO_TYPE *info, void *context)
{
	MonoJitTlsData *jit_tls = mono_tls_get_jit_tls ();

#ifdef MONO_ARCH_USE_SIGACTION
	struct sigaction sa;
	sa.sa_handler = SIG_DFL;
	sigemptyset (&sa.sa_mask);
	sa.sa_flags = 0;

	/* Mono has crashed - remove our handlers except SIGTERM, which is used by crash reporting */
	/*  TODO: Combine with mono_runtime_cleanup_handlers (but with an option to not de-allocate anything) */
	if (mini_debug_options.handle_sigint) {
		g_assert (sigaction (SIGINT, &sa, NULL) != -1);
	}

	g_assert (sigaction (SIGABRT, &sa, NULL) != -1);
	g_assert (sigaction (SIGFPE, &sa, NULL) != -1);
	g_assert (sigaction (SIGSYS, &sa, NULL) != -1);
	g_assert (sigaction (SIGSEGV, &sa, NULL) != -1);
	g_assert (sigaction (SIGQUIT, &sa, NULL) != -1);
	g_assert (sigaction (SIGBUS, &sa, NULL) != -1);
	g_assert (sigaction (SIGILL, &sa, NULL) != -1);
	g_assert (sigaction (SIGCHLD, &sa, NULL) != -1);
	g_assert (sigaction (SIGQUIT, &sa, NULL) != -1);

#endif

	if (mini_debug_options.suspend_on_native_crash) {
		g_async_safe_printf ("Received %s, suspending...\n", signal);
		while (1) {
			// Sleep for 1 second.
			g_usleep (1000 * 1000);
		}
	}

	/*
	 * A crash indicates something went very wrong so we can no longer depend
	 * on anything working. So try to print out lots of diagnostics, starting 
	 * with ones which have a greater chance of working.
	 */

	g_async_safe_printf("\n=================================================================\n");
	g_async_safe_printf("\tNative Crash Reporting\n");
	g_async_safe_printf("=================================================================\n");
	g_async_safe_printf("Got a %s while executing native code. This usually indicates\n", signal);
	g_async_safe_printf("a fatal error in the mono runtime or one of the native libraries \n");
	g_async_safe_printf("used by your application.\n");
	g_async_safe_printf("=================================================================\n");
	mono_dump_native_crash_info (signal, mctx, info);

	/* !jit_tls means the thread was not registered with the runtime */
	// This must be below the native crash dump, because we can't safely
	// do runtime state probing after we have walked the managed stack here.
	if (jit_tls && mono_thread_internal_current () && mctx) {
		g_async_safe_printf ("\n=================================================================\n");
		g_async_safe_printf ("\tManaged Stacktrace:\n");
		g_async_safe_printf ("=================================================================\n");

		mono_walk_stack_full (print_stack_frame_signal_safe, mctx, mono_domain_get (), jit_tls, mono_get_lmf (), MONO_UNWIND_LOOKUP_IL_OFFSET, NULL, TRUE);
		g_async_safe_printf ("=================================================================\n");
	}

	mono_post_native_crash_handler (signal, mctx, info, mono_do_crash_chaining, context);
}

#else

void
mono_handle_native_crash (const char *signal, MonoContext *mctx, MONO_SIG_HANDLER_INFO_TYPE *info, void *context)
{
	g_assert_not_reached ();
}

#endif /* !MONO_CROSS_COMPILE */

static void
mono_print_thread_dump_internal (void *sigctx, MonoContext *start_ctx)
{
	MonoInternalThread *thread = mono_thread_internal_current ();
	MonoContext ctx;
	GString* text;

	if (!thread)
		return;

	text = g_string_new (0);

	mono_gstring_append_thread_name (text, thread);

	g_string_append_printf (text, " tid=%p this=%p ", (gpointer)(gsize)thread->tid, thread);
	mono_thread_internal_describe (thread, text);
	g_string_append (text, "\n");

	if (start_ctx) {
		memcpy (&ctx, start_ctx, sizeof (MonoContext));
	} else if (!sigctx)
		MONO_INIT_CONTEXT_FROM_FUNC (&ctx, mono_print_thread_dump);
	else
		mono_sigctx_to_monoctx (sigctx, &ctx);

	mono_walk_stack_with_ctx (print_stack_frame_to_string, &ctx, MONO_UNWIND_LOOKUP_ALL, text);

	mono_runtime_printf ("%s", text->str);

#if HOST_WIN32 && TARGET_WIN32 && _DEBUG
	OutputDebugStringA(text->str);
#endif

	g_string_free (text, TRUE);
	mono_runtime_stdout_fflush ();
}

/**
 * mono_print_thread_dump:
 *
 * Print information about the current thread to stdout.
 * \p sigctx can be NULL, allowing this to be called from gdb.
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
	MONO_REQ_GC_UNSAFE_MODE;

	MonoJitTlsData *jit_tls = mono_tls_get_jit_tls ();
	MonoContext new_ctx;

	MONO_CONTEXT_SET_IP (ctx, MONO_CONTEXT_GET_IP (&jit_tls->resume_state.ctx));
	MONO_CONTEXT_SET_SP (ctx, MONO_CONTEXT_GET_SP (&jit_tls->resume_state.ctx));
	new_ctx = *ctx;

	mono_handle_exception_internal (&new_ctx, (MonoObject *)jit_tls->resume_state.ex_obj, TRUE, NULL);

	mono_restore_context (&new_ctx);
}

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
	FindHandlerBlockData *pdata = (FindHandlerBlockData *)data;
	MonoJitInfo *ji = frame->ji;

	if (!ji)
		return FALSE;

	ip = MONO_CONTEXT_GET_IP (ctx);

	for (i = 0; i < ji->num_clauses; ++i) {
		MonoJitExceptionInfo *ei = ji->clauses + i;
		if (ei->flags != MONO_EXCEPTION_CLAUSE_FINALLY)
			continue;
		/*If ip points to the first instruction it means the handler block didn't start
		 so we can leave its execution to the EH machinery*/
		if (ei->handler_start <= ip && ip < ei->data.handler_end) {
			pdata->ji = ji;
			pdata->ei = ei;
			pdata->ctx = *ctx;
			break;
		}
	}
	return FALSE;
}


static void
install_handler_block_guard (MonoJitInfo *ji, MonoContext *ctx)
{
	int i;
	MonoJitExceptionInfo *clause = NULL;
	gpointer ip;
	guint8 *bp;

	ip = MONO_CONTEXT_GET_IP (ctx);

	for (i = 0; i < ji->num_clauses; ++i) {
		clause = &ji->clauses [i];
		if (clause->flags != MONO_EXCEPTION_CLAUSE_FINALLY)
			continue;
		if (clause->handler_start <= ip && clause->data.handler_end > ip)
			break;
	}

	/*no matching finally - can't happen, we parallel the logic in find_last_handler_block. */
	g_assert (i < ji->num_clauses);

	/*Load the spvar*/
	bp = (guint8*)MONO_CONTEXT_GET_BP (ctx);
	*(bp + clause->exvar_offset) = 1;
}

/*
 * Finds the bottom handler block running and install a block guard if needed.
 */
static gboolean
mono_install_handler_block_guard (MonoThreadUnwindState *ctx)
{
	FindHandlerBlockData data = { 0 };
	MonoJitTlsData *jit_tls = (MonoJitTlsData *)ctx->unwind_data [MONO_UNWIND_DATA_JIT_TLS];

	/* Guard against a null MonoJitTlsData. This can happens if the thread receives the
         * interrupt signal before the JIT has time to initialize its TLS data for the given thread.
	 */
	if (!jit_tls || jit_tls->handler_block)
		return FALSE;

	/* Do an async safe stack walk */
	mono_thread_info_set_is_async_context (TRUE);
	mono_walk_stack_with_state (find_last_handler_block, ctx, MONO_UNWIND_NONE, &data);
	mono_thread_info_set_is_async_context (FALSE);

	if (!data.ji)
		return FALSE;

	memcpy (&jit_tls->handler_block_context, &data.ctx, sizeof (MonoContext));

	install_handler_block_guard (data.ji, &data.ctx);

	jit_tls->handler_block = data.ei;

	return TRUE;
}

static void
mono_uninstall_current_handler_block_guard (void)
{
	MonoJitTlsData *jit_tls = mono_tls_get_jit_tls ();
	if (jit_tls)
		jit_tls->handler_block = NULL;
}


static gboolean
mono_current_thread_has_handle_block_guard (void)
{
	MonoJitTlsData *jit_tls = mono_tls_get_jit_tls ();
	return jit_tls && jit_tls->handler_block != NULL;
}

void
mono_set_cast_details (MonoClass *from, MonoClass *to)
{
	MonoJitTlsData *jit_tls = NULL;

	if (mini_debug_options.better_cast_details) {
		jit_tls = mono_tls_get_jit_tls ();
		jit_tls->class_cast_from = from;
		jit_tls->class_cast_to = to;
	}
}


/*returns false if the thread is not attached*/
gboolean
mono_thread_state_init_from_sigctx (MonoThreadUnwindState *ctx, void *sigctx)
{
	MonoThreadInfo *thread = mono_thread_info_current_unchecked ();
	if (!thread) {
		ctx->valid = FALSE;
		return FALSE;
	}

	if (sigctx) {
		mono_sigctx_to_monoctx (sigctx, &ctx->ctx);

		ctx->unwind_data [MONO_UNWIND_DATA_DOMAIN] = mono_domain_get ();
		ctx->unwind_data [MONO_UNWIND_DATA_LMF] = mono_get_lmf ();
		ctx->unwind_data [MONO_UNWIND_DATA_JIT_TLS] = thread->jit_data;
	}
	else {
		mono_thread_state_init (ctx);
	}

	if (!ctx->unwind_data [MONO_UNWIND_DATA_DOMAIN] || !ctx->unwind_data [MONO_UNWIND_DATA_LMF])
		return FALSE;

	ctx->valid = TRUE;
	return TRUE;
}

void
mono_thread_state_init (MonoThreadUnwindState *ctx)
{
	MonoThreadInfo *thread = mono_thread_info_current_unchecked ();

#if defined(MONO_CROSS_COMPILE)
	ctx->valid = FALSE; //A cross compiler doesn't need to suspend.
#elif MONO_ARCH_HAS_MONO_CONTEXT
	MONO_CONTEXT_GET_CURRENT (ctx->ctx);
#else
	g_error ("Use a null sigctx requires a working mono-context");
#endif

	ctx->unwind_data [MONO_UNWIND_DATA_DOMAIN] = mono_domain_get ();
	ctx->unwind_data [MONO_UNWIND_DATA_LMF] = mono_get_lmf ();
	ctx->unwind_data [MONO_UNWIND_DATA_JIT_TLS] = thread ? thread->jit_data : NULL;
	ctx->valid = TRUE;
}


gboolean
mono_thread_state_init_from_monoctx (MonoThreadUnwindState *ctx, MonoContext *mctx)
{
	MonoThreadInfo *thread = mono_thread_info_current_unchecked ();
	if (!thread) {
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
	MonoThreadInfo *thread = mono_thread_info_current_unchecked ();
	MONO_ARCH_CONTEXT_DEF

	mono_arch_flush_register_windows ();

	if (!thread || !thread->jit_data) {
		ctx->valid = FALSE;
		return FALSE;
	}
	MONO_INIT_CONTEXT_FROM_FUNC (&ctx->ctx, mono_thread_state_init_from_current);

	ctx->unwind_data [MONO_UNWIND_DATA_DOMAIN] = mono_domain_get ();
	ctx->unwind_data [MONO_UNWIND_DATA_LMF] = mono_get_lmf ();
	ctx->unwind_data [MONO_UNWIND_DATA_JIT_TLS] = thread->jit_data;
	ctx->valid = TRUE;
	return TRUE;
}

static void
mono_raise_exception_with_ctx (MonoException *exc, MonoContext *ctx)
{
	mono_handle_exception (ctx, (MonoObject *)exc);
	mono_restore_context (ctx);
}

/*FIXME Move all monoctx -> sigctx conversion to signal handlers once all archs support utils/mono-context */
void
mono_setup_async_callback (MonoContext *ctx, void (*async_cb)(void *fun), gpointer user_data)
{
#ifdef MONO_ARCH_HAVE_SETUP_ASYNC_CALLBACK
	MonoJitTlsData *jit_tls = mono_tls_get_jit_tls ();
	jit_tls->ex_ctx = *ctx;

	mono_arch_setup_async_callback (ctx, async_cb, user_data);
#else
	g_error ("This target doesn't support mono_arch_setup_async_callback");
#endif
}

/*
 * mono_restore_context:
 *
 *   Call the architecture specific restore context function.
 */
void
mono_restore_context (MonoContext *ctx)
{
	static void (*restore_context) (MonoContext *);

	if (!restore_context)
		restore_context = (void (*)(MonoContext *))mono_get_restore_context ();
	restore_context (ctx);
	g_assert_not_reached ();
}

/*
 * mono_jinfo_get_unwind_info:
 *
 *   Return the unwind info for JI.
 */
guint8*
mono_jinfo_get_unwind_info (MonoJitInfo *ji, guint32 *unwind_info_len)
{
	if (ji->has_unwind_info) {
		/* The address/length in the MonoJitInfo structure itself */
		MonoUnwindJitInfo *info = mono_jit_info_get_unwind_info (ji);
		*unwind_info_len = info->unw_info_len;
		return info->unw_info;
	} else if (ji->from_aot)
		return mono_aot_get_unwind_info (ji, unwind_info_len);
	else
		return mono_get_cached_unwind_info (ji->unwind_info, unwind_info_len);
}

int
mono_jinfo_get_epilog_size (MonoJitInfo *ji)
{
	MonoArchEHJitInfo *info;

	info = mono_jit_info_get_arch_eh_info (ji);
	g_assert (info);

	return info->epilog_size;
}

/*
 * mono_install_ftnptr_eh_callback:
 *
 *   Install a callback that should be called when there is a managed exception
 *   in a native-to-managed wrapper. This is mainly used by iOS to convert a
 *   managed exception to a native exception, to properly unwind the native
 *   stack; this native exception will then be converted back to a managed
 *   exception in their managed-to-native wrapper.
 */
void
mono_install_ftnptr_eh_callback (MonoFtnPtrEHCallback callback)
{
	ftnptr_eh_callback = callback;
}

/*
 * LLVM/Bitcode exception handling.
 */

static void
throw_exception (MonoObject *ex, gboolean rethrow)
{
	MONO_REQ_GC_UNSAFE_MODE;

	ERROR_DECL (error);
	MonoJitTlsData *jit_tls = mono_get_jit_tls ();
	MonoException *mono_ex;

	if (!mono_object_isinst_checked (ex, mono_defaults.exception_class, error)) {
		mono_error_assert_ok (error);
		mono_ex = mono_get_exception_runtime_wrapped_checked (ex, error);
		mono_error_assert_ok (error);
		jit_tls->thrown_non_exc = mono_gchandle_new_internal (ex, FALSE);
	}
	else
		mono_ex = (MonoException*)ex;

	// Note: Not pinned
	jit_tls->thrown_exc = mono_gchandle_new_internal ((MonoObject*)mono_ex, FALSE);

	if (!rethrow) {
#ifdef MONO_ARCH_HAVE_UNWIND_BACKTRACE
		GList *l, *ips = NULL;
		GList *trace;

		_Unwind_Backtrace (build_stack_trace, &ips);
		/* The list contains ip-gshared info pairs */
		trace = NULL;
		ips = g_list_reverse (ips);
		for (l = ips; l; l = l->next) {
			trace = g_list_append (trace, l->data);
			trace = g_list_append (trace, NULL);
			trace = g_list_append (trace, NULL);
		}
		MonoArray *ips_arr = mono_glist_to_array (trace, mono_defaults.int_class, error);
		mono_error_assert_ok (error);
		MONO_OBJECT_SETREF_INTERNAL (mono_ex, trace_ips, ips_arr);
		g_list_free (l);
		g_list_free (trace);
#endif
	}

	mono_llvm_cpp_throw_exception ();
}

void
mono_llvm_throw_exception (MonoObject *ex)
{
	throw_exception (ex, FALSE);
}

void
mono_llvm_rethrow_exception (MonoObject *ex)
{
	throw_exception (ex, TRUE);
}

void
mono_llvm_raise_exception (MonoException *e)
{
	mono_llvm_throw_exception ((MonoObject*)e);
}

void
mono_llvm_reraise_exception (MonoException *e)
{
	mono_llvm_rethrow_exception ((MonoObject*)e);
}

void
mono_llvm_throw_corlib_exception (guint32 ex_token_index)
{
	guint32 ex_token = MONO_TOKEN_TYPE_DEF | ex_token_index;
	MonoException *ex;

	ex = mono_exception_from_token (m_class_get_image (mono_defaults.exception_class), ex_token);

	mono_llvm_throw_exception ((MonoObject*)ex);
}

/*
 * mono_llvm_resume_exception:
 *
 *   Resume exception propagation.
 */
void
mono_llvm_resume_exception (void)
{
	mono_llvm_cpp_throw_exception ();
}

/*
 * mono_llvm_load_exception:
 *
 *   Return the currently thrown exception.
 */
MonoObject *
mono_llvm_load_exception (void)
{
	ERROR_DECL (error);
	MonoJitTlsData *jit_tls = mono_get_jit_tls ();

	MonoException *mono_ex = (MonoException*)mono_gchandle_get_target_internal (jit_tls->thrown_exc);

	MonoArray *ta = mono_ex->trace_ips;

	if (ta) {
		GList *trace_ips = NULL;
		gpointer ip = MONO_RETURN_ADDRESS ();

		size_t upper = mono_array_length_internal (ta);

		for (int i = 0; i < upper; i += TRACE_IP_ENTRY_SIZE) {
			gpointer curr_ip = mono_array_get_internal (ta, gpointer, i);
			for (int j = 0; j < TRACE_IP_ENTRY_SIZE; ++j) {
				gpointer p = mono_array_get_internal (ta, gpointer, i + j);
				trace_ips = g_list_append (trace_ips, p);
			}
			if (ip == curr_ip)
				break;
		}

		// FIXME: Does this work correctly for rethrows?
		// We may be discarding useful information
		// when this gets GC'ed
		MonoArray *ips_arr = mono_glist_to_array (trace_ips, mono_defaults.int_class, error);
		mono_error_assert_ok (error);
		MONO_OBJECT_SETREF_INTERNAL (mono_ex, trace_ips, ips_arr);
		g_list_free (trace_ips);

		// FIXME:
		//MONO_OBJECT_SETREF_INTERNAL (mono_ex, stack_trace, ves_icall_System_Exception_get_trace (mono_ex));
	} else {
		MONO_OBJECT_SETREF_INTERNAL (mono_ex, trace_ips, mono_array_new_checked (mono_domain_get (), mono_defaults.int_class, 0, error));
		mono_error_assert_ok (error);
		MONO_OBJECT_SETREF_INTERNAL (mono_ex, stack_trace, mono_array_new_checked (mono_domain_get (), mono_defaults.stack_frame_class, 0, error));
		mono_error_assert_ok (error);
	}

	return &mono_ex->object;
}

/*
 * mono_llvm_clear_exception:
 *
 *   Mark the currently thrown exception as handled.
 */
void
mono_llvm_clear_exception (void)
{
	MonoJitTlsData *jit_tls = mono_get_jit_tls ();
	mono_gchandle_free_internal (jit_tls->thrown_exc);
	jit_tls->thrown_exc = 0;
	if (jit_tls->thrown_non_exc)
		mono_gchandle_free_internal (jit_tls->thrown_non_exc);
	jit_tls->thrown_non_exc = 0;

	mono_memory_barrier ();
}

/*
 * mono_llvm_match_exception:
 *
 *   Return the innermost clause containing REGION_START-REGION_END which can handle
 * the current exception.
 */
gint32
mono_llvm_match_exception (MonoJitInfo *jinfo, guint32 region_start, guint32 region_end, gpointer rgctx, MonoObject *this_obj)
{
	ERROR_DECL (error);
	MonoJitTlsData *jit_tls = mono_get_jit_tls ();
	MonoObject *exc;
	gint32 index = -1;

	g_assert (jit_tls->thrown_exc);
	exc = mono_gchandle_get_target_internal (jit_tls->thrown_exc);
	if (jit_tls->thrown_non_exc) {
		/*
		 * Have to unwrap RuntimeWrappedExceptions if the
		 * method's assembly doesn't have a RuntimeCompatibilityAttribute.
		 */
		if (!wrap_non_exception_throws (jinfo_get_method (jinfo)))
			exc = mono_gchandle_get_target_internal (jit_tls->thrown_non_exc);
	}

	for (int i = 0; i < jinfo->num_clauses; i++) {
		MonoJitExceptionInfo *ei = &jinfo->clauses [i];
		MonoClass *catch_class;

		if (! (ei->try_offset == region_start && ei->try_offset + ei->try_len == region_end) )
			continue;

		catch_class = ei->data.catch_class;
		if (mono_class_is_open_constructed_type (m_class_get_byval_arg (catch_class))) {
			MonoGenericContext context;
			MonoType *inflated_type;

			g_assert (rgctx || this_obj);
			context = mono_get_generic_context_from_stack_frame (jinfo, rgctx ? rgctx : this_obj->vtable);
			inflated_type = mono_class_inflate_generic_type_checked (m_class_get_byval_arg (catch_class), &context, error);
			mono_error_assert_ok (error); /* FIXME don't swallow the error */

			catch_class = mono_class_from_mono_type_internal (inflated_type);
			mono_metadata_free_type (inflated_type);
		}

		// FIXME: Handle edge cases handled in get_exception_catch_class
		if (ei->flags == MONO_EXCEPTION_CLAUSE_NONE && mono_object_isinst_checked (exc, catch_class, error)) {
			index = ei->clause_index;
			break;
		} else
			mono_error_assert_ok (error);
		
		if (ei->flags == MONO_EXCEPTION_CLAUSE_FILTER) {
			g_assert_not_reached ();
		}
	}

	return index;
}

#if defined(ENABLE_LLVM) && defined(HAVE_UNWIND_H)
G_EXTERN_C _Unwind_Reason_Code mono_debug_personality (int a, _Unwind_Action b,
	uint64_t c, struct _Unwind_Exception *d, struct _Unwind_Context *e)
{
	g_assert_not_reached ();
}
#else
G_EXTERN_C void mono_debug_personality (void);

void
mono_debug_personality (void)
{
	g_assert_not_reached ();
}
#endif
