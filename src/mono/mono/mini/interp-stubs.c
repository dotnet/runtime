#include <config.h>

#include "interp/interp.h"

/* interpreter callback stubs */

static MonoJitInfo*
stub_find_jit_info (MonoDomain *domain, MonoMethod *method)
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
stub_set_resume_state (MonoJitTlsData *jit_tls, MonoException *ex, MonoJitExceptionInfo *ei, MonoInterpFrameHandle interp_frame, gpointer handler_ip)
{
	g_assert_not_reached ();
}

static gboolean
stub_run_finally (StackFrameInfo *frame, int clause_index, gpointer handler_ip)
{
	g_assert_not_reached ();
}

static gboolean
stub_run_filter (StackFrameInfo *frame, MonoException *ex, int clause_index, gpointer handler_ip)
{
	g_assert_not_reached ();
	return FALSE;
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
stub_create_method_pointer (MonoMethod *method, MonoError *error)
{
	g_assert_not_reached ();
	return NULL;
}

static MonoObject*
stub_runtime_invoke (MonoMethod *method, void *obj, void **params, MonoObject **exc, MonoError *error)
{
	g_assert_not_reached ();
	return NULL;
}

static void
stub_init_delegate (MonoDelegate *del)
{
	g_assert_not_reached ();
}

#ifndef DISABLE_REMOTING
static gpointer
stub_get_remoting_invoke (gpointer imethod, MonoError *error)
{
	g_assert_not_reached ();
	return NULL;
}
#endif

static gpointer
stub_create_trampoline (MonoDomain *domain, MonoMethod *method, MonoError *error)
{
	g_assert_not_reached ();
	return NULL;
}

static void
stub_walk_stack_with_ctx (MonoInternalStackWalk func, MonoContext *ctx, MonoUnwindOptions options, void *user_data)
{
	g_assert_not_reached ();
}

void
mono_interp_stub_init (void)
{
	MonoInterpCallbacks c;
	c.create_method_pointer = stub_create_method_pointer;
	c.runtime_invoke = stub_runtime_invoke;
	c.init_delegate = stub_init_delegate;
#ifndef DISABLE_REMOTING
	c.get_remoting_invoke = stub_get_remoting_invoke;
#endif
	c.create_trampoline = stub_create_trampoline;
	c.walk_stack_with_ctx = stub_walk_stack_with_ctx;
	c.set_resume_state = stub_set_resume_state;
	c.run_finally = stub_run_finally;
	c.run_filter = stub_run_filter;
	c.frame_iter_init = stub_frame_iter_init;
	c.frame_iter_next = stub_frame_iter_next;
	c.find_jit_info = stub_find_jit_info;
	c.set_breakpoint = stub_set_breakpoint;
	c.clear_breakpoint = stub_clear_breakpoint;
	c.frame_get_jit_info = stub_frame_get_jit_info;
	c.frame_get_ip = stub_frame_get_ip;
	c.frame_get_arg = stub_frame_get_arg;
	c.frame_get_local = stub_frame_get_local;
	c.frame_get_this = stub_frame_get_this;
	c.frame_get_parent = stub_frame_get_parent;
	c.start_single_stepping = stub_start_single_stepping;
	c.stop_single_stepping = stub_stop_single_stepping;
	mini_install_interp_callbacks (&c);
}
