/* \file
 * Soft Debugger stubs
 *
 * Author:
 *   Zoltan Varga (vargaz@gmail.com)
 *
 * Copyright 2009-2010 Novell, Inc.
 * Copyright 2011 Xamarin Inc.
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */

#include <config.h>

#include "mini-runtime.h"
#include "debugger-agent.h"

static void
stub_debugger_agent_parse_options (char *options)
{
	g_error ("This runtime is configured with the debugger agent disabled.");
}

static void
stub_debugger_agent_init (void)
{
}

static void
stub_debugger_agent_breakpoint_hit (void *sigctx)
{
}

static void
stub_debugger_agent_single_step_event (void *sigctx)
{
}

static void
stub_debugger_agent_free_domain_info (MonoDomain *domain)
{
}

static void
stub_debugger_agent_handle_exception (MonoException *exc, MonoContext *throw_ctx,
									  MonoContext *catch_ctx, StackFrameInfo *catch_frame)
{
}

static void
stub_debugger_agent_begin_exception_filter (MonoException *exc, MonoContext *ctx, MonoContext *orig_ctx)
{
}

static void
stub_debugger_agent_end_exception_filter (MonoException *exc, MonoContext *ctx, MonoContext *orig_ctx)
{
}

static void
stub_debugger_agent_user_break (void)
{
	G_BREAKPOINT ();
}

static void
stub_debugger_agent_debug_log (int level, MonoString *category, MonoString *message)
{
}

static gboolean
stub_debugger_agent_debug_log_is_enabled (void)
{
	return FALSE;
}

static void
stub_debugger_agent_single_step_from_context (MonoContext *ctx)
{
	g_assert_not_reached ();
}

static void
stub_debugger_agent_breakpoint_from_context (MonoContext *ctx)
{
	g_assert_not_reached ();
}

static void
stub_debugger_agent_send_crash (char *json_dump, MonoStackHash *hashes, int pause)
{
}

void
mono_debugger_agent_stub_init (void)
{
	MonoDebuggerCallbacks cbs;

	memset (&cbs, 0, sizeof (MonoDebuggerCallbacks));
	cbs.version = MONO_DBG_CALLBACKS_VERSION;
	cbs.parse_options = stub_debugger_agent_parse_options;
	cbs.init = stub_debugger_agent_init;
	cbs.breakpoint_hit = stub_debugger_agent_breakpoint_hit;
	cbs.single_step_event = stub_debugger_agent_single_step_event;
	cbs.single_step_from_context = stub_debugger_agent_single_step_from_context;
	cbs.breakpoint_from_context = stub_debugger_agent_breakpoint_from_context;
	cbs.free_domain_info = stub_debugger_agent_free_domain_info;
	cbs.handle_exception = stub_debugger_agent_handle_exception;
	cbs.begin_exception_filter = stub_debugger_agent_begin_exception_filter;
	cbs.end_exception_filter = stub_debugger_agent_end_exception_filter;
	cbs.user_break = stub_debugger_agent_user_break;
	cbs.debug_log = stub_debugger_agent_debug_log;
	cbs.debug_log_is_enabled = stub_debugger_agent_debug_log_is_enabled;
	cbs.send_crash = stub_debugger_agent_send_crash;

	mini_install_dbg_callbacks (&cbs);
}
