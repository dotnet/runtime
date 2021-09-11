// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

#include <config.h>

#include "mono/mini/mini-runtime.h"
#include "debugger-agent.h"

#include <mono/component/debugger.h>

static bool
debugger_avaliable (void);

static void
stub_debugger_parse_options (char *options);

static void
stub_debugger_init (MonoDefaults *mono_defaults);

static void
stub_debugger_breakpoint_hit (void *sigctx);

static void
stub_debugger_single_step_event (void *sigctx);

static void
stub_debugger_free_mem_manager (gpointer mem_manager);

static void
stub_debugger_handle_exception (MonoException *exc, MonoContext *throw_ctx, MonoContext *catch_ctx, StackFrameInfo *catch_frame);

static void
stub_debugger_begin_exception_filter (MonoException *exc, MonoContext *ctx, MonoContext *orig_ctx);

static void
stub_debugger_end_exception_filter (MonoException *exc, MonoContext *ctx, MonoContext *orig_ctx);

static void
stub_debugger_user_break (void);

static void
stub_debugger_debug_log (int level, MonoString *category, MonoString *message);

static gboolean
stub_debugger_debug_log_is_enabled (void);

static void
stub_debugger_unhandled_exception (MonoException *exc);

static void
stub_debugger_single_step_from_context (MonoContext *ctx);

static void
stub_debugger_breakpoint_from_context (MonoContext *ctx);

static gboolean 
stub_debugger_transport_handshake (void);

static void
stub_mono_wasm_breakpoint_hit (void);

static void
stub_mono_wasm_single_step_hit (void);

static void 
stub_send_enc_delta (MonoImage *image, gconstpointer dmeta_bytes, int32_t dmeta_len, gconstpointer dpdb_bytes, int32_t dpdb_len);

static MonoComponentDebugger fn_table = {
	{ MONO_COMPONENT_ITF_VERSION, &debugger_avaliable },
	&stub_debugger_init,
	&stub_debugger_user_break,	
	&stub_debugger_parse_options,
	&stub_debugger_breakpoint_hit,
	&stub_debugger_single_step_event,
	&stub_debugger_single_step_from_context,
	&stub_debugger_breakpoint_from_context,
	&stub_debugger_free_mem_manager,
	&stub_debugger_unhandled_exception,
	&stub_debugger_handle_exception,
	&stub_debugger_begin_exception_filter,
	&stub_debugger_end_exception_filter,
	&stub_debugger_debug_log,
	&stub_debugger_debug_log_is_enabled,
	&stub_debugger_transport_handshake,

	//wasm
	&stub_mono_wasm_breakpoint_hit,
	&stub_mono_wasm_single_step_hit,

	//HotReload
	&stub_send_enc_delta,
};

static bool
debugger_avaliable (void)
{
	return false;
}

MonoComponentDebugger *
mono_component_debugger_init (void)
{
	return &fn_table;
}

static void
stub_debugger_parse_options (char *options)
{
	if (!options)
		return;
	g_error ("This runtime is configured with the debugger agent disabled.");
}

static void
stub_debugger_init (MonoDefaults *mono_defaults)
{
}

static void
stub_debugger_breakpoint_hit (void *sigctx)
{
}

static void
stub_debugger_single_step_event (void *sigctx)
{
}

static void
stub_debugger_free_mem_manager (gpointer mem_manager)
{
}

static void
stub_debugger_handle_exception (MonoException *exc, MonoContext *throw_ctx,
									  MonoContext *catch_ctx, StackFrameInfo *catch_frame)
{
}

static void
stub_debugger_begin_exception_filter (MonoException *exc, MonoContext *ctx, MonoContext *orig_ctx)
{
}

static void
stub_debugger_end_exception_filter (MonoException *exc, MonoContext *ctx, MonoContext *orig_ctx)
{
}

static void
stub_debugger_user_break (void)
{
	G_BREAKPOINT ();
}

static void
stub_debugger_debug_log (int level, MonoString *category, MonoString *message)
{
}

static gboolean
stub_debugger_debug_log_is_enabled (void)
{
	return FALSE;
}

static void
stub_debugger_unhandled_exception (MonoException *exc)
{
	g_assert_not_reached ();
}

static void
stub_debugger_single_step_from_context (MonoContext *ctx)
{
	g_assert_not_reached ();
}

static void
stub_debugger_breakpoint_from_context (MonoContext *ctx)
{
	g_assert_not_reached ();
}

static gboolean 
stub_debugger_transport_handshake (void)
{
	g_assert_not_reached();
}

static void
stub_mono_wasm_breakpoint_hit (void)
{
}

static void
stub_mono_wasm_single_step_hit (void)
{
}

static void 
stub_send_enc_delta (MonoImage *image, gconstpointer dmeta_bytes, int32_t dmeta_len, gconstpointer dpdb_bytes, int32_t dpdb_len)
{
}

#ifdef HOST_WASM

#include <emscripten.h>

//functions exported to be used by JS
G_BEGIN_DECLS

EMSCRIPTEN_KEEPALIVE gboolean mono_wasm_send_dbg_command (int id, int command_set, int command, guint8* data, unsigned int size);
EMSCRIPTEN_KEEPALIVE gboolean mono_wasm_send_dbg_command_with_parms (int id, int command_set, int command, guint8* data, unsigned int size, int valtype, char* newvalue);

G_END_DECLS


EMSCRIPTEN_KEEPALIVE gboolean 
mono_wasm_send_dbg_command_with_parms (int id, int command_set, int command, guint8* data, unsigned int size, int valtype, char* newvalue)
{
	return false;
}

EMSCRIPTEN_KEEPALIVE gboolean 
mono_wasm_send_dbg_command (int id, int command_set, int command, guint8* data, unsigned int size)
{
	return false;
}

#endif // HOST_WASM


