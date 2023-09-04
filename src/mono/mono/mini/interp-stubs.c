#include <config.h>

#include "interp/interp.h"

/* interpreter callback stubs */

static MonoJitInfo*
stub_find_jit_info (MonoMethod *method)
{
	return NULL;
}

static void
stub_set_breakpoint (MonoJitInfo *jinfo, gpointer ip)
{
	g_assert_not_reached ();
}

static void
stub_clear_breakpoint (MonoJitInfo *jinfo, gpointer ip)
{
	g_assert_not_reached ();
}

static MonoJitInfo*
stub_frame_get_jit_info (MonoInterpFrameHandle frame)
{
	g_assert_not_reached ();
	return NULL;
}

static gpointer
stub_frame_get_ip (MonoInterpFrameHandle frame)
{
	g_assert_not_reached ();
	return NULL;
}

static gpointer
stub_frame_get_arg (MonoInterpFrameHandle frame, int pos)
{
	g_assert_not_reached ();
	return NULL;
}

static gpointer
stub_frame_get_local (MonoInterpFrameHandle frame, int pos)
{
	g_assert_not_reached ();
	return NULL;
}

static gpointer
stub_frame_get_this (MonoInterpFrameHandle frame)
{
	g_assert_not_reached ();
	return NULL;
}

static MonoInterpFrameHandle
stub_frame_get_parent (MonoInterpFrameHandle frame)
{
	g_assert_not_reached ();
	return NULL;
}

static void
stub_start_single_stepping (void)
{
}

static void
stub_stop_single_stepping (void)
{
}

static void
stub_set_optimizations (guint32 i)
{
}

static void
stub_invalidate_transformed (void)
{
}

static void
stub_cleanup (void)
{
}

static void
stub_set_resume_state (MonoJitTlsData *jit_tls, MonoObject *ex, MonoJitExceptionInfo *ei, MonoInterpFrameHandle interp_frame, gpointer handler_ip)
{
	g_assert_not_reached ();
}

static void
stub_get_resume_state (const MonoJitTlsData *jit_tls, gboolean *has_resume_state, MonoInterpFrameHandle *interp_frame, gpointer *handler_ip)
{
	*has_resume_state = FALSE;
}

static gboolean
stub_run_finally (StackFrameInfo *frame, int clause_index)
{
	g_assert_not_reached ();
}

static gboolean
stub_run_filter (StackFrameInfo *frame, MonoException *ex, int clause_index, gpointer handler_ip, gpointer handler_ip_end)
{
	g_assert_not_reached ();
	return FALSE;
}

static gboolean
stub_run_clause_with_il_state (gpointer il_state, int clause_index, MonoObject *ex, gboolean *filtered)
{
	g_assert_not_reached ();
}

static void
stub_frame_iter_init (MonoInterpStackIter *iter, gpointer interp_exit_data)
{
	g_assert_not_reached ();
}

static gboolean
stub_frame_iter_next (MonoInterpStackIter *iter, StackFrameInfo *frame)
{
	g_assert_not_reached ();
	return FALSE;
}

static gpointer
stub_create_method_pointer (MonoMethod *method, gboolean compile, MonoError *error)
{
	g_assert_not_reached ();
	return NULL;
}

static MonoFtnDesc*
stub_create_method_pointer_llvmonly (MonoMethod *method, gboolean compile, MonoError *error)
{
	g_assert_not_reached ();
	return NULL;
}

static void
stub_free_method (MonoMethod *method)
{
	g_assert_not_reached ();
}

static MonoObject*
stub_runtime_invoke (MonoMethod *method, void *obj, void **params, MonoObject **exc, MonoError *error)
{
	g_assert_not_reached ();
	return NULL;
}

static void
stub_init_delegate (MonoDelegate *del, MonoDelegateTrampInfo **info, MonoError *error)
{
	g_assert_not_reached ();
}

static gpointer
stub_get_remoting_invoke (MonoMethod *method, gpointer imethod, MonoError *error)
{
	g_assert_not_reached ();
	return NULL;
}

static void
stub_delegate_ctor (MonoObjectHandle this_obj, MonoObjectHandle target, gpointer addr, MonoError *error)
{
	g_assert_not_reached ();
}

static void
stub_entry_from_trampoline (gpointer ccontext, gpointer imethod)
{
	g_assert_not_reached ();
}

static void
stub_to_native_trampoline (gpointer addr, gpointer ccontext)
{
	g_assert_not_reached ();
}

static void
stub_frame_arg_to_data (MonoInterpFrameHandle frame, MonoMethodSignature *sig, int index, gpointer data)
{
g_assert_not_reached ();
}

static void
stub_data_to_frame_arg (MonoInterpFrameHandle frame, MonoMethodSignature *sig, int index, gconstpointer data)
{
	g_assert_not_reached ();
}

static gpointer
stub_frame_arg_to_storage (MonoInterpFrameHandle frame, MonoMethodSignature *sig, int index)
{
	g_assert_not_reached ();
	return NULL;
}

static void
stub_free_context (gpointer context)
{
	g_assert_not_reached ();
}

static void
stub_mark_stack (gpointer thread_data, GcScanFunc func, gpointer gc_data, gboolean precise)
{
}

static void
stub_jit_info_foreach (InterpJitInfoFunc func, gpointer user_data)
{
}

static gboolean
stub_sufficient_stack (gsize size)
{
	g_assert_not_reached ();
}

static void
stub_entry_llvmonly (gpointer res, gpointer *args, gpointer imethod)
{
	g_assert_not_reached ();
}

static gpointer
stub_get_interp_method (MonoMethod *method)
{
	g_assert_not_reached ();
	return NULL;
}

static MonoJitInfo*
stub_compile_interp_method (MonoMethod *method, MonoError *error)
{
	g_assert_not_reached ();
	return NULL;
}

static gboolean
stub_jit_call_can_be_supported (MonoMethod *method, MonoMethodSignature *sig, gboolean is_llvm_only)
{
	return TRUE;
}

#undef MONO_EE_CALLBACK
#define MONO_EE_CALLBACK(ret, name, sig) stub_ ## name,

static const MonoEECallbacks mono_interp_stub_callbacks = {
	MONO_EE_CALLBACKS
};

void
mono_interp_stub_init (void)
{
	if (mini_get_interp_callbacks ())
		/* already initialized */
		return;

	mini_install_interp_callbacks (&mono_interp_stub_callbacks);
}
