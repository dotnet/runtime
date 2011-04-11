#ifndef __MONO_DEBUGGER_AGENT_H__
#define __MONO_DEBUGGER_AGENT_H__

#include "mini.h"

/* IL offsets used to mark the sequence points belonging to method entry/exit events */
#define METHOD_ENTRY_IL_OFFSET -1
#define METHOD_EXIT_IL_OFFSET 0xffffff

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

void
mono_debugger_agent_handle_exception (MonoException *ext, MonoContext *throw_ctx, MonoContext *catch_ctx) MONO_INTERNAL;

void
mono_debugger_agent_begin_exception_filter (MonoException *exc, MonoContext *ctx, MonoContext *orig_ctx) MONO_INTERNAL;

void
mono_debugger_agent_end_exception_filter (MonoException *exc, MonoContext *ctx, MonoContext *orig_ctx) MONO_INTERNAL;

#endif
