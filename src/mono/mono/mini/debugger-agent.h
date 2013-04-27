#ifndef __MONO_DEBUGGER_AGENT_H__
#define __MONO_DEBUGGER_AGENT_H__

#include "mini.h"

/* IL offsets used to mark the sequence points belonging to method entry/exit events */
#define METHOD_ENTRY_IL_OFFSET -1
#define METHOD_EXIT_IL_OFFSET 0xffffff

/* Native offset used to mark seq points in dead code */
#define SEQ_POINT_NATIVE_OFFSET_DEAD_CODE -1

void
mono_debugger_agent_parse_options (char *options) MONO_INTERNAL;

void
mono_debugger_agent_init (void) MONO_INTERNAL;

void
mono_debugger_agent_breakpoint_hit (void *sigctx) MONO_INTERNAL;

void
mono_debugger_agent_single_step_event (void *sigctx) MONO_INTERNAL;

void
debugger_agent_single_step_from_context (MonoContext *ctx) MONO_INTERNAL;

void
debugger_agent_breakpoint_from_context (MonoContext *ctx) MONO_INTERNAL;

void
mono_debugger_agent_free_domain_info (MonoDomain *domain) MONO_INTERNAL;

gboolean mono_debugger_agent_thread_interrupt (void *sigctx, MonoJitInfo *ji) MONO_INTERNAL;

#ifdef PLATFORM_ANDROID
void
mono_debugger_agent_unhandled_exception (MonoException *exc);
#endif

void
mono_debugger_agent_handle_exception (MonoException *ext, MonoContext *throw_ctx, MonoContext *catch_ctx) MONO_INTERNAL;

void
mono_debugger_agent_begin_exception_filter (MonoException *exc, MonoContext *ctx, MonoContext *orig_ctx) MONO_INTERNAL;

void
mono_debugger_agent_end_exception_filter (MonoException *exc, MonoContext *ctx, MonoContext *orig_ctx) MONO_INTERNAL;

void
mono_debugger_agent_user_break (void) MONO_INTERNAL;

void
mono_debugger_agent_debug_log (int level, MonoString *category, MonoString *message) MONO_INTERNAL;

gboolean
mono_debugger_agent_debug_log_is_enabled (void) MONO_INTERNAL;

gboolean
mono_debugger_agent_transport_handshake (void) MONO_INTERNAL;

#endif
