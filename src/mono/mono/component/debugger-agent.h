// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

#ifndef __MONO_DEBUGGER_AGENT_COMPONENT_H__
#define __MONO_DEBUGGER_AGENT_COMPONENT_H__

#include <mono/mini/mini.h>
#include "debugger.h"
#include <mono/utils/mono-stack-unwinding.h>

void
debugger_agent_add_function_pointers (MonoComponentDebugger* fn_table);

void
mono_ss_calculate_framecount (void *the_tls, MonoContext *ctx, gboolean force_use_ctx, DbgEngineStackFrame ***frames, int *nframes);

void
mono_ss_discard_frame_context (void *the_tls);

#ifdef TARGET_WASM
DebuggerTlsData*
mono_wasm_get_tls (void);

void
mono_init_debugger_agent_for_wasm (int log_level, MonoProfilerHandle *prof);

void 
mono_change_log_level (int new_log_level);

void
mono_wasm_save_thread_context (void);

bool
mono_wasm_is_breakpoint_and_stepping_disabled (void);
#endif

void
mini_wasm_debugger_add_function_pointers (MonoComponentDebugger* fn_table);

void
mini_wasi_debugger_add_function_pointers (MonoComponentDebugger* fn_table);

#if defined(HOST_WASI)
void
mono_wasi_suspend_current (void);

void
mono_debugger_agent_initialize_function_pointers (void *start_debugger_thread, void *suspend_vm, void *suspend_current);
#endif

MdbgProtErrorCode
mono_do_invoke_method (DebuggerTlsData *tls, MdbgProtBuffer *buf, InvokeData *invoke, guint8 *p, guint8 **endp);

MdbgProtErrorCode
mono_process_dbg_packet (int id, MdbgProtCommandSet command_set, int command, gboolean *no_reply, guint8 *p, guint8 *end, MdbgProtBuffer *buf);

void
mono_dbg_process_breakpoint_events (void *_evts, MonoMethod *method, MonoContext *ctx, int il_offset);

void*
mono_dbg_create_breakpoint_events (GPtrArray *ss_reqs, GPtrArray *bp_reqs, MonoJitInfo *ji, MdbgProtEventKind kind);

int
mono_ss_create_init_args (SingleStepReq *ss_req, SingleStepArgs *args);

void
mono_ss_args_destroy (SingleStepArgs *ss_args);

int
mono_de_frame_async_id (DbgEngineStackFrame *frame);

bool
mono_debugger_agent_receive_and_process_command (void);

bool
mono_begin_breakpoint_processing (void *the_tls, MonoContext *ctx, MonoJitInfo *ji, gboolean from_signal);

void 
mono_dbg_assembly_load (MonoProfiler *prof, MonoAssembly *assembly);
#endif
