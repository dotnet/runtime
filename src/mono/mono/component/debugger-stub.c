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
stub_debugger_init (void);

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

static void
stub_debugger_send_crash (char *json_dump, MonoStackHash *hashes, int pause);

static void stub_register_transport (DebuggerTransport *trans); //debugger-agent
static gboolean stub_mono_debugger_agent_transport_handshake (void);
static void stub_mono_debugger_agent_parse_options (char *options);
static void stub_mono_de_init (DebuggerEngineCallbacks *cbs); //debugger-engine
static void stub_mono_debugger_free_objref (gpointer value); //debugger-engine removeAfterMergeWasmPR
static void stub_mono_de_set_log_level (int level, FILE *file); //debugger-engine removeAfterMergeWasmPR
static void stub_mono_de_add_pending_breakpoints (MonoMethod *method, MonoJitInfo *ji); //debugger-engine removeAfterMergeWasmPR
static void stub_mono_de_clear_breakpoint (MonoBreakpoint *bp); //debugger-engine removeAfterMergeWasmPR
static void stub_mono_de_process_single_step (void *tls, gboolean from_signal); //debugger-engine removeAfterMergeWasmPR
static void stub_mono_de_process_breakpoint (void *tls, gboolean from_signal); //debugger-engine removeAfterMergeWasmPR
static MonoBreakpoint *stub_mono_de_set_breakpoint (MonoMethod *method, long il_offset, EventRequest *req, MonoError *error); //debugger-engine removeAfterMergeWasmPR
static void stub_mono_de_cancel_all_ss (void); //debugger-engine removeAfterMergeWasmPR
static DbgEngineErrorCode stub_mono_de_ss_create (MonoInternalThread *thread, MdbgProtStepSize size, MdbgProtStepDepth depth, MdbgProtStepFilter filter, EventRequest *req); //debugger-engine removeAfterMergeWasmPR
static void stub_mono_de_domain_add (MonoDomain *domain); //debugger-engine removeAfterMergeWasmPR
static void stub_mono_de_collect_breakpoints_by_sp (SeqPoint *sp, MonoJitInfo *ji, GPtrArray *ss_reqs, GPtrArray *bp_reqs); //debugger-engine removeAfterMergeWasmPR
static MonoBreakpoint *stub_mono_de_get_breakpoint_by_id (int id); //debugger-engine removeAfterMergeWasmPR
static DbgEngineErrorCode stub_mono_de_set_interp_var (MonoType *t, gpointer addr, guint8 *val_buf); //debugger-engine removeAfterMergeWasmPR
static gboolean stub_set_set_notification_for_wait_completion_flag (DbgEngineStackFrame *frame); //debugger-engine removeAfterMergeWasmPR
static MonoMethod *stub_get_notify_debugger_of_wait_completion_method (void); //debugger-engine removeAfterMergeWasmPR
static MonoClass *stub_get_class_to_get_builder_field (DbgEngineStackFrame *frame); //debugger-engine removeAfterMergeWasmPR
static MonoMethod *stub_get_object_id_for_debugger_method (MonoClass *async_builder_class); //debugger-engine removeAfterMergeWasmPR
static void stub_mono_de_clear_all_breakpoints (void); //debugger-engine removeAfterMergeWasmPR
static gpointer stub_get_async_method_builder (DbgEngineStackFrame *frame); //debugger-engine removeAfterMergeWasmPR

static MonoComponentDebugger fn_table = {
	{ MONO_COMPONENT_ITF_VERSION, &debugger_avaliable },
	&stub_debugger_parse_options,
	&stub_debugger_init,
	&stub_debugger_breakpoint_hit,
	&stub_debugger_single_step_event,
	&stub_debugger_single_step_from_context,
	&stub_debugger_breakpoint_from_context,
	&stub_debugger_free_mem_manager,
	&stub_debugger_unhandled_exception,
	&stub_debugger_handle_exception,
	&stub_debugger_begin_exception_filter,
	&stub_debugger_end_exception_filter,
	&stub_debugger_user_break,
	&stub_debugger_debug_log,
	&stub_debugger_debug_log_is_enabled,
	&stub_debugger_send_crash,
	&stub_register_transport,
	&stub_mono_debugger_agent_transport_handshake,
	&stub_mono_debugger_agent_parse_options,
	&stub_mono_de_init,
	&stub_mono_debugger_free_objref,
	&stub_mono_de_set_log_level,
	&stub_mono_de_add_pending_breakpoints,
	&stub_mono_de_clear_breakpoint,
	&stub_mono_de_process_single_step,
	&stub_mono_de_process_breakpoint,
	&stub_mono_de_set_breakpoint,
	&stub_mono_de_cancel_all_ss,
	&stub_mono_de_ss_create,
	&stub_mono_de_domain_add,
	&stub_mono_de_collect_breakpoints_by_sp,
	&stub_mono_de_get_breakpoint_by_id,
	&stub_mono_de_set_interp_var,
	&stub_set_set_notification_for_wait_completion_flag,
	&stub_get_notify_debugger_of_wait_completion_method,
	&stub_get_class_to_get_builder_field,
	&stub_get_object_id_for_debugger_method,
	&stub_mono_de_clear_all_breakpoints,
	&stub_get_async_method_builder
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
	g_error ("This runtime is configured with the debugger agent disabled.");
}

static void
stub_debugger_init (void)
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

static void
stub_debugger_send_crash (char *json_dump, MonoStackHash *hashes, int pause)
{
}

static void 
stub_register_transport(DebuggerTransport *trans) //debugger-agent
{
}

static gboolean 
stub_mono_debugger_agent_transport_handshake(void)
{
	g_assert_not_reached();
}

static void 
stub_mono_debugger_agent_parse_options (char *options) 
{
}

static void 
stub_mono_de_init (DebuggerEngineCallbacks *cbs)
{
}

static void 
stub_mono_debugger_free_objref (gpointer value)
{
}

static void 
stub_mono_de_set_log_level (int level, FILE *file)
{
}

static void 
stub_mono_de_add_pending_breakpoints (MonoMethod *method, MonoJitInfo *ji)
{
}

static void 
stub_mono_de_clear_breakpoint (MonoBreakpoint *bp)
{
}

static void 
stub_mono_de_process_single_step (void *tls, gboolean from_signal)
{
}

static void
stub_mono_de_process_breakpoint (void *tls, gboolean from_signal)
{
}

static MonoBreakpoint *
stub_mono_de_set_breakpoint (MonoMethod *method, long il_offset, EventRequest *req, MonoError *error)
{
	g_assert_not_reached();
}

static void 
stub_mono_de_cancel_all_ss (void)
{
}

static DbgEngineErrorCode 
stub_mono_de_ss_create (MonoInternalThread *thread, MdbgProtStepSize size, MdbgProtStepDepth depth, MdbgProtStepFilter filter, EventRequest *req)
{
	g_assert_not_reached();
}

static void 
stub_mono_de_domain_add (MonoDomain *domain)
{
}

static void 
stub_mono_de_collect_breakpoints_by_sp (SeqPoint *sp, MonoJitInfo *ji, GPtrArray *ss_reqs, GPtrArray *bp_reqs)
{
}

static MonoBreakpoint *
stub_mono_de_get_breakpoint_by_id (int id)
{
	g_assert_not_reached();
}

static DbgEngineErrorCode 
stub_mono_de_set_interp_var (MonoType *t, gpointer addr, guint8 *val_buf)
{
	g_assert_not_reached();
}

static gboolean 
stub_set_set_notification_for_wait_completion_flag (DbgEngineStackFrame *frame)
{
	g_assert_not_reached();
}

static MonoMethod *
stub_get_notify_debugger_of_wait_completion_method (void)
{
	g_assert_not_reached();
}

static MonoClass *
stub_get_class_to_get_builder_field (DbgEngineStackFrame *frame)
{
	g_assert_not_reached();
}

static MonoMethod *
stub_get_object_id_for_debugger_method (MonoClass *async_builder_class)
{
	g_assert_not_reached();
}

static void stub_mono_de_clear_all_breakpoints (void)
{
}

static gpointer stub_get_async_method_builder (DbgEngineStackFrame *frame)
{
	g_assert_not_reached();
}
