#include <config.h>

#ifndef ENABLE_INTERPRETER

#include "interp.h"

/* Dummy versions of interpreter functions to avoid ifdefs at call sites */

MonoJitInfo*
mono_interp_find_jit_info (MonoDomain *domain, MonoMethod *method)
{
	return NULL;
}

void
mono_interp_set_breakpoint (MonoJitInfo *jinfo, gpointer ip)
{
	g_assert_not_reached ();
}

void
mono_interp_clear_breakpoint (MonoJitInfo *jinfo, gpointer ip)
{
	g_assert_not_reached ();
}

MonoJitInfo*
mono_interp_frame_get_jit_info (MonoInterpFrameHandle frame)
{
	g_assert_not_reached ();
	return NULL;
}

gpointer
mono_interp_frame_get_ip (MonoInterpFrameHandle frame)
{
	g_assert_not_reached ();
	return NULL;
}

gpointer
mono_interp_frame_get_arg (MonoInterpFrameHandle frame, int pos)
{
	g_assert_not_reached ();
	return NULL;
}

gpointer
mono_interp_frame_get_local (MonoInterpFrameHandle frame, int pos)
{
	g_assert_not_reached ();
	return NULL;
}

gpointer
mono_interp_frame_get_this (MonoInterpFrameHandle frame)
{
	g_assert_not_reached ();
	return NULL;
}

void
mono_interp_start_single_stepping (void)
{
}

void
mono_interp_stop_single_stepping (void)
{
}

void
mono_interp_set_resume_state (MonoJitTlsData *jit_tls, MonoException *ex, MonoInterpFrameHandle interp_frame, gpointer handler_ip)
{
	g_assert_not_reached ();
}

void
mono_interp_run_finally (StackFrameInfo *frame, int clause_index, gpointer handler_ip)
{
	g_assert_not_reached ();
}

void
mono_interp_frame_iter_init (MonoInterpStackIter *iter, gpointer interp_exit_data)
{
	g_assert_not_reached ();
}

gboolean
mono_interp_frame_iter_next (MonoInterpStackIter *iter, StackFrameInfo *frame)
{
	g_assert_not_reached ();
	return FALSE;
}

#endif

